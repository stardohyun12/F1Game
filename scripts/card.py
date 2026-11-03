#!/usr/bin/env python
"""카드 흐름 도구 (2026-11-03 팀 재편 — docs/team/FLOW.md, 정의 docs/team/flow.json).

카드 = docs/team/cards/<ID>-<slug>/card.md (맨 위 --- 머리말이 기계용 상태).
넘김 기록 = docs/team/handoffs.jsonl (한 줄 = 한 번 넘김). 보드 스냅숏 = docs/team/board.json (화면이 읽음).

  python -X utf8 scripts/card.py new "<제목>" --slug repair [--start-place 9] [--opus]
  python -X utf8 scripts/card.py move <ID> <단계> [--by pd] [--note "..."] [--fail]
  python -X utf8 scripts/card.py dispatch <ID>      # 지금 단계 일꾼의 대기열에 과제 파일을 쓴다
  python -X utf8 scripts/card.py advance <ID>       # 일꾼 과제가 끝났을 때(team_queue.sh 가 부름): 증거 → 다음 단계 → 다음 일꾼
  python -X utf8 scripts/card.py hold <ID> "<이유>" # 일꾼 과제가 보류(실패)로 끝남 → 막힘
  python -X utf8 scripts/card.py board | log [N]
"""
import json, os, re, sys, subprocess, datetime, glob

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(ROOT)
FLOW = json.load(open("docs/team/flow.json", encoding="utf-8"))
STAGES = {s["id"]: s for s in FLOW["stages"]}
CARDS = "docs/team/cards"
HANDOFFS = "docs/team/handoffs.jsonl"
BOARD = "docs/team/board.json"
QUEUE = ".claude/team/queue"


def now():
    return datetime.datetime.now().astimezone().isoformat(timespec="seconds")


def card_dir(cid):
    m = glob.glob(f"{CARDS}/{cid}-*") + glob.glob(f"{CARDS}/{cid}")
    if not m:
        sys.exit(f"카드 없음: {cid}")
    return m[0].replace("\\", "/")


def read(cid):
    d = card_dir(cid)
    txt = open(f"{d}/card.md", encoding="utf-8").read()
    m = re.match(r"---\n(.*?)\n---\n", txt, re.S)
    head = {}
    for ln in m.group(1).splitlines():
        if ":" in ln:
            k, v = ln.split(":", 1)
            head[k.strip()] = v.strip()
    return d, head, txt[m.end():]


def write(d, head, body):
    h = "\n".join(f"{k}: {v}" for k, v in head.items())
    open(f"{d}/card.md", "w", encoding="utf-8", newline="\n").write(f"---\n{h}\n---\n{body}")


def qlog(msg):
    with open(".ai_logs/team/queue.log", "a", encoding="utf-8", newline="\n") as f:
        f.write(f"[{datetime.datetime.now():%m/%d %H:%M}] card {msg}\n")


def handoff(cid, frm, to, s_from, s_to, note, **extra):
    rec = {"ts": now(), "card": cid, "from": frm, "to": to, "stage_from": s_from, "stage_to": s_to, "note": note}
    rec.update({k: v for k, v in extra.items() if v is not None})
    with open(HANDOFFS, "a", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(rec, ensure_ascii=False) + "\n")
    qlog(f"{cid} {s_from}→{s_to} ({frm}→{to}) {note[:80]}")


def board():
    cards = []
    for d in sorted(glob.glob(f"{CARDS}/C-*")):
        if not os.path.exists(f"{d}/card.md"):
            continue
        cid = os.path.basename(d).split("-")[0] + "-" + os.path.basename(d).split("-")[1]
        _, h, _ = read(cid)
        ev = {}
        try:
            ev = json.load(open(f"{d}/evidence.json", encoding="utf-8"))
        except Exception:
            pass
        cards.append({"id": h.get("id"), "title": h.get("title"), "stage": h.get("stage"), "owner": h.get("owner"),
                      "fails": int(h.get("fails", 0)), "updated": h.get("updated"), "dir": d.replace("\\", "/"),
                      "machine": ev.get("machine"), "clip": (ev.get("after") or {}).get("clip"),
                      "strip": (ev.get("after") or {}).get("strip")})
    snap = {"updated": now(), "goal": FLOW["goal"], "stages": [{"id": s["id"], "label": s["label"]} for s in FLOW["stages"]],
            "workers": {k: v["label"] for k, v in FLOW["workers"].items()}, "cards": cards,
            "dohyun_waiting": [c["id"] for c in cards if c["stage"] == "dohyun"][: FLOW["dohyun_queue_max"]]}
    json.dump(snap, open(BOARD, "w", encoding="utf-8", newline="\n"), ensure_ascii=False, indent=1)
    return snap


