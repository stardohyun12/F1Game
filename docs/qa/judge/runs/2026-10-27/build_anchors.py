import json, random, os
from PIL import Image, ImageDraw, ImageFont

R = "D:/Github/F1Game/"
S = "C:/Users/rtx40/AppData/Local/hermes/cache/scratch/judge/"
OUT = R + "docs/qa/judge/"
os.makedirs(OUT, exist_ok=True)
os.makedirs(S + "run/img", exist_ok=True)

# crop = (x0,y0,x1,y1) in original pixels or None
A = [
 # ---- positive
 dict(label="pos", path="docs/qa/evidence/slice-vs-concept.png", crop=(960,0,1920,1080),
      what="원화 재현 슬라이스 Slice_PostcardStreet (오른쪽 열 = 게임 캡처 2장, 원화 칸 잘라냄)",
      date="2026-10-23 17:57", quote="괜찮네 … 비주얼적으론 합격임", src="D-130, Hermes 대화"),
 dict(label="pos", path="art/_review/2026-10-25-car-candidates/A_village-960.png", crop=None,
      what="차 후보 A, 실제 마을 캡처 위 Blender 합성",
      date="2026-10-26 01:17", quote="B 좋고 자동차는 A,C 둘다 좋은것 같은데", src="D-143"),
 dict(label="pos", path="art/_review/2026-10-25-car-candidates/C_village-960.png", crop=None,
      what="차 후보 C, 실제 마을 캡처 위 Blender 합성",
      date="2026-10-26 01:17 / 01:38", quote="A,C 둘다 좋은것 같은데 / 차도 이전에 뽑은 이 이미지 느낌대로 ㄱㄱ", src="D-143·D-144"),
 dict(label="pos", path="art/source/coastal-town/codex4_08_building_variants.png", crop=None,
      what="건물 세트 Codex 4차 변형 모음",
      date="2026-10-13", quote="이게 더 좋다, 이런 느낌으로 다양하게", src="D-097 (결정 로그 요약 인용)"),
 dict(label="pos", path="art/concept/lofi-styles-2026-10-13/02_warm_street.png", crop=None,
      what="기준 원화 02_warm_street (Codex imagegen) — 채택 당시. 뒤에 'AI 티' 원인으로 강등(도현 직접 거부 아님)",
      date="2026-10-13 / 10-23 재확인", quote="Codex 하고 만든 아트 스타일로 가는 게 맞을 듯", src="D-096·D-123"),
 dict(label="pos", path="docs/qa/evidence/hilltop-v2/hilltop-5-crest-wide.png", crop=None,
      what="언덕 위 도착 슬라이스 2차, 꼭대기 넓게",
      date="2026-10-25 09:58", quote="괜찮은것 같은데 내가 씬 화면 직접 둘러보면서 어떤지 봐야할듯", src="Hermes 대화"),
 dict(label="pos", path="docs/qa/evidence/hilltop-3-crest.png", crop=None,
      what="언덕 위 도착 슬라이스 3차 꼭대기 (구조 비판은 맵 단위, 화면 분위기는 긍정)",
      date="2026-10-25 10:33", quote="주행하는곳 자체는 나쁘지 않은데 … 단편적인 분위기는 괜찮지만 자세히 보면 이상하거나", src="D-134"),
 # ---- negative
 dict(label="neg", path="art/_review/2026-10-23-d121/cargo-sheet.png", crop=None,
      what="D-121 장난감 실루엣 화물 시안",
      date="2026-10-23", quote="초반 방향보다 아기자기하다", src="D-123"),
 dict(label="neg", path="docs/qa/evidence/mood-compare.png", crop=(960,0,1920,1080),
      what="무드 조명 시안 (원화 칸 잘라냄, 게임 캡처 2장)",
      date="2026-10-23 17:21", quote="조명이 문제가 아닌듯 분위기가 안남", src="Hermes 대화"),
 dict(label="neg", path="art/_review/2026-10-23-car/compare.png", crop=None,
      what="새 F1 차 v1 (해안 캡처 위)",
      date="2026-10-23 21:40", quote="이것도 약간은 배경과 톤의 이질감이 들지않아? … ai티가 많이남", src="Hermes 대화"),
 dict(label="neg", path="docs/qa/evidence/dohyun-1509/1.png", crop=None,
      what="도현 직접 플레이 스샷 — 해안 비탈",
      date="2026-10-26 15:09", quote="솔직히 이런 느낌도 그냥 그림판에 색칠한 느낌인데", src="D-145"),
 dict(label="neg", path="docs/qa/evidence/dohyun-terrain-noise-20261026.png", crop=None,
      what="도현 직접 플레이 스샷 — 지형 질감",
      date="2026-10-26 14:47", quote="그림처럼 지형에 노이즈가 끼듯이 표현되는점이 이상함", src="Hermes 대화"),
 dict(label="neg", path="docs/qa/evidence/dohyun-1940/1.png", crop=None,
      what="도현 직접 플레이 스샷 — 사진→픽셀 점무늬 지면",
      date="2026-10-26 19:40", quote="이건 레트로 감성보다 더럽게 느껴질것 같음", src="D-146"),
 dict(label="neg", path="art/_review/2026-10-26-people-v3/sheet.png", crop=None,
      what="사람 v3 (4~5등신)",
      date="2026-10-26 20:11", quote="v3별로임 게임 톤과 안맞음", src="D-147"),
 dict(label="neg", path="docs/qa/evidence/retro-v1/b_street_look4.png", crop=None,
      what="Look 4 Retro-Clean 새로 칠한 배경",
      date="2026-10-26 20:11", quote="새로 칠한 배경도 그렇고 easy delivery co 가 레퍼런스 였는데 거기서 점점 멀어지는 느낌", src="D-147"),
 dict(label="neg", path="docs/qa/evidence/dohyun-2110/1.png", crop=None,
      what="도현 직접 플레이 스샷 — 지글거림",
      date="2026-10-26 21:10", quote="지금은 그냥 성능이상한 게임으로 밖에 보이지 않아", src="D-149"),
 dict(label="neg", path="art/_review/2026-10-27-garage-kart/sheet.png", crop=None,
      what="정비소·고카트 v0 (Codex, AI 판정 7·7)",
      date="2026-10-27 02:10", quote="도트 기반 로우폴리인데 둘다 아닌것 같은데", src="D-163"),
 dict(label="neg", path="art/_review/2026-10-27-garage-kart-v1/sheet.png", crop=None,
      what="정비소·고카트 v1 Codex (AI 판정 6·5)",
      date="2026-10-27 02:25", quote="둘다 우리 게임의 아트를 잘 이해를 못한것 같은데", src="감사 §1"),
 dict(label="neg", path="art/_review/2026-10-27-garage-kart-opus-v2/sheet.png", crop=None,
      what="정비소·고카트 v2 Opus (AI 판정 6, v1 보다 올라감)",
      date="2026-10-27 02:46", quote="솔직히 별로, 퀄리티가 더 낮아진 것 같아", src="D-165"),
 dict(label="neg", path="art/_review/2026-10-27-garage-kart-opus-v5/x1_game.png", crop=None,
      what="정비소 앞 장면 v5 Opus (AI 판정 5.3)",
      date="2026-10-27 03:58", quote="왜 계속 안돼는지 파악해보고 어떻게 해결할지 생각해봐", src="감사 §1"),
 dict(label="neg", path="art/_review/2026-10-27-scene-a-tex/sheet.png", crop=None,
      what="장면 A 텍스처 V1 포토배싱·V2 팔레트 시트",
      date="2026-10-27 04:33", quote="바로위 그림 보면 퀄리티가 더 낮아진것같음", src="Hermes 대화"),
]
T = [
 dict(label="target", path="art/reference/dotlp-2026-10-27/a-short-hike_1.jpg", what="① A Short Hike", quote="픽셀에서 1,6,12 같이 나왔으면 좋겠는데", date="2026-10-27 02:47", src="D-165 PICKS"),
 dict(label="target", path="art/reference/dotlp-2026-10-27/easy-delivery-co_1.jpg", what="⑥ Easy Delivery Co.", quote="픽셀에서 1,6,12 같이 나왔으면 좋겠는데", date="2026-10-27 02:47", src="D-165 PICKS"),
 dict(label="target", path="art/reference/dotlp-2026-10-27/crow-country_1.jpg", what="⑫ Crow Country", quote="픽셀에서 1,6,12 같이 나왔으면 좋겠는데", date="2026-10-27 02:47", src="D-165 PICKS"),
]
EXCL = [
 ("art/_review/2026-10-23-cargo-box-v03/density-compare.png", "D-124 \"ㅇㅇ\"는 32 vs 64px/m 밀도 수치 선택이지 화면 수용이 아님 + 배경이 AI 원화 합성"),
 ("art/_review/2026-10-27-garage-kart-opus/sheet.png", "Opus v1: \"opus도 좋은데 조금 아쉽지 않나\" — 긍정·부정 혼합"),
 ("docs/qa/evidence/hilltop-v1/hilltop-3-crest.png", "\"지형 자체는 괜찮은것 같기도 하고 근데 생활감 … 필요\" — 혼합"),
 ("docs/qa/evidence/bayloop-v1/02_shop_street_climb.png", "\"너무 일본 느낌\" — 방향(나라) 지적, 품질 판정 아님"),
 ("docs/qa/evidence/bayloop-v4/contact.png", "\"괜찮긴한데 건물들이 왤케 부자연스럽지\" — 혼합, 원인이 배치"),
 ("docs/qa/evidence/opening-v0/car_C_harbor.png", "반응(\"이전에 뽑은 이 이미지 느낌대로\")이 시안을 가리킴, 게임 컷 자체 판정 불명"),
 ("docs/qa/evidence/tone-slice*/ , look5c", "\"해상도를 조금 더 낮춘게 오히려 안좋은것 같음\" — 해상도 값 판정, 컷 판정 아님"),
 ("docs/qa/evidence/look5-edc, look5b", "\"EDC는 너무 어둡다\" — EDC 원본에 대한 말, 우리 컷 판정 아님"),
 ("art/_review/2026-10-27-garage-kart-v3, -opus-v3, -opus-v4", "도현 직접 판정 없음(감사 §1)"),
 ("art/_review/2026-10-27-shape-language/sheet.png", "승인(D-167)했지만 기준 게임 측정 시트 — 우리 아트 아님"),
 ("art/_review/2026-10-23-cargo-set-v2/lineup-scene.png", "\"계란과 케이크 … 바꿔야\" — 일부 품목만"),
 ("docs/qa/evidence/car-look-v1/sheet.png", "반응 기록 없음"),
 ("D-101 어두운 씬, D-110/112 TownGen 씬 뷰", "반응은 있으나 캡처 파일 없음"),
]

