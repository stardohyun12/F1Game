import tempfile
import unittest
from unittest.mock import patch
from hub import Hub

class ExternalSyncTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.h=Hub(self.tmp.name)
        self.a=self.h.store.agents()[0];self.h.store.update(self.a['id'],originSource='native',session='original')
        self.h.store.put('original-history:'+self.a['id'],[dict(id='old',role='user',text='old',ts=10)])
    def tearDown(self):
        self.h.close();self.h.store.db.close();self.tmp.cleanup()
    def test_external_records_append_without_duplicate_or_losing_provenance(self):
        local=self.h.store.message(self.a['id'],'user','local',context={'snapshots':[]})
        external=dict(id='new',role='assistant',text='external app reply',ts=local['ts']+1)
        with patch.object(self.h.sources,'messages',return_value=[dict(id='old',ts=10),dict(id='native-local',ts=local['ts']-.1),external]),patch.object(self.h,'children',return_value=[]):
            for _ in range(2):
                result=self.h.detail(self.a['id'])['messages']
                self.assertEqual([m['id'] for m in result],['old',local['id'],'new'])
                self.assertEqual(result[1]['context'],{'snapshots':[]})
    def test_missing_log_preserves_saved_history(self):
        with patch.object(self.h.sources,'messages',side_effect=OSError('missing')),patch.object(self.h,'children',return_value=[]):
            self.assertEqual(self.h.detail(self.a['id'])['messages'][0]['text'],'old')

if __name__=='__main__':unittest.main()
