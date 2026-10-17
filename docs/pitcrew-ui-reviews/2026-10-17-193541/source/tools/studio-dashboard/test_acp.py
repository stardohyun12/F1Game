"""ACP adapter against a local mock agent: streaming, tools, plan, permission, images, resume, cancel."""
import base64, json, os, sys, tempfile, threading, time, unittest
from pathlib import Path
from unittest.mock import patch

import hub

MOCK = {'id': 'acp-mock', 'name': 'Mock ACP', 'type': 'acp', 'command': [sys.executable, str(Path(__file__).with_name('acp_mock_agent.py'))], 'models': []}

class AcpTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.h = hub.Hub(self.tmp.name)
        # Protocol fixtures must not launch desktop notification processes.
        self.alert_patch = patch.object(self.h, 'alert'); self.alert_patch.start()
        self.addCleanup(self.alert_patch.stop)
        self.p = patch.object(self.h, 'extra_providers', return_value=[MOCK]); self.p.start()
        self.a = self.h.store.create(name='ACP', provider='acp-mock', mode='work')

    def tearDown(self):
        self.p.stop(); self.h.settle(10)
        self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def turn(self, text, files=None, answer=None, cancel_after=None):
        self.h.send(self.a['id'], {'text': text, **({'files': files} if files else {})})
        t = time.time(); stopped = False
        while time.time() - t < 20:
            time.sleep(.1)
            if answer is not None and self.h.approvals:
                rid = next(iter(self.h.approvals)); self.h.decide(rid, {'allow': answer})
            if cancel_after and not stopped and time.time() - t > cancel_after and self.a['id'] in self.h.runs:
                self.h.stop(self.a['id']); stopped = True
            if self.a['id'] not in self.h.runs: break
        time.sleep(.2)
        return self.h.store.agent(self.a['id']), [m for m in self.h.store.messages(self.a['id']) if m['role'] != 'user']

    def reply(self, msgs): return [m['text'] for m in msgs if m['role'] == 'assistant'][-1]

    def test_stream_tools_plan_and_resume(self):
        a, msgs = self.turn('hello')
        self.assertEqual(a['status'], 'done'); self.assertTrue(a['session'].startswith('sess_'))
        self.assertIn('TURN:1', self.reply(msgs)); self.assertIn('DONE', self.reply(msgs))
        tool = next(m for m in msgs if m.get('title') == 'Read README')
        self.assertEqual((tool['status'], tool['label'], tool['text']), ('done', 'README.md', 'README contents'))
        self.assertTrue(any(m.get('title') == '계획' and '1/2' in m.get('label', '') for m in msgs))
        a2, msgs2 = self.turn('again')
        self.assertEqual(a2['session'], a['session'])  # same session via session/load
        self.assertNotIn('REPLAYED_OLD_TEXT', ''.join(m['text'] for m in msgs2 if m['role'] == 'assistant'))

    def test_permission_allow_and_deny(self):
        _, msgs = self.turn('ASK please', answer=True); self.assertIn('PERMISSION:ok', self.reply(msgs))
        _, msgs = self.turn('ASK again', answer=False); self.assertIn('PERMISSION:no', self.reply(msgs))

    def test_images_are_sent_as_image_blocks(self):
        img = Path(self.tmp.name) / 'x.png'; img.write_bytes(base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/58BAwAI/AL+hc2rNAAAAABJRU5ErkJggg=='))
        _, msgs = self.turn('look', files=[{'name': 'x.png', 'path': str(img)}]); self.assertIn('IMAGES:1', self.reply(msgs))

    def test_fast_finish_keeps_every_chunk(self):
        # Codex review RC3: the final response can arrive before the last updates are consumed.
        for i in range(5):
            _, msgs = self.turn(f'quick {i}')
            self.assertIn('DONE', self.reply(msgs)); self.assertIn('IMAGES:0', self.reply(msgs))

    def test_tool_image_survives_status_only_update(self):
        with patch.object(hub, 'MEDIA', Path(self.tmp.name) / 'media'):
            _, msgs = self.turn('RETURN_IMAGE')
            m = next(m for m in msgs if m.get('title') == 'Read README')
            self.assertEqual(m['text'], 'Game capture')
            self.assertEqual(m['status'], 'done')
            self.assertTrue(m['output'])
            self.assertEqual(len(m.get('images', [])), 1)
            self.assertTrue((hub.MEDIA / m['images'][0]).read_bytes().startswith(b'\x89PNG'))
            self.assertEqual(self.h.artifacts(self.a['id'])['images'][0]['src'], '/media/' + m['images'][0])

    def test_add_cli_handshake_then_saves(self):
        self.p.stop()
        cfg = Path(self.tmp.name) / 'providers.json'
        with patch.dict(os.environ, {'PITCREW_PROVIDERS_FILE': str(cfg)}):
            cmd = f'"{sys.executable}" "{Path(__file__).with_name("acp_mock_agent.py")}"'
            info = self.h.add_cli({'command': cmd, 'check': True})
            self.assertEqual((info['name'], info['image'], info['loadSession']), ('Mock ACP', True, True)); self.assertFalse(cfg.exists())
            r = self.h.add_cli({'command': cmd, 'name': '내 CLI'})
            saved = json.loads(cfg.read_text(encoding='utf-8'))['providers'][0]
            self.assertEqual((saved['type'], saved['name'], r['id']), ('acp', '내 CLI', saved['id']))
            self.assertTrue(any(p['id'] == r['id'] and p.get('protocol') == 'acp' and p['ready'] for p in self.h.providers()))
            with self.assertRaises(ValueError): self.h.add_cli({'command': 'definitely-not-installed-cli --acp'})
            # Codex review RC8: Korean names and repeats get their own ids; nothing is overwritten.
            r2 = self.h.add_cli({'command': cmd, 'name': '검토 담당'}); r3 = self.h.add_cli({'command': cmd, 'name': '조사 담당'}); r4 = self.h.add_cli({'command': cmd, 'name': '내 CLI'})
            ids = [p['id'] for p in json.loads(cfg.read_text(encoding='utf-8'))['providers']]
            self.assertEqual(len(ids), 4); self.assertEqual(len(set(ids)), 4); self.assertNotEqual(r2['id'], r3['id']); self.assertNotEqual(r4['id'], r['id'])
        self.p.start()

    def test_cancel(self):
        a, _ = self.turn('SLOW', cancel_after=1.0)
        self.assertEqual(a['status'], 'interrupted')

    def test_auth_error_gets_a_hint(self):
        cfg = dict(MOCK, command=[sys.executable, '-c', "import sys,json\nfor l in sys.stdin:\n m=json.loads(l)\n r={'protocolVersion':1,'agentCapabilities':{}} if m['method']=='initialize' else None\n print(json.dumps({'jsonrpc':'2.0','id':m['id'],**({'result':r} if r else {'error':{'code':-32000,'message':'Gemini API key is missing or not configured.'}})}),flush=True)"],
                   authHint='HINT_LOGIN')
        with patch.object(self.h, 'extra_providers', return_value=[cfg]):
            a, msgs = self.turn('hi')
        self.assertEqual(a['status'], 'error'); self.assertIn('HINT_LOGIN', a['error'])

if __name__ == '__main__':
    unittest.main()
