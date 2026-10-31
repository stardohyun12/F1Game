"""Cut loops + gull clips from the raw CC0 sources. Usage: build.py RAWDIR OUTDIR
Writes amb_wind_01.wav, amb_waves_01.wav, amb_gull_01..05.wav and build_log.json (cut ranges) into OUTDIR."""
import json
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from audiolib import SR, db, decode, make_loop, rms_env, write_wav  # noqa: E402

raw, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
log = {}


def rms_db(x):
    m = x.mean(axis=1) if x.ndim == 2 else x
    return float(db(np.sqrt(np.mean(m ** 2))))


def best_loop(x, L_s, xf_s, step_s=0.5, lo_s=0.0, hi_s=None):
    L, xf = int(L_s * SR), int(xf_s * SR)
    n = L + xf
    hi = len(x) - n if hi_s is None else min(int(hi_s * SR), len(x) - n)
    best = None
    for st in range(int(lo_s * SR), hi, int(step_s * SR)):
        seg = x[st:st + n]
        head, tail = seg[:xf], seg[L:L + xf]
        h, t = rms_db(head), rms_db(tail)
        loop = make_loop(seg, xf)
        tiled = np.concatenate([loop, loop[: SR * 4]])
        env = db(rms_env(tiled, SR, SR // 2))  # 1 s windows, 0.5 s hop, includes wrap
        score = abs(h - t) * 2 + env.std() * 1.0 + (np.abs(loop).max() > 0.98) * 5
        if best is None or score < best[0]:
            best = (score, st, h, t, env.std())
    return best


def finish_loop(name, x, st_s, L_s, xf_s):
    L, xf = int(L_s * SR), int(xf_s * SR)
    st = int(st_s * SR)
    seg = x[st:st + L + xf]
    loop = make_loop(seg, xf)
    pk = float(np.abs(loop).max())
    gain = 1.0
    if pk > 0.89:  # keep headroom -1 dBFS
        gain = 0.89 / pk
        loop = loop * gain
    write_wav(os.path.join(out, name), loop)
    tiled = np.concatenate([loop, loop])
    seam = len(loop)
    around = []
    for off in np.arange(-2.0, 2.01, 0.5):
        a = int(seam + off * SR)
        around.append(round(rms_db(tiled[a - SR // 2:a + SR // 2]), 1))
    step_at_seam = float(np.abs(tiled[seam, :] - tiled[seam - 1, :]).max())
    med_step = float(np.median(np.abs(np.diff(tiled[:, 0]))))
    p999 = float(np.percentile(np.abs(np.diff(tiled[:, 0])), 99.9))
    info = {
        "source_start_s": round(st_s, 2), "source_end_s": round(st_s + L_s + xf_s, 2),
        "loop_len_s": L_s, "crossfade_s": xf_s, "crossfade_curve": "equal-power (sin/cos)",
        "gain_applied_dB": round(float(db(gain)), 2), "peak_dBFS": round(float(db(np.abs(loop).max())), 1),
        "rms_dBFS": round(rms_db(loop), 1),
        "rms_1s_around_seam_(-2..+2s step .5)": around,
        "seam_sample_step": round(step_at_seam, 5), "median_sample_step": round(med_step, 5), "p99.9_sample_step": round(p999, 5),
    }
    log[name] = info
    print(name, json.dumps(info))


# ---- waves (BSB 1445) ----
xw = decode(os.path.join(raw, "1445.wav"))
sc, st, h, t, sd = best_loop(xw, 45, 4, step_s=0.5, lo_s=2, hi_s=len(xw) / SR - 52)
print("waves best start", st / SR, "head/tail dB", h, t, "env std", sd)
finish_loop("amb_waves_01.wav", xw, st / SR, 45, 4)

# ---- wind (BSB 0595) ----
xd = decode(os.path.join(raw, "0595.wav"))
sc, st, h, t, sd = best_loop(xd, 45, 5, step_s=0.5, lo_s=3, hi_s=len(xd) / SR - 53)
print("wind best start", st / SR, "head/tail dB", h, t, "env std", sd)
finish_loop("amb_wind_01.wav", xd, st / SR, 45, 5)

# ---- gulls (BSB 2573): (clip_start_s, clip_end_s) chosen from onset list in analyze.py output ----
xg = decode(os.path.join(raw, "2573.wav"))
clips = [(15.15, 17.25), (20.0, 21.9), (33.65, 35.45), (38.8, 40.7), (110.15, 112.35)]
for i, (a, b) in enumerate(clips, 1):
    seg = xg[int(a * SR):int(b * SR)].copy()
    n = len(seg)
    fi = int(0.04 * SR)
    fo = int(0.25 * SR)
    seg[:fi] *= np.sin(np.linspace(0, np.pi / 2, fi))[:, None]
    seg[-fo:] *= np.cos(np.linspace(0, np.pi / 2, fo))[:, None]
    name = f"amb_gull_{i:02d}.wav"
    write_wav(os.path.join(out, name), seg)
    info = {"source_start_s": a, "source_end_s": b, "len_s": round(n / SR, 2), "fade_in_ms": 40, "fade_out_ms": 250,
            "peak_dBFS": round(float(db(np.abs(seg).max())), 1), "rms_dBFS": round(rms_db(seg), 1)}
    log[name] = info
    print(name, json.dumps(info))

with open(os.path.join(out, "build_log.json"), "w", encoding="utf-8") as f:
    json.dump(log, f, indent=1, ensure_ascii=False)
