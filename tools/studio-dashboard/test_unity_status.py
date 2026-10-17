"""unity_status: project discovery bounds, heartbeat freshness, and the one fixed loopback query.
No real Unity editor: a fake connector (ThreadingHTTPServer) answers POST /command per test."""
import json
import os
import subprocess
import tempfile
import threading
import time
import unittest
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from unittest.mock import patch

import unity_status as U


class FakeEditor:
    """Records every request. mode: ok | wrong | slow | drip | redirect | huge | refuse-exec | notjson."""
    def __init__(self, project, mode='ok', scene='Assets/Scenes/Main.unity', playing=False, compiling=False):
        self.project, self.mode, self.hits = str(project).replace('\\', '/'), mode, []
        self.scene, self.playing, self.compiling = scene, playing, compiling
        self.delay = 1.5
        self.release = threading.Event()
        fake = self

        class H(BaseHTTPRequestHandler):
            def log_message(self, *a): pass

            def do_POST(self):
                n = int(self.headers.get('Content-Length', '0'))
                fake.hits.append(dict(path=self.path, body=self.rfile.read(n), origin=self.headers.get('Origin'), via=self.headers.get('Via')))
                if fake.mode == 'slow':
                    time.sleep(fake.delay)
                if fake.mode in ('stall-http10', 'stall-close', 'stall-headers'):
                    # Bytes then silence: no more data ever arrives, so only closing the socket can end a blocked read.
                    head = {'stall-http10': b'HTTP/1.0 200 OK\r\nContent-Length: 100\r\n\r\n',
                            'stall-close': b'HTTP/1.1 200 OK\r\nConnection: close\r\nContent-Length: 100\r\n\r\n',
                            'stall-headers': b'HTTP/1.1 200 OK\r\nX-Half: '}[fake.mode]
                    try:
                        self.wfile.write(head); self.wfile.flush()
                        # one more byte just before the 0.3 s budget: a per-recv socket timeout restarts here and
                        # would run past the deadline; only the deadline close ends the read on time
                        time.sleep(0.25); self.wfile.write(b' ' if fake.mode != 'stall-headers' else b'x'); self.wfile.flush()
                        fake.release.wait(10)
                    except OSError:
                        pass
                    return
                if fake.mode == 'drip-headers':  # status line, then a header that never ends (one byte every 0.02 s)
                    try:
                        self.wfile.write(b'HTTP/1.0 200 OK\r\nX-Slow: '); self.wfile.flush()
                        for _ in range(5000): self.wfile.write(b'a'); self.wfile.flush(); time.sleep(0.02)
                    except OSError:
                        pass
                    return
                if fake.mode == 'drip':  # headers at once, then one body byte every 0.2 s
                    try:
                        self.send_response(200); self.send_header('Content-Length', '4000'); self.end_headers()
                        for _ in range(4000): self.wfile.write(b' '); self.wfile.flush(); time.sleep(0.2)
                    except OSError:
                        pass
                    return
                if fake.mode == 'redirect':
                    self.send_response(302); self.send_header('Location', fake.redirect_to); self.send_header('Content-Length', '0'); self.end_headers(); return
                if fake.mode == 'huge':
                    body = b'{"success":true,"data":"' + b'x' * (U.MAX_RESPONSE_BYTES + 10) + b'"}'
                elif fake.mode == 'notjson':
                    body = b'<html>hello</html>'
                elif fake.mode == 'refuse-exec':
                    body = json.dumps({'success': False, 'message': 'Compile error: nope'}).encode()
                else:
                    path = 'C:/Other/Project' if fake.mode == 'wrong' else fake.project
                    body = json.dumps({'success': True, 'message': 'OK', 'data': {
                        'dataPath': path + '/Assets', 'playing': fake.playing, 'compiling': fake.compiling, 'scene': fake.scene}}).encode()
                try:
                    self.send_response(200); self.send_header('Content-Length', str(len(body))); self.end_headers(); self.wfile.write(body)
                except OSError:
                    pass

        self.server = ThreadingHTTPServer(('127.0.0.1', 0), H)
        self.port = self.server.server_address[1]
        threading.Thread(target=self.server.serve_forever, daemon=True).start()

    def close(self):
        self.release.set(); self.server.shutdown(); self.server.server_close()


