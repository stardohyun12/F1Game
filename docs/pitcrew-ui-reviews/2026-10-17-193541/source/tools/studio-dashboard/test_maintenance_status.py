"""Maintenance observation and shutdown use the same conservative state predicate."""
import copy
import tempfile
import unittest
from pathlib import Path
from hub import Hub
from runtime import maintenance_status, begin_shutdown


class MaintenanceStatusTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.h = Hub(self.root/'runtime',root=self.root)
        self.folder = self.root/'other'; self.folder.mkdir()
        self.pid = self.h.add_project({'path':str(self.folder)})['id']

    def tearDown(self):
        self.h.close()
        for h in self.h.project_hubs.values(): h.store.db.close()
        self.h.store.db.close(); self.tmp.cleanup()

    def test_empty_unopened_project_does_not_get_created_or_block(self):
        path = self.h.directory/'projects'/self.pid
        self.assertFalse(path.exists())
        snapshot = maintenance_status(self.h)
        self.assertTrue(snapshot['ready'])
        self.assertEqual(len(snapshot['projects']),2)
        self.assertFalse(snapshot['projects'][1]['loaded'])
        self.assertFalse(path.exists()); self.assertEqual(self.h.project_hubs,{})

    def test_each_blocker_matches_shutdown_and_keeps_state(self):
        child = self.h.project(self.pid)
        agent = child.store.agents()[0]
        cases = [dict(runs={'fake':object()}),dict(post_runs=1),dict(pending_requests=1),
                 dict(cycles=[dict(id='goal',status='running')]),dict(queue=[dict(id='q')]),
                 dict(pendingReplies=[dict(cycle='goal')])]
        for case in cases:
            with self.subTest(case=list(case)):
                child.runs=case.get('runs',{}); child.post_runs=case.get('post_runs',0)
                self.h.pending_requests=case.get('pending_requests',0)
                child.store.put('cycles',case.get('cycles',[]))
                child.store.update(agent['id'],queue=case.get('queue',[]),pendingReplies=case.get('pendingReplies',[]))
                before=copy.deepcopy(child.store.agent(agent['id']))
                result=maintenance_status(self.h)
                self.assertFalse(result['ready'])
                self.assertTrue(result['blockers'] or result['projects'][1]['blockers'])
                self.assertFalse(begin_shutdown(self.h))
                self.assertFalse(getattr(child,'shutting_down',False))
                self.assertEqual(child.store.agent(agent['id']),before)
        child.runs={}; child.post_runs=0; self.h.pending_requests=0
        child.store.update(agent['id'],queue=[],pendingReplies=[])
        child.store.put('cycles',[dict(id='held',status='paused',heldStage={'kind':'review'})])
        self.assertTrue(maintenance_status(self.h)['ready'])
        self.assertEqual(maintenance_status(self.h)['projects'][1]['heldGoals'],['held'])
        self.assertTrue(begin_shutdown(child))
        self.assertTrue(self.h.shutting_down); self.assertTrue(child.shutting_down)

    def test_unopened_existing_store_is_read_without_recovery(self):
        child=self.h.project(self.pid)
        agent=child.store.agents()[0]
        child.store.update(agent['id'],status='running',queue=[dict(id='keep')])
        child.store.put('cycles',[dict(id='unfinished',status='running')])
        child.close(); child.store.db.close(); del self.h.project_hubs[self.pid]
        path=self.h.directory/'projects'/self.pid/'hub.sqlite3'
        before=path.read_bytes()
        snapshot=maintenance_status(self.h)
        self.assertFalse(snapshot['ready'])
        row=snapshot['projects'][1]
        self.assertFalse(row['loaded']); self.assertEqual(row['runningGoals'],['unfinished'])
        self.assertEqual(row['queuedMessages'],1)
        self.assertEqual(path.read_bytes(),before)
        self.assertNotIn(self.pid,self.h.project_hubs)
        self.assertFalse(begin_shutdown(self.h))

    def test_unreadable_store_blocks_instead_of_guessing_idle(self):
        path=self.h.directory/'projects'/self.pid/'hub.sqlite3'
        path.parent.mkdir(parents=True); path.write_bytes(b'broken database')
        snapshot=maintenance_status(self.h)
        self.assertFalse(snapshot['ready'])
        self.assertIn('state-unreadable',snapshot['projects'][1]['blockers'])
        self.assertFalse(begin_shutdown(self.h))


if __name__ == '__main__': unittest.main()
