#!/usr/bin/env python
"""브리프 한 장 생성기 (카디널 D-281, docs/CARDINAL.md §1 「작가」) — 구현이 읽는 유일한 시작 글.

  python -X utf8 scripts/brief.py <과제파일> [--max N]   → stdout 마크다운 (목표 1만 자 안, 글자 수 기준)

순서: (a) 과제 글 (b) world/SPEC.md 발췌 — 과제 글에 나온 장소·주민·물건 이름이 든 절, 없으면 §0~1(톤·금지)만. SPEC.md 가 없으면 생략
      (c) 훅이 못 막는 필수 규칙 (d) 세계 스냅숏 docs/world/WORLD.md 전문
      (e) 파일 지도 — 과제 글의 .cs 마다 class/enum/public 메서드 + 줄 번호, 참조하는 .cs 목록
      (f) 에디터 명령 치트
run_task.sh 가 구현을 부를 때 과제 글 대신 이 출력을 넘긴다(실패하면 과제 글 그대로). 규칙은 글이 아니라 훅(.claude/hooks/guard-pre.py)이 막는다 —
여기 적는 규칙은 훅이 못 막는 것(보고 양식·끝 조건·승인 순서)만. 옛 카드·규칙 문서 읽기는 archive/harness-v1/scripts/brief.py.
"""
import glob
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
os.chdir(ROOT)
SRC_ROOT = "F1/Assets"
MAX_FILES = 12          # 파일 지도 상한(글자 예산)
MAX_MEMBERS = 10        # 파일당 멤버 줄 상한
MAX_REFS = 6            # 파일당 참조 파일 상한

# (c) 훅이 못 막는 규칙 — CARDINAL §4 끝 조건 + 승인 순서. 훅이 막는 것(봇 전체 경로·sleep 대기·잠근 파일·규칙 문서 읽기)은 적지 않는다.
RULES = """## 필수 규칙 (훅이 못 막는 것만 — 나머지는 훅이 막는다)
1. 한국어, 짧고 직설. 실행 안 한 것은 "안 했다"고 쓴다.
2. 끝 조건(CARDINAL §4): 컴파일 0 에러 · 불변 검사 통과(`bash scripts/world_check.sh`) · 규칙 테스트 통과 · 네가 찍은 그림 3장(1×, docs/qa/) · 보고 4줄(한 것 파일:줄 / 확인 방법 / 증거 / 남은 것).
3. git commit/push 금지(run_task.sh 가 함). 폰트 SDF(F1/Assets/UI/Fonts)는 끝에 `git checkout --`.
4. 과제 범위 파일만. 범위 밖 개선·아트·도구는 「남은 것」 한 줄로만. 씬을 직접 찔러 고치지 않는다(명세→생성기→검사, 좌표를 손으로 쓰지 않는다).
5. 확인 = 순간이동(DebugWindow.GoPlace)으로 고친 자리에서 바로 + 캡처 + "안 됐으면 실패할 검사 1개". 숫자만으로 "고쳤음" 금지(같은 카메라 전/후).
6. Play 중엔 .cs 를 고치지 않는다(재컴파일 → 상태 날아감). 끝에 `editor stop`, 원래 씬, 저장 안 된 변경 0.
7. 도현이 정한 값(해상도 360·Nearest, 건물 위치·높이·노선, 차 조작값)·공유 .mat·ProjectSettings 는 바꾸지 않는다 — 필요하면 멈추고 보고.
8. 아트: AI 생성 이미지·3D·소리 금지, CC0 만(출처 art/provenance.csv). 간판 글자는 도현이 정한 이름만. 아트·리서치는 과제 안 한 줄 범위만.
9. 버그 1개 고치면 같은 버그를 잡을 불변 조건 1개(`WorldInvariants.cs`) 추가.
10. 이 브리프가 시작 글의 전부다. 다른 규칙 문서는 열지 않는다(훅이 막는다)."""

CHEATS = """## 에디터 명령 치트
- 실행: `bash scripts/ucli-http.sh exec 'return DebugWindow.GoPlace(12);'` (긴 C# 는 파일로 `exec @docs/qa/tmp/<이름>.cs`; `Object` 는 `UnityEngine.Object` 로 완전 한정)
- Play: `bash scripts/ucli-http.sh editor play --wait` → 바로 `exec 'Application.runInBackground = true; return "ok";'` → 끝에 `editor stop`
- 콘솔: `bash scripts/ucli-http.sh console --type error` (에러 0 이어야 끝)
- 순간이동: `DebugWindow.GoPlace(n)` — n 은 아래 세계 스냅숏 「장소」 표 순서(0부터). 카트는 장소의 카트 규칙대로 같이 옮겨진다
- 캡처: Play 중 `ScreenCapture.CaptureScreenshot("docs/qa/<과제>/<n>.png")` 3장(1×) — 도현이 보는 화면(LowResRenderTest.Present → RenderTexture)"""

