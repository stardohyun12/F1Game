import concurrent.futures
import tempfile
import unittest
from hub import Hub

class MembershipTests(unittest.TestCase):
    def setUp(self):
        self.folder=tempfile.TemporaryDirectory()
        self.hub=Hub(self.folder.name)
        self.agents=self.hub.store.agents()
        self.team=self.hub.save_team({'name':'Shared','members':[],'context':'Keep this goal'})
    def tearDown(self):
        self.hub.close();self.hub.store.db.close();self.folder.cleanup()
    def test_concurrent_additions_keep_both_members_and_metadata(self):
        def add(a):return self.hub.change_team_member({'team':self.team['id'],'agent':a['id']})
        with concurrent.futures.ThreadPoolExecutor(2) as pool:
            list(pool.map(add,self.agents[:2]))
        saved=self.hub.store.get('teams')[0]
        self.assertEqual(set(saved['members']),{a['id'] for a in self.agents[:2]})
        self.assertEqual(saved['context'],'Keep this goal')
    def test_repeated_add_remove_are_idempotent(self):
        data={'team':self.team['id'],'agent':self.agents[0]['id']}
        self.hub.change_team_member(data);self.hub.change_team_member(data)
        self.assertEqual(len(self.hub.store.get('teams')[0]['members']),1)
        self.hub.change_team_member({**data,'remove':True});self.hub.change_team_member({**data,'remove':True})
        self.assertEqual(self.hub.store.get('teams')[0]['members'],[])
        self.assertIsNotNone(self.hub.store.agent(data['agent']))
    def test_archived_member_is_rejected(self):
        aid=self.agents[0]['id'];self.hub.archive(aid)
        with self.assertRaises(ValueError):self.hub.change_team_member({'team':self.team['id'],'agent':aid})
        self.assertEqual(self.hub.store.get('teams')[0]['members'],[])

if __name__=='__main__':unittest.main()
