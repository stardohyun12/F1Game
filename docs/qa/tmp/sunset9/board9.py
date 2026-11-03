"""9판 최종판(도현용) → docs/qa/evidence/sunset9/
 sit_1x.png       앉은 시점 1장(640×360 그대로)
 sit_3x.png       같은 그림 3배(Nearest)
 cmp_6_9_ash.png  6판 | 9판 | A Short Hike(참고, 낮 장면) — 1배 같은 크기
 pick_1..3.png    15 s 연속 16장(raw/w00..w15)에서 고른 3장 + strip15.png(16장 한 판)
 fix_before_after.png  8판 결함 3개 전/후(같은 카메라, 3배 확대 조각)"""
import pathlib, json, shutil
from PIL import Image, ImageDraw, ImageFont
R = pathlib.Path(__file__).parents[4]
E6, E8, E9 = R / "docs/qa/evidence/sunset6", R / "docs/qa/evidence/sunset8", R / "docs/qa/evidence/sunset9"
font = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 18)
W, H, G, TOP = 640, 360, 6, 28

def fit(p):
    im = Image.open(p).convert("RGB")
    s = max(W / im.width, H / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS if s < 1 else Image.NEAREST)
    x, y = (im.width - W) // 2, (im.height - H) // 2
    return im.crop((x, y, x + W, y + H))

def row(tiles, out):
    b = Image.new("RGB", (len(tiles) * (W + G) + G, H + TOP + 2 * G), (24, 24, 28)); d = ImageDraw.Draw(b)
    for i, (im, cap) in enumerate(tiles):
        x = G + i * (W + G); d.text((x, G), cap, fill=(225, 225, 225), font=font); b.paste(im, (x, G + TOP))
    b.save(out); print(out.name, b.size)

sit = Image.open(E9 / "raw/final.png").convert("RGB"); sit.save(E9 / "sit_1x.png")
sit.resize((W * 3, H * 3), Image.NEAREST).save(E9 / "sit_3x.png")
row([(fit(E6 / "play/finB_a_fmid.png"), "6판 (Play)"), (sit, "9판 (Play, 최종)"),
     (fit(R / "art/reference/dotlp-2026-10-27/a-short-hike_2.jpg"), "A Short Hike 참고 (낮 장면, 16:9 로 자름)")], E9 / "cmp_6_9_ash.png")

rows = [json.loads(l) for l in (E9 / "raw/w.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]
def cap(j): return f"{j['tag']} {j['t']:.1f}s 등대{'켜짐' if j['lamp'] else '-'} 반짝{'O' if j['glint'] else '-'} 갈매기{'O' if j['gulls'] else '-'}{' 울음' if abs(j['lastCall'] - j['t']) < 0.6 else ''}"
for n, tag in enumerate(("w00", "w06", "w15"), 1):
    shutil.copy(E9 / f"raw/{tag}.png", E9 / f"pick_{n}_{tag}.png")
S = 0.5; sw, sh = int(W * S), int(H * S)
sb = Image.new("RGB", (4 * (sw + G) + G, 4 * (sh + 24 + G) + G), (24, 24, 28)); d = ImageDraw.Draw(sb)
f2 = ImageFont.truetype("C:/Windows/Fonts/malgun.ttf", 13)
for i, j in enumerate(rows):
    x, y = G + (i % 4) * (sw + G), G + (i // 4) * (sh + 24 + G)
    d.text((x, y), cap(j), fill=(220, 220, 220), font=f2); sb.paste(Image.open(E9 / f"raw/{j['tag']}.png").convert("RGB").resize((sw, sh), Image.LANCZOS), (x, y + 22))
sb.save(E9 / "strip15.png"); print("strip15.png", sb.size)

# 결함 전/후: 8판 s1(기본 시선) ↔ 9판 final, 같은 카메라. 조각은 3배 Nearest
def crop3(p, box): return Image.open(p).convert("RGB").crop(box).resize(((box[2] - box[0]) * 3, (box[3] - box[1]) * 3), Image.NEAREST)
parts = [("① 가운데 나무 반사 덩어리 (화면 전체 1배)", None),
         ("② 섬 끝 물가 흰 점선 (3배)", (250, 115, 430, 175)),
         ("③ 낚싯대 끝이 물에 묻힘 (3배)", (450, 150, 600, 300))]
tiles = []
for name, box in parts:
    a = Image.open(E8 / "raw/s1.png").convert("RGB") if box is None else crop3(E8 / "raw/s5.png" if "섬" in name else E8 / "raw/s1.png", box)
    b = Image.open(E9 / "raw/final.png").convert("RGB") if box is None else crop3(E9 / "raw/final.png", box)
    tiles.append((name, a, b))
TW = max(t[1].width for t in tiles) * 2 + 3 * G; TH = sum(t[1].height + TOP + G for t in tiles) + G
fb = Image.new("RGB", (TW, TH), (24, 24, 28)); d = ImageDraw.Draw(fb); y = G
for name, a, b in tiles:
    d.text((G, y), name + "   왼쪽 8판 · 오른쪽 9판", fill=(225, 225, 225), font=font)
    fb.paste(a, (G, y + TOP)); fb.paste(b, (2 * G + a.width, y + TOP)); y += a.height + TOP + G
fb.save(E9 / "fix_before_after.png"); print("fix_before_after.png", fb.size)
