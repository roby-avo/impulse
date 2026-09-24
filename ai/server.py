"""Local Laya inference with a warmed checkpoint and a bounded worker.
The SDK selects every answer; this module contains no game policy.
"""
import os
os.environ.setdefault('USE_TF', '0')
os.environ.setdefault('HF_HUB_OFFLINE', '1')
os.environ.setdefault('LAYA_THREADS', '4')
os.environ['LAYA_MODELS'] = 'english'
os.environ['LAYA_PRELOAD'] = '1'
os.environ['LAYA_AUTO_TASK'] = '0'
import asyncio
from concurrent.futures import ThreadPoolExecutor
from contextlib import asynccontextmanager
import json
import logging
from pathlib import Path
import time
import torch
from fastapi import FastAPI, Header, HTTPException, Request
from laya.serve import build_router
import uvicorn

RUNTIME_VERSION = '0.9.1'

def warmup_payload():
    root = Path(__file__).resolve().parent.parent
    return {'model': 'english', 'state': {
        'self': {'distance_to_edge_m': 4, 'holding_object': False, 'speed': 0, 'balance': 1},
        'opponent': {'distance_m': 3, 'near_edge': True, 'holding_object': False},
        'nearest_object': {'type': 'barrel', 'distance_m': 1.5},
        'incoming_projectile': False, 'previous_action': 'none'},
        'questions': json.loads((root/'Assets/StreamingAssets/laya-question.json').read_text())}

class InferenceRuntime:
    def __init__(self, router):
        self.router = router
        self.agent = router.load('english')
        self.agent.cfg.update(max_len=1024, head_max_len=384)
        self.busy = False
        self.last_ms = 0.0
        self.completed = 0
        self.worker = ThreadPoolExecutor(max_workers=1, thread_name_prefix='laya-inference')

    def predict(self, body):
        started = time.perf_counter()
        with torch.inference_mode():
            result = self.router.predict(body.get('state'), body['questions'], model='english')
        self.last_ms = (time.perf_counter()-started)*1000
        self.completed += 1
        return result

    def released(self, future):
        self.busy = False
        # Retrieve exceptions even when an HTTP client disconnected before completion.
        if not future.cancelled():
            future.exception()


def create_game_app(runtime):
    @asynccontextmanager
    async def lifespan(_):
        yield
        runtime.worker.shutdown(wait=True, cancel_futures=True)

    app = FastAPI(title='Physics Playground · local Laya', lifespan=lifespan)

    @app.get('/health')
    async def health():
        return {'status': 'ok', 'loaded': runtime.router.loaded,
                'device': str(runtime.agent.device), 'runtime_version': RUNTIME_VERSION,
                'threads': torch.get_num_threads(), 'busy': runtime.busy,
                'last_inference_ms': round(runtime.last_ms, 1), 'completed': runtime.completed}

    @app.post('/v1/systemone')
    async def systemone(request: Request, authorization: str | None = Header(default=None)):
        api_key = os.environ.get('LAYA_API_KEY')
        if api_key and authorization != 'Bearer '+api_key:
            raise HTTPException(401, 'invalid or missing bearer token')
        body = await request.json()
        if not isinstance(body, dict) or not isinstance(body.get('questions'), dict) or not body['questions']:
            raise HTTPException(400, 'questions must be a non-empty object')
        if body.get('model', 'english') != 'english':
            raise HTTPException(422, 'this game service only hosts the English Laya checkpoint')
        if runtime.busy:
            # Reject overload instead of building a queue of obsolete world states.
            raise HTTPException(429, 'inference busy; retry with fresh state', headers={'Retry-After': '1'})
        runtime.busy = True
        future = asyncio.get_running_loop().run_in_executor(runtime.worker, runtime.predict, body)
        future.add_done_callback(runtime.released)
        try:
            result = await asyncio.shield(future)
        except Exception as exc:
            logging.getLogger("uvicorn.error").error("Local inference failed (%s)", type(exc).__name__)
            raise HTTPException(422, 'local inference failed; check service diagnostics') from None
        from fastapi.responses import JSONResponse
        return JSONResponse(result, headers={'Server-Timing': f'inference;dur={runtime.last_ms:.1f}',
                                             'X-Laya-Device': str(runtime.agent.device),
                                             'X-Laya-Inference-Ms': f'{runtime.last_ms:.1f}'})
    return app


def main():
    # Prefer the Apple GPU when present. CPU remains available as an explicit override.
    os.environ.setdefault('LAYA_DEVICE', 'mps' if torch.backends.mps.is_available() else 'cpu')
    torch.set_num_interop_threads(1)
    runtime = InferenceRuntime(build_router())
    # Warm on the SAME worker that serves gameplay, before reporting readiness.
    try:
        runtime.worker.submit(runtime.predict, warmup_payload()).result()
    except RuntimeError:
        if runtime.agent.device.type == 'cpu':
            raise
        # Device recovery uses exactly the same checkpoint and weights, never another policy.
        runtime.agent.device = torch.device('cpu')
        runtime.agent.model.to('cpu')
        runtime.worker.submit(runtime.predict, warmup_payload()).result()
    print(f'Laya ready: convaiinnovations/laya; {runtime.agent.device}; '
          f'warmup {runtime.last_ms:.0f} ms; runtime {RUNTIME_VERSION}; no fallback policy.', flush=True)
    uvicorn.run(create_game_app(runtime), host='127.0.0.1', port=8000, log_level='info')

if __name__ == '__main__':
    main()
