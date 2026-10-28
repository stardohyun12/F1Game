"""PitCrew building viewer server (stdlib only).

    python tools/viewer/serve.py            # http://127.0.0.1:8770/
    python tools/viewer/serve.py --port 8771

Serves tools/viewer/ statically (no-store cache headers) plus a tiny feedback API:
  POST /api/feedback        JSON body -> appended to feedback/<building>.jsonl,
                            body.screenshot (data:image/png;base64,...) -> feedback/shots/<id>.png
  GET  /api/feedback?b=<n>  -> JSON list of entries for that building (oldest first); &archived=1 appends archived ones (archived:true)
  POST /api/feedback/archive?b=<n>  moves status=="done" lines to feedback/archive/<n>.jsonl (+ shots to archive/shots/)
  GET  /api/plan?f=<name>   -> docs/design/tutorial/<name>.yaml parsed to JSON (pyyaml if installed, else a small built-in parser)
                            + "placements": docs/design/tutorial/placements.yaml (있으면; 뷰어가 places/steps/targets 위에 덮어 씀)
  POST /api/plan/placements?f=<name>  {places:{id:{world:[x,z],y,yaw}}, steps:{...}, targets:{...}, route_ends:{...}, place_boxes:{...}, delta:[str]}
                            -> docs/design/tutorial/placements.yaml 를 통째로 다시 쓰고(first-delivery.yaml 은 안 건드림),
                               delta 를 feedback/plan-<name>.jsonl 에 한 줄(target "placements") 로 남긴다
  GET  /repo/docs/<path>    -> read-only files under <repo>/docs (captures, reference photos) for the 기획 뷰어 (/plan/)
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
from urllib.parse import parse_qs, urlparse, unquote

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
PLAN_DIR = os.path.join(REPO, "docs", "design", "tutorial")
FB = os.path.join(HERE, "feedback")
SHOTS = os.path.join(FB, "shots")
ARCH = os.path.join(FB, "archive")
ARCH_SHOTS = os.path.join(ARCH, "shots")
MAX_BODY = 40 * 1024 * 1024
SAFE = re.compile(r"[^A-Za-z0-9_.-]")
LOCK = threading.Lock()


def safe(s, fallback):
    s = SAFE.sub("_", str(s or ""))[:80].strip("._")
    return s or fallback


try:
    import yaml as _yaml  # pyyaml (있으면 씀)
    YAML_PARSER = "pyyaml"

    def load_yaml(text):
        return _yaml.safe_load(text)
except ImportError:  # pragma: no cover
    YAML_PARSER = "mini"

    def load_yaml(text):
        return _mini_yaml(text)


def _scalar(s):
    s = s.strip()
    if s == "" or s == "~" or s == "null":
        return None
    if s in ("true", "True"):
        return True
    if s in ("false", "False"):
        return False
    if len(s) >= 2 and s[0] == s[-1] and s[0] in "\"'":
        return s[1:-1]
    if s.startswith("[") and s.endswith("]"):
        inner = s[1:-1].strip()
        return [_scalar(x) for x in _split_commas(inner)] if inner else []
    if s.startswith("{") and s.endswith("}"):
        out = {}
        for kv in _split_commas(s[1:-1].strip()):
            if ":" in kv:
                k, v = kv.split(":", 1)
                out[k.strip().strip("\"'")] = _scalar(v)
        return out
    try:
        return int(s)
    except ValueError:
        pass
    try:
        return float(s)
    except ValueError:
        pass
    return s


def _split_commas(s):
    out, cur, q, depth = [], "", None, 0
    for ch in s:
        if q:
            cur += ch
            if ch == q:
                q = None
        elif ch in "\"'":
            q = ch; cur += ch
        elif ch in "[{":
            depth += 1; cur += ch
        elif ch in "]}":
            depth -= 1; cur += ch
        elif ch == "," and depth == 0:
            out.append(cur); cur = ""
        else:
            cur += ch
    if cur.strip():
        out.append(cur)
    return out


def _mini_yaml(text):
    """pyyaml 이 없을 때의 최소 파서: 들여쓰기 블록 맵/리스트, `- key: v` 리스트 항목, 인라인 [a, b], 따옴표 문자열, # 주석. 블록 스칼라(|, >)·앵커는 지원 안 함."""
    lines = []
    for raw in text.splitlines():
        if not raw.strip() or raw.lstrip().startswith("#"):
            continue
        # strip trailing comment (outside quotes)
        q, cut = None, len(raw)
        for i, ch in enumerate(raw):
            if q:
                if ch == q:
                    q = None
            elif ch in "\"'":
                q = ch
            elif ch == "#" and (i == 0 or raw[i - 1] in " \t"):
                cut = i; break
        s = raw[:cut].rstrip()
        if s.strip():
            lines.append((len(s) - len(s.lstrip(" ")), s.strip()))

    def parse(i, indent):
        # returns (value, next_i)
        if i >= len(lines):
            return None, i
        ind, s = lines[i]
        if s.startswith("- ") or s == "-":
            out = []
            while i < len(lines) and lines[i][0] == indent and (lines[i][1].startswith("- ") or lines[i][1] == "-"):
                item = lines[i][1][1:].strip()
                if not item:
                    v, i = parse(i + 1, lines[i + 1][0] if i + 1 < len(lines) else indent + 1)
                    out.append(v); continue
                if _is_kv(item):
                    # inline first key of a mapping inside the list item
                    sub_indent = indent + 2
                    lines[i] = (sub_indent, item)
                    v, i = parse(i, sub_indent)
                    out.append(v)
                else:
                    out.append(_scalar(item)); i += 1
            return out, i
        out = {}
        while i < len(lines) and lines[i][0] == indent and _is_kv(lines[i][1]):
            k, v = lines[i][1].split(":", 1)
            k = k.strip().strip("\"'"); v = v.strip()
            if v == "":
                if i + 1 < len(lines) and lines[i + 1][0] > indent:
                    val, i = parse(i + 1, lines[i + 1][0])
                elif i + 1 < len(lines) and lines[i + 1][0] == indent and lines[i + 1][1].startswith("- "):
                    val, i = parse(i + 1, indent)
                else:
                    val, i = None, i + 1
            else:
                val, i = _scalar(v), i + 1
            out[k] = val
        return out, i

    def _is_kv(s):
        if s.startswith(("\"", "'", "{", "[")):
            return False
        return re.match(r"^[^\s:#\-][^:]*:(\s|$)", s) is not None

    val, _ = parse(0, lines[0][0] if lines else 0)
    return val


