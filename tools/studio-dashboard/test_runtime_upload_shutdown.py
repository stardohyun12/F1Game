"""Real isolated runtime and HTTP upload; no external provider or production data."""
import base64
from concurrent.futures import ThreadPoolExecutor
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
import urllib.error
import urllib.request

HERE = Path(__file__).resolve().parent
CHILD = r'''
import os, time
from pathlib import Path
os.environ['PITCREW_RUNTIME_SERVICE'] = '1'
from hub import Hub
import runtime
root = Path(os.environ['PITCREW_RUNTIME_DIR'])
original = Hub.upload
def delayed(self, data):
    (root / 'upload-entered').touch()
    deadline = time.monotonic() + 15
    while not (root / 'upload-release').exists():
        if time.monotonic() > deadline: raise RuntimeError('test upload gate timed out')
        time.sleep(.02)
    return original(self, data)
Hub.upload = delayed
runtime.serve()
'''


class UploadShutdownTests(unittest.TestCase):
    def test_upload_finishes_before_shutdown_is_accepted(self):
        self.check_upload(False)

    def test_secondary_project_upload_blocks_shutdown(self):
        self.check_upload(True)

    def check_upload(self, secondary):
        with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as directory:
            root = Path(directory)
            with (root / 'process.log').open('wb') as log:
                child = subprocess.Popen([sys.executable, '-X', 'utf8', '-c', CHILD], cwd=HERE,
                    env={**os.environ, 'PITCREW_RUNTIME_DIR': directory}, stdout=log, stderr=log,
                    creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
                pool = ThreadPoolExecutor(max_workers=1)
                def wait_for(check):
                    deadline = time.monotonic() + 8
                    while time.monotonic() < deadline:
                        if check(): return
                        self.assertIsNone(child.poll(), 'isolated runtime exited early')
                        time.sleep(.02)
                    self.fail('isolated runtime gate timed out')
                try:
                    wait_for(lambda: (root / 'runtime.json').exists())
                    info = json.loads((root / 'runtime.json').read_text())
                    def request(path, data=None, token=''):
                        req = urllib.request.Request(f"http://127.0.0.1:{info['port']}" + path,
                            data=None if data is None else json.dumps(data).encode(),
                            headers={'X-PitCrew-Runtime': info['secret'], 'X-PitCrew-Token': token})
                        try:
                            with urllib.request.urlopen(req, timeout=10) as r:
                                return r.status, json.load(r)
                        except urllib.error.HTTPError as error:
                            with error: return error.code, json.load(error)
                    token = request('/api/hub/bootstrap')[1]['token']
                    route = '/api/hub/upload'
                    if secondary:
                        folder = root / 'second-project'; folder.mkdir()
                        code, project = request('/api/hub/projects', {'path': str(folder)}, token)
                        self.assertEqual(code, 200)
                        route += '?project=' + project['id']
                    content = b'isolated attachment bytes\x00\xff'
                    pending = pool.submit(request, route,
                        {'name': 'sample.bin', 'data': base64.b64encode(content).decode()}, token)
                    wait_for(lambda: (root / 'upload-entered').exists())
                    code, result = request('/internal/shutdown', {})
                    if code == 200:
                        child.wait(timeout=5)
                        try:
                            pending.result(timeout=2)
                        except Exception as error:
                            print(f'Premature shutdown: upload response={type(error).__name__}, '
                                  f'saved files={len(list((root / "uploads").glob("*")))}')
                    self.assertEqual(code, 409, f'upload is still saving: {result}')
                    self.assertIsNone(child.poll())
                    (root / 'upload-release').touch()
                    code, uploaded = pending.result(timeout=5)
                    self.assertEqual(code, 200)
                    saved = Path(uploaded['path']).resolve()
                    self.assertTrue(saved.is_relative_to(root.resolve()))
                    self.assertEqual(saved.read_bytes(), content)
                    # Rejected requests also release the shutdown guard.
                    self.assertEqual(request('/api/hub/upload', {'data': 'invalid'}, token)[0], 400)
                    self.assertEqual(request('/api/hub/shared', {'text': 'forged'}, 'wrong')[0], 403)
                    self.assertEqual(request('/internal/shutdown', {})[0], 200)
                    child.wait(timeout=5)
                    self.assertEqual(child.returncode, 0)
                finally:
                    (root / 'upload-release').touch()
                    pool.shutdown(wait=True)
                    if child.poll() is None:
                        child.terminate(); child.wait(timeout=5)


if __name__ == '__main__':
    unittest.main()