random.seed(20261027)
ids = random.sample(range(10, 99), len(A))
MAXS = 1600
for a, n in zip(A, ids):
    a["id"] = f"X{n}"
    im = Image.open(R + a["path"]).convert("RGB")
    if a["crop"]:
        im = im.crop(a["crop"])
    im.thumbnail((MAXS, MAXS))
    im.save(S + f"run/img/{a['id']}.png")
for i, t in enumerate(T):
    t["id"] = ["REF1", "REF6", "REF12"][i]
    im = Image.open(R + t["path"]).convert("RGB"); im.thumbnail((1280, 1280))
    im.save(S + f"run/img/{t['id']}.png")

json.dump(dict(
    made="2026-10-27", note="id = 판정 때 쓰는 익명 이름(판정자에게 경로·반응 비공개). crop = 원본 픽셀 좌표.",
    targets=T, positive=[a for a in A if a["label"] == "pos"], negative=[a for a in A if a["label"] == "neg"],
    excluded=[dict(path=p, reason=r) for p, r in EXCL]),
    open(OUT + "anchors.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)

# anchors.png : rows target / positive / negative
font = None
for f in ["C:/Windows/Fonts/malgun.ttf"]:
    try: font = ImageFont.truetype(f, 15)
    except Exception: pass
cw, ch = 300, 190
rows = [("목표 ①⑥⑫", T), ("긍정 (도현 수용)", [a for a in A if a["label"] == "pos"]), ("부정 (도현 거부)", [a for a in A if a["label"] == "neg"])]
maxn = 7
lines = []
for name, items in rows:
    for k in range(0, len(items), maxn):
        lines.append((name if k == 0 else name + " (계속)", items[k:k + maxn]))
sheet = Image.new("RGB", (140 + maxn * cw, len(lines) * (ch + 46)), (245, 242, 235))
d = ImageDraw.Draw(sheet)
for r, (name, items) in enumerate(lines):
    y = r * (ch + 46)
    col = {"목": (40, 90, 160), "긍": (30, 130, 60), "부": (170, 40, 40)}[name[0]]
    d.rectangle([0, y, 132, y + ch + 40], fill=col)
    d.text((8, y + 10), name, fill="white", font=font)
    for c, it in enumerate(items):
        x = 140 + c * cw
        src = Image.open(S + f"run/img/{it['id']}.png"); src.thumbnail((cw - 8, ch - 4))
        sheet.paste(src, (x + 4, y + 4))
        q = it["quote"]; q = q if len(q) <= 20 else q[:19] + "…"
        d.text((x + 4, y + ch + 2), f"{it['id']} {it['date'][:10]}", fill=(60, 60, 60), font=font)
        d.text((x + 4, y + ch + 20), f"\"{q}\"", fill=col, font=font)
sheet.save(OUT + "anchors.png")
print(len(A), "anchors;", sum(a['label']=='pos' for a in A), "pos")
print({a['id']: a['label'] for a in A})
