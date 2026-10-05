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

    @patch.object(service, 'health', return_value={'status': 'ok', 'runtime_version': '0.0.1'})
    @patch.object(service, 'managed_pid', return_value=None)
    @patch.object(service.subprocess, 'Popen')
    def test_outdated_foreign_service_is_reported_not_killed(self, spawn, pid, health):
        with patch.object(service.os, 'kill') as kill:
            result = service.ensure()
        self.assertEqual(result['state'], 'error')
        self.assertIn('older local Laya', result['message'])
        kill.assert_not_called()
        spawn.assert_not_called()

    @patch.object(service.socket, 'socket')
    @patch.object(service.subprocess, 'Popen')
    def test_outdated_managed_service_is_replaced(self, spawn, socket):
        socket.return_value.__enter__.return_value.connect_ex.return_value = 1
        spawn.return_value = Mock(pid=456)
        states = iter([{'status': 'ok', 'runtime_version': '0.0.1'}] * 3 + [None] * 10)
        with patch.object(service, 'health', side_effect=lambda: next(states)), \
                patch.object(service, 'managed_pid', side_effect=[123, None]), patch.object(service.os, 'kill') as kill:
            result = service.ensure()
        kill.assert_called_once_with(123, service.signal.SIGTERM)
        self.assertEqual(result['state'], 'starting')
        spawn.assert_called_once()


if __name__ == '__main__':
    unittest.main()
