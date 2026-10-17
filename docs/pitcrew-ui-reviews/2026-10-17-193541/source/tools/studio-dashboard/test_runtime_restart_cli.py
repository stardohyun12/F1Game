"""Exercise the public restart CLI in disposable directories, without AI requests."""
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch
import urllib.request

from isolated_runtime import HERE, isolated_runtime
from isolated_process_scope import WindowsProcessScope
import test_goal_start_proxy as proxy


def restart(env, processes, *, directory):
    supplied = env.get('PITCREW_RUNTIME_DIR')
    if not supplied or not Path(supplied).is_absolute():
        raise ValueError('An explicit absolute isolated runtime directory is required')
    target = Path(supplied).resolve()
    expected = Path(directory).resolve()
    temporary = Path(tempfile.gettempdir()).resolve()
    production = (HERE / '.runtime').resolve()
    if target != expected or target == production or temporary not in target.parents:
        raise ValueError('Restart target is not the owned temporary runtime directory')
    # Also reject a copied descriptor which would direct HTTP to production.
    if (target / 'runtime.json').exists() and (production / 'runtime.json').exists():
        candidate = json.loads((target / 'runtime.json').read_text(encoding='utf-8'))
        owner = json.loads((production / 'runtime.json').read_text(encoding='utf-8'))
        if any(candidate.get(key) and candidate.get(key) == owner.get(key) for key in ('pid', 'secret')):
            raise ValueError('Restart descriptor refers to production')
    return processes.run([sys.executable, '-X', 'utf8', str(HERE / 'runtime.py'), '--restart'],
        cwd=HERE, env={**env, 'PITCREW_RUNTIME_DIR': str(target)}, timeout=35)


