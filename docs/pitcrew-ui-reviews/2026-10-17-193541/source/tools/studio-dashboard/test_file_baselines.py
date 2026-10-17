import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import file_baselines as fb
from hub import Hub

class FileBaselineTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name)/'project';self.root.mkdir()
        self.h=Hub(Path(self.tmp.name)/'runtime',root=self.root);self.aid=self.h.store.agents()[0]['id']
    def tearDown(self):self.h.close();self.h.store.db.close();self.tmp.cleanup()
    def save(self,paths):return self.h.file_baselines(dict(label='HUD 수정 전',agent=self.aid,paths=paths))['record']

    def test_dirty_git_status_same_but_contents_change(self):
        def git(*args):return subprocess.check_output(['git','-C',str(self.root),*args],stderr=subprocess.DEVNULL)
        git('init');p=self.root/'HUD.unity';p.write_text('baseline',encoding='utf-8');git('add','HUD.unity')
        p.write_text('dirty-v1',encoding='utf-8');status1=git('status','--porcelain=v1');hash1=git('hash-object','--no-filters','HUD.unity');before=self.save(['HUD.unity'])
        p.write_text('dirty-v2',encoding='utf-8');status2=git('status','--porcelain=v1');hash2=git('hash-object','--no-filters','HUD.unity')
        self.assertEqual(status1,status2);self.assertNotEqual(hash1,hash2)
        changes=self.h.file_baselines(bid=before['id']);self.assertEqual(changes['entries'][0]['status'],'changed')
        self.assertEqual(self.h.file_baselines()['items'][0],before)

    def test_create_delete_same_and_unavailable(self):
        (self.root/'a').write_text('old');(self.root/'b').write_text('same');(self.root/'folder').mkdir()
        before=self.save(['a','b','new','folder']);(self.root/'a').unlink();(self.root/'new').write_text('new')
        result=self.h.file_baselines(bid=before['id'])
        self.assertEqual([x['status'] for x in result['entries']],['deleted','same','created','unavailable'])

    def test_invalid_paths_and_metadata(self):
        for paths in ([],['../x'],['/x'],['C:\\x'],['x:stream'],['a/.git/config'],['a//b'],['x\nq'],['a']*21):
            with self.subTest(paths=paths),self.assertRaises(ValueError):self.save(paths)
        with self.assertRaises(ValueError):self.h.file_baselines(dict(label='',agent=self.aid,paths=['a']))
        with self.assertRaises(ValueError):self.h.file_baselines(dict(label='x',agent='other',paths=['a']))
        self.assertEqual(self.h.file_baselines()['items'],[])

    def test_sizes_and_read_time_limits_are_not_same(self):
        (self.root/'a').write_bytes(b'0123456789')
        before=self.save(['a'])
        with patch.object(fb,'MAX_FILE_BYTES',5):
            self.assertEqual(self.h.file_baselines(bid=before['id'])['entries'][0]['status'],'unavailable')
        with patch.object(fb.time,'monotonic',side_effect=[0,6]):
            self.assertEqual(fb.observe(self.root,['a'])['entries'][0]['status'],'unavailable')

    def test_all_unreadable_does_not_save(self):
        (self.root/'folder').mkdir()
        with self.assertRaises(ValueError):self.save(['folder'])
        self.assertEqual(self.h.file_baselines()['items'],[])

    def test_missing_baseline_can_detect_future_file_without_writes_on_compare(self):
        record=self.save(['new']);(self.root/'new').write_bytes(b'new content')
        before=self.h.store.db.total_changes
        self.assertEqual(self.h.file_baselines(bid=record['id'])['entries'][0]['status'],'created')
        self.assertEqual(self.h.store.db.total_changes,before)

    def test_absent_at_both_times_is_not_unchanged_file(self):
        record=self.save(['not-created'])
        self.assertEqual(self.h.file_baselines(bid=record['id'])['entries'][0]['status'],'missing')

    @unittest.skipUnless(__import__('os').name=='nt','Windows path aliases')
    def test_windows_device_names_and_path_aliases_rejected(self):
        for path in ['CON','aux.txt','sub/COM1','LPT¹.txt','CONIN$','a.txt.','sub /a.txt','a?.txt']:
            with self.subTest(path=path),self.assertRaises(ValueError):self.save([path])
        self.assertEqual(self.h.file_baselines()['items'],[])

    def test_reparse_point_rejected(self):
        (self.root/'a').write_bytes(b'x');original=Path.lstat
        class Reparse:
            st_mode=0o100644;st_file_attributes=0x400
        def lstat(p,*args,**kwargs):return Reparse() if p.name=='a' else original(p,*args,**kwargs)
        with patch.object(Path,'lstat',lstat):
            self.assertEqual(fb.observe(self.root,['a'])['entries'][0]['status'],'unavailable')

    def test_changed_during_read_not_reported_as_comparable(self):
        (self.root/'a').write_bytes(b'x')
        with patch.object(fb,'_signature',side_effect=['old','new']):
            self.assertEqual(fb.observe(self.root,['a'])['entries'][0]['status'],'unavailable')

    def test_scoped_to_project_and_root_identity(self):
        record=self.save(['a'])
        with tempfile.TemporaryDirectory() as d:
            other=Hub(d,root=self.root)
            try:
                with self.assertRaises(ValueError):other.file_baselines(bid=record['id'])
            finally:other.close();other.store.db.close()
        self.h.root=Path(self.tmp.name)
        with self.assertRaises(ValueError):self.h.file_baselines(bid=record['id'])

    def test_retention_limit_does_not_drop_old_records(self):
        record=self.save(['a']);self.h.store.put('fileBaselines',[record]*100)
        with self.assertRaises(ValueError):self.save(['b'])
        self.assertEqual(len(self.h.file_baselines()['items']),100)

    def test_delete_selected_metadata_frees_capacity_and_preserves_files(self):
        p=self.root/'a';p.write_bytes(b'unchanged content')
        first=self.save(['a']);second=self.save(['future'])
        self.assertEqual(first['agentName'],self.h.store.agent(self.aid)['name'])
        rows=[dict(first,id='record-'+str(i)) for i in range(99)]+[second]
        self.h.store.put('fileBaselines',rows)
        result=self.h.file_baselines({'action':'delete','id':second['id']})
        self.assertEqual(result['deleted'],second['id'])
        self.assertEqual(list(reversed(self.h.file_baselines()['items'])),rows[:-1])
        self.assertEqual(p.read_bytes(),b'unchanged content')
        self.save(['a']);self.assertEqual(len(self.h.file_baselines()['items']),100)

    def test_delete_unknown_or_foreign_record_never_writes(self):
        record=self.save(['a']);before=self.h.store.db.total_changes
        for data in [{'action':'delete'},{'action':'delete','id':[]},{'action':'delete','id':'foreign'},{'action':'overwrite'}]:
            with self.subTest(data=data),self.assertRaises(ValueError):self.h.file_baselines(data)
        self.assertEqual(self.h.store.db.total_changes,before)
        self.assertEqual(self.h.file_baselines()['items'],[record])
        with tempfile.TemporaryDirectory() as d:
            other=Hub(d,root=self.root)
            try:
                with self.assertRaises(ValueError):other.file_baselines({'action':'delete','id':record['id']})
            finally:other.close();other.store.db.close()

    def test_goal_link_filters_and_guards_compare_delete(self):
        self.h.store.put('cycles',[dict(id='old',goal='HUD 이전 작업',stages=[]),dict(id='new',goal='HUD 다음 작업',stages=[])])
        unlinked=self.save(['a'])
        linked=self.h.file_baselines(dict(goal='old',label='작업 기준',agent=self.aid,paths=['a']))['record']
        self.assertEqual(linked['goalText'],'HUD 이전 작업')
        self.assertEqual(self.h.file_baselines(goal='old')['items'],[linked])
        self.assertEqual(self.h.file_baselines(goal='new')['items'],[])
        self.assertEqual(self.h.file_baselines(bid=linked['id'],goal='old')['entries'][0]['status'],'missing')
        before=self.h.store.db.total_changes
        for goal in ['new','unknown']:
            with self.assertRaises(ValueError):self.h.file_baselines(bid=linked['id'],goal=goal)
            with self.assertRaises(ValueError):self.h.file_baselines({'action':'delete','id':linked['id'],'goal':goal})
        with self.assertRaises(ValueError):self.h.file_baselines(dict(goal='unknown',label='no',agent=self.aid,paths=['a']))
        self.assertEqual(self.h.store.db.total_changes,before)
        self.h.file_baselines({'action':'delete','id':linked['id'],'goal':'old'})
        self.assertEqual(self.h.file_baselines()['items'],[unlinked])

if __name__=='__main__':unittest.main()
