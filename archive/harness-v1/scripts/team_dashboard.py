#!/usr/bin/env python
"""PitCrew 스튜디오 화면 (2026-11-03 t_faee99de) — 「지금 목표 / 누가 뭘 하나 / 누가 누구에게 넘기나 / 내가 뭘 판정하나 / 게임이 어떻게 바뀌었나」 한 장.

실행: python -X utf8 scripts/team_dashboard.py   →  http://localhost:8765   (5초마다 갱신, start_queues.ps1 이 창 없이 띄움)
  /            스튜디오 화면 (scripts/studio_page.html)
  /classic     옛 화면 — 일꾼별 실시간 로그·설정·과제 보내기 (scripts/studio_classic.html)
  /graph       Hermes 칸반 카드 그래프(kanban-graph 플러그인 정적 스냅숏, 60초 캐시)
  /api/studio  화면 데이터 JSON — scripts/tg_digest.py(텔레그램 묶음 메시지)도 같은 studio() 를 씀
같은 프로세스 안 스레드: 워치독(team_watchdog.py, 60초) + 묶음 메시지·즉시 알림(tg_digest.py, 60초).
읽는 것: docs/team/flow.json · cards/*/card.md·evidence.json·verdict.json · handoffs.jsonl, .claude/team/sessions/<일꾼>.state·.task,
        .ai_logs/team/<일꾼>.live.log·queue.log, .claude/team/ledger.jsonl(비용), .claude/team/queue/<줄>/*.task
쓰는 것: 도현 판정 버튼 → cards/<ID>/dohyun.json + 카드 기록 줄 + handoffs.jsonl(도현→PD) + queue.log 「도현 판정 끝」(PD 깨우기 감시가 봄).
        단계 이동(아트/통합/그레이박스/완료)은 PD 가 그 답을 보고 card.py move 로 한다 — 화면은 판정을 대신 내리지 않는다.
"""
import datetime as dt
import glob
import json
import os
import re
import subprocess
import sys
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "scripts"))
CONFIG = ROOT / ".claude/team/config.json"
FLOWF = ROOT / "docs/team/flow.json"
CARDS = ROOT / "docs/team/cards"
HANDOFFS = ROOT / "docs/team/handoffs.jsonl"
LEDGER = ROOT / ".claude/team/ledger.jsonl"
GRAPH_PY = Path(os.environ.get("LOCALAPPDATA", r"C:\Users\rtx40\AppData\Local")) / "hermes/plugins/kanban-graph/snapshot/kanban_graph.py"

# 카드가 지나가는 길(레인 막대 칸) — 리서치는 기획 곁가지, 막힘은 따로
PATH = ["plan", "greybox", "verify", "dohyun", "art", "integrate", "final_verify", "done"]
NEXT = {"plan": "greybox", "research": "plan", "greybox": "verify", "verify": "dohyun", "dohyun": "dohyun",
        "art": "integrate", "integrate": "final_verify", "final_verify": "done", "blocked": "blocked"}
WORKER_ORDER = ["pd", "build", "verify", "art", "research"]
LINES = {"build": ["editor"], "verify": ["verify"], "art": ["art", "studio"], "research": ["research"]}
WHO = {"pd": "PD", "build": "구현", "verify": "검증", "art": "아트", "research": "리서치", "dohyun": "도현",
       "harness": "하네스", "-": "-", None: "-"}
ACTIVE = {"research", "greybox", "verify", "art", "integrate", "final_verify"}


def read(p, default=""):
    try:
        return Path(p).read_text(encoding="utf-8", errors="replace")
    except OSError:
        return default


def jload(p, default=None):
    try:
        v = json.loads(read(p) or "null")
        return default if v is None else v
    except json.JSONDecodeError:
        return default


def load_config():
    return jload(CONFIG, {})


def flow():
    return jload(FLOWF, {})


def now():
    return dt.datetime.now().astimezone()


def parse_ts(s):
    try:
        t = dt.datetime.fromisoformat(s)
        return t if t.tzinfo else t.astimezone()
    except (TypeError, ValueError):
        return None


