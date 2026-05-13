"""PitCrew 3D 포장 미니게임 설계 .pptx 생성.
실행: uvx --from python-pptx python scripts/make-packing-deck.py
"""
from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pathlib import Path

# ─── 컬러 (기획서와 동일) ────────────────────────────────
BG = RGBColor(0x14, 0x14, 0x18)
FG = RGBColor(0xF0, 0xF0, 0xF0)
DIM = RGBColor(0xA0, 0xA0, 0xA8)
ACCENT = RGBColor(0xE1, 0x06, 0x00)
ACCENT2 = RGBColor(0x33, 0x33, 0x3A)

# 재료 색상
BUBBLE = RGBColor(0x6A, 0xB7, 0xE8)   # 옅은 파랑
FOAM = RGBColor(0xE8, 0xD6, 0x6A)     # 노란
HARDCASE = RGBColor(0x55, 0x55, 0x5C) # 진회색

FONT = "Malgun Gothic"


def set_bg(slide):
    fill = slide.background.fill
    fill.solid()
    fill.fore_color.rgb = BG


def add_text(slide, left, top, width, height, text, *, size=Pt(18), color=FG, bold=False, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.TOP):
    box = slide.shapes.add_textbox(left, top, width, height)
    tf = box.text_frame
    tf.word_wrap = True
    tf.vertical_anchor = anchor
    tf.margin_top = tf.margin_bottom = tf.margin_left = tf.margin_right = 0
    lines = [text] if isinstance(text, str) else text
    for i, line in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.alignment = align
        run = p.add_run()
        run.text = line
        run.font.name = FONT
        run.font.size = size
        run.font.color.rgb = color
        run.font.bold = bold
    return box


def add_accent_bar(slide, top, height=Emu(60000)):
    bar = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Emu(0), top, Inches(0.15), height)
    bar.line.fill.background()
    bar.fill.solid()
    bar.fill.fore_color.rgb = ACCENT
    return bar


def add_title(slide, text, *, subtitle=None):
    add_accent_bar(slide, Inches(0.55), Inches(0.55))
    add_text(slide, Inches(0.45), Inches(0.45), Inches(12), Inches(0.7), text, size=Pt(32), bold=True)
    if subtitle:
        add_text(slide, Inches(0.45), Inches(1.08), Inches(12), Inches(0.4), subtitle, size=Pt(16), color=DIM)


def add_footer(slide, num, total, label):
    add_text(slide, Inches(0.45), Inches(7.0), Inches(8), Inches(0.3), label, size=Pt(14), color=DIM)
    add_text(slide, Inches(12.4), Inches(7.0), Inches(1.0), Inches(0.3), f"{num} / {total}", size=Pt(14), color=DIM, align=PP_ALIGN.RIGHT)


def add_table(slide, left, top, width, height, headers, rows, *, header_size=13, body_size=12):
    cols = len(headers)
    n = len(rows) + 1
    tbl = slide.shapes.add_table(n, cols, left, top, width, height).table
    for j, h in enumerate(headers):
        c = tbl.cell(0, j)
        c.fill.solid(); c.fill.fore_color.rgb = ACCENT
        c.text_frame.text = ""
        p = c.text_frame.paragraphs[0]; p.alignment = PP_ALIGN.LEFT
        r = p.add_run(); r.text = h
        r.font.name = FONT; r.font.size = Pt(header_size); r.font.bold = True
        r.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)
    for i, row in enumerate(rows, start=1):
        for j, val in enumerate(row):
            c = tbl.cell(i, j)
            c.fill.solid()
            c.fill.fore_color.rgb = ACCENT2 if i % 2 == 0 else BG
            c.text_frame.text = ""
            p = c.text_frame.paragraphs[0]; p.alignment = PP_ALIGN.LEFT
            r = p.add_run(); r.text = str(val)
            r.font.name = FONT; r.font.size = Pt(body_size); r.font.color.rgb = FG
    return tbl


