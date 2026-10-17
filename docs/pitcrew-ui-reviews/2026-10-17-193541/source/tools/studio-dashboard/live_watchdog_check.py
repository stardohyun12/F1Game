"""Opt-in: tiny real Codex/Claude turns in disposable sessions, across runtime death."""
import json
import os
from pathlib import Path
import secrets
import signal
import socket
import subprocess
import sys
import tempfile
import time
import urllib.request

HERE = Path(__file__).resolve().parent

def main():
    with tempfile.TemporaryDirectory(prefix='pitcrew-crash-check-') as folder:
        directory = Path(folder)
        with socket.socket() as sock:
            sock.bind(('127.0.0.1', 0)); port = sock.getsockname()[1]
        env = {**os.environ, 'PITCREW_RUNTIME_DIR': folder}
        log = (directory/'check.log').open('wb')
        watcher = subprocess.Popen([sys.executable, str(HERE/'watchdog.py'), '--port', str(port)], env=env, stdout=log, stderr=log)
        token = ''
        def api(path, body=None):
            request = urllib.request.Request(f'http://127.0.0.1:{port}/api/hub/'+path,
                data=None if body is None else json.dumps(body).encode(),
                headers={'Content-Type':'application/json', 'X-PitCrew-Token':token})
            with urllib.request.urlopen(request, timeout=10) as response: return json.load(response)
        def wait(fn, seconds=150):
            deadline = time.monotonic()+seconds
            while time.monotonic()<deadline:
                try:
                    value = fn()
                    if value: return value
                except OSError: pass
                time.sleep(.25)
            raise RuntimeError('verification timed out')
        def done(aid):
            data = api('detail?agent='+aid)
            if data['agent']['status'] in ('error','interrupted','waiting'):
                raise RuntimeError(str(data['agent'].get('error') or data['agent']['status']))
            return data if data['agent']['status']=='done' else None
        try:
            before = wait(lambda:api('bootstrap'), 30); token = before['token']
            fixtures = []
            for provider in ('codex','claude'):
                word = 'CRASH_'+secrets.token_hex(6).upper()
                agent = api('agents', {'provider':provider, 'name':'Disposable recovery check', 'mode':'read'})
                aid = agent['id']
                api('send', {'agent':aid, 'text':'Do not use tools. Remember this exact test word: '+word+'. Reply READY only.'})
                result = wait(lambda:done(aid))
                fixtures.append((provider, aid, word, result['agent']['session']))
                print(provider+' first turn saved', flush=True)
            os.kill(before['runtime']['pid'], signal.SIGTERM)
            after = wait(lambda:(d if (d:=api('bootstrap'))['runtime']['pid']!=before['runtime']['pid'] else None), 30)
            token = after['token']
            reports = []
            for provider, aid, word, sid in fixtures:
                api('send', {'agent':aid,'text':'Do not use tools. What exact test word did I ask you to remember? Reply with that word only.'})
                result = wait(lambda:done(aid))
                answer = next(m['text'] for m in reversed(result['messages']) if m['role']=='assistant')
                assert word in answer, (provider, answer)
                assert result['agent']['session']==sid
                assert len([m for m in result['messages'] if m['role']=='user'])==2
                reports.append(dict(provider=provider, sameSession=True, remembered=True, messagesPreserved=True))
                print(json.dumps(reports[-1]), flush=True)
            (HERE/'.runtime'/'watchdog-live-verified.json').write_text(json.dumps({'checkedAt':time.time(),'results':reports}, indent=2), encoding='utf-8')
        finally:
            (directory/f'watchdog-{port}.stop').touch(); watcher.wait(20)
            for path in (directory/f'web-{port}.json', directory/'runtime.json'):
                if path.exists():
                    try: os.kill(json.loads(path.read_text())['pid'], signal.SIGTERM)
                    except OSError: pass
            time.sleep(.5); log.close()

if __name__=='__main__': main()