@unittest.skipUnless(os.name == 'nt', 'Windows restart process ownership')
class RuntimeRestartCliTests(unittest.TestCase):
    def test_idle_restart_preserves_saved_baseline(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            project = root / 'project'
            project.mkdir()
            (project / 'HUD.cs').write_text('before', encoding='utf-8')
            directory = root / 'runtime'
            env = dict(PITCREW_PROJECT=str(project), PITCREW_WINDOWS_NOTIFY='0',
                       CODEX_HOME=str(root / 'codex'), CLAUDE_CONFIG_DIR=str(root / 'claude'))
            with WindowsProcessScope() as processes, isolated_runtime(directory, env) as child_env:
                def request(path, data=None, token=''):
                    descriptor = json.loads((directory / 'runtime.json').read_text(encoding='utf-8'))
                    req = urllib.request.Request(f"http://127.0.0.1:{descriptor['port']}" + path,
                        data=None if data is None else json.dumps(data).encode(),
                        headers={'X-PitCrew-Runtime': descriptor['secret'],
                                 'X-PitCrew-Token': token, 'Content-Type': 'application/json'})
                    with urllib.request.urlopen(req, timeout=5) as response:
                        return json.load(response)

                before = request('/api/hub/bootstrap')
                identity = request('/api/hub/runtime-identity')
                saved = request('/api/hub/file-baselines',
                    dict(label='CLI restart baseline', agent=before['agents'][0]['id'], paths=['HUD.cs']),
                    before['token'])['record']
                self.assertTrue(request('/api/hub/maintenance-status')['ready'])
                result = restart(child_env, processes, directory=directory)
                self.assertEqual(result.returncode, 0, result.stderr)
                after = request('/api/hub/bootstrap')
                self.assertNotEqual(after['runtime']['pid'], before['runtime']['pid'])
                self.assertIn(str(after['runtime']['pid']), result.stdout)
                self.assertEqual([a['id'] for a in after['agents']], [a['id'] for a in before['agents']])
                self.assertEqual(request('/api/hub/file-baselines')['items'], [saved])
                self.assertEqual(request('/api/hub/goal-history')['total'], 0)
                self.assertTrue(request('/api/hub/maintenance-status')['ready'])
                for name, row in request('/api/hub/runtime-identity')['modules'].items():
                    self.assertEqual(row['loadedCodeSha256'], identity['modules'][name]['loadedCodeSha256'])
                    self.assertIs(row['sourceChangedSinceCapture'], False)
                self.assertEqual((project / 'HUD.cs').read_text(), 'before')
                descriptor = json.loads((directory / 'runtime.json').read_text())
                health_request = urllib.request.Request(
                    f"http://127.0.0.1:{descriptor['port']}/api/hub/runtime-health",
                    headers={'X-PitCrew-Runtime': descriptor['secret']})
                print(json.dumps(dict(case='idle', beforePid=before['runtime']['pid'],
                    afterPid=after['runtime']['pid'], cliExit=result.returncode,
                    baseline=saved['id'], goals=0, ready=True)))
            # Context cleanup must also stop the replacement runtime, not just the original child.
            with self.assertRaises(urllib.error.URLError) as stopped:
                urllib.request.urlopen(health_request, timeout=2)
            self.assertNotIsInstance(stopped.exception, urllib.error.HTTPError)

    def test_running_goal_refuses_restart_and_keeps_web_and_work(self):
        # Reuse the verified real proxy/HTTP/SQLite fixture. Only dispatch is a stub.
        fixture = proxy.GoalStartProxyTests('test_requests_during_backend_gap_fail_without_replay')
        processes = WindowsProcessScope()
        try:
            fixture.setUp()
            code, goal = fixture.request('/api/hub/cycle', fixture.payload())
            self.assertEqual(code, 200, goal)
            web_before = fixture.request('/__test/web-identity')
            code, identity = fixture.request('/api/hub/runtime-identity')
            self.assertEqual(code, 200, identity)
            events = fixture.events()
            counts = fixture.counts()
            history_before = fixture.request('/api/hub/goal-history?id=' + goal['id'])
            self.assertEqual(history_before[0], 200, history_before)
            code, maintenance = fixture.request('/api/hub/maintenance-status')
            self.assertEqual(code, 200, maintenance)
            self.assertFalse(maintenance['ready'])
            self.assertIn(goal['id'], maintenance['projects'][0]['runningGoals'])
            result = restart({**os.environ, 'PITCREW_RUNTIME_DIR': str(fixture.root),
                              'PITCREW_WINDOWS_NOTIFY': '0', 'PITCREW_PROJECT': str(fixture.root / 'project'),
                              'CODEX_HOME': str(fixture.root / 'codex'),
                              'CLAUDE_CONFIG_DIR': str(fixture.root / 'claude')}, processes, directory=fixture.root)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('작업·후속 처리 또는 저장 요청을 처리 중이어서 실행 서비스를 유지합니다.', result.stderr)
            self.assertNotIn('준비됨', result.stdout)
            self.assertIsNone(fixture.child.poll())
            self.assertEqual(fixture.request('/__test/web-identity'), web_before)
            code, after = fixture.request('/api/hub/runtime-identity')
            self.assertEqual(code, 200, after)
            self.assertEqual(after['runtime']['pid'], identity['runtime']['pid'])
            self.assertEqual(fixture.events(), events)
            self.assertEqual(fixture.counts(), counts)
            code, history = fixture.request('/api/hub/goal-history?id=' + goal['id'])
            self.assertEqual(code, 200, history)
            self.assertEqual(history['cycle'], history_before[1]['cycle'])
            print(json.dumps(dict(case='busy', runtimePid=after['runtime']['pid'],
                web=web_before[1], cliExit=result.returncode, goal=goal['id'],
                dispatches=len(events), counts=counts, stderr=result.stderr.strip())))
        finally:
            try:
                # Stop any replacement before fixture cleanup removes its temporary DB/logs.
                processes.__exit__(*sys.exc_info())
            finally:
                self.assertTrue(fixture.doCleanups(), 'Fixture cleanup failed')

    def test_failed_busy_assertion_cleans_unexpected_replacement(self):
        fixture = proxy.GoalStartProxyTests('test_requests_during_backend_gap_fail_without_replay')
        scopes = []
        replacement = {}
        original_restart = restart

        def injected_bad_restart(env, scope, *, directory):
            # Simulate a regression that stops busy work. Only this exact fixture child is killed.
            old_pid = fixture.info['pid']
            fixture.child.terminate()
            fixture.child.wait(timeout=5)
            result = original_restart(env, scope, directory=directory)  # Real runtime; no AI request.
            descriptor = json.loads((fixture.root / 'runtime.json').read_text())
            replacement.update(oldPid=old_pid, newPid=descriptor['pid'], code=result.returncode,
                               port=descriptor['port'], secret=descriptor['secret'])
            return result

        nested = RuntimeRestartCliTests('test_running_goal_refuses_restart_and_keeps_web_and_work')
        result = unittest.TestResult()
        # Capture the real class before replacing its factory used by the nested test.
        real_scope = WindowsProcessScope
        def scope_factory():
            scope = real_scope()
            scopes.append(scope)
            return scope
        with patch.object(proxy, 'GoalStartProxyTests', return_value=fixture), \
             patch(__name__ + '.restart', side_effect=injected_bad_restart), \
             patch(__name__ + '.WindowsProcessScope', side_effect=scope_factory):
            nested.run(result)
        self.assertEqual(len(result.failures), 1, result.failures)
        self.assertIn('0 == 0', result.failures[0][1])
        self.assertEqual(result.errors, [])
        self.assertEqual(replacement['code'], 0)
        self.assertNotEqual(replacement['oldPid'], replacement['newPid'])
        self.assertEqual([scope.remaining for scope in scopes], [0])
        request = urllib.request.Request(f"http://127.0.0.1:{replacement['port']}/api/hub/runtime-health",
            headers={'X-PitCrew-Runtime': replacement['secret']})
        with self.assertRaises(urllib.error.URLError) as stopped:
            urllib.request.urlopen(request, timeout=2)
        self.assertNotIsInstance(stopped.exception, urllib.error.HTTPError)
        self.assertFalse(fixture.root.exists())

    def test_restart_rejects_missing_wrong_and_production_targets_before_launch(self):
        with tempfile.TemporaryDirectory() as folder:
            owned = Path(folder) / 'owned'
            runner = Mock()
            candidates = [None, '', 'relative-runtime', str(HERE / '.runtime'),
                          tempfile.gettempdir(), str(Path(folder) / 'other')]
            for candidate in candidates:
                with self.subTest(candidate=candidate):
                    env = {} if candidate is None else {'PITCREW_RUNTIME_DIR': candidate}
                    with self.assertRaises(ValueError):
                        restart(env, runner, directory=owned)
            runner.run.assert_not_called()

    def test_restart_rejects_copied_production_descriptor_before_launch(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            fake_here = root / 'tool'
            production = fake_here / '.runtime'
            production.mkdir(parents=True)
            owned = root / 'owned'
            owned.mkdir()
            production.joinpath('runtime.json').write_text(json.dumps(dict(pid=42, secret='production-test')))
            runner = Mock()
            with patch(__name__ + '.HERE', fake_here):
                for descriptor in (dict(pid=42, secret='different'), dict(pid=43, secret='production-test')):
                    owned.joinpath('runtime.json').write_text(json.dumps(descriptor))
                    with self.assertRaisesRegex(ValueError, 'production'):
                        restart({'PITCREW_RUNTIME_DIR': str(owned)}, runner, directory=owned)
            runner.run.assert_not_called()


if __name__ == '__main__':
    unittest.main()
