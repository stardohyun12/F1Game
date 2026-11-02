"""D-257 프리셋별 실측 표 — probe_A/B/C.json → table.md (목록 형식, 한글 표 테두리 어긋남 회피)."""
import json, os, sys
here = os.path.dirname(os.path.abspath(__file__))
rows = [
    ("0→40km/h (초)", "accel", "t0to40"),
    ("5초 뒤 속도 (km/h)", "accel", "kmhAt5s"),
    ("7초 뒤 속도 (km/h)", "accel", "kmhAt7s"),
    ("40→0 제동 거리 (m)", "brake", "distM"),
    ("40→0 제동 시간 (초)", "brake", "timeS"),
    ("제동 때 화물 앞뒤 G 최대", "brake", "cargoLongGMax"),
    ("제동 때 차체 앞숙임 최대 (°)", "brake", "bodyPitchMaxDeg"),
    ("제동 때 짐칸 흔들림 최대 (°)", "brake", "boxSwayMaxDeg"),
    ("15km/h 핸들 끝 회전 반경 (m)", "turn15", "radiusM"),
    ("35km/h 핸들 끝 회전 반경 (m)", "turn35", "radiusM"),
    ("35km/h 회전 속도 (°/초)", "turn35", "yawDegS"),
    ("35km/h 핸들 반응 63% (초)", "turn35", "resp63S"),
    ("35km/h 코너 화물 옆 G 최대", "turn35", "cargoLatGMax"),
    ("35km/h 코너 차체 기울기 최대 (°)", "turn35", "bodyRollMaxDeg"),
    ("35km/h 코너 짐칸 흔들림 최대 (°)", "turn35", "boxSwayMaxDeg"),
    ("턱 8cm 통과", "bump8cm", "passed"),
    ("턱 8cm 위아래 출렁 최대 (cm)", "bump8cm", "heaveMaxCm"),
    ("턱 8cm 짐칸 흔들림 최대 (°)", "bump8cm", "boxSwayMaxDeg"),
    ("턱 8cm 화물 앞뒤 G 최대", "bump8cm", "cargoLongGMax"),
    ("비탈 10° 30m 오르는 시간 (초)", "ramp10", "timeToTopS"),
    ("비탈 꼭대기 속도 (km/h)", "ramp10", "kmhAtTop"),
    ("핸드브레이크 1초 뒤 속도 (km/h, 40에서)", "handbrake", "kmhAfter1s"),
    ("핸드브레이크 1초 회전 (°)", "handbrake", "yawDeg"),
    ("핸드브레이크 미끄러짐 각 최대 (°)", "handbrake", "slipMaxDeg"),
    ("빈 차 0→40 (초)", "load0", "t0to40"),
    ("달걀 6kg 0→40 (초)", "load6", "t0to40"),
    ("상자 45kg 0→40 (초)", "load45", "t0to40"),
    ("빈 차 40→0 거리 (m)", "load0", "brakeDistM"),
    ("상자 45kg 40→0 거리 (m)", "load45", "brakeDistM"),
]
data = {}
for p in "ABC":
    f = os.path.join(here, f"probe_{p}.json")
    if os.path.exists(f):
        data[p] = json.load(open(f, encoding="utf-8"))
out = ["# D-257 프리셋별 실측 (DriveFeelProbe, y=400 임시 평지, 같은 입력)", ""]
for label, sec, key in rows:
    vals = []
    for p in "ABC":
        v = data.get(p, {}).get(sec, {}).get(key, "-")
        vals.append(f"{p} {v}")
    out.append(f"- {label}: " + " / ".join(vals))
open(os.path.join(here, "table.md"), "w", encoding="utf-8").write("\n".join(out) + "\n")
print("\n".join(out))
