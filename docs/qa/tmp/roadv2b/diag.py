# 1c 진단: 뜸 지점 근처 사슬 중심선·갈림목·레이 높이
import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..', '..', '..', 'scripts'))
import fb_geom
PROJ = 'D:/Github/F1Game/F1'
code = open(os.path.join(os.path.dirname(__file__), 'diag.cs.txt'), encoding='utf-8').read()
print(fb_geom.ucli_exec(PROJ, code))