def add_box(slide, left, top, width, height, *, fill_color=ACCENT2, line_color=DIM, line_width=Pt(0.5)):
    box = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
    box.line.color.rgb = line_color
    box.line.width = line_width
    box.fill.solid(); box.fill.fore_color.rgb = fill_color
    return box


# ─── 슬라이드 빌더 ─────────────────────────────────────────
prs = Presentation()
prs.slide_width = Inches(13.333)
prs.slide_height = Inches(7.5)
blank = prs.slide_layouts[6]
TOTAL = 8

# ─── 1. Cover ─────────────────────────────────────────────
s = prs.slides.add_slide(blank); set_bg(s)
top_bar = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Emu(0), Emu(0), prs.slide_width, Inches(0.6))
top_bar.line.fill.background(); top_bar.fill.solid(); top_bar.fill.fore_color.rgb = ACCENT
add_text(s, Inches(0.45), Inches(1.8), Inches(12), Inches(0.6), "PIT CREW · 시스템 설계", size=Pt(20), color=DIM)
add_text(s, Inches(0.45), Inches(2.5), Inches(12), Inches(1.2), "3D 포장 미니게임", size=Pt(72), bold=True)
add_text(s, Inches(0.45), Inches(4.0), Inches(12), Inches(0.6), "마이크로 루프 USP — 한 잡의 깊이를 만드는 시스템", size=Pt(22), color=ACCENT)
add_text(s, Inches(0.45), Inches(4.8), Inches(12), Inches(0.5), "기존 PackingDef 4단계 시스템 갈아치움", size=Pt(15), color=DIM)
add_text(s, Inches(0.45), Inches(5.3), Inches(12), Inches(0.5), "자유 랩핑 (마우스 경로 = 랩 띠) · 재료 3종 · 음식 랩 두르는 메타포", size=Pt(15), color=DIM)
add_text(s, Inches(0.45), Inches(6.8), Inches(12), Inches(0.4), "Decision D-070  ·  2026-05-13  ·  Vertical Slice 데모 (6/24)", size=Pt(12), color=DIM)

# ─── 2. Why — 이 시스템이 왜 필요한가 ────────────────────
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "왜 3D 포장?", subtitle="마이크로 루프 깊이 = 한 잡의 ‘결정 밀도’")
add_text(s, Inches(0.6), Inches(2.0), Inches(12.2), Inches(1.0),
         "“얼마짜리 재료를 어디까지 칠할 것인가” — 단 한 가지 결정이",
         size=Pt(24), color=FG)
add_text(s, Inches(0.6), Inches(2.55), Inches(12.2), Inches(1.0),
         "비용 ↔ 보호도 ↔ 보상 트레이드오프를 손맛으로 만든다.",
         size=Pt(24), bold=True, color=ACCENT)

# 비교 박스 두 개
add_box(s, Inches(0.6), Inches(4.0), Inches(6.0), Inches(2.7), fill_color=ACCENT2)
add_text(s, Inches(0.85), Inches(4.15), Inches(5.5), Inches(0.5), "기존 — PackingDef 4단계", size=Pt(16), bold=True, color=DIM)
add_text(s, Inches(0.85), Inches(4.6), Inches(5.5), Inches(2.0), [
    "None / Bubble / Foam / Hardcase",
    "버튼 4개 중 1개 클릭 → 끝",
    "multiplier 곱셈 (1.0~2.0)",
    "결정 1초. 손맛 0.",
], size=Pt(15), color=FG)

