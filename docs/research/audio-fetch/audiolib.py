"""Small audio helpers: ffmpeg decode to float32, wav writer, envelope, crossfade loop."""
import shutil
import subprocess
import wave

import numpy as np

SR = 44100


def decode(path, ch=2, sr=SR):
    ff = shutil.which("ffmpeg")
    cmd = [ff, "-v", "error", "-i", path, "-ar", str(sr), "-ac", str(ch), "-f", "f32le", "-"]
    raw = subprocess.run(cmd, capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype="<f4").reshape(-1, ch).copy()


def write_wav(path, x, sr=SR):
    x = np.clip(x, -1.0, 1.0)
    pcm = (x * 32767.0).round().astype("<i2")
    if pcm.ndim == 1:
        pcm = pcm[:, None]
    with wave.open(path, "wb") as w:
        w.setnchannels(pcm.shape[1])
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(pcm.tobytes())


def db(v):
    return 20 * np.log10(np.maximum(v, 1e-9))


def rms_env(x, win, hop=None):
    hop = hop or win
    m = x.mean(axis=1) if x.ndim == 2 else x
    n = (len(m) - win) // hop + 1
    return np.array([np.sqrt(np.mean(m[i * hop:i * hop + win] ** 2)) for i in range(n)])


def make_loop(seg, xf):
    """seg has length L+xf. Returns length-L loop whose wrap point is a crossfaded (equal-power) blend."""
    n = len(seg)
    L = n - xf
    t = np.linspace(0, 1, xf, endpoint=False)[:, None] if seg.ndim == 2 else np.linspace(0, 1, xf, endpoint=False)
    fin = np.sin(t * np.pi / 2)
    fout = np.cos(t * np.pi / 2)
    head = seg[:xf]
    tail = seg[L:L + xf]
    blend = tail * fout + head * fin
    return np.concatenate([blend, seg[xf:L]], axis=0)


def seam_report(loop, sr=SR, span=0.5):
    """Compare level of 0.5 s before/after the wrap point, and sample jump at the seam vs typical jump."""
    k = int(span * sr)
    m = loop.mean(axis=1) if loop.ndim == 2 else loop
    before = m[-k:]
    after = m[:k]
    jump = abs(m[0] - m[-1])
    typical = np.median(np.abs(np.diff(m)))
    p99 = np.percentile(np.abs(np.diff(m)), 99.9)
    return {
        "rms_before_dB": float(db(np.sqrt(np.mean(before ** 2)))),
        "rms_after_dB": float(db(np.sqrt(np.mean(after ** 2)))),
        "seam_jump": float(jump),
        "median_step": float(typical),
        "p99.9_step": float(p99),
    }
