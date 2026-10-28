"""artlive 1623: q4_postoffice.py 패치 — icons 재질·PX·유리문 오른짝 안쪽 80° 열림·바닥·천장·실내등·창구 2·사서함 벽·분류 선반·의자 2·포스터·달력·화분·CamIn 빈 객체·렌더 생략."""
from pathlib import Path
p = Path(r'D:/Github/F1Game/tools/asset-gen/q4_postoffice.py'); s = p.read_text(encoding='utf-8')
def rep(a, b):
    global s
    assert s.count(a) == 1, a
    s = s.replace(a, b)
rep("L = json.loads((K / 'layout.json').read_text()); mats = {}; cells = {}",
    "L = json.loads((K / 'layout.json').read_text()); mats = {}; cells = {}\nPX = 32  # px/m. 작은 물건은 64(교훈 5)\nICONS = {'parcel': (0, 0, 128, 128), 'clock': (128, 0, 64, 64), 'fragile': (192, 0, 64, 32), 'slip': (192, 32, 32, 48), 'calendar': (128, 64, 48, 64), 'ticket': (176, 64, 48, 24)}  # tools/asset-gen/icons_shared.py")
rep("for s in ['wall', 'trim', 'floor', 'roof', 'decals']:\n", "for s in ['wall', 'trim', 'floor', 'roof', 'decals', 'icons']:\n")
rep("    if s == 'decals': nt.links.new(", "    if s in ('decals', 'icons'): nt.links.new(")
rep("    for c in L[s]: cells[c['name']] = (s, c['rect_px'])", "    for c in (L[s] if s != 'icons' else [dict(name=k, rect_px=v) for k, v in ICONS.items()]): cells[c['name']] = (s, c['rect_px'])")
rep("Wm, Hm = w / 32, h / 32", "Wm, Hm = w / PX, h / PX")
rep("    if s == 'decals': uvm = uvm or [(0, 0), (1, 0), (1, 1), (0, 1)]; uv = [((x + .5 + u * (w - 1)) / 512, 1 - (y + h - .5 - v * (h - 1)) / 512) for u, v in uvm]",
    "    if s in ('decals', 'icons'): A = 512 if s == 'decals' else 256; uvm = uvm or [(0, 0), (1, 0), (1, 1), (0, 1)]; uv = [((x + .5 + u * (w - 1)) / A, 1 - (y + h - .5 - v * (h - 1)) / A) for u, v in uvm]")
rep("-1e-3 <= u * 32 <= w + 1e-2 and -1e-3 <= v * 32 <= h + 1e-2", "-1e-3 <= u * PX <= w + 1e-2 and -1e-3 <= v * PX <= h + 1e-2")
rep("min(u * 32, w - .02)", "min(u * PX, w - .02)"); rep("min(v * 32, h - .02)", "min(v * PX, h - .02)")
# 유리문: 닫힌 두 짝 → 왼짝 닫힘 + 오른짝 열림(아래 블록)
rep("for a, b in ((D0, (D0 + D1) / 2), ((D0 + D1) / 2, D1)): wquad('front', a, b, .28, DH - .08, -.18, 'glass')\n", "")
rep("for a, b in ((D0, D0 + .07), ((D0 + D1) / 2 - .035, (D0 + D1) / 2 + .035), (D1 - .07, D1)): wbox('front', a, b, 0, DH, -.19, -.13, 'door_frame')\n", "")
rep("wbox('front', D0, D1, DH - .08, DH, -.19, -.13, 'door_frame'); wbox('front', D0, D1, 0, .28, -.19, -.13, 'door_frame')\n", "")
rep("for a in (M - .15, M + .12): wbox('front', a, a + .03, .85, 1.35, -.13, -.07, 'gutter')  # 문 손잡이 막대",
"""# 1623: 왼짝은 닫힌 채, 오른짝(M..D1)은 경첩 x D1 에서 안쪽으로 80° 열림
wquad('front', D0, M, .28, DH - .08, -.18, 'glass')
for a, b in ((D0, D0 + .07), (M - .035, M)): wbox('front', a, b, 0, DH, -.19, -.13, 'door_frame')
wbox('front', D0, M, DH - .08, DH, -.19, -.13, 'door_frame'); wbox('front', D0, M, 0, .28, -.19, -.13, 'door_frame')
wbox('front', M - .15, M - .12, .85, 1.35, -.13, -.07, 'gutter')  # 문 손잡이 막대
i0 = len(G['v'])
wquad('front', M, D1, .28, DH - .08, -.18, 'glass')
for a, b in ((M, M + .035), (D1 - .07, D1)): wbox('front', a, b, 0, DH, -.19, -.13, 'door_frame')
wbox('front', M, D1, DH - .08, DH, -.19, -.13, 'door_frame'); wbox('front', M, D1, 0, .28, -.19, -.13, 'door_frame')
for o0, o1 in ((-.13, -.07), (-.25, -.19)): wbox('front', M + .12, M + .15, .85, 1.35, o0, o1, 'gutter')
_hg = Vector((D1, .16, 0)); _RZ = Matrix.Rotation(math.radians(-80), 4, 'Z')
G['v'][i0:] = [tuple(_hg + _RZ @ (Vector(v) - _hg)) for v in G['v'][i0:]]""")
INT = Path(__file__).with_name('po_interior_1623.txt').read_text(encoding='utf-8')
rep("# ---------- 메시 만들기 ----------", INT + "\n# ---------- 메시 만들기 ----------")
rep("for o in asset: mw = o.matrix_world.copy(); o.parent = root; o.matrix_world = mw",
    "for o in asset: mw = o.matrix_world.copy(); o.parent = root; o.matrix_world = mw\nEMP = []\nfor nm, loc in (('STUDIO_CamIn', (-.05, 2.0, 1.5)), ('STUDIO_CamInLook', (-1.0, 6.0, 1.3)), ('STUDIO_Lamp', (0, 4.0, 3.1))):\n    e = bpy.data.objects.new(nm, None); bpy.context.collection.objects.link(e); e.location = loc; e.parent = root; EMP.append(e)")
rep("for o in asset + [root]: o.select_set(True)", "for o in asset + [root] + EMP: o.select_set(True)")
rep("for n in ['wall.png', 'trim.png', 'floor.png', 'roof.png', 'decals.png', 'postbox_G1.png']}", "for n in ['wall.png', 'trim.png', 'floor.png', 'roof.png', 'decals.png', 'icons.png', 'postbox_G1.png']}")
rep("print('VAL', val['tris'], bounds)", "print('VAL', val['tris'], bounds, flush=True)\nimport sys, os\nif '--no-render' in sys.argv: os._exit(0)")
p.write_text(s, encoding='utf-8'); print('ok')
