"""Exercise the real script entry point; never touch the operational runtime."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time
import unittest
import urllib.request


class RuntimeEntryTests(unittest.TestCase):
    def test_conflicting_runtime_module_refuses_before_starting_service(self):
        with tempfile.TemporaryDirectory(prefix='pitcrew-entry-conflict-') as tmp:
            rt = Path(tmp) / 'not-created'
            script = """
import runpy, sys, types
sys.modules['runtime'] = types.ModuleType('conflicting_runtime')
sys.argv = ['runtime.py', '--serve']
try:
    runpy.run_path('runtime.py', run_name='__main__')
except RuntimeError as error:
    print(type(error).__name__)
else:
    raise AssertionError('conflicting module was accepted')
"""
            result = subprocess.run([sys.executable, '-X', 'utf8', '-c', script],
                cwd=Path(__file__).parent, env={**os.environ, 'PITCREW_RUNTIME_DIR': str(rt)},
                capture_output=True, text=True, timeout=10,
                creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout.strip(), 'RuntimeError')
            self.assertFalse(rt.exists())

    def test_disk_edit_after_script_start_cannot_split_shutdown_predicate(self):
        with tempfile.TemporaryDirectory(prefix='pitcrew-entry-') as tmp:
            root = Path(tmp)
            code = root / 'code'; code.mkdir()
            for source in Path(__file__).parent.glob('*.py'):
                shutil.copy2(source, code / source.name)
            rt = root / 'runtime'
            env = {**os.environ, 'PITCREW_RUNTIME_DIR': str(rt),
                   'CODEX_HOME': str(root / 'codex'), 'CLAUDE_CONFIG_DIR': str(root / 'claude')}
            env.pop('PITCREW_RUNTIME_SERVICE', None)
            with (root / 'child.log').open('wb') as log:
                child = subprocess.Popen([sys.executable, '-X', 'utf8', str(code / 'runtime.py'), '--serve'],
                    cwd=root, env=env, stdout=log, stderr=log,
                    creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
                try:
                    deadline = time.monotonic() + 15
                    while not (rt / 'runtime.json').exists() and time.monotonic() < deadline and child.poll() is None:
                        time.sleep(.05)
                    self.assertTrue((rt / 'runtime.json').exists(), (root / 'child.log').read_text(encoding='utf-8'))
                    info = json.loads((rt / 'runtime.json').read_text(encoding='utf-8'))
                    def call(path, data=None):
                        request = urllib.request.Request(f"http://127.0.0.1:{info['port']}" + path,
                            data=data, headers={'X-PitCrew-Runtime': info['secret']})
                        with urllib.request.urlopen(request, timeout=10) as response:
                            return response.status, json.loads(response.read())
                    before = call('/api/hub/runtime-identity')[1]
                    path = code / 'runtime.py'
                    source = path.read_text(encoding='utf-8')
                    old = "return dict(ready=not blockers and not any(p['blockers'] for p in projects),"
                    self.assertIn(old, source)
                    path.write_text(source.replace(old, "return dict(diskCopy=True, ready=False,"), encoding='utf-8')
                    # First maintenance request happens AFTER the disk edit, as during deployment.
                    for _ in range(2):
                        status, snapshot = call('/api/hub/maintenance-status')
                        self.assertEqual(status, 200)
                        self.assertTrue(snapshot['ready'], snapshot)
                        self.assertNotIn('diskCopy', snapshot)
                    after = call('/api/hub/runtime-identity')[1]
                    module = after['modules']['runtime.py']
                    self.assertTrue(module['sourceChangedSinceCapture'])
                    self.assertEqual(module['loadedCodeSha256'], before['modules']['runtime.py']['loadedCodeSha256'])
                    self.assertEqual(call('/internal/shutdown', b'{}')[0], 200)
                    self.assertEqual(child.wait(timeout=10), 0)
                finally:
                    if child.poll() is None:
                        child.terminate(); child.wait(timeout=10)


if __name__ == '__main__': unittest.main()
