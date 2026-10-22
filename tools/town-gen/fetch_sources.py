# -*- coding: utf-8 -*-
"""마을 생성기 실험 E0 — 원본 캐시 (Overpass OSM 도로 + Copernicus DEM GLO-30).

스펙: docs/superpowers/specs/2026-10-19-town-generator-experiment.md §6.1, §6.3
재생성 때 서버에 의존하지 않도록 원본 응답을 날짜와 함께 저장한다.
이미 받은 파일은 건너뛴다(--force 로 다시 받기).

사용:  py -X utf8 tools/town-gen/fetch_sources.py [--force]
표준 라이브러리만 쓴다.
"""
import json
import os
import sys
import time
import urllib.parse
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
OSM_DIR = os.path.join(HERE, "osm")
DEM_DIR = os.path.join(HERE, "dem")
TODAY = time.strftime("%Y-%m-%d")
FORCE = "--force" in sys.argv
UA = "PitCrew-F1Game-towngen/0.1 (experiment cache; stardohyun12/F1Game)"

# 캐시 상자는 스펙 조각(아말피 240x100m, 포지타노 480x300m)보다 넉넉하게 잡는다 — 자르기는 E2 앞단에서.
# (south, west, north, east). 위도 1도 = 약 111.0km, 경도 1도 = 약 84.5km (위도 40.63 기준)
BOXES = {
    # 아말피 항구 — 두오모 앞 광장·항구 방파제·SS163 해안도로 포함. 약 675m(동서) x 555m(남북)
    "amalfi_harbor": (40.6315, 14.5990, 40.6365, 14.6070),
    # 포지타노 비탈 — 해변(Spiaggia Grande)에서 북쪽 비탈 위 SS163 까지. 약 845m x 666m
    "positano_slope": (40.6270, 14.4800, 40.6330, 14.4900),
    # 구조 스펙 D-113 상자 A — 아말피 항구 → SS163 해안도로 → 카포 디 콘카 곶 → 콘카 데이 마리니. 3.12 x 2.72km (남쪽 절반 바다)
    "structure_a": (40.6130, 14.5690, 40.6375, 14.6060),
}

OVERPASS_MIRRORS = [
    "https://overpass-api.de/api/interpreter",
    "https://overpass.kumi.systems/api/interpreter",
    "https://maps.mail.ru/osm/tools/overpass/api/interpreter",
]

# Copernicus DEM GLO-30 (공개판) — AWS Open Data. 1도 타일 하나가 두 상자를 모두 덮는다(N40, E14).
DEM_TILE = "Copernicus_DSM_COG_10_N40_00_E014_00_DEM"
DEM_URL = f"https://copernicus-dem-30m.s3.amazonaws.com/{DEM_TILE}/{DEM_TILE}.tif"
DEM_DOC_URLS = {
    "readme.html": "https://copernicus-dem-30m.s3.amazonaws.com/readme.html",
}


