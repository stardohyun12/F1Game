"""PitCrew 기획서 .pptx 생성.
실행: uvx --from python-pptx python scripts/make-pitch-deck.py
또는 pip install python-pptx 후 python scripts/make-pitch-deck.py
"""
from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pathlib import Path

# ─── 컬러 ────────────────────────────────────────────────
BG = RGBColor(0x14, 0x14, 0x18)       # 다크 배경
FG = RGBColor(0xF0, 0xF0, 0xF0)       # 본문 텍스트
DIM = RGBColor(0xA0, 0xA0, 0xA8)      # 보조 텍스트
ACCENT = RGBColor(0xE1, 0x06, 0x00)   # F1 빨강
ACCENT2 = RGBColor(0x33, 0x33, 0x3A)  # 줄무늬 표 행

FONT = "Malgun Gothic"
TITLE_PT = Pt(40)
BODY_PT = Pt(20)
SUB_PT = Pt(14)


def set_bg(slide):
    bg = slide.background
    fill = bg.fill
    fill.solid()
    fill.fore_color.rgb = BG


def add_text(slide, left, top, width, height, text, *, size=BODY_PT, color=FG, bold=False, align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.TOP):
    box = slide.shapes.add_textbox(left, top, width, height)
    tf = box.text_frame
    tf.word_wrap = True
    tf.vertical_anchor = anchor
    tf.margin_top = tf.margin_bottom = tf.margin_left = tf.margin_right = 0
    if isinstance(text, str):
        lines = [text]
    else:
        lines = text
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
    """슬라이드 좌측 빨간 액센트 바."""
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
    add_text(slide, Inches(0.45), Inches(7.0), Inches(8), Inches(0.3), label, size=SUB_PT, color=DIM)
    add_text(slide, Inches(12.4), Inches(7.0), Inches(1.0), Inches(0.3), f"{num} / {total}", size=SUB_PT, color=DIM, align=PP_ALIGN.RIGHT)


def add_table(slide, left, top, width, height, headers, rows):
    cols = len(headers)
    n = len(rows) + 1
    tbl_shape = slide.shapes.add_table(n, cols, left, top, width, height)
    tbl = tbl_shape.table
    # 헤더
    for j, h in enumerate(headers):
        cell = tbl.cell(0, j)
        cell.fill.solid(); cell.fill.fore_color.rgb = ACCENT
        for p in cell.text_frame.paragraphs:
            p.alignment = PP_ALIGN.LEFT
        cell.text_frame.text = ""
        p = cell.text_frame.paragraphs[0]
        r = p.add_run(); r.text = h
        r.font.name = FONT; r.font.size = Pt(13); r.font.bold = True
        r.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)
    # body
    for i, row in enumerate(rows, start=1):
        for j, val in enumerate(row):
            cell = tbl.cell(i, j)
            cell.fill.solid()
            cell.fill.fore_color.rgb = ACCENT2 if i % 2 == 0 else BG
            cell.text_frame.text = ""
            p = cell.text_frame.paragraphs[0]
            p.alignment = PP_ALIGN.LEFT
            r = p.add_run(); r.text = str(val)
            r.font.name = FONT; r.font.size = Pt(12); r.font.color.rgb = FG
    return tbl


def add_bullet_list(slide, left, top, width, height, items, *, size=Pt(18)):
    box = slide.shapes.add_textbox(left, top, width, height)
    tf = box.text_frame
    tf.word_wrap = True
    for i, item in enumerate(items):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.space_after = Pt(8)
        # bullet
        r1 = p.add_run(); r1.text = "■ "
        r1.font.name = FONT; r1.font.size = size; r1.font.color.rgb = ACCENT; r1.font.bold = True
        r2 = p.add_run(); r2.text = item
        r2.font.name = FONT; r2.font.size = size; r2.font.color.rgb = FG


# ─── 슬라이드 빌더 ─────────────────────────────────────────
prs = Presentation()
prs.slide_width = Inches(13.333)
prs.slide_height = Inches(7.5)
blank = prs.slide_layouts[6]
TOTAL = 12