def move(cid, to, by=None, note="", fail=False, verdict=None, evidence=None):
    d, h, body = read(cid)
    frm = h["stage"]
    if to != "blocked" and to not in FLOW["transitions"].get(frm, []):
        sys.exit(f"안 되는 넘김: {frm} → {to} (허용 {FLOW['transitions'].get(frm)})")
    # 도현 판정 칸을 떠나면 그 답은 dohyun-<n>.json 으로 — 다음 판 때 웹 버튼이 「답함」으로 숨지 않게(D-273)
    if frm == "dohyun" and to != "dohyun" and os.path.exists(f"{d}/dohyun.json"):
        k = len(glob.glob(f"{d}/dohyun-*.json")) + 1
        os.replace(f"{d}/dohyun.json", f"{d}/dohyun-{k}.json")
    fails = int(h.get("fails", 0)) + (1 if fail else 0)
    if fail and fails >= FLOW["fail_limit"]:
        note = f"{note} | 3회 규칙: {fails}번 불통과 → 멈춤, 도현 2지선다"
        to = "blocked"
    owner = "dohyun" if to == "blocked" and fail else (STAGES[to]["owner"] or "-")
    if to == "blocked" and not fail:
        owner = "pd"
    by = by or h.get("owner", "pd")
    h.update(stage=to, owner=owner, fails=str(fails), updated=now())
    body = body.rstrip("\n") + f"\n- {now()[:16]} {by}→{owner} · {STAGES[frm]['label']} → {STAGES[to]['label']} · {note}\n"
    write(d, h, body)
    handoff(cid, by, owner, frm, to, note, verdict=verdict, evidence=evidence, fails=fails)
    board()
    return to


def next_num(q):
    os.makedirs(q, exist_ok=True)
    return 60