add_box(s, Inches(6.95), Inches(4.0), Inches(6.0), Inches(2.7), fill_color=ACCENT, line_color=ACCENT)
add_text(s, Inches(7.2), Inches(4.15), Inches(5.5), Inches(0.5), "신규 — 3D 자유 페인트", size=Pt(16), bold=True, color=RGBColor(0xFF, 0xFF, 0xFF))
add_text(s, Inches(7.2), Inches(4.6), Inches(5.5), Inches(2.0), [
    "음식 랩 두르듯 — 마우스 경로 = 랩 띠",
    "WASD 회전 + 드래그로 띠 펼치기",
    "충격 방향 면의 wrap 상태로 손상 계산",
    "결정 30초~1분. 차분히 고민.",
], size=Pt(15), color=RGBColor(0xFF, 0xFF, 0xFF))
add_footer(s, 2, TOTAL, "Why")

# ─── 3. Player Flow ───────────────────────────────────────
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Player Flow", subtitle="포장은 잡 선택과 운전 사이에 들어감")
stages = ["잡 보드", "3D 포장", "운전·배송", "정산"]
times = ["~30초", "30초~2분", "5~10분", "30초"]
notes = ["1택배=1목적지\n물건 1~3개 선택", "← 본 문서\n비용 ↔ 보호도", "충격 누적", "손상↔보상\n차감"]
n = len(stages)
total_w = Inches(12.4)
gap = Inches(0.25)
w = Emu(int((total_w.emu - gap.emu * (n - 1)) / n))
left0 = Inches(0.45)
top = Inches(2.4)
for i, (st, tm, nt) in enumerate(zip(stages, times, notes)):
    l = Emu(left0.emu + i * (w + gap.emu))
    highlight = (i == 1)
    box = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, l, top, w, Inches(2.0))
    box.line.color.rgb = ACCENT if highlight else DIM
    box.line.width = Pt(2.5) if highlight else Pt(0.5)
    box.fill.solid(); box.fill.fore_color.rgb = ACCENT2 if highlight else BG
    tf = box.text_frame; tf.word_wrap = True; tf.vertical_anchor = MSO_ANCHOR.MIDDLE
    tf.margin_top = tf.margin_bottom = Pt(8); tf.margin_left = tf.margin_right = Pt(8)
    p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
    r = p.add_run(); r.text = st
    r.font.name = FONT; r.font.size = Pt(18); r.font.bold = True
    r.font.color.rgb = ACCENT if highlight else FG
    p2 = tf.add_paragraph(); p2.alignment = PP_ALIGN.CENTER
    r2 = p2.add_run(); r2.text = tm
    r2.font.name = FONT; r2.font.size = Pt(11); r2.font.color.rgb = DIM
    p3 = tf.add_paragraph(); p3.alignment = PP_ALIGN.CENTER; p3.space_before = Pt(6)
    r3 = p3.add_run(); r3.text = nt
    r3.font.name = FONT; r3.font.size = Pt(11); r3.font.color.rgb = FG

# arrows between
for i in range(n - 1):
    l = Emu(left0.emu + i * (w + gap.emu) + w)
    arrow = s.shapes.add_shape(MSO_SHAPE.RIGHT_ARROW, l, Emu(top.emu + Inches(0.8).emu), gap, Inches(0.4))
    arrow.line.fill.background(); arrow.fill.solid(); arrow.fill.fore_color.rgb = DIM

add_text(s, Inches(0.6), Inches(5.2), Inches(12.2), Inches(0.5), "→ 포장 결과는 운전 중 충격 시 손상 계산의 입력. 잘 포장하면 보상 ↑.", size=Pt(16), color=FG)
add_text(s, Inches(0.6), Inches(5.7), Inches(12.2), Inches(0.5), "→ 잡당 물건 1~3개 결합 가능. 각 물건은 미니게임 안에서 탭 전환으로 따로 포장.", size=Pt(16), color=FG)
add_footer(s, 3, TOTAL, "Flow")

# ─── 4. 화면 구성 ──────────────────────────────────────────
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "화면 구성", subtitle="작업대 도제 — 가운데 화물, 좌측 재료 선반, 우측 비용·커버리지")

