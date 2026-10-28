#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Map_TownV6 회색 박스 → glTF(.glb) — Unity 에디터 없이 평면 json 에서 바로 뽑는다 (기획 뷰어 /plan/ 의 3D 바탕).

    python -X utf8 tools/viewer/map/export_map.py [--plan docs/map/town-v6b.json] [--out tools/viewer/assets/map_townv6.glb]
                                                 [--step 3] [--no-scene]

F1/Assets/Editor/BuildBayLoop.Gray.cs 의 가벼운 재현(순수 python, numpy 만):
  지형   controlPoints(8 m) 쌍선형 → --step m 격자, 3×3 평균 2회, 차길 어깨(가장자리 +3 m 노면 높이, 그 밖 5 m 녹임), 걷는 길은 묻히지 않게
  바다   y=0 평면 1600×1600
  길     knots [x,z,y] 꺾은선 → 띠(폭 = json width, 들림 main 6 cm · 차길 4 cm · 걷는 길 2 cm) + 양옆 0.6 m 치마. 종류별로 한 메시(ROAD_Main/Village/Stone/Stairs)
         + 빌더 DeadEnds ① 과 같은 이음(끝이 3 m 밖·25 m 안 다른 차길) 7개
  건물   OSM 폴리곤 압출. 층수 = 2 + ((osm_way·2654435761)&0x7fffffff)%3 (빌더와 같은 해시), y0 = min(y, 지형)-1, y1 = y+층·3. 건물마다 노드 B_<osm_way>
  부두   sea.piers 꺾은선 → 3.5 m 폭 상자
  자리   (--no-scene 아니면) Map_TownV6.unity 를 읽기 전용으로 훑어 Q_*/Q<n>_*/TUT_* 빈 상자(위치·스케일)를 PLACE_<이름> 노드로. 씬은 쓰지 않는다.

좌표(중요): Unity 는 왼손(x 동, y 위, z 북), glTF/three.js 는 오른손 Y-up.  glTF (x, y, z) = Unity (x, y, −z).
  → 뷰어에서 YAML `world: [x, z]` 는 three.js (x, h, −z) 에 놓는다 (plan.js `W2T`). 삼각형 감김은 z 반전에 맞춰 바깥면이 앞면(CCW)이 되게 쓴다.
  노드 extras 에 unity_pos 를 그대로 넣어 둔다(검사용).
