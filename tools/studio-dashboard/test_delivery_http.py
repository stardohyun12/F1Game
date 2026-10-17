"""Actual web -> independent runtime -> registry -> attachment bytes, no model calls."""
import hashlib
import json
from pathlib import Path
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
from http.server import ThreadingHTTPServer
from unittest.mock import patch
import zipfile
import hub
import runtime
import server
from isolated_runtime import isolated_runtime


class DeliveryHTTPTests(unittest.TestCase):
    def test_proxy_roundtrip_and_project_boundary(self):
        with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as folder:
            root=Path(folder);project=root/'project';project.mkdir();directory=root/'runtime'
            h=hub.Hub(directory,project);aid=h.store.agents()[0]['id'];h.store.message(aid,'user','create',run='verified-test-run');h.store.db.close()
            source=project/'검토 자료.zip'
            with zipfile.ZipFile(source,'w') as z:z.writestr('한글.txt','ZIP delivery bytes')
            original=source.read_bytes()
            with isolated_runtime(directory,{'PITCREW_PROJECT':str(project)}):
                web=ThreadingHTTPServer(('127.0.0.1',0),server.H);port=web.server_port
                with patch.object(server,'PORT',port),patch.object(server.H,'runtime_proxy',True),patch.object(runtime,'DESCRIPTOR',directory/'runtime.json'):
                    thread=threading.Thread(target=web.serve_forever,daemon=True);thread.start()
                    base=f'http://127.0.0.1:{port}'
                    def request(path,body=None,token=None):
                        req=urllib.request.Request(base+path,data=json.dumps(body).encode() if body else None,
                            headers={'X-PitCrew-Token':token or '', 'Content-Type':'application/json'})
                        return urllib.request.urlopen(req,timeout=10)
                    try:
                        with request('/api/hub/bootstrap') as r:token=json.load(r)['token']
                        with request('/api/hub/deliverables',dict(agent=aid,run='verified-test-run',path=str(source)),token) as r:item=json.load(r)
                        source.unlink()
                        with request(item['url']) as r:
                            self.assertEqual(r.headers['Content-Type'],'application/zip')
                            self.assertIn("filename*=UTF-8''",r.headers['Content-Disposition'])
                            self.assertEqual(hashlib.sha256(r.read()).hexdigest(),item['sha256'])
                        with self.assertRaises(urllib.error.HTTPError) as e:request(item['url'].replace('default','unknown-project'))
                        self.assertEqual(e.exception.code,410)
                        with self.assertRaises(urllib.error.HTTPError):request('/api/hub/deliverables',dict(agent=aid,run='verified-test-run',path=str(root/'private.txt')),token)
                        with request('/api/hub/deliverables',dict(action='expire',id=item['id']),token) as r:self.assertTrue(json.load(r)['ok'])
                        with self.assertRaises(urllib.error.HTTPError) as e:request(item['url'])
                        self.assertEqual(e.exception.code,410)
                    finally:web.shutdown();web.server_close();thread.join(2)


if __name__=='__main__':unittest.main()
