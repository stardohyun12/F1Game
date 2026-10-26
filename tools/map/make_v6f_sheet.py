"""v6e ↔ v6f 크롭(과제 0506): 0444 보고의 문제 자리. 사용: python -X utf8 tools/map/make_v6f_sheet.py"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import road_check as RC

C = [('posi_419759968_325~a', '헤어핀(민 다리, 0444 #1)', (-120, 312)), ('posi_419759968_325~b', '헤어핀 ~b 끝', (-60, 290)),
     ('tomo_118266930_2', '갈림목 끼임(0444 #4)', (63, 37)), ('seam6_tomo_posi_T', '갈림목 끼임(0444 #4)', (6, 98)),
     ('seam0/seam1', '나란한 이음매 4 m 간격(턱 3.2 m)', (152, 17)), ('posi_190982121_315', '큰길 끝 T(턱 2.0 m)', (-166, 368))]
centers = [{'a': a, 'b': why, 'm': 0, 'gapMin': '-', 'dy': '-', 'fix': '', 'at': at} for a, why, at in C]
RC.crop_sheet(['v6e', 'v6f'], centers, os.path.join(RC.OUT, 'v6e-vs-v6f-spots.png'), box=70.0, scale=5)
print('ok')
