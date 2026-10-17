"""Persisted goal evidence reads: exact identity, no latest-message inference, project isolation."""
import tempfile
import unittest
from unittest.mock import patch
from hub import Hub


class StageReportTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.h = Hub(self.tmp.name)
        self.report = '담당자 보고\n' + '긴 검증 출력\n' * 200 + '8 PASS / 1 FAIL'
        self.h.store.put('cycles', [dict(id='old', goal='HUD 검증', stages=[
            dict(kind='build', report='다른 단계'),
            dict(kind='review', report=self.report, input='전달 자료', criteria=[
                dict(n=1, status='충족', evidence='담당자의 주장')])
        ])] + [dict(id=str(n), stages=[]) for n in range(7)])

    def tearDown(self):
        self.h.close()
        self.h.store.db.close()
        self.tmp.cleanup()

    def test_exact_old_stage_full_report_and_no_write(self):
        before = self.h.store.get('cycles')
        with patch.object(self.h.store, 'put', side_effect=AssertionError('write')):
            row = self.h.stage_report('old', '1')
        self.assertEqual(row['stage']['report'], self.report)
        self.assertEqual(row['index'], 1)
        self.assertEqual(row['projectId'], 'default')
        self.assertEqual(self.h.store.get('cycles'), before)

    def test_invalid_indices_do_not_fall_back(self):
        for value in (-1, '-1', '', '1.0', '01', 'bad', None, True, 99):
            with self.subTest(value=value), self.assertRaises(ValueError):
                self.h.stage_report('old', value)

    def test_missing_cycle_does_not_return_latest(self):
        with self.assertRaisesRegex(ValueError, '프로젝트'):
            self.h.stage_report('missing', 0)

    def test_other_project_cannot_read_cycle(self):
        with tempfile.TemporaryDirectory() as directory:
            other = Hub(directory)
            try:
                other.project_id = 'other'
                with self.assertRaises(ValueError):
                    other.stage_report('old', 1)
            finally:
                other.close()
                other.store.db.close()

    def test_pending_report_is_not_fabricated(self):
        self.h.store.put('cycles', [dict(id='pending', stages=[dict(status='running')])])
        self.assertNotIn('report', self.h.stage_report('pending', 0)['stage'])


if __name__ == '__main__':
    unittest.main()
