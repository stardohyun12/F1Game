import tempfile
import unittest
from hub import Hub

class TeamLinksTests(unittest.TestCase):
    def setUp(self):
        self.folder=tempfile.TemporaryDirectory();self.h=Hub(self.folder.name)
        self.a,self.b,self.out=self.h.store.agents()
        self.t=self.h.save_team(dict(name='Team',members=[self.a['id'],self.b['id']]))
    def tearDown(self):
        self.h.close();self.h.store.db.close();self.folder.cleanup()
    def save(self,links,**extra):
        return self.h.save_team_links(dict(team=self.t['id'],members=self.t['members'],links=links,**extra))
    def test_bidirectional_and_preserve_outside(self):
        self.h.store.update(self.a['id'],peers=[self.out['id']])
        self.save({self.a['id']:[self.b['id']],self.b['id']:[self.a['id']]})
        self.assertEqual(self.h.store.agent(self.a['id'])['peers'],[self.out['id'],self.b['id']])
        self.assertEqual(self.h.store.agent(self.b['id'])['peers'],[self.a['id']])
    def test_invalid_second_receiver_rolls_back_first(self):
        with self.assertRaises(ValueError):self.save({self.a['id']:[self.b['id']],self.b['id']:[self.out['id']]})
        self.assertEqual(self.h.store.agent(self.a['id'])['peers'],[])
    def test_stale_team_rejected(self):
        self.h.change_team_member(dict(team=self.t['id'],agent=self.out['id']))
        with self.assertRaises(ValueError):self.save({self.a['id']:[self.b['id']]})
    def test_readonly_and_self_rejected(self):
        with self.assertRaises(ValueError):self.save({self.a['id']:[self.a['id']]})
        self.h.store.update(self.a['id'],source='record')
        with self.assertRaises(ValueError):self.save({self.a['id']:[self.b['id']]})
    def test_limit_checks_outside_connections_before_write(self):
        outside=[self.h.store.create()['id'] for _ in range(8)]
        self.h.store.update(self.b['id'],peers=outside)
        with self.assertRaises(ValueError):self.save({self.a['id']:[self.b['id']],self.b['id']:[self.a['id']]})
        self.assertEqual(self.h.store.agent(self.a['id'])['peers'],[])

if __name__=='__main__':unittest.main()