def read_entries(building, archived=False):
    p = os.path.join(FB, safe(building, "unknown") + ".jsonl")
    out = _read_jsonl(p)
    if archived:
        for e in _read_jsonl(os.path.join(ARCH, safe(building, "unknown") + ".jsonl")):
            e["archived"] = True
            if e.get("screenshot") and not str(e["screenshot"]).startswith("archive/"):
                e["screenshot"] = "archive/" + e["screenshot"]
            out.append(e)
    return out


def _read_jsonl(p):
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


def archive_done(building):
    """status == "done" 인 줄을 feedback/archive/<building>.jsonl 로 옮기고(append) 스샷은 archive/shots/ 로. 남은 줄만 원본에 다시 쓴다."""
    b = safe(building, "unknown")
    p = os.path.join(FB, b + ".jsonl")
    if not os.path.isfile(p):
        return {"ok": True, "building": b, "archived": 0, "remaining": 0}
    with LOCK:
        keep, done = [], []
        with open(p, encoding="utf-8") as f:
            for line in f:
                s = line.strip()
                if not s:
                    continue
                try:
                    e = json.loads(s)
                except ValueError:
                    keep.append(s)
                    continue
                (done if e.get("status") == "done" else keep).append(e)
        if done:
            os.makedirs(ARCH_SHOTS, exist_ok=True)
            with open(os.path.join(ARCH, b + ".jsonl"), "a", encoding="utf-8") as f:
                for e in done:
                    e["archived_at"] = time.strftime("%Y-%m-%dT%H:%M:%S%z")
                    shot = e.get("screenshot")
                    if shot:
                        src = os.path.join(FB, shot)
                        if os.path.isfile(src):
                            os.replace(src, os.path.join(ARCH_SHOTS, os.path.basename(shot)))
                    f.write(json.dumps(e, ensure_ascii=False) + "\n")
            with open(p, "w", encoding="utf-8") as f:
                for e in keep:
                    f.write((e if isinstance(e, str) else json.dumps(e, ensure_ascii=False)) + "\n")
    sys.stderr.write("[serve] archive %s: %d done -> archive, %d remain\n" % (b, len(done), len(keep)))
    return {"ok": True, "building": b, "archived": len(done), "remaining": len(keep)}


