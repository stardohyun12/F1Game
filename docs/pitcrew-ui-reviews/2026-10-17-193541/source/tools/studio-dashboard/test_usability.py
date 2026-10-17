import tempfile, unittest, concurrent.futures
from unittest.mock import patch
from hub import Hub
class UsabilityTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.h=Hub(self.tmp.name);self.a=self.h.store.agents()[0];self.aid=self.a['id']
 def tearDown(self):
  self.h.runs.clear();self.h.close();self.h.store.db.close();self.tmp.cleanup()
 def test_busy_name_only(self):
  self.h.runs[self.aid]=object();self.h.store.update(self.aid,status='running')
  self.assertEqual(self.h.save_agent({'id':self.aid,'name':'Renamed'})['name'],'Renamed')
  for field in ['role','model','mode','peers','instructions']:
   with self.assertRaises(ValueError):self.h.save_agent({'id':self.aid,field:[] if field=='peers' else 'changed'})
  self.assertEqual(self.h.store.agent(self.aid)['status'],'running')
 def test_session_import_reuses_and_restores(self):
  self.h.store.update(self.aid,session='session',archived=True)
  source={'id':'src','provider':self.a['provider'],'parent':None,'name':'source'}
  with patch.object(self.h.sources,'get',return_value=source),patch.object(self.h.sources,'native_session',return_value='session'),patch.object(self.h.sources,'public',return_value=[source]):
   self.assertEqual(self.h.source_catalog()[0]['connectedAgent']['id'],self.aid)
   with concurrent.futures.ThreadPoolExecutor(4) as pool: rows=list(pool.map(lambda _:self.h.watch('src'),range(8)))
   self.assertEqual({a['id'] for a in rows},{self.aid});self.assertEqual(len(self.h.store.agents()),3)
   team=self.h.save_team({'name':'T','members':[]})
   result=self.h.import_sources({'sources':['src'],'team':team['id']})
   self.assertEqual(result['team']['members'],[self.aid]);self.assertEqual(result['agents'][0]['name'],self.a['name'])
 def test_session_provider_is_part_of_identity(self):
  self.h.store.update(self.aid,session='session')
  with patch.object(self.h.sources,'get',return_value={'provider':'codex','parent':None,'name':'S'}),patch.object(self.h.sources,'native_session',return_value='session'):
   self.assertIsNone(self.h.source_agent('src'))
 def test_continue_settings_teams_reference_not_session(self):
  self.h.store.update(self.aid,session='old',model='model',mode='full',context='private',error='active writer')
  teams=[self.h.save_team({'name':str(i),'members':[self.aid]}) for i in range(2)]
  fresh=self.h.continue_conversation(self.aid)
  self.assertIsNone(fresh['session']);self.assertEqual(fresh['mode'],'full');self.assertEqual(fresh['model'],'model');self.assertEqual(fresh['context'],'private');self.assertEqual(fresh['peers'],[self.aid]);self.assertEqual(fresh['status'],'idle')
  self.assertTrue(all(fresh['id'] in t['members'] for t in self.h.store.get('teams')))
  self.assertEqual(self.h.store.agent(self.aid)['session'],'old');self.assertEqual(self.h.store.messages(fresh['id']),[])
 def test_continue_running_rejected_without_creation(self):
  self.h.runs[self.aid]=object()
  with self.assertRaises(ValueError):self.h.continue_conversation(self.aid)
  self.assertEqual(len(self.h.store.agents()),3)
if __name__=='__main__':unittest.main()