def dispatch(cid, by="pd"):
    d, h, body = read(cid)
    st, title = h["stage"], h["title"]
    opus = "[OPUS]" if h.get("opus", "") in ("true", "yes", "1") and st in ("greybox", "integrate") else ""
    tries = int(h.get("fails", 0))
    if st in ("greybox", "integrate"):
        last = ""
        vp = f"{d}/verdict.json"
        if os.path.exists(vp):
            v = json.load(open(vp, encoding="utf-8"))
            if v.get("verdict") == "FAIL":
                last = f"직전 검증 불통과({tries}회째) — 판정 원문: {v.get('reason','')}\n근거 프레임: {v.get('frames','')}\n먼저 이것부터 고친다.\n"
        mfail = ""
        try:
            ev = json.load(open(f"{d}/evidence.json", encoding="utf-8"))
            if ev.get("machine") == "FAIL":
                a = ev.get("after", {})
                mfail = f"직전 기계 확인 FAIL: 프레임 +{a.get('frames_advanced')} · 에러 {a.get('errors')} · {a.get('samples')}\n"
        except Exception:
            pass
        task = f"""[EDITOR]{opus}[{cid} {STAGES[st]['label']}] {title}
카드: {d}/card.md — 본문은 브리프(이 과제 글) 안에 있다. 카드의 「범위」 파일만 만지고 「하지 말 것」은 건드리지 않는다. 카드 밖 일·아트·다듬기는 하지 않는다.
{last}{mfail}단계 = {STAGES[st]['label']}: {'회색 대역으로 수용 기준이 실제로 돌게만 만든다(겉모습은 나중 — 아트 단계가 따로 있다).' if st == 'greybox' else '도현이 통과시킨 그레이박스에 아트 납품을 끼워 넣고 수용 기준이 그대로 도는지.'}
끝낼 것(이 넷이 없으면 미완료). 시간 상자 60분 — 40분이 지나면 start.cs·critN.cs·보고서부터 지금 상태로 쓰고 이어서:
1. {d}/start.cs — Play 중 exec 로 실행되는 C# 조각. 카드 「시작 장면」(start_place {h.get('start_place','?')}, 하네스가 먼저 순간이동함)에서 수용 기준의 동작을 10초 안에 화면에 보여 준다(입력 흉내·순간이동·EditorApplication.update 콜백 허용, 마지막 줄 return "<한 줄 상태>";). 하네스는 이걸로 10초 클립(프레임 증가·에러 0)을 찍는다. 네가 직접 한 번 돌려 확인.
2. {d}/crit1.cs, crit2.cs, … — **수용 기준 번호마다 하나.** 각각 새 Play 에서 start_place 순간이동 뒤 exec 되며, 순간이동·상태 세팅(단계 값·든 물건·카트 위치·시선)으로 **그 기준이 화면에 보이는 순간**을 2초 안에 만든다. 하네스가 2초 뒤 캡처 1장(critN.png, 1×+3×)을 찍고 검증 일꾼은 기준 N 을 그 사진으로 판정한다 — 긴 흐름을 클립 하나에 욱여넣지 마라(지난 불통과의 원인). 마지막 줄 return "<그 기준의 상태 값>";. 네가 각각 돌려 화면을 확인.
3. 보고서(4요소) — 무엇을 바꿨나·파일 목록. 스크린샷은 1~2장만(증거 묶음은 하네스가 tools/evidence/bundle.sh 로 따로 찍는다).
4. 카드의 「기계 확인」 항목을 실제로 돌린 결과를 보고서에.
"""
        q = f"{QUEUE}/editor"
    elif st in ("verify", "final_verify"):
        # 지난 판정은 verdict-<n>.json 으로 옮겨 둔다 — 검증 일꾼이 못 쓰고 끝나면 옛 판정을 새 판정으로 읽지 않게
        if os.path.exists(f"{d}/verdict.json"):
            k = len(glob.glob(f"{d}/verdict-*.json")) + 1
            os.replace(f"{d}/verdict.json", f"{d}/verdict-{k}.json")
        ev = f"{d}/evidence.json"
        accept = re.search(r"## 수용 기준.*?(?=\n## )", body, re.S)
        human = re.search(r"## 사람 확인.*?(?=\n## |\Z)", body, re.S)
        task = f"""[{cid} 검증] {title}
너는 블라인드 검증자다. 이 과제 글에 적힌 파일만 본다 — 코드·git diff·작업자 보고서(docs/team/reports)·card.md 의 다른 절은 열지 마라. 「재밌나」는 판정하지 않는다(도현 몫).
{accept.group(0) if accept else '(수용 기준 없음 → FAIL)'}

증거 묶음(이것만 본다):
- {ev} — 기계 확인(frames_ok = 프레임이 실제로 늘었나, log_ok = 에러 0, warnings 수·samples, after.crit = 기준별 사진 목록·crit_missing = 빠진 번호)
- {d}/evidence/after/crit<N>.png (+ crit<N>_3x.png 3배 확대) — **수용 기준 N 은 critN.png 로 판정한다.** 그 번호 사진이 없으면(crit_missing) 그 기준은 strip 으로 찾고, 거기도 없으면 안 보임.
- {d}/evidence/after/strip.png — 클립 10초를 시간 순 8칸(왼쪽 위 → 오른쪽 아래). 흐름이 돌았나(프레임 증가)·기준 사진이 없는 기준용
- {d}/evidence/after/shot.png · {d}/evidence/before/shot.png — 전/후 한 쌍
(클립 원본 {d}/evidence/after/clip.mp4 는 도현용 — 너는 strip 으로 본다)

판정: 수용 기준 문장마다 「보인다/안 보인다」 + 근거(critN.png, strip 몇 번째 칸, 또는 evidence.json 값). 하나라도 안 보이거나 기계 확인이 false 면 FAIL.
결과를 {d}/verdict.json 에 쓴다(덮어씀): {{"verdict":"PASS|FAIL","reason":"한 줄","frames":"crit2.png · strip 3·5칸","criteria":[{{"text":"...","seen":true,"evidence":"..."}}],"by":"verify"}}
보고서는 verdict 한 줄 + 표만. 다른 파일은 고치지 않는다.
"""
        q = f"{QUEUE}/verify"
    elif st == "art":
        eng = "codex"
        try:
            eng = json.load(open(".claude/team/config.json", encoding="utf-8")).get("art", {}).get("engine", "codex")
        except Exception:
            pass
        task = f"""[{cid} 아트] {title}
카드: {d}/card.md 의 「아트」 절에 적힌 것만 만든다(카드에 안 묶인 아트 금지). 도현 한 판 판정 원문이 카드 끝 기록에 있다 — 그대로 따른다.
산출: 후보 시트 1장(게임 화면 크롭 옆, 같은 배율) + 파일 경로. F1/Assets 투입은 통합 단계에서 구현 일꾼이 한다.
"""
        q = f"{QUEUE}/{'art' if eng == 'codex' else 'studio'}"
    elif st == "research":
        task = f"""[{cid} 리서치] {title}
카드: {d}/card.md 의 「리서치 질문」 절의 질문에만 답한다. 원문을 직접 연 출처만 근거로 센다. 결과는 {d}/research.md 한 장(질문별 답 3줄 + 출처). 코드·씬 수정 금지.
"""
        q = f"{QUEUE}/research"
    else:
        print(f"{cid}: 단계 {st} 는 일꾼 과제가 없음(PD/도현 차례)")
        return None
    os.makedirs(q, exist_ok=True)
    path = f"{q}/{next_num(q)}-{cid}-{st}-{tries}.task"
    open(path, "w", encoding="utf-8", newline="\n").write(task)
    worker = STAGES[st]["owner"]
    handoff(cid, by, worker, st, st, f"대기열에 과제 → {path}")
    print(path)
    return path


