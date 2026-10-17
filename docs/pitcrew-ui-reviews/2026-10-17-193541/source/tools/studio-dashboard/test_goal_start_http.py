"""Real loopback HTTP/runtime/SQLite. Only AI dispatch is replaced; all files are temporary."""
from concurrent.futures import ThreadPoolExecutor
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest.mock import patch
import urllib.error
import urllib.request

HERE=Path(__file__).resolve().parent
CHILD=r'''
import json, os, time
from pathlib import Path
os.environ['PITCREW_RUNTIME_SERVICE']='1'
import hub, runtime, file_baselines
folder=Path(os.environ['PITCREW_RUNTIME_DIR'])
hub.ROOT=folder/'project'
original=file_baselines.observe
def gated(root, paths):
    if isinstance(paths,list) and 'slow.txt' in paths:
        (folder/'reading').touch()
        deadline=time.monotonic()+10
        while not (folder/'release').exists():
            if time.monotonic()>deadline:raise ValueError('test gate timed out')
            time.sleep(.01)
    return original(root,paths)
file_baselines.observe=gated
def dispatch(self, aid, data):
    prepared=self.prepare_message(data)  # same validation as real send; only provider execution is replaced
    cid=data['replyTo']['cycle'];c=self.cycle(cid)
    records=self.file_baselines(goal=cid)['items']
    if not c.get('startBaseline') or len(records)!=1 or records[0]['id']!=c['startBaseline']:
        raise ValueError('test: baseline was not saved before dispatch')
    if (folder/'fail-dispatch').exists():
        (folder/'failed-dispatch.json').write_text(json.dumps(dict(goal=cid,baseline=records[0]['id'])))
        raise ValueError('시험 전달 실패 · 재시도 필요')
    with (folder/'dispatch.jsonl').open('a',encoding='utf-8') as out:
        out.write(json.dumps(dict(project=self.project_id,goal=cid,baseline=records[0],agent=aid,attachments=data.get('attachments',[]),files=prepared.get('files',[])))+'\n')
    # Simulated work changes a temp file only after the real pre-dispatch observation.
    (self.root/'HUD.cs').write_text('after',encoding='utf-8')
hub.Hub.send=dispatch
hub.Run.execute=lambda self: (_ for _ in ()).throw(AssertionError('AI execution forbidden'))
hub.Hub.cycle_alert=lambda *args,**kwargs: None
runtime.serve()
'''


class GoalStartHttpTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(ignore_cleanup_errors=True);self.root=Path(self.tmp.name)
        project=self.root/'project';project.mkdir();(project/'HUD.cs').write_text('before',encoding='utf-8')
        (project/'slow.txt').write_text('slow',encoding='utf-8');(project/'folder').mkdir()
        self.log=(self.root/'child.log').open('wb')
        self.child=subprocess.Popen([sys.executable,'-X','utf8','-c',CHILD],cwd=HERE,
            env={**os.environ,'PITCREW_RUNTIME_DIR':str(self.root),'CODEX_HOME':str(self.root/'codex'),
                 'CLAUDE_CONFIG_DIR':str(self.root/'claude')},stdout=self.log,stderr=self.log,
            creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        self.addCleanup(self.cleanup)
        self.wait_descriptor()
        code,self.bootstrap=self.request('/api/hub/bootstrap');self.assertEqual(code,200)
        self.token=self.bootstrap['token'];self.aid=self.bootstrap['agents'][0]['id']
    def cleanup(self):
        (self.root/'release').touch()
        if self.child.poll() is None:self.child.terminate();self.child.wait(timeout=5)
        self.log.close();self.tmp.cleanup()
    def wait_for(self,predicate):
        deadline=time.monotonic()+8
        while time.monotonic()<deadline:
            if predicate():return
            self.assertIsNone(self.child.poll(),'isolated runtime exited early');time.sleep(.02)
        self.fail('isolated runtime gate timed out')
    def wait_descriptor(self,previous_pid=None):
        def read_ready():
            # Windows can deny a read briefly while runtime.json is atomically replaced.
            try:info=json.loads((self.root/'runtime.json').read_text())
            except (OSError,ValueError):return False
            if previous_pid is not None and info.get('pid')==previous_pid:return False
            self.info=info
            return True
        self.wait_for(read_ready)
    def request(self,path,data=None,token=None):
        req=urllib.request.Request(f"http://127.0.0.1:{self.info['port']}"+path,
            data=None if data is None else json.dumps(data).encode(),
            headers={'X-PitCrew-Runtime':self.info['secret'],'X-PitCrew-Token':getattr(self,'token','') if token is None else token})
        try:
            with urllib.request.urlopen(req,timeout=12) as response:return response.status,json.load(response)
        except urllib.error.HTTPError as error:
            with error:return error.code,json.load(error)
    def payload(self,**extra):return dict(action='start',mode='goal',agent=self.aid,goal='HTTP 파일 기준 검증',baselinePaths=['HUD.cs','new.cs'],**extra)
    def events(self):
        path=self.root/'dispatch.jsonl'
        return [json.loads(line) for line in path.read_text().splitlines()] if path.exists() else []
    def counts(self):
        return (self.request('/api/hub/goal-history')[1]['total'],len(self.request('/api/hub/file-baselines')[1]['items']))

    def test_start_compare_and_project_boundary_over_http(self):
        self.assertTrue(self.bootstrap['capabilities']['goalStartBaseline'])
        code,c=self.request('/api/hub/cycle',self.payload());self.assertEqual(code,200,c)
        events=self.events();self.assertEqual(len(events),1)
        record=events[0]['baseline'];self.assertEqual(record['id'],c['startBaseline'])
        self.assertEqual(record['entries'][0]['sha256'],hashlib.sha256(b'before').hexdigest())
        code,result=self.request('/api/hub/file-baselines?goal='+c['id']+'&id='+record['id']);self.assertEqual(code,200)
        self.assertEqual([r['status'] for r in result['entries']],['changed','missing'])
        code,history=self.request('/api/hub/goal-history?id='+c['id']);self.assertEqual(code,200);self.assertEqual(history['cycle']['id'],c['id'])
        other=self.root/'other';other.mkdir();(other/'HUD.cs').write_text('other',encoding='utf-8')
        code,p=self.request('/api/hub/projects',{'path':str(other)});self.assertEqual(code,200,p)
        suffix='?project='+p['id']
        self.assertEqual(self.request('/api/hub/file-baselines'+suffix+'&id='+record['id'])[0],400)
        code,rejected=self.request('/api/hub/cycle'+suffix,self.payload())
        self.assertEqual(code,400);self.assertIn('에이전트를 찾을 수 없습니다',rejected['error'])
        self.assertEqual(self.request('/api/hub/goal-history'+suffix)[1]['total'],0)
        self.assertEqual(self.request('/api/hub/file-baselines'+suffix)[1]['items'],[])
        self.assertEqual(self.counts(),(1,1));self.assertEqual(len(self.events()),1)
        code,b=self.request('/api/hub/bootstrap'+suffix);self.assertEqual(code,200)
        payload=self.payload();payload['agent']=b['agents'][0]['id']
        code,c2=self.request('/api/hub/cycle'+suffix,payload);self.assertEqual(code,200,c2)
        e=self.events()[-1];self.assertEqual(e['project'],p['id'])
        self.assertEqual(e['baseline']['entries'][0]['sha256'],hashlib.sha256(b'other').hexdigest())

    def test_attachment_and_start_baseline_arrive_together_over_http(self):
        import base64
        self.assertTrue(self.bootstrap['capabilities']['goalStartAttachments'])
        code,saved=self.request('/api/hub/upload',dict(name='before.txt',type='text/plain',data=base64.b64encode(b'game evidence').decode()))
        self.assertEqual(code,200,saved)
        payload={**self.payload(),'attachments':[{'name':saved['name'],'path':saved['path']}]}
        code,c=self.request('/api/hub/cycle',payload);self.assertEqual(code,200,c)
        event=self.events()[0];self.assertEqual(event['attachments'],payload['attachments'])
        self.assertEqual(event['baseline']['id'],c['startBaseline'])
        self.assertEqual(event['baseline']['entries'][0]['sha256'],hashlib.sha256(b'before').hexdigest())
        self.assertEqual(Path(saved['path']).read_bytes(),b'game evidence')

    def team_payload_with_upload(self):
        import base64
        teams={};leads={}
        for kind in ('research','build','qa'):
            code,a=self.request('/api/hub/agents',dict(name='HTTP '+kind,provider='codex'))
            self.assertEqual(code,200,a);leads[kind]=a['id']
            code,t=self.request('/api/hub/teams',dict(name=kind,members=[a['id']],lead=a['id']))
            self.assertEqual(code,200,t);teams[kind]=t['id']
        code,saved=self.request('/api/hub/upload',dict(name='before.txt',type='text/plain',data=base64.b64encode(b'team evidence').decode()))
        self.assertEqual(code,200,saved)
        payload=dict(action='start',goal='Team HUD fix',criteria='HUD is readable',teams=teams,
                     baselinePaths=['HUD.cs'],attachments=[dict(name=saved['name'],path=saved['path'])])
        return payload,leads,saved

    def test_team_attachment_baseline_and_research_recipient_over_http(self):
        payload,leads,saved=self.team_payload_with_upload()
        code,c=self.request('/api/hub/cycle',payload);self.assertEqual(code,200,c)
        self.assertEqual(c['status'],'running');self.assertEqual(c['startAttachments'],payload['attachments'])
        self.assertEqual(c['stages'][0]['kind'],'research');self.assertEqual(c['stages'][0]['agent'],leads['research'])
        events=self.events();self.assertEqual(len(events),1);e=events[0]
        self.assertEqual(e['agent'],leads['research']);self.assertEqual(e['attachments'],payload['attachments'])
        self.assertEqual(e['files'],payload['attachments'])
        self.assertEqual(e['baseline']['id'],c['startBaseline'])
        self.assertEqual(e['baseline']['entries'][0]['sha256'],hashlib.sha256(b'before').hexdigest())
        stored=self.request('/api/hub/goal-history?id='+c['id'])[1]['cycle']
        self.assertEqual(stored['startAttachments'],payload['attachments'])
        self.assertEqual(self.counts(),(1,1));self.assertEqual(Path(saved['path']).read_bytes(),b'team evidence')

    def test_team_retry_with_removed_upload_fails_without_dispatch_over_http(self):
        payload,leads,saved=self.team_payload_with_upload()
        gate=self.root/'fail-dispatch';gate.touch()
        code,c=self.request('/api/hub/cycle',payload);self.assertEqual(code,200,c)
        self.assertEqual(c['status'],'failed');self.assertEqual(c['startAttachments'],payload['attachments'])
        self.assertEqual(self.counts(),(1,1));self.assertEqual(self.events(),[])
        gate.unlink();Path(saved['path']).unlink()  # delete only this temporary fixture's uploaded file
        code,retried=self.request('/api/hub/cycle',dict(action='retry',id=c['id']))
        self.assertEqual(code,200,retried);self.assertEqual(retried['status'],'failed')
        self.assertIn('첨부 파일을 찾을 수 없습니다',retried['stages'][-1]['summary'])
        self.assertEqual(retried['startBaseline'],c['startBaseline'])
        self.assertEqual(self.counts(),(1,1));self.assertEqual(self.events(),[])
        # Restoring the same fixture upload permits a retry with the original baseline and recipient.
        Path(saved['path']).write_bytes(b'team evidence')
        code,restored=self.request('/api/hub/cycle',dict(action='retry',id=c['id']))
        self.assertEqual(code,200,restored);self.assertEqual(restored['status'],'running')
        self.assertEqual(restored['startBaseline'],c['startBaseline'])
        self.assertEqual(len(self.events()),1);self.assertEqual(self.events()[0]['agent'],leads['research'])
        self.assertEqual(self.events()[0]['attachments'],payload['attachments'])

    def test_unknown_cycle_actions_do_not_stop_existing_goal(self):
        code,goal=self.request('/api/hub/cycle',self.payload());self.assertEqual(code,200,goal)
        route='/api/hub/goal-history?id='+goal['id']
        before=self.request(route)[1]['cycle']
        baseline=self.request('/api/hub/file-baselines?goal='+goal['id'])[1]
        for payload in ({'id':goal['id']}, *({'id':goal['id'],'action':value} for value in ('pause','stpo','',None,[],{}))):
            with self.subTest(action=payload.get('action','missing')):
                code,error=self.request('/api/hub/cycle',payload)
                self.assertEqual(code,400,error)
                self.assertEqual(self.request(route)[1]['cycle'],before)
                self.assertEqual(self.request('/api/hub/file-baselines?goal='+goal['id'])[1],baseline)
                self.assertEqual(len(self.events()),1)
        code,stopped=self.request('/api/hub/cycle',{'id':goal['id'],'action':'stop'})
        self.assertEqual(code,200,stopped);self.assertEqual(stopped['status'],'stopped')
        # A stopped goal is not a maintenance pause: retry explicitly rejects it.
        code,error=self.request('/api/hub/cycle',{'id':goal['id'],'action':'retry'})
        self.assertEqual(code,400,error)
        self.assertEqual(self.request(route)[1]['cycle']['status'],'stopped')
        self.assertEqual(self.request('/api/hub/file-baselines?goal='+goal['id'])[1],baseline)
        self.assertEqual(len(self.events()),1)

    def test_malformed_attachments_return_400_without_saving_or_dispatch(self):
        for item in ('invalid',None,123,[]):
            with self.subTest(item=item):
                code,error=self.request('/api/hub/cycle',self.payload(attachments=[item]))
                self.assertEqual(code,400,error)
                self.assertIn('첨부 항목의 형식',error['error'])
                self.assertEqual(self.counts(),(0,0))
                self.assertEqual(self.events(),[])

    def test_rejected_requests_leave_no_goal_or_baseline(self):
        before=self.counts()
        self.assertEqual(self.request('/api/hub/cycle',self.payload(),token='wrong')[0],403)
        for paths in (['../outside'],['HUD.cs','folder'],[],None):
            payload=self.payload();payload['baselinePaths']=paths
            code,body=self.request('/api/hub/cycle',payload)
            self.assertEqual(code,400,body);self.assertEqual(self.counts(),before)
        self.assertEqual(self.events(),[])

    def test_hold_request_is_project_scoped_and_does_not_dispatch(self):
        code,goal=self.request('/api/hub/cycle',self.payload());self.assertEqual(code,200,goal)
        route='/api/hub/cycle';payload={'id':goal['id'],'action':'hold'}
        self.assertEqual(self.request(route,payload,token='wrong')[0],403)
        for _ in range(2):
            code,held=self.request(route,payload);self.assertEqual(code,200,held)
            self.assertTrue(held['holdRequested']);self.assertEqual(held['status'],'running')
            self.assertEqual(len(held['stages']),1)
        self.assertEqual(self.request(route,{'id':goal['id'],'action':'release'})[0],400)
        before=self.request('/api/hub/goal-history?id='+goal['id'])[1]['cycle']
        other=self.root/'hold-other';other.mkdir()
        code,project=self.request('/api/hub/projects',{'path':str(other)});self.assertEqual(code,200,project)
        self.assertEqual(self.request(route+'?project='+project['id'],payload)[0],400)
        self.assertEqual(self.request('/api/hub/goal-history?id='+goal['id'])[1]['cycle'],before)
        self.assertEqual(len(self.events()),1)

    def test_maintenance_snapshot_is_global_and_read_only(self):
        code,snapshot=self.request('/api/hub/maintenance-status');self.assertEqual(code,200,snapshot)
        self.assertTrue(snapshot['ready'])
        code,goal=self.request('/api/hub/cycle',self.payload());self.assertEqual(code,200,goal)
        before=self.request('/api/hub/goal-history?id='+goal['id'])[1]
        other=self.root/'maintenance-other';other.mkdir()
        code,project=self.request('/api/hub/projects',{'path':str(other)});self.assertEqual(code,200,project)
        code,snapshot=self.request('/api/hub/maintenance-status?project='+project['id'])
        self.assertEqual(code,200,snapshot);self.assertFalse(snapshot['ready'])
        rows={p['id']:p for p in snapshot['projects']}
        self.assertIn(goal['id'],rows['default']['runningGoals'])
        self.assertIn('runningGoals',rows['default']['blockers'])
        self.assertFalse(rows[project['id']]['loaded'])
        self.assertEqual(self.request('/api/hub/goal-history?id='+goal['id'])[1],before)
        self.assertEqual(len(self.events()),1)

    def test_runtime_identity_exposes_startup_fingerprints_without_secrets(self):
        code,result=self.request('/api/hub/runtime-identity?project=../ignored')
        self.assertEqual(code,200,result);self.assertTrue(result['available'])
        self.assertEqual(result['runtime']['pid'],self.info['pid'])
        self.assertEqual(set(result['modules']),{'hub.py','server.py','runtime.py','runtime_identity.py'})
        for row in result['modules'].values():
            self.assertRegex(row['loadedCodeSha256'],r'^[0-9a-f]{64}$')
            self.assertGreater(row['functionCount'],0)
            self.assertRegex(row['sourceSha256AtCapture'],r'^[0-9a-f]{64}$')
            self.assertEqual(row['sourceSha256Now'],row['sourceSha256AtCapture'])
            self.assertFalse(row['sourceChangedSinceCapture'])
        self.assertNotIn(self.token,json.dumps(result))
        self.assertNotIn(self.info['secret'],json.dumps(result))
        self.assertEqual(self.counts(),(0,0));self.assertEqual(self.events(),[])

    def test_secondary_goal_recovers_after_runtime_restart_and_retries_once(self):
        project=self.root/'second';project.mkdir();(project/'HUD.cs').write_text('before',encoding='utf-8')
        code,p=self.request('/api/hub/projects',dict(path=str(project)))
        self.assertEqual(code,200,p)
        query='?project='+p['id']
        code,b=self.request('/api/hub/bootstrap'+query);self.assertEqual(code,200,b)
        aid=b['agents'][0]['id']
        payload=self.payload();payload['agent']=aid
        code,c=self.request('/api/hub/cycle'+query,payload)
        self.assertEqual(code,200,c);self.assertEqual(len(self.events()),1)
        before=c['stages'][0]['input']
        old_pid=self.info['pid']
        # Only this test's owned runtime is terminated; no production process is touched.
        self.child.terminate();self.child.wait(timeout=5)
        self.child=subprocess.Popen([sys.executable,'-X','utf8','-c',CHILD],cwd=HERE,
            env={**os.environ,'PITCREW_RUNTIME_DIR':str(self.root),'CODEX_HOME':str(self.root/'codex'),
                 'CLAUDE_CONFIG_DIR':str(self.root/'claude')},stdout=self.log,stderr=self.log,
            creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
        self.wait_descriptor(previous_pid=old_pid)
        self.assertNotEqual(self.info['pid'],old_pid)
        code,b=self.request('/api/hub/bootstrap');self.assertEqual(code,200,b)
        self.token=b['token']
        self.assertEqual(self.request('/api/hub/goal-history')[1]['total'],0)
        code,recovered=self.request('/api/hub/goal-history'+query+'&id='+c['id'])
        self.assertEqual(code,200,recovered)
        recovered=recovered['cycle']
        self.assertEqual(recovered['status'],'paused')
        self.assertEqual(recovered['stages'][0]['status'],'interrupted')
        self.assertEqual(recovered['startBaseline'],c['startBaseline'])
        self.assertEqual(len(self.events()),1)  # restart/read did not resend the input
        code,retried=self.request('/api/hub/cycle'+query,dict(id=c['id'],action='retry'))
        self.assertEqual(code,200,retried);self.assertEqual(retried['id'],c['id'])
        self.assertEqual(retried['stages'][-1]['input'],before)
        self.assertEqual(self.events()[-1]['agent'],aid)
        self.assertEqual(self.request('/api/hub/cycle'+query,dict(id=c['id'],action='retry'))[0],400)
        self.assertEqual(len(self.events()),2)
        code,again=self.request('/api/hub/goal-history'+query+'&id='+c['id'])
        self.assertEqual(code,200,again);self.assertEqual(again['cycle']['status'],'running')

    def test_missing_stage_status_allows_project_and_normal_goal_http_reads(self):
        import hub
        project=self.root/'damaged-project';project.mkdir();(project/'HUD.cs').write_text('before',encoding='utf-8')
        code,p=self.request('/api/hub/projects',dict(path=str(project)))
        self.assertEqual(code,200,p)
        # Persist a previous session's records before the runtime lazily opens this project.
        seed=hub.Hub(self.root/'projects'/p['id'],root=project)
        try:
            aid=seed.store.agents()[0]['id']
            with patch.object(seed,'send'),patch.object(seed,'cycle_alert'):
                good=seed.start_cycle(dict(mode='goal',agent=aid,goal='normal goal',baselinePaths=['HUD.cs']))
            bad={**good,'id':'damaged','goal':'damaged record','stages':[dict(kind='build')]}
            seed.store.put('cycles',[bad,good])
        finally:seed.close();seed.store.db.close()
        query='?project='+p['id']
        for _ in range(3):
            code,b=self.request('/api/hub/bootstrap'+query)
            self.assertEqual(code,200,b)
            warnings=[x for x in b['activity'] if 'damaged' in x['text']]
            self.assertEqual(len(warnings),1)
            self.assertIn('단계 상태',warnings[0]['text'])
        code,history=self.request('/api/hub/goal-history'+query)
        self.assertEqual(code,200,history);self.assertEqual(history['total'],2)
        code,recovered=self.request('/api/hub/goal-history'+query+'&id='+good['id'])
        self.assertEqual(code,200,recovered)
        self.assertEqual(recovered['cycle']['status'],'paused')
        self.assertEqual(recovered['cycle']['startBaseline'],good['startBaseline'])
        code,damaged=self.request('/api/hub/goal-history'+query+'&id=damaged')
        self.assertEqual(code,200,damaged);self.assertEqual(damaged['cycle'],bad)
        self.assertEqual(self.events(),[])  # recovery/read did not invoke even the fake dispatcher

    def test_shutdown_waits_for_start_request_and_response(self):
        payload=self.payload();payload['baselinePaths']=['slow.txt']
        with ThreadPoolExecutor(max_workers=1) as pool:
            pending=pool.submit(self.request,'/api/hub/cycle',payload)
            try:
                self.wait_for(lambda:(self.root/'reading').exists())
                snapshot=self.request('/api/hub/maintenance-status')[1]
                self.assertFalse(snapshot['ready'])
                self.assertIn('pendingRequests',snapshot['blockers'])
                self.assertEqual(self.request('/internal/shutdown',{})[0],409)
                self.assertIsNone(self.child.poll())
            finally:(self.root/'release').touch()
            code,c=pending.result(timeout=8);self.assertEqual(code,200,c)
        self.assertEqual(self.counts(),(1,1));self.assertEqual(len(self.events()),1)
        # The dispatcher is a stub: no run exists, but the running goal still blocks shutdown.
        self.assertEqual(self.request('/internal/shutdown',{})[0],409)
        self.assertEqual(self.request('/api/hub/cycle',{'id':c['id'],'action':'stop'})[0],200)
        self.assertEqual(self.request('/internal/shutdown',{})[0],200)
        self.child.wait(timeout=5);self.assertEqual(self.child.returncode,0)


if __name__=='__main__':unittest.main()