PLACEMENTS = os.path.join(PLAN_DIR, "placements.yaml")


def read_placements():
    """docs/design/tutorial/placements.yaml → dict (없으면 None). 기획 뷰어 '배치' 저장분: 기획 YAML 은 그대로 두고 자리만 따로 둔다."""
    if not os.path.isfile(PLACEMENTS):
        return None
    try:
        with open(PLACEMENTS, encoding="utf-8") as f:
            return load_yaml(f.read())
    except Exception as e:  # noqa: BLE001
        sys.stderr.write("[serve] placements.yaml 읽기 실패: %s\n" % e)
        return None


def _num(v, nd=2):
    try:
        return round(float(v), nd)
    except (TypeError, ValueError):
        return None


def _clean_entry(e):
    """{world:[x,z], y, yaw} 만 남기고 숫자는 소수 2자리."""
    if not isinstance(e, dict):
        return None
    out = {}
    w = e.get("world")
    if isinstance(w, (list, tuple)) and len(w) == 2:
        out["world"] = [_num(w[0]), _num(w[1])]
    for k in ("y", "yaw"):
        if e.get(k) is not None:
            out[k] = _num(e[k])
    return out or None


def _yaml_dump(doc):
    if YAML_PARSER == "pyyaml":
        return _yaml.safe_dump(doc, allow_unicode=True, sort_keys=False, default_flow_style=None, width=120)
    # mini: 2단 맵 + 인라인 {world: [x, z], y: .., yaw: ..} 만 쓴다
    lines = []
    for sec, items in doc.items():
        if not isinstance(items, dict):
            lines.append("%s: %s" % (sec, json.dumps(items, ensure_ascii=False)))
            continue
        lines.append("%s:" % sec)
        for k, v in items.items():
            lines.append("  %s: %s" % (k, json.dumps(v, ensure_ascii=False)))
    return "\n".join(lines) + "\n"


