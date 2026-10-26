import sys, json, glob, colorsys
import numpy as np
from PIL import Image

def load(f):
    return np.asarray(Image.open(f).convert('RGB')).astype(np.float32)

def period(a, axis):
    # edge profile along axis, autocorrelation -> dominant period (block size)
    g = np.abs(np.diff(a.mean(2), axis=axis))
    prof = g.sum(axis=1-axis)
    prof = prof - prof.mean()
    n = len(prof)
    f = np.abs(np.fft.rfft(prof, n=2*n))**2
    ac = np.fft.irfft(f)[:n]
    ac = ac / ac[0]
    best = None
    for p in range(2, 16):
        if best is None or ac[p] > ac[best]:
            best = p
    return best, round(float(ac[best]), 3), [round(float(ac[p]),3) for p in range(2,9)]

def edge_phase_hist(a, axis, p):
    g = np.abs(np.diff(a.mean(2), axis=axis))
    prof = g.sum(axis=1-axis)
    idx = np.arange(len(prof))
    h = np.array([prof[idx % p == k].mean() for k in range(p)])
    return (h / h.mean()).round(2).tolist()

def runlen(a):
    # horizontal run lengths of near-identical pixels (tol 6)
    d = np.abs(np.diff(a, axis=1)).max(2) > 10
    runs = []
    for row in d[::7]:
        pos = np.flatnonzero(row)
        if len(pos) > 1:
            runs.extend(np.diff(pos).tolist())
    runs = np.array(runs)
    runs = runs[runs < 40]
    hist = np.bincount(runs, minlength=12)[:12]
    return (hist / hist.sum()).round(3).tolist()

def stats(a, block=1):
    if block > 1:
        a = a[block//2::block, block//2::block]
    rgb = a / 255
    lum = 0.2126*rgb[...,0] + 0.7152*rgb[...,1] + 0.0722*rgb[...,2]
    mx = rgb.max(2); mn = rgb.min(2)
    sat = np.where(mx > 0, (mx-mn)/np.maximum(mx, 1e-6), 0)
    q = (a // 16).astype(np.int32)  # 4-bit/channel bins
    keys = q[...,0]*256 + q[...,1]*16 + q[...,2]
    cnt = np.bincount(keys.ravel(), minlength=4096)
    tot = keys.size
    sig = int((cnt > tot*0.001).sum())
    cover90 = int(np.searchsorted(np.cumsum(np.sort(cnt)[::-1]), tot*0.9) + 1)
    # local texture: std in 8x8 tiles of block-sampled image
    L = lum*255
    h, w = (L.shape[0]//8)*8, (L.shape[1]//8)*8
    t = L[:h,:w].reshape(h//8, 8, w//8, 8).std(axis=(1,3))
    # high-freq noise: mean abs laplacian on block-sampled
    lap = np.abs(4*L[1:-1,1:-1] - L[:-2,1:-1] - L[2:,1:-1] - L[1:-1,:-2] - L[1:-1,2:])
    # checkerboard / dither signature: pixel differs from both horizontal neighbours in the same direction
    c = L[1:-1,1:-1]
    alt = ((c - L[1:-1,:-2]) * (c - L[1:-1,2:]) > 25) & ((c - L[:-2,1:-1]) * (c - L[2:,1:-1]) > 25)
    return dict(lum_mean=round(float(L.mean()),1), lum_p5=round(float(np.percentile(L,5)),1),
                lum_p95=round(float(np.percentile(L,95)),1), lum_std=round(float(L.std()),1),
                sat_mean=round(float(sat.mean()),3), sat_p90=round(float(np.percentile(sat,90)),3),
                colors_4bit_sig=sig, colors_4bit_cover90=cover90,
                tile_std_median=round(float(np.median(t)),2), tile_std_p75=round(float(np.percentile(t,75)),2),
                lap_mean=round(float(lap.mean()),2), lap_p90=round(float(np.percentile(lap,90)),2),
                isolated_px_pct=round(float(alt.mean()*100),2))

if __name__ == '__main__':
    out = {}
    for f in sys.argv[1:]:
        a = load(f)
        px, pxs, pxac = period(a, 1)
        py, pys, pyac = period(a, 0)
        out[f] = dict(size=[a.shape[1], a.shape[0]], period_x=[px, pxs], period_y=[py, pys], ac_x=pxac,
                      runlen_hist=runlen(a), phase_x=edge_phase_hist(a, 1, px),
                      full=stats(a), block=stats(a, px if pxs > 0.15 else 1))
    print(json.dumps(out, ensure_ascii=False, indent=1))
