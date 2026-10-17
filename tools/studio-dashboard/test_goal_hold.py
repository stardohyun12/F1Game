"""Boundary holds using real Hub/SQLite; provider execution is disabled."""
import copy
import threading
import unittest

import hub
import runtime
import test_simple_goal as base
BODY = base.BODY


class GoalHoldTests(unittest.TestCase):
    setUp = base.SimpleGoalTests.setUp
    tearDown = base.SimpleGoalTests.tearDown
    start = base.SimpleGoalTests.start
    end_turn = base.SimpleGoalTests.end_turn
    cyc = base.SimpleGoalTests.cyc

    def finish_held(self, state='계속'):
        c = self.start()
        before = copy.deepcopy(c)
        run = self.h.runs[self.g['id']]
        self.h.hold_cycle(c['id'])
        self.h.hold_cycle(c['id'])
        self.assertIs(self.h.runs[self.g['id']], run)
        self.end_turn(self.g['id'], '검증 결과' + BODY + '\n상태: ' + state)
        c = self.cyc(c)
        self.assertEqual(c['status'], 'paused')
        self.assertEqual(len(c['stages']), 1)
        self.assertEqual(c['stages'][0]['status'], 'done')
        for key in ('id', 'goal', 'criteria', 'owner'):
            self.assertEqual(c[key], before[key])
        self.assertFalse(self.h.runs)
        return c

    def test_build_and_review_keep_round_input_and_dispatch_once(self):
        for state, kind, expected_round in [('계속', 'build', 2), ('완료 후보', 'review', 1)]:
            with self.subTest(state=state):
                c = self.finish_held(state)
                pending = copy.deepcopy(c['heldStage'])
                self.assertEqual((pending['kind'], pending['round']), (kind, expected_round))
                with self.assertRaises(ValueError): self.h.retry_cycle(c['id'])
                c = self.h.release_cycle(c['id'])
                self.assertEqual((c['stages'][-1]['kind'], c['round']), (kind, expected_round))
                self.assertEqual(c['stages'][-1]['input'], pending['input'])
                self.assertIn(pending['input'], self.h.store.messages(self.g['id'])[-1]['text'])
                with self.assertRaises(ValueError): self.h.release_cycle(c['id'])
                self.assertEqual(len(self.cyc(c)['stages']), 2)
                self.h.stop_cycle(c['id'])
                self.h.runs.clear()

    def test_restart_preserves_saved_review_and_session(self):
        c = self.finish_held('완료 후보')
        self.h.store.update(self.g['id'], session='test-session-preserved')
        saved = copy.deepcopy(c)
        self.h.close(); self.h.store.db.close()
        self.h = hub.Hub(self.tmp.name)
        self.h.recover_cycles()
        self.assertEqual(self.h.cycle(c['id']), saved)
        self.assertEqual(self.h.store.agent(self.g['id'])['session'], 'test-session-preserved')
        c = self.h.release_cycle(c['id'])
        self.assertEqual(c['stages'][-1]['kind'], 'review')
        self.assertEqual(c['round'], 1)

    def test_release_race_creates_one_stage(self):
        c = self.finish_held()
        gate = threading.Barrier(3)
        outcomes = []
        def release():
            gate.wait()
            try: self.h.release_cycle(c['id']); outcomes.append('ok')
            except ValueError: outcomes.append('rejected')
        threads = [threading.Thread(target=release) for _ in range(2)]
        for t in threads: t.start()
        gate.wait()
        for t in threads: t.join(5); self.assertFalse(t.is_alive())
        self.assertCountEqual(outcomes, ['ok', 'rejected'])
        self.assertEqual(len(self.cyc(c)['stages']), 2)

    def test_user_queue_preserved_and_release_appends_after_it(self):
        c = self.finish_held()
        self.h.enqueue(self.g['id'], self.h.prepare_message({'text': 'user instruction'}))
        running = self.h.runs[self.g['id']]
        self.h.enqueue(self.g['id'], self.h.prepare_message({'text': 'second user instruction'}))
        before = copy.deepcopy(self.h.store.agent(self.g['id'])['queue'])
        self.assertFalse(runtime.begin_shutdown(self.h))
        self.h.release_cycle(c['id'])
        queued = self.h.store.agent(self.g['id'])['queue']
        self.assertEqual(queued[:-1], before)
        self.assertEqual(queued[-1]['replyTo']['cycle'], c['id'])
        self.assertIs(self.h.runs[self.g['id']], running)

    def test_natural_block_failure_and_completion_are_not_held(self):
        for state, status in [('막힘 — 입력 필요', 'paused'), ('계속', 'failed')]:
            c = self.start(); self.h.hold_cycle(c['id'])
            self.end_turn(self.g['id'], '검증' + BODY + '\n상태: ' + state,
                          status='error' if status == 'failed' else 'done')
            c = self.cyc(c)
            self.assertEqual(c['status'], status)
            self.assertFalse(c.get('heldStage'))
            self.h.retry_cycle(c['id'])
            self.assertEqual(self.cyc(c)['status'], 'running')
            self.assertFalse(self.cyc(c).get('holdRequested'))
            self.assertIn('이전 보류 예약을 해제', self.cyc(c)['resumeNote'])
            self.h.stop_cycle(c['id'])
            self.h.runs.clear()
        c = self.start()
        self.end_turn(self.g['id'], '완료' + BODY + '\n상태: 완료 후보')
        self.h.hold_cycle(c['id'])
        self.end_turn(self.g['id'], BODY + base.SimpleGoalTests.MET)
        self.assertEqual(self.cyc(c)['status'], 'done')
        self.assertFalse(self.cyc(c).get('heldStage'))

    def test_full_queue_release_preserves_hold_and_every_queued_message(self):
        c = self.finish_held('완료 후보')
        self.h.enqueue(self.g['id'], self.h.prepare_message({'text': 'running user task'}))
        for i in range(10):
            self.h.enqueue(self.g['id'], self.h.prepare_message({'text': 'queued ' + str(i)}))
        before = copy.deepcopy(self.h.store.agent(self.g['id'])['queue'])
        with self.assertRaises(ValueError): self.h.release_cycle(c['id'])
        self.assertEqual(self.cyc(c), c)
        self.assertEqual(self.h.store.agent(self.g['id'])['queue'], before)

    def test_short_report_reask_waits_for_release_without_new_stage(self):
        c = self.start(); self.h.hold_cycle(c['id'])
        self.end_turn(self.g['id'], '짧음')
        saved = self.cyc(c)
        self.assertFalse(self.h.runs)
        self.assertEqual(saved['status'], 'paused')
        self.assertEqual(saved['heldStage']['reask'], 0)
        self.assertEqual((saved['round'], len(saved['stages'])), (1, 1))
        self.h.close(); self.h.store.db.close()
        self.h = hub.Hub(self.tmp.name); self.h.recover_cycles()
        self.assertEqual(self.cyc(c), saved)
        self.h.release_cycle(c['id'])
        self.assertEqual(self.h.store.messages(self.g['id'])[-1]['text'], saved['heldStage']['input'])
        self.assertEqual((self.cyc(c)['round'], len(self.cyc(c)['stages'])), (1, 1))
        with self.assertRaises(ValueError): self.h.release_cycle(c['id'])
        self.end_turn(self.g['id'], BODY + '\n상태: 계속')
        self.assertEqual((self.cyc(c)['round'], len(self.cyc(c)['stages'])), (2, 2))

    def test_reask_dispatch_failure_keeps_exact_pending_request(self):
        from unittest.mock import patch
        c = self.start(); self.h.hold_cycle(c['id'])
        self.end_turn(self.g['id'], '짧음')
        saved = copy.deepcopy(self.cyc(c))
        with patch.object(self.h, 'send', side_effect=ValueError('fixture dispatch failure')):
            with self.assertRaises(ValueError): self.h.release_cycle(c['id'])
        self.assertEqual(self.cyc(c), saved)
        self.h.release_cycle(c['id'])
        self.assertEqual(len(self.cyc(c)['stages']), 1)
        self.assertEqual(len(self.h.runs), 1)

    def test_scope_reask_hold_resumes_same_review(self):
        c = self.start(goal='project/result.txt 에 VERIFIED 한 줄을 저장하고 다시 읽어 확인한다. 이 파일 외에는 수정하지 않는다.')
        self.end_turn(self.g['id'], BODY + '\n상태: 완료 후보')
        self.h.hold_cycle(c['id'])
        self.end_turn(self.g['id'], base.SimpleGoalReviewTests.LIVE_REVIEW_OUT_OF_SCOPE)
        saved = self.cyc(c)
        self.assertEqual(saved['status'], 'paused')
        self.assertEqual(saved['heldStage']['kind'], 'review')
        self.assertEqual(saved['heldStage']['reask'], 1)
        self.assertTrue(saved['stages'][1]['scopeAsked'])
        self.assertFalse(self.h.runs)
        self.h.release_cycle(c['id'])
        self.assertEqual(self.h.store.messages(self.g['id'])[-1]['text'], saved['heldStage']['input'])
        self.end_turn(self.g['id'], '범위 확인: 목표 밖 — 원문은 project/result.txt만 언급함\n'
            '기준 1: 충족 — 바이트 덤프 56 45 52 49 46 49 45 44 0A, 폴더 목록 result.txt 하나\n판정: 통과')
        after = self.cyc(c)
        self.assertEqual(after['status'], 'done')
        self.assertEqual((after['round'], len(after['stages'])), (1, 2))

    def test_stopped_hold_retry_does_not_suggest_release(self):
        c = self.finish_held(); self.h.stop_cycle(c['id'])
        stopped = self.cyc(c)
        self.assertFalse(stopped.get('heldStage'))
        self.assertFalse(stopped.get('holdRequested'))
        self.assertEqual(stopped['why'], '')
        with self.assertRaises(ValueError) as raised: self.h.retry_cycle(c['id'])
        self.assertNotIn('release', str(raised.exception))

    def test_new_hold_clears_previous_resume_note(self):
        c=self.start(); self.h.hold_cycle(c['id'])
        self.end_turn(self.g['id'],BODY+'\n상태: 막힘 — 입력 필요')
        self.h.retry_cycle(c['id'])
        self.assertTrue(self.cyc(c)['resumeNote'])
        self.h.hold_cycle(c['id'])
        self.assertEqual(self.cyc(c)['resumeNote'],'')

    def test_shutdown_rejects_running_goal_without_run_but_accepts_held(self):
        c = self.finish_held()
        saved = copy.deepcopy(c)
        self.h.cycle_update(c['id'], lambda c: c.update(status='running'))
        self.assertFalse(runtime.begin_shutdown(self.h))
        self.assertFalse(getattr(self.h, 'shutting_down', False))
        self.h.store.put('cycles', [saved])
        self.assertTrue(runtime.begin_shutdown(self.h))
        with self.assertRaises(ValueError): self.h.release_cycle(c['id'])
        self.assertEqual(self.cyc(c), saved)


if __name__ == '__main__': unittest.main()
