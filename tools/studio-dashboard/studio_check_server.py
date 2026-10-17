"""Disposable UI verification host. --mock is explicitly simulated provider work."""
import argparse
import os
from pathlib import Path
import time
import zipfile
from http.server import ThreadingHTTPServer

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--root',required=True);p.add_argument('--data',required=True)
    p.add_argument('--port',type=int,default=7891);p.add_argument('--mock',action='store_true');a=p.parse_args()
    os.environ['PITCREW_PROJECT']=str(Path(a.root).resolve());os.environ['PITCREW_RUNTIME_DIR']=str(Path(a.data).resolve());os.environ['PITCREW_PORT']=str(a.port)
    Path(a.root).mkdir(parents=True,exist_ok=True)
    import hub,server,studio
    h=hub.get_hub();studio.setup(h)
    if a.mock:
        h.studio_test_mode=True
        def provider(run):
            for _ in range(20):
                if run.cancel.wait(.1):return
            if 'FAIL_TEST' in run.prompt:raise ValueError('격리 시험: 도구 실패 전달')
            path=h.root/'팀 결과 한글.zip'
            with zipfile.ZipFile(path,'w') as z:z.writestr('result.txt',run.prompt)
            run.output('result','[모의 실행 시험] 요청 결과입니다.\n\n'+run.prompt+'\n\n공통 방향: '+str(run.context['production']['adopted']))
            run.register_artifact({'path':str(path)})
        hub.Run.codex=provider
        h.after_run=lambda *a:None
    print(f'http://127.0.0.1:{a.port}/studio mode={"MOCK" if a.mock else "REAL"}',flush=True)
    service=ThreadingHTTPServer(('127.0.0.1',a.port),server.H)
    try:service.serve_forever()
    finally:service.server_close();h.close()
