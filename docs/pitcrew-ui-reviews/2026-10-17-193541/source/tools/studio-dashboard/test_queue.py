"""Queue and hand-off with a local mock provider: no AI usage, real Run threads."""
import json
import tempfile
import threading
import time
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from unittest.mock import patch

import hub

class Mock(BaseHTTPRequestHandler):
    fail = False
    def log_message(self, *a): pass
    def do_POST(self):
        body = json.loads(self.rfile.read(int(self.headers['Content-Length'])))
        prompt = body['messages'][-1]['content'].split('[사용자 메시지]\n')[-1]
        time.sleep(.4)
        if Mock.fail:
            self.send_response(500); self.end_headers(); return
        self.send_response(200); self.send_header('Content-Type', 'text/event-stream'); self.end_headers()
        reply = 'ECHO:' + prompt.splitlines()[0]
        self.wfile.write(('data: ' + json.dumps({'choices': [{'delta': {'content': reply}}]}) + '\n\ndata: [DONE]\n\n').encode())

class QueueTests(unittest.TestCase):
    def setUp(self):
        Mock.fail = False
        self.tmp = tempfile.TemporaryDirectory(); self.h = hub.Hub(self.tmp.name)
        # Queue tests must not launch desktop notifications from service environments.
        notices = patch.object(self.h, 'alert'); notices.start(); self.addCleanup(notices.stop)
        self.server = ThreadingHTTPServer(('127.0.0.1', 0), Mock)
        threading.Thread(target=self.server.serve_forever, daemon=True).start()
        cfg = {'id': 'mock', 'name': 'mock', 'baseUrl': f'http://127.0.0.1:{self.server.server_port}/v1', 'defaultModel': 'm'}
        self.p = patch.object(self.h, 'extra_providers', return_value=[cfg]); self.p.start()
        self.a = self.h.store.create(name='A', provider='mock'); self.b = self.h.store.create(name='B', provider='mock')

    def tearDown(self):
        self.p.stop(); self.server.shutdown(); self.server.server_close()
        self.h.settle(10)
        self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def wait_idle(self, *ids, timeout=8):
        deadline = time.time() + timeout
        while time.time() < deadline:
            time.sleep(.1)
            if not any(i in self.h.runs for i in ids) and all(not self.h.store.agent(i).get('queue') for i in ids):
                time.sleep(.3)
                if not any(i in self.h.runs for i in ids): return
        self.fail('timed out waiting for runs')

    def texts(self, aid, role):
        return [m['text'] for m in self.h.store.messages(aid) if m['role'] == role]

    def test_message_sent_while_running_is_queued_then_delivered(self):
        self.h.send(self.a['id'], {'text': 'first'})
        r = self.h.send(self.a['id'], {'text': 'second', 'queue': True})
        self.assertIn('queued', r)
        with self.assertRaises(ValueError): self.h.send(self.a['id'], {'text': 'third'})  # without queue flag: unchanged behaviour
        self.wait_idle(self.a['id'])
        self.assertEqual(self.texts(self.a['id'], 'user'), ['first', 'second'])
        self.assertEqual(self.texts(self.a['id'], 'assistant'), ['ECHO:first', 'ECHO:second'])

    def test_handoff_delivers_last_report_to_teammate(self):
        self.h.save_handoff({'from': self.a['id'], 'to': self.b['id'], 'text': 'review this'})
        self.h.send(self.a['id'], {'text': 'build'})
        self.wait_idle(self.a['id'], self.b['id'])
        got = self.texts(self.b['id'], 'user')
        self.assertEqual(len(got), 1); self.assertTrue(got[0].startswith('review this'))
        self.assertIn('ECHO:build', got[0]); self.assertIn('인계 · A', got[0])
        self.assertEqual(self.h.store.agent(self.a['id']).get('handoffs'), [])  # one-shot

    def test_handoff_waits_for_queue(self):
        self.h.save_handoff({'from': self.a['id'], 'to': self.b['id']})
        self.h.send(self.a['id'], {'text': 'one'}); self.h.send(self.a['id'], {'text': 'two', 'queue': True})
        self.wait_idle(self.a['id'], self.b['id'])
        got = self.texts(self.b['id'], 'user')
        self.assertEqual(len(got), 1); self.assertIn('ECHO:two', got[0]); self.assertNotIn('ECHO:one', got[0])

    def test_failed_run_holds_queue_and_handoff(self):
        Mock.fail = True
        self.h.save_handoff({'from': self.a['id'], 'to': self.b['id']})
        self.h.send(self.a['id'], {'text': 'x'}); self.h.send(self.a['id'], {'text': 'later', 'queue': True})
        deadline = time.time() + 6
        while (self.a['id'] in self.h.runs or self.h.store.agent(self.a['id'])['status'] == 'running') and time.time() < deadline: time.sleep(.1)
        time.sleep(.5)
        a = self.h.store.agent(self.a['id'])
        self.assertEqual(a['status'], 'error'); self.assertEqual([q['text'] for q in a['queue']], ['later'])
        self.assertEqual(len(a['handoffs']), 1); self.assertEqual(self.texts(self.b['id'], 'user'), [])
        Mock.fail = False; self.h.queue_action({'agent': self.a['id'], 'action': 'resume'})
        self.wait_idle(self.a['id'])
        self.assertEqual(self.texts(self.a['id'], 'user')[-1], 'later')

    def test_queued_message_keeps_teammate_mention(self):
        # Codex review RC2: @teammate reference must survive the queue.
        self.h.store.message(self.b['id'], 'assistant', 'B_FINDINGS_123')
        self.h.send(self.a['id'], {'text': 'first'}); self.h.send(self.a['id'], {'text': 'second', 'queue': True, 'mentions': [self.b['id']]})
        self.wait_idle(self.a['id'])
        second = [m for m in self.h.store.messages(self.a['id']) if m['role'] == 'user'][-1]
        self.assertEqual(second['text'], 'second')
        self.assertTrue(any(s['kind'] == 'mention' and 'B_FINDINGS_123' in s['text'] for s in second['context']['snapshots']))

    def test_each_request_reply_goes_to_its_own_requester(self):
        # Codex review RC5: two queued requests to one agent must not both receive the last report.
        c = self.h.store.create(name='C', provider='mock')
        self.h.send(self.a['id'], {'text': 'TOPIC_ONE', 'replyTo': {'agent': self.b['id'], 'text': 'R1'}})
        self.h.send(self.a['id'], {'text': 'TOPIC_TWO', 'queue': True, 'replyTo': {'agent': c['id'], 'text': 'R2'}})
        self.wait_idle(self.a['id'], self.b['id'], c['id'])
        got_b = self.texts(self.b['id'], 'user'); got_c = self.texts(c['id'], 'user')
        self.assertEqual(len(got_b), 1); self.assertEqual(len(got_c), 1)
        self.assertIn('ECHO:TOPIC_ONE', got_b[0]); self.assertNotIn('TOPIC_TWO', got_b[0])
        self.assertIn('ECHO:TOPIC_TWO', got_c[0]); self.assertNotIn('TOPIC_ONE', got_c[0])

    def test_settle_waits_for_post_run_work(self):
        # Codex review RC6: settle() must not report idle while after_run is still working.
        import threading
        gate = threading.Event(); real = self.h.after_run
        def slow(*a, **k): gate.wait(5); return real(*a, **k)
        with patch.object(self.h, 'after_run', slow):
            self.h.send(self.a['id'], {'text': 'x'})
            t = time.time()
            while self.a['id'] in self.h.runs and time.time() - t < 5: time.sleep(.05)
            self.assertFalse(self.h.settle(.3)); gate.set(); self.assertTrue(self.h.settle(5))

    def test_sender_timeline_shows_what_it_handed_off(self):
        self.h.save_handoff({'from': self.a['id'], 'to': self.b['id'], 'text': 'review'})
        self.h.send(self.a['id'], {'text': 'build'})
        self.wait_idle(self.a['id'], self.b['id'])
        events = [m for m in self.h.detail(self.a['id'])['messages'] if m['role'] == 'event']
        self.assertEqual([(e['kind'], e['to']) for e in events], [('handoff', self.b['id'])])
        received = [m for m in self.h.store.messages(self.b['id']) if m['role'] == 'user'][0]
        self.assertEqual(events[0]['messageId'], received['id'])
        roles = [m['role'] for m in self.h.detail(self.a['id'])['messages']]
        self.assertLess(roles.index('assistant'), roles.index('event'))  # placed after the report it carried

    def test_queue_remove_and_limits(self):
        self.h.send(self.a['id'], {'text': 'busy'})
        ids = [self.h.send(self.a['id'], {'text': f'q{i}', 'queue': True})['queued'] for i in range(10)]
        with self.assertRaises(ValueError): self.h.send(self.a['id'], {'text': 'q10', 'queue': True})
        self.h.queue_action({'agent': self.a['id'], 'action': 'clear'})
        self.assertEqual(self.h.store.agent(self.a['id'])['queue'], [])
        with self.assertRaises(ValueError): self.h.save_handoff({'from': self.a['id'], 'to': self.a['id']})
        self.wait_idle(self.a['id'])

if __name__ == '__main__':
    unittest.main()
