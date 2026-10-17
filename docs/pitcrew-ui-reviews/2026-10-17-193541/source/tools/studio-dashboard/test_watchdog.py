"""Kill only isolated fixture processes; assert automatic recovery and persistence."""
import json
import os
from pathlib import Path
import signal
import socket
import subprocess
import sys
import tempfile
import time
import unittest
import urllib.request
from hub import Store
from isolated_runtime import stop_isolated_runtime

HERE = Path(__file__).resolve().parent

class WatchdogTests(unittest.TestCase):
    def test_dead_web_and_runtime_recover_without_replaying(self):
        with tempfile.TemporaryDirectory() as folder:
            directory = Path(folder)
            store = Store(directory)
            agent = store.create(name='fixture', provider='codex')
            sid = '12345678-1234-4234-8234-123456789abc'
            store.update(agent['id'], session=sid, status='running')
            store.db.close()
            with socket.socket() as sock:
                sock.bind(('127.0.0.1', 0)); port = sock.getsockname()[1]
            env = {**os.environ, 'PITCREW_RUNTIME_DIR': folder}
            log = (directory / 'test.log').open('wb')
            watcher = subprocess.Popen([sys.executable, str(HERE/'watchdog.py'), '--port', str(port)], env=env, stdout=log, stderr=log)
            def api():
                with urllib.request.urlopen(f'http://127.0.0.1:{port}/api/hub/bootstrap', timeout=2) as response:
                    return json.load(response)
            def wait_for(fn):
                deadline = time.monotonic()+30
                while time.monotonic()<deadline:
                    try:
                        value = fn()
                        if value: return value
                    except (OSError, ValueError): pass
                    time.sleep(.1)
                self.fail('automatic recovery timed out')
            try:
                first = wait_for(api)
                pid = first['runtime']['pid']
                webpid = json.loads((directory/f'web-{port}.json').read_text())['pid']
                os.kill(webpid, signal.SIGTERM)
                wait_for(lambda: json.loads((directory/f'web-{port}.json').read_text())['pid'] != webpid)
                second = wait_for(api)
                self.assertEqual(second['runtime']['pid'], pid)
                self.assertEqual(second['token'], first['token'])
                os.kill(pid, signal.SIGTERM)
                third = wait_for(lambda: (data if (data:=api())['runtime']['pid'] != pid else None))
                restored = next(a for a in third['agents'] if a['id']==agent['id'])
                self.assertEqual(restored['session'], sid)
                self.assertEqual(restored['status'], 'interrupted')
                duplicate = subprocess.run([sys.executable, str(HERE/'watchdog.py'), '--port', str(port)], env=env, timeout=5)
                self.assertEqual(duplicate.returncode, 0)
                print('PASS automatic web recovery, same runtime/token; runtime recovery, same native session, interrupted turn not replayed; singleton lock')
            finally:
                (directory/f'watchdog-{port}.stop').touch()
                watcher.wait(20)
                for path in (directory/f'web-{port}.json',):
                    if path.exists():
                        try: os.kill(json.loads(path.read_text())['pid'], signal.SIGTERM)
                        except OSError: pass
                try:
                    stop_isolated_runtime(directory)
                finally:
                    time.sleep(.5)
                    log.close()

if __name__ == '__main__': unittest.main()
