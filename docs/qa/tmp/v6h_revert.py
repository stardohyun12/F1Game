# 과제 0640 — v6h 봇 실패 악화(Box 20→31, 합 48→50) → 씬을 커밋된 상태로 되돌림. 빌더 .cs 는 유지.
import subprocess
S = 'F1/Assets/Scenes/'
def g(*a): r = subprocess.run(['git', *a], capture_output=True, text=True, encoding='utf-8', errors='replace'); return r.stdout + r.stderr
print(g('checkout', '--', S + 'Map_TownV6.unity', S + 'Map_TownV6.unity.meta', S + 'Map_TownV6_Mats'))
un = [l for l in g('clean', '-n', S + 'Map_TownV6_Mats').splitlines() if l.strip()]
print('untracked', len(un), un[:3])
print(g('clean', '-fq', S + 'Map_TownV6_Mats'))
st = [l for l in g('status', '--short', S).splitlines() if l.strip()]
print('scene status lines', len(st), st[:5])
