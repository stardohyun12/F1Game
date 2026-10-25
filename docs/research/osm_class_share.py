# -*- coding: utf-8 -*-
"""실제 마을 차길의 '등급 비중' 을 OSM highway 태그로 잰다 (PD 20261025-1418). 읽기 전용.
사용: python -X utf8 docs/research/osm_class_share.py   (osm_street_metrics.py 가 임시 폴더에 받아 둔 캐시를 쓴다)
등급: 간선=trunk/primary/secondary/tertiary(+link) · 동네길=unclassified/residential/living_street · 서비스=service · 비포장=track
주의: OSM 태그는 지역 편집자의 선택이다 — 한국·이탈리아·일본의 'residential' 이 같은 폭이라는 보장은 없다.
"""
import math, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import osm_street_metrics as O   # noqa: E402

TRUNK = {"motorway", "trunk", "primary", "secondary", "tertiary", "motorway_link", "trunk_link", "primary_link", "secondary_link", "tertiary_link"}
LOCAL = {"unclassified", "residential", "living_street"}


def dist(a, b, lat0):
    kx = 111320 * math.cos(math.radians(lat0)); ky = 110540
    return math.hypot((a[0] - b[0]) * kx, (a[1] - b[1]) * ky)


def main():
    print("| 장소 | 차길 km | 간선 % | 동네길 % | 서비스 % | 비포장 % | 이름길 1위 % | 2위 % | 3위 % |\n|---|---|---|---|---|---|---|---|---|")
    for name, bb in O.PLACES.items():
        try:
            d = O.fetch(name, bb)
        except Exception as e:  # noqa: BLE001
            print(f"| {name} | 실패 {e} |"); continue
        nodes = {e["id"]: (e["lon"], e["lat"]) for e in d["elements"] if e["type"] == "node"}
        lat0 = (bb[0] + bb[2]) / 2
        acc = {"trunk": 0.0, "local": 0.0, "service": 0.0, "track": 0.0}
        by_name = {}
        for w in d["elements"]:
            if w["type"] != "way": continue
            t = w.get("tags", {})
            if O.classify(t) not in ("car", "track"): continue
            ids = [n for n in w["nodes"] if n in nodes]
            ln = sum(dist(nodes[a], nodes[b], lat0) for a, b in zip(ids, ids[1:]))
            h = t.get("highway")
            k = "trunk" if h in TRUNK else "local" if h in LOCAL else "track" if h == "track" else "service"
            acc[k] += ln
            nm = t.get("name") or t.get("ref")
            if nm: by_name[nm] = by_name.get(nm, 0.0) + ln
        tot = sum(acc.values())
        if tot == 0: print(f"| {name} | 0 |"); continue
        tops = sorted(by_name.items(), key=lambda kv: -kv[1])[:3] + [("-", 0)] * 3
        print(f"| {name} | {tot/1000:.2f} | {100*acc['trunk']/tot:.0f} | {100*acc['local']/tot:.0f} | {100*acc['service']/tot:.0f} | {100*acc['track']/tot:.0f} | {100*tops[0][1]/tot:.0f} ({tops[0][0]}) | {100*tops[1][1]/tot:.0f} ({tops[1][0]}) | {100*tops[2][1]/tot:.0f} ({tops[2][0]}) |")


if __name__ == "__main__":
    main()