def make_project(path, version='6000.3.19f1'):
    (path / 'ProjectSettings').mkdir(parents=True, exist_ok=True)
    (path / 'ProjectSettings' / 'ProjectVersion.txt').write_text(f'm_EditorVersion: {version}\nm_EditorVersionWithRevision: {version} (abc)\n', encoding='utf-8')
    return path


def junction(link, target):
    """Directory link without admin rights on Windows (mklink /J); symlink elsewhere. Returns False if not possible."""
    try:
        if os.name == 'nt':
            r = subprocess.run(['cmd', '/c', 'mklink', '/J', str(link), str(target)], capture_output=True)
            return r.returncode == 0
        os.symlink(target, link, target_is_directory=True); return True
    except OSError:
        return False


class Base(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(ignore_cleanup_errors=True)
        self.root = Path(self.tmp.name).resolve() / 'repo'; self.root.mkdir()
        self.inst = Path(self.tmp.name) / 'instances'; self.inst.mkdir()
        self.patches = [patch.object(U, 'INSTANCES', self.inst), patch.object(U, 'TIMEOUT', 0.6)]
        for p in self.patches: p.start()
        U.clear_cache(); self.fakes = []

    def tearDown(self):
        for f in self.fakes: f.close()
        for p in self.patches: p.stop()
        U.clear_cache(); self.tmp.cleanup()

    def fake(self, project, **kw):
        f = FakeEditor(project, **kw); self.fakes.append(f); return f

    def heartbeat(self, project, port, state='ready', age=0.0, name=None, timestamp=None):
        d = dict(state=state, projectPath=str(project).replace('\\', '/'), port=port, pid=1234, unityVersion='6000.3.19f1',
                 connectorVersion='0.3.18', timestamp=int((time.time() - age) * 1000) if timestamp is None else timestamp, compileErrors=False)
        (self.inst / (name or f'{abs(hash(str(project))) % 10**12:012d}.json')).write_text(json.dumps(d), encoding='utf-8')

    def snap(self):
        return U.snapshot(self.root)


class DiscoveryTests(Base):
    def test_non_unity_root_reports_nothing(self):
        (self.root / 'src').mkdir(); (self.root / 'README.md').write_text('x')
        r = self.snap()
        self.assertEqual(r['projects'], []); self.assertIs(r['supported'], True); self.assertIsInstance(r['checkedAt'], float)

    def test_root_and_direct_children_only(self):
        make_project(self.root); make_project(self.root / 'Game'); make_project(self.root / 'tools' / 'Nested')
        names = [p['name'] for p in self.snap()['projects']]
        self.assertEqual(names, ['repo', 'Game'])  # grandchild 'Nested' is out of range

    def test_version_and_fields(self):
        make_project(self.root / 'F1', '6000.3.9f1')
        p = self.snap()['projects'][0]
        self.assertEqual(p['version'], '6000.3.9f1'); self.assertEqual(p['path'], str(self.root / 'F1').replace('\\', '/'))
        self.assertEqual(set(p), {'path', 'name', 'version', 'status', 'reason', 'observedAt', 'scene', 'playing', 'compiling'})

    def test_linked_child_pointing_outside_is_skipped(self):
        outside = make_project(Path(self.tmp.name).resolve() / 'outside' / 'Other')
        if not junction(self.root / 'Linked', outside): self.skipTest('cannot create a directory link here')
        self.assertEqual(self.snap()['projects'], [])

    def test_linked_projectsettings_is_not_trusted(self):
        outside = make_project(Path(self.tmp.name).resolve() / 'outside2')
        (self.root / 'Fake').mkdir()
        if not junction(self.root / 'Fake' / 'ProjectSettings', outside / 'ProjectSettings'): self.skipTest('cannot create a directory link here')
        self.assertEqual(self.snap()['projects'], [])

    def test_project_count_is_bounded(self):
        for i in range(U.MAX_PROJECTS + 5): make_project(self.root / f'P{i:02d}')
        self.assertEqual(len(self.snap()['projects']), U.MAX_PROJECTS)


class StatusTests(Base):
    def setUp(self):
        super().setUp(); self.f1 = make_project(self.root / 'F1')

    def one(self):
        return self.snap()['projects'][0]

    def test_no_heartbeat_is_offline(self):
        p = self.one(); self.assertEqual(p['status'], 'offline'); self.assertIn('연결 기록이 없습니다', p['reason']); self.assertIsNone(p['observedAt'])

    def test_fresh_and_matching_is_connected_with_the_fixed_query(self):
        f = self.fake(self.f1, playing=True, compiling=False); self.heartbeat(self.f1, f.port)
        p = self.one()
        self.assertEqual((p['status'], p['scene'], p['playing'], p['compiling']), ('connected', 'Assets/Scenes/Main.unity', True, False))
        self.assertIsInstance(p['observedAt'], float); self.assertEqual(p['reason'], '')
        self.assertEqual(len(f.hits), 1); hit = f.hits[0]
        self.assertEqual(hit['path'], '/command'); self.assertEqual(hit['body'], U.PROBE_BODY); self.assertIsNone(hit['origin'])
        sent = json.loads(hit['body'])
        self.assertEqual(sent, {'command': 'exec', 'params': {'code': U.PROBE_CODE}})
        for word in ('Application.dataPath', 'EditorApplication.isPlaying', 'EditorApplication.isCompiling', 'GetActiveScene().path'):
            self.assertIn(word, U.PROBE_CODE)

    def test_untitled_scene_is_null(self):
        f = self.fake(self.f1, scene=''); self.heartbeat(self.f1, f.port)
        self.assertIsNone(self.one()['scene'])

    def test_stopped_heartbeat_is_offline_without_asking(self):
        f = self.fake(self.f1); self.heartbeat(self.f1, f.port, state='stopped')
        p = self.one(); self.assertEqual(p['status'], 'offline'); self.assertIn('stopped', p['reason']); self.assertEqual(f.hits, [])

    def test_stale_heartbeat_is_offline_without_asking(self):
        f = self.fake(self.f1); self.heartbeat(self.f1, f.port, age=2 * 86400)
        p = self.one()
        self.assertEqual(p['status'], 'offline'); self.assertIn('2일 전', p['reason']); self.assertEqual(f.hits, [])
        self.assertAlmostEqual(p['observedAt'], time.time() - 2 * 86400, delta=5)  # last heartbeat, so the UI can say "마지막 확인"

    def test_fresh_heartbeat_but_nobody_listening_is_offline(self):
        f = self.fake(self.f1); port = f.port; f.close(); self.fakes.remove(f)
        self.heartbeat(self.f1, port)
        p = self.one(); self.assertEqual(p['status'], 'offline')
        # Windows retries a refused loopback connect for ~2 s, so a closed port can surface as a timeout there.
        self.assertRegex(p['reason'], '연결되지 않았습니다|시간 안에 응답하지 않았습니다')

    def test_timeout_is_offline(self):
        f = self.fake(self.f1, mode='slow'); self.heartbeat(self.f1, f.port)
        t = time.monotonic(); p = self.one()
        self.assertEqual(p['status'], 'offline'); self.assertIn('시간 안에 응답하지 않았습니다', p['reason']); self.assertLess(time.monotonic() - t, 1.4)

    def test_other_project_on_that_port_is_unverified(self):
        f = self.fake(self.f1, mode='wrong'); self.heartbeat(self.f1, f.port)
        p = self.one(); self.assertEqual(p['status'], 'unverified'); self.assertIn('다른 프로젝트', p['reason'])
        self.assertIsNone(p['observedAt']); self.assertIsNone(p['scene']); self.assertIsNone(p['playing'])

    def test_redirect_is_not_followed(self):
        target = self.fake(self.f1); f = self.fake(self.f1, mode='redirect'); f.redirect_to = f'http://127.0.0.1:{target.port}/command'
        self.heartbeat(self.f1, f.port)
        p = self.one(); self.assertEqual(p['status'], 'unverified'); self.assertIn('따라가지 않았습니다', p['reason']); self.assertEqual(target.hits, [])

    def test_oversized_response_is_cut_and_unverified(self):
        f = self.fake(self.f1, mode='huge'); self.heartbeat(self.f1, f.port)
        p = self.one(); self.assertEqual(p['status'], 'unverified'); self.assertIn('너무 큽니다', p['reason'])

    def test_refused_or_garbage_answers_are_unverified(self):
        for mode, word in (('refuse-exec', '거절'), ('notjson', 'JSON')):
            U.clear_cache(); f = self.fake(self.f1, mode=mode); self.heartbeat(self.f1, f.port)
            p = self.one(); self.assertEqual(p['status'], 'unverified', mode); self.assertIn(word, p['reason'])

    def test_bad_port_value_is_unverified(self):
        self.heartbeat(self.f1, 'abc'); self.assertEqual(self.one()['status'], 'unverified')

    def test_proxy_environment_is_ignored(self):
        f = self.fake(self.f1); self.heartbeat(self.f1, f.port)
        with patch.dict(os.environ, {'HTTP_PROXY': 'http://127.0.0.1:9', 'http_proxy': 'http://127.0.0.1:9', 'NO_PROXY': ''}):
            self.assertEqual(self.one()['status'], 'connected')
        self.assertIsNone(f.hits[0]['via'])

    def test_heartbeat_must_match_exact_path(self):
        f = self.fake(self.f1)
        self.heartbeat(str(self.f1) + 'Other', f.port, name='a.json'); self.heartbeat(self.root, f.port, name='b.json')
        p = self.one(); self.assertEqual(p['status'], 'offline'); self.assertEqual(f.hits, [])

    def test_case_and_slash_differences_still_match_on_windows(self):
        if os.name != 'nt': self.skipTest('Windows paths only')
        f = self.fake(self.f1); self.heartbeat(str(self.f1).upper().replace('/', '\\'), f.port)
        self.assertEqual(self.one()['status'], 'connected')

    def test_two_projects_are_reported_separately(self):
        other = make_project(self.root / 'Tool')
        a = self.fake(self.f1, playing=True); b = self.fake(other, mode='wrong')
        self.heartbeat(self.f1, a.port); self.heartbeat(other, b.port)
        rows = {p['name']: p for p in self.snap()['projects']}
        self.assertEqual((rows['F1']['status'], rows['F1']['playing']), ('connected', True))
        self.assertEqual(rows['Tool']['status'], 'unverified')

    def test_broken_or_huge_descriptor_files_are_ignored(self):
        (self.inst / 'broken.json').write_text('{not json', encoding='utf-8')
        (self.inst / 'huge.json').write_text(json.dumps({'projectPath': str(self.f1), 'pad': 'x' * (U.MAX_DESCRIPTOR_BYTES + 1)}), encoding='utf-8')
        self.assertEqual(self.one()['status'], 'offline')


class CacheTests(Base):
    def setUp(self):
        super().setUp(); self.f1 = make_project(self.root / 'F1'); self.f = self.fake(self.f1); self.heartbeat(self.f1, self.f.port)

    def test_repeat_within_ttl_asks_once(self):
        a = self.snap(); b = self.snap(); self.assertEqual(len(self.f.hits), 1); self.assertEqual(a['checkedAt'], b['checkedAt'])

    def test_concurrent_callers_share_one_probe(self):
        self.f.mode = 'slow'
        with patch.object(U, 'TIMEOUT', 3.0):
            out = []; ts = [threading.Thread(target=lambda: out.append(self.snap())) for _ in range(8)]
            for t in ts: t.start()
            for t in ts: t.join(10)
        self.assertEqual(len(out), 8); self.assertEqual(len(self.f.hits), 1)
        self.assertEqual({r['projects'][0]['status'] for r in out}, {'connected'})

    def test_expired_cache_asks_again(self):
        with patch.object(U, 'CACHE_TTL', 0.05):
            self.snap(); time.sleep(0.1); self.snap()
        self.assertEqual(len(self.f.hits), 2)


class BudgetTests(Base):
    """The whole snapshot answers within BUDGET (the web client waits 12 s); what was not asked in time is 'unverified'."""
    def test_projects_beyond_the_budget_are_unverified_not_waited_for(self):
        fakes = []
        for i in range(5):
            proj = make_project(self.root / f'P{i}'); f = self.fake(proj, mode='slow'); f.delay = 1.8
            self.heartbeat(proj, f.port, name=f'p{i}.json'); fakes.append(f)
        with patch.object(U, 'TIMEOUT', 3.0), patch.object(U, 'BUDGET', 2.0):
            t = time.monotonic(); rows = self.snap()['projects']; took = time.monotonic() - t
        self.assertLess(took, 2.6)
        states = [r['status'] for r in rows]
        self.assertEqual(states.count('connected'), 2)        # two probe slots, each answered in 1.8 s
        late = [r for r in rows if r['status'] == 'unverified']
        self.assertEqual(len(late), 3)
        for r in late:
            self.assertIn('2초) 안에 차례가 오지 않아', r['reason']); self.assertIsNone(r['observedAt']); self.assertIsNone(r['playing'])
        self.assertEqual(sum(len(f.hits) for f in fakes), 2)  # the other three were never asked

    def test_a_request_cut_by_the_budget_is_unverified(self):
        proj = make_project(self.root / 'F1'); f = self.fake(proj, mode='slow'); f.delay = 3.0; self.heartbeat(proj, f.port)
        with patch.object(U, 'TIMEOUT', 6.0), patch.object(U, 'BUDGET', 1.0):
            t = time.monotonic(); r = self.snap()['projects'][0]; took = time.monotonic() - t
        self.assertLess(took, 1.6)
        self.assertEqual(r['status'], 'unverified'); self.assertIn('응답이 끝나지 않아', r['reason'])

    def test_a_slow_drip_cannot_hold_the_answer(self):
        proj = make_project(self.root / 'F1'); f = self.fake(proj, mode='drip'); self.heartbeat(proj, f.port)
        with patch.object(U, 'TIMEOUT', 6.0), patch.object(U, 'BUDGET', 1.5):
            t = time.monotonic(); r = self.snap()['projects'][0]; took = time.monotonic() - t
        self.assertLess(took, 2.1); self.assertEqual(r['status'], 'unverified')
        # The probe thread must also let go of its slot (read(n) used to block on a drip and keep it).
        got = [U._probe_slots.acquire(timeout=1.0) for _ in range(2)]
        for g in got:
            if g: U._probe_slots.release()
        self.assertEqual(got, [True, True])

    def test_trickled_headers_are_cut_at_the_deadline_and_free_the_slot(self):
        # Socket timeouts restart per byte, so only the deadline shutdown can stop getresponse() here.
        proj = make_project(self.root / 'F1'); f = self.fake(proj, mode='drip-headers'); self.heartbeat(proj, f.port)
        with patch.object(U, 'TIMEOUT', 0.5), patch.object(U, 'BUDGET', 0.3), patch.object(U, 'MIN_PROBE', 0.01):
            t = time.monotonic(); r = self.snap()['projects'][0]; took = time.monotonic() - t
        self.assertLess(took, 0.8); self.assertEqual(r['status'], 'unverified'); self.assertIn('응답이 끝나지 않아', r['reason'])
        end = time.monotonic() + 0.4
        while time.monotonic() < end and any(th.name == 'unity-status-probe' and th.is_alive() for th in threading.enumerate()):
            time.sleep(0.02)
        self.assertFalse(any(th.name == 'unity-status-probe' and th.is_alive() for th in threading.enumerate()))
        got = [U._probe_slots.acquire(timeout=0.2) for _ in range(2)]
        for g in got:
            if g: U._probe_slots.release()
        self.assertEqual(got, [True, True])

    def assert_cut_and_freed(self, mode):
        proj = make_project(self.root / mode); f = self.fake(proj, mode=mode); self.heartbeat(proj, f.port, name=mode + '.json')
        with patch.object(U, 'TIMEOUT', 2.0), patch.object(U, 'BUDGET', 0.3), patch.object(U, 'MIN_PROBE', 0.01):
            t = time.monotonic(); r = self.snap()['projects'][0]; took = time.monotonic() - t
        self.assertLess(took, 0.8, mode); self.assertEqual(r['status'], 'unverified', mode); self.assertIn('응답이 끝나지 않아', r['reason'])
        # The probe thread must be gone right after the budget, long before the 2 s socket timeout.
        end = time.monotonic() + 0.15
        while time.monotonic() < end and any(th.name == 'unity-status-probe' and th.is_alive() for th in threading.enumerate()):
            time.sleep(0.01)
        self.assertFalse(any(th.name == 'unity-status-probe' and th.is_alive() for th in threading.enumerate()), mode)
        got = [U._probe_slots.acquire(timeout=0.1) for _ in range(2)]
        for g in got:
            if g: U._probe_slots.release()
        self.assertEqual(got, [True, True], mode)

    def test_stalled_body_on_http10_is_cut(self):
        # http.client clears conn.sock for HTTP/1.0 answers; the deadline must still reach the real socket.
        self.assert_cut_and_freed('stall-http10')

    def test_stalled_body_with_connection_close_is_cut(self):
        self.assert_cut_and_freed('stall-close')

    def test_stalled_headers_are_cut(self):
        self.assert_cut_and_freed('stall-headers')

    def test_after_two_stuck_editors_a_healthy_one_is_still_asked(self):
        # Before the shutdown fix, two header-trickling ports held both slots and every later snapshot was 'unverified'.
        for i in range(2):
            proj = make_project(self.root / f'Stuck{i}'); f = self.fake(proj, mode='drip-headers'); self.heartbeat(proj, f.port, name=f's{i}.json')
        with patch.object(U, 'TIMEOUT', 0.5), patch.object(U, 'BUDGET', 0.3), patch.object(U, 'MIN_PROBE', 0.01):
            self.snap()
        time.sleep(0.3)
        for i in range(2): (self.inst / f's{i}.json').unlink()
        good = make_project(self.root / 'Good'); g = self.fake(good); self.heartbeat(good, g.port)
        U.clear_cache()
        rows = {r['name']: r['status'] for r in self.snap()['projects']}
        self.assertEqual(rows['Good'], 'connected')

    def test_default_budget_fits_the_web_timeout(self):
        self.assertLessEqual(U.BUDGET, 10.0); self.assertLess(U.BUDGET, 12.0); self.assertLessEqual(U.TIMEOUT, U.BUDGET)


class TimestampTests(Base):
    """A heartbeat time that is not a real, present time must never count as fresh (and the editor is not asked)."""
    def setUp(self):
        super().setUp(); self.f1 = make_project(self.root / 'F1'); self.f = self.fake(self.f1)

    def raw_heartbeat(self, ts_literal, name='raw.json'):
        text = ('{"state":"ready","projectPath":%s,"port":%d,"pid":1,"timestamp":%s}'
                % (json.dumps(str(self.f1).replace('\\', '/')), self.f.port, ts_literal))
        (self.inst / name).write_text(text, encoding='utf-8')

    def test_nan_infinity_and_future_times_are_not_fresh(self):
        future = int((time.time() + 3600) * 1000)
        for literal in ('NaN', 'Infinity', '-Infinity', str(future), '"1792149733008"', 'true', '0', '1e400'):
            U.clear_cache(); self.raw_heartbeat(literal)
            r = self.snap()['projects'][0]
            self.assertEqual(r['status'], 'unverified', literal); self.assertIn('시각이 올바르지 않습니다', r['reason'])
            self.assertIsNone(r['observedAt'], literal)
        self.assertEqual(self.f.hits, [])

    def test_small_clock_skew_is_allowed(self):
        self.heartbeat(self.f1, self.f.port, timestamp=int((time.time() + 2) * 1000))
        self.assertEqual(self.snap()['projects'][0]['status'], 'connected')

    def test_a_valid_heartbeat_wins_over_a_broken_one_for_the_same_project(self):
        self.raw_heartbeat('NaN', name='a-broken.json'); self.heartbeat(self.f1, self.f.port, name='b-good.json')
        self.assertEqual(self.snap()['projects'][0]['status'], 'connected')


class RouteTests(Base):
    """GET /api/hub/unity-status goes through the real server.H handler with the selected project's root."""
    def test_route_passes_the_project_root(self):
        import hub, server
        f1 = make_project(self.root / 'F1'); f = self.fake(f1); self.heartbeat(f1, f.port)
        seen = []

        class FakeHub:
            root = self.root
        def fake_get_hub(project=None):
            seen.append(project)
            if project not in (None, 'default', 'p1'): raise ValueError('등록되지 않은 프로젝트입니다.')
            return FakeHub()
        srv = ThreadingHTTPServer(('127.0.0.1', 0), server.H); threading.Thread(target=srv.serve_forever, daemon=True).start()
        try:
            with patch.object(hub, 'get_hub', fake_get_hub), patch.object(server, 'PORT', srv.server_address[1]):
                base = f'http://127.0.0.1:{srv.server_address[1]}/api/hub/unity-status'
                body = json.loads(urllib.request.urlopen(base + '?project=p1', timeout=10).read())
                self.assertEqual(body['projects'][0]['status'], 'connected'); self.assertIn('p1', seen)
                with self.assertRaises(urllib.error.HTTPError) as e:
                    urllib.request.urlopen(base + '?project=nope', timeout=10)
                self.assertEqual(e.exception.code, 400)
        finally:
            srv.shutdown(); srv.server_close()


if __name__ == '__main__':
    unittest.main()