# 상단 바: 물건 탭 + 출발
top_strip = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.6), Inches(1.7), Inches(12.2), Inches(0.55))
top_strip.line.color.rgb = DIM; top_strip.line.width = Pt(0.5)
top_strip.fill.solid(); top_strip.fill.fore_color.rgb = ACCENT2
add_text(s, Inches(0.8), Inches(1.8), Inches(4.0), Inches(0.4), "◀  물건  1 / 3  ▶", size=Pt(14), color=FG, anchor=MSO_ANCHOR.MIDDLE)
go_btn = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(10.8), Inches(1.78), Inches(1.9), Inches(0.4))
go_btn.line.fill.background(); go_btn.fill.solid(); go_btn.fill.fore_color.rgb = ACCENT
tf = go_btn.text_frame; tf.vertical_anchor = MSO_ANCHOR.MIDDLE
p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
r = p.add_run(); r.text = "출발하기"
r.font.name = FONT; r.font.size = Pt(14); r.font.bold = True; r.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)

# 좌측 재료 선반
shelf_top = Inches(2.45)
shelf_left = Inches(0.6)
shelf_w = Inches(2.5)
materials = [
    ("부블랩", "₩50 / 회", BUBBLE, "1"),
    ("폼", "₩120 / 회", FOAM, "2"),
    ("하드케이스", "₩300 / 회", HARDCASE, "3"),
]
for i, (name, cost, col, key) in enumerate(materials):
    y = Emu(shelf_top.emu + i * Inches(1.05).emu)
    card = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, shelf_left, y, shelf_w, Inches(0.95))
    card.line.color.rgb = DIM; card.line.width = Pt(0.5)
    card.fill.solid(); card.fill.fore_color.rgb = ACCENT2
    swatch = s.shapes.add_shape(MSO_SHAPE.OVAL, Emu(shelf_left.emu + Inches(0.15).emu), Emu(y + Inches(0.22).emu), Inches(0.55), Inches(0.55))
    swatch.line.fill.background(); swatch.fill.solid(); swatch.fill.fore_color.rgb = col
    add_text(s, Emu(shelf_left.emu + Inches(0.85).emu), Emu(y + Inches(0.12).emu), Inches(1.6), Inches(0.4), name, size=Pt(14), bold=True, color=FG)
    add_text(s, Emu(shelf_left.emu + Inches(0.85).emu), Emu(y + Inches(0.48).emu), Inches(1.6), Inches(0.35), cost, size=Pt(11), color=DIM)
    key_badge = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Emu(shelf_left.emu + shelf_w.emu - Inches(0.4).emu), Emu(y + Inches(0.12).emu), Inches(0.28), Inches(0.28))
    key_badge.line.color.rgb = DIM; key_badge.line.width = Pt(0.5)
    key_badge.fill.solid(); key_badge.fill.fore_color.rgb = BG
    tf = key_badge.text_frame; tf.vertical_anchor = MSO_ANCHOR.MIDDLE
    tf.margin_top = tf.margin_bottom = tf.margin_left = tf.margin_right = 0
    p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
    r = p.add_run(); r.text = key
    r.font.name = FONT; r.font.size = Pt(11); r.font.bold = True; r.font.color.rgb = FG

# 랩 폭
add_text(s, shelf_left, Emu(shelf_top.emu + 3 * Inches(1.05).emu + Inches(0.1).emu), shelf_w, Inches(0.3), "랩 폭   ▬  ▬▬  ▬▬▬", size=Pt(13), color=DIM)
add_text(s, shelf_left, Emu(shelf_top.emu + 3 * Inches(1.05).emu + Inches(0.45).emu), shelf_w, Inches(0.3), "마우스 휠로 조절", size=Pt(10), color=DIM)

# 가운데 작업대 (3D 화물)
center = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(3.3), Inches(2.45), Inches(6.4), Inches(4.3))
center.line.color.rgb = DIM; center.line.width = Pt(0.5)
center.fill.solid(); center.fill.fore_color.rgb = BG

