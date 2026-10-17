import tempfile
import unittest
from unittest.mock import patch
import hub

class GoalTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.h=hub.Hub(self.tmp.name)
        self.a=self.h.store.create(name='goal',provider='codex')
        self.h.store.update(self.a['id'],session='fixture')
    def tearDown(self):
        self.h.close();self.h.store.db.close();self.tmp.cleanup()
    def test_set_and_read_back_before_caching(self):
        goal={'objective':'checked','status':'active','tokensUsed':12}
        with patch.object(hub,'Rpc') as rpc:
            rpc.return_value.call.side_effect=[{}, {'goal':goal}]
            self.assertEqual(self.h.goal(self.a['id'],{'objective':'checked'})['goal'],goal)
            self.assertEqual([c.args[0] for c in rpc.return_value.call.call_args_list],['thread/goal/set','thread/goal/get'])
            self.assertEqual(self.h.store.agent(self.a['id'])['goal'],goal)
    def test_failure_does_not_cache_claimed_success(self):
        with patch.object(hub,'Rpc') as rpc:
            rpc.return_value.call.side_effect=RuntimeError('unsupported')
            with self.assertRaises(RuntimeError):self.h.goal(self.a['id'],{'objective':'x'})
            self.assertFalse(self.h.store.agent(self.a['id']).get('goal'))
    def test_invalid_or_new_session_does_not_start_ai(self):
        with patch.object(hub,'Rpc') as rpc:
            with self.assertRaises(ValueError):self.h.goal(self.a['id'],{'objective':''})
            self.h.store.update(self.a['id'],session='')
            with self.assertRaises(ValueError):self.h.goal(self.a['id'])
            rpc.assert_not_called()

if __name__=='__main__':unittest.main()
