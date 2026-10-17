import json
import os
import queue
import subprocess
import tempfile
import unittest
import urllib.error
import urllib.request
from unittest.mock import patch

import hub
from delegate_bridge import bridge


class CodexDelegateTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.h = hub.Hub(self.tmp.name)
        self.lead = self.h.store.create(name='lead', provider='codex')
        self.member = self.h.store.create(name='member', provider='claude')
        self.h.save_team({'name':'team', 'members':[self.lead['id'], self.member['id']], 'lead':self.lead['id']})
        self.run = hub.Run(self.h, self.lead, {}, 'task')

    def tearDown(self):
        self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def test_stdio_mcp_to_common_delegation_and_report_route(self):
        with bridge(self.run.codex_delegate) as config, patch.object(self.h,'send') as send:
            requests = [{'id':1,'method':'initialize','params':{'protocolVersion':'2024-11-05'}},
                        {'method':'notifications/initialized'}, {'id':2,'method':'tools/list'},
                        {'id':3,'method':'tools/call','params':{'name':'delegate','arguments':{'member':'member','task':'check'}}}]
            p = subprocess.run([config['command'],*config['args']], input='\n'.join(map(json.dumps,requests))+'\n',
                text=True, encoding='utf-8', capture_output=True, env={**os.environ,**config['env']}, timeout=10,
                creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
            self.assertEqual(p.returncode,0,p.stderr)
            replies = [json.loads(line) for line in p.stdout.splitlines()]
            self.assertEqual(len(replies),3)
            self.assertEqual(replies[1]['result']['tools'][0]['name'],'delegate')
            self.assertNotIn('isError',replies[2]['result'])
            aid,data = send.call_args.args
            self.assertEqual(aid,self.member['id'])
            self.assertEqual(data['replyTo']['agent'],self.lead['id'])
            self.assertEqual(data['origin']['delegation'],data['replyTo']['delegation'])

    def test_auth_and_malformed_arguments_do_not_delegate(self):
        with bridge(self.run.codex_delegate) as config, patch.object(self.h,'send') as send:
            url=config['env']['PITCREW_DELEGATE_URL']
            with self.assertRaises(urllib.error.HTTPError) as err:
                urllib.request.urlopen(urllib.request.Request(url,data=b'{}'),timeout=2)
            self.assertEqual(err.exception.code,403)
            req=urllib.request.Request(url,data=b'{"member": []}',headers={'Authorization':'Bearer '+config['env']['PITCREW_DELEGATE_TOKEN']})
            with urllib.request.urlopen(req,timeout=2) as response:self.assertTrue(json.load(response)['isError'])
            send.assert_not_called()

    def test_limits_cancellation_and_membership(self):
        with patch.object(self.h,'send') as send:
            with self.assertRaises(ValueError): self.run.codex_delegate({'member':'outsider','task':'x'})
            self.assertEqual(getattr(self.run,'delegated',0),0)
            self.run.delegated=8
            with self.assertRaises(ValueError): self.run.codex_delegate({'member':'member','task':'x'})
            self.run.delegated=0; self.run.cancel.set()
            with self.assertRaises(ValueError): self.run.codex_delegate({'member':'member','task':'x'})
            send.assert_not_called()

    def test_start_and_resume_receive_mcp_config(self):
        for session in ('','saved-thread'):
            with self.subTest(session=session),patch.object(hub,'Rpc') as rpc:
                self.run.a['session']=session
                rpc.return_value.call.side_effect=[{'thread':{'id':'saved-thread'}},{'turn':{'id':'turn'}}]
                rpc.return_value.events=queue.Queue()
                for rid,server in [(7,'pitcrew'),(8,'unrelated')]:
                    rpc.return_value.events.put({'id':rid,'method':'mcpServer/elicitation/request','params':{'serverName':server,'_meta':{'codex_approval_kind':'mcp_tool_call'}}})
                rpc.return_value.events.put({'method':'turn/completed','params':{'threadId':'saved-thread','turn':{'status':'completed'}}})
                with patch.object(self.h,'codex_skills',return_value=[]):self.run.codex()
                method,params=rpc.return_value.call.call_args_list[0].args
                self.assertEqual(method,'thread/resume' if session else 'thread/start')
                self.assertIn('mcp_servers.pitcrew',params['config'])
                self.assertIn('delegate',self.h.team_brief(self.run.a)['how'])
                replies={c.args[0]['id']:c.args[0]['result'] for c in rpc.return_value.write.call_args_list}
                self.assertEqual(replies[7]['action'],'accept')
                self.assertEqual(replies[8]['action'],'decline')


if __name__=='__main__':unittest.main()
