"""Model catalog, installation and service endpoints with fake checkpoints (no weights loaded)."""
import json
from pathlib import Path
import tempfile
import time
import unittest
from unittest.mock import patch
import torch
from fastapi.testclient import TestClient
import models as catalog
import server

QUESTIONS = json.loads((catalog.ROOT / 'Assets/StreamingAssets/laya-question.json').read_text())


def write_checkpoint(directory, extra_sibling=False):
    directory.mkdir(parents=True, exist_ok=True)
    (directory / 'rl_agent_config.json').write_text('{}')
    (directory / 'model.safetensors').write_bytes(b'weights')
    (directory / 'tokenizer').mkdir(exist_ok=True)
    (directory / 'tokenizer/tokenizer.json').write_text('{}')
    if extra_sibling:
        write_checkpoint(directory / 'sibling')


class FakeAgent:
    def __init__(self, choice='PUSH_OPPONENT'):
        self.device = torch.device('cpu')
        self.choice = choice
        self.calls = 0

    def system_one(self, state, questions):
        self.calls += 1
        return {'model': 'laya-rl-agent', 'answers': {'action': {'type': 'choice', 'choice': self.choice, 'confidence': .4}}}


class Sandbox(unittest.TestCase):
    """A temporary catalog and model store; the real ai/models folder is never touched."""
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        (self.root / 'models.json').write_text(json.dumps({'default': 'alpha', 'models': [
            {'id': 'alpha', 'label': 'Alpha', 'repo': 'org/alpha', 'revision': 'abc', 'size_mb': 1},
            {'id': 'beta', 'label': 'Beta', 'repo': 'org/bundle', 'subfolder': 'beta', 'size_mb': 1}]}))
        for name, value in [('CATALOG', self.root / 'models.json'), ('LOCAL_CATALOG', self.root / 'models.local.json'),
                            ('STORE', self.root / 'store'), ('PARTIAL', self.root / 'store/.partial'),
                            ('VERIFIED', self.root / 'store/.verified')]:
            patcher = patch.object(catalog, name, value)
            patcher.start()
            self.addCleanup(patcher.stop)


class CatalogTests(Sandbox):
    def test_local_entries_extend_and_override_shipped_catalog(self):
        (self.root / 'models.local.json').write_text(json.dumps({'models': [
            {'id': 'beta', 'label': 'My beta', 'repo': 'me/beta'}, {'id': 'gamma', 'path': str(self.root / 'g')}]}))
        models = catalog.catalog()
        self.assertEqual(list(models), ['alpha', 'beta', 'gamma'])
        self.assertEqual(models['beta']['label'], 'My beta')
        self.assertEqual(catalog.default_model(), 'alpha')

    def test_invalid_entries_are_reported(self):
        for bad in [{'id': 'Bad Id', 'repo': 'x/y'}, {'id': 'ok', 'repo': 'x/y', 'path': '/tmp'},
                    {'id': 'ok', 'repo': 'x/y', 'runtime': 'gguf'}]:
            (self.root / 'models.local.json').write_text(json.dumps({'models': [bad]}))
            with self.assertRaises(catalog.ModelError):
                catalog.catalog()

    def test_cached_bundle_installs_only_its_checkpoint_and_links_weights(self):
        cache = self.root / 'hf'
        write_checkpoint(cache, extra_sibling=True)
        with patch('huggingface_hub.snapshot_download', return_value=str(cache)) as snapshot:
            result = catalog.download('alpha')
        self.assertTrue(result['reused_cache'])
        self.assertTrue(snapshot.call_args.kwargs['local_files_only'])
        installed = self.root / 'store/alpha'
        self.assertTrue(catalog.installed(catalog.entry('alpha')))
        self.assertFalse((installed / 'sibling').exists())
        self.assertEqual((installed / 'model.safetensors').stat().st_ino, (cache / 'model.safetensors').stat().st_ino)
        self.assertFalse((self.root / 'store/.partial/alpha').exists())

    def test_uncached_model_downloads_only_requested_subfolder(self):
        from huggingface_hub.errors import LocalEntryNotFoundError

        def fake(local_files_only=False, local_dir=None, allow_patterns=None, **_):
            if local_files_only:
                raise LocalEntryNotFoundError('not cached')
            self.assertTrue(all(p.startswith('beta/') for p in allow_patterns))
            write_checkpoint(Path(local_dir) / 'beta')
            return local_dir
        with patch('huggingface_hub.snapshot_download', side_effect=fake), patch.dict('os.environ', {'HF_HUB_OFFLINE': '0'}):
            result = catalog.download('beta')
        self.assertFalse(result['reused_cache'])
        self.assertTrue(catalog.installed(catalog.entry('beta')))

    def test_offline_download_of_uncached_model_fails_cleanly(self):
        from huggingface_hub.errors import LocalEntryNotFoundError
        with patch('huggingface_hub.snapshot_download', side_effect=LocalEntryNotFoundError('x')):
            with self.assertRaises(catalog.ModelError):
                catalog.download('alpha', allow_network=False)
        self.assertFalse((self.root / 'store/.partial/alpha').exists())
        self.assertFalse(catalog.installed(catalog.entry('alpha')))

    def test_add_local_folder_and_refuse_to_delete_it(self):
        folder = self.root / 'my-run'
        write_checkpoint(folder)
        self.assertTrue(catalog.add('my-run', str(folder))['installed'])
        self.assertTrue(catalog.status(catalog.entry('my-run'))['local_path'])
        with self.assertRaises(catalog.ModelError):
            catalog.remove('my-run')
        self.assertTrue(folder.exists())
        with self.assertRaises(catalog.ModelError):
            catalog.add('empty', str(self.root / 'missing-folder'))

    def test_remove_deletes_install_and_verification(self):
        write_checkpoint(self.root / 'store/alpha')
        catalog.record_verification(catalog.entry('alpha'), True, {'choice': 'JUMP'}, 1, 'cpu')
        self.assertTrue(catalog.status(catalog.entry('alpha'))['verified'])
        catalog.remove('alpha')
        self.assertFalse(catalog.installed(catalog.entry('alpha')))
        self.assertFalse(catalog.status(catalog.entry('alpha'))['verified'])

    def test_answer_check_requires_legal_choice_and_confidence(self):
        good = {'answers': {'action': {'choice': 'JUMP', 'confidence': .3}}}
        self.assertTrue(catalog.check_answer(good, QUESTIONS)[0])
        self.assertFalse(catalog.check_answer({'answers': {'action': {'choice': 'FLY', 'confidence': .3}}}, QUESTIONS)[0])
        self.assertFalse(catalog.check_answer({'answers': {'action': {'choice': 'JUMP', 'confidence': 2}}}, QUESTIONS)[0])


