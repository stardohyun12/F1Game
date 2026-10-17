"""Real-process integration: restart only the web UI while a local mock AI streams."""
import json
import os
import signal
from pathlib import Path
import socket
import subprocess
import sys
import tempfile
import threading
import time
import unittest
import urllib.error
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from isolated_runtime import stop_isolated_runtime

HERE=Path(__file__).resolve().parent

class RuntimeTests(unittest.TestCase):
    def test_watchdog_recovers_web_during_stream(self):
        self.run_case(False, supervised=True)

    def test_web_restart_preserves_active_turn_and_runtime_auth(self):
        self.run_case(False)

    def test_secondary_project_keeps_running_across_switch_and_restart(self):
        self.run_case(True)

    def run_case(self,secondary,supervised=False):
        with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as directory:  # Windows frees the child log a moment after exit
            release=threading.Event();started=threading.Event();calls=[]
            class Provider(BaseHTTPRequestHandler):
                def log_message(self,*args):pass
                def do_POST(self):
                    calls.append(self.rfile.read(int(self.headers['Content-Length'])))
                    self.send_response(200);self.send_header('Content-Type','text/event-stream');self.end_headers()
                    self.wfile.write(b'data: {"choices":[{"delta":{"content":"BEFORE "}}]}\n\n');self.wfile.flush();started.set()
                    release.wait(20)
                    self.wfile.write(b'data: {"choices":[{"delta":{"content":"AFTER"}}]}\n\ndata: [DONE]\n\n');self.wfile.flush()
            provider=ThreadingHTTPServer(('127.0.0.1',0),Provider)
            threading.Thread(target=provider.serve_forever,daemon=True).start()
            config=Path(directory)/'providers.json'
            config.write_text(json.dumps({'providers':[{'id':'fixture','name':'fixture','baseUrl':f'http://127.0.0.1:{provider.server_port}/v1','defaultModel':'fixture'}]}))
            with socket.socket() as s:s.bind(('127.0.0.1',0));port=s.getsockname()[1]
            env={**os.environ,'PITCREW_RUNTIME_DIR':directory,'PITCREW_PROVIDERS_FILE':str(config),'PITCREW_PORT':str(port)}
            child=None;runtime_pid=None;log=(Path(directory)/'web.log').open('wb')
            def spawn():return subprocess.Popen([sys.executable,'-X','utf8',str(HERE/('watchdog.py' if supervised else 'server.py'))]+(['--port',str(port)] if supervised else []),env=env,stdout=log,stderr=log,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
            project_id=''
            def api(path,data=None,token=None):
                if project_id and 'project=' not in path:path+=('&' if '?' in path else '?')+'project='+project_id
                request=urllib.request.Request(f'http://127.0.0.1:{port}/api/hub/'+path,data=None if data is None else json.dumps(data).encode(),headers={'X-PitCrew-Token':token or '', 'Content-Type':'application/json'})
                with urllib.request.urlopen(request,timeout=3) as r:return json.load(r)
            def wait_for(fn):
                deadline=time.monotonic()+20
                while time.monotonic()<deadline:
                    try:
                        value=fn()
                        if value:return value
                    except (OSError,ValueError):pass
                    time.sleep(.1)
                self.fail('condition timed out')
            try:
                child=spawn();first=wait_for(lambda:api('bootstrap'));runtime_pid=first['runtime']['pid'];token=first['token']
                self.assertNotEqual(runtime_pid,child.pid)
                info=json.loads((Path(directory)/'runtime.json').read_text())
                with self.assertRaises(urllib.error.HTTPError) as cm:
                    urllib.request.urlopen(f"http://127.0.0.1:{info['port']}/api/hub/bootstrap")
                self.assertEqual(cm.exception.code,403)
                with self.assertRaises(urllib.error.HTTPError) as cm:api('shared',{'text':'forged'},'wrong')
                self.assertEqual(cm.exception.code,403)
                if secondary:
                    folder=Path(directory)/'second-project';folder.mkdir()
                    project_id=api('projects',{'path':str(folder),'name':'Second'},token)['id']
                a=api('agents',{'name':'restart fixture','provider':'fixture'},token)
                api('send',{'agent':a['id'],'text':'integration only'},token);self.assertTrue(started.wait(5))
                if secondary:
                    default=api('bootstrap?project=default')
                    self.assertNotIn(a['id'],[x['id'] for x in default['agents']])
                    self.assertEqual(next(p for p in default['projects'] if p['id']==project_id)['running'],1)
                shutdown=urllib.request.Request(f"http://127.0.0.1:{info['port']}/internal/shutdown",data=b'{}',headers={'X-PitCrew-Runtime':info['secret']})
                with self.assertRaises(urllib.error.HTTPError) as cm:urllib.request.urlopen(shutdown)
                self.assertEqual(cm.exception.code,409)
                duplicate=subprocess.run([sys.executable,str(HERE/'runtime.py'),'--serve'],env=env,stdout=log,stderr=log,timeout=5)
                self.assertEqual(duplicate.returncode,0)
                self.assertEqual(json.loads((Path(directory)/'runtime.json').read_text())['pid'],runtime_pid)
                if supervised:
                    webfile=Path(directory)/f'web-{port}.json'
                    webpid=json.loads(webfile.read_text())['pid']
                    os.kill(webpid,signal.SIGTERM)
                    wait_for(lambda:json.loads(webfile.read_text())['pid']!=webpid)
                else:
                    child.terminate();child.wait(5)
                    child=spawn()
                second=wait_for(lambda:api('bootstrap'))
                self.assertEqual(second['runtime']['pid'],runtime_pid);self.assertEqual(second['token'],token)
                self.assertEqual(next(x for x in second['agents'] if x['id']==a['id'])['status'],'running')
                release.set()
                detail=wait_for(lambda:(d if (d:=api('detail?agent='+a['id']))['agent']['status']=='done' else None))
                self.assertEqual(''.join(m['text'] for m in detail['messages'] if m['role']=='assistant'),'BEFORE AFTER')
                self.assertEqual(len(calls),1)
                self.assertEqual(len([m for m in detail['messages'] if m['role']=='user']),1)
                with urllib.request.urlopen(shutdown) as r:self.assertEqual(r.status,200)
                print('Verified: independent runtime, web restart during active turn, same PID/token, complete response, one provider call, authentication guards.')
            finally:
                release.set()
                if supervised and child:
                    (Path(directory)/f'watchdog-{port}.stop').touch()
                    child.wait(20)
                    webfile=Path(directory)/f'web-{port}.json'
                    if webfile.exists():
                        try:os.kill(json.loads(webfile.read_text())['pid'],signal.SIGTERM)
                        except OSError:pass
                if child and child.poll() is None:child.terminate();child.wait(5)
                try:
                    stop_isolated_runtime(directory)
                finally:
                    provider.shutdown();provider.server_close();log.close()

if __name__=='__main__':unittest.main(verbosity=2)
