#!/usr/bin/env python
"""텔레그램 F1 탭(도현 스레드 5864) — 하루 2번 묶음 메시지 + 즉시 알림 4가지 (2026-11-03 t_faee99de).

묶음(flow.json digest_hours, 기본 13시·21시): 목표 1줄 + 도현 판정 카드 최대 3장(클립 + 질문 1개) + 막힘 1줄 + 비용 1줄.
즉시 알림(🚨, 같은 일은 한 번만):
  1) 도현 판정 급함 — 판정 대기가 한도(3장)에 참 / 3회 규칙으로 막혀 도현 2지선다 / 일꾼이 모두 쉬고 남은 일이 도현 답뿐인 채 60분(밤 1~9시 제외)
  밤(alert_quiet_hours 1~9시)에는 나머지 알림도 소리 없이 보냄.
  2) 빌드(과제) 실패 — queue.log 에 새 「보류」 또는 컴파일 실패 줄
  3) 같은 카드 반복 실패 — 불통과가 fail_limit-1(2번) 이상
  4) 비용 한도 근접 — 오늘 팀원 비용이 daily_budget_usd 의 80% / 100%
그 밖의 텔레그램 자동 메시지는 D-265 조용 모드(tg_quiet) 그대로 끔 — 이 둘이 텔레그램의 전부(+ 기존 「PD 응답 감시」「Codex 한도」 긴급).

사용: python -X utf8 scripts/tg_digest.py now        # 묶음 지금 보내기
      python -X utf8 scripts/tg_digest.py dry        # 보낼 글만 찍기(안 보냄)
      python -X utf8 scripts/tg_digest.py alerts     # 즉시 알림 한 번 점검(보냄)
루프: team_dashboard.py(8765) 가 스레드로 loop() 를 60초마다 돌린다 — 따로 프로세스 띄우지 않음.
기록: .claude/team/tg_digest.json (보낸 묶음 시각·보낸 알림 키·queue.log 읽은 줄 수), 오류 .ai_logs/team/tg_digest.err
"""
import datetime as dt
import json
import os
import re
import sys
import time
import urllib.request
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "scripts"))
import team_dashboard as td  # noqa: E402  같은 studio() 데이터

CHAT, THREAD = "7449275102", 5864
STATE = ROOT / ".claude/team/tg_digest.json"
QLOG = ROOT / ".ai_logs/team/queue.log"


def _token():
    env = Path(os.environ.get("LOCALAPPDATA", r"C:\Users\rtx40\AppData\Local")) / "hermes" / ".env"
    return next(ln.split("=", 1)[1].strip().strip('"') for ln in env.read_text(encoding="utf-8").splitlines()
                if ln.startswith("TELEGRAM_BOT_TOKEN="))