"""
import argparse, json, math, os, re, struct, sys, time
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
MATS = {  # name: (base color rgb 0-1, metallic, roughness)
    "Terrain": (0.62, 0.66, 0.48), "Sea": (0.36, 0.58, 0.70),
    "Main": (0.40, 0.40, 0.42), "Village": (0.56, 0.56, 0.55), "Stone": (0.60, 0.48, 0.30), "Stairs": (0.80, 0.42, 0.16),
    "Wall": (0.78, 0.77, 0.74), "Roof": (0.62, 0.58, 0.54), "Pier": (0.55, 0.55, 0.55), "Place": (0.95, 0.60, 0.20),
}
ROAD_LIFT = {"main": 0.06, "village": 0.04, "alley": 0.02, "stairs": 0.02}


def F(v): return float(v)


# ---------------------------------------------------------------- terrain
class Heights:
    def __init__(self, terr):
        cps = terr["controlPoints"]; self.step = F(terr["step"])
        self.n = int(round(math.sqrt(len(cps)))); self.x0 = F(cps[0][0])
        h = np.zeros((self.n, self.n), np.float64)
        for p in cps:
            i = int(round((F(p[0]) - self.x0) / self.step)); j = int(round((F(p[1]) - self.x0) / self.step)); h[i, j] = F(p[2])
        self.h = h

    def at(self, x, z):  # 쌍선형 (numpy 배열 가능)
        fx = np.clip((np.asarray(x, np.float64) - self.x0) / self.step, 0, self.n - 1.001)
        fz = np.clip((np.asarray(z, np.float64) - self.x0) / self.step, 0, self.n - 1.001)
        i = fx.astype(int); j = fz.astype(int); u = fx - i; v = fz - j
        a = self.h[i, j]; b = self.h[i + 1, j]; c = self.h[i, j + 1]; d = self.h[i + 1, j + 1]
        return (a * (1 - u) + b * u) * (1 - v) + (c * (1 - u) + d * u) * v


def build_terrain(cp, roads, step, half=392.0):
    n = int(round(2 * half / step)) + 1; x0 = -half
    xs = x0 + np.arange(n) * step
    X, Z = np.meshgrid(xs, xs, indexing="ij")
    H = cp.at(X, Z)
    for _ in range(2):  # 3×3 평균(빌더는 2 m 격자 4회 ≈ ±8 m; 3 m 격자면 2회)
        P = np.pad(H, 1, mode="edge")
        H = sum(P[1 + di:1 + di + n, 1 + dj:1 + dj + n] for di in (-1, 0, 1) for dj in (-1, 0, 1)) / 9.0
    # 차길 어깨: 가장자리 +3 m 까지 노면(−4.5 cm), 그 밖 5 m 녹임
    shoulder, blend = 3.0, 5.0
    best = np.full((n, n), 1e9); bestY = np.zeros((n, n))
    for r in roads:
        if not r["car"]: continue
        hw = r["width"] * 0.5; reach = hw + shoulder + blend
        for (px, pz, py) in r["pts"]:
            i0 = max(0, int(math.floor((px - reach - x0) / step))); i1 = min(n - 1, int(math.ceil((px + reach - x0) / step)))
            j0 = max(0, int(math.floor((pz - reach - x0) / step))); j1 = min(n - 1, int(math.ceil((pz + reach - x0) / step)))
            if i1 < i0 or j1 < j0: continue
            sx = xs[i0:i1 + 1][:, None] - px; sz = xs[j0:j1 + 1][None, :] - pz
            e = np.sqrt(sx * sx + sz * sz) - hw
            sub = best[i0:i1 + 1, j0:j1 + 1]
            m = (e <= shoulder + blend) & (e < sub)
            sub[m] = e[m]; bestY[i0:i1 + 1, j0:j1 + 1][m] = py + 0.04 - 0.045
    has = best < 1e8
    t = np.clip((best - shoulder) / blend, 0, 1); t = t * t * (3 - 2 * t)
    H = np.where(has, bestY * (1 - t) + H * t, H)
    carved = has & (best <= shoulder)
    for r in roads:  # 걷는 길은 지형 위로 묻히지 않게만
        if r["car"]: continue
        for (px, pz, py) in r["pts"]:
            i = int(round((px - x0) / step)); j = int(round((pz - x0) / step))
            if 0 <= i < n and 0 <= j < n and not carved[i, j]: H[i, j] = min(H[i, j], py - 0.1)
    V = np.stack([X.ravel(), H.ravel(), Z.ravel()], 1)
    I, J = np.meshgrid(np.arange(n - 1), np.arange(n - 1), indexing="ij")
    a = (I * n + J).ravel(); b = ((I + 1) * n + J).ravel(); c = (I * n + J + 1).ravel(); d = ((I + 1) * n + J + 1).ravel()
    T = np.stack([a, c, b, b, c, d], 1).reshape(-1, 3)  # Unity 와 같은 순서(위에서 볼 때 CW = Unity 앞면); prim() 이 z 반전과 함께 감김을 바꾼다
    return V, T, H, xs


# ---------------------------------------------------------------- roads
def load_roads(plan):
    out = []
    gone = set(plan.get("removed", {}).get("roads", []))  # D-198: 씬에서 끈 길(건물 밑)
    for r in plan["roads"]:
        if r.get("id") in gone: continue
        ks =[(F(k[0]), F(k[1]), F(k[2])) for k in r["knots"]]
        pts = []
        for i in range(len(ks) - 1):  # 1 m 표본(어깨 깎기용)
            a, b = ks[i], ks[i + 1]; L = math.hypot(b[0] - a[0], b[1] - a[1]); nseg = max(1, int(math.ceil(L)))
            for s in range(nseg):
                t = s / nseg; pts.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t))
        if ks: pts.append(ks[-1])
        if len(pts) < 2: continue
        kind = r["kind"]
        out.append({"id": r["id"], "kind": kind, "width": F(r["width"]), "rough": F(r.get("roughness", 0)), "pts": pts,
                    "car": kind in ("main", "village")})
    return out


def dead_end_joins(roads):
    """빌더 DeadEnds ①: 차길 끝이 다른 차길 3 m 밖·25 m 안이면 잇는 길."""
    cars = [r for r in roads if r["car"]]
    allpts = np.array([p for r in cars for p in r["pts"]]); owner = np.array([k for k, r in enumerate(cars) for _ in r["pts"]])
    add = []
    for k, r in enumerate(cars):
        for e in (0, 1):
            p = r["pts"][0] if e == 0 else r["pts"][-1]
            if abs(p[0]) > 372 or abs(p[1]) > 372: continue
            d = np.hypot(allpts[:, 0] - p[0], allpts[:, 1] - p[1])
            own = owner == k
            idx = np.arange(len(d)); kstart = int(np.argmax(own)); kend = kstart + len(r["pts"])
            skip = own & ((idx < kstart + 12) if e == 0 else (idx > kend - 1 - 12))
            d = np.where(skip, 1e9, d)
            bi = int(np.argmin(d)); bd = d[bi]
            if bd <= 3.0 or bd > 25.0 or owner[bi] == k: continue
            q = tuple(allpts[bi]); n = max(2, int(math.ceil(bd)))
            pts = [(p[0] + (q[0] - p[0]) * s / n, p[1] + (q[1] - p[1]) * s / n, p[2] + (q[2] - p[2]) * s / n) for s in range(n + 1)]
            add.append({"id": "join_%s_%d" % (r["id"], e), "kind": "village", "width": min(r["width"], cars[owner[bi]]["width"]), "rough": 0.0, "pts": pts, "car": True})
    roads.extend(add); return len(add)


def thin(pts, dmin):
    q = [pts[0]]
    for p in pts[1:-1]:
        if math.hypot(p[0] - q[-1][0], p[1] - q[-1][1]) >= dmin: q.append(p)
    q.append(pts[-1]); return q


def ribbon(pts, width, lift, skirt=0.6):
    """띠 + 치마(양면). 정점 (x,y,z) Unity 좌표, 삼각형은 Unity 감김(빌더 Ribbon 과 같음)."""
    V = []; T = []; h = width * 0.5; n = len(pts)
    for i in range(n):
        a = pts[max(i - 1, 0)]; b = pts[min(i + 1, n - 1)]
        fx, fz = b[0] - a[0], b[1] - a[1]; L = math.hypot(fx, fz) or 1.0; fx /= L; fz /= L
        rx, rz = fz, -fx; c = (pts[i][0], pts[i][2] + lift, pts[i][1])
        V.append((c[0] - rx * h, c[1], c[2] - rz * h)); V.append((c[0] + rx * h, c[1], c[2] + rz * h))
        if i > 0:
            a0 = (i - 1) * 2; T += [(a0, a0 + 2, a0 + 1), (a0 + 1, a0 + 2, a0 + 3)]
    b0 = len(V)
    for i in range(n):
        for s in (0, 1):
            v = V[i * 2 + s]; V.append(v); V.append((v[0], v[1] - skirt, v[2]))
    for i in range(1, n):
        a0 = b0 + (i - 1) * 4; b1 = b0 + i * 4
        for s0 in (0, 2):
            T += [(a0 + s0, a0 + s0 + 1, b1 + s0), (b1 + s0, a0 + s0 + 1, b1 + s0 + 1), (a0 + s0, b1 + s0, a0 + s0 + 1), (a0 + s0 + 1, b1 + s0, b1 + s0 + 1)]
    return V, T


# ---------------------------------------------------------------- buildings
def area2(p):
    return 0.5 * sum(p[i][0] * p[(i + 1) % len(p)][1] - p[(i + 1) % len(p)][0] * p[i][1] for i in range(len(p)))


def in_tri(p, a, b, c):
    d1 = (p[0] - b[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (p[1] - b[1]); d2 = (p[0] - c[0]) * (b[1] - c[1]) - (b[0] - c[0]) * (p[1] - c[1]); d3 = (p[0] - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (p[1] - a[1])
    return not (((d1 < 0) or (d2 < 0) or (d3 < 0)) and ((d1 > 0) or (d2 > 0) or (d3 > 0)))


def ear_clip(p):  # p 반시계(x,z 평면, 위에서). 빌더 EarClip 과 같음
    idx = list(range(len(p))); out = []; guard = 0
    while len(idx) > 3 and guard < 4 * len(p):
        guard += 1; cut = False
        for k in range(len(idx)):
            i0, i1, i2 = idx[k - 1], idx[k], idx[(k + 1) % len(idx)]; a, b, c = p[i0], p[i1], p[i2]
            if (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]) <= 0: continue
            if any(in_tri(p[j], a, b, c) for j in idx if j not in (i0, i1, i2)): continue
            out.append((i0, i1, i2)); idx.pop(k); cut = True; break
        if not cut: break
    if len(idx) == 3: out.append((idx[0], idx[1], idx[2]))
    else: out += [(idx[0], idx[k], idx[k + 1]) for k in range(1, len(idx) - 1)]
    return out


def building_mesh(poly, y0, y1):
    """poly 반시계(x,z). 벽은 바깥면, 지붕은 윗면이 앞면(Unity 감김)."""
    WV = []; WT = []; RV = []; RT = []
    n = len(poly)
    for i in range(n):
        a = poly[i]; c = poly[(i + 1) % n]; s = len(WV)
        WV += [(a[0], y0, a[1]), (c[0], y0, c[1]), (a[0], y1, a[1]), (c[0], y1, c[1])]
        WT += [(s, s + 1, s + 2), (s + 1, s + 3, s + 2)]
    for p in poly: RV.append((p[0], y1, p[1]))
    for (a, b, c) in ear_clip(poly): RT.append((a, c, b))
    return WV, WT, RV, RT


def box(cx, cy, cz, sx, sy, sz, yaw_deg=0.0):
    hx, hy, hz = sx / 2, sy / 2, sz / 2; cy_, sy_ = math.cos(math.radians(yaw_deg)), math.sin(math.radians(yaw_deg))
    def R(x, z): return (cx + x * cy_ + z * sy_, cz - x * sy_ + z * cy_)  # Unity yaw(+y 축, 왼손): x' = x cos + z sin, z' = -x sin + z cos
    V = []; T = []
    faces = [((-1, 0, 0), (0, 0, 1), (0, 1, 0)), ((1, 0, 0), (0, 0, -1), (0, 1, 0)), ((0, 0, -1), (1, 0, 0), (0, 1, 0)), ((0, 0, 1), (-1, 0, 0), (0, 1, 0)),
             ((0, 1, 0), (1, 0, 0), (0, 0, 1)), ((0, -1, 0), (1, 0, 0), (0, 0, -1))]
    for nrm, u, w in faces:
        s = len(V)
        for du, dw in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
            x = nrm[0] * hx + u[0] * hx * du + w[0] * hx * dw; y = nrm[1] * hy + u[1] * hy * du + w[1] * hy * dw; z = nrm[2] * hz + u[2] * hz * du + w[2] * hz * dw
            X, Z = R(x, z); V.append((X, cy + y, Z))
        # Unity 앞면 = 대수적 외적(오른손)이 바깥을 향함 → 아니면 뒤집는다
        p0, p1, p2 = (np.array(V[s + k]) for k in (0, 1, 2)); c = np.cross(p1 - p0, p2 - p0)
        n_rot = np.array([*R(nrm[0], nrm[2])]) - np.array([cx, cz]); nn = np.array([n_rot[0], nrm[1], n_rot[1]])
        if np.dot(c, nn) >= 0: T += [(s, s + 1, s + 2), (s + 1, s + 3, s + 2)]
        else: T += [(s, s + 2, s + 1), (s + 1, s + 2, s + 3)]
    return V, T


# ---------------------------------------------------------------- Unity 씬의 자리 상자(읽기 전용)
def scene_places(scene_path):
    """GameObject(Q_*/Q<n>_*/TUT_*) → Transform(localPosition·scale·rotation). 부모가 원점이 아닌 경우는 부모 위치를 더한다(1단)."""
    if not os.path.isfile(scene_path): return []
    txt = open(scene_path, encoding="utf-8", errors="ignore").read()
    docs = re.split(r"\n--- !u!", txt)
    gos = {}; trs = {}; tr_by_go = {}
    for d in docs:
        m = re.match(r"(\d+) &(\d+)", d)
        if not m: continue
        cls, fid = int(m.group(1)), m.group(2)
        if cls == 1:
            nm = re.search(r"\n  m_Name: (.*)", d)
            if nm: gos[fid] = nm.group(1).strip()
        elif cls == 4:
            go = re.search(r"m_GameObject: \{fileID: (\d+)\}", d); pos = re.search(r"m_LocalPosition: \{x: ([-\d.e]+), y: ([-\d.e]+), z: ([-\d.e]+)\}", d)
            sc = re.search(r"m_LocalScale: \{x: ([-\d.e]+), y: ([-\d.e]+), z: ([-\d.e]+)\}", d); rot = re.search(r"m_LocalRotation: \{x: ([-\d.e]+), y: ([-\d.e]+), z: ([-\d.e]+), w: ([-\d.e]+)\}", d)
            par = re.search(r"m_Father: \{fileID: (\d+)\}", d)
            if go and pos:
                trs[fid] = {"go": go.group(1), "pos": [float(v) for v in pos.groups()], "scale": [float(v) for v in sc.groups()] if sc else [1, 1, 1],
                            "rot": [float(v) for v in rot.groups()] if rot else [0, 0, 0, 1], "father": par.group(1) if par else "0"}
                tr_by_go[go.group(1)] = fid
    out = []
    for fid, t in trs.items():
        name = gos.get(t["go"], "")
        if not re.match(r"(Q\d*_|TUT_)", name): continue
        p = list(t["pos"]); f = t["father"]; depth = 0
        while f in trs and depth < 4:
            fp = trs[f]["pos"]; p = [p[0] + fp[0], p[1] + fp[1], p[2] + fp[2]]; f = trs[f]["father"]; depth += 1
        qx, qy, qz, qw = t["rot"]; yaw = math.degrees(math.atan2(2 * (qw * qy + qx * qz), 1 - 2 * (qy * qy + qz * qz)))
        out.append({"name": name, "pos": p, "scale": t["scale"], "yaw": yaw})
    return out


# ---------------------------------------------------------------- glTF 쓰기
class Glb:
    def __init__(self):
        self.bin = bytearray(); self.views = []; self.acc = []; self.meshes = []; self.nodes = []; self.mats = []; self.matIdx = {}

    def mat(self, name):
        if name not in self.matIdx:
            c = MATS[name]; self.matIdx[name] = len(self.mats)
            self.mats.append({"name": name, "pbrMetallicRoughness": {"baseColorFactor": [c[0], c[1], c[2], 1.0], "metallicFactor": 0.0, "roughnessFactor": 1.0}, "doubleSided": False})
        return self.matIdx[name]

    def _view(self, data, target):
        while len(self.bin) % 4: self.bin.append(0)
        off = len(self.bin); self.bin.extend(data)
        self.views.append({"buffer": 0, "byteOffset": off, "byteLength": len(data), "target": target}); return len(self.views) - 1

    def prim(self, V, T, mat_name):
        """V: (n,3) Unity 좌표 → glTF (x,y,−z); T: (m,3) Unity 감김(CW 앞면) → 감김 뒤집어 CCW. 법선 = 면 법선 평균(정점 공유)."""
        V = np.asarray(V, np.float32).reshape(-1, 3).copy(); T = np.asarray(T, np.uint32).reshape(-1, 3)
        V[:, 2] *= -1
        T = T[:, [0, 2, 1]]  # 거울(z 반전)은 감김을 뒤집는다 → Unity 앞면(CW) 이 glTF 앞면(CCW) 이 되게 되돌림
        fn = np.cross(V[T[:, 1]] - V[T[:, 0]], V[T[:, 2]] - V[T[:, 0]])
        N = np.zeros_like(V)
        for k in range(3): np.add.at(N, T[:, k], fn)
        ln = np.linalg.norm(N, axis=1); ln[ln == 0] = 1; N = (N / ln[:, None]).astype(np.float32)
        pv = self._view(V.tobytes(), 34962); nv = self._view(N.tobytes(), 34962)
        use16 = len(V) < 65535
        I = T.astype(np.uint16 if use16 else np.uint32).ravel(); iv = self._view(I.tobytes(), 34963)
        self.acc.append({"bufferView": pv, "componentType": 5126, "count": len(V), "type": "VEC3", "min": V.min(0).tolist(), "max": V.max(0).tolist()}); pa = len(self.acc) - 1
        self.acc.append({"bufferView": nv, "componentType": 5126, "count": len(V), "type": "VEC3"}); na = len(self.acc) - 1
        self.acc.append({"bufferView": iv, "componentType": 5123 if use16 else 5125, "count": len(I), "type": "SCALAR"}); ia = len(self.acc) - 1
        return {"attributes": {"POSITION": pa, "NORMAL": na}, "indices": ia, "material": self.mat(mat_name)}, len(T)

    def node(self, name, prims, extras=None):
        self.meshes.append({"name": name, "primitives": prims}); n = {"name": name, "mesh": len(self.meshes) - 1}
        if extras: n["extras"] = extras
        self.nodes.append(n)

    def write(self, path, extras=None):
        while len(self.bin) % 4: self.bin.append(0)
        g = {"asset": {"version": "2.0", "generator": "export_map.py (PitCrew)"}, "scene": 0, "scenes": [{"nodes": list(range(len(self.nodes)))}],
             "nodes": self.nodes, "meshes": self.meshes, "materials": self.mats, "accessors": self.acc, "bufferViews": self.views, "buffers": [{"byteLength": len(self.bin)}]}
        if extras: g["extras"] = extras
        js = json.dumps(g, separators=(",", ":")).encode("utf-8")
        while len(js) % 4: js += b" "
        total = 12 + 8 + len(js) + 8 + len(self.bin)
        with open(path, "wb") as f:
            f.write(struct.pack("<III", 0x46546C67, 2, total)); f.write(struct.pack("<II", len(js), 0x4E4F534A)); f.write(js)
            f.write(struct.pack("<II", len(self.bin), 0x004E4942)); f.write(self.bin)
        return total


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--plan", default=os.path.join(REPO, "docs/map/town-v6b.json"))  # Map_TownV6.unity 는 v6b 로 지어졌다(ROAD_ 이름 대조 450=443+join 7)
    ap.add_argument("--out", default=os.path.join(REPO, "tools/viewer/assets/map_townv6.glb"))
    ap.add_argument("--scene", default=os.path.join(REPO, "F1/Assets/Scenes/Map_TownV6.unity"))
    ap.add_argument("--step", type=float, default=3.0)
    ap.add_argument("--no-scene", action="store_true")
    a = ap.parse_args(); t0 = time.time()
    plan = json.load(open(a.plan, encoding="utf-8"))
    cp = Heights(plan["terrain"]); roads = load_roads(plan); nj = dead_end_joins(roads)
    g = Glb(); stats = {}
    # 지형
    V, T, H, xs = build_terrain(cp, roads, a.step)
    p, nt = g.prim(V, T, "Terrain"); g.node("TV6_Terrain", [p]); stats["terrain_tris"] = nt
    # 바다
    p, nt = g.prim([(-800, 0, -800), (800, 0, -800), (-800, 0, 800), (800, 0, 800)], [(0, 2, 1), (1, 2, 3)], "Sea"); g.node("Sea", [p]); stats["sea_tris"] = nt
    # 길: 종류별 한 메시
    groups = {"Main": [], "Village": [], "Stone": [], "Stairs": []}; carM = 0.0
    for r in roads:
        rough = r["kind"] == "alley" or (r["kind"] == "village" and r["rough"] > 0)
        key = "Stairs" if r["kind"] == "stairs" else "Main" if r["kind"] == "main" else "Stone" if rough else "Village"
        pts = thin(r["pts"], 2.0 if r["car"] else 3.0)
        groups[key].append((r["id"], ribbon(pts, r["width"], ROAD_LIFT.get(r["kind"], 0.02))))
        if r["car"]: carM += sum(math.hypot(r["pts"][i][0] - r["pts"][i - 1][0], r["pts"][i][1] - r["pts"][i - 1][1]) for i in range(1, len(r["pts"])))
    stats["road_tris"] = 0
    for key, lst in groups.items():
        if not lst: continue
        VV = []; TT = []; ids = []
        for rid, (v, t) in lst:
            o = len(VV); VV += v; TT += [(x + o, y + o, z + o) for (x, y, z) in t]; ids.append(rid)
        p, nt = g.prim(VV, TT, key); g.node("ROAD_" + key, [p], {"roads": ids}); stats["road_tris"] += nt
    stats["roads"] = len(roads); stats["joins"] = nj; stats["car_km"] = round(carM / 1000, 2)
    # 건물
    nb = 0; stats["building_tris"] = 0
    gone_b = set(plan.get("removed", {}).get("osm_ways", []))  # D-198: 새 건물 자리와 겹쳐 지운 OSM 건물
    for b in plan["buildings"]:
        if b.get("osm_way") in gone_b: continue
        poly = [(F(q[0]), F(q[1])) for q in b["poly"]]
        if len(poly) > 1 and poly[0] == poly[-1]: poly.pop()
        if len(poly) < 3: continue
        cx = sum(q[0] for q in poly) / len(poly); cz = sum(q[1] for q in poly) / len(poly)
        if abs(cx) > 390 or abs(cz) > 390: continue
        if area2(poly) < 0: poly.reverse()
        gy = F(b["y"]); minG = float(np.min(cp.at([q[0] for q in poly], [q[1] for q in poly])))
        way = int(b.get("osm_way", nb)); floors = 2 + ((way * 2654435761) & 0x7fffffff) % 3
        y0 = min(gy, minG) - 1.0; y1 = gy + floors * 3.0
        WV, WT, RV, RT = building_mesh(poly, y0, y1)
        pw, n1 = g.prim(WV, WT, "Wall"); pr, n2 = g.prim(RV, RT, "Roof")
        g.node("B_%d" % way, [pw, pr], {"osm_way": way, "floors": floors, "base_y": round(y0, 2), "top_y": round(y1, 2), "unity_pos": [round(cx, 1), round(y1, 1), round(cz, 1)]})
        nb += 1; stats["building_tris"] += n1 + n2
    stats["buildings"] = nb
    # 부두
    PV = []; PT = []; npier = 0
    for line in plan.get("sea", {}).get("piers", []):
        for i in range(len(line) - 1):
            ax, az = F(line[i][0]), F(line[i][1]); bx, bz = F(line[i + 1][0]), F(line[i + 1][1]); L = math.hypot(bx - ax, bz - az)
            if L < 0.3: continue
            yaw = math.degrees(math.atan2(bx - ax, bz - az))
            v, t = box((ax + bx) / 2, -0.35, (az + bz) / 2, 3.5, 3.3, L + 1.5, yaw); o = len(PV); PV += v; PT += [(x + o, y + o, z + o) for (x, y, z) in t]; npier += 1
    if PV: p, nt = g.prim(PV, PT, "Pier"); g.node("Piers", [p]); stats["pier_tris"] = nt
    stats["piers"] = npier
    # Unity 씬의 자리 상자(Q_*, TUT_*) — 읽기만
    places = [] if a.no_scene else scene_places(a.scene)
    for pl in places:
        s = pl["scale"]; sx, sy, sz = max(0.5, abs(s[0])), max(0.5, abs(s[1])), max(0.5, abs(s[2]))
        if sx > 60 or sz > 60: sx, sy, sz = 2, 2, 2  # 큰 트리거 볼륨은 표식만
        v, t = box(pl["pos"][0], pl["pos"][1], pl["pos"][2], sx, sy, sz, pl["yaw"])
        p, nt = g.prim(v, t, "Place"); g.node("PLACE_" + pl["name"], [p], {"unity_pos": [round(c, 2) for c in pl["pos"]], "scale": s, "yaw": round(pl["yaw"], 1)})
    stats["places"] = len(places)
    stats["tris_total"] = sum(v for k, v in stats.items() if k.endswith("_tris"))
    extras = {"source": os.path.relpath(a.plan, REPO).replace("\\", "/"), "axis": "glTF(x,y,z) = Unity(x,y,-z); YAML world [x,z] -> three (x, h, -z)",
              "terrain_step_m": a.step, "stats": stats, "generated": time.strftime("%Y-%m-%dT%H:%M:%S")}
    os.makedirs(os.path.dirname(a.out), exist_ok=True)
    size = g.write(a.out, extras)
    stats["bytes"] = size; stats["seconds"] = round(time.time() - t0, 1)
    json.dump(extras, open(os.path.splitext(a.out)[0] + ".meta.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(json.dumps(stats, ensure_ascii=False))


if __name__ == "__main__":
    main()
