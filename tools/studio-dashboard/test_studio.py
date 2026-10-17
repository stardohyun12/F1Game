import base64
import hashlib
from pathlib import Path
import tempfile
import threading
import time
import unittest
from unittest.mock import patch
import zipfile
import hub
import studio
import deliverables


class StudioTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name)/'project';self.root.mkdir()
        self.h=hub.Hub(Path(self.tmp.name)/'runtime',self.root)
        studio.setup(self.h);self.rooms=studio.state(self.h)['rooms']
        self.aid=self.rooms['art']['agent']
        self.h.store.message(self.aid,'user','create delivery',run='test-run')
        self.file=self.root/'결과 파일.zip'
        with zipfile.ZipFile(self.file,'w') as z:z.writestr('result.txt','real zip bytes')
    def tearDown(self):
        self.h.close();self.h.store.db.close();self.tmp.cleanup()
    def register(self,path=None):
        return deliverables.register(self.h,dict(agent=self.aid,run='test-run',path=str(path or self.file)))
    def test_immutable_copy_and_hash(self):
        content=self.file.read_bytes();item=self.register();self.file.write_bytes(b'changed');self.file.unlink()
        got=deliverables.download(self.h,item['id'])
        self.assertEqual(base64.b64decode(got['content']),content)
        self.assertEqual(item['sha256'],hashlib.sha256(content).hexdigest())
        self.assertEqual(item['name'],'결과 파일.zip')
        self.assertEqual(len(self.h.artifacts(self.aid)['deliveries']),1)
    def test_boundaries_and_expiry(self):
        outside=self.root.parent/'outside.txt';outside.write_text('private')
        (self.root/'.env').write_text('private')
        for path in [outside,self.root/'..'/'outside.txt',self.root/'.env']:
            with self.assertRaises((ValueError,FileNotFoundError)):self.register(path)
        item=self.register();other=hub.Hub(self.root.parent/'other-runtime',self.root)
        try:
            with self.assertRaises(ValueError):deliverables.download(other,item['id'])
        finally:other.store.db.close()
        deliverables.revoke(self.h,item['id'])
        with self.assertRaises(ValueError):deliverables.download(self.h,item['id'])
        if __import__('os').name=='nt':
            import subprocess
            outside_dir=self.root.parent/'outside-dir';outside_dir.mkdir();(outside_dir/'secret.txt').write_text('secret')
            link=self.root/'junction'
            result=subprocess.run(['cmd','/c','mklink','/J',str(link),str(outside_dir)],capture_output=True)
            self.assertEqual(result.returncode,0)
            try:
                with self.assertRaises(ValueError):self.register(link/'secret.txt')
            finally:link.rmdir()
    def test_missing_corrupt_and_symlink(self):
        item=self.register();copy=self.h.directory/'deliverables'/item['id'];copy.write_bytes(b'tamper')
        with self.assertRaises(ValueError):deliverables.download(self.h,item['id'])
        copy.unlink();self.assertEqual(deliverables.listing(self.h)[0]['status'],'missing')
        link=self.root/'link.zip'
        try:link.symlink_to(self.file)
        except OSError:return  # Windows privilege depends on host; junction covered separately in HTTP suite
        with self.assertRaises(ValueError):self.register(link)
    def test_only_adopted_shared_and_original_request(self):
        studio.action(self.h,dict(action='goal',text='따뜻한 배달 게임'))
        studio.action(self.h,dict(action='propose',room='art',text='노랑을 강조'))
        self.assertEqual(studio.context(self.h,self.rooms['feel']['agent'])['adopted'],[])
        identity=studio.state(self.h)['decisions'][0]['id']
        studio.action(self.h,dict(action='adopt',id=identity))
        self.assertEqual(studio.context(self.h,self.rooms['feel']['agent'])['adopted'][0]['text'],'노랑을 강조')
    def test_serial_queue_real_run_lifecycle(self):
        entered=threading.Event();release=threading.Event();seen=[]
        def provider(run):
            seen.append((run.prompt,run.context['production']));entered.set();release.wait(3)
            run.output('answer','구현 결과. 통합 미검증')
            run.register_artifact({'path':str(self.file)})
        with patch.object(hub.Run,'codex',provider),patch.object(self.h,'after_run',lambda *a:None):
            first=studio.submit(self.h,dict(action='send',room='art',text='색감을 바꿔 줘'))
            self.assertTrue(entered.wait(2))
            with self.assertRaises(ValueError):studio.submit(self.h,dict(action='send',room='feel',text='반응 개선'))
            second=studio.submit(self.h,dict(action='queue',room='feel',text='반응 개선'))
            with self.assertRaises(ValueError):self.h.send(self.rooms['production']['agent'],{'text':'conflicting edit'})
            self.assertEqual(len(self.h.runs),1);release.set()
            deadline=time.time()+6
            while time.time()<deadline and any(j['status'] not in studio.TERMINAL for j in studio.state(self.h)['jobs']):time.sleep(.02)
            self.assertEqual([s[0] for s in seen],['색감을 바꿔 줘','반응 개선'])
            self.assertEqual([j['status'] for j in studio.state(self.h)['jobs']],['done','done'])
            self.assertEqual(studio.snapshot(self.h)['integration'],'not_verified')
            self.assertEqual(len(deliverables.listing(self.h)),2)
            self.assertTrue(all(j.get('message') for j in studio.state(self.h)['jobs']))
    def test_restart_and_link_limit(self):
        with patch.object(self.h,'send',lambda *a:None):
            first=studio.submit(self.h,dict(action='send',room='art',text='test'))
            studio.snapshot(self.h)
            self.assertEqual(studio.state(self.h)['jobs'][0]['status'],'interrupted')
            for _ in range(4):studio.submit(self.h,dict(action='queue',room='feel',text='linked',parent=first['id']))
            with self.assertRaises(ValueError):studio.submit(self.h,dict(action='queue',room='feel',text='loop',parent=first['id']))


if __name__=='__main__':unittest.main()
