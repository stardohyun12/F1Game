"""기준 화면(①⑥⑫) 픽셀 측정: (a) 화면 블록 크기(1080 기준 px) — 가로·세로 밝기 경계 사이 거리의 주기,
(c) 면당 색 수 — 평평한 칸(32×32)에서 블록 단위 색 수(5단계 양자화, JPEG 잡음 제거).

    python -X utf8 tools/asset-gen/opus_measure_refs.py
"""
import json, os, sys
from collections import Counter
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
REF = os.path.join(ROOT, 'art', 'reference', 'dotlp-2026-10-27')


def edges_period(im, axis):
    """한 줄을 따라 밝기 차 > 18 인 경계 위치를 모아, 경계 간 거리 히스토그램 → 주기 점수(k=1..12)."""
    g = im.convert('L'); w, h = g.size; px = g.load(); score = Counter()
    lines = range(0, h, 3) if axis == 0 else range(0, w, 3)
    for j in lines:
        pos = []
        n = w if axis == 0 else h
        prev = px[0, j] if axis == 0 else px[j, 0]
        for i in range(1, n):
            v = px[i, j] if axis == 0 else px[j, i]
            if abs(v - prev) > 18: pos.append(i)
            prev = v
        for a, b in zip(pos, pos[1:]):
            if 1 <= b - a <= 40: score[b - a] += 1
    # 주기 k 의 점수 = 경계 거리 d 중 k 의 배수 비율(무작위면 1/k)
    tot = sum(score.values()) or 1
    res = {}
    for k in range(2, 13):
        hit = sum(c for d, c in score.items() if d % k == 0) / tot
        res[k] = round(hit * k, 2)  # 1.0 = 우연, 클수록 주기적
    return res, score.most_common(8)


def colors_flat(im, bs):
    """bs px 블록 중심 샘플로 축소 → 8×8 블록 창마다 색 수(양자화 24단계) 중간값."""
    sm = im.convert('RGB').resize((im.width // bs, im.height // bs), Image.NEAREST)
    q = sm.point(lambda v: v // 24); w, h = q.size; out = []
    for y in range(0, h - 8, 8):
        for x in range(0, w - 8, 8):
            out.append(len(set(q.crop((x, y, x + 8, y + 8)).getdata())))
    out.sort(); return out[len(out) // 4], out[len(out) // 2], out[3 * len(out) // 4]


def main():
    rep = {}
    for n in sys.argv[1:] or ['a-short-hike_1', 'a-short-hike_2', 'easy-delivery-co_1', 'easy-delivery-co_2', 'crow-country_1', 'crow-country_2']:
        im = Image.open(os.path.join(REF, n + '.jpg'))
        px_h, top_h = edges_period(im, 0); px_v, top_v = edges_period(im, 1)
        kh = max(px_h, key=px_h.get); kv = max(px_v, key=px_v.get)
        s1080 = 1080 / im.height
        rep[n] = {'size': im.size, 'period_h': px_h, 'period_v': px_v, 'top_dist_h': top_h, 'best_k': (kh, kv),
                  'block_1080': round(kh * s1080, 2), 'internal_h_est': round(im.height / kh)}
        print(n, im.size, 'best k', kh, kv, '→1080', rep[n]['block_1080'], 'internal≈', rep[n]['internal_h_est'], 'top', top_h[:5])
    json.dump(rep, open(os.path.join(ROOT, 'art', '_review', '2026-10-27-garage-kart-opus-v3', 'measure_refs.json'), 'w'), indent=1)


if __name__ == '__main__':
    main()