class ServiceTests(Sandbox):
    def setUp(self):
        super().setUp()
        write_checkpoint(self.root / 'store/alpha')
        write_checkpoint(self.root / 'store/beta')
        self.agents = {}

        self.bad_beta = False

        def loader(item, device):
            agent = self.agents[item['id']] = FakeAgent('ILLEGAL' if item['id'] == 'beta' and self.bad_beta else 'PUSH_OPPONENT')
            return agent
        self.runtime = server.InferenceRuntime('cpu', max_loaded=1, loader=loader)
        self.client = TestClient(server.create_game_app(self.runtime))
        self.request = {'model': 'alpha', 'state': {'x': 1}, 'questions': QUESTIONS}

    def wait_loaded(self, name):
        deadline = time.time() + 5
        while self.runtime.ensure(name) == 'loading' and time.time() < deadline:
            time.sleep(.02)
        return self.runtime.ensure(name)

    def test_unloaded_model_reports_loading_then_answers_with_its_own_name(self):
        first = self.client.post('/v1/systemone', json=self.request)
        self.assertEqual(first.status_code, 503)
        self.assertEqual(first.headers['X-Laya-Status'], 'loading')
        self.assertEqual(self.wait_loaded('alpha'), 'loaded')
        reply = self.client.post('/v1/systemone', json=self.request)
        self.assertEqual(reply.status_code, 200)
        self.assertEqual(reply.json()['model'], 'alpha')
        self.assertEqual(reply.headers['X-Laya-Device'], 'cpu')

    def test_unknown_and_uninstalled_models_are_rejected_without_substitution(self):
        self.assertEqual(self.client.post('/v1/systemone', json=dict(self.request, model='nope')).status_code, 404)
        catalog.remove('beta')
        self.assertEqual(self.client.post('/v1/systemone', json=dict(self.request, model='beta')).status_code, 404)
        self.assertNotIn('alpha', self.runtime.agents)

    def test_model_with_illegal_warmup_answer_is_not_served(self):
        self.bad_beta = True
        self.client.post('/v1/models/beta/load')
        self.assertEqual(self.wait_loaded('beta'), 'error')
        reply = self.client.post('/v1/systemone', json=dict(self.request, model='beta'))
        self.assertEqual(reply.status_code, 422)
        self.assertFalse(catalog.status(catalog.entry('beta'))['verified'])

    def test_resident_model_limit_evicts_least_recently_used(self):
        self.wait_loaded('alpha')
        self.wait_loaded('beta')
        self.assertEqual(list(self.runtime.agents), ['beta'])

    def test_model_listing_reports_install_and_load_state(self):
        self.wait_loaded('alpha')
        listing = self.client.get('/v1/models').json()
        state = {m['name']: m for m in listing['models']}
        self.assertEqual(listing['default'], 'alpha')
        self.assertTrue(state['alpha']['loaded'] and state['alpha']['verified'])
        self.assertTrue(state['beta']['installed'] and not state['beta']['loaded'])

    def test_download_of_installed_model_is_a_no_op(self):
        with patch.object(server.subprocess, 'Popen') as spawn:
            self.assertEqual(self.client.post('/v1/models/alpha/download').json()['state'], 'installed')
        spawn.assert_not_called()

    def test_health_reports_runtime_version_and_loaded_models(self):
        self.wait_loaded('alpha')
        health = self.client.get('/health').json()
        self.assertEqual(health['runtime_version'], catalog.RUNTIME_VERSION)
        self.assertEqual(health['loaded'], ['alpha'])


if __name__ == '__main__':
    unittest.main()
