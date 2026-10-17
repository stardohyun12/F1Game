"""Read-only fixture preview. Never imports hub, launches a model, or reads a local user profile."""
from http.server import BaseHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit,parse_qs
import argparse,json,time
OUT=Path(__file__).resolve().parents[1];WEB=OUT/'source/tools/studio-dashboard/web'
AGENTS=[dict(id='demo-'+str(i),name=name,provider=provider,role='격리 검토용 가상 작업자',status=status,model='',mode='read',queue=[],context='',private='',shared='',session='',createdAt=0,updatedAt=0) for i,(name,provider,status) in enumerate([('구현 예시','codex','done'),('검토 예시','claude','waiting'),('실행 예시','codex','running'),('실패 예시','claude','error')])]
MESSAGES=[dict(id='m1',role='user',text='완료한 검토 자료 ZIP을 내려받고 싶습니다.',ts=0),dict(id='m2',role='assistant',text='진단용 예시 응답입니다. [인계 ZIP](/D:/example/review.zip)\n\n이 링크는 일반 파일 다운로드 기능 부재를 보여 주기 위한 의도적인 실패 사례입니다.',ts=1)]
class H(BaseHTTPRequestHandler):
    def log_message(self,*args):pass
    def send(self,data,status=200,kind='application/json; charset=utf-8'):
        b=data if isinstance(data,bytes) else json.dumps(data,ensure_ascii=False).encode()
        self.send_response(status);self.send_header('Content-Type',kind);self.send_header('Content-Length',str(len(b)));self.end_headers();self.wfile.write(b)
    def do_POST(self):self.send({'error':'Read-only fixture: all writes and model calls disabled'},405)
    do_DELETE=do_POST;do_PUT=do_POST
    def do_GET(self):
        u=urlsplit(self.path);route=u.path;q=parse_qs(u.query)
        if route=='/':
            b=(WEB/'index.html').read_text(encoding='utf-8')
            b=b.replace('<body>','<body><div style="position:fixed;bottom:0;left:0;right:0;z-index:99999;background:#fce7a0;text-align:center;font:12px sans-serif">격리 UI fixture · 가상 데이터 · 모델/Unity 호출 없음 · 제출 차단</div>')
            return self.send(b.encode(),kind='text/html; charset=utf-8')
        name=route.lstrip('/')
        if '/' not in name and name and (WEB/name).is_file() and (WEB/name).suffix in ('.js','.css'):
            return self.send((WEB/name).read_bytes(),kind='text/javascript; charset=utf-8' if name.endswith('.js') else 'text/css; charset=utf-8')
        if route=='/api/hub/bootstrap':return self.send(dict(token='fixture-not-a-credential',capabilities={},project='/fixture/F1Game',projectId='default',projects=[dict(id='default',name='격리 검토 예시',path='/fixture/F1Game')],agents=AGENTS,providers=[dict(id='codex',name='Codex',ready=False),dict(id='claude',name='Claude',ready=False)],teams=[],delegations=[],cycles=[],alertLog=[],runtime=dict(independent=True,pid=0,startedAt=0),shared='',activity=[],approvals=[]))
        if route=='/api/hub/detail':return self.send(dict(agent=next((a for a in AGENTS if a['id']==q.get('agent',[''])[0]),AGENTS[0]),messages=MESSAGES,approvals=[],context={},refs=[],shared='',activity=[]))
        if route=='/api/hub/artifacts':return self.send(dict(images=[],files=[],inputs=[],query='',counts=dict(images=0,files=0,inputs=0),pagination=dict(images=dict(page=1,pages=0,total=0,pageSize=12),inputs=dict(page=1,pages=0,total=0,pageSize=20))))
        if route in ('/api/hub/stage-reports','/api/hub/goal-history','/api/hub/files','/api/hub/projects','/api/hub/file-baselines'):return self.send([])
        if route.startswith('/api/'):return self.send({'error':'Not implemented in the read-only review fixture'},503)
        return self.send(b'not found',404,'text/html; charset=utf-8')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--port',type=int,default=7899);args=p.parse_args()
    s=ThreadingHTTPServer(('127.0.0.1',args.port),H)
    print(f'Read-only fixture http://127.0.0.1:{s.server_port}/ (synthetic data)',flush=True)
    s.serve_forever()