# 작업대 표면 (얇은 띠)
deck = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(3.5), Inches(5.6), Inches(6.0), Inches(0.15))
deck.line.fill.background(); deck.fill.solid(); deck.fill.fore_color.rgb = ACCENT2

# 화물 (Cube primitive 표현 — 3D 박스 효과: 앞 사각형 + 위 사선)
cube_front = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(5.2), Inches(3.6), Inches(2.6), Inches(2.0))
cube_front.line.color.rgb = FG; cube_front.line.width = Pt(1.5)
cube_front.fill.solid(); cube_front.fill.fore_color.rgb = ACCENT2
# 부블랩 랩 띠 (가로로 두른 띠 — 가장자리 깔끔)
wrap1 = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(5.2), Inches(4.0), Inches(2.6), Inches(0.45))
wrap1.line.fill.background(); wrap1.fill.solid(); wrap1.fill.fore_color.rgb = BUBBLE
# 하드케이스 랩 띠 (대각으로 한 번 더 두름 — 덧칠)
wrap2 = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(5.2), Inches(4.85), Inches(2.6), Inches(0.35))
wrap2.line.fill.background(); wrap2.fill.solid(); wrap2.fill.fore_color.rgb = HARDCASE

# 윗면 (사선)
top_face = s.shapes.add_shape(MSO_SHAPE.PARALLELOGRAM, Inches(5.4), Inches(3.2), Inches(2.6), Inches(0.5))
top_face.line.color.rgb = FG; top_face.line.width = Pt(1.5)
top_face.fill.solid(); top_face.fill.fore_color.rgb = ACCENT2

# 측면
side_face = s.shapes.add_shape(MSO_SHAPE.RIGHT_TRIANGLE, Inches(7.8), Inches(3.4), Inches(0.4), Inches(2.2))
side_face.line.color.rgb = FG; side_face.line.width = Pt(1.5)
side_face.fill.solid(); side_face.fill.fore_color.rgb = ACCENT2

add_text(s, Inches(3.4), Inches(6.85), Inches(6.2), Inches(0.3), "WASD = 회전     좌클릭 누름 + 드래그 = 랩 띠 펼치기 (떼면 한 띠 끝)     R = 리셋", size=Pt(11), color=DIM, align=PP_ALIGN.CENTER)

# 우측 비용/커버리지 패널
panel_left = Inches(9.9)
panel_w = Inches(2.95)
panel = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, panel_left, Inches(2.45), panel_w, Inches(4.3))
panel.line.color.rgb = DIM; panel.line.width = Pt(0.5)
panel.fill.solid(); panel.fill.fore_color.rgb = ACCENT2

add_text(s, Emu(panel_left.emu + Inches(0.2).emu), Inches(2.6), Inches(2.6), Inches(0.4), "비용", size=Pt(13), bold=True, color=ACCENT)
cost_rows = [("부블랩", "₩200", BUBBLE), ("폼", "₩0", FOAM), ("하드케이스", "₩600", HARDCASE)]
for i, (n_, v, col) in enumerate(cost_rows):
    y = Emu(Inches(3.0).emu + i * Inches(0.35).emu)
    sw = s.shapes.add_shape(MSO_SHAPE.OVAL, Emu(panel_left.emu + Inches(0.2).emu), y, Inches(0.18), Inches(0.18))
    sw.line.fill.background(); sw.fill.solid(); sw.fill.fore_color.rgb = col
    add_text(s, Emu(panel_left.emu + Inches(0.45).emu), Emu(y - Inches(0.04).emu), Inches(1.4), Inches(0.3), n_, size=Pt(12), color=FG)
    add_text(s, Emu(panel_left.emu + Inches(1.9).emu), Emu(y - Inches(0.04).emu), Inches(0.9), Inches(0.3), v, size=Pt(12), color=FG, align=PP_ALIGN.RIGHT)

