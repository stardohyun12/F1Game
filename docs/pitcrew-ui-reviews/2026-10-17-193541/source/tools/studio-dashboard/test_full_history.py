import json,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
from hub import Hub,Sources,codex_permissions
class FullHistoryTests(unittest.TestCase):
 def test_full_log_large_line_and_message(self):
  with tempfile.TemporaryDirectory() as tmp:
   p=Path(tmp)/'log.jsonl'
   rows=[dict(type='response_item',timestamp=f'2026-01-01T00:00:{i%60:02d}Z',payload=dict(type='message',role='user',content=[dict(type='input_text',text=str(i)+'x'*25000)])) for i in range(170)]
   rows.insert(80,dict(type='ignored',data='x'*1100000))
   p.write_text('\n'.join(map(json.dumps,rows))+'\n{partial',encoding='utf-8')
   src=Sources(tmp)
   with patch.object(src,'get',return_value={'provider':'codex','path':str(p)}):
    result=src.messages('test');self.assertEqual(len(result),170);self.assertTrue(result[0]['text'].startswith('0x'));self.assertGreater(len(result[0]['text']),24000)
 def test_backfill_existing_import_and_all_local_messages(self):
  with tempfile.TemporaryDirectory() as tmp:
   h=Hub(tmp)
   try:
    a=h.store.agents()[0];aid=a['id'];h.store.update(aid,originSource='native',session='same')
    h.store.put('original-history:'+aid,[dict(id='last',role='user',text='last',ts=10)])
    with patch.object(h.sources,'messages',return_value=[dict(id='first',role='user',text='first',ts=1),dict(id='last',role='user',text='last',ts=10)]),patch.object(h,'children',return_value=[]):
     self.assertEqual([m['id'] for m in h.detail(aid)['messages']],['first','last'])
    h.store.update(aid,originSource=None)
    for i in range(305):h.store.message(aid,'user',str(i))
    with patch.object(h,'children',return_value=[]):self.assertEqual(len(h.detail(aid)['messages']),305)
    self.assertEqual(h.save_agent({'id':aid,'mode':'full'})['mode'],'full')
    self.assertEqual(h.store.agent(aid)['session'],'same')
   finally:h.close();h.store.db.close()
 def test_permissions(self):
  self.assertEqual(codex_permissions('full'),{'approvalPolicy':'never','sandbox':'danger-full-access'})
  self.assertEqual(codex_permissions('read')['sandbox'],'read-only')
  self.assertEqual(codex_permissions('work')['approvalPolicy'],'untrusted')
if __name__=='__main__':unittest.main()
