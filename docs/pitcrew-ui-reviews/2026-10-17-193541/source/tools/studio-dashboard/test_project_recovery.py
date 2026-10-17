"""Secondary project reopen recovery: real SQLite, provider dispatch forbidden."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import sqlite3
import tempfile
import unittest
from unittest.mock import patch

import hub


class ProjectRecoveryTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.base = Path(self.tmp.name)
        self.project_root = self.base / 'game'
        self.project_root.mkdir()
        (self.project_root / 'HUD.cs').write_text('before', encoding='utf-8')
        self.hubs = []
        self.send = patch.object(hub.Hub, 'send', side_effect=AssertionError('Unexpected AI dispatch')).start()
        self.alert = patch.object(hub.Hub, 'cycle_alert').start()
        self.addCleanup(patch.stopall)
        self.addCleanup(self.cleanup)
        self.owner = self.open_owner()
        self.pid = self.owner.add_project({'path': str(self.project_root)})['id']
        child = self.owner.project(self.pid)
        a = child.store.create(name='test agent', provider='claude')
        self.aid = a['id']
        with patch.object(child, 'send'):
            self.goal = child.start_cycle(dict(mode='goal', agent=self.aid, goal='HUD test', baselinePaths=['HUD.cs']))
        child.cycle_update(self.goal['id'], lambda c: c['stages'][0].update(status='running'))
        child.store.update(self.aid, status='running', session='fixture-session')
        child.store.message(self.aid, 'user', 'original task')
        self.before_messages = child.store.messages(self.aid)
        self.before_baselines = child.store.get('fileBaselines', [])
        self.reply = {'cycle': self.goal['id'], 'stage': 0}

    def open_owner(self):
        owner = hub.Hub(self.base / 'runtime', root=self.base)
        self.hubs.append(owner)
        return owner

    def close_owner(self, owner):
        owner.close()
        for child in owner.project_hubs.values(): child.store.db.close()
        owner.store.db.close()
        self.hubs.remove(owner)

    def reopen(self):
        self.close_owner(self.owner)
        self.owner = self.open_owner()
        return self.owner.project(self.pid)

    def cleanup(self):
        for owner in list(self.hubs): self.close_owner(owner)
        self.tmp.cleanup()

    def test_first_open_recovers_cut_goal_preserving_session_history_and_baseline(self):
        child = self.reopen()
        c = child.cycle(self.goal['id'])
        self.assertEqual((c['status'], c['stages'][0]['status']), ('paused', 'interrupted'))
        self.assertEqual(len(c['stages']), len(self.goal['stages']))
        self.assertEqual(child.store.agent(self.aid)['session'], 'fixture-session')
        self.assertEqual(child.store.agent(self.aid)['status'], 'interrupted')
        self.assertEqual(child.store.messages(self.aid), self.before_messages)
        self.assertEqual(child.store.get('fileBaselines', []), self.before_baselines)
        self.send.assert_not_called()
        self.assertEqual(self.alert.call_count, 1)

    def test_queued_stage_is_preserved_without_automatic_send(self):
        child = self.owner.project(self.pid)
        child.cycle_update(self.goal['id'], lambda c: c['stages'][0].update(status='queued'))
        queue = [dict(id='queued', text='pending input', replyTo=self.reply)]
        child.store.update(self.aid, queue=queue)
        child = self.reopen()
        self.assertEqual(child.cycle(self.goal['id'])['status'], 'running')
        self.assertEqual(child.store.agent(self.aid)['queue'], queue)
        self.send.assert_not_called()
        self.alert.assert_not_called()

    def test_queued_stage_with_no_remaining_message_is_paused(self):
        self.owner.project(self.pid).cycle_update(self.goal['id'], lambda c: c['stages'][0].update(status='queued'))
        child = self.reopen()
        self.assertEqual(child.cycle(self.goal['id'])['status'], 'paused')
        self.send.assert_not_called()

    def test_pending_member_reply_is_preserved_without_automatic_send(self):
        self.owner.project(self.pid).store.update(self.aid, pendingReplies=[self.reply])
        child = self.reopen()
        self.assertEqual(child.cycle(self.goal['id'])['status'], 'running')
        self.assertEqual(child.store.agent(self.aid)['pendingReplies'], [self.reply])
        self.send.assert_not_called()
        self.alert.assert_not_called()

    def test_retry_same_goal_once_and_later_project_lookups_do_not_pause_it(self):
        child = self.reopen()
        with patch.object(child, 'send') as sent:
            c = child.retry_cycle(self.goal['id'])
            with self.assertRaises(ValueError): child.retry_cycle(c['id'])
            self.assertEqual(sent.call_count, 1)
            self.assertEqual(sent.call_args.args[0], self.aid)
            self.assertEqual(c['stages'][-1]['input'], self.goal['stages'][0]['input'])
            self.assertEqual(c['id'], self.goal['id'])
            self.assertIs(self.owner.project(self.pid), child)
            self.assertEqual(child.cycle(c['id'])['status'], 'running')

    def test_concurrent_first_open_recovers_only_once(self):
        self.close_owner(self.owner)
        self.owner = self.open_owner()
        with ThreadPoolExecutor(max_workers=4) as pool:
            children = list(pool.map(self.owner.project, [self.pid] * 4))
        self.assertTrue(all(c is children[0] for c in children))
        self.assertEqual(children[0].cycle(self.goal['id'])['status'], 'paused')
        self.assertEqual(self.alert.call_count, 1)
        self.send.assert_not_called()

    def test_stage_missing_status_does_not_block_other_goals_or_project_access(self):
        child = self.owner.project(self.pid)
        bad = dict(id='damaged', mode='goal', goal='damaged record', status='running',
                   stages=[dict(kind='build')])
        child.store.put('cycles', [bad] + child.store.get('cycles', []))
        child = self.reopen()
        self.assertEqual(child.cycle(self.goal['id'])['status'], 'paused')
        self.assertEqual(child.cycle('damaged'), bad)  # do not invent a stage state or discard history
        warnings = [x for x in child.store.feed() if 'damaged' in x['text']]
        self.assertEqual(len(warnings), 1)
        self.assertIn('단계 상태', warnings[0]['text'])
        self.assertIs(self.owner.project(self.pid), child)
        self.assertEqual([x for x in child.store.feed() if 'damaged' in x['text']], warnings)
        self.send.assert_not_called()

    def test_failed_load_closes_uncached_database_and_allows_later_retry(self):
        self.close_owner(self.owner)
        self.owner = self.open_owner()
        connections = []
        def fail(child):
            connections.append(child.store.db)
            raise OSError('fixture read failure')
        with patch.object(hub.Hub, 'recover_cycles', fail):
            with self.assertRaises(OSError): self.owner.project(self.pid)
        self.assertNotIn(self.pid, self.owner.project_hubs)
        with self.assertRaises(sqlite3.ProgrammingError): connections[0].execute('SELECT 1')
        child = self.owner.project(self.pid)
        self.assertEqual(child.cycle(self.goal['id'])['status'], 'paused')
        self.send.assert_not_called()


if __name__ == '__main__': unittest.main()
