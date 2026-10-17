"""Offline regression tests: state, context provenance, cancellation, HTTP boundaries, providers."""
import json
import tempfile
import threading
import time
import unittest
import urllib.error
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from unittest.mock import patch
from pathlib import Path

import hub

class HubTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.h=hub.Hub(self.tmp.name)
        # These offline state/provider tests must not launch real desktop notifications.
        notices=patch.object(self.h,'alert');notices.start();self.addCleanup(notices.stop)
        self.a,self.b=self.h.store.agents()[:2]

    def tearDown(self):
        self.h.close();self.h.store.db.close();self.tmp.cleanup()

    def test_persistence_and_recovery(self):
        self.h.store.update(self.a['id'],model='chosen-model',status='running')
        self.h.store.message(self.a['id'],'user','한글 conversation')
        self.h.store.message(self.a['id'],'assistant','partial',streaming=True)
        self.h.store.message(self.a['id'],'tool','pending',status='running')
        second=hub.Store(self.tmp.name)
        self.assertEqual(second.agent(self.a['id'])['model'],'chosen-model')
        self.assertEqual(second.agent(self.a['id'])['status'],'interrupted')
        self.assertEqual(second.messages(self.a['id'])[0]['text'],'한글 conversation')
        self.assertFalse(second.messages(self.a['id'])[1]['streaming'])
        self.assertEqual(second.messages(self.a['id'])[2]['status'],'interrupted')
        second.db.close()

    def test_forward_is_immutable_with_provenance(self):
        m=self.h.store.message(self.a['id'],'assistant','Evidence 123')
        c=self.h.context(self.b,[{'agent':self.a['id'],'message':m['id']}])
        self.h.store.edit_message(m['id'],text='changed')
        self.assertEqual(c['snapshots'][0]['text'],'Evidence 123')
        self.assertEqual(c['snapshots'][0]['agent'],self.a['id'])
        with self.assertRaises(ValueError):self.h.context(self.b,[{'agent':self.a['id'],'message':'forged'}])

    def test_peers_include_only_selected_context(self):
        self.h.store.message(self.a['id'],'assistant','Selected context')
        other=self.h.store.agents()[2]
        self.h.store.message(other['id'],'assistant','Do not include')
        b=self.h.store.update(self.b['id'],peers=[self.a['id']])
        self.h.store.put('shared','Shared decision')
        c=self.h.context(b,[])
        self.assertEqual(c['shared'],'Shared decision')
        self.assertEqual(len(c['snapshots']),1)
        self.assertIn('Selected context',c['snapshots'][0]['text'])
        self.assertNotIn('Do not include',hub.dumps(c))

    def test_user_team_persistence_and_context_scope(self):
        t=self.h.save_team(dict(name='아트 팀',members=[self.a['id']],context='Keep the palette warm'))
        self.assertIn('Keep the palette warm',self.h.context(self.a,[])['shared'])
        self.assertNotIn('Keep the palette warm',self.h.context(self.b,[])['shared'])
        self.assertEqual(self.h.context(self.a,[])['snapshots'],[])
        other=hub.Store(self.tmp.name)
        self.assertEqual(other.get('teams',[]),[t]);other.db.close()
        new=self.h.save_agent(dict(provider='codex',name='Artist',team=t['id']))
        self.assertIn(new['id'],self.h.store.get('teams',[])[0]['members'])
        before=len(self.h.store.agents())
        with self.assertRaises(ValueError):self.h.save_agent(dict(provider='codex',name='Bad',team='missing'))
        self.assertEqual(len(self.h.store.agents()),before)

    def test_bulk_import_restores_and_deduplicates(self):
        t=self.h.save_team(dict(name='Art',members=[],context=''))
        source=dict(id='source',parent=None,provider='claude',name='original')
        with patch.object(self.h.sources,'get',return_value=source):
            result=self.h.import_sources(dict(sources=['source','source'],team=t['id'],names={'source':'Art history'}))
            a=result['agents'][0]
            self.assertEqual(a['name'],'Art history')
            self.h.store.update(a['id'],archived=True)
            again=self.h.import_sources(dict(sources=['source'],team=t['id']))
            self.assertEqual(again['agents'][0]['id'],a['id'])
            self.assertFalse(again['agents'][0]['archived'])
            self.assertEqual(again['team']['members'],[a['id']])
            before=len(self.h.store.agents())
            with self.assertRaises(ValueError):self.h.import_sources(dict(sources=['source'],team='missing'))
            self.assertEqual(len(self.h.store.agents()),before)

    def test_team_tree_deduplicates_connected_child_and_retains_parent(self):
        parent=self.h.store.create(name='parent',provider='claude',source='parent-source',status='observing')
        child=self.h.store.create(name='child',provider='claude',source='child-source',status='observing')
        sources=[dict(id='parent-source',path='parent.jsonl',provider='claude',parent=None,name='parent',updated=1),
                 dict(id='child-source',path='child.jsonl',provider='claude',parent='parent-source',name='child',label='Reviewer',updated=2)]
        with patch.object(self.h.sources,'discover',return_value=sources):
            tree=self.h.team()
        self.assertNotIn(child['id'],[g['agent']['id'] for g in tree['groups']])
        group=next(g for g in tree['groups'] if g['agent']['id']==parent['id'])
        self.assertEqual(group['children'][0]['agent'],child['id'])
        self.assertEqual(group['children'][0]['status'],'observing')
        self.assertEqual(group['children'][0]['name'],'Reviewer')

    def test_busy_agent_rejects_duplicate_and_edit(self):
        ready=threading.Event();done=threading.Event()
        def fake(run):
            ready.set();done.wait(3)
        with patch.object(hub.Run,'execute',fake):
            self.h.send(self.b['id'],{'text':'one'})
            self.assertTrue(ready.wait(1))
            with self.assertRaises(ValueError):self.h.send(self.b['id'],{'text':'two'})
            with self.assertRaises(ValueError):self.h.save_agent({'id':self.b['id'],'model':'new'})
            done.set();self.h.runs.pop(self.b['id'])
        self.assertEqual(len(self.h.store.messages(self.b['id'])),1)

    def test_permission_resolves_and_cancellation_declines(self):
        r=hub.Run(self.h,self.a,{},'test');self.h.runs[self.a['id']]=r
        answers=[]
        t=threading.Thread(target=lambda:answers.append(self.h.request(self.a['id'],'approval',{'command':'test'})))
        t.start()
        deadline=time.time()+2
        while not self.h.approvals and time.time()<deadline:time.sleep(.01)
        rid=next(iter(self.h.approvals));self.h.decide(rid,{'allow':True});t.join(2)
        self.assertEqual(answers,[{'allow':True,'answers':{}}])
        t=threading.Thread(target=lambda:answers.append(self.h.request(self.a['id'],'approval',{})));t.start();r.stop();t.join(2)
        self.assertFalse(t.is_alive());self.assertFalse(answers[-1]['allow'])

    def test_observers_cannot_be_sent_to(self):
        a=self.h.store.create(provider='codex',source='test')
        with self.assertRaises(ValueError):self.h.send(a['id'],{'text':'hello'})

    def test_resume_import_keeps_identity_history_and_team(self):
        source=dict(id='external',provider='claude',parent=None,name='external')
        sid='11111111-1111-4111-8111-111111111111'
        history=[dict(id='source-first',role='user',text='Earlier context',ts=1)]
        with patch.object(self.h.sources,'get',return_value=source),patch.object(self.h.sources,'native_session',return_value=sid),patch.object(self.h.sources,'messages',return_value=history):
            a=self.h.watch('external')
            team=self.h.save_team(dict(name='Team',members=[a['id']]))
            resumed=self.h.resume_source(a['id'])
            self.assertEqual(resumed['session'],sid)
            self.assertIsNone(resumed['source'])
            self.assertEqual(resumed['originSource'],'external')
            self.assertEqual(self.h.watch('external')['id'],a['id'])
            self.assertEqual(self.h.resume_source(a['id'])['session'],sid)
            self.assertEqual(self.h.store.get('teams')[0]['members'],team['members'])
            self.h.store.message(a['id'],'assistant','New reply')
            self.assertEqual([m['text'] for m in self.h.detail(a['id'])['messages']],['Earlier context','New reply'])
            duplicate=self.h.store.create(provider='claude',source='external')
            with self.assertRaises(ValueError):self.h.resume_source(duplicate['id'])

    def test_native_session_uses_codex_metadata_and_rejects_children(self):
        path=Path(self.tmp.name)/'rollout.jsonl'
        sid='22222222-2222-4222-8222-222222222222'
        path.write_text(json.dumps(dict(type='session_meta',payload=dict(id=sid,cwd=str(hub.ROOT)))),encoding='utf-8')
        src=dict(provider='codex',parent=None,path=str(path))
        with patch.object(self.h.sources,'get',return_value=src):
            self.assertEqual(self.h.sources.native_session('id'),sid)
            src['parent']='parent'
            with self.assertRaises(ValueError):self.h.sources.native_session('id')
            src['parent']=None
            path.write_text(json.dumps(dict(type='session_meta',payload=dict(id=sid,cwd='other-project'))),encoding='utf-8')
            with self.assertRaises(ValueError):self.h.sources.native_session('id')

    def test_model_options_checked_against_live_catalog(self):
        self.h.catalogs['codex']={'models':[{'id':'example','efforts':[{'id':'low'}],'tiers':[]}]}
        with self.assertRaises(ValueError):self.h.save_agent({'id':self.b['id'],'model':'example','effort':'ultra'})
        saved=self.h.save_agent({'id':self.b['id'],'model':'example','effort':'low'})
        self.assertEqual(saved['effort'],'low')

    def test_source_scope_and_tail_parser(self):
        with self.assertRaises(ValueError):self.h.sources.get('../../secret')
        p=Path(self.tmp.name)/'partial.jsonl'
        p.write_text('{"text":"'+'x'*3000+'"}\n{"type":"ok"}\n{unfinished',encoding='utf-8')
        self.assertEqual(hub.tail_json(p,100),[{'type':'ok'}])

    def test_external_message_ids_survive_tail_shift(self):
        p=Path(self.tmp.name)/'source.jsonl'
        first={'type':'response_item','timestamp':'t1','payload':{'type':'message','role':'assistant','content':[{'type':'output_text','text':'keep this'}]}}
        p.write_text(hub.dumps(first)+'\n',encoding='utf-8')
        with patch.object(self.h.sources,'get',return_value={'provider':'codex','path':str(p)}):
            original=self.h.sources.messages('key')[0]['id']
            p.write_text(hub.dumps({'type':'ignored'})+'\n'+hub.dumps(first)+'\n',encoding='utf-8')
            self.assertEqual(original,self.h.sources.messages('key')[0]['id'])

    def test_compaction_is_not_attributed_to_user(self):
        p=Path(self.tmp.name)/'source.jsonl'
        p.write_text(hub.dumps({'type':'user','uuid':'summary','isCompactSummary':True,'message':{'role':'user','content':'summary'}})+'\n',encoding='utf-8')
        with patch.object(self.h.sources,'get',return_value={'provider':'claude','path':str(p)}):
            self.assertEqual(self.h.sources.messages('key')[0]['role'],'context')

    def test_archive_restore_keeps_messages(self):
        self.h.store.message(self.a['id'],'user','keep')
        self.h.archive(self.a['id'])
        self.assertNotIn(self.a['id'],[a['id'] for a in self.h.store.agents()])
        self.h.restore(self.a['id'])
        self.assertEqual(self.h.export(self.a['id'])['messages'][0]['text'],'keep')

    def test_compatible_provider_stream_and_model(self):
        seen=[]
        class MockProvider(BaseHTTPRequestHandler):
            def log_message(self,*a):pass
            def do_POST(self):
                seen.append(json.loads(self.rfile.read(int(self.headers['Content-Length']))))
                self.send_response(200);self.send_header('Content-Type','text/event-stream');self.end_headers()
                self.wfile.write(b'data: {"model":"local-test","choices":[{"delta":{"content":"HELLO"}}]}\n\ndata: [DONE]\n\n')
        server=ThreadingHTTPServer(('127.0.0.1',0),MockProvider)
        t=threading.Thread(target=server.serve_forever,daemon=True);t.start()
        try:
            config={'id':'local-test','name':'local','baseUrl':f'http://127.0.0.1:{server.server_port}/v1','defaultModel':'local-test'}
            with patch.object(self.h,'extra_providers',return_value=[config]):
                a=self.h.store.create(provider='local-test');r=hub.Run(self.h,a,{'shared':'test'},'hello');r.execute()
                self.assertEqual(self.h.store.messages(a['id'])[0]['text'],'HELLO')
                self.assertEqual(seen[0]['model'],'local-test')
                self.assertEqual(self.h.store.agent(a['id'])['status'],'done')
        finally:server.shutdown();server.server_close()

