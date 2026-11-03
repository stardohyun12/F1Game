# wav 마다 전체 크기(dBFS)와 1 kHz 위 에너지 비율 — 로우패스가 실제로 먹었는지
import sys, glob, os, wave, json
import numpy as np
out = []
for p in sorted(glob.glob(sys.argv[1] + "/*.wav") + glob.glob(sys.argv[1] + "/before/*.wav")):
    w = wave.open(p); n, ch, sr = w.getnframes(), w.getnchannels(), w.getframerate()
    x = np.frombuffer(w.readframes(n), dtype=np.int16).astype(np.float64) / 32768.0
    x = x.reshape(-1, ch).mean(axis=1)
    if len(x) < sr: out.append((os.path.relpath(p, sys.argv[1]), "too short", len(x))); continue
    rms = 10 * np.log10(np.mean(x ** 2) + 1e-20)
    S = np.abs(np.fft.rfft(x * np.hanning(len(x)))) ** 2; f = np.fft.rfftfreq(len(x), 1 / sr)
    hi = S[f > 1000].sum() / S.sum(); hi3 = S[f > 3000].sum() / S.sum()
    out.append((os.path.relpath(p, sys.argv[1]).replace("\\", "/"), round(len(x) / sr, 2), round(rms, 1), round(100 * hi, 1), round(100 * hi3, 2)))
for o in out: print(o)
json.dump([dict(zip(["wav", "sec", "dbfs", "pct_above_1k", "pct_above_3k"], o)) for o in out], open(sys.argv[1] + "/wav_spectrum.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
