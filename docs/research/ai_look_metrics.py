# 'AI 티' 내부 조사용 측정 (numpy 없이 PIL 만). docs/research/2026-10-27-ai-look-internal.md 의 수치 출처.
# 실행: python docs/research/ai_look_metrics.py  → 표 출력 + docs/research/ai-look/crops/*.png
import colorsys, math, os, statistics as st
from collections import Counter
from PIL import Image, ImageFilter

R = 'D:/Github/F1Game/'
OUT = R + 'docs/research/ai-look/crops/'
os.makedirs(OUT, exist_ok=True)

# (이름, 경로, 정면 박스[원본 px], 비고)
CROPS = [
    ('OUR_c_close', 'docs/qa/evidence/scene-a4c/house_close.png', (620, 20, 1420, 900), 'A4c 게임 근접(도현 "AI티")'),
    ('OUR_c_shop',  'art/_review/2026-10-27-houses-c/close_house_shop.png', (205, 70, 478, 510), 'c판 shop 정면'),
    ('OUR_c_narA',  'art/_review/2026-10-27-houses-c/close_house_narrow_a.png', (200, 0, 478, 590), 'c판 narrow_a 정면'),
    ('OUR_c_slope', 'art/_review/2026-10-27-houses-c/close_house_slope.png', (80, 20, 340, 420), 'c판 slope 정면'),
    ('OUR_d_shop',  'art/_review/2026-10-27-houses-d/close_house_shop.png', (130, 40, 478, 560), 'd판 사진 정면(실패)'),
    ('ACC_codex4_TEA', 'art/source/coastal-town/codex4_08_building_variants.png', (1080, 400, 1460, 820), 'D-097 받아들임'),
    ('ACC_codex4_MART','art/source/coastal-town/codex4_08_building_variants.png', (1470, 320, 1850, 740), 'D-097 받아들임'),
    ('REF1_ASH_house', 'art/reference/dotlp-2026-10-27/a-short-hike_1.jpg', (120, 320, 600, 780), '① 오두막'),
    ('REF6_EDC_bldg',  'art/reference/dotlp-2026-10-27/easy-delivery-co_1.jpg', (0, 0, 880, 660), '⑥ 붉은 건물'),
    ('REF5_WW_shop',   'art/reference/dotlp-2026-10-27/wheel-world_1.jpg', (200, 170, 1120, 800), '⑤ 자전거 가게'),
]


def load(path, box):
    im = Image.open(R + path).convert('RGB').crop(box)
    # 같은 크기 비교: 긴 변 256 (Nearest — 색 수 보존)
    w, h = im.size
    s = 256 / max(w, h)
    return im.resize((max(8, round(w * s)), max(8, round(h * s))), Image.NEAREST)


def lum(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]


def metrics(im):
    w, h = im.size
    px = list(im.getdata())
    L = [lum(p) for p in px]
    # 1) 유효 색 수: 4bit 양자화 후 90% 덮는 색 수
    q = Counter(((r >> 4), (g >> 4), (b >> 4)) for r, g, b in px)
    tot, acc, n90 = len(px), 0, 0
    for _, c in q.most_common():
        acc += c; n90 += 1
        if acc >= 0.9 * tot: break
    # 2) 8x8 블록: 거의 단색(L std<2) 비율, 블록 안 변화 중앙값
    flat, stds = 0, []
    for by in range(0, h - 7, 8):
        for bx in range(0, w - 7, 8):
            v = [L[(by + y) * w + bx + x] for y in range(8) for x in range(8)]
            sd = st.pstdev(v); stds.append(sd)
            if sd < 2: flat += 1
    # 3) 엣지 밀도
    e = im.convert('L').filter(ImageFilter.FIND_EDGES)
    ed = sum(1 for v in e.getdata() if v > 25) / tot
    # 4) 좌우 반전 차이 (L 평균 절대차 / L 평균편차) — 1.0 근처 = 비대칭, 0 = 완전 대칭
    mL = sum(L) / tot
    dev = sum(abs(v - mL) for v in L) / tot or 1
    flipd = sum(abs(L[y * w + x] - L[y * w + (w - 1 - x)]) for y in range(h) for x in range(w)) / tot
    # 5) 색: 평균 채도·명도, 색상군 수(12칸 중 ≥3%), 색상 원형 표준편차
    hs = []; S = []; V = []
    for r, g, b in px:
        hh, ss, vv = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        S.append(ss); V.append(vv)
        if ss > 0.15 and vv > 0.15: hs.append(hh)
    bins = Counter(int(x * 12) % 12 for x in hs)
    fam = sum(1 for c in bins.values() if c >= 0.03 * tot)
    if hs:
        cx = sum(math.cos(2 * math.pi * x) for x in hs) / len(hs); cy = sum(math.sin(2 * math.pi * x) for x in hs) / len(hs)
        hstd = math.degrees(math.sqrt(-2 * math.log(max(1e-9, math.hypot(cx, cy)))))
    else:
        hstd = 0
    return dict(colors90=n90, flat8=round(100 * flat / max(1, len(stds))), blockstd=round(st.median(stds), 1),
                edge=round(100 * ed, 1), asym=round(flipd / dev, 2), S=round(sum(S) / tot, 2), V=round(sum(V) / tot, 2),
                huefam=fam, huestd=round(hstd))


def windows_c_close():
    """house_close 정면에서 창·문(짙은 초록 L<95, 초록>빨강) 연결 성분 → 크기 종류·간격 균일도."""
    im = Image.open(R + 'docs/qa/evidence/scene-a4c/house_close.png').convert('RGB').crop((620, 20, 1420, 900))
    im = im.resize((im.width // 4, im.height // 4), Image.NEAREST)
    w, h = im.size; px = im.load()
    mask = [[(lum(px[x, y]) < 95 and px[x, y][1] >= px[x, y][0]) for x in range(w)] for y in range(h)]
    seen = [[False] * w for _ in range(h)]; comps = []
    for y in range(h):
        for x in range(w):
            if mask[y][x] and not seen[y][x]:
                stack = [(x, y)]; seen[y][x] = True; xs = []; ys = []
                while stack:
                    cx, cy = stack.pop(); xs.append(cx); ys.append(cy)
                    for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                        if 0 <= nx < w and 0 <= ny < h and mask[ny][nx] and not seen[ny][nx]:
                            seen[ny][nx] = True; stack.append((nx, ny))
                if len(xs) > 40:
                    comps.append((min(xs) * 4, min(ys) * 4, (max(xs) - min(xs) + 1) * 4, (max(ys) - min(ys) + 1) * 4, len(xs)))
    return comps


if __name__ == '__main__':
    rows = []
    for name, path, box, note in CROPS:
        im = load(path, box); im.resize((im.width * 2, im.height * 2), Image.NEAREST).save(OUT + name + '.png')
        m = metrics(im); rows.append((name, note, m))
    keys = ['colors90', 'flat8', 'blockstd', 'edge', 'asym', 'S', 'V', 'huefam', 'huestd']
    print('| 크롭 | 비고 | ' + ' | '.join(keys) + ' |')
    for name, note, m in rows:
        print(f'| {name} | {note} | ' + ' | '.join(str(m[k]) for k in keys) + ' |')
    print('\n창·문 성분(OUR_c_close, x,y,w,h,area/16):')
    for c in sorted(windows_c_close()):
        print(c)
