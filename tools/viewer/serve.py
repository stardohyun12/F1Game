"""PitCrew building viewer server (stdlib only).

    python tools/viewer/serve.py            # http://127.0.0.1:8770/
    python tools/viewer/serve.py --port 8771

Serves tools/viewer/ statically (no-store cache headers) plus a tiny feedback API:
  POST /api/feedback        JSON body -> appended to feedback/<building>.jsonl,
                            body.screenshot (data:image/png;base64,...) -> feedback/shots/<id>.png
  GET  /api/feedback?b=<n>  -> JSON list of entries for that building (oldest first)
Binds 127.0.0.1 only (PC local).
"""
import argparse
import base64
import json
import os
import re
import sys
import threading
import time
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

HERE = os.path.dirname(os.path.abspath(__file__))
FB = os.path.join(HERE, "feedback")
SHOTS = os.path.join(FB, "shots")
MAX_BODY = 40 * 1024 * 1024
SAFE = re.compile(r"[^A-Za-z0-9_.-]")
LOCK = threading.Lock()


def safe(s, fallback):
    s = SAFE.sub("_", str(s or ""))[:80].strip("._")
    return s or fallback


def read_entries(building):
    p = os.path.join(FB, safe(building, "unknown") + ".jsonl")
    out = []
    if os.path.isfile(p):
        with open(p, encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line:
                    try:
                        out.append(json.loads(line))
                    except ValueError:
                        pass
    return out


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=HERE, **kw)

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def log_message(self, format, *args):  # noqa: A002
        if "/api/" in (self.path or "") or (args and str(args[1]) not in ("200", "304")):
            sys.stderr.write("[serve] %s %s\n" % (self.log_date_time_string(), format % args))

    def _json(self, code, obj):
        data = json.dumps(obj, ensure_ascii=False).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        u = urlparse(self.path)
        if u.path == "/api/feedback":
            b = parse_qs(u.query).get("b", [""])[0]
            return self._json(200, read_entries(b))
        return super().do_GET()

    def do_POST(self):
        u = urlparse(self.path)
        if u.path != "/api/feedback":
            return self._json(404, {"ok": False, "error": "not found"})
        n = int(self.headers.get("Content-Length") or 0)
        if n <= 0 or n > MAX_BODY:
            return self._json(413, {"ok": False, "error": "body size %d" % n})
        try:
            body = json.loads(self.rfile.read(n).decode("utf-8"))
            assert isinstance(body, dict)
        except Exception as e:  # noqa: BLE001
            return self._json(400, {"ok": False, "error": "bad json: %s" % e})
        building = safe(body.get("building"), "unknown")
        fid = safe(body.get("id"), "")
        if not fid:
            fid = "%s-%s" % (building, time.strftime("%Y%m%d-%H%M%S"))
        body["id"] = fid
        body["building"] = building
        body.setdefault("time", time.strftime("%Y-%m-%dT%H:%M:%S%z"))
        body["received"] = time.strftime("%Y-%m-%dT%H:%M:%S%z")
        shot = body.pop("screenshot", None)
        body["screenshot"] = None
        if isinstance(shot, str) and shot.startswith("data:image/png;base64,"):
            try:
                png = base64.b64decode(shot.split(",", 1)[1])
                os.makedirs(SHOTS, exist_ok=True)
                with open(os.path.join(SHOTS, fid + ".png"), "wb") as f:
                    f.write(png)
                body["screenshot"] = "shots/%s.png" % fid
            except Exception as e:  # noqa: BLE001
                body["screenshot_error"] = str(e)
        os.makedirs(FB, exist_ok=True)
        with LOCK:
            with open(os.path.join(FB, building + ".jsonl"), "a", encoding="utf-8") as f:
                f.write(json.dumps(body, ensure_ascii=False) + "\n")
        sys.stderr.write("[serve] feedback %s node=%s shot=%s\n" % (fid, body.get("node"), body["screenshot"]))
        return self._json(200, {"ok": True, "id": fid, "screenshot": body["screenshot"]})


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--port", type=int, default=8770)
    ap.add_argument("--host", default="127.0.0.1")
    a = ap.parse_args()
    srv = ThreadingHTTPServer((a.host, a.port), Handler)
    print("[serve] http://%s:%d/  (dir %s, Ctrl+C 로 끄기)" % (a.host, a.port, HERE), flush=True)
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
