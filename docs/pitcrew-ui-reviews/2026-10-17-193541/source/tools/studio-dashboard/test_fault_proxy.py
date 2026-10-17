"""Read-only browser fault-injection fixture, never used by the production launcher.

Run with the app on 7878. Write online/offline/hang to .runtime/fault-mode.txt.
Open http://127.0.0.1:7879 and verify recovery without reloading.
"""
import time
import urllib.request
from pathlib import Path
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

MODE = Path(__file__).parent / '.runtime' / 'fault-mode.txt'

class Proxy(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def do_POST(self):
        self.send_error(405, 'Test fixture is read-only')

    def do_GET(self):
        mode = MODE.read_text().strip() if MODE.exists() else 'online'
        if self.path.startswith('/api/hub/bootstrap'):
            if mode == 'hang':
                time.sleep(20)
            elif mode == 'offline':
                self.send_response(503)
                self.send_header('Content-Type', 'application/json')
                self.end_headers()
                self.wfile.write(b'{"error":"Injected outage"}')
                return
        try:
            with urllib.request.urlopen('http://127.0.0.1:7878' + self.path, timeout=15) as response:
                body = response.read()
                self.send_response(response.status)
                self.send_header('Content-Type', response.headers['Content-Type'])
                self.send_header('Content-Length', str(len(body)))
                self.send_header('Cache-Control', 'no-store')
                self.end_headers()
                self.wfile.write(body)
        except (BrokenPipeError, ConnectionResetError):
            pass

if __name__ == '__main__':
    ThreadingHTTPServer(('127.0.0.1', 7879), Proxy).serve_forever()
