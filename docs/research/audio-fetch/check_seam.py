"""Seam click check + tiled wav for spectrogram. Usage: check_seam.py LOOP.wav TILED_OUT.wav"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from audiolib import SR, db, decode, write_wav  # noqa: E402

x = decode(sys.argv[1])
tiled = np.concatenate([x, x, x])
write_wav(sys.argv[2], tiled)
m = tiled.mean(axis=1)
d = np.diff(m)  # first difference ~ high-pass; clicks show as spikes
win = int(0.01 * SR)
env = np.array([np.sqrt(np.mean(d[i:i + win] ** 2)) for i in range(0, len(d) - win, win // 2)])
seam_idx = int(len(x) / (win // 2))
local = env[seam_idx - 4:seam_idx + 4]
print("file", sys.argv[1], f"len {len(x) / SR:.2f}s")
print("10ms HF-energy at seam (dB):", [round(float(db(v)), 1) for v in local])
print("10ms HF-energy overall median / p99 / max (dB): %.1f %.1f %.1f" % (db(np.median(env)), db(np.percentile(env, 99)), db(env.max())))
print("seam max vs overall p99: %+.1f dB" % (db(local.max()) - db(np.percentile(env, 99))))
# 3-sample abs jump at the seam compared with max jump anywhere else
j = np.abs(d[len(x) - 3:len(x) + 3])
print("max |step| within 3 samples of seam: %.5f ; 99.99th pct elsewhere: %.5f" % (j.max(), np.percentile(np.abs(d), 99.99)))
