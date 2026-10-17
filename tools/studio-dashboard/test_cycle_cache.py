import json
import sqlite3
import tempfile
import unittest
from contextlib import closing
from pathlib import Path
from hub import Hub


class CycleCacheTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.h=Hub(self.tmp.name)
        self.rows=[dict(id='c',goal='HUD',status='done',stages=[dict(report='원문',input='자료',criteria=[dict(evidence='근거')])])]
        self.h.store.put('cycles',self.rows)

    def tearDown(self):
        self.h.close();self.h.store.db.close();self.tmp.cleanup()

    def test_own_write_refreshes_list_detail_and_report(self):
        self.h.goal_history();self.h.stage_report('c',0)
        self.rows[0]['goal']='변경';self.rows[0]['stages'][0]['report']='새 보고'
        self.h.store.put('cycles',self.rows)
        self.assertEqual(self.h.goal_history()['items'][0]['goal'],'변경')
        self.assertEqual(self.h.goal_history(cid='c')['cycle']['goal'],'변경')
        self.assertEqual(self.h.stage_report('c',0)['stage']['report'],'새 보고')

    def test_external_update_delete_reinsert_and_replace(self):
        self.h.stage_report('c',0)
        with closing(sqlite3.connect(Path(self.tmp.name)/'hub.sqlite3')) as db:
            for sql in ("UPDATE settings SET body=? WHERE key='cycles'", "INSERT OR REPLACE INTO settings(key,body) VALUES('cycles',?)"):
                self.rows[0]['goal']+=' 갱신';db.execute(sql,(json.dumps(self.rows),));db.commit()
                self.assertEqual(self.h.goal_history()['items'][0]['goal'],self.rows[0]['goal'])
            db.execute("DELETE FROM settings WHERE key='cycles'");db.commit()
            self.assertEqual(self.h.goal_history()['total'],0)
            with self.assertRaises(ValueError):self.h.stage_report('c',0)
            db.execute("INSERT INTO settings VALUES('cycles',?)",(json.dumps(self.rows),));db.commit()
            self.assertEqual(self.h.stage_report('c',0)['stage']['report'],'원문')

    def test_uncommitted_read_rollback_revision_reuse_is_not_stale(self):
        self.h.goal_history()
        db=self.h.store.db
        changed=[dict(self.rows[0],goal='취소할 값')]
        db.execute("UPDATE settings SET body=? WHERE key='cycles'",(json.dumps(changed),))
        self.assertEqual(self.h.goal_history()['items'][0]['goal'],'취소할 값')
        db.rollback()
        changed[0]['goal']='확정 값'
        db.execute("UPDATE settings SET body=? WHERE key='cycles'",(json.dumps(changed),));db.commit()
        self.assertEqual(self.h.goal_history()['items'][0]['goal'],'확정 값')

    def test_returned_objects_do_not_mutate_stored_or_cached_records(self):
        self.h.cycle('c')['stages'][0]['report']='손상'
        self.h.goal_history(cid='c')['cycle']['stages'][0]['criteria'].clear()
        self.h.store.cycle_summaries()[0]['stages'].clear()
        self.assertEqual(self.h.stage_report('c',0)['stage']['report'],'원문')
        self.assertEqual(len(self.h.goal_history(cid='c')['cycle']['stages'][0]['criteria']),1)

    def test_message_writes_do_not_reread_large_cycle_blob(self):
        self.h.goal_history();queries=[];db=self.h.store.db
        db.set_trace_callback(queries.append)
        try:
            aid=self.h.store.agents()[0]['id']
            for _ in range(10):
                self.h.store.message(aid,'assistant','진행 중')
                self.h.goal_history();self.h.stage_report('c',0);self.h.store.cycle_summaries(6)
        finally:db.set_trace_callback(None)
        self.assertFalse(any("SELECT body FROM settings WHERE key='cycles'" in q for q in queries))

    def test_read_does_not_write_and_reopen_keeps_original_data(self):
        before=self.h.store.db.total_changes
        self.h.goal_history();self.h.stage_report('c',0)
        self.assertEqual(self.h.store.db.total_changes,before)
        self.h.store.db.close()
        self.h.store.db=sqlite3.connect(Path(self.tmp.name)/'hub.sqlite3')
        self.assertEqual(self.h.store.get('cycles'),self.rows)
        self.assertEqual(self.h.store.db.execute('PRAGMA integrity_check').fetchone()[0],'ok')

    def test_legacy_database_migration_preserves_cycles(self):
        with tempfile.TemporaryDirectory() as directory:
            db=sqlite3.connect(Path(directory)/'hub.sqlite3')
            db.execute('CREATE TABLE settings(key TEXT PRIMARY KEY,body TEXT NOT NULL)')
            raw=json.dumps(self.rows,ensure_ascii=False)
            db.execute('INSERT INTO settings VALUES(?,?)',('cycles',raw));db.commit();db.close()
            h=Hub(directory)
            try:
                self.assertEqual(h.store.db.execute("SELECT body FROM settings WHERE key='cycles'").fetchone()[0],raw)
                self.assertEqual(h.stage_report('c',0)['stage']['report'],'원문')
            finally:h.close();h.store.db.close()

    def test_same_revision_same_length_backup_restore_refreshes_report(self):
        self.h.stage_report('c',0)
        with closing(sqlite3.connect(':memory:')) as backup:
            self.h.store.db.backup(backup)
            changed=json.loads(json.dumps(self.rows));changed[0]['stages'][0]['report']='다름'
            self.assertEqual(len(json.dumps(changed)),len(json.dumps(self.rows)))
            self.h.store.put('cycles',changed)
            self.assertEqual(self.h.stage_report('c',0)['stage']['report'],'다름')
            revision=self.h.store.db.execute('SELECT version FROM cycle_revision').fetchone()[0]
            backup.execute('UPDATE cycle_revision SET version=?',(revision,));backup.commit()
            with closing(sqlite3.connect(Path(self.tmp.name)/'hub.sqlite3')) as dest:
                backup.backup(dest)
            self.assertEqual(self.h.stage_report('c',0)['stage']['report'],'원문')

    def test_missing_revision_row_reads_fresh_without_writing(self):
        self.h.stage_report('c',0)
        with closing(sqlite3.connect(Path(self.tmp.name)/'hub.sqlite3')) as db:
            db.execute('DELETE FROM cycle_revision');db.commit()
            for goal in ('first','second'):
                self.rows[0]['goal']=goal
                db.execute("UPDATE settings SET body=? WHERE key='cycles'",(json.dumps(self.rows),));db.commit()
                before=self.h.store.db.total_changes
                self.assertEqual(self.h.goal_history()['items'][0]['goal'],goal)
                self.assertEqual(self.h.store.db.total_changes,before)
            db.execute('INSERT INTO cycle_revision VALUES(1,1)');db.commit()
            self.assertEqual(self.h.goal_history()['items'][0]['goal'],'second')


if __name__=='__main__':unittest.main()
