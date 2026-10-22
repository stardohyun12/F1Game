# -*- coding: utf-8 -*-
"""Copernicus DEM GLO-30 COG 를 GDAL 없이 읽어 상자별로 잘라 JSON 으로 쓴다 (E1).

COG 형식(실측 2026-10-19): float32 1 밴드, 1024x1024 타일, Compression=8(DEFLATE),
Predictor=3(부동소수 예측자), 픽셀 1/3600 도, 왼쪽 위 (14E, 41N).
→ zlib 로 풀고, 예측자 3을 되돌린다(행마다 바이트 누적합 → 바이트 평면 4장 → 빅엔디언 float32).

사용:  py -X utf8 tools/town-gen/dem_crop.py
출력:  tools/town-gen/dem/crop_<box>.json
  { "box_swne": [...], "lat0": 북쪽 행 중심 위도, "lon0": 서쪽 열 중심 경도, "dlat", "dlon" (도),
    "w", "h", "rows_north_to_south": true, "heights_m": [h*w] }
표기: produced using Copernicus WorldDEM-30 ... (dem/ATTRIBUTION.md)
"""
import json
import os
import struct
import sys
import zlib

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from fetch_sources import BOXES, DEM_TILE  # noqa: E402

TIF = os.path.join(HERE, "dem", f"{DEM_TILE}.tif")


def read_ifd(b):
    bo = "<" if b[:2] == b"II" else ">"
    off = struct.unpack(bo + "I", b[4:8])[0]
    n = struct.unpack(bo + "H", b[off:off + 2])[0]
    size = {1: 1, 2: 1, 3: 2, 4: 4, 5: 8, 11: 4, 12: 8, 16: 8}
    fmt = {3: "H", 4: "I", 12: "d", 16: "Q"}
    tags = {}
    for i in range(n):
        e = b[off + 2 + i * 12: off + 14 + i * 12]
        tag, typ, cnt = struct.unpack(bo + "HHI", e[:8])
        sz = size.get(typ, 1) * cnt
        raw = e[8:8 + sz] if sz <= 4 else b[struct.unpack(bo + "I", e[8:12])[0]:][:sz]
        tags[tag] = struct.unpack(bo + fmt[typ] * cnt, raw) if typ in fmt else raw
    return bo, tags


def decode_tile(b, off, cnt, tw, th):
    raw = np.frombuffer(zlib.decompress(b[off:off + cnt]), dtype=np.uint8).reshape(th, tw * 4)
    raw = np.cumsum(raw, axis=1, dtype=np.uint8)  # 바이트 수평 차분 되돌리기 (mod 256)
    planes = raw.reshape(th, 4, tw).transpose(0, 2, 1)  # (행, 열, 바이트) — 평면 0 이 최상위 바이트
    return np.ascontiguousarray(planes).view(">f4").reshape(th, tw).astype(np.float32)


def main():
    b = open(TIF, "rb").read()
    bo, t = read_ifd(b)
    assert t[259][0] == 8 and t[317][0] == 3 and t[339][0] == 3 and t[258][0] == 32, "예상과 다른 COG 형식"
    W, H, tw, th = t[256][0], t[257][0], t[322][0], t[323][0]
    offs, cnts = t[324], t[325]
    sx, sy = t[33550][0], t[33550][1]
    lon_left, lat_top = t[33922][3], t[33922][4]  # 픽셀 (0,0) 의 왼쪽 위 모서리 (PixelIsArea 가정)
    tiles_x = (W + tw - 1) // tw
    cache = {}

    def tile(tx, ty):
        k = (tx, ty)
        if k not in cache:
            i = ty * tiles_x + tx
            cache[k] = decode_tile(b, offs[i], cnts[i], tw, th)
        return cache[k]

    only = [a for a in sys.argv[1:] if not a.startswith("-")]   # 이름을 주면 그 상자만(기존 crop 파일을 다시 쓰지 않게)
    for name, (s, w, n, e) in BOXES.items():
        if only and name not in only:
            continue
        c0 = int((w - lon_left) / sx) - 1
        c1 = int((e - lon_left) / sx) + 2
        r0 = int((lat_top - n) / sy) - 1
        r1 = int((lat_top - s) / sy) + 2
        out = np.zeros((r1 - r0, c1 - c0), np.float32)
        for r in range(r0, r1):
            for c in range(c0, c1):
                out[r - r0, c - c0] = tile(c // tw, r // th)[r % th, c % tw]
        doc = {
            "box_swne": [s, w, n, e],
            "source": f"{DEM_TILE}.tif",
            "lat0": lat_top - (r0 + 0.5) * sy, "lon0": lon_left + (c0 + 0.5) * sx,
            "dlat": sy, "dlon": sx, "w": int(out.shape[1]), "h": int(out.shape[0]),
            "rows_north_to_south": True,
            "attribution": "produced using Copernicus WorldDEM-30 © DLR e.V. 2010-2014 and © Airbus Defence and Space GmbH 2014-2018 provided under COPERNICUS by the European Union and ESA; all rights reserved",
            "heights_m": [round(float(v), 2) for v in out.ravel()],
        }
        p = os.path.join(HERE, "dem", f"crop_{name}.json")
        with open(p, "w", encoding="utf-8") as f:
            json.dump(doc, f, ensure_ascii=False)
        print(f"[crop] {name}: {out.shape[1]}x{out.shape[0]} px, min {out.min():.1f} m, max {out.max():.1f} m, mean {out.mean():.1f} m -> {os.path.basename(p)}")


if __name__ == "__main__":
    main()
