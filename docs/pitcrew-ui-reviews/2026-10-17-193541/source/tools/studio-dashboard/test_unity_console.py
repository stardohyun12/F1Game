import json
import threading
import unittest
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from unittest.mock import patch
import unity_console as C
from test_unity_status import Base, make_project


class Editor:
    def __init__(self, project, entries=None, wrong=False):
        self.hits = []
        self.failure = None
        self.entries = entries if entries is not None else ['[Error] example', '[Warning] example']
        owner = self
        class Handler(BaseHTTPRequestHandler):
            def log_message(self, *args): pass
            def do_POST(self):
                body = json.loads(self.rfile.read(int(self.headers['Content-Length'])))
                owner.hits.append(body)
                data_path = str(project / ('Wrong' if wrong else 'Assets')).replace('\\', '/')
                data = (dict(dataPath=data_path, scene='Assets/Main.unity', playing=False, compiling=False)
                        if body == json.loads(C.U.PROBE_BODY) else
                        dict(dataPath=data_path, entries=owner.entries, order='newest-first',
                             totalRows=len(owner.entries)+(1 if len(owner.entries)>=50 else 0), scannedRows=len(owner.entries),
                             hasEarlier=len(owner.entries)>=50, scanLimited=False, textTruncated=False))
                result = (dict(success=False, message=owner.failure)
                          if owner.failure and body != json.loads(C.U.PROBE_BODY)
                          else dict(success=True, data=data))
                payload = json.dumps(result).encode()
                self.send_response(200); self.send_header('Content-Length', str(len(payload)))
                self.end_headers(); self.wfile.write(payload)
        self.server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
        self.port = self.server.server_address[1]
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True); self.thread.start()

    def close(self):
        self.server.shutdown(); self.server.server_close(); self.thread.join()


class ConsoleTests(Base):
    def setUp(self):
        super().setUp(); C._cache.clear(); self.project = make_project(self.root / 'Game')

    def editor(self, **kwargs):
        editor = Editor(self.project, **kwargs); self.fakes.append(editor)
        self.heartbeat(self.project, editor.port)
        return editor

    def test_identity_then_fixed_read_without_clear_and_cache(self):
        editor = self.editor()
        result = C.snapshot(self.root, str(self.project))
        self.assertEqual(result['entries'], editor.entries)
        self.assertFalse(result['atLimit']); self.assertFalse(result['textTruncated'])
        self.assertEqual(result['project']['scene'], 'Assets/Main.unity')
        self.assertGreaterEqual(result['observedAt'], result['project']['observedAt'])
        self.assertEqual(editor.hits, [json.loads(C.U.PROBE_BODY), json.loads(C.CONSOLE_BODY)])
        self.assertEqual(editor.hits[1]['params'], dict(code=C.CONSOLE_CODE))
        self.assertEqual(result['order'], 'newest-first')
        self.assertEqual(C.snapshot(self.root, str(self.project)), result)
        self.assertEqual(len(editor.hits), 2)

    def test_wrong_editor_identity_never_reads_console(self):
        editor = self.editor(wrong=True)
        with self.assertRaises(ValueError): C.snapshot(self.root, str(self.project))
        self.assertEqual([h['command'] for h in editor.hits], ['exec'])

    def test_stale_heartbeat_never_contacts_editor(self):
        editor = self.editor(); self.heartbeat(self.project, editor.port, age=60)
        with self.assertRaises(ValueError): C.snapshot(self.root, str(self.project))
        self.assertEqual(editor.hits, [])

    def test_outside_workspace_rejected_before_network(self):
        outside = make_project(self.root.parent / 'Other')
        with patch.object(C.U, '_request_editor', side_effect=AssertionError('must not contact')):
            with self.assertRaises(ValueError): C.snapshot(self.root, str(outside))

    def test_bad_entries_and_too_many_rejected(self):
        editor = self.editor()
        for entries in ({'logs': []}, [None], ['log'] * 51):
            with self.subTest(entries=type(entries).__name__):
                editor.entries = entries
                with self.assertRaises(ValueError): C.snapshot(self.root, str(self.project))
        self.assertFalse(C._cache)

    def test_limits_explicit_and_empty_console_not_a_pass(self):
        editor = self.editor(entries=['x' * 4100] + ['log'] * 49)
        result = C.snapshot(self.root, str(self.project))
        self.assertTrue(result['atLimit']); self.assertTrue(result['textTruncated'])
        self.assertEqual(len(result['entries'][0]), 4000)
        C._cache.clear(); editor.entries = []
        result = C.snapshot(self.root, str(self.project))
        self.assertEqual(result['entries'], []); self.assertNotIn('passed', result)

    def test_busy_request_fails_without_network(self):
        editor = self.editor()
        with C._lock:
            with self.assertRaises(ValueError): C.snapshot(self.root, str(self.project))
        self.assertEqual(editor.hits, [])

    def test_normal_stack_traces_above_status_limit_and_console_cap(self):
        stack = 'NullReferenceException\n' + '  at Game.Hud.Update in Assets/Hud.cs:42\n' * 45
        editor = self.editor(entries=[stack] * 50)
        self.assertGreater(len(json.dumps(editor.entries).encode()), C.U.MAX_RESPONSE_BYTES)
        self.assertEqual(len(C.snapshot(self.root, str(self.project))['entries']), 50)
        C._cache.clear(); editor.entries = ['x' * (C.MAX_CONSOLE_BYTES + 1)]
        with self.assertRaisesRegex(ValueError, '1024KB'): C.snapshot(self.root, str(self.project))

    def test_connector_failure_cause_survives_transport_and_retry(self):
        editor = self.editor()
        editor.failure = 'Runtime error: InvalidOperationException: Console read/restore failed: IOException: console read denied'
        with self.assertRaisesRegex(ValueError, 'IOException: console read denied'):
            C.snapshot(self.root, str(self.project))
        self.assertFalse(C._cache)
        editor.failure = None
        self.assertEqual(C.snapshot(self.root, str(self.project))['entries'], editor.entries)
        self.assertEqual(len(editor.hits), 4)

    def test_restore_warning_and_long_dual_failure_are_visible(self):
        editor = self.editor()
        editor.failure = ('Runtime error: InvalidOperationException: Console settings may not be restored. '
                          'Search: ' + 'x' * 230 + ' | Read: original read failure')
        with self.assertRaises(ValueError) as error:
            C.snapshot(self.root, str(self.project))
        message = str(error.exception)
        self.assertIn('콘솔 조회', message)
        self.assertIn('콘솔 표시 설정을 되돌리지 못했을 수 있습니다', message)
        self.assertIn('original read failure', message)
        self.assertNotIn('잘림', message)
        editor.failure = 'x' * 3000
        with self.assertRaises(ValueError) as error:
            C.snapshot(self.root, str(self.project))
        self.assertIn('[잘림]', str(error.exception))
        self.assertLess(len(str(error.exception)), 2150)
        self.assertFalse(C._cache)


if __name__ == '__main__': unittest.main()
