"""Research preset: reuses light agents, allows only web tools, hands results back. No AI calls (send is captured)."""
import tempfile, unittest
from unittest.mock import patch

import hub

class ResearchTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.h = hub.Hub(self.tmp.name)
        self.me = self.h.store.agents()[0]; self.sent = []
        self.p1 = patch.object(self.h, 'providers', return_value=[{'id': 'claude', 'ready': True}, {'id': 'codex', 'ready': True}])
        self.p2 = patch.object(hub.Hub, 'send', lambda h, aid, data: self.sent.append((aid, data)) or {'ok': True})
        self.p1.start(); self.p2.start()

    def tearDown(self):
        self.p2.stop(); self.p1.stop(); self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def test_creates_light_agents_with_narrow_permissions_and_handoff(self):
        r = self.h.research({'topic': 'Claude 입력창 구성', 'receiver': self.me['id']})
        self.assertEqual(len(r['agents']), 2)
        web, local = (self.h.store.agent(i) for i in r['agents'])
        self.assertEqual((web['provider'], web['model'], web['effort'], web['mode']), ('claude', 'haiku', 'low', 'read'))
        self.assertEqual(web['autoAllow'], ['tool:WebFetch', 'tool:WebSearch'])
        self.assertTrue(web.get('lean'))  # measured 64k -> 25k input tokens per fresh Haiku turn
        self.assertEqual(self.h.context(web, [])['projectRules'], [])
        self.assertEqual((local['provider'], local['mode']), ('codex', 'read'))
        for a in (web, local):
            self.assertFalse(a.get('handoffs'))  # per-request reply address instead of a standing rule (Codex RC5)
        self.assertEqual(len(self.sent), 2)
        aid, data = self.sent[0]
        self.assertEqual(data['replyTo']['agent'], self.me['id'])
        self.assertIn('사실과 출처만', data['text']); self.assertIn('Claude 입력창 구성', data['text']); self.assertTrue(data['queue'])
        self.assertEqual(data['origin']['agent'], self.me['id'])
        team = next(t for t in self.h.store.get('teams') if t.get('preset') == 'research'); self.assertEqual(set(team['members']), set(r['agents']))

    def test_second_request_reuses_agents(self):
        a = self.h.research({'topic': 'one', 'receiver': self.me['id'], 'kinds': ['web']})
        b = self.h.research({'topic': 'two', 'receiver': self.me['id'], 'kinds': ['web']})
        self.assertEqual(a['agents'], b['agents'])
        self.assertEqual(len([x for x in self.h.store.agents() if x.get('preset') == 'research-web']), 1)

    def test_lean_flag_is_saved_and_validated(self):
        a = self.h.save_agent({'id': self.me['id'], 'lean': True}); self.assertTrue(a['lean'])
        with self.assertRaises(ValueError): self.h.save_agent({'id': self.me['id'], 'lean': 'yes'})
        # Codex review RC10: a leftover lean flag must not strip project rules from a non-Claude agent.
        codex = self.h.store.update(self.me['id'], provider='codex', lean=True)
        self.assertEqual(self.h.context(codex, [])['projectRules'], self.h.context({**codex, 'lean': False}, [])['projectRules'])

    def test_validation(self):
        with self.assertRaises(ValueError): self.h.research({'topic': '  ', 'receiver': self.me['id']})
        with self.assertRaises(ValueError): self.h.research({'topic': 'x', 'kinds': ['nope']})
        with self.assertRaises(ValueError): self.h.research({'topic': 'x', 'receiver': self.me['id'], 'kinds': []})  # Codex RC4
        self.assertEqual(self.sent, [])

if __name__ == '__main__':
    unittest.main()
