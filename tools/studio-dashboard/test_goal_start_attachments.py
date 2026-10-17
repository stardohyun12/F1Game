"""Goal-start attachments and baselines use real temporary files; no provider calls."""
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
from hub import Hub


class GoalStartAttachmentsTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name)
        self.uploads=self.root/'uploads';self.uploads.mkdir()
        self.file=self.uploads/'before.png';self.file.write_bytes(b'isolated attachment bytes')
        self.attachments=[dict(path=str(self.file),name='before.png')]
        (self.root/'HUD.cs').write_text('before',encoding='utf-8')
        self.h=Hub(self.root/'runtime',root=self.root)
        self.a=self.h.store.agents()[0]['id'];self.b=self.h.store.create(name='QA',provider='claude')['id']
        self.team=self.h.save_team(dict(name='Build',members=[self.a],lead=self.a))
        self.qa=self.h.save_team(dict(name='QA',members=[self.b],lead=self.b))
        self.patches=[patch('hub.UPLOADS',self.uploads),patch.object(self.h,'send'),patch.object(self.h,'cycle_alert')]
        for p in self.patches:p.start()
    def tearDown(self):
        for p in reversed(self.patches):p.stop()
        self.h.close();self.h.store.db.close();self.tmp.cleanup()
    def start(self,team=False,**extra):
        data=dict(goal='HUD layout',baselinePaths=['HUD.cs'],attachments=self.attachments)
        data.update(dict(teams=dict(research=self.team['id'],build=self.team['id'],qa=self.qa['id'])) if team else dict(mode='goal',agent=self.a))
        return self.h.start_cycle({**data,**extra})
    def test_personal_and_team_have_baseline_before_first_attachment_dispatch(self):
        for team in (False,True):
            seen=[]
            def send(aid,payload):
                c=self.h.cycle(payload['replyTo']['cycle'])
                self.assertEqual(len(self.h.file_baselines(goal=c['id'])['items']),1)
                self.assertEqual(payload['attachments'],self.attachments)
                self.assertIn(str(self.file),self.h.prepare_message(payload)['text'])
                seen.append(payload)
            self.h.send.side_effect=send
            c=self.start(team);self.assertEqual(len(seen),1)
            self.h.cycle_update(c['id'],lambda x:x.update(status='stopped'))
    def test_team_retry_keeps_attachments_but_later_build_does_not_repeat_them(self):
        self.h.send.side_effect=ValueError('first handoff refused')
        c=self.start(True);self.assertEqual(c['status'],'failed')
        self.assertEqual(c['startAttachments'],self.attachments)
        self.h.send.side_effect=None;self.h.send.reset_mock();c=self.h.retry_cycle(c['id'])
        self.assertEqual(self.h.send.call_args.args[1]['attachments'],self.attachments)
        self.h.cycle_stage(c['id'],'build','research report',self.a)
        self.assertNotIn('attachments',self.h.send.call_args.args[1])
    def test_missing_attachment_creates_neither_goal_nor_baseline(self):
        self.file.unlink()
        for team in (False,True):
            with self.assertRaises(ValueError):self.start(team)
            self.assertEqual(self.h.store.get('cycles',[]),[])
            self.assertEqual(self.h.file_baselines()['items'],[])
        self.h.send.assert_not_called()
    def test_personal_interrupted_first_stage_retry_keeps_attachment(self):
        c=self.start();self.h.cycle_running(self.a);self.h.pause_cycle(c['id'],0,'isolated interruption')
        self.h.send.reset_mock();self.h.retry_cycle(c['id'])
        self.assertEqual(self.h.send.call_args.args[1]['attachments'],self.attachments)
        self.h.cycle_stage(c['id'],'build','next report',None)
        self.assertNotIn('attachments',self.h.send.call_args.args[1])
    def test_invalid_baseline_does_not_dispatch_attachment(self):
        for team in (False,True):
            with self.assertRaises(ValueError):self.start(team,baselinePaths=['../outside.cs'])
        self.h.send.assert_not_called();self.assertTrue(self.file.exists())

    def test_malformed_attachment_items_leave_no_goal_baseline_or_dispatch(self):
        for item in ('bad item',None,123,[]):
            for team in (False,True):
                with self.subTest(item=item,team=team):
                    with self.assertRaises(ValueError):self.start(team,attachments=[item])
                    self.assertEqual(self.h.store.get('cycles',[]),[])
                    self.assertEqual(self.h.file_baselines()['items'],[])
                    self.h.send.assert_not_called()


if __name__=='__main__':unittest.main()