# 1. Cover
s = prs.slides.add_slide(blank); set_bg(s)
bar = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Emu(0), Emu(0), prs.slide_width, Inches(0.6))
bar.line.fill.background(); bar.fill.solid(); bar.fill.fore_color.rgb = ACCENT
add_text(s, Inches(0.45), Inches(2.4), Inches(12), Inches(1.2), "PIT CREW", size=Pt(80), bold=True)
add_text(s, Inches(0.45), Inches(3.7), Inches(12), Inches(0.7), "망한 F1팀이 배달로 자금을 벌어 머신을 복원하고 그랑프리에 복귀한다.", size=Pt(22), color=DIM)
add_text(s, Inches(0.45), Inches(4.5), Inches(12), Inches(0.5), "Arcade Racing × Delivery × Route Puzzle × Roguelite", size=Pt(15), color=DIM)
add_text(s, Inches(0.45), Inches(6.8), Inches(12), Inches(0.4), "Game Design Document  ·  Solo Dev  ·  Unity 6 (URP)", size=Pt(12), color=DIM)

# 2. Elevator Pitch
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Elevator Pitch", subtitle="한 문장")
add_text(s, Inches(0.6), Inches(2.2), Inches(12.2), Inches(1.0), "망한 F1팀이 배달로 자금을 벌어 머신을 복원하고", size=Pt(36), bold=True, color=FG)
add_text(s, Inches(0.6), Inches(3.05), Inches(12.2), Inches(1.0), "F1 그랑프리에 복귀한다.", size=Pt(36), bold=True, color=ACCENT)
add_text(s, Inches(0.6), Inches(4.5), Inches(12.2), Inches(2.0), [
    "배달은 수단, F1 복귀가 목적.",
    "드라이버는 당신, 엔지니어도 당신, 전략가도 당신.",
    "",
    "마이크로 루프(한 런 5~15분) ↔ 매크로 루프(머신 복원) 직각 관계.",
], size=Pt(18), color=DIM)
add_footer(s, 2, TOTAL, "Pitch")

# 3. Gameplay Snapshot
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Gameplay Snapshot", subtitle="Unity 6 / Kenny 도시 에셋 / 톱다운 운전")
img = Path("F1/Screenshots/screenshot.png")
if img.exists():
    pic = s.shapes.add_picture(str(img), Inches(0.6), Inches(1.7), height=Inches(5.0))
add_text(s, Inches(8.5), Inches(1.7), Inches(4.5), Inches(0.5), "현재 빌드", size=Pt(14), bold=True, color=ACCENT)
add_text(s, Inches(8.5), Inches(2.1), Inches(4.5), Inches(4.5), [
    "• OpenWorld_v2 씬",
    "• 106 도로 타일 + 8개 배달지",
    "• 차량 물리: 박스콜라이더 + 자체 컨트롤러",
    "• Service Locator 12/12 등록",
    "• 화물: 박스 슬라이드 → trunk 박음 → 손상",
    "",
    "⚠ 발표 시 30초 영상 권장",
], size=Pt(14), color=DIM)
add_footer(s, 3, TOTAL, "Snapshot")

# 4. Core Loop
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Core Loop", subtitle="한 런 5~15분 · 핵심 결정 4개")
stages = ["주문 보드", "포장 결정", "경로 계획", "출발 세팅", "주행", "정산", "업그레이드 / F1 복원"]
times = ["30초", "1분", "1~2분", "30초", "5~8분", "30초", "1분"]
decisions = ["결정 1", "", "결정 2", "결정 3", "", "", "결정 4"]
n = len(stages)
total_w = Inches(12.4)
gap = Inches(0.15)
w = Emu(int((total_w.emu - gap.emu * (n - 1)) / n))
left0 = Inches(0.45)
top = Inches(2.5)
for i, (st, tm, dc) in enumerate(zip(stages, times, decisions)):
    l = Emu(left0.emu + i * (w + gap.emu))
    box = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, l, top, w, Inches(1.4))
    box.line.color.rgb = ACCENT if dc else DIM
    box.line.width = Pt(1.5) if dc else Pt(0.5)
    box.fill.solid(); box.fill.fore_color.rgb = ACCENT2 if dc else BG
    tf = box.text_frame; tf.word_wrap = True; tf.vertical_anchor = MSO_ANCHOR.MIDDLE
    tf.margin_top = tf.margin_bottom = Pt(4); tf.margin_left = tf.margin_right = Pt(4)
    p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
    r = p.add_run(); r.text = st
    r.font.name = FONT; r.font.size = Pt(11); r.font.bold = True; r.font.color.rgb = FG
    p2 = tf.add_paragraph(); p2.alignment = PP_ALIGN.CENTER
    r2 = p2.add_run(); r2.text = tm
    r2.font.name = FONT; r2.font.size = Pt(9); r2.font.color.rgb = DIM
    if dc:
        p3 = tf.add_paragraph(); p3.alignment = PP_ALIGN.CENTER
        r3 = p3.add_run(); r3.text = dc
        r3.font.name = FONT; r3.font.size = Pt(9); r3.font.color.rgb = ACCENT; r3.font.bold = True
