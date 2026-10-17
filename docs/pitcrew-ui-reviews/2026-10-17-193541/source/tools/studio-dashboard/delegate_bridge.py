"""Run-scoped MCP stdio adapter; only an authenticated loopback endpoint can delegate."""
import contextlib
import hmac
import json
import os
from pathlib import Path
import secrets
import sys
import threading
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

TOOL = {'name': 'delegate', 'description': '팀원에게 작업을 맡깁니다. 보고는 이 대화로 돌아옵니다. 목표·범위·완료 조건을 task에 적으세요.',
        'inputSchema': {'type': 'object', 'properties': {'member': {'type': 'string'}, 'task': {'type': 'string'}},
                        'required': ['member', 'task'], 'additionalProperties': False}}


@contextlib.contextmanager
def bridge(callback):
    token = secrets.token_urlsafe(32)

    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *args): pass

        def do_POST(self):
            if self.path != '/delegate' or not hmac.compare_digest(self.headers.get('Authorization', ''), 'Bearer '+token):
                # Closing a Windows socket with unread request bytes can reset it before
                # the client receives the 403. Drain only a bounded, promptly available body.
                try:
                    size=int(self.headers.get('Content-Length','0'))
                    if 0<size<=100000:
                        self.connection.settimeout(1)
                        self.rfile.read(size)
                except (ValueError,OSError):pass
                self.send_error(403); return
            try:
                size = int(self.headers.get('Content-Length', '0'))
                if not 0 < size <= 100000: raise ValueError('Invalid request size')
                args = json.loads(self.rfile.read(size))
                if not isinstance(args, dict) or set(args) != {'member', 'task'} or not all(isinstance(v, str) for v in args.values()):
                    raise ValueError('member와 task 문자열이 필요합니다.')
                result = callback(args)
            except Exception as exc:
                result = {'content': [{'type': 'text', 'text': str(exc)}], 'isError': True}
            body = json.dumps(result, ensure_ascii=False).encode('utf-8')
            self.send_response(200); self.send_header('Content-Type', 'application/json')
            self.send_header('Content-Length', str(len(body))); self.end_headers(); self.wfile.write(body)

    server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
    worker = threading.Thread(target=server.serve_forever, daemon=True); worker.start()
    try:
        yield {'command': sys.executable, 'args': [str(Path(__file__).resolve())],
               'env': {'PITCREW_DELEGATE_URL': f'http://127.0.0.1:{server.server_port}/delegate', 'PITCREW_DELEGATE_TOKEN': token}}
    finally:
        server.shutdown(); server.server_close(); worker.join(timeout=2)


def dispatch(message):
    method = message.get('method'); params = message.get('params') or {}
    if method == 'initialize':
        return {'protocolVersion': params.get('protocolVersion', '2024-11-05'), 'capabilities': {'tools': {}},
                'serverInfo': {'name': 'pitcrew', 'version': '1.0.0'}}
    if method == 'ping': return {}
    if method == 'tools/list': return {'tools': [TOOL]}
    if method == 'tools/call':
        if params.get('name') != 'delegate': raise ValueError('Unknown tool')
        req = urllib.request.Request(os.environ['PITCREW_DELEGATE_URL'],
            data=json.dumps(params.get('arguments')).encode('utf-8'),
            headers={'Authorization': 'Bearer '+os.environ['PITCREW_DELEGATE_TOKEN'], 'Content-Type': 'application/json'})
        try:
            with urllib.request.urlopen(req, timeout=30) as response: return json.load(response)
        except Exception as exc:
            return {'content': [{'type': 'text', 'text': str(exc)}], 'isError': True}
    raise ValueError('Unsupported method')


def main():
    for line in sys.stdin.buffer:
        message = json.loads(line)
        if 'id' not in message: continue
        try: reply = {'result': dispatch(message)}
        except Exception as exc: reply = {'error': {'code': -32602, 'message': str(exc)}}
        sys.stdout.buffer.write((json.dumps({'jsonrpc': '2.0', 'id': message['id'], **reply}, ensure_ascii=False)+'\n').encode('utf-8'))
        sys.stdout.buffer.flush()


if __name__ == '__main__': main()
