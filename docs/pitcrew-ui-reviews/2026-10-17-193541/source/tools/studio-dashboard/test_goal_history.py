import tempfile
import unittest
from unittest.mock import patch
from hub import Hub


class GoalHistoryTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.h = Hub(self.tmp.name)
        self.rows = [dict(id=str(n), goal=f'HUD 점검 {n}', status='done', stages=[
            dict(kind='build', report=f'전문 {n}', input='큰 전달 자료', summary='요약')]) for n in range(25)]
        self.h.store.put('cycles', self.rows)

    def tearDown(self):
        self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def test_pagination_no_report_payload_or_write(self):
        with patch.object(self.h.store, 'put', side_effect=AssertionError('write')):
            pages = [self.h.goal_history(page=str(n)) for n in (1, 2, 3)]
        self.assertEqual([r['id'] for p in pages for r in p['items']], [str(n) for n in reversed(range(25))])
        self.assertEqual([len(p['items']) for p in pages], [10, 10, 5])
        self.assertTrue(all('stages' not in r for p in pages for r in p['items']))
        self.assertEqual(self.h.goal_history(page='999')['page'], 3)

    def test_search_and_exact_older_record(self):
        self.assertEqual(self.h.goal_history(' hud 점검 24 ')['items'][0]['id'], '24')
        self.assertEqual(self.h.goal_history('none')['total'], 0)
        c = self.h.goal_history(cid='0')['cycle']
        self.assertNotIn('report', c['stages'][0]); self.assertNotIn('input', c['stages'][0])
        self.assertEqual(self.h.stage_report(c['id'], 0)['stage']['report'], '전문 0')

    def test_invalid_pages_and_missing_id(self):
        for value in ('0', '-1', '1.2', '01', 'bad', '1'*10):
            with self.subTest(value=value), self.assertRaises(ValueError):self.h.goal_history(page=value)
        with self.assertRaises(ValueError):self.h.goal_history(cid='missing')
        with self.assertRaises(ValueError):self.h.goal_history('x'*201)

    def test_project_scope(self):
        with tempfile.TemporaryDirectory() as directory:
            other = Hub(directory)
            try:
                self.assertEqual(other.goal_history()['total'], 0)
                with self.assertRaises(ValueError):other.goal_history(cid='0')
            finally:other.close(); other.store.db.close()

    def test_new_team_goal_keeps_old_report(self):
        with patch.object(self.h, 'cycle_target', side_effect=lambda tid:(tid, [])), patch.object(self.h, 'cycle_stage'):
            self.h.start_cycle(dict(goal='새 팀 목표',teams=dict(research='r',build='b',qa='q')))
        self.assertEqual(self.h.goal_history()['total'], 26)
        self.assertEqual(self.h.stage_report('0', 0)['stage']['report'], '전문 0')

    def test_new_personal_goal_keeps_old_report(self):
        a=self.h.store.create(name='담당',provider='claude')
        with patch.object(self.h, 'goal_stage'):
            self.h.start_goal(dict(goal='새 개인 목표',agent=a['id']))
        self.assertEqual(self.h.goal_history()['total'], 26)
        self.assertEqual(self.h.stage_report('0', 0)['stage']['report'], '전문 0')


if __name__=='__main__':unittest.main()