add_text(s, Inches(0.6), Inches(4.5), Inches(12.2), Inches(2.0), [
    "→ 한 런에서 핵심 결정 4개. 너무 적으면 지루, 너무 많으면 과부하.",
    "→ 5~15분 = 카페 한 잔/출퇴근 시간. EDC·Pacific Drive(90분+)와 차별.",
    "→ 런 = 통화 획득, 복원 = 서사 진행. 두 루프가 직각.",
], size=Pt(16), color=FG)
add_footer(s, 4, TOTAL, "Loop")

# 5. USP
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "USP — 차별점 4가지", subtitle="매 시스템 결정은 이 4가지 중 하나 이상을 강화해야 채택")
add_bullet_list(s, Inches(0.6), Inches(2.0), Inches(12), Inches(5), [
    "압축 폼팩터 — 5~15분 한 런 = 카페 한 잔",
    "결정 밀도 — 한 런에 핵심 결정 4개 (주문/경로/세팅/업그레이드)",
    "운전감 — Burnout급 아케이드 드리프트 + 시각적 즉각 피드백",
    "로그라이트 정산 — 업그레이드가 다음 런 감각을 바꿈 (단방향 진보 X)",
], size=Pt(22))
add_text(s, Inches(0.6), Inches(6.3), Inches(12), Inches(0.5), "강화 안 하는 시스템은 ideas-parking 직행.", size=Pt(14), color=DIM)
add_footer(s, 5, TOTAL, "USP")

# 6. Comparables
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "시장 빈 칸 (Comparables)", subtitle="왜 이 조합인가")
add_table(s, Inches(0.6), Inches(2.0), Inches(12.2), Inches(3.5),
          headers=["게임", "런 길이", "결정/런", "정산 → 다음 런"],
          rows=[
              ["Easy Delivery Co.", "무제한 (총 5h)", "낮음", "단방향"],
              ["Crazy Taxi", "라운드 짧음", "1", "없음"],
              ["Burnout Paradise", "이벤트 짧음", "0", "차량 잠금"],
              ["Pacific Drive", "90분+ 런", "높음", "로그라이트"],
              ["PIT CREW", "5~15분", "4", "로그라이트"],
          ])
add_text(s, Inches(0.6), Inches(6.0), Inches(12), Inches(0.8), "위 4 게임 어느 것도 「5~15분 + 결정 밀도 + 로그라이트 + 운전감」을 동시에 만족하지 못함.", size=Pt(15), color=ACCENT, bold=True)
add_footer(s, 6, TOTAL, "Market")

# 7. Key Features
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Key Features", subtitle="결정 채널 9 — 플레이어가 시간을 들여 조절하는 dial 9개")
add_table(s, Inches(0.45), Inches(1.85), Inches(12.4), Inches(4.8),
          headers=["#", "채널", "역할", "스코프"],
          rows=[
              ["1", "속도", "최고 속도 캡 + 가속/브레이크 곡선", "MVP 부분"],
              ["3", "맵 접근도", "도시·지역 해금 → 잡 거리 풀 확장", "MVP"],
              ["4", "배송물자 종류", "5종 화물 (Eggs/Glass/Cake/...)", "MVP"],
              ["5", "보관박스 크기", "적재 가능 수량 cap", "MVP"],
              ["6", "포장 (랩핑)", "4단계 (None/Bubble/Foam/Hardcase)", "MVP 통합"],
              ["7", "F1 복원 트리", "망가진 머신 부품 복원 — 서사 진행", "MVP"],
              ["8", "지도/내비", "GPS·이정표 해금 → 길찾기 부하 ↓", "MVP"],
              ["9", "연료탱크", "한 런 스테미나 (스타듀밸리 비유)", "MVP"],
          ])
add_text(s, Inches(0.6), Inches(6.85), Inches(12), Inches(0.5), "+ #2 외형 (Phase 3+) — 9채널 모두 dial 형태로 메타 진행 축 형성.", size=Pt(13), color=DIM)
add_footer(s, 7, TOTAL, "Features")

