"""데스크톱 플러그인(pitcrew-status)이 읽을 지금 상태 JSON 을 127.0.0.1:8799 로 내줌(D-254). start_queues.ps1 이 창 없이 띄움."""
import json, os, pathlib, http.server, socketserver
root = pathlib.Path("D:/Github/F1Game"); os.chdir(root)
src = (root / "scripts/tg_status.py").read_text(encoding="utf-8").split('prev = ""')[0]
g = {}; exec(src.replace("pidf.write_text(str(os.getpid()))", "pass"), g)
(root / ".claude/team/status_server.pid").write_text(str(os.getpid()))

class H(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        try:
            txt = g["status"]()
        except Exception as e:
            txt = f"상태 오류: {e}"
        lines = txt.splitlines()[1:]
        body = json.dumps({"text": "\n".join(lines), "lines": lines}, ensure_ascii=False).encode("utf-8")
        self.send_response(200)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Access-Control-Allow-Origin", "*")
        self.end_headers(); self.wfile.write(body)
    def log_message(self, *a): pass

socketserver.TCPServer.allow_reuse_address = True
with socketserver.ThreadingTCPServer(("127.0.0.1", 8799), H) as s:
    s.serve_forever()
