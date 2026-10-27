"""집 j3판 시트용 — opus_houses_h_scene.py 와 같은 장면·카메라, 줄만 j3 / j2 (과제 20261027-1902). Blender:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_j3_scene.py
출력: art/_review/2026-10-27-houses-j3/{row_j3,row_j2}.png, scene_measure.json
"""
import os
_P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_h_scene.py')
_s = open(_P, encoding='utf-8').read()
for a, b in (("art/_review/2026-10-27-houses-h/'", "art/_review/2026-10-27-houses-j3/'"), ("for v in ('h', 'e'):", "for v in ('j3', 'j2'):"),
             ("(('h', 'e'), ('e', 'h'))", "(('j3', 'j2'), ('j2', 'j3'))")):
    assert a in _s, a; _s = _s.replace(a, b, 1)
if os.environ.get('J3_LEFT'):  # 왼쪽(-X) 옆벽이 보이게 카메라를 반대로 18° — 출력 left/
    for a, b in (("art/_review/2026-10-27-houses-j3/'", "art/_review/2026-10-27-houses-j3/left/'"), ("math.sin(math.radians(18))", "math.sin(math.radians(-18))")):
        assert a in _s, a; _s = _s.replace(a, b, 1)
    os.makedirs('D:/Github/F1Game/art/_review/2026-10-27-houses-j3/left', exist_ok=True)
exec(compile(_s, _P, 'exec'))