class HttpTests(unittest.TestCase):
    def test_host_origin_and_token_guards(self):
        import server
        tmp=tempfile.TemporaryDirectory();instance=hub.Hub(tmp.name)
        http=ThreadingHTTPServer(('127.0.0.1',0),server.H)
        with patch.object(server,'PORT',http.server_port),patch.object(hub,'_hub',instance):
            threading.Thread(target=http.serve_forever,daemon=True).start()
            base=f'http://127.0.0.1:{http.server_port}'
            with urllib.request.urlopen(base+'/api/hub/bootstrap') as r:self.assertEqual(r.status,200)
            for headers in ({'Host':'evil.example'},{'Origin':'https://evil.example'},{'Sec-Fetch-Site':'cross-site'}):
                with self.assertRaises(urllib.error.HTTPError) as cm:urllib.request.urlopen(urllib.request.Request(base+'/api/hub/bootstrap',headers=headers))
                self.assertEqual(cm.exception.code,403)
            request=urllib.request.Request(base+'/api/hub/shared',data=b'{"text":"x"}',headers={'Content-Type':'application/json'})
            with self.assertRaises(urllib.error.HTTPError) as cm:urllib.request.urlopen(request)
            self.assertEqual(cm.exception.code,403)
            request.add_header('X-PitCrew-Token',instance.token)
            with urllib.request.urlopen(request) as r:self.assertEqual(r.status,200)
            self.assertEqual(instance.store.get('shared'),'x')
            http.shutdown();http.server_close()
        instance.store.db.close();tmp.cleanup()

if __name__=='__main__':unittest.main(verbosity=2)
