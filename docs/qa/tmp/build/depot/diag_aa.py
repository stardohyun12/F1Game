# 2058 진단: COUNTER_01 영수증 아이콘 칸의 text_metrics(AA) 재현
import sys; sys.argv = ['x']; sys.path.insert(0, 'tools/art')
import check_rules as C, numpy as np
from pathlib import Path
t = C.tex(Path('art/export/q3-depot/icons.png'))
m = np.zeros((t['H'], t['W']), bool); m[10:25, 60:70] = True; m &= t['a'][..., 3] >= 128
print(C.text_metrics(t, m)); print(np.unique(t['l'][m].round(), return_counts=True))
print(t['l'][9:26, 59:71].round().astype(int))
