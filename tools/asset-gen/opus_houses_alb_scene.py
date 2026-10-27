"""집 albedo 시트용 — opus_houses_h_scene.py 와 같은 장면·카메라 (과제 20261027-2023). Blender:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_alb_scene.py [-- lit]
기본 = 빛 없음(Emission): row_alb(albedo 평면) · row_j5(빛 구운 판)
lit  = 해 1개 + 하늘빛(Diffuse, 게임 실시간 빛 흉내): lit/row_alb · lit/row_j5(구운 판에 빛을 또 얹으면 = 이중)
       해: 세기 3.2, 방향 = 왼쪽 위 앞(고도 50°, 방위 -35°), 하늘빛 (.62,.72,.78) 세기 .9 · 정비소는 원래 구운 판이라 빛 없이 그대로
"""
import os, sys
_P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_h_scene.py')
_s = open(_P, encoding='utf-8').read(); LIT = 'lit' in (sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
RV = "art/_review/2026-10-27-houses-alb/" + ('lit/' if LIT else '')
os.makedirs('D:/Github/F1Game/' + RV, exist_ok=True)
REP = [("art/_review/2026-10-27-houses-h/'", RV + "'"), ("for v in ('h', 'e'):", "for v in ('alb', 'j5'):"),
       ("(('h', 'e'), ('e', 'h'))", "(('alb', 'j5'), ('j5', 'alb'))")]
if LIT:
    REP += [("    im, _ = pix(png); m = bpy.data.materials.new(", "    LITM = 'garage' not in png; im, _ = pix(png); m = bpy.data.materials.new("),
            ("em = nt.nodes.new('ShaderNodeEmission')", "em = nt.nodes.new('ShaderNodeBsdfDiffuse' if LITM else 'ShaderNodeEmission')"),
            ("wd.node_tree.nodes['Background'].inputs[0].default_value = (.62, .72, .78, 1)",
             "wd.node_tree.nodes['Background'].inputs[0].default_value = (.62, .72, .78, 1); wd.node_tree.nodes['Background'].inputs[1].default_value = .9\n"
             "sd = bpy.data.lights.new('sun', 'SUN'); sd.energy = 3.2; sd.angle = math.radians(2); so = bpy.data.objects.new('sun', sd); sc.collection.objects.link(so)\n"
             "so.rotation_euler = (math.radians(40), 0, math.radians(-35))"),
            ("sc.cycles.samples = 4; sc.cycles.use_denoising = False", "sc.cycles.samples = 64; sc.cycles.use_denoising = True")]
for a, b in REP:
    assert a in _s, a; _s = _s.replace(a, b, 1)
exec(compile(_s, _P, 'exec'))
