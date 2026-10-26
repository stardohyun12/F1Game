"""town-v6b.json 의 길 하나: 매듭 사이 수평거리·경사·꺾임각."""
import json, math, sys
rid = sys.argv[1] if len(sys.argv) > 1 else 'sc_garage_top_house_0'
J = json.load(open('docs/map/town-v6b.json', encoding='utf-8'))
r = next(r for r in J['roads'] if r['id'] == rid)
k = r['knots']; print(rid, r['kind'], r['width'], len(k), 'knots')
prev = None
for i in range(len(k) - 1):
    dx, dz, dy = k[i+1][0]-k[i][0], k[i+1][1]-k[i][1], k[i+1][2]-k[i][2]
    h = math.hypot(dx, dz); a = math.degrees(math.atan2(dz, dx))
    turn = '' if prev is None else f'{((a - prev + 180) % 360) - 180:+.0f}'
    prev = a
    if h < 3 or abs(dy / max(h, .01)) > .2 or (turn and abs(float(turn)) > 60):
        print(i, f'h{h:.1f} g{dy/max(h,.01)*100:.0f}% turn{turn} at {k[i][0]:.0f},{k[i][1]:.0f}')
