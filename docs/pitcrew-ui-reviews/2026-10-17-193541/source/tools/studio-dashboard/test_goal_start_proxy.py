"""Real web proxy -> isolated runtime -> SQLite; no browser fixture or external AI."""
import json
import os
import subprocess
import sys
import urllib.error
import urllib.request
import unittest
import test_goal_start_http as base

HERE=base.HERE

WEB=r'''
import json, os
from pathlib import Path
from http.server import ThreadingHTTPServer
import server
server.H.runtime_proxy=True
class ProbeHandler(server.H):
    def do_GET(self):
        if self.path=='/__test/web-identity':
            return self._json(dict(pid=os.getpid(),port=self.server.server_port))
        return super().do_GET()
service=ThreadingHTTPServer(('127.0.0.1',0),ProbeHandler)
server.PORT=service.server_port
folder=Path(os.environ['PITCREW_RUNTIME_DIR'])
temp=folder/'test-web.tmp'
temp.write_text(json.dumps(dict(port=service.server_port,pid=os.getpid())))
temp.replace(folder/'test-web.json')
service.serve_forever()
'''


class GoalStartProxyTests(base.GoalStartHttpTests):
    # The inherited success/project-boundary and rejection cases now traverse the web proxy.
    # Internal shutdown is deliberately not exposed through the public web proxy.
    test_shutdown_waits_for_start_request_and_response=None

    def setUp(self):
        self.web_info=None
        super().setUp()
        self.web_log=(self.root/'web.log').open('wb')
        self.web=subprocess.Popen([sys.executable,'-X','utf8','-c',WEB],cwd=HERE,
            env={**os.environ,'PITCREW_RUNTIME_DIR':str(self.root)},stdout=self.web_log,stderr=self.web_log,
            creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        self.addCleanup(self.cleanup_web)
        self.wait_for(lambda:(self.root/'test-web.json').exists())
        self.web_info=json.loads((self.root/'test-web.json').read_text())
        code,bootstrap=self.request('/api/hub/bootstrap')
        # Windows venv launchers can have a different PID from the serving interpreter.
        self.assertEqual(code,200);self.assertEqual(bootstrap['runtime']['pid'],self.info['pid'])
        self.assertEqual(bootstrap['token'],self.token)

    def cleanup_web(self):
        if self.web.poll() is None:self.web.terminate();self.web.wait(timeout=5)
        self.web_log.close()

    def request(self,path,data=None,token=None):
        if not self.web_info:return super().request(path,data,token)
        origin=f"http://127.0.0.1:{self.web_info['port']}"
        # Browser-facing requests have no runtime secret; the real proxy supplies it.
        req=urllib.request.Request(origin+path,data=None if data is None else json.dumps(data,ensure_ascii=False).encode('utf-8'),
            headers={'Origin':origin,'Content-Type':'application/json','X-PitCrew-Token':self.token if token is None else token})
        try:
            with urllib.request.urlopen(req,timeout=12) as response:return response.status,json.load(response)
        except urllib.error.HTTPError as error:
            with error:return error.code,json.load(error)

    def test_first_dispatch_failure_preserves_existing_records_and_retry_succeeds(self):
        code,saved=self.request('/api/hub/file-baselines',dict(label='기존 기준',agent=self.aid,paths=['HUD.cs']))
        self.assertEqual(code,200);old=saved['record'];before=self.counts()
        marker=self.root/'fail-dispatch';marker.touch()
        code,error=self.request('/api/hub/cycle',self.payload())
        self.assertEqual(code,400);self.assertIn('목표를 시작하지 못했습니다',error['error'])
        self.assertIn('시험 전달 실패 · 재시도 필요',error['error'])
        self.assertEqual(self.counts(),before);self.assertEqual(self.events(),[])
        self.assertEqual(self.request('/api/hub/file-baselines')[1]['items'],[old])
        failed=json.loads((self.root/'failed-dispatch.json').read_text())
        self.assertEqual(self.request('/api/hub/goal-history?id='+failed['goal'])[0],400)
        self.assertEqual(self.request('/api/hub/file-baselines?id='+failed['baseline'])[0],400)
        self.assertEqual((self.root/'project'/'HUD.cs').read_text(),'before')
        marker.unlink()
        code,c=self.request('/api/hub/cycle',self.payload());self.assertEqual(code,200,c)
        self.assertEqual(self.counts(),(1,2));self.assertEqual(len(self.events()),1)
        self.assertEqual(self.request('/api/hub/file-baselines?id='+old['id'])[1]['record'],old)

    def test_secondary_goal_recovers_after_runtime_restart_and_retries_once(self):
        # Keep the web process alive while the inherited test replaces only its owned backend.
        # This verifies the current proxy, not the unknown code loaded by a legacy production web process.
        code,web_before=self.request('/__test/web-identity')
        self.assertEqual(code,200,web_before)
        self.assertEqual(web_before,self.web_info)
        code,before=self.request('/api/hub/runtime-identity')
        self.assertEqual(code,200,before)
        old_pid=before['runtime']['pid']
        super().test_secondary_goal_recovers_after_runtime_restart_and_retries_once()
        self.assertIsNone(self.web.poll())
        code,web_after=self.request('/__test/web-identity')
        self.assertEqual(code,200,web_after)
        self.assertEqual(web_after,web_before)
        code,after=self.request('/api/hub/runtime-identity')
        self.assertEqual(code,200,after)
        self.assertNotEqual(after['runtime']['pid'],old_pid)
        self.assertEqual(after['runtime']['pid'],self.info['pid'])
        for name,row in after['modules'].items():
            self.assertEqual(row['loadedCodeSha256'],before['modules'][name]['loadedCodeSha256'])
            self.assertIs(row['sourceChangedSinceCapture'],False)
        code,bootstrap=self.request('/api/hub/bootstrap')
        self.assertEqual(code,200,bootstrap)
        for capability in ('stageReports','goalHistory','goalFileBaselines','maintenanceStatus','runtimeIdentity'):
            self.assertTrue(bootstrap['capabilities'][capability])
        event=self.events()[-1]
        query='?project='+event['project']
        code,report=self.request('/api/hub/stage-report'+query+'&id='+event['goal']+'&stage=0')
        self.assertEqual(code,200,report)
        self.assertEqual(report['id'],event['goal'])
        self.assertEqual(report['projectId'],event['project'])
        code,comparison=self.request('/api/hub/file-baselines'+query+'&goal='+event['goal']+'&id='+event['baseline']['id'])
        self.assertEqual(code,200,comparison)
        self.assertEqual(comparison['record']['id'],event['baseline']['id'])
        self.assertEqual(comparison['entries'][0]['status'],'changed')
        code,maintenance=self.request('/api/hub/maintenance-status')
        self.assertEqual(code,200,maintenance)
        self.assertFalse(maintenance['ready'])
        project=next(p for p in maintenance['projects'] if p['id']==event['project'])
        self.assertIn(event['goal'],project['runningGoals'])
        self.assertEqual(len(self.events()),2)  # initial request + explicit retry only

    def test_requests_during_backend_gap_fail_without_replay(self):
        code,web_before=self.request('/__test/web-identity')
        self.assertEqual(code,200,web_before)
        old_pid=self.info['pid']
        # This test owns the directory and processes. Exercise graceful shutdown while idle.
        code,result=base.GoalStartHttpTests.request(self,'/internal/shutdown',{})
        self.assertEqual(code,200,result)
        self.child.wait(timeout=5)
        self.assertEqual(self.child.returncode,0)
        for path,data in [('/api/hub/bootstrap',None),('/api/hub/cycle',self.payload())]:
            code,error=self.request(path,data)
            self.assertEqual(code,503,error)
            self.assertIn('error',error)
        self.assertEqual(self.events(),[])
        self.assertIsNone(self.web.poll())
        self.child=subprocess.Popen([sys.executable,'-X','utf8','-c',base.CHILD],cwd=HERE,
            env={**os.environ,'PITCREW_RUNTIME_DIR':str(self.root),'CODEX_HOME':str(self.root/'codex'),
                 'CLAUDE_CONFIG_DIR':str(self.root/'claude')},stdout=self.log,stderr=self.log,
            creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        self.wait_descriptor(previous_pid=old_pid)
        code,b=self.request('/api/hub/bootstrap')
        self.assertEqual(code,200,b);self.token=b['token']
        self.assertNotEqual(b['runtime']['pid'],old_pid)
        self.assertEqual(self.request('/__test/web-identity'),(200,web_before))
        self.assertEqual(self.counts(),(0,0))
        self.assertEqual(self.events(),[])
        # A new explicit request succeeds once. The failed request was never queued or replayed.
        code,goal=self.request('/api/hub/cycle',self.payload())
        self.assertEqual(code,200,goal)
        self.assertEqual(self.counts(),(1,1))
        self.assertEqual(len(self.events()),1)


if __name__=='__main__':unittest.main()