MEMBER_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*(?:public|internal)\s+(?:(?:static|override|virtual|abstract|sealed|async|readonly|new|partial|unsafe|extern)\s+)*"
    r"(?P<kind>class|struct|enum|interface)?\s*(?P<type>[\w<>\[\],\.\?]+)?\s*(?P<name>\w+)\s*(?P<paren>\()?", re.M)
TYPE_RE = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|internal|private|protected|static|sealed|abstract|partial)\s+)*(?P<kind>class|struct|enum|interface)\s+(?P<name>\w+)", re.M)
PUB_RE = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*public\s+(?:(?:static|override|virtual|abstract|sealed|async|new|unsafe|extern)\s+)*(?!class\b|struct\b|enum\b|interface\b)(?P<type>[\w<>\[\],\.\?]+)\s+(?P<name>\w+)\s*(?P<tail>\(|\{|=>|;|=)", re.M)


def read(p):
    try:
        return open(p, encoding="utf-8", errors="replace").read().replace("\r\n", "\n").replace("\r", "\n")
    except Exception:
        return ""


def rel(p):
    return os.path.relpath(p, ROOT).replace("\\", "/")


def find_cs(token):
    """토큰 하나(절대/상대 경로·파일 이름·디렉터리/*)를 리포 안 .cs 경로 목록으로."""
    t = token.strip().strip("`'\"()[],:;").replace("\\", "/")
    if not t:
        return []
    if t.endswith("/*") or t.endswith("/"):
        d = t.rstrip("/*")
        hits = []
        for base in (d, f"{SRC_ROOT}/{d}", f"{SRC_ROOT}/Scripts/{d}"):
            hits += glob.glob(f"{base}/**/*.cs", recursive=True)
        if not hits:   # 상대 디렉터리 이름(예: Packing/Bench) 을 어디서든 찾음
            hits = [p for p in glob.glob(f"{SRC_ROOT}/**/{d}/**/*.cs", recursive=True)]
        return sorted(set(rel(h) for h in hits))
    if not t.endswith(".cs"):
        return []
    if os.path.isfile(t):
        return [rel(t)]
    base = os.path.basename(t)
    return sorted(set(rel(h) for h in glob.glob(f"{SRC_ROOT}/**/{base}", recursive=True)))


def scope_tokens(text):
    """과제 글(「범위」 절이 있으면 거기 먼저)에서 .cs 경로·파일 이름·디렉터리/* 토큰."""
    toks = []
    m = re.search(r"## 범위.*?(?=\n## |\Z)", text, re.S)
    if m:
        for ln in m.group(0).splitlines()[1:]:
            ln = ln.lstrip("- ").strip()
            for part in re.split(r"[,，、]\s*|\s+\(|\)|\s+—\s+", ln):
                part = part.strip()
                if part.endswith(".cs") or part.endswith("/*"):
                    toks.append(part)
    toks += re.findall(r"[\w./\\-]+\.cs\b", text)
    seen, out = set(), []
    for t in toks:
        if t not in seen:
            seen.add(t)
            out.append(t)
    return out


def members(path):
    src = read(path)
    lines = src.split("\n")
    out = []
    for i, ln in enumerate(lines, 1):
        m = TYPE_RE.match(ln)
        if m:
            out.append((i, f"{m.group('kind')} {m.group('name')}"))
            continue
        m = PUB_RE.match(ln)
        if m and m.group("tail") == "(":   # public 메서드만(프로퍼티·필드는 글자 예산 때문에 뺌)
            out.append((i, f"{m.group('name')}()"))
    types = [n.split(" ", 1)[1] for _, n in out if n.split(" ", 1)[0] in ("class", "struct", "enum", "interface")]
    return out, types, len(lines)


def referrers(types, self_path, cache):
    if not types:
        return []
    rx = re.compile(r"\b(" + "|".join(re.escape(t) for t in types) + r")\b")
    hits = []
    for p, src in cache.items():
        if p == self_path:
            continue
        if rx.search(src):
            hits.append(os.path.basename(p))
    return sorted(hits)


def file_map(files):
    if not files:
        return "## 파일 지도\n(과제·카드에 .cs 경로 없음)"
    cache = {rel(p): read(p) for p in glob.glob(f"{SRC_ROOT}/**/*.cs", recursive=True)}
    parts = ["## 파일 지도 (범위 파일마다 멤버:줄 · 이 파일을 참조하는 .cs)"]
    files = [f for f in files if f.startswith(SRC_ROOT + "/")]   # 카드 폴더의 start.cs 같은 비게임 .cs 는 뺌
    # partial class(PackBench.cs · PackBench.Day.cs …)는 한 줄로 묶는다
    groups, order = {}, []
    for f in files:
        key = os.path.dirname(f) + "/" + os.path.basename(f).split(".")[0]
        if key not in groups:
            groups[key] = []
            order.append(key)
        groups[key].append(f)
    for key in order[:MAX_FILES]:
        fs = sorted(groups[key], key=len)   # 본 파일(PackBench.cs)이 먼저, partial(.Day.cs …)은 뒤
        if not all(os.path.isfile(f) for f in fs):
            parts.append(f"- {key}.cs — (없음)")
            continue
        mem, types, n = [], [], 0
        for f in fs:
            m1, t1, n1 = members(f)
            tag = "" if len(fs) == 1 else f"[{os.path.basename(f).split('.', 1)[1].rsplit('.cs', 1)[0] or 'cs'}]"
            mem += [(ln, name + tag) for ln, name in m1]
            types += [t for t in t1 if t not in types]
            n += n1
        shown = mem[:MAX_MEMBERS]
        mtxt = " · ".join(f"{name}:{ln}" for ln, name in shown) + (f" · …+{len(mem)-MAX_MEMBERS}" if len(mem) > MAX_MEMBERS else "")
        refs = sorted(set(referrers(types, fs[0], cache)) - {os.path.basename(f) for f in fs})
        rtxt = ", ".join(refs[:MAX_REFS]) + (f" +{len(refs)-MAX_REFS}" if len(refs) > MAX_REFS else "") if refs else "없음"
        label = fs[0] if len(fs) == 1 else f"{key}.cs (+{', '.join(os.path.basename(f) for f in fs[1:])})"
        parts.append(f"- **{label}** ({n}줄): {mtxt or '(public 멤버 없음)'}\n  참조: {rtxt}")
    if len(order) > MAX_FILES:
        parts.append(f"- (…{len(order)-MAX_FILES}개 더: {', '.join(os.path.basename(x) for x in order[MAX_FILES:])})")
    return "\n".join(parts)


SPEC_PATH = os.environ.get("BRIEF_SPEC", "world/SPEC.md")   # 시험용 덮어쓰기(BRIEF_SPEC=경로)
STOP = {"과제", "구현", "확인", "보고", "파일", "에디터", "씬", "코드", "테스트", "검사", "그림", "캡처", "장면", "세계", "명세", "도현", "규칙", "금지", "이름"}


def _split_sections(text):
    """'#'·'##'·'###' 헤딩마다 (level, heading, body) — 헤딩 없는 머리글은 level 0."""
    out, cur = [], [0, "", []]
    for ln in text.split("\n"):
        m = re.match(r"^(#{1,3})\s+(.*)$", ln)
        if m:
            out.append(cur)
            cur = [len(m.group(1)), m.group(2).strip(), []]
        else:
            cur[2].append(ln)
    out.append(cur)
    return [(lv, h, "\n".join(b).strip()) for lv, h, b in out if h or "\n".join(b).strip()]


def _names(sec_text, heading):
    """절이 정의하는 이름들: 헤딩 · **굵은 글** · `- 이름:`/`- 이름 —` 목록 키 · `코드`."""
    names = set()
    h = re.sub(r"^[\d§.\s]+", "", heading).strip()
    if h:
        names.add(h)
        names.update(re.split(r"[\s·/(),:—-]+", h))
    names.update(re.findall(r"\*\*([^*\n]{2,30})\*\*", sec_text))
    names.update(re.findall(r"`([^`\n]{2,30})`", sec_text))
    names.update(re.findall(r"^\s*[-*]\s+([^\s:—(（]{2,20})\s*[:—(（]", sec_text, re.M))
    names.update(re.findall(r"^\|\s*([^|\n]{2,20}?)\s*\|", sec_text, re.M))
    return {n.strip() for n in names if len(n.strip()) >= 2 and n.strip() not in STOP and not re.fullmatch(r"[\d.§]+", n.strip())}


def spec_excerpt(task):
    """world/SPEC.md 에서 과제 글에 나온 장소·주민·물건 이름이 든 절. 없으면 §0~1(톤·금지). 파일 없으면 ''."""
    spec = read(SPEC_PATH).strip()
    if not spec:
        return ""
    secs = _split_sections(spec)
    hit, hit_names = [], set()
    for i, (lv, h, body) in enumerate(secs):
        if lv == 0:
            continue
        found = {n for n in _names(body, h) if n in task}
        if found:
            hit.append(i)
            hit_names |= found
    if hit:
        # 맞은 ### 절은 그 위 ## 헤딩을 같이 보여 문맥을 남긴다
        keep = set(hit)
        for i in hit:
            if secs[i][0] == 3:
                for j in range(i - 1, -1, -1):
                    if secs[j][0] <= 2:
                        keep.add(j)
                        break
        parts = [(f"{'#' * lv} {h}\n{body}" if i in hit else f"{'#' * lv} {h}") for i, (lv, h, body) in enumerate(secs) if i in keep]
        why = "과제 글에 나온 이름: " + ", ".join(sorted(hit_names)[:12])
    else:
        tone = [i for i, (lv, h, _) in enumerate(secs) if lv >= 1 and (re.search(r"톤|금지|^0\b|^1\b|§0|§1", h))]
        if not tone:
            tone = [i for i, (lv, h, _) in enumerate(secs) if lv == 2][:2]
        parts = [f"{'#' * lv} {h}\n{body}" for i, (lv, h, body) in enumerate(secs) if i in tone]
        why = "과제 글에 명세 이름이 없어 톤·금지 절만"
    if not parts:
        return ""
    return f"## 세계 명세 발췌 (world/SPEC.md — {why}. 전문은 열지 말 것, 필요한 절은 PD 에게)\n" + "\n\n".join(parts)


def build(task_path, max_chars=10000):
    task = read(task_path).strip()
    sec = []
    # 첫 줄 = 과제 첫 줄 그대로(team.sh 가 상태·[EDITOR]/[OPUS] 태그를 첫 줄에서 읽는다)
    sec.append(task + "\n\n[브리프 한 장 — 카디널 D-281] 위 과제 + 아래 명세 발췌·필수 규칙·세계 스냅숏·파일 지도·에디터 치트가 시작 글의 전부다. 다른 규칙 문서는 열지 않는다.")
    spec = spec_excerpt(task)
    if spec:
        sec.append(spec)
    sec.append(RULES)
    world = read("docs/world/WORLD.md").strip()
    if world:
        sec.append("## 세계 스냅숏 (docs/world/WORLD.md 전문 — 끝에 world_check.sh 가 불변 조건을 검사한다)\n" + world)
    toks = scope_tokens(task)
    files = []
    for t in toks:
        for f in find_cs(t):
            if f not in files:
                files.append(f)
    sec.append(file_map(files))
    sec.append(CHEATS)
    out = "\n\n".join(sec) + "\n"
    # 예산 초과면 파일 지도부터 줄인다(규칙·명세·스냅숏은 자르지 않는다) — 그래도 넘으면 그대로 내고 stderr 에 알린다
    if len(out) > max_chars and files:
        global MAX_MEMBERS, MAX_REFS
        MAX_MEMBERS, MAX_REFS = 8, 4
        sec[-2] = file_map(files)
        out = "\n\n".join(sec) + "\n"
    if len(out) > max_chars:
        sys.stderr.write(f"brief: {len(out)}자 — 목표 {max_chars}자 초과(스냅숏 {len(world)}자, 명세 발췌 {len(spec)}자)\n")
    return out


def main(a):
    if not a or a[0] in ("-h", "--help"):
        print(__doc__)
        return 1
    mx = 10000
    if "--max" in a:
        mx = int(a[a.index("--max") + 1])
        a = [x for i, x in enumerate(a) if x != "--max" and (i == 0 or a[i - 1] != "--max")]
    task_path = a[0]
    if not os.path.isfile(task_path):
        sys.stderr.write(f"brief: 과제 파일 없음 {task_path}\n")
        return 2
    sys.stdout.buffer.write(build(task_path, mx).encode("utf-8"))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
