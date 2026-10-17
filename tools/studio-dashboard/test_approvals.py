"""Approval summary and 'keep allowing in this conversation'."""
import tempfile
import threading
import time
import unittest

import hub

class ApprovalTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.h = hub.Hub(self.tmp.name)
        self.a = self.h.store.agents()[0]

    def tearDown(self):
        self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def ask(self, detail):
        out = {}
        t = threading.Thread(target=lambda: out.update(self.h.request(self.a['id'], 'approval', detail))); t.start()
        deadline = time.time() + 3
        while not self.h.approvals and time.time() < deadline: time.sleep(.02)
        return t, out

    def test_remember_then_auto_allow_same_tool_only(self):
        t, out = self.ask({'tool': 'Bash', 'input': {'command': 'npm test', 'description': 'Run tests'}})
        rid, item = next(iter(self.h.approvals.items()))
        self.assertEqual(item['detail']['summary']['action'], '명령 실행'); self.assertEqual(item['detail']['summary']['target'], 'npm test')
        self.h.decide(rid, {'allow': True, 'remember': True}); t.join(2)
        self.assertTrue(out['allow']); self.assertEqual(self.h.store.agent(self.a['id'])['autoAllow'], ['tool:Bash'])
        # Same tool: answered without asking.
        self.assertTrue(self.h.request(self.a['id'], 'approval', {'tool': 'Bash', 'input': {'command': 'ls'}})['allow'])
        self.assertEqual(self.h.approvals, {})
        # Different tool still asks.
        t2, out2 = self.ask({'tool': 'Write', 'input': {'file_path': 'a.txt'}})
        self.assertEqual(len(self.h.approvals), 1); rid2 = next(iter(self.h.approvals))
        self.h.decide(rid2, {'allow': False, 'remember': True}); t2.join(2)
        self.assertFalse(out2['allow']); self.assertEqual(self.h.store.agent(self.a['id'])['autoAllow'], ['tool:Bash'])  # deny never remembers

    def test_revoke_and_questions_never_auto(self):
        self.h.store.update(self.a['id'], autoAllow=['tool:Bash', 'codex:commandExecution'])
        self.assertTrue(self.h.request(self.a['id'], 'approval', {'method': 'item/commandExecution/requestApproval', 'command': 'ls'})['allow'])
        self.h.revoke_allow({'agent': self.a['id'], 'key': 'tool:Bash'})
        self.assertEqual(self.h.store.agent(self.a['id'])['autoAllow'], ['codex:commandExecution'])
        t, out = self.ask({'tool': 'Bash', 'input': {'command': 'ls'}}); self.assertEqual(len(self.h.approvals), 1)
        self.h.decide(next(iter(self.h.approvals)), {'allow': False}); t.join(2)

    def test_codex_read_mode_never_stalls_on_approval(self):
        # Seen in a real chain: read-mode Codex asked to approve `Get-Content`; the read-only sandbox already blocks writes.
        self.assertEqual(hub.codex_permissions('read'), {'approvalPolicy': 'never', 'sandbox': 'read-only'})
        self.assertEqual(hub.codex_permissions('work')['approvalPolicy'], 'untrusted')

    def test_codex_file_change_summary_lists_paths(self):
        s = hub.Hub.approval_summary({'method': 'item/fileChange/requestApproval', 'proposal': {'content': hub.dumps({'changes': [{'path': 'web/a.js'}, {'path': 'hub.py'}]})}})
        self.assertEqual(s['action'], '파일 변경'); self.assertEqual(s['target'], 'web/a.js\nhub.py')

if __name__ == '__main__':
    unittest.main()