def ago(t):
    if not t:
        return ""
    m = int((now() - t).total_seconds() // 60)
    return f"{m}분" if m < 60 else (f"{m // 60}시간 {m % 60}분" if m < 1440 else f"{m // 1440}일")


# ---------- 카드 ----------
def card_head(txt):
    m = re.match(r"---\r?\n(.*?)\r?\n---\r?\n", txt, re.S)
    head = {}
    if m:
        for ln in m.group(1).splitlines():
            if ":" in ln:
                k, v = ln.split(":", 1)
                head[k.strip()] = v.strip()
    return head, (txt[m.end():] if m else txt)


def section(body, name):
    m = re.search(rf"^## {name}[^\n]*\n(.*?)(?=^## |\Z)", body, re.S | re.M)
    return m.group(1).strip() if m else ""


def load_cards():
    cards = []
    for d in sorted(CARDS.glob("C-*")):
        txt = read(d / "card.md")
        if not txt:
            continue
        h, body = card_head(txt)
        ev = jload(d / "evidence.json", {})
        after, before = ev.get("after") or {}, ev.get("before") or {}
        scope = section(body, "범위")
        files = set(re.findall(r"`([^`\s]+\.(?:cs|unity|prefab|shader|mat|asset|json|py|sh|fbx|png|uss|uxml))`", scope))
        human = section(body, "사람 확인")
        q = re.search(r"묻는 질문[^「]*「(.+?)」", human)
        question = q.group(1) if q else (human.splitlines()[0].lstrip("- ").strip() if human else "")
        v = jload(d / "verdict.json", {})
        cid = h.get("id") or "-".join(d.name.split("-")[:2])
        cards.append({
            "id": cid, "title": h.get("title", d.name), "stage": h.get("stage", "plan"), "owner": h.get("owner", "pd"),
            "fails": int(h.get("fails", 0) or 0), "updated": h.get("updated"), "created": h.get("created"),
            "dir": d.relative_to(ROOT).as_posix(), "files": len(files), "question": question,
            "machine": ev.get("machine"), "verdict": v.get("verdict"), "verdict_reason": v.get("reason", ""),
            "clip": after.get("clip"), "strip": after.get("strip"), "shot_after": after.get("shot"),
            "shot_before": before.get("shot"), "evidence_made": after.get("made") or before.get("made"),
            "answer": jload(d / "dohyun.json", None),
        })
    return cards


def handoffs(n=60):
    rows = []
    for ln in read(HANDOFFS).splitlines()[-n:]:
        try:
            rows.append(json.loads(ln))
        except json.JSONDecodeError:
            pass
    return rows


def today_cost():
    """팀원(Claude Code) 과제 비용 — ledger.jsonl stamp 가 오늘인 줄. Codex(아트)·PD(Hermes) 는 $ 장부에 없음."""
    day = dt.datetime.now().strftime("%Y%m%d")
    alias = {old: new for new, w in flow().get("workers", {}).items() for old in w.get("merges", [])}
    tot, by = 0.0, {}
    for ln in read(LEDGER).splitlines():
        try:
            r = json.loads(ln)
        except json.JSONDecodeError:
            continue
        if str(r.get("stamp", "")).startswith(day):
            c = float(r.get("cost") or 0)
            role = alias.get(r.get("role"), r.get("role", "?"))
            tot += c
            by[role] = by.get(role, 0) + c
    return round(tot, 2), {k: round(v, 2) for k, v in by.items()}


# ---------- 일꾼 ----------
def worker_state(w):
    s = read(ROOT / f".claude/team/sessions/{w}.state", "idle").strip() or "idle"
    working = s.startswith("working")
    parts = s.split(" ", 2)
    started = parts[1] if len(parts) > 1 else ""
    title = parts[2] if len(parts) > 2 else ""
    m = re.search(r"\b(C-\d{3})\b", title) or (re.search(r"\b(C-\d{3})\b", read(ROOT / f".claude/team/sessions/{w}.task")[:600]) if working else None)
    cid = m.group(1) if m else None
    elapsed = None
    if working and re.match(r"\d\d:\d\d$", started):
        h, mi = map(int, started.split(":"))
        n = dt.datetime.now()
        elapsed = (n.hour * 60 + n.minute - h * 60 - mi) % 1440
    live = ROOT / f".ai_logs/team/{w}.live.log"
    quiet = int((time.time() - live.stat().st_mtime) // 60) if live.exists() else None
    last = ""
    if working and live.exists():
        tail = live.read_bytes()[-1200:].decode("utf-8", "ignore").strip().splitlines()
        last = re.sub(r"^\[\S+\]\s+\S+\s*\|\s*", "", tail[-1])[:90] if tail else ""
    clean = re.sub(r"\[[^\]]*\]\s*", "", title).strip() or title
    queued = []
    for line in LINES.get(w, []):
        queued += [Path(p).stem for p in sorted(glob.glob(str(ROOT / f".claude/team/queue/{line}/*.task")))]
    return {"working": working, "started": started, "elapsed": elapsed, "title": clean[:90], "card": cid,
            "quiet": quiet, "last": last, "queued": queued}


def studio():
    fl = flow()
    stages = fl.get("stages", [])
    label = {s["id"]: s["label"] for s in stages}
    owner_of = {s["id"]: s.get("owner") for s in stages}
    cards = load_cards()
    byid = {c["id"]: c for c in cards}
    goal = fl.get("goal", {})
    try:
        dday = (dt.date.fromisoformat(goal.get("deadline", "")) - dt.date.today()).days
    except ValueError:
        dday = None
    workers = {w: worker_state(w) for w in WORKER_ORDER}
    cost, cost_by = today_cost()
    moving = {byid[st["card"]]["stage"] for st in workers.values() if st["working"] and st["card"] in byid}

    flow_boxes = []
    for s in stages:
        cs = [c for c in cards if c["stage"] == s["id"]]
        flow_boxes.append({"id": s["id"], "label": s["label"], "who": WHO.get(s.get("owner"), ""), "n": len(cs),
                           "cards": [c["id"] for c in cs], "optional": bool(s.get("optional")),
                           "on": s["id"] in moving or (s["id"] == "dohyun" and any(not c["answer"] for c in cs))})

    lanes = []
    labels = {k: v.get("label", k) for k, v in fl.get("workers", {}).items()}
    for w in WORKER_ORDER + ["dohyun"]:
        st = workers.get(w) or worker_state(w)
        mine = [c for c in cards if c["owner"] == w and c["stage"] != "done"]
        cur = byid.get(st["card"]) if st["card"] else None
        if not cur and mine:
            cur = sorted(mine, key=lambda c: c.get("updated") or "")[-1]
        lane = {"worker": w, "name": WHO[w], "label": labels.get(w, w), "working": st["working"], "elapsed": st["elapsed"],
                "quiet": st["quiet"], "last": st["last"], "queued": st["queued"][:3], "task_title": st["title"], "card": None,
                "others": [c["id"] for c in mine if not cur or c["id"] != cur["id"]]}
        if cur:
            nxt = NEXT.get(cur["stage"])
            if cur["stage"] == "dohyun":
                nxt_who = "도현 판정 → PD"
            elif cur["stage"] == "blocked":
                nxt_who = "도현 2지선다" if cur["owner"] == "dohyun" else "PD 정리"
            elif nxt == "done":
                nxt_who = "완료"
            else:
                nxt_who = f"{WHO.get(owner_of.get(nxt))} ({label.get(nxt)})"
            lane["card"] = {"id": cur["id"], "title": cur["title"], "stage": cur["stage"], "stage_label": label.get(cur["stage"], cur["stage"]),
                            "files": cur["files"], "fails": cur["fails"], "next": nxt_who,
                            "pos": PATH.index(cur["stage"]) if cur["stage"] in PATH else -1,
                            "since": ago(parse_ts(cur.get("updated"))), "mine": st["card"] == cur["id"] and st["working"]}
        lanes.append(lane)

    ask = [c for c in cards if c["stage"] == "dohyun"][: int(fl.get("dohyun_queue_max", 3))]
    shots = sorted([c for c in cards if c["clip"] or c["shot_after"]],
                   key=lambda c: (c["stage"] == "done", c.get("evidence_made") or ""), reverse=True)[:3]

    warns = []
    limit = int(fl.get("fail_limit", 3))
    for c in cards:
        if c["stage"] == "blocked":
            warns.append({"level": "red", "kind": "blocked", "card": c["id"],
                          "text": f"{c['id']} {c['title'][:40]} — 막힘({c['fails']}번 불통과). " + ("도현 2지선다 필요" if c["owner"] == "dohyun" else "PD 가 정리 중")})
        elif c["fails"] >= limit - 1:
            warns.append({"level": "amber", "kind": "fails", "card": c["id"],
                          "text": f"{c['id']} {c['title'][:40]} — {c['fails']}번 불통과. 한 번 더 실패하면 막힘(3회 규칙)"})
    for w, st in workers.items():
        if st["working"] and st["quiet"] is not None and st["quiet"] >= int(fl.get("stall_min", 20)):
            warns.append({"level": "red", "kind": "stall", "card": st["card"], "text": f"{WHO[w]} 로그가 {st['quiet']}분째 조용 — 멈춘 듯 (워치독이 확인)"})
    stale_h = float(fl.get("stale_hours", 6))
    for c in cards:
        t = parse_ts(c.get("updated"))
        own = workers.get(c["owner"], {})
        if c["stage"] in ACTIVE and t and (now() - t).total_seconds() > stale_h * 3600 and not own.get("working") and not own.get("queued"):
            warns.append({"level": "amber", "kind": "stale", "card": c["id"], "text": f"{c['id']} — {label.get(c['stage'])} 칸에서 {ago(t)}째 아무도 안 잡음"})
        if c["stage"] == "dohyun" and not c["answer"] and t and (now() - t).total_seconds() > 24 * 3600:
            warns.append({"level": "amber", "kind": "wait", "card": c["id"], "text": f"{c['id']} — 도현 판정을 {ago(t)}째 기다림"})
    holds = [ln for ln in read(ROOT / ".ai_logs/team/queue.log").splitlines()[-300:]
             if " 보류 " in ln and ln.startswith("[" + dt.datetime.now().strftime("%m/%d"))]
    if holds:
        warns.append({"level": "amber", "kind": "hold", "card": None,
                      "text": f"오늘 일꾼 과제 보류(실패) {len(holds)}건 — 마지막 {holds[-1][7:12]} {holds[-1].split(' 보류 ', 1)[1][:60]}"})

    log = []
    for r in reversed(handoffs(80)):
        t = parse_ts(r.get("ts"))
        note = r.get("note", "")
        log.append({"t": t.strftime("%H:%M") if t else "", "d": t.strftime("%m/%d") if t else "", "card": r.get("card"),
                    "from": WHO.get(r.get("from"), r.get("from")), "to": WHO.get(r.get("to"), r.get("to")),
                    "stage": f"{label.get(r.get('stage_from'), r.get('stage_from') or '새 카드')} → {label.get(r.get('stage_to'), r.get('stage_to'))}",
                    "note": note[:150], "verdict": r.get("verdict"), "queue": note.startswith("대기열에 과제")})

    return {
        "updated": now().strftime("%H:%M:%S"),
        "goal": {"name": goal.get("name", "목표 없음"), "deadline": goal.get("deadline"), "dday": dday,
                 "done": sum(1 for c in cards if c["stage"] == "done"), "total": len(cards),
                 "wip": sum(1 for c in cards if c["stage"] in ACTIVE), "wip_limit": int(fl.get("wip_limit", 3)),
                 "cost": cost, "cost_by": cost_by, "budget": float(fl.get("daily_budget_usd", 40))},
        "flow": flow_boxes, "lanes": lanes, "ask": ask, "shots": shots, "warns": warns, "log": log[:30],
        "lock": read(ROOT / ".claude/team/editor.lock").strip(),
        "digest_hours": fl.get("digest_hours", [13, 21]),
    }


# ---------- 도현 판정 버튼 ----------
ANS = {"pass": "통과", "fix": "수정", "drop": "버림"}


def record_answer(cid, answer, text):
    if answer not in ANS:
        return False, "알 수 없는 답"
    text = (text or "").strip()[:500]
    if answer == "fix" and not text:
        return False, "수정은 한 줄을 적어 주세요."
    import card  # card.py — 같은 기록 형식(카드 본문 줄 + handoffs.jsonl + queue.log)
    d, h, body = card.read(cid)
    if h.get("stage") != "dohyun":
        return False, f"{cid} 는 지금 「도현 한 판」 칸이 아니에요."
    rec = {"answer": answer, "label": ANS[answer], "text": text, "ts": card.now(), "via": "studio"}
    (ROOT / d / "dohyun.json").write_text(json.dumps(rec, ensure_ascii=False, indent=1), encoding="utf-8")
    note = f"도현 판정: {ANS[answer]}" + (f" — {text}" if text else "")
    h.update(owner="pd", updated=card.now())
    card.write(d, h, body.rstrip("\n") + f"\n- {card.now()[:16]} dohyun→pd · {note} (스튜디오 화면) · PD 가 다음 칸으로 옮김\n")
    card.handoff(cid, "dohyun", "pd", "dohyun", "dohyun", note, verdict=ANS[answer])
    card.qlog(f"{cid} 도현 판정 끝: {ANS[answer]} {text[:60]}")
    card.board()
    return True, f"{cid} {ANS[answer]} — PD 에게 넘겼어요."


# ---------- 옛 화면(/classic) ----------
ROLES = [("pd", "PD (Hermes)", "#e8a33d"), ("build", "구현", "#e0607e"), ("verify", "검증 (블라인드)", "#5aa0e0"),
         ("art", "아트", "#6cc070"), ("research", "리서치", "#b08be0")]
WORKERS = ["build", "verify", "art", "research"]
ENGINES = {
    "claude": {"models": ["sonnet", "opus", "haiku"], "efforts": ["", "low", "medium", "high", "xhigh", "max"], "limits": True},
    "codex": {"models": ["gpt-6-astra"], "efforts": ["", "low", "medium", "high", "xhigh"], "limits": False},
}
LINE_RE = re.compile(r"^\[(\d\d:\d\d:\d\d)\]\s+\S+\s+\|\s?(.*)$")


def engine_of(role):
    return load_config().get("art", {}).get("engine", "codex") if role == "art" else "claude"


def role_data(role):
    state = read(ROOT / f".claude/team/sessions/{role}.state", "없음").strip()
    lines = read(ROOT / f".ai_logs/team/{role}.live.log").splitlines()
    start = max((i for i, ln in enumerate(lines) if "===== 새 과제 =====" in ln), default=0)
    events, task = [], ""
    for ln in lines[start:]:
        m = LINE_RE.match(ln.strip())
        if not m:
            continue
        t, msg = m.groups()
        if msg.startswith("📋"):
            task = msg[1:].strip()
        elif "새 과제" not in msg:
            events.append({"t": t, "msg": msg})
    reports = sorted((ROOT / "docs/team/reports").glob(f"{role}-*.md"), key=os.path.getmtime, reverse=True)[:3]
    log = ROOT / f".ai_logs/team/{role}.live.log"
    quiet = int((time.time() - log.stat().st_mtime) // 60) if log.exists() else 0
    return {"state": state, "task": task, "events": events[-40:], "reports": [r.name for r in reports],
            "working": state.startswith("working"), "quiet": quiet}


def classic_api():
    return {"roles": {r: role_data(r) for r, _, _ in ROLES}, "lock": read(ROOT / ".claude/team/editor.lock").strip(),
            "config": load_config()}


def save_config(role, values):
    cfg = load_config()
    cur = cfg.get(role, {})
    eng_name = engine_of(role)
    eng = ENGINES[eng_name]
    model = re.sub(r"[^\w.\-]", "", str(values.get("model", "")))
    if eng_name == "claude" and model not in eng["models"]:
        raise ValueError(f"모델은 {', '.join(eng['models'])} 중 하나여야 해요")
    pre = "claude_" if (role == "art" and eng_name == "claude") else ""
    cur[pre + "model"] = model
    cur[pre + "effort"] = values.get("effort", "") if values.get("effort", "") in eng["efforts"] else ""
    if eng["limits"]:
        cur[pre + "budget"] = max(0.1, min(50.0, float(values.get("budget") or 5)))
        t = values.get("turns")
        cur[pre + "turns"] = 80 if t in (None, "") else max(0, min(300, int(float(t))))
    cfg[role] = cur
    CONFIG.write_text(json.dumps(cfg, ensure_ascii=False, indent=1), encoding="utf-8")
    return cur


def send_task(role, text, editor):
    if role_data(role)["working"]:
        return False, "이 일꾼은 지금 일하는 중이에요. 끝난 뒤 보내세요."
    text = text.strip()
    if not text:
        return False, "과제가 비어 있어요."
    if editor and "[EDITOR]" not in text:
        text = "[EDITOR] " + text
    lock = read(ROOT / ".claude/team/editor.lock").strip()
    if "[EDITOR]" in text and lock and lock != role:
        return False, f"에디터를 {lock} 가 쓰는 중이에요."
    out = open(ROOT / f".ai_logs/team/{role}.run.txt", "w", encoding="utf-8")
    flags = getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0) | getattr(subprocess, "CREATE_NO_WINDOW", 0)
    subprocess.Popen(["bash", "scripts/team.sh", role, "[도현 직접] " + text], cwd=ROOT, stdout=out,
                     stderr=subprocess.STDOUT, stdin=subprocess.DEVNULL, creationflags=flags)
    return True, "보냈어요."


def reset_session(role):
    if role_data(role)["working"]:
        return False, "일하는 중에는 초기화할 수 없어요."
    (ROOT / f".claude/team/sessions/{role}.id").unlink(missing_ok=True)
    (ROOT / f".claude/team/sessions/{role}.cost").unlink(missing_ok=True)
    return True, "기억을 지웠어요. 다음 과제부터 새 대화로 시작해요."


# ---------- 칸반 그래프(정적) ----------
_graph = {"t": 0.0, "html": b""}


def graph_html():
    if time.time() - _graph["t"] > 60 and GRAPH_PY.exists():
        out = ROOT / ".ai_logs/team/kanban-graph.html"
        try:
            r = subprocess.run([sys.executable, "-X", "utf8", str(GRAPH_PY), "--out", str(out)], capture_output=True, timeout=60,
                               stdin=subprocess.DEVNULL, creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
                               env=dict(os.environ, HERMES_HOME=os.environ.get("HERMES_HOME") or str(GRAPH_PY.parents[3])))
            if r.returncode != 0:
                raise RuntimeError(r.stderr.decode("utf-8", "ignore")[-600:])
            _graph.update(t=time.time(), html=out.read_bytes())
        except Exception as e:  # noqa: BLE001
            _graph.update(t=time.time(), html=f"kanban-graph 실패: {e}".encode())
    return _graph["html"] or "kanban-graph 플러그인 없음".encode()


# ---------- HTTP ----------
PAGE = ROOT / "scripts/studio_page.html"
CLASSIC = ROOT / "scripts/studio_classic.html"
TYPES = {".png": "image/png", ".jpg": "image/jpeg", ".gif": "image/gif", ".mp4": "video/mp4", ".webm": "video/webm"}


class H(BaseHTTPRequestHandler):
    def log_message(self, format, *args):  # noqa: A002 — 요청 로그 끔
        pass

    def _send(self, body: bytes, ctype: str, code=200, extra=None):
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Cache-Control", "no-store")
        for k, v in (extra or {}).items():
            self.send_header(k, v)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _json(self, obj):
        self._send(json.dumps(obj, ensure_ascii=False).encode(), "application/json; charset=utf-8")

    def _file(self, relp):
        p = (ROOT / relp).resolve()
        base = (ROOT / "docs/team/cards").resolve()
        ext = p.suffix.lower()
        if base not in p.parents or ext not in TYPES or not p.is_file():
            return self.send_error(404)
        data = p.read_bytes()
        m = re.match(r"bytes=(\d*)-(\d*)", self.headers.get("Range") or "")
        if m and (m.group(1) or m.group(2)):
            a = int(m.group(1)) if m.group(1) else max(0, len(data) - int(m.group(2)))
            b = int(m.group(2)) if (m.group(1) and m.group(2)) else len(data) - 1
            chunk = data[a:b + 1]
            return self._send(chunk, TYPES[ext], 206, {"Content-Range": f"bytes {a}-{a + len(chunk) - 1}/{len(data)}", "Accept-Ranges": "bytes"})
        self._send(data, TYPES[ext], 200, {"Accept-Ranges": "bytes"})

    def do_GET(self):
        from urllib.parse import parse_qs
        path, _, qs = self.path.partition("?")
        if path == "/api/studio":
            return self._json(studio())
        if path == "/api":
            return self._json(classic_api())
        if path == "/file":
            return self._file(parse_qs(qs).get("p", [""])[0])
        if path == "/graph":
            return self._send(graph_html(), "text/html; charset=utf-8")
        if path == "/classic":
            page = (read(CLASSIC).replace("__ROLES__", json.dumps(ROLES, ensure_ascii=False)).replace("__ENGINES__", json.dumps(ENGINES))
                    .replace("__ENGINE_OF__", json.dumps({r: engine_of(r) for r in WORKERS})).replace("__WORKERS__", json.dumps(WORKERS)))
            return self._send(page.encode(), "text/html; charset=utf-8")
        if path in ("/", "/index.html"):
            return self._send(read(PAGE).encode(), "text/html; charset=utf-8")
        self.send_error(404)

    def do_POST(self):
        origin = self.headers.get("Origin", "")
        if origin and not re.match(r"^http://(localhost|127\.0\.0\.1)(:\d+)?$", origin):
            return self.send_error(403)
        try:
            data = json.loads(self.rfile.read(int(self.headers.get("Content-Length", 0))) or b"{}")
            if self.path == "/verdict":
                ok, msg = record_answer(str(data.get("card", "")), data.get("answer"), data.get("text", ""))
                return self._json({"ok": ok, "msg": msg})
            role = data.get("role")
            if role not in WORKERS:
                return self._json({"ok": False, "msg": "알 수 없는 일꾼"})
            if self.path == "/config":
                return self._json({"ok": True, "config": save_config(role, data.get("values", {}))})
            if self.path == "/task":
                ok, msg = send_task(role, data.get("text", ""), bool(data.get("editor")))
                return self._json({"ok": ok, "msg": msg})
            if self.path == "/reset":
                ok, msg = reset_session(role)
                return self._json({"ok": ok, "msg": msg})
            self.send_error(404)
        except SystemExit as e:  # card.py 는 오류를 sys.exit 로 알림
            self._json({"ok": False, "msg": str(e)})
        except Exception as e:  # noqa: BLE001 — 화면이 죽지 않게
            self._json({"ok": False, "msg": f"오류: {e}"})


if __name__ == "__main__":
    import threading
    os.chdir(ROOT)
    port = int(os.environ.get("TEAM_DASH_PORT", "8765"))
    srv = ThreadingHTTPServer(("127.0.0.1", port), H)  # 먼저 묶음 — 이미 떠 있으면 여기서 실패해 루프가 두 벌 안 돎
    (ROOT / ".claude/team/dashboard.pid").write_text(str(os.getpid()))
    if os.environ.get("TEAM_DASH_NO_LOOPS") != "1":
        import team_watchdog  # 멈춘/죽은 일꾼을 60초마다 잡는다
        threading.Thread(target=team_watchdog.loop, daemon=True).start()
        import tg_digest  # 하루 2번 묶음 메시지 + 즉시 알림 4종
        threading.Thread(target=tg_digest.loop, daemon=True).start()
    print(f"http://localhost:{port}")
    srv.serve_forever()