# divider
div = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Emu(panel_left.emu + Inches(0.2).emu), Inches(4.2), Inches(2.55), Emu(8000))
div.line.fill.background(); div.fill.solid(); div.fill.fore_color.rgb = DIM

add_text(s, Emu(panel_left.emu + Inches(0.2).emu), Inches(4.3), Inches(1.5), Inches(0.3), "합계", size=Pt(13), bold=True, color=FG)
add_text(s, Emu(panel_left.emu + Inches(1.9).emu), Inches(4.3), Inches(0.9), Inches(0.3), "₩800", size=Pt(14), bold=True, color=ACCENT, align=PP_ALIGN.RIGHT)

add_text(s, Emu(panel_left.emu + Inches(0.2).emu), Inches(4.9), Inches(2.6), Inches(0.4), "평균 커버", size=Pt(13), bold=True, color=ACCENT)
# coverage bar
cov_bg = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Emu(panel_left.emu + Inches(0.2).emu), Inches(5.3), Inches(2.55), Inches(0.3))
cov_bg.line.fill.background(); cov_bg.fill.solid(); cov_bg.fill.fore_color.rgb = BG
cov_fg = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Emu(panel_left.emu + Inches(0.2).emu), Inches(5.3), Inches(2.55 * 0.67), Inches(0.3))
cov_fg.line.fill.background(); cov_fg.fill.solid(); cov_fg.fill.fore_color.rgb = ACCENT
add_text(s, Emu(panel_left.emu + Inches(0.2).emu), Inches(5.7), Inches(2.6), Inches(0.4), "67%", size=Pt(20), bold=True, color=FG)
add_text(s, Emu(panel_left.emu + Inches(0.2).emu), Inches(6.1), Inches(2.6), Inches(0.4), "( 충격 위치별로 실제 흡수율은 변동 )", size=Pt(9), color=DIM)

add_footer(s, 4, TOTAL, "Screen")

# ─── 5. 입력 모델 ──────────────────────────────────────────
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "입력 모델", subtitle="음식 랩 두르는 메타포 · 시간 압박 없음 — calm crafting")

inputs = [
    ("WASD", "3D 화물 회전 (yaw / pitch)"),
    ("좌클릭 누름", "랩 띠 시작점 고정"),
    ("누른 채 드래그", "경로 따라 일정 폭의 띠가 표면에 펼쳐짐"),
    ("떼기", "한 띠 종료 → 다음 누름은 새 띠"),
    ("1 / 2 / 3", "재료 전환 (부블랩 / 폼 / 하드케이스)"),
    ("마우스 휠", "랩 폭 (띠의 굵기)"),
    ("R", "회전 리셋 (편의)"),
]
top = Inches(1.8)
row_h = Inches(0.68)
for i, (key, desc) in enumerate(inputs):
    y = Emu(top.emu + i * row_h.emu)
    key_box = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.6), y, Inches(3.2), Inches(0.55))
    key_box.line.color.rgb = ACCENT; key_box.line.width = Pt(1.5)
    key_box.fill.solid(); key_box.fill.fore_color.rgb = ACCENT2
    tf = key_box.text_frame; tf.vertical_anchor = MSO_ANCHOR.MIDDLE
    p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
    r = p.add_run(); r.text = key
    r.font.name = FONT; r.font.size = Pt(15); r.font.bold = True; r.font.color.rgb = FG
    add_text(s, Inches(4.05), y, Inches(8.8), Inches(0.55), desc, size=Pt(15), color=FG, anchor=MSO_ANCHOR.MIDDLE)

add_footer(s, 5, TOTAL, "Input")

# ─── 6. 재료 3종 ───────────────────────────────────────────
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "재료 3종", subtitle="가격 ∝ 보호도 — 단순·직관 트레이드오프")

