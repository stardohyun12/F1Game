import math, os
H = os.path.dirname(os.path.abspath(__file__))
P = [tuple(map(float, l.split())) for l in open(os.path.join(H, "loop_pts.txt")) if l.strip()]
def ang(a, b, c):
    u = (b[0] - a[0], b[1] - a[1]); v = (c[0] - b[0], c[1] - b[1])
    d = math.hypot(*u) * math.hypot(*v)
    return 0 if d == 0 else math.degrees(math.acos(max(-1, min(1, (u[0] * v[0] + u[1] * v[1]) / d))))
for i in range(1, len(P) - 1):
    a = ang(P[i - 1], P[i], P[i + 1])
    if a > 100: print(i, P[i], round(a))
print("tail", P[-6:])
