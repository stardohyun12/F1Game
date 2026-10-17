import base64
import io
import tempfile
import time
import sys
from pathlib import Path
import unittest
from unittest.mock import patch
from PIL import Image
import hub,studio,studio_assets,studio_terminal,runtime

class WorkbenchTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)
        self.h=hub.Hub(self.root/'data',self.root);studio.setup(self.h)
        out=io.BytesIO();Image.new('RGB',(32,24),'yellow').save(out,format='PNG')
        self.raw=out.getvalue()
    def tearDown(self):
        self.h.close();self.h.store.db.close();self.temp.cleanup()
    def upload(self,**kw):
        return studio_assets.upload(self.h,dict(name='한글 원본.png',type='image/png',data=base64.b64encode(self.raw).decode(),**kw))
    def test_version_region_reaches_run(self):
        a=self.upload();captured=[]
        with patch.object(self.h,'send',side_effect=lambda aid,data:captured.append(data)):
            studio.submit(self.h,dict(room='art',text='색감 변경',images=[dict(id=a['id'],purpose='차체 유지',region=dict(x=.1,y=.2,w=.3,h=.4))]))
        value=captured[0];self.assertIn('차체 유지',value['text']);self.assertIn(a['sha256'],value['text'])
        run=hub.Run(self.h,self.h.store.agent(studio.state(self.h)['rooms']['art']['agent']),{},value['text']);run.files=value['files']
        self.assertEqual(Path(run.images()[0]).read_bytes(),self.raw)
        self.assertIn('"w":0.3',value['text'].replace(' ',''))
    def test_scope_invalid_region_tamper(self):
        a=self.upload()
        for selections in [[{'id':'other-project'}],[{'id':a['id'],'region':dict(x=.9,y=0,w=.2,h=.1)}],[{'id':a['id'],'region':dict(x=float('nan'),y=0,w=.2,h=.1)}]]:
            with self.assertRaises(ValueError):studio_assets.inputs(self.h,selections)
        Path(a['path']).write_bytes(b'changed')
        with self.assertRaises(ValueError):studio_assets.image(self.h,a['id'])
    def test_adoption_explicit(self):
        a=self.upload();aid=studio.state(self.h)['rooms']['feel']['agent']
        self.assertEqual(studio.context(self.h,aid)['adoptedAssets'],[])
        studio_assets.update(self.h,dict(id=a['id'],kind='candidate',explanation='실험'))
        self.assertEqual(studio.context(self.h,aid)['adoptedAssets'],[])
        studio_assets.update(self.h,dict(id=a['id'],kind='adopted',explanation='선택한 방향'))
        self.assertEqual(studio.context(self.h,aid)['adoptedAssets'][0]['explanation'],'선택한 방향')
    def test_mime_rejected(self):
        with self.assertRaises(ValueError):studio_assets.upload(self.h,dict(name='bad.jpg',type='image/jpeg',data=base64.b64encode(self.raw).decode()))
    def test_traceable_impact_is_serial_and_cannot_cycle(self):
        with patch.object(self.h,'send') as send:
            parent=studio.submit(self.h,dict(room='feel',text='조향 변경'))
            child=studio.report_impact(self.h,parent['id'],dict(room='art',task='카메라 흔들림 확인',reason='회전 초기 응답 증가'))
            self.assertEqual(send.call_count,1)
            self.assertEqual(child['status'],'queued')
            self.assertEqual(studio.state(self.h)['jobs'][0]['impactEvents'][0]['job'],child['id'])
            with self.assertRaises(ValueError):studio.report_impact(self.h,parent['id'],dict(room='art',task='중복',reason='중복'))
            studio.finished(self.h,parent['id'],'done')
            self.assertEqual(send.call_count,2)
            with self.assertRaises(ValueError):studio.report_impact(self.h,child['id'],dict(room='feel',task='역호출',reason='순환'))
    def test_lead_assigns_serially_and_receives_results_once(self):
        with patch.object(self.h,'send') as send:
            lead=studio.submit(self.h,dict(room='production',text='색감과 반응을 개선'))
            context=studio.context(self.h,lead['agent'])
            self.assertIn('report_team_impact',context['coordination'])
            art=studio.report_impact(self.h,lead['id'],dict(room='art',task='색감',reason='시각 개선'))
            feel=studio.report_impact(self.h,lead['id'],dict(room='feel',task='반응',reason='조작 개선'))
            self.assertEqual(send.call_count,1)
            studio.finished(self.h,lead['id'],'done')
            self.assertEqual(send.call_count,2)
            aid=studio.state(self.h)['rooms']['art']['agent']
            self.h.store.message(aid,'assistant','B 색감 결과, 플레이 미검증',run=art['id'])
            studio.finished(self.h,art['id'],'done')
            self.assertEqual(send.call_count,3)
            studio.finished(self.h,art['id'],'done')
            reports=[m for m in self.h.store.messages(lead['agent']) if m.get('sourceJob')==art['id']]
            self.assertEqual(len(reports),1)
            self.assertIn('B 색감',reports[0]['text'])
            self.assertIn(art['id'],[r['id'] for r in studio.context(self.h,lead['agent'])['references']])

    @unittest.skipUnless(sys.platform=='win32','ConPTY')
    def test_real_pty_unicode_resize_reconnect_and_exclusion(self):
        s=studio_terminal.Session('test','art',[sys.executable,'-X','utf8','-u','-c',"print('READY'); print(input()); input()"],self.root)
        self.h.studio_terminals={s.id:s}
        try:
            deadline=time.time()+8
            while 'READY' not in s.snapshot()['output'] and time.time()<deadline:time.sleep(.05)
            studio_terminal.action(self.h,dict(action='input',id=s.id,text='한글 붙여넣기\r',inputId='once'))
            studio_terminal.action(self.h,dict(action='input',id=s.id,text='duplicate\r',inputId='once'))
            studio_terminal.action(self.h,dict(action='resize',id=s.id,cols=110,rows=30))
            time.sleep(.3);first=studio_terminal.state(self.h,s.id)
            self.assertIn('한글 붙여넣기',first['output']);self.assertNotIn('duplicate',first['output'])
            self.assertEqual(studio_terminal.state(self.h,s.id,first['offset'])['output'],'')
            with self.assertRaises(ValueError):studio.submit(self.h,dict(room='art',text='conflict'))
            self.assertFalse(runtime.maintenance_status(self.h)['ready'])
        finally:studio_terminal.action(self.h,dict(action='close',id=s.id))
        self.assertFalse(studio_terminal.active(self.h))

if __name__=='__main__':unittest.main()