mats = [
    ("부블랩", "싸 · 약", "₩50 / 회", "흡수율 25%", BUBBLE, "옅은 파랑\n반투명"),
    ("폼", "중 · 중", "₩120 / 회", "흡수율 55%", FOAM, "노란\n불투명"),
    ("하드케이스", "비싸 · 강", "₩300 / 회", "흡수율 85%", HARDCASE, "진회색\n불투명"),
]
top = Inches(2.0)
card_w = Inches(3.9)
gap = Inches(0.25)
total_w_emu = 3 * card_w.emu + 2 * gap.emu
start_left = Emu(int((prs.slide_width.emu - total_w_emu) / 2))
for i, (n_, tier, cost, eff, col, look) in enumerate(mats):
    l = Emu(start_left + i * (card_w.emu + gap.emu))
    card = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, l, top, card_w, Inches(4.7))
    card.line.color.rgb = DIM; card.line.width = Pt(0.5)
    card.fill.solid(); card.fill.fore_color.rgb = ACCENT2
    swatch = s.shapes.add_shape(MSO_SHAPE.OVAL, Emu(l + Inches(1.3).emu), Emu(top.emu + Inches(0.3).emu), Inches(1.3), Inches(1.3))
    swatch.line.fill.background(); swatch.fill.solid(); swatch.fill.fore_color.rgb = col
    add_text(s, l, Emu(top.emu + Inches(1.8).emu), card_w, Inches(0.5), n_, size=Pt(24), bold=True, color=FG, align=PP_ALIGN.CENTER)
    add_text(s, l, Emu(top.emu + Inches(2.35).emu), card_w, Inches(0.4), tier, size=Pt(14), color=DIM, align=PP_ALIGN.CENTER)
    add_text(s, l, Emu(top.emu + Inches(3.0).emu), card_w, Inches(0.4), cost, size=Pt(18), bold=True, color=ACCENT, align=PP_ALIGN.CENTER)
    add_text(s, l, Emu(top.emu + Inches(3.5).emu), card_w, Inches(0.4), eff, size=Pt(15), color=FG, align=PP_ALIGN.CENTER)
    add_text(s, l, Emu(top.emu + Inches(4.05).emu), card_w, Inches(0.5), look, size=Pt(11), color=DIM, align=PP_ALIGN.CENTER)

add_text(s, Inches(0.6), Inches(6.9), Inches(12.2), Inches(0.4), "덧칠 가능 · 최상위 재료만 보호도 적용 · 아래 레이어 비용은 이미 지불 (낭비 가능성)", size=Pt(13), color=DIM, align=PP_ALIGN.CENTER)
add_footer(s, 6, TOTAL, "Materials")

# ─── 7. 게임 룰 — 충격 → 손상 ─────────────────────────────
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "충격 → 손상 매핑", subtitle="기존 CargoBox 손상 인과(D-068)와 통합")

steps = [
    ("1", "차 G", "급가속/급제동/코너"),
    ("2", "박스 슬라이드", "트렁크 안에서 이동"),
    ("3", "벽 충돌", "충격 방향 벡터 발생"),
    ("4", "Raycast → UV", "화물 표면 충격 지점"),
    ("5", "Wrap 상태 조회", "그 지점 주변 평균 재료"),
    ("6", "흡수율 적용", "1 − 흡수율 = 손상량"),
]
top = Inches(2.0)
n = len(steps)
total_w = Inches(12.4)
gap = Inches(0.12)
w = Emu(int((total_w.emu - gap.emu * (n - 1)) / n))
left0 = Inches(0.45)
for i, (num, title, desc) in enumerate(steps):
    l = Emu(left0.emu + i * (w + gap.emu))
    box = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, l, top, w, Inches(1.6))
    box.line.color.rgb = ACCENT; box.line.width = Pt(1.0)
    box.fill.solid(); box.fill.fore_color.rgb = ACCENT2
    tf = box.text_frame; tf.word_wrap = True; tf.vertical_anchor = MSO_ANCHOR.MIDDLE
    tf.margin_top = tf.margin_bottom = Pt(6); tf.margin_left = tf.margin_right = Pt(4)
    p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
    r = p.add_run(); r.text = num
    r.font.name = FONT; r.font.size = Pt(20); r.font.bold = True; r.font.color.rgb = ACCENT
    p2 = tf.add_paragraph(); p2.alignment = PP_ALIGN.CENTER
    r2 = p2.add_run(); r2.text = title
    r2.font.name = FONT; r2.font.size = Pt(11); r2.font.bold = True; r2.font.color.rgb = FG
    p3 = tf.add_paragraph(); p3.alignment = PP_ALIGN.CENTER
    r3 = p3.add_run(); r3.text = desc
    r3.font.name = FONT; r3.font.size = Pt(9); r3.font.color.rgb = DIM

