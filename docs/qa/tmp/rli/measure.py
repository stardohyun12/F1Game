"""방 바닥+벽 픽셀(mask.json) 평균 밝기 L·색. 사용: python measure.py <png> [...]  → JSON 한 줄씩"""
import json, sys
from PIL import Image

m = json.load(open("D:/Github/F1Game/docs/qa/tmp/rli/mask.json"))
lampR = m["lampR"] or 0
for p in sys.argv[1:]:
    im = Image.open(p).convert("RGB"); W, H = im.size; px = im.load()
    acc = {"all": [], "out": [], "wall": [], "win": []}
    for u, v, face, ld, win in m["pts"]:
        r, g, b = px[min(W - 1, int(u * W)), min(H - 1, int((1 - v) * H))]
        c = (r / 255, g / 255, b / 255)
        acc["all"].append(c)
        if ld > lampR: acc["out"].append(c)          # 전등 웅덩이 밖
        if face != 0: acc["wall"].append(c)
        if win: acc["win"].append(c)
    L = lambda c: 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]
    def mean(cs):
        if not cs: return None
        n = len(cs); r = sum(c[0] for c in cs) / n; g = sum(c[1] for c in cs) / n; b = sum(c[2] for c in cs) / n
        return {"n": n, "L": round(L((r, g, b)), 3), "rgb": [round(r, 3), round(g, 3), round(b, 3)], "R/B": round(r / max(b, 1e-3), 2)}
    out = {"png": p.split("/")[-1], "all": mean(acc["all"]), "outLamp": mean(acc["out"]),
           "blackWall%": round(100 * sum(1 for c in acc["wall"] if L(c) < 0.05) / max(1, len(acc["wall"])), 1),
           "winWall": mean(acc["win"]),
           "winWallLit%": round(100 * sum(1 for c in acc["win"] if L(c) > 0.45) / max(1, len(acc["win"])), 1),
           "wallLit%": round(100 * sum(1 for c in acc["wall"] if L(c) > 0.45) / max(1, len(acc["wall"])), 1)}
    print(json.dumps(out, ensure_ascii=False))
