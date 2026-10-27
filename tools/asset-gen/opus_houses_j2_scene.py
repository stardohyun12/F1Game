"""집 j2판 시트용 — opus_houses_h_scene.py 와 같은 장면·카메라·측정, 줄만 j2 / j (과제 20261027-1807). Blender:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_j2_scene.py
출력: art/_review/2026-10-27-houses-j2/{row_j2,row_j}.png, scene_measure.json
"""
import os
_P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_h_scene.py')
_s = open(_P, encoding='utf-8').read()
for a, b in (("art/_review/2026-10-27-houses-h/'", "art/_review/2026-10-27-houses-j2/'"), ("for v in ('h', 'e'):", "for v in ('j2', 'j'):"),
             ("(('h', 'e'), ('e', 'h'))", "(('j2', 'j'), ('j', 'j2'))")):
    assert a in _s, a; _s = _s.replace(a, b, 1)
exec(compile(_s, _P, 'exec'))