def get(url, data=None, timeout=180):
    req = urllib.request.Request(url, data=data, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.status, r.read()


def fetch_osm(name, box, kind=""):
    """kind="" → way[highway] (<name>_<날짜>.json) · kind="buildings" → way/relation[building] (<name>_buildings_<날짜>.json, 조각 10 D-114)."""
    tag = f"{name}_{kind}" if kind else name
    out = os.path.join(OSM_DIR, f"{tag}_{TODAY}.json")
    existing = [f for f in os.listdir(OSM_DIR) if f.startswith(tag + "_2") and f.endswith(".json") and not f.endswith(".meta.json")]
    if existing and not FORCE:
        print(f"[osm] {tag}: 캐시 있음 {existing[0]} — 건너뜀")
        return os.path.join(OSM_DIR, existing[0])
    s, w, n, e = box
    if kind == "buildings":
        return fetch_buildings_tiled(tag, out, box)
    query = f'[out:json][timeout:120];way["highway"]({s},{w},{n},{e});out body geom;'
    raw, mirror, errors = overpass(query)
    if raw is None:
        raise RuntimeError(f"[osm] {tag} 실패:\n  " + "\n  ".join(errors))
    doc = json.loads(raw)
    ways = [el for el in doc.get("elements", []) if el.get("type") == "way"]
    with open(out, "wb") as f:
        f.write(raw)
    meta = {
        "box_swne": box, "query": query, "mirror": mirror, "fetched": time.strftime("%Y-%m-%dT%H:%M:%S%z"),
        "osm_base": doc.get("osm3s", {}).get("timestamp_osm_base"), "ways": len(ways), "bytes": len(raw), "license": "© OpenStreetMap contributors, ODbL",
    }
    with open(out.replace(".json", ".meta.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, ensure_ascii=False, indent=1)
    print(f"[osm] {tag}: way {len(ways)}개, {len(raw)} bytes, {mirror}")
    return out


def overpass(query, need_elements=True):
    """미러·재시도를 돌며 (raw, mirror, errors). 실패면 raw=None. 502/500/504/406/429 가 실제로 난다."""
    body = urllib.parse.urlencode({"data": query}).encode()
    errors = []
    for mirror in OVERPASS_MIRRORS:
        for attempt in range(3):
            try:
                status, raw = get(mirror, data=body)
                doc = json.loads(raw)
                if need_elements and not doc.get("elements"):
                    raise ValueError("요소 0개")
                return raw, mirror, errors
            except Exception as ex:
                errors.append(f"{mirror} #{attempt + 1}: {ex}")
                time.sleep(5 * (attempt + 1))
    return None, None, errors


def fetch_buildings_tiled(tag, out, box, tiles=4):
    """상자 A 한 번에 물으면 서버가 502/500(2026-10-20 실측) → tiles×tiles 칸으로 나눠 받고 (type,id) 로 중복 제거해 합친다."""
    s, w, n, e = box
    elems, seen, queries, mirrors, base = [], set(), [], set(), None
    for j in range(tiles):
        for i in range(tiles):
            ts, tn = s + (n - s) * j / tiles, s + (n - s) * (j + 1) / tiles
            tw, te = w + (e - w) * i / tiles, w + (e - w) * (i + 1) / tiles
            q = f'[out:json][timeout:120];(way["building"]({ts:.6f},{tw:.6f},{tn:.6f},{te:.6f});relation["building"]({ts:.6f},{tw:.6f},{tn:.6f},{te:.6f}););out body geom;'
            raw, mirror, errors = overpass(q, need_elements=False)
            if raw is None:
                raise RuntimeError(f"[osm] {tag} 칸 {i},{j} 실패:\n  " + "\n  ".join(errors))
            doc = json.loads(raw)
            base = base or doc.get("osm3s", {}).get("timestamp_osm_base")
            k0 = len(elems)
            for el in doc.get("elements", []):
                key = (el.get("type"), el.get("id"))
                if key not in seen:
                    seen.add(key); elems.append(el)
            queries.append(q); mirrors.add(mirror)
            print(f"[osm] {tag} 칸 {i},{j}: 새 요소 {len(elems) - k0}")
            time.sleep(2)
    if not elems:
        raise RuntimeError(f"[osm] {tag}: 건물 0개")
    doc = {"version": 0.6, "generator": "fetch_sources.py tiled merge", "osm3s": {"timestamp_osm_base": base, "copyright": "© OpenStreetMap contributors, ODbL"}, "elements": elems}
    raw = json.dumps(doc, ensure_ascii=False).encode("utf-8")
    with open(out, "wb") as f:
        f.write(raw)
    meta = {"box_swne": box, "tiles": f"{tiles}x{tiles}", "queries": queries, "mirrors": sorted(mirrors), "fetched": time.strftime("%Y-%m-%dT%H:%M:%S%z"),
            "osm_base": base, "ways": sum(1 for x in elems if x["type"] == "way"), "relations": sum(1 for x in elems if x["type"] == "relation"), "bytes": len(raw),
            "license": "© OpenStreetMap contributors, ODbL"}
    with open(out.replace(".json", ".meta.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, ensure_ascii=False, indent=1)
    print(f"[osm] {tag}: way {meta['ways']} · relation {meta['relations']} · {len(raw)} bytes")
    return out


def fetch_dem():
    out = os.path.join(DEM_DIR, f"{DEM_TILE}.tif")
    if os.path.exists(out) and not FORCE:
        print(f"[dem] 캐시 있음 {os.path.basename(out)} ({os.path.getsize(out)} bytes) — 건너뜀")
    else:
        status, raw = get(DEM_URL, timeout=600)
        with open(out, "wb") as f:
            f.write(raw)
        with open(out.replace(".tif", ".meta.json"), "w", encoding="utf-8") as f:
            json.dump({"url": DEM_URL, "fetched": time.strftime("%Y-%m-%dT%H:%M:%S%z"), "bytes": len(raw)}, f, indent=1)
        print(f"[dem] {DEM_TILE}.tif {len(raw)} bytes")
    for fname, url in DEM_DOC_URLS.items():
        p = os.path.join(DEM_DIR, fname)
        if os.path.exists(p) and not FORCE:
            continue
        try:
            _, raw = get(url, timeout=60)
            with open(p, "wb") as f:
                f.write(raw)
            print(f"[dem] 문서 {fname} {len(raw)} bytes")
        except Exception as ex:
            print(f"[dem] 문서 {fname} 실패: {ex}")
    return out


if __name__ == "__main__":
    os.makedirs(OSM_DIR, exist_ok=True)
    os.makedirs(DEM_DIR, exist_ok=True)
    failed = False
    for name, box in BOXES.items():
        try:
            fetch_osm(name, box)
        except Exception as ex:
            print(ex)
            failed = True
        time.sleep(2)  # Overpass 예의
    try:
        fetch_osm("structure_a", BOXES["structure_a"], "buildings")   # 조각 10(D-114): 실제 건물 윤곽
    except Exception as ex:
        print(ex)
        failed = True
    try:
        fetch_dem()
    except Exception as ex:
        print(f"[dem] 실패: {ex}")
        failed = True
    sys.exit(1 if failed else 0)
