"""D-238 UI 목업 2안 — 360 바탕 컷(raw/s*.png) 위에 화면 픽셀 = 게임 픽셀로 UI 를 찍는다.
글꼴 Galmuri(OFL, 안티앨리어싱 끔), 색은 노을 9판 팔레트 + 종이 크림. 결과: art/_review/2026-11-01-ui-mock/
python -X utf8 docs/qa/tmp/uimock/compose.py
"""
import json, pathlib
from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[4]
OUT = ROOT / "art/_review/2026-11-01-ui-mock"
RAW = OUT / "raw"
FONTS = OUT / "fonts"

# 팔레트 — 9판 상위색(palette_6_8_9.json) 에서 고르고 종이 크림 2단만 더함
INK = (58, 48, 70)        # 남보라 진함(글자·테두리)
INK2 = (93, 64, 71)       # 9판 (93,64,71)
NAVY = (53, 63, 90)       # 9판 물 남색
MAUVE = (128, 95, 121)    # 9판
ORANGE = (227, 152, 107)  # 9판 노을 주황
CORAL = (213, 129, 115)   # 9판
PAPER = (240, 226, 198)   # 종이 크림
PAPER2 = (222, 202, 170)  # 종이 결(한 단 어둡게)
GRAY = (150, 146, 150)

def font(name, px):
    return ImageFont.truetype(str(FONTS / f"{name}.ttf"), px)

F7, F9, F11 = font("Galmuri7", 8), font("Galmuri9", 10), font("Galmuri11", 12)


def D(im):
    d = ImageDraw.Draw(im)
    d.fontmode = "1"   # 안티앨리어싱 끔 — 글자 픽셀이 게임 픽셀과 같은 격자
    return d


def text(d, xy, s, f, fill, shadow=None):
    if shadow:
        d.text((xy[0] + 1, xy[1] + 1), s, font=f, fill=shadow)
    d.text(xy, s, font=f, fill=fill)


def tw(f, s):
    b = f.getbbox(s)
    return b[2] - b[0]


def panel(d, x0, y0, x1, y1, fill, edge, cut=1):
    """도트 상자: 1px 테두리, 모서리 cut 픽셀 계단으로 깎음."""
    d.rectangle((x0 + cut, y0, x1 - cut, y1), fill=fill)
    d.rectangle((x0, y0 + cut, x1, y1 - cut), fill=fill)
    d.line((x0 + cut, y0, x1 - cut, y0), fill=edge)
    d.line((x0 + cut, y1, x1 - cut, y1), fill=edge)
    d.line((x0, y0 + cut, x0, y1 - cut), fill=edge)
    d.line((x1, y0 + cut, x1, y1 - cut), fill=edge)
    if cut == 2:   # 계단 모서리 한 칸
        for p in ((x0 + 1, y0 + 1), (x1 - 1, y0 + 1), (x0 + 1, y1 - 1), (x1 - 1, y1 - 1)):
            d.point(p, fill=edge)