# 8. Genre / Tone
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Genre × Tone", subtitle="쿨하고 스타일리시 + 박살내는 카타르시스")
add_text(s, Inches(0.6), Inches(2.0), Inches(12), Inches(0.6), "장르", size=Pt(18), bold=True, color=ACCENT)
add_text(s, Inches(0.6), Inches(2.45), Inches(12), Inches(0.5), "Arcade Racing × Delivery × Route Optimization Puzzle × Roguelite", size=Pt(16), color=FG)
add_text(s, Inches(0.6), Inches(3.3), Inches(12), Inches(0.6), "톤", size=Pt(18), bold=True, color=ACCENT)
add_text(s, Inches(0.6), Inches(3.75), Inches(12), Inches(1.5), [
    "F1의 진지함과 「피자 배달」의 코믹한 갭이 게임의 매력.",
    "Kenny 에셋의 귀여운 로우폴리 미학 ↔ F1 브로드캐스트 UI 스타일의 대비.",
], size=Pt(16), color=FG)
add_text(s, Inches(0.6), Inches(5.3), Inches(12), Inches(0.6), "Pillars", size=Pt(18), bold=True, color=ACCENT)
add_text(s, Inches(0.6), Inches(5.75), Inches(12), Inches(1.5), [
    "① 운전 자체가 재밌어야 한다",
    "② 매 런 결정이 의미를 가져야 한다",
    "③ 시각 폴리시는 4주차 이후 (스코프 보호)",
], size=Pt(16), color=FG)
add_footer(s, 8, TOTAL, "Tone")

# 9. Player Experience
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Player Experience", subtitle="한 런 동안 플레이어가 느껴야 할 6단계 감정")
emotions = [
    ("계획", "어떤 순서로 돌까, 뭘 받을까", "머리 쓰는 만족감"),
    ("출발", "가자", "몰입 진입"),
    ("주행", "빠르게 가고 싶은데 박스가 걱정", "긴장"),
    ("코너", "속도 줄여야 하나 말아야 하나", "실시간 판단"),
    ("피트", "지금 들어갈까 한 바퀴 더 돌까", "자원 관리"),
    ("완주", "내 계획이 통했다 / 실패했다", "성취 혹은 재도전"),
]
top = Inches(2.0)
row_h = Inches(0.78)
for i, (stage, line, feel) in enumerate(emotions):
    y = Emu(top.emu + i * row_h.emu)
    badge = s.shapes.add_shape(MSO_SHAPE.OVAL, Inches(0.6), y, Inches(0.6), Inches(0.6))
    badge.line.fill.background(); badge.fill.solid(); badge.fill.fore_color.rgb = ACCENT
    tf = badge.text_frame; tf.vertical_anchor = MSO_ANCHOR.MIDDLE
    p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
    r = p.add_run(); r.text = str(i + 1); r.font.name = FONT; r.font.size = Pt(16); r.font.bold = True; r.font.color.rgb = RGBColor(0xFF, 0xFF, 0xFF)
    add_text(s, Inches(1.4), y, Inches(1.5), Inches(0.6), stage, size=Pt(18), bold=True, color=FG, anchor=MSO_ANCHOR.MIDDLE)
    add_text(s, Inches(3.0), y, Inches(6.5), Inches(0.6), f"“{line}”", size=Pt(15), color=FG, anchor=MSO_ANCHOR.MIDDLE)
    add_text(s, Inches(9.7), y, Inches(3.5), Inches(0.6), feel, size=Pt(14), color=ACCENT, bold=True, anchor=MSO_ANCHOR.MIDDLE)
add_footer(s, 9, TOTAL, "Experience")

# 10. Scope / Content
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Scope / Content", subtitle="MVP 4주 — 완성 가능 범위로 고정")
add_table(s, Inches(0.6), Inches(1.9), Inches(12.2), Inches(3.4),
          headers=["주차", "검증 질문", "산출"],
          rows=[
              ["1주", "차 모는 것 자체가 재밌는가?", "ArcadeCarController + 트랙 1개"],
              ["2주", "화물 관리가 긴장감을 주는가?", "CargoBox + 박스 시뮬"],
              ["3주", "경로 계획이 의미 있는가?", "주문 보드 + 경로 플래너"],
              ["4주", "5~15분 한 런이 실제로 굴러가는가?", "정산 + 업그레이드 + F1 복원 트리"],
          ])
add_text(s, Inches(0.6), Inches(5.7), Inches(12), Inches(0.5), "MVP 콘텐츠 양", size=Pt(16), bold=True, color=ACCENT)
add_text(s, Inches(0.6), Inches(6.1), Inches(12), Inches(1.0), [
    "화물 5종 · 포장 4단계 · 배달지 3개 · 업그레이드 5장 · 결정 채널 9개 · 맵 1개 (OpenWorld_v2)",
], size=Pt(14), color=FG)
add_footer(s, 10, TOTAL, "Scope")

