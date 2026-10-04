"""Local System One inference for IMPULSE with catalog checkpoints and one bounded worker.
The requested checkpoint selects every answer; this module contains no game policy and
never substitutes another model for the one a request names.
"""
import os
os.environ.setdefault('USE_TF', '0')
os.environ.setdefault('HF_HUB_OFFLINE', '1')
os.environ.setdefault('LAYA_THREADS', '4')
import asyncio
from collections import OrderedDict
from concurrent.futures import ThreadPoolExecutor
from contextlib import asynccontextmanager
import gc
import json
import logging
from pathlib import Path
import subprocess
import sys
import threading
import time
import torch
from fastapi import FastAPI, Header, HTTPException, Request
from fastapi.responses import JSONResponse
import uvicorn
import models as catalog
from models import RUNTIME_VERSION

QUEUE_WAIT_S = .6     # Two AI players share one worker; a short wait beats a 429 storm.
REWARM_AFTER_S = 60   # Accelerators page out after long idle; the game asks for a re-warm.
log = logging.getLogger('uvicorn.error')


class InferenceRuntime:
    def __init__(self, device, max_loaded=2, loader=catalog.load_agent):
        self.device = device
        self.max_loaded = max(1, max_loaded)
        self.load_agent = loader
        self.agents = OrderedDict()   # least recently used first
        self.loading = set()
        self.errors = {}
        self.downloads = {}
        self.download_errors = {}
        self.lock = threading.RLock()
        self.busy = False
        self.last_ms = 0.0
        self.last_used = time.monotonic()
        self.completed = 0
        self.worker = ThreadPoolExecutor(max_workers=1, thread_name_prefix='laya-inference')
        self.loader = ThreadPoolExecutor(max_workers=1, thread_name_prefix='laya-loader')

    @property
    def actual_device(self):
        with self.lock:
            agent = next(reversed(self.agents.values()), None)
        return str(agent.device) if agent is not None else str(self.device)

    def run(self, agent, name, state, questions):
        started = time.perf_counter()
        with torch.inference_mode():
            result = agent.system_one(state, questions)
        self.last_ms = (time.perf_counter() - started) * 1000
        self.last_used = time.monotonic()
        self.completed += 1
        result['model'] = name
        return result

    def warm(self, name, item, agent):
        """Runs on the inference worker. The warm-up doubles as a legality check."""
        request = catalog.game_request()
        try:
            result = self.run(agent, name, request['state'], request['questions'])
        except RuntimeError:
            if agent.device.type == 'cpu':
                raise
            # Device recovery keeps exactly the same checkpoint and weights, never another model.
            agent.device = torch.device('cpu')
            agent.model.to('cpu')
            result = self.run(agent, name, request['state'], request['questions'])
        ok, answer = catalog.check_answer(result, request['questions'])
        catalog.record_verification(item, ok, answer, self.last_ms, agent.device)
        if not ok:
            raise catalog.ModelError(f'{name} returned an answer the game cannot execute')

    def install(self, name, item, agent):
        """Runs on the inference worker: Metal is not thread-safe, so every accelerator step
        (move, warm-up, eviction, cache release) shares the thread that serves inference."""
        if str(self.device) != 'cpu':
            agent.model.to(self.device)
            agent.device = torch.device(self.device)
        self.warm(name, item, agent)
        with self.lock:
            self.agents[name] = agent
            self.agents.move_to_end(name)
            evicted = []
            while len(self.agents) > self.max_loaded:
                evicted.append(self.agents.popitem(last=False))
            self.errors.pop(name, None)
        for old_name in [entry[0] for entry in evicted]:
            log.info('Unloaded %s to stay within %d resident models', old_name, self.max_loaded)
        evicted.clear()  # release evicted weights here, on the worker
        gc.collect()
        if torch.backends.mps.is_available():
            torch.mps.empty_cache()

    def load(self, name):
        """Read weights on the loader thread (CPU only), then install on the worker. Blocking."""
        try:
            item = catalog.entry(name)
            agent = self.load_agent(item, 'cpu')
            self.worker.submit(self.install, name, item, agent).result()
            log.info('Model ready: %s on %s (warm-up %.0f ms)', name, agent.device, self.last_ms)
        except Exception as exc:
            message = str(exc) if isinstance(exc, catalog.ModelError) else f'{name} failed to load ({type(exc).__name__})'
            log.error('Model load failed: %s', message)
            with self.lock:
                self.errors[name] = message
        finally:
            with self.lock:
                self.loading.discard(name)

    def ensure(self, name, retry=False):
        """'loaded', 'loading' or 'error'. Loads in the background; never blocks a request."""
        with self.lock:
            if name in self.agents:
                self.agents.move_to_end(name)
                return 'loaded'
            if name in self.loading:
                return 'loading'
            if name in self.errors and not retry:
                return 'error'
            self.errors.pop(name, None)
            self.loading.add(name)
        self.loader.submit(self.load, name)
        return 'loading'

    def rewarm(self, name):
        """After long idle, run one game-shaped inference so the next real one is fast."""
        with self.lock:
            agent = self.agents.get(name)
        if agent is None or self.busy or time.monotonic() - self.last_used < REWARM_AFTER_S:
            return False
        request = catalog.game_request()
        self.worker.submit(self.run, agent, name, request['state'], request['questions'])
        return True

    def download(self, name):
        item = catalog.entry(name)
        if catalog.installed(item):
            return 'installed'
        process = self.downloads.get(name)
        if process and process.poll() is None:
            return 'downloading'
        self.download_errors.pop(name, None)
        # A separate process can go online even though this service stays offline.
        env = dict(os.environ, HF_HUB_OFFLINE='0')
        self.downloads[name] = subprocess.Popen(
            [sys.executable, str(catalog.AI / 'models.py'), '--json', 'download', name],
            cwd=catalog.AI, env=env, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True)
        return 'downloading'

    def download_state(self, name):
        process = self.downloads.get(name)
        if process is None:
            return None
        if process.poll() is None:
            return 'downloading'
        output, _ = process.communicate()
        del self.downloads[name]
        try:
            result = json.loads(output.strip().splitlines()[-1])
        except (ValueError, IndexError):
            result = {'state': 'error', 'message': f'download of {name} stopped unexpectedly'}
        if result.get('state') == 'error':
            self.download_errors[name] = result.get('message')
        return None

    def describe(self, item):
        name = item['id']
        state = catalog.status(item)
        downloading = self.download_state(name) == 'downloading'
        with self.lock:
            state.update(loaded=name in self.agents, loading=name in self.loading, error=self.errors.get(name))
        state.update(downloading=downloading, download_error=self.download_errors.get(name))
        if not downloading:
            state['download_progress'] = None
        return state

    def released(self, future):
        self.busy = False
        # Retrieve exceptions even when an HTTP client disconnected before completion.
        if not future.cancelled():
            future.exception()


