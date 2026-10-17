import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

from isolated_process_scope import WindowsProcessScope


@unittest.skipUnless(os.name == 'nt', 'Windows process ownership')
class ProcessScopeTests(unittest.TestCase):
    def check_descendant(self, timeout):
        with tempfile.TemporaryDirectory() as directory:
            marker = Path(directory) / 'started'
            # Like runtime.ensure_running: detached child, no descriptor, parent may exit or hang.
            script = """
import subprocess, sys, time
from pathlib import Path
child = subprocess.Popen([sys.executable, '-c', 'import time; time.sleep(60)'],
    stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
    creationflags=subprocess.CREATE_NO_WINDOW | subprocess.CREATE_NEW_PROCESS_GROUP)
Path(sys.argv[1]).write_text(str(child.pid))
if sys.argv[2] == 'hang': time.sleep(60)
"""
            scope = WindowsProcessScope()
            try:
                with scope:
                    args = [sys.executable, '-c', script, str(marker), 'hang' if timeout else 'exit']
                    if timeout:
                        with self.assertRaises(subprocess.TimeoutExpired):
                            scope.run(args, env=os.environ.copy(), cwd=directory, timeout=2)
                    else:
                        self.assertEqual(scope.run(args, env=os.environ.copy(), cwd=directory, timeout=5).returncode, 0)
                    self.assertTrue(marker.exists())
                    self.assertGreater(scope.active_count(), 0)  # Parent-only cleanup leaves a descendant.
                    raise AssertionError('injected assertion failure')
            except AssertionError as error:
                self.assertEqual(str(error), 'injected assertion failure')
            self.assertEqual(scope.remaining, 0)  # OS job accounting, not PID lookup.

    def test_exited_parent_with_detached_child(self):
        self.check_descendant(timeout=False)

    def test_timeout_before_descriptor_exists(self):
        self.check_descendant(timeout=True)

    def test_cleanup_failure_preserves_original_failure(self):
        scope = WindowsProcessScope()
        original = AssertionError('original assertion')
        cleanup = RuntimeError('injected cleanup failure')
        close = scope._close
        def fail_after_close():
            close()
            raise cleanup
        with self.assertRaises(BaseExceptionGroup) as result:
            with patch.object(scope, '_close', side_effect=fail_after_close):
                with scope:
                    raise original
        self.assertEqual(result.exception.exceptions, (original, cleanup))
        self.assertIsNone(scope.handle)


if __name__ == '__main__':
    unittest.main()