def tg(method, fields, file_field=None, path=None):
    base = f"https://api.telegram.org/bot{_token()}/{method}"
    if path:
        b = uuid.uuid4().hex
        parts = [f'--{b}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode() for k, v in fields.items()]
        ctype = td.TYPES.get(Path(path).suffix.lower(), "application/octet-stream")
        parts.append(f'--{b}\r\nContent-Disposition: form-data; name="{file_field}"; filename="{Path(path).name}"\r\nContent-Type: {ctype}\r\n\r\n'.encode()
                     + Path(path).read_bytes() + b"\r\n")
        parts.append(f"--{b}--\r\n".encode())
        req = urllib.request.Request(base, data=b"".join(parts), headers={"Content-Type": f"multipart/form-data; boundary={b}"})
    else:
        req = urllib.request.Request(base, data=json.dumps(fields).encode(), headers={"Content-Type": "application/json"})
    try:
        r = json.loads(urllib.request.urlopen(req, timeout=90).read())
    except Exception as e:  # noqa: BLE001
        body = getattr(e, "read", lambda: str(e).encode())()
        return {"ok": False, "description": body[:300].decode("utf-8", "ignore") if isinstance(body, bytes) else str(body)}
    return r


def send_text(txt, silent=False):
    return tg("sendMessage", {"chat_id": CHAT, "message_thread_id": THREAD, "text": txt[:4000], "disable_notification": silent})


def send_media(caption, path, silent=False):
    p = str(ROOT / path)
    ext = Path(p).suffix.lower()
    f = {"chat_id": CHAT, "message_thread_id": THREAD, "caption": caption[:1000], "disable_notification": "true" if silent else "false"}
    if ext == ".mp4":
        f["supports_streaming"] = "true"
        return tg("sendVideo", f, "video", p)
    return tg("sendPhoto", f, "photo", p)


def load_state():
    s = td.jload(STATE, {}) or {}
    s.setdefault("sent", {})
    s.setdefault("digest", {})
    return s


def save_state(s):
    STATE.write_text(json.dumps(s, ensure_ascii=False, indent=1), encoding="utf-8")


# ---------- 묶음 ----------
def dday_text(g):
    d = g.get("dday")
    return "" if d is None else (f"D-{d}" if d > 0 else ("D-DAY" if d == 0 else f"D+{-d}"))


def digest_parts(d=None):
    d = d or td.studio()
    g = d["goal"]
    ask = [c for c in d["ask"] if not c.get("answer")]
    answered = [c for c in d["ask"] if c.get("answer")]
    head = [f"📬 PitCrew 묶음 {dt.datetime.now():%m/%d %H:%M}",
            f"🎯 목표: {g['name']} · {dday_text(g)} · 카드 {g['done']}/{g['total']} 완료 · 동시 진행 {g['wip']}/{g['wip_limit']}"]
    if ask:
        head.append(f"✋ 판정 {len(ask)}장 — 아래 영상마다 답 하나: 「{ask[0]['id']} 통과」/「{ask[0]['id']} 수정: 한 줄」/「{ask[0]['id']} 버림」")
    else:
        head.append("✋ 판정: 없음" + (f" (답한 {', '.join(c['id'] for c in answered)} 는 PD 가 옮기는 중)" if answered else ""))
    w = d["warns"]
    head.append("⚠ 막힘: " + ("없음" if not w else w[0]["text"] + (f" 외 {len(w) - 1}건" if len(w) > 1 else "")))
    by = " · ".join(f"{td.WHO.get(k, k)} ${v:.2f}" for k, v in sorted(g["cost_by"].items(), key=lambda x: -x[1]))
    head.append(f"💰 비용: 오늘 ${g['cost']:.2f} / 한도 ${g['budget']:.0f}" + (f" ({by})" if by else "") + " — 팀원 Claude 기준")
    cards = []
    for c in ask[:3]:
        cap = (f"✋ {c['id']} {c['title']}\n❓ {c['question'] or '직접 해 보고 통과/수정/버림'}\n"
               f"검증 {c.get('verdict') or '-'} · 불통과 {c['fails']}\n"
               f"답: 「{c['id']} 통과」 / 「{c['id']} 수정: …」 / 「{c['id']} 버림」")
        media = c.get("clip") or c.get("shot_after") or c.get("strip")
        cards.append((cap, media))
    return "\n".join(head), cards


def send_digest(dry=False):
    text, cards = digest_parts()
    if dry:
        print(text)
        for cap, media in cards:
            print("---", media, "\n" + cap)
        return {"ok": True, "dry": True}
    r = send_text(text)
    res = {"ok": bool(r.get("ok")), "message_ids": [r.get("result", {}).get("message_id")], "errors": []}
    if not r.get("ok"):
        res["errors"].append(r.get("description"))
    for cap, media in cards:
        rr = send_media(cap, media) if media else send_text(cap)
        res["message_ids"].append(rr.get("result", {}).get("message_id"))
        if not rr.get("ok"):
            res["ok"] = False
            res["errors"].append(rr.get("description"))
    return res


# ---------- 즉시 알림 ----------
def alerts(s, d=None):
    """보낼 (key, 글, 매체) 목록 — 이미 보낸 key 는 뺀다."""
    d = d or td.studio()
    fl = td.flow()
    out = []
    g = d["goal"]
    lim = int(fl.get("fail_limit", 3))
    qmax = int(fl.get("dohyun_queue_max", 3))
    cards = td.load_cards()
    waiting = [c for c in cards if c["stage"] == "dohyun" and not c.get("answer")]
    # 1) 도현 판정 급함
    if len(waiting) >= qmax:
        ids = ",".join(sorted(c["id"] for c in waiting))
        out.append((f"askfull:{ids}", f"🚨 판정 대기가 {len(waiting)}장으로 꽉 참 — 새 카드가 「도현 한 판」으로 못 넘어감: {ids}\n답: 「C-xxx 통과/수정: …/버림」", None))
    for c in cards:
        if c["stage"] == "blocked" and c["owner"] == "dohyun":
            out.append((f"blocked:{c['id']}:{c['fails']}", f"🚨 {c['id']} {c['title'][:50]} — {c['fails']}번 불통과로 막힘. PD 가 2지선다를 대화에 올림", c.get("strip")))
    busy = any(l["working"] or l["queued"] for l in d["lanes"])
    active = any(c["stage"] in td.ACTIVE for c in cards)
    qh = fl.get("alert_quiet_hours", [1, 9])  # 밤: 「모두 쉬는 중」 알림은 아침으로 미룸, 나머지는 소리 없이
    night = qh[0] <= dt.datetime.now().hour < qh[1]
    if waiting and not busy and not active and not night:
        since = s.setdefault("idle_wait_since", time.time())
        if time.time() - since >= 60 * int(fl.get("idle_wait_alert_min", 60)):
            ids = ",".join(sorted(c["id"] for c in waiting))
            out.append((f"idlewait:{ids}", f"🚨 일꾼이 모두 쉬는 중 — 남은 일이 도현 판정뿐: {ids}. 영상은 21시/13시 묶음 또는 localhost:8765", None))
    else:
        s.pop("idle_wait_since", None)
    # 2) 빌드(과제) 실패 — queue.log 새 줄
    lines = td.read(QLOG).splitlines()
    n0 = s.get("qlog_n")
    if n0 is None or n0 > len(lines):
        n0 = len(lines)
    for ln in lines[n0:]:
        if " 보류 " in ln or re.search(r"컴파일 실패|scriptCompilationFailed=True|빌드 실패", ln):
            out.append((f"qlog:{ln[:60]}", f"🚨 과제 실패 — {ln.strip()[:200]}", None))
    s["qlog_n_next"] = len(lines)
    # 3) 같은 카드 반복 실패
    for c in cards:
        if c["fails"] >= lim - 1 and c["stage"] != "blocked":
            out.append((f"fails:{c['id']}:{c['fails']}", f"🚨 {c['id']} {c['title'][:50]} — {c['fails']}번 불통과({td.WHO.get(c['owner'], c['owner'])} 다시 하는 중). 한 번 더면 막힘", c.get("strip")))
    # 4) 비용 한도 근접
    day = dt.date.today().isoformat()
    if g["budget"]:
        pct = g["cost"] / g["budget"]
        for p in (1.0, 0.8):
            if pct >= p:
                out.append((f"cost:{day}:{int(p * 100)}", f"🚨 오늘 팀원 비용 ${g['cost']:.2f} — 한도 ${g['budget']:.0f} 의 {int(pct * 100)}%", None))
                break
    return [a for a in out if a[0] not in s["sent"]]


def run_alerts(s, dry=False):
    sent = []
    qh = td.flow().get("alert_quiet_hours", [1, 9])
    silent = qh[0] <= dt.datetime.now().hour < qh[1]
    for key, txt, media in alerts(s):
        if dry:
            print(key, "|", txt)
            continue
        r = send_media(txt, media, silent) if media else send_text(txt, silent)
        if r.get("ok"):
            s["sent"][key] = dt.datetime.now().isoformat(timespec="seconds")
            sent.append(key)
    if not dry:
        s["qlog_n"] = s.pop("qlog_n_next", s.get("qlog_n"))
    else:
        s.pop("qlog_n_next", None)
    return sent


def tick():
    s = load_state()
    now = dt.datetime.now()
    hours = td.flow().get("digest_hours", [13, 21])
    slot = f"{now:%Y-%m-%d}-{now.hour:02d}"
    if now.hour in hours and now.minute < 50 and slot not in s["digest"]:
        r = send_digest()
        s["digest"][slot] = {"at": now.isoformat(timespec="seconds"), "ok": r["ok"], "ids": r.get("message_ids"), "errors": r.get("errors")}
    run_alerts(s)
    save_state(s)


def loop():
    while True:
        try:
            tick()
        except Exception as e:  # noqa: BLE001 — 화면 프로세스가 죽지 않게
            (ROOT / ".ai_logs/team/tg_digest.err").write_text(f"{dt.datetime.now()} {e!r}", encoding="utf-8")
        time.sleep(60)


if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "dry"
    if cmd == "now":
        st = load_state()
        r = send_digest()
        st["digest"][f"manual-{dt.datetime.now():%Y-%m-%d-%H%M}"] = {"ok": r["ok"], "ids": r.get("message_ids"), "errors": r.get("errors")}
        save_state(st)
        print(json.dumps(r, ensure_ascii=False))
    elif cmd == "dry":
        send_digest(dry=True)
        print("=== 즉시 알림(보낼 것) ===")
        run_alerts(load_state(), dry=True)
    elif cmd == "alerts":
        st = load_state()
        print(run_alerts(st))
        save_state(st)
    else:
        print(__doc__)
