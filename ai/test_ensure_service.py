"""Startup failure cases without starting a model or touching the real runtime."""
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import Mock, patch
import ensure_service as service


class StartupTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        (root / 'ai/.runtime').mkdir(parents=True)
        (root / 'ai/.venv/bin').mkdir(parents=True)
        (root / 'ai/.venv/bin/python').touch()
        self.root = root
        self.runtime = root / 'ai/.runtime'
        for name, value in [('ROOT', root), ('RUNTIME', self.runtime)]:
            p = patch.object(service, name, value); p.start(); self.addCleanup(p.stop)

    @patch.object(service, 'healthy', return_value=True)
    @patch.object(service.subprocess, 'Popen')
    def test_existing_healthy_service_never_spawns(self, spawn, healthy):
        self.assertEqual(service.ensure()['state'], 'ready')
        spawn.assert_not_called()

    @patch.object(service, 'healthy', return_value=False)
    def test_missing_installation_is_actionable(self, healthy):
        (self.root / 'ai/.venv/bin/python').unlink()
        result = service.ensure()
        self.assertEqual(result['state'], 'error')
        self.assertIn('ai/setup.sh', result['message'])

    @patch.object(service.subprocess, 'run')
    def test_reused_pid_of_unrelated_process_is_not_managed(self, run):
        (self.runtime / 'service.pid').write_text('123')
        run.return_value = Mock(returncode=0, stdout='/usr/bin/unrelated-server')
        self.assertIsNone(service.managed_pid())

    @patch.object(service, 'healthy', return_value=False)
    @patch.object(service.socket, 'socket')
    @patch.object(service.subprocess, 'Popen')
    def test_foreign_listener_is_reported_without_spawning(self, spawn, socket, healthy):
        socket.return_value.__enter__.return_value.connect_ex.return_value = 0
        result = service.ensure()
        self.assertEqual(result['state'], 'error')
        self.assertIn('Port 8000', result['message'])
        spawn.assert_not_called()

    @patch.object(service, 'healthy', return_value=False)
    @patch.object(service.socket, 'socket')
    @patch.object(service.subprocess, 'Popen')
    def test_failed_start_cooldown_prevents_spawn_loop(self, spawn, socket, healthy):
        socket.return_value.__enter__.return_value.connect_ex.return_value = 1
        (self.runtime / 'last-start').touch()
        self.assertEqual(service.ensure()['state'], 'starting')
        spawn.assert_not_called()


if __name__ == '__main__':
    unittest.main()
