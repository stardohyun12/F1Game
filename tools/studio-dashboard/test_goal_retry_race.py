"""Concurrent retry regression: real Hub/SQLite, no provider execution."""
import threading
import time
import unittest
from unittest.mock import patch
import test_simple_goal as base


class GoalRetryRaceTests(unittest.TestCase):
    setUp = base.SimpleGoalTests.setUp
    tearDown = base.SimpleGoalTests.tearDown
    start = base.SimpleGoalTests.start
    end_turn = base.SimpleGoalTests.end_turn
    cyc = base.SimpleGoalTests.cyc

    def test_concurrent_retry_schedules_only_one_turn(self):
        c = self.start()
        self.end_turn(self.g['id'], base.BODY + '\n상태: 막힘 — 입력 필요')
        original = self.h.cycle
        def delayed_snapshot(cid):
            snapshot = original(cid)
            if snapshot['status'] == 'paused': time.sleep(.05)
            return snapshot
        gate = threading.Barrier(3)
        outcomes = []
        def retry():
            gate.wait()
            try: self.h.retry_cycle(c['id']); outcomes.append('ok')
            except ValueError: outcomes.append('rejected')
        with patch.object(self.h, 'cycle', delayed_snapshot):
            threads = [threading.Thread(target=retry) for _ in range(2)]
            for thread in threads: thread.start()
            gate.wait()
            for thread in threads:
                thread.join(5)
                self.assertFalse(thread.is_alive())
        self.assertCountEqual(outcomes, ['ok', 'rejected'])
        after = self.cyc(c)
        self.assertEqual(after['round'], 2)
        self.assertEqual(len(after['stages']), 2)
        self.assertEqual(len(self.h.runs), 1)
        self.assertEqual(self.h.store.agent(self.g['id']).get('queue', []), [])


if __name__ == '__main__': unittest.main()