def advance(cid, bundle_rc=None):
    d, h, _ = read(cid)
    st = h["stage"]
    if st in ("greybox", "integrate"):
        # 증거 묶음은 bash 쪽(team_queue.sh)이 먼저 찍는다: bash tools/evidence/bundle.sh <ID> after
        # (이 PC 에서 파이썬 subprocess → ucli-http.sh 는 0xC0000005 로 죽는다 — 그래서 여기서 부르지 않음)
        rc = int(bundle_rc) if bundle_rc is not None else 0
        try:
            ev = json.load(open(f"{d}/evidence.json", encoding="utf-8"))
        except Exception:
            ev = {}
        a = ev.get("after", {})
        evp = f"{d}/evidence.json"
        nxt = "verify" if st == "greybox" else "final_verify"
        if rc != 0 or ev.get("machine") != "PASS":
            # 기계 확인 FAIL = 같은 단계에서 다시 구현(검증 일꾼 시간을 안 씀). 같은 단계 넘김이라 move() 대신 직접 기록
            d2, h2, b2 = read(cid)
            fails = int(h2.get("fails", 0)) + 1
            note = f"기계 확인 FAIL(rc={rc}, 프레임 +{a.get('frames_advanced')}, 에러 {a.get('errors')})"
            if fails >= FLOW["fail_limit"]:
                h2.update(fails=str(fails))
                write(d2, h2, b2)
                move(cid, "blocked", by="harness", note=note + " | 3회 규칙 → 도현 2지선다", evidence=evp)
                return
            h2.update(fails=str(fails), updated=now())
            write(d2, h2, b2.rstrip("\n") + f"\n- {now()[:16]} harness→build · {note} · 다시 구현\n")
            handoff(cid, "harness", "build", st, st, note, evidence=evp, fails=fails)
            board()
            dispatch(cid, by="harness")
            return
        move(cid, nxt, by="build", note=f"증거 묶음 기계 PASS(프레임 +{a.get('frames_advanced')}, 경고 {a.get('warnings')}, 에러 0) → 블라인드 검증", evidence=evp)
        dispatch(cid, by="harness")
    elif st in ("verify", "final_verify"):
        vp = f"{d}/verdict.json"
        try:
            v = json.load(open(vp, encoding="utf-8"))
        except Exception:
            hold(cid, "검증 일꾼이 verdict.json 을 안 씀")
            return
        if v.get("verdict") == "PASS":
            to = "dohyun" if st == "verify" else "done"
            move(cid, to, by="verify", note=f"검증 PASS — {v.get('reason','')}", verdict="PASS", evidence=vp)
        else:
            back = "greybox" if st == "verify" else "integrate"
            to = move(cid, back, by="verify", fail=True, note=f"검증 FAIL — {v.get('reason','')}", verdict="FAIL", evidence=vp)
            if to == back:
                dispatch(cid, by="harness")
    elif st == "art":
        move(cid, "integrate", by="art", note="아트 납품 → 통합")
        dispatch(cid, by="harness")
    elif st == "research":
        move(cid, "plan", by="research", note="질문 답 → PD 가 카드 고침")
    else:
        print(f"{cid}: {st} 에서 advance 할 것 없음")