def create_game_app(runtime):
    gate = asyncio.Semaphore(1)

    @asynccontextmanager
    async def lifespan(_):
        yield
        runtime.loader.shutdown(wait=False, cancel_futures=True)
        runtime.worker.shutdown(wait=True, cancel_futures=True)

    app = FastAPI(title='IMPULSE · local System One', lifespan=lifespan)

    def check_auth(authorization):
        api_key = os.environ.get('LAYA_API_KEY')
        if api_key and authorization != 'Bearer ' + api_key:
            raise HTTPException(401, 'invalid or missing bearer token')

    def known(name):
        try:
            return catalog.entry(name)
        except catalog.ModelError as exc:
            raise HTTPException(404, str(exc)) from None

    @app.get('/health')
    async def health():
        with runtime.lock:
            loaded, loading = list(runtime.agents), sorted(runtime.loading)
        return {'status': 'ok', 'loaded': loaded, 'loading': loading, 'default': catalog.default_model(),
                'device': runtime.actual_device, 'runtime_version': RUNTIME_VERSION,
                'threads': torch.get_num_threads(), 'busy': runtime.busy,
                'last_inference_ms': round(runtime.last_ms, 1), 'completed': runtime.completed}

    @app.get('/v1/models')
    async def models(authorization: str | None = Header(default=None)):
        check_auth(authorization)
        return {'default': catalog.default_model(), 'models': [runtime.describe(item) for item in catalog.catalog().values()]}

    @app.post('/v1/models/{name}/load')
    async def load(name: str, authorization: str | None = Header(default=None)):
        check_auth(authorization)
        item = known(name)
        if not catalog.installed(item):
            raise HTTPException(409, f'{name} is not installed')
        state = runtime.ensure(name, retry=True)
        rewarmed = state == 'loaded' and runtime.rewarm(name)
        return {'name': name, 'state': state, 'rewarming': rewarmed}

    @app.post('/v1/models/{name}/download')
    async def download(name: str, authorization: str | None = Header(default=None)):
        check_auth(authorization)
        known(name)
        try:
            return {'name': name, 'state': runtime.download(name)}
        except catalog.ModelError as exc:
            raise HTTPException(409, str(exc)) from None

    @app.post('/v1/systemone')
    async def systemone(request: Request, authorization: str | None = Header(default=None)):
        check_auth(authorization)
        body = await request.json()
        if not isinstance(body, dict) or not isinstance(body.get('questions'), dict) or not body['questions']:
            raise HTTPException(400, 'questions must be a non-empty object')
        name = body.get('model') or catalog.default_model()
        item = known(name)
        if not catalog.installed(item):
            raise HTTPException(404, f'{name} is not installed; download it from the match setup screen')
        state = runtime.ensure(name)
        if state == 'error':
            raise HTTPException(422, runtime.errors.get(name) or f'{name} could not be loaded')
        if state == 'loading':
            raise HTTPException(503, f'{name} is loading', headers={'Retry-After': '1', 'X-Laya-Status': 'loading'})
        with runtime.lock:
            agent = runtime.agents.get(name)
        if agent is None:
            raise HTTPException(503, f'{name} is loading', headers={'Retry-After': '1', 'X-Laya-Status': 'loading'})
        try:
            await asyncio.wait_for(gate.acquire(), QUEUE_WAIT_S)
        except asyncio.TimeoutError:
            # Reject overload instead of building a queue of obsolete world states.
            raise HTTPException(429, 'inference busy; retry with fresh state', headers={'Retry-After': '1'}) from None
        runtime.busy = True
        future = asyncio.get_running_loop().run_in_executor(runtime.worker, runtime.run, agent, name, body.get('state'), body['questions'])
        future.add_done_callback(runtime.released)
        future.add_done_callback(lambda _: gate.release())
        try:
            result = await asyncio.shield(future)
        except Exception as exc:
            log.error('Local inference failed (%s)', type(exc).__name__)
            raise HTTPException(422, 'local inference failed; check service diagnostics') from None
        return JSONResponse(result, headers={'Server-Timing': f'inference;dur={runtime.last_ms:.1f}',
                                             'X-Laya-Device': str(agent.device),
                                             'X-Laya-Inference-Ms': f'{runtime.last_ms:.1f}'})
    return app


