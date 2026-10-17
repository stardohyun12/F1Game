"""Real temporary SQLite/files; dispatch is intercepted so no AI calls are made."""
import hashlib
import sqlite3
import tempfile
import threading
import unittest
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from unittest.mock import patch
from hub import Hub


class GoalStartBaselineTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name)/'project';self.root.mkdir()
        self.h=Hub(Path(self.tmp.name)/'runtime',root=self.root)
        self.a=self.h.store.agents()[0]['id'];self.b=self.h.store.create(name='QA',provider='claude')['id']
        self.p=self.root/'HUD.cs';self.p.write_text('before',encoding='utf-8')
        self.send=patch.object(self.h,'send');self.dispatch=self.send.start()
        self.alert=patch.object(self.h,'cycle_alert');self.alert.start()
    def tearDown(self):
        self.alert.stop();self.send.stop();self.h.close();self.h.store.db.close();self.tmp.cleanup()
    def start(self,**extra):
        return self.h.start_cycle(dict(mode='goal',goal='HUD 개선',agent=self.a,**extra))
    def unchanged_on_failure(self,**extra):
        before=[tuple(r) for r in self.h.store.db.execute('SELECT * FROM settings ORDER BY key')]
        with self.assertRaises((ValueError,sqlite3.DatabaseError)):self.start(**extra)
        self.assertEqual(before,[tuple(r) for r in self.h.store.db.execute('SELECT * FROM settings ORDER BY key')])
        self.dispatch.assert_not_called()

    def test_saved_before_dispatch_and_content_change_is_visible(self):
        seen=[]
        def dispatch(aid,data):
            cid=data['replyTo']['cycle'];c=self.h.cycle(cid)
            rows=self.h.file_baselines(goal=cid)['items'];self.assertEqual(len(rows),1)
            self.assertEqual(rows[0]['id'],c['startBaseline']);seen.append(rows[0])
            self.p.write_text('after',encoding='utf-8')
        self.dispatch.side_effect=dispatch
        c=self.start(baselinePaths=['HUD.cs','new.cs'])
        self.assertEqual(len(seen),1);r=seen[0]
        self.assertEqual(r['entries'][0]['sha256'],hashlib.sha256(b'before').hexdigest())
        self.assertEqual(r['entries'][1]['status'],'missing');self.assertEqual(r['capturePoint'],'goal-start')
        self.assertEqual([x['status'] for x in self.h.file_baselines(goal=c['id'],bid=r['id'])['entries']],['changed','missing'])

    def test_no_option_keeps_original_start(self):
        c=self.start();self.assertNotIn('startBaseline',c)
        self.assertEqual(self.h.file_baselines()['items'],[]);self.dispatch.assert_called_once()

    def test_invalid_paths_never_start_or_write(self):
        for paths in ([],None,'HUD.cs',['../x'],['HUD.cs','CON'],['x']*21):
            with self.subTest(paths=paths):self.unchanged_on_failure(baselinePaths=paths)

    def test_partial_unavailable_aborts_entire_start(self):
        (self.root/'folder').mkdir();self.unchanged_on_failure(baselinePaths=['HUD.cs','folder'])

    def test_capacity_failure_no_goal_no_dispatch(self):
        self.h.store.put('fileBaselines',[{'id':str(i)} for i in range(100)])
        self.unchanged_on_failure(baselinePaths=['HUD.cs'])

    def test_second_insert_failure_rolls_back_goal_and_revision(self):
        self.h.store.put('cycles',[]);self.h.store.cycle_summaries()
        before=self.h.store.db.execute('SELECT version FROM cycle_revision').fetchone()[0]
        self.h.store.db.execute("CREATE TEMP TRIGGER deny_baseline BEFORE INSERT ON settings WHEN NEW.key='fileBaselines' BEGIN SELECT RAISE(ABORT,'test disk failure'); END")
        self.unchanged_on_failure(baselinePaths=['HUD.cs'])
        self.assertEqual(self.h.store.db.execute('SELECT version FROM cycle_revision').fetchone()[0],before)
        self.assertEqual(self.h.store.cycle_summaries(),[])

    def test_failed_personal_dispatch_removes_only_its_goal_and_baseline(self):
        old=self.h.file_baselines(dict(agent=self.a,label='기존',paths=['HUD.cs']))['record']
        self.dispatch.side_effect=ValueError('dispatch refused')
        with self.assertRaisesRegex(ValueError,'목표를 시작하지 못했습니다'):self.start(baselinePaths=['HUD.cs'])
        self.assertEqual(self.h.store.get('cycles',[]),[]);self.assertEqual(self.h.file_baselines()['items'],[old])

    def test_team_start_uses_same_pre_dispatch_recording(self):
        t=self.h.save_team(dict(name='개발',members=[self.a],lead=self.a))
        q=self.h.save_team(dict(name='검토',members=[self.b],lead=self.b))
        def dispatch(aid,data):
            rows=self.h.file_baselines(goal=data['replyTo']['cycle'])['items']
            self.assertEqual(len(rows),1);self.assertEqual(rows[0]['agent'],self.a)
        self.dispatch.side_effect=dispatch
        c=self.h.start_cycle(dict(goal='HUD 개선',criteria='표시 확인',teams=dict(research=t['id'],build=t['id'],qa=q['id']),baselinePaths=['HUD.cs']))
        self.assertEqual(c['stages'][0]['kind'],'research');self.dispatch.assert_called_once()

    def test_slow_observation_does_not_hold_shared_lock_and_rechecks_capacity(self):
        import file_baselines
        observe=file_baselines.observe;entered=threading.Event();release=threading.Event()
        def slow(*args):
            entered.set()
            if not release.wait(3):raise RuntimeError('probe release timed out')
            return observe(*args)
        with patch.object(file_baselines,'observe',side_effect=slow),ThreadPoolExecutor(max_workers=1) as pool:
            future=pool.submit(self.start,baselinePaths=['HUD.cs'])
            try:
                self.assertTrue(entered.wait(2));locked=self.h.lock.acquire(timeout=.1)
                try:
                    self.assertTrue(locked,'file observation held the shared Hub lock')
                    self.h.store.put('fileBaselines',[{'id':str(i)} for i in range(100)])
                finally:
                    if locked:self.h.lock.release()
            finally:release.set()
            with self.assertRaisesRegex(ValueError,'100개 한도'):future.result(timeout=3)
        self.assertEqual(self.h.store.get('cycles',[]),[]);self.dispatch.assert_not_called()

    def test_goal_started_during_observation_is_rechecked(self):
        import file_baselines
        observe=file_baselines.observe
        def competing(*args):
            self.start();return observe(*args)
        with patch.object(file_baselines,'observe',side_effect=competing):
            with self.assertRaisesRegex(ValueError,'이미 진행 중인 목표'):self.start(baselinePaths=['HUD.cs'])
        self.assertEqual(len(self.h.store.get('cycles',[])),1)
        self.assertEqual(self.h.file_baselines()['items'],[]);self.dispatch.assert_called_once()

    def test_team_changed_during_observation_is_rechecked(self):
        import file_baselines
        observe=file_baselines.observe
        t=self.h.save_team(dict(name='개발',members=[self.a],lead=self.a))
        q=self.h.save_team(dict(name='검토',members=[self.b],lead=self.b))
        def competing(*args):
            self.h.save_team(dict(id=q['id'],name='검토',members=[self.a],lead=self.a))
            return observe(*args)
        with patch.object(file_baselines,'observe',side_effect=competing):
            with self.assertRaisesRegex(ValueError,'서로 다른 팀장'):
                self.h.start_cycle(dict(goal='HUD',teams=dict(research=t['id'],build=t['id'],qa=q['id']),baselinePaths=['HUD.cs']))
        self.assertEqual(self.h.store.get('cycles',[]),[])
        self.assertEqual(self.h.file_baselines()['items'],[]);self.dispatch.assert_not_called()


if __name__=='__main__':unittest.main()