# 규칙 박스
add_text(s, Inches(0.6), Inches(4.3), Inches(12.2), Inches(0.5), "핵심 룰", size=Pt(18), bold=True, color=ACCENT)
rules = [
    "완료 조건 없음 — 0%도 100%도 자유. 포장 정도 = 손상 차단율.",
    "덧칠 시 최상위 재료만 흡수율 적용 (아래 비용은 손실).",
    "화물에 부착된 RenderTexture (256×256, RGB 채널 = 재료별) 가 wrap 상태 저장.",
    "충격 지점 raycast 실패 시 fallback: 충격 방향과 가장 일치하는 표면 normal 영역.",
]
add_text(s, Inches(0.6), Inches(4.85), Inches(12.2), Inches(2.5), rules, size=Pt(14), color=FG)

add_footer(s, 7, TOTAL, "Damage")

# ─── 8. 작업량 + 다음 단계 ────────────────────────────────
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "작업량 추정 & 다음", subtitle="단독 추정 약 9일 (1.8주)")

add_table(s, Inches(0.6), Inches(2.0), Inches(7.5), Inches(4.5),
          headers=["작업 단위", "추정"],
          rows=[
              ["WrapMaterialDef SO + 3종 자산", "0.3일"],
              ["WrapState + RenderTexture 페인트 셰이더", "2일"],
              ["PackingScreen UGUI (작업대 + 사이드)", "2일"],
              ["비용 / 평균 커버 실시간 표시", "0.5일"],
              ["잡 보드 → 포장 화면 전환 로직", "0.5일"],
              ["물건 1~3개 탭 전환", "0.5일"],
              ["CargoShakeSimulator wrap 흡수율 통합", "1일"],
              ["기존 PackingDef 제거 + 참조 정리", "0.5일"],
              ["폴리시 (사운드, hover, 시각)", "1.5일"],
              ["합", "약 9일"],
          ], header_size=14, body_size=12)

# 위험 + 다음 단계
add_text(s, Inches(8.4), Inches(2.0), Inches(4.5), Inches(0.5), "위험", size=Pt(18), bold=True, color=ACCENT)
risks = [
    "Texture painting 셰이더 슬립",
    "→ day 3 fallback 결정 포인트",
    "→ mesh face 단위 (6면 / 12면)",
    "",
    "충격 → UV 매핑 정확도",
    "→ normal 일치 fallback",
]
add_text(s, Inches(8.4), Inches(2.5), Inches(4.5), Inches(2.5), risks, size=Pt(12), color=FG)

add_text(s, Inches(8.4), Inches(4.7), Inches(4.5), Inches(0.5), "다음 단계", size=Pt(18), bold=True, color=ACCENT)
add_text(s, Inches(8.4), Inches(5.2), Inches(4.5), Inches(2.0), [
    "1. 본 덱 검토 → 도현 승인",
    "2. writing-plans 스킬로 구현 계획",
    "3. unity-cli로 단계별 검증",
    "4. 차고/부품 시스템 별도 결정",
], size=Pt(13), color=FG)

add_footer(s, 8, TOTAL, "Plan")

out = Path("PitCrew_3D포장_설계.pptx")
prs.save(out)
print(f"saved: {out.resolve()}")
