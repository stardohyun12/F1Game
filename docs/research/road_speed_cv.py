# -*- coding: utf-8 -*-
"""속도 변동폭(시간 가중 평균·표준편차·변동계수) — road_rhythm.py 와 같은 모형 (PD 20261025-1509). 읽기 전용.
사용: python -X utf8 docs/research/road_speed_cv.py [최고속 km/h, 기본 85]
Game Developer 'A Rational Approach To Racing Game Track Design' 가 아케이드 트랙의 속도 표준편차를 ±30 % 안쪽으로 잡으라 했다 → 같은 잣대로 v2 와 실제 도로를 잰다.
"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "..", "..", "tools", "map"))
import road_rhythm as RR   # noqa: E402
import planlib as L        # noqa: E402


def cv(pts, cap_kmh, closed=False):
    q = RR.resample(pts)
    R, v = RR.profile(q, cap_kmh / 3.6, closed)
    ts = [4.0 / max(0.5, (v[i] + v[i + 1]) / 2) for i in range(len(v) - 1)]
    m = sum(t * x for t, x in zip(ts, v)) / sum(ts)
    sd = (sum(t * (x - m) ** 2 for t, x in zip(ts, v)) / sum(ts)) ** 0.5
    return m * 3.6, sd * 3.6, 100 * sd / m


def main():
    cap = float(sys.argv[1]) if len(sys.argv) > 1 else 85.0
    plan = L.load()
    r = next(x for x in plan["roads"] if x["id"] == "main")
    print(f"최고속 {cap:.0f} km/h — | 대상 | 시간가중 평균 km/h | 표준편차 km/h | 변동계수 % |")
    print("v2 main", [round(x, 1) for x in cv([(p[0], p[1]) for p in L.sample(r)], cap, True)])
    for n, (bb, f) in RR.PLACES_ROAD.items():
        d = RR.fetch(n, bb, f)
        pts, _ = RR.longest_chain(d, (bb[0] + bb[2]) / 2)
        print(n, [round(x, 1) for x in cv(pts, cap)])


if __name__ == "__main__":
    main()
