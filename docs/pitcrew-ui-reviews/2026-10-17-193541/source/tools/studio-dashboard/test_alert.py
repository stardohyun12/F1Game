"""Windows alert decision: only when no browser can show the event, once per run/kind."""
import os
import tempfile
import time
import unittest
from unittest.mock import patch

import hub

class AlertTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.h = hub.Hub(self.tmp.name)
        self.a = self.h.store.update(self.h.store.agents()[0]['id'], runStartedAt=1.0, lastTask='빌드')
        self.env = patch.dict(os.environ, {'PITCREW_WINDOWS_NOTIFY': '1'}); self.env.start()
        self.pop = patch.object(hub.subprocess, 'Popen'); self.popen = self.pop.start()

    def tearDown(self):
        self.pop.stop(); self.env.stop(); self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def calls(self): return self.popen.call_count

    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_no_browser_alerts_once_per_run_and_kind(self):
        self.h.alert(self.a['id'], 'done'); self.h.alert(self.a['id'], 'done')
        self.assertEqual(self.calls(), 1)
        args = self.popen.call_args[0][0]
        self.assertIn('작업 완료', ' '.join(args)); self.assertTrue(any('agent=' + self.a['id'] in x for x in args))
        self.h.store.update(self.a['id'], runStartedAt=2.0); self.h.last_done_alert = 0; self.h.alert(self.a['id'], 'done')  # next run, outside the 3-minute burst
        self.assertEqual(self.calls(), 2)

    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_visible_or_notifying_browser_covers_it(self):
        self.h.client_report(True, False); self.h.alert(self.a['id'], 'done'); self.assertEqual(self.calls(), 0)
        self.h.client_report(False, True); self.h.alert(self.a['id'], 'waiting'); self.assertEqual(self.calls(), 0)

    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_hidden_tab_without_permission_still_alerts(self):
        # Codex I: a browser that cannot notify must not suppress the Windows alert.
        self.h.client_report(False, False); self.h.alert(self.a['id'], 'error'); self.assertEqual(self.calls(), 1)

    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_stale_browser_report_does_not_cover(self):
        self.h.client_report(True, True); self.h.client['at'] = time.time() - 120
        self.h.alert(self.a['id'], 'done'); self.assertEqual(self.calls(), 1)

    def test_silent_outside_the_runtime_service(self):
        with patch.dict(os.environ, {}, clear=False):
            os.environ.pop('PITCREW_WINDOWS_NOTIFY', None); os.environ.pop('PITCREW_RUNTIME_SERVICE', None)
            self.h.alert(self.a['id'], 'done')
        self.assertEqual(self.calls(), 0)

    def test_isolated_runtime_is_silent_by_default(self):
        with patch.dict(os.environ, {'PITCREW_RUNTIME_DIR': self.tmp.name}):
            os.environ.pop('PITCREW_WINDOWS_NOTIFY', None)
            self.h.alert(self.a['id'], 'done')
        self.assertEqual(self.calls(), 0)

if __name__ == '__main__':
    unittest.main()
