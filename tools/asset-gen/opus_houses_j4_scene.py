"""집 j4판 시트용 — opus_houses_h_scene.py 와 같은 장면·카메라, 줄만 j4 / j3 (과제 20261027-2002). Blender:
    D:/Tools/Blender/blender.exe -b --factory-startup -P tools/asset-gen/opus_houses_j4_scene.py [-- left]
출력: art/_review/2026-10-27-houses-j4/{row_j4,row_j3}.png · left/ 는 카메라를 반대로 18°(왼쪽 옆벽)
"""
import os, sys
_P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'opus_houses_h_scene.py')
_s = open(_P, encoding='utf-8').read(); LEFT = 'left' in sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else False
RV = "art/_review/2026-10-27-houses-j4/" + ('left/' if LEFT else '')
os.makedirs('D:/Github/F1Game/' + RV, exist_ok=True)
for a, b in (("art/_review/2026-10-27-houses-h/'", RV + "'"), ("for v in ('h', 'e'):", "for v in ('j4', 'j3'):"),
             ("(('h', 'e'), ('e', 'h'))", "(('j4', 'j3'), ('j3', 'j4'))")) + ((("math.sin(math.radians(18))", "math.sin(math.radians(-18))"),) if LEFT else ()):
    assert a in _s, a; _s = _s.replace(a, b, 1)
exec(compile(_s, _P, 'exec'))
