"""Hold a real Run at the hand-off boundary; no external provider or production store."""
import tempfile
import threading
import unittest
from pathlib import Path
from unittest.mock import patch

from hub import Hub, Run
from runtime import begin_shutdown


class ShutdownTests(unittest.TestCase):
    def test_shutdown_waits_for_default_project_followup(self):
        self.check_followup(False)

    def test_shutdown_waits_for_secondary_project_followup(self):
        self.check_followup(True)

    def check_followup(self, secondary):
        with tempfile.TemporaryDirectory() as directory:
            root = Hub(Path(directory) / 'runtime', root=directory)
            worker = root
            if secondary:
                folder = Path(directory) / 'project'; folder.mkdir()
                worker = root.project(root.add_project({'path': str(folder)})['id'])
            started = threading.Event(); release_run = threading.Event()
            followup = threading.Event(); release_followup = threading.Event()
            def provider(run):
                started.set()
                if not release_run.wait(5):
                    raise RuntimeError('test provider gate timed out')
            def after_run(*args):
                followup.set()
                if not release_followup.wait(5):
                    raise RuntimeError('test followup gate timed out')
            try:
                with patch.object(Run, 'codex', provider), patch.object(worker, 'context', return_value={}), patch.object(worker, 'after_run', after_run):
                    agent = worker.store.create(name='shutdown fixture', provider='codex')
                    worker.send(agent['id'], {'text': 'local fixture only'})
                    self.assertTrue(started.wait(2))
                    self.assertFalse(begin_shutdown(root), 'active provider must block shutdown')
                    release_run.set()
                    self.assertTrue(followup.wait(2))
                    self.assertFalse(worker.runs)
                    self.assertEqual(worker.post_runs, 1)
                    self.assertFalse(begin_shutdown(root), 'hand-off is still processing after the run leaves runs')
                    self.assertFalse(getattr(root, 'shutting_down', False))
                    self.assertFalse(getattr(worker, 'shutting_down', False))
                    release_followup.set()
                    self.assertTrue(worker.settle(3))
                    self.assertTrue(begin_shutdown(root))
                    with self.assertRaisesRegex(ValueError, '종료 중'):
                        worker.send(agent['id'], {'text': 'must not start after shutdown'})
            finally:
                release_run.set(); release_followup.set()
                worker.settle(3)
                root.close()
                for child in root.project_hubs.values():
                    child.store.db.close()
                root.store.db.close()


if __name__ == '__main__':
    unittest.main()
