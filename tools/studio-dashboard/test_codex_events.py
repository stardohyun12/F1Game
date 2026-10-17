"""Codex event handling through the real Run.codex path with a fake app-server (approach from Codex's review harness)."""
import queue, tempfile, unittest
from pathlib import Path
from unittest.mock import patch

import hub

def fake_rpc(events):
    class FakeRpc:
        def __init__(self):
            self.events = queue.Queue()
            for e in events: self.events.put(e)
            self.events.put({'method': 'turn/completed', 'params': {'threadId': 't', 'turn': {'id': 'turn', 'status': 'completed'}}})
        def call(self, method, params): return {'thread': {'id': 't'}} if method == 'thread/start' else {'turn': {'id': 'turn'}}
        def write(self, m): pass
        def close(self): pass
    return FakeRpc

def usage(total, last):
    return {'method': 'thread/tokenUsage/updated', 'params': {'threadId': 't', 'turnId': 'turn', 'tokenUsage': {
        'total': {'totalTokens': total + total // 10, 'inputTokens': total, 'outputTokens': total // 10}, 'last': {'inputTokens': last, 'outputTokens': last // 10}}}}

def plan(steps):
    return {'method': 'turn/plan/updated', 'params': {'threadId': 't', 'turnId': 'turn', 'plan': [{'step': s, 'status': st} for s, st in steps]}}

class CodexEventTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.h = hub.Hub(self.tmp.name); self.h.root = Path(self.tmp.name)
        self.a = self.h.store.create(name='Codex', provider='codex')

    def tearDown(self):
        self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def run_events(self, events):
        r = hub.Run(self.h, self.a, {}, 'x')
        with patch.object(hub, 'Rpc', fake_rpc(events)), patch.object(self.h, 'after_run'): r.execute()
        return self.h.store.messages(self.a['id'])

    def test_plan_updates_one_live_checklist(self):
        msgs = self.run_events([plan([('읽기', 'completed'), ('구현', 'inProgress'), ('검증', 'pending')]),
                                plan([('읽기', 'completed'), ('구현', 'completed'), ('검증', 'completed')]),
                                {'method': 'item/agentMessage/delta', 'params': {'threadId': 't', 'itemId': 'm', 'delta': 'done'}}])
        plans = [m for m in msgs if m.get('plan')]
        self.assertEqual(len(plans), 1)  # updated in place, not appended
        self.assertEqual([s['status'] for s in plans[0]['plan']], ['completed'] * 3)
        self.assertEqual((plans[0]['label'], plans[0]['status']), ('3/3 완료', 'done'))

    def test_other_turn_completion_does_not_drop_current_answer(self):
        msgs=self.run_events([
            {'method':'turn/completed','params':{'threadId':'t','turn':{'id':'other','status':'completed'}}},
            {'method':'thread/goal/updated','params':{'threadId':'t','goal':{'status':'paused','objective':'checked'}}},
            {'method':'item/agentMessage/delta','params':{'threadId':'t','turnId':'turn','itemId':'answer','delta':'USER ANSWER'}}])
        self.assertTrue(any(m['text']=='USER ANSWER' for m in msgs))
        self.assertEqual(self.h.store.agent(self.a['id'])['goal']['status'],'paused')

    def test_turn_and_item_start_times_are_kept(self):
        # Codex schema v2 (codex-cli 0.154): turn/started.turn.startedAt (s) and item/started.startedAtMs (ms)
        msgs = self.run_events([
            {'method': 'turn/started', 'params': {'threadId': 't', 'turn': {'id': 'turn', 'status': 'inProgress', 'startedAt': 1700000000}}},
            {'method': 'item/started', 'params': {'threadId': 't', 'turnId': 'turn', 'startedAtMs': 1700000005000,
                                                  'item': {'id': 'c1', 'type': 'commandExecution', 'command': 'pytest', 'status': 'inProgress'}}},
            {'method': 'item/agentMessage/delta', 'params': {'threadId': 't', 'itemId': 'm', 'delta': 'done'}}])
        self.assertEqual(self.h.store.agent(self.a['id'])['turnStartedAt'], 1700000000)
        tool = next(m for m in msgs if m['role'] == 'tool')
        self.assertEqual(tool['startedAt'], 1700000005.0)

    def test_turn_usage_sums_requests_and_ignores_repeats(self):
        msgs = self.run_events([usage(100, 100), usage(100, 100), usage(300, 200), usage(300, 200),
                                {'method': 'item/agentMessage/delta', 'params': {'threadId': 't', 'itemId': 'm', 'delta': 'FINAL'}}])
        u = [m for m in msgs if m['role'] == 'assistant'][-1]['turnUsage']
        self.assertEqual((u['input'], u['output'], u['requests']), (300, 30, 2))

if __name__ == '__main__':
    unittest.main()
