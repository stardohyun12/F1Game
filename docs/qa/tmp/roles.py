import re
notes = [
 "2층 방 가운데, 문 쪽을 봄",
 "1층 현관(DOOR_01 184.5,-63.9) 밖 2.6 m, 집을 봄",
 "사장(232.7,-55.2) 1.9 m 앞 — E 말하기가 떠야 함",
 "지금 카트가 있는 곳, 내릴 때 서는 자리 — E 타기가 떠야 함",
 "DP_tea (93.8,-288.0) 표식 6 m 북쪽 도로에서 표식을 봄",
 "손가락 부두 남쪽 끝 자리 뒤 1.1 m — E 앉기가 떠야 함",
 "동쪽 부두 갑판(Pier 272,-235), 바다를 봄",
 "Cam_FB_sink 가 보는 점(120,19,18)을 내려다보는 자리",
 "main 도로 중심선 점(175,-75)",
 "차고 가운데(벽까지 5~8 m) — 바람·파도가 먹먹해야 함",
 "마을 둘레 지형 최고점(y 64.8, 레이 격자 4 m) — 바람이 가장 큼",
 "열린 셔터(x 268~271.5) 앞 마당, 안을 봄 — 카트는 앞 길, 마당엔 동쪽 회색 경사로(x 277)로",
 "서쪽 벽 칠판 1.8 m 앞 — E 칠판 보기가 떠야 함",
]
p='world/world.yaml'
lines=open(p,encoding='utf-8').read().split('\n')
k=0
for i,l in enumerate(lines):
    if l.startswith('  - {name:') and 'car:' in l and 'feet:' in l:
        l2=re.sub(r'role: "?[^"]*?"?,(\s+)feet:', lambda m: 'role: "%s",%sfeet:'%(notes[k],m.group(1)), l)
        assert l2!=l or notes[k] in l, (i,l)
        lines[i]=l2; k+=1
assert k==13,k
open(p,'w',encoding='utf-8').write('\n'.join(lines))
import yaml
d=yaml.safe_load(open(p,encoding='utf-8'))
for pl,n in zip(d['places'],notes): assert pl['role']==n,(pl,n)
print('ok')
