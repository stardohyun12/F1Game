"""Analyse raw sources: per-2s RMS for waves/wind, event list for gull file. Usage: analyze.py RAWDIR"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from audiolib import SR, db, decode  # noqa: E402

raw = sys.argv[1]

for name in ("1445", "0595"):
    x = decode(os.path.join(raw, name + ".wav"))
    print(f"== {name}: {len(x) / SR:.1f}s peak {db(np.abs(x).max()):.1f} dBFS")
    w = 2 * SR
    env = db([np.sqrt(np.mean(x[i:i + w].mean(axis=1) ** 2)) for i in range(0, len(x) - w + 1, w)])
    print("  RMS dB per 2s:", " ".join(f"{v:.0f}" for v in env))
    # low-frequency (<150Hz) vs total to catch handling thumps
    m = x.mean(axis=1)
    spec = np.abs(np.fft.rfft(m[: SR * 20]))
    fr = np.fft.rfftfreq(len(m[: SR * 20]), 1 / SR)
    tot = spec.sum()
    print("  energy share  <150Hz %.0f%%  150-1k %.0f%%  1-4k %.0f%%  >4k %.0f%%" % (
        100 * spec[fr < 150].sum() / tot, 100 * spec[(fr >= 150) & (fr < 1000)].sum() / tot,
        100 * spec[(fr >= 1000) & (fr < 4000)].sum() / tot, 100 * spec[fr >= 4000].sum() / tot))

# gull events: band 1.2-6 kHz energy per 50 ms frame vs. median
x = decode(os.path.join(raw, "2573.wav"), ch=1)[:, 0]
print(f"== 2573: {len(x) / SR:.1f}s peak {db(np.abs(x).max()):.1f} dBFS")
fr_len = int(0.05 * SR)
n = len(x) // fr_len
band = []
low = []
for i in range(n):
    seg = x[i * fr_len:(i + 1) * fr_len] * np.hanning(fr_len)
    s = np.abs(np.fft.rfft(seg))
    f = np.fft.rfftfreq(fr_len, 1 / SR)
    band.append(s[(f >= 1200) & (f < 6000)].sum())
    low.append(s[f < 400].sum())
band = np.array(band)
low = np.array(low)
bdb = db(band)
med = np.median(bdb)
thr = med + 10
act = bdb > thr
print(f"  band median {med:.1f} dB, threshold {thr:.1f} dB")
events = []
i = 0
while i < n:
    if act[i]:
        j = i
        gap = 0
        while j < n and gap < 6:  # merge gaps up to 0.3 s
            if act[j]:
                gap = 0
                last = j
            else:
                gap += 1
            j += 1
        events.append((i * 0.05, (last + 1) * 0.05, float(bdb[i:last + 1].max() - med), float(db(low[i:last + 1].mean()) - bdb[i:last + 1].mean())))
        i = last + 1
    else:
        i += 1
print("  events (start, end, dur, peak-over-median dB, low-vs-band dB):")
for s, e, p, lv in events:
    print(f"   {s:6.2f} {e:6.2f}  {e - s:4.2f}s  +{p:4.1f}dB  low-band {lv:5.1f}")