# 11. Tech / Platform
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Tech / Platform")
add_text(s, Inches(0.6), Inches(2.0), Inches(6), Inches(0.5), "엔진 / 렌더", size=Pt(16), bold=True, color=ACCENT)
add_text(s, Inches(0.6), Inches(2.45), Inches(6), Inches(2), ["Unity 6 (6000.3.9f1)", "URP (Universal Render Pipeline)", "Input System 패키지"], size=Pt(15), color=FG)
add_text(s, Inches(0.6), Inches(4.0), Inches(6), Inches(0.5), "에셋", size=Pt(16), bold=True, color=ACCENT)
add_text(s, Inches(0.6), Inches(4.45), Inches(6), Inches(2), ["Kenny 도시 키트 (라이센스 안전)", "커스텀 모델링 0 — 제한이 곧 재미"], size=Pt(15), color=FG)
add_text(s, Inches(7.0), Inches(2.0), Inches(6), Inches(0.5), "아키텍처", size=Pt(16), bold=True, color=ACCENT)
add_text(s, Inches(7.0), Inches(2.45), Inches(6), Inches(2.5), [
    "Composition Root + Service Locator (12 시스템)",
    "Template Method (HUD 4종 통합 예정)",
    "Modifier Chain (CargoBox stats)",
    "Pub/Sub Audio Cue Bus",
    "Data Lookup (TireTypePresets)",
], size=Pt(14), color=FG)
add_text(s, Inches(7.0), Inches(4.5), Inches(6), Inches(0.5), "플랫폼 / 운영", size=Pt(16), bold=True, color=ACCENT)
add_text(s, Inches(7.0), Inches(4.95), Inches(6), Inches(2), ["PC (Windows) — Steam 1차 타깃", "솔로 개발", "Kenny 라이센스 안전 (CC0)"], size=Pt(15), color=FG)
add_footer(s, 11, TOTAL, "Tech")

# 12. Current Status
s = prs.slides.add_slide(blank); set_bg(s)
add_title(s, "Current Status", subtitle="아키텍처 Phase 1~6 완료 / 7~9 남음")
phases = [
    ("Phase 1", "DIP + Service Locator + Composition Root", True),
    ("Phase 2", "Template Method (Bar HUD 통합)", True),
    ("Phase 3", "Modifier Chain (CargoBox stats)", True),
    ("Phase 4", "Audio Cue Bus (Pub/Sub Mediator)", True),
    ("Phase 5", "Tire Type Presets (Data Lookup)", True),
    ("Phase 6", "Peer Review 자명 패치 6건", True),
    ("Phase 7", "IRunController DTO + UpgradeSystem 싱글톤 제거", False),
    ("Phase 8", "ResultPanel MVP 분할 + RewardCalculator SO", False),
    ("Phase 9", "TextHUDBase — 4 HUD 통합", False),
]
top = Inches(2.0)
row_h = Inches(0.48)
for i, (name, desc, done) in enumerate(phases):
    y = Emu(top.emu + i * row_h.emu)
    mark = "✓" if done else "○"
    color = ACCENT if done else DIM
    add_text(s, Inches(0.6), y, Inches(0.4), row_h, mark, size=Pt(20), bold=True, color=color, anchor=MSO_ANCHOR.MIDDLE)
    add_text(s, Inches(1.1), y, Inches(2.0), row_h, name, size=Pt(15), bold=True, color=FG if done else DIM, anchor=MSO_ANCHOR.MIDDLE)
    add_text(s, Inches(3.2), y, Inches(9.5), row_h, desc, size=Pt(14), color=FG if done else DIM, anchor=MSO_ANCHOR.MIDDLE)
# progress bar
bar_bg = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.6), Inches(6.5), Inches(12.2), Inches(0.4))
bar_bg.line.fill.background(); bar_bg.fill.solid(); bar_bg.fill.fore_color.rgb = ACCENT2
bar_fg = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.6), Inches(6.5), Inches(12.2 * 6 / 9), Inches(0.4))
bar_fg.line.fill.background(); bar_fg.fill.solid(); bar_fg.fill.fore_color.rgb = ACCENT
add_text(s, Inches(0.6), Inches(7.0), Inches(12), Inches(0.4), "아키텍처 진척: 6 / 9 phase  ·  MVP 4주 스코프 안에서 진행 중", size=Pt(12), color=DIM)
add_footer(s, 12, TOTAL, "Status")

out = Path("PitCrew_기획서.pptx")
prs.save(out)
print(f"saved: {out.resolve()}")