def main():
    # Prefer the Apple GPU when present. CPU remains available as an explicit override.
    device = os.environ.get('LAYA_DEVICE') or ('mps' if torch.backends.mps.is_available() else 'cpu')
    torch.set_num_threads(int(os.environ['LAYA_THREADS']))
    torch.set_num_interop_threads(1)
    runtime = InferenceRuntime(device, int(os.environ.get('LAYA_MAX_LOADED', '2')))
    default = catalog.default_model()
    item = catalog.entry(default)
    if not catalog.installed(item):
        try:
            # Earlier setups cached weights in the Hugging Face cache; adopt them offline.
            catalog.download(default, allow_network=False)
        except catalog.ModelError as exc:
            print(f'Default model not installed: {exc}', flush=True)
    if catalog.installed(item):
        runtime.loading.add(default)
        runtime.load(default)
        if default not in runtime.agents:
            raise SystemExit(runtime.errors.get(default, 'default model failed to load'))
        print(f'Laya ready: {default} ({catalog.source_string(item)}); {runtime.actual_device}; '
              f'warm-up {runtime.last_ms:.0f} ms; runtime {RUNTIME_VERSION}; no fallback policy.', flush=True)
    uvicorn.run(create_game_app(runtime), host='127.0.0.1', port=8000, log_level='info')


if __name__ == '__main__':
    main()