def note(cid, text, fails=None, by="pd"):
    """같은 단계 안의 기록(예: 하네스 오류로 센 불통과 정정). 넘김 기록에도 남긴다."""
    d, h, body = read(cid)
    if fails is not None:
        h["fails"] = str(int(fails))
    h["updated"] = now()
    write(d, h, body.rstrip("\n") + f"\n- {now()[:16]} {by} · {text}\n")
    handoff(cid, by, h.get("owner", "pd"), h["stage"], h["stage"], text, fails=int(h.get("fails", 0)))
    board()


def hold(cid, why):
    move(cid, "blocked", by="harness", note=f"일꾼 과제 보류 — {why}")


def new(title, slug, start_place=None, opus=False):
    os.makedirs(CARDS, exist_ok=True)
    nums = [int(m.group(1)) for p in glob.glob(f"{CARDS}/C-*") if (m := re.match(r"C-(\d+)", os.path.basename(p)))]
    cid = f"C-{(max(nums) + 1) if nums else 1:03d}"
    d = f"{CARDS}/{cid}-{slug}"
    os.makedirs(d)
    tpl = open("docs/team/CARD_TEMPLATE.md", encoding="utf-8").read()
    body = tpl.split("<!-- 카드 본문 시작 -->", 1)[1]
    head = {"id": cid, "title": title, "goal": FLOW["goal"]["name"], "stage": "plan", "owner": "pd", "fails": "0",
            "start_place": "" if start_place is None else str(start_place), "opus": "true" if opus else "false",
            "created": now(), "updated": now()}
    write(d, head, body)
    handoff(cid, "pd", "pd", None, "plan", "카드 만듦")
    board()
    print(d)


def show_board():
    b = board()
    print(f"목표: {b['goal']['name']} (마감 {b['goal']['deadline']})")
    for s in FLOW["stages"]:
        cs = [c for c in b["cards"] if c["stage"] == s["id"]]
        if cs:
            print(f"[{s['label']}] " + " · ".join(f"{c['id']} {c['title'][:40]} ({c['owner']}, 불통과 {c['fails']})" for c in cs))
    print("도현 판정 대기:", ", ".join(b["dohyun_waiting"]) or "-")


def main(a):
    if not a:
        print(__doc__); return
    cmd = a[0]
    opt = lambda k, dflt=None: a[a.index(k) + 1] if k in a else dflt
    if cmd == "new":
        new(a[1], opt("--slug", "card"), opt("--start-place"), "--opus" in a)
    elif cmd == "move":
        move(a[1], a[2], by=opt("--by"), note=opt("--note", ""), fail="--fail" in a)
    elif cmd == "dispatch":
        dispatch(a[1])
    elif cmd == "advance":
        advance(a[1], opt("--bundle-rc"))
    elif cmd == "note":
        note(a[1], a[2], fails=opt("--fails"), by=opt("--by", "pd"))
    elif cmd == "hold":
        hold(a[1], a[2] if len(a) > 2 else "")
    elif cmd == "board":
        show_board()
    elif cmd == "log":
        n = int(a[1]) if len(a) > 1 else 15
        for ln in open(HANDOFFS, encoding="utf-8").read().splitlines()[-n:]:
            r = json.loads(ln)
            print(f"{r['ts'][5:16]} {r['card']} {r['from']}→{r['to']} {r.get('stage_from')}→{r['stage_to']} · {r['note'][:90]}")
    else:
        print(__doc__)


if __name__ == "__main__":
    main(sys.argv[1:])
