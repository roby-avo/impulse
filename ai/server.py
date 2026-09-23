"""Official Laya HTTP interface, pinned to the specified local English checkpoint.
No game policy exists here: only model configuration and the official server.
"""
import os
os.environ.setdefault('USE_TF', '0')
os.environ.setdefault('HF_HUB_OFFLINE', '1')
os.environ.setdefault('LAYA_DEVICE', 'cpu')
os.environ.setdefault('LAYA_THREADS', '4')
os.environ['LAYA_MODELS'] = 'english'
os.environ['LAYA_PRELOAD'] = '1'
os.environ['LAYA_AUTO_TASK'] = '0'
from laya.serve import build_router, create_app
import uvicorn

if __name__ == '__main__':
    router = build_router()
    agent = router.load("english")
    agent.cfg["max_len"] = 1024
    agent.cfg["head_max_len"] = 384
    # Keep all ten choice descriptions plus the semantic state intact.
    # Inspect the loaded SDK's public agent configuration via an explicitly loaded agent.
    print('Laya ready: convaiinnovations/laya; local-only; no fallback policy.', flush=True)
    uvicorn.run(create_app(router), host='127.0.0.1', port=8000, log_level='info')