def paper_grain(d, x0, y0, x1, y1):
    """종이 결: 규칙적인 짧은 가로 결 한 단 어둡게(무작위 점무늬 아님)."""
    for y in range(y0, y1 + 1, 4):
        off = (y // 4) % 3 * 5
        for x in range(x0 + off, x1 + 1, 15):
            d.line((x, y, min(x + 3, x1), y), fill=PAPER2)


def key_chip(d, x, y, k="E"):
    """11×11 도트 키 칩."""
    d.rectangle((x + 1, y + 10, x + 10, y + 10), fill=INK2)       # 아래 그림자 한 줄 = 눌리는 키
    panel(d, x, y, x + 10, y + 9, PAPER, INK)
    text(d, (x + 3, y + 1), k, F7, INK)


def gaze_dot(d):
    d.rectangle((319, 179, 320, 180), fill=PAPER)


# ── 장면별 그리기 ─────────────────────────────────────

def s1_A(im):
    d = D(im)
    # 짐칸 상자 뒷면에 끼운 전표(세상 속, 2D 덧칠 — 3D 모델 아님): 9×8 크림 + 손글씨 줄 2 + 집게
    x, y = 324, 251
    d.rectangle((x, y, x + 8, y + 7), fill=PAPER)
    d.line((x + 2, y + 3, x + 6, y + 3), fill=INK2); d.line((x + 2, y + 5, x + 5, y + 5), fill=INK2)
    d.line((x + 3, y - 1, x + 5, y - 1), fill=INK)
    # 계기 바늘(엔진 위 작은 둥근 계기 7px)
    cx, cy = 356, 244
    d.ellipse((cx - 3, cy - 3, cx + 3, cy + 3), fill=PAPER, outline=INK)
    d.line((cx, cy, cx - 2, cy - 2), fill=CORAL)
    # 화면에 남는 것: 위 가운데 작은 도트 방향 표시(목적지 = 왼쪽 앞 약 36°) 9×9
    ax, ay = 316, 8
    arrow = ["....XXXXX",
             "....XOOOX",
             ".....XOOX",
             "....XOXOX",
             "...XOX.XX",
             "..XOX...X",
             ".XOX.....",
             "XOX......",
             "XX......."]
    # 왼쪽 위를 가리키도록 좌우 뒤집음
    for j, row in enumerate(arrow):
        for i, c in enumerate(row[::-1]):
            if c == "X": d.point((ax + i, ay + j), fill=INK)
            elif c == "O": d.point((ax + i, ay + j), fill=PAPER)


def s1_B(im):
    d = D(im)
    # 왼쪽 위: 전표 아이콘 + 목적지(기존 대사·튜토리얼에 있는 이름 그대로)
    panel(d, 6, 6, 92, 21, PAPER, INK)
    d.rectangle((10, 9, 17, 18), fill=(255, 246, 226)); d.rectangle((10, 9, 17, 18), outline=INK2)
    d.line((12, 12, 15, 12), fill=INK2); d.line((12, 14, 15, 14), fill=INK2); d.line((12, 16, 14, 16), fill=INK2)
    text(d, (21, 8), "언덕 위 찻집", F9, INK)
    # 왼쪽 아래: 화물 위험 아이콘(달걀 + !) — 위험할 때만 몇 초
    panel(d, 5, 333, 27, 354, PAPER, INK)
    d.ellipse((9, 337, 19, 350), fill=(255, 246, 226), outline=INK)
    d.line((11, 343, 13, 341), fill=INK2); d.line((13, 341, 15, 344), fill=INK2); d.line((15, 344, 17, 342), fill=INK2)   # 금
    d.line((22, 337, 22, 346), fill=CORAL); d.line((23, 337, 23, 346), fill=CORAL)
    d.rectangle((22, 349, 23, 350), fill=CORAL)
    # 위 가운데: 방향 점(가는 줄 + 점) — 목적지 왼쪽 앞 약 36°
    d.line((290, 10, 350, 10), fill=INK); d.line((290, 11, 350, 11), fill=PAPER2)
    d.point((320, 8), fill=INK); d.point((320, 13), fill=INK)
    px = 320 - int(36 / 90 * 30)
    d.rectangle((px - 1, 9, px + 1, 12), fill=ORANGE, outline=None)
    d.point((px - 1, 9), fill=INK); d.point((px + 1, 9), fill=INK); d.point((px - 1, 12), fill=INK); d.point((px + 1, 12), fill=INK)


def s2_A(im):
    d = D(im); gaze_dot(d)
    # 물건(카트 핸들) 곁에 붙은 키 칩 + 한 단어
    x, y = 334, 150
    key_chip(d, x, y)
    text(d, (x + 14, y + 1), "타기", F9, PAPER, shadow=INK)


def s2_B(im):
    d = D(im); gaze_dot(d)
    w = 11 + 4 + tw(F9, "타기")
    x, y = 320 - (w + 8) // 2, 330
    panel(d, x, y - 3, x + w + 8, y + 13, PAPER, INK)
    key_chip(d, x + 3, y)
    text(d, (x + 18, y + 1), "타기", F9, INK)


LINE = "뒤에 짐상자 달아 놨어. 깨지는 건 네 책임."   # OnFootDirector.BossLines[1] 그대로
CUT = 16   # 한 글자씩 — 정지 컷은 16번째 글자("깨지" 까지)에서


def s3_A(im):
    d = D(im)
    full = LINE.split(" 깨지")[0] + "\n깨지" + LINE.split(" 깨지")[1]
    lines = full.split("\n")
    w = max(tw(F9, l) for l in lines) + 10
    h = 11 * len(lines) + 8
    hx, hy = 321, 154            # 캡슐 머리 위(cap.cs 측정 + 화면 확인)
    x0, y1 = hx - w // 2 + 14, hy - 4
    y0 = y1 - h
    panel(d, x0, y0, x0 + w, y1, PAPER, INK, cut=2)
    # 꼬리(말하는 사람 쪽 아래로) 계단 3단
    tx = hx + 2
    for i in range(5):
        d.line((tx - 2 + i // 2, y1 + i, tx + 3 - i, y1 + i), fill=PAPER)
        d.point((tx - 3 + i // 2, y1 + i), fill=INK); d.point((tx + 4 - i, y1 + i), fill=INK)
    d.line((tx - 2, y1, tx + 3, y1), fill=PAPER)
    # 이름표: 왼쪽 위 남색 탭
    nw = tw(F7, "사장") + 6
    panel(d, x0 + 4, y0 - 7, x0 + 4 + nw, y0 + 2, NAVY, INK)
    text(d, (x0 + 7, y0 - 6), "사장", F7, PAPER)
    # 한 글자씩: CUT 글자까지만
    shown = LINE[:CUT]
    ln0 = shown if len(shown) <= len(lines[0]) else lines[0]
    text(d, (x0 + 5, y0 + 5), ln0, F9, INK)
    if len(shown) > len(lines[0]) + 1:
        text(d, (x0 + 5, y0 + 16), shown[len(lines[0]) + 1:], F9, INK)


def s3_B(im):
    d = D(im)
    x0, y0, x1, y1 = 120, 296, 520, 350
    d.rectangle((x0 + 2, y1 + 1, x1 + 1, y1 + 2), fill=INK2)   # 바닥 그림자 한 줄
    panel(d, x0, y0, x1, y1, PAPER, INK, cut=2)
    paper_grain(d, x0 + 3, y0 + 3, x1 - 3, y1 - 3)
    # 초상 칸 40×40(지금은 캡슐 대역 실루엣 — 초상 스프라이트 슬롯)
    px0, py0 = x0 + 6, y0 + 7
    d.rectangle((px0, py0, px0 + 40, py0 + 40), fill=MAUVE, outline=INK)
    d.rounded_rectangle((px0 + 12, py0 + 8, px0 + 28, py0 + 40), radius=8, fill=GRAY)
    d.rectangle((px0 + 15, py0 + 14, px0 + 25, py0 + 18), fill=(120, 116, 120))
    # 이름 탭(상자 위 왼쪽)
    nw = tw(F9, "사장") + 8
    panel(d, x0 + 6, y0 - 9, x0 + 6 + nw, y0 + 3, NAVY, INK)
    text(d, (x0 + 10, y0 - 8), "사장", F9, PAPER)
    shown = LINE[:CUT]
    text(d, (px0 + 48, py0 + 4), shown, F11, INK)


def tongs(d, x, y):
    """집게 커서 약 17×17 — 위 경첩에서 벌어진 두 팔(주황, 남보라 테), 아래 왼쪽 두 끝 사이가 집는 점."""
    hx, hy = x + 14, y - 15
    tips = ((x - 1, y - 4), (x + 4, y + 1))
    for t in tips: d.line((hx, hy, t[0], t[1]), fill=INK, width=4)
    for t in tips: d.line((hx, hy, t[0], t[1]), fill=ORANGE, width=2)
    d.ellipse((hx - 2, hy - 2, hx + 2, hy + 2), fill=PAPER, outline=INK)
    d.point((x + 1, y - 2), fill=INK); d.point((x + 2, y - 1), fill=INK)   # 끝 안쪽 갈고리


def s4_A(im):
    d = D(im)
    tongs(d, 452, 140)   # 뽁뽁이 덩어리 위


STEPS = ["바닥 깔기", "넣기", "채우기", "닫기", "라벨"]   # PACKING-UI.md §3 의 단계 이름(제안)
NOW = 2


def s4_B(im):
    s4_A(im)
    d = D(im)
    sep = " · "
    total = sum(tw(F9, s) for s in STEPS) + tw(F9, sep) * (len(STEPS) - 1)
    x = 320 - total // 2
    y = 6
    for i, s in enumerate(STEPS):
        col = PAPER if i == NOW else (190, 170, 160)
        text(d, (x, y), s, F9, col, shadow=INK)
        if i == NOW:
            d.line((x, y + 12, x + tw(F9, s) - 1, y + 12), fill=ORANGE)
        x += tw(F9, s)
        if i < len(STEPS) - 1:
            text(d, (x, y), sep, F9, (160, 140, 140), shadow=INK); x += tw(F9, sep)


SCENES = [("s1_drive", "1 카트 주행", s1_A, s1_B), ("s2_walk", "2 걷기+상호작용", s2_A, s2_B),
          ("s3_talk", "3 대화", s3_A, s3_B), ("s4_bench", "4 포장 작업대", s4_A, s4_B)]


def ui_area(base, im):
    a, b = base.convert("RGB").tobytes(), im.convert("RGB").tobytes()
    n = sum(1 for i in range(0, len(a), 3) if a[i:i + 3] != b[i:i + 3])
    return round(100 * n / (base.width * base.height), 2)


def main():
    meas = {}
    cells = {}
    for key, title, fa, fb in SCENES:
        base = Image.open(RAW / f"{key}.png").convert("RGB")
        for tag, fn in (("A", fa), ("B", fb)):
            im = base.copy(); fn(im)
            name = f"{key[:2]}{tag}"
            im.save(OUT / f"{name}_1x.png")
            im.resize((im.width * 3, im.height * 3), Image.NEAREST).save(OUT / f"{name}_3x.png")
            meas[name] = {"ui_px_pct": ui_area(base, im), "colors_added": len(set(im.getdata()) - set(base.getdata()))}
            cells[(key, tag)] = im
    # 비교판: 왼쪽 칸 = 지금 UI(1920 캡처를 640×360 으로 줄임), 위 A / 아래 B
    W, H, M, TOP, LEFT = 640, 360, 8, 26, 22
    cols = 1 + len(SCENES)
    sheet = Image.new("RGB", (LEFT + cols * (W + M), TOP + 2 * (H + M) + 14), (34, 30, 40))
    d = D(sheet)
    now = [("f2_05_hud_target.png", "지금 UI — 주행"), ("f2_03_talk_1.png", "지금 UI — 대화")]
    for r, (fn, cap) in enumerate(now):
        im = Image.open(ROOT / "docs/qa/evidence/feature2" / fn).convert("RGB").resize((W, H), Image.BOX)
        sheet.paste(im, (LEFT, TOP + r * (H + M)))
    text(d, (LEFT, 8), "지금 UI (f2_05·f2_03, 1920 원해상도 UI 를 줄임)", F11, PAPER)
    for c, (key, title, _, _) in enumerate(SCENES):
        x = LEFT + (c + 1) * (W + M)
        text(d, (x, 8), title, F11, PAPER)
        for r, tag in enumerate("AB"):
            sheet.paste(cells[(key, tag)], (x, TOP + r * (H + M)))
    for r, tag in enumerate("AB"):
        text(d, (6, TOP + r * (H + M) + H // 2), tag, F11, ORANGE)
    text(d, (LEFT, sheet.height - 13), "A = 세상 속(화면 UI 0~거의 0)   B = 최소 도트 HUD   · 모든 칸 640×360 1배 · 3배는 s?A_3x/s?B_3x", F9, (200, 190, 190))
    sheet.save(OUT / "sheet.png")
    (OUT / "measure.json").write_text(json.dumps(meas, ensure_ascii=False, indent=1), encoding="utf-8")
    print(json.dumps(meas, ensure_ascii=False))


if __name__ == "__main__":
    main()