def write_placements(name, body):
    """뷰어 '저장' → placements.yaml 통째로 다시 쓰기 + feedback/plan-<name>.jsonl 에 바뀐 것 한 줄."""
    doc = {}
    for sec in ("places", "steps", "targets", "route_ends", "place_boxes"):
        src = body.get(sec) or {}
        if not isinstance(src, dict):
            continue
        cleaned = {}
        for k, v in src.items():
            c = _clean_entry(v)
            if c:
                cleaned[safe(k, "_")] = c
        if cleaned:
            doc[sec] = cleaned
    delta = [str(d) for d in (body.get("delta") or []) if str(d).strip()]
    stamp = time.strftime("%Y-%m-%dT%H:%M:%S%z")
    head = ("# 기획 뷰어(/plan/) '배치' 저장분 — 자리만. first-delivery.yaml 은 그대로 두고 뷰어가 이 값을 위에 덮어 씀.\n"
            "# 좌표 = Unity 월드: world [x, z], y = 바닥(피벗) 높이, yaw = Unity Y 회전(도). 프로그래머는 이 값을 Map_TownV6 에 그대로.\n"
            "# 마지막 저장 %s (%s). 자동 생성 — 손으로 고쳐도 되지만 다음 저장에 통째로 덮어 씀.\n" % (stamp, "plan=" + name))
    text = head + _yaml_dump({"plan": name, "saved": stamp, **doc})
    os.makedirs(PLAN_DIR, exist_ok=True)
    with LOCK:
        tmp = PLACEMENTS + ".tmp"
        with open(tmp, "w", encoding="utf-8") as f:
            f.write(text)
        os.replace(tmp, PLACEMENTS)
    fb_id = None
    if delta:
        building = "plan-" + name
        fb_id = "%s-%s-placement" % (building, time.strftime("%Y%m%d-%H%M%S"))
        line = {"id": fb_id, "building": building, "plan": name, "target": "placements", "kind": "placement",
                "label": "배치 저장", "text": "\n".join(delta), "comment": "[배치] " + " / ".join(delta),
                "changes": delta, "time": stamp, "received": stamp, "screenshot": None, "file": "docs/design/tutorial/placements.yaml"}
        os.makedirs(FB, exist_ok=True)
        with LOCK:
            with open(os.path.join(FB, building + ".jsonl"), "a", encoding="utf-8") as f:
                f.write(json.dumps(line, ensure_ascii=False) + "\n")
    sys.stderr.write("[serve] placements %s: %s, delta %d\n" % (name, {k: len(v) for k, v in doc.items()}, len(delta)))
    return {"ok": True, "file": os.path.relpath(PLACEMENTS, REPO).replace("\\", "/"), "counts": {k: len(v) for k, v in doc.items()}, "feedback_id": fb_id}


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

    def _repo_file(self, rel):
        """GET /repo/docs/<path> — read-only, docs/ 아래만."""
        rel = unquote(rel).replace("\\", "/")
        root = os.path.join(REPO, "docs")
        full = os.path.normpath(os.path.join(root, rel))
        if not full.startswith(os.path.normpath(root) + os.sep) or not os.path.isfile(full):
            return self._json(404, {"ok": False, "error": "not found"})
        ctype = self.guess_type(full)
        with open(full, "rb") as f:
            data = f.read()
        self.send_response(200)
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        u = urlparse(self.path)
        if u.path == "/api/feedback":
            q = parse_qs(u.query)
            return self._json(200, read_entries(q.get("b", [""])[0], archived=q.get("archived", ["0"])[0] not in ("", "0")))
        if u.path == "/api/plan":
            name = safe(parse_qs(u.query).get("f", ["first-delivery"])[0], "first-delivery")
            p = os.path.join(PLAN_DIR, name + ".yaml")
            if not os.path.isfile(p):
                return self._json(404, {"ok": False, "error": "no plan %s" % name})
            try:
                with open(p, encoding="utf-8") as f:
                    data = load_yaml(f.read())
            except Exception as e:  # noqa: BLE001
                return self._json(500, {"ok": False, "error": "yaml: %s" % e})
            return self._json(200, {"ok": True, "name": name, "mtime": time.strftime("%Y-%m-%dT%H:%M:%S", time.localtime(os.path.getmtime(p))),
                                    "parser": YAML_PARSER, "plan": data, "placements": read_placements()})
        if u.path.startswith("/repo/docs/"):
            return self._repo_file(u.path[len("/repo/docs/"):])
        if u.path == "/plan":
            self.send_response(301); self.send_header("Location", "/plan/"); self.end_headers(); return
        return super().do_GET()

    def do_POST(self):
        u = urlparse(self.path)
        if u.path == "/api/feedback/archive":
            return self._json(200, archive_done(parse_qs(u.query).get("b", [""])[0]))
        if u.path == "/api/plan/placements":
            n = int(self.headers.get("Content-Length") or 0)
            if n <= 0 or n > MAX_BODY:
                return self._json(413, {"ok": False, "error": "body size %d" % n})
            try:
                body = json.loads(self.rfile.read(n).decode("utf-8"))
                assert isinstance(body, dict)
            except Exception as e:  # noqa: BLE001
                return self._json(400, {"ok": False, "error": "bad json: %s" % e})
            name = safe(parse_qs(u.query).get("f", ["first-delivery"])[0], "first-delivery")
            try:
                return self._json(200, write_placements(name, body))
            except Exception as e:  # noqa: BLE001
                return self._json(500, {"ok": False, "error": str(e)})
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
