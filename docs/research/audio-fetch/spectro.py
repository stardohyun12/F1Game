"""Write a spectrogram PNG (numpy + zlib only). Usage: spectro.py WAV OUT.png START_S DUR_S [MAXHZ]"""
import os
import struct
import sys
import zlib

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from audiolib import SR, decode  # noqa: E402

src, out, start, dur = sys.argv[1], sys.argv[2], float(sys.argv[3]), float(sys.argv[4])
maxhz = float(sys.argv[5]) if len(sys.argv) > 5 else 10000
x = decode(src, ch=1)[:, 0]
x = x[int(start * SR):int((start + dur) * SR)]
nfft = 1024
hop = int(SR * dur / 1400)
hop = max(hop, 64)
win = np.hanning(nfft)
frames = []
for i in range(0, len(x) - nfft, hop):
    s = np.abs(np.fft.rfft(x[i:i + nfft] * win))
    frames.append(s)
S = np.array(frames).T  # freq x time
f = np.fft.rfftfreq(nfft, 1 / SR)
S = S[f <= maxhz]
S = 20 * np.log10(S + 1e-6)
hi = np.percentile(S, 99.5)
lo = hi - 60
img = np.clip((S - lo) / (hi - lo), 0, 1)
img = (img[::-1] * 255).astype(np.uint8)
# upscale vertical x2 for visibility
img = np.repeat(img, 2, axis=0)
h, w = img.shape
rgb = np.stack([img, (img.astype(int) * 0.8).astype(np.uint8), 255 - img], axis=2) if False else np.stack([img] * 3, axis=2)
raw = b"".join(b"\x00" + rgb[r].tobytes() for r in range(h))


def chunk(t, d):
    c = struct.pack(">I", len(d)) + t + d
    return c + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)


png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 6)) + chunk(b"IEND", b"")
open(out, "wb").write(png)
print("wrote", out, w, h, f"time {start}-{start + dur}s, freq 0-{maxhz}Hz, ~{hop / SR * 1000:.0f}ms/px")
