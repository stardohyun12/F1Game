import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import urllib.error
from email.message import Message
import hub
from usage import AccountUsage,claude_windows,codex_windows

class WorkspaceTests(unittest.TestCase):
    def test_projects_keep_rules_agents_and_sources_separate_and_persist(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);one=root/'one';two=root/'한글 two';one.mkdir();two.mkdir()
            (one/'AGENTS.md').write_text('Only one',encoding='utf-8')
            (two/'AGENTS.md').write_text('Only two',encoding='utf-8')
            h=hub.Hub(root/'data',root=one)
            p=h.add_project(dict(path=str(two),name='Second'))
            self.assertEqual(h.add_project(dict(path=str(two)))['id'],p['id'])
            child=h.project(p['id']);a=child.store.agents()[0]
            child.store.put('shared','Second only');child.store.message(a['id'],'user','Second history')
            self.assertEqual(child.root,two.resolve());self.assertEqual(child.sources.root,two.resolve())
            self.assertIs(child.lock,h.lock)
            self.assertEqual(child.token,h.token)
            self.assertEqual(child.context(a,[])['projectRules'][0]['text'],'Only two')
            self.assertEqual(h.context(h.store.agents()[0],[])['projectRules'][0]['text'],'Only one')
            self.assertEqual(h.store.get('shared',''),'')
            with self.assertRaises(ValueError):h.detail(a['id'])
            self.assertNotEqual(child.sources.claude_directory(),h.sources.claude_directory())
            child.store.db.close();h.store.db.close()
            reopened=hub.Hub(root/'data',root=one);again=reopened.project(p['id'])
            self.assertEqual(again.store.messages(a['id'])[0]['text'],'Second history')
            again.store.db.close();reopened.store.db.close()

    def test_codex_run_uses_registered_project_directory(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);project=root/'project';project.mkdir();h=hub.Hub(root/'data',root=project)
            a=h.store.create(provider='codex');calls=[]
            class Rpc:
                def __init__(self):
                    import queue
                    self.events=queue.Queue();self.events.put(dict(method='turn/completed',params=dict(turn=dict(status='completed'))))
                def call(self,method,params):
                    calls.append((method,params))
                    return dict(thread=dict(id='test'),model='test') if method=='thread/start' else dict(turn=dict(id='turn'))
                def close(self):pass
            with patch.object(hub,'Rpc',Rpc):hub.Run(h,a,{},'hello').execute()
            self.assertEqual(calls[0][1]['cwd'],str(project.resolve()))
            h.store.db.close()

    def test_usage_normalizes_missing_and_multiple_windows(self):
        rows=codex_windows(dict(rateLimitsByLimitId=dict(codex=dict(primary=dict(usedPercent=20,windowDurationMins=10080)),other=dict(primary=dict(usedPercent=None)))))
        self.assertEqual(rows[0]['usedPercent'],20);self.assertIsNone(rows[1]['usedPercent'])
        rows=claude_windows(dict(five_hour=dict(utilization=0,resets_at='2026-10-14T00:00:00Z'),seven_day=dict(utilization=63)))
        self.assertEqual([r['usedPercent'] for r in rows],[0,63]);self.assertIsNotNone(rows[0]['resetsAt'])

    def test_claude_run_uses_registered_project_directory(self):
        from claude_agent_sdk import ResultMessage
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);project=root/'claude-project';project.mkdir();h=hub.Hub(root/'data',root=project)
            a=h.store.create(provider='claude');seen=[]
            class Client:
                def __init__(self,options):seen.append(options)
                async def __aenter__(self):return self
                async def __aexit__(self,*args):pass
                async def query(self,prompt):pass
                async def receive_response(self):
                    yield ResultMessage(subtype='success',duration_ms=1,duration_api_ms=1,is_error=False,num_turns=1,session_id='test',total_cost_usd=0,usage={},result='OK')
            with patch('claude_agent_sdk.ClaudeSDKClient',Client),patch.object(h,'models',return_value={'models':[]}):
                hub.Run(h,a,{},'hello').execute()
            self.assertEqual(seen[0].cwd,str(project.resolve()))
            self.assertEqual(h.store.agent(a['id'])['status'],'done')
            h.store.db.close()

    def test_usage_rate_limit_honors_retry_after_and_keeps_previous(self):
        with tempfile.TemporaryDirectory() as temp:
            store=hub.Store(temp);usage=AccountUsage(store)
            usage.cache['claude']=dict(windows=[dict(usedPercent=12)],lastSuccessAt=90)
            headers=Message();headers['Retry-After']='3037'
            err=urllib.error.HTTPError('https://api.anthropic.com/api/oauth/usage',429,'limited',headers,None)
            class Opener:
                def open(self,*a,**k):raise err
            with patch('usage.Path.read_text',return_value=json.dumps(dict(claudeAiOauth=dict(accessToken='test-token')))),patch('usage.urllib.request.build_opener',return_value=Opener()),patch('usage.time.time',return_value=100):
                usage.refresh('claude')
            result=usage.cache['claude'];self.assertEqual(result['retryAt'],3137)
            self.assertEqual(result['windows'][0]['usedPercent'],12);self.assertTrue(result['stale'])
            self.assertNotIn('test-token',json.dumps(result));store.db.close()

if __name__=='__main__':unittest.main(verbosity=2)
