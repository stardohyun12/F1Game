# 0.1초 기록에서: 실내↔바깥 전환에 걸린 시간, 실내로 잡힌 구간들(엉뚱한 곳이 실내로 잡히는지), 카트 주행 중 값
import csv, json, sys
r = list(csv.DictReader(open(sys.argv[1], encoding="utf-8"), delimiter="\t"))
f = lambda x, k: float(x[k])
out = {"rows": len(r), "transitions": [], "indoor_segments": [], "drive": {}}
# 전환: indoor 가 0.95 이상 → 0.05 이하(또는 반대)까지 걸린 시간
i = 0
while i < len(r):
    a = f(r[i], "indoor")
    if a >= 0.95 or a <= 0.05:
        for j in range(i + 1, min(i + 40, len(r))):
            b = f(r[j], "indoor")
            if (a >= 0.95 and b <= 0.05) or (a <= 0.05 and b >= 0.95):
                # 실제로 움직이기 시작한 프레임
                k = i
                while k + 1 < j and abs(f(r[k + 1], "indoor") - a) < 0.01: k += 1
                out["transitions"].append({"t": f(r[k], "t"), "from": a, "to": b, "sec": round(f(r[j], "t") - f(r[k], "t"), 2),
                    "pos": [f(r[k], "x"), f(r[k], "y"), f(r[k], "z")], "windDb": [f(r[k], "windDb"), f(r[j], "windDb")], "cutoff": [f(r[k], "cutoffHz"), f(r[j], "cutoffHz")],
                    "ear": [f(r[k], "earDbFS"), f(r[j], "earDbFS")]})
                i = j; break
    i += 1
# 실내 구간
seg = None
for x in r:
    on = f(x, "indoor") > 0.3
    if on and seg is None: seg = {"t0": f(x, "t"), "start": [f(x, "x"), f(x, "y"), f(x, "z")]}
    if not on and seg is not None:
        seg["t1"] = f(x, "t"); seg["end"] = [f(x, "x"), f(x, "y"), f(x, "z")]; out["indoor_segments"].append(seg); seg = None
if seg: seg["t1"] = f(r[-1], "t"); seg["end"] = [f(r[-1], "x"), f(r[-1], "y"), f(r[-1], "z")]; out["indoor_segments"].append(seg)
d = [x for x in r if f(x, "speed") > 0.1]
if d:
    s = lambda k: sorted(f(x, k) for x in d)
    med = lambda v: v[len(v) // 2]
    out["drive"] = {"rows": len(d), "speed_med": med(s("speed")), "windDb_med": med(s("windDb")), "waveDb_med": med(s("waveDb")), "ear_med": med(s("earDbFS")),
                    "windDb_range": [min(s("windDb")), max(s("windDb"))], "indoor_max": max(s("indoor")), "cutoff_min": min(s("cutoffHz"))}
w = [x for x in r if f(x, "speed") == 0 and x["label"] != "실내"]
if w: out["onfoot_outdoor_ear_med"] = sorted(f(x, "earDbFS") for x in w)[len(w) // 2]
json.dump(out, open(sys.argv[2], "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(json.dumps(out, ensure_ascii=False, indent=1))
