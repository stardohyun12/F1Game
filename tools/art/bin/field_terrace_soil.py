"""Tilled-soil albedo from CC0 Poly Haven 'Dirt' (Charlotte Baglioni, art/textures/2026-10-26-bayloop-terrain/sources/dirt_diff_1k.png).
Steps: remove baked light (divide by wide blur) -> keep grain -> add clod-scale value variation -> +-10% contrast -> neutral brown.
Writes only art/_review/2026-10-30-field_terrace/soil_albedo.png (+ soil_albedo.json, path_albedo.png).
Colour is deliberately left near-neutral warm brown; the material _Tint places it on the terrain's brown.
"""
from PIL import Image, ImageFilter
import numpy as np, json, pathlib
root = pathlib.Path(__file__).resolve().parents[3]
out = root / 'art/_review/2026-10-30-field_terrace'; out.mkdir(parents=True, exist_ok=True)
src = Image.open(root / 'art/textures/2026-10-26-bayloop-terrain/sources/dirt_diff_1k.png').convert('RGB')
a = np.asarray(src).astype(np.float64) / 255.0
lum = a @ np.array([.299, .587, .114])

def wrap_blur(x, s):  # tileable gaussian via FFT
    h, w = x.shape
    fy = np.fft.fftfreq(h)[:, None]; fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-2 * (np.pi ** 2) * (s ** 2) * (fx ** 2 + fy ** 2))
    return np.real(np.fft.ifft2(np.fft.fft2(x) * g))

flat = lum / np.maximum(wrap_blur(lum, 40), 1e-3)          # light removed, grain kept (mean ~1)
grain = (flat - 1)
grain /= max(grain.std(), 1e-6)                            # unit std

rng = np.random.default_rng(2610)
def clods(sigma, n):
    z = wrap_blur(rng.standard_normal((1024, 1024)), sigma)
    return z / z.std()
clod = .65 * clods(14, 0) + .45 * clods(5, 1) + .3 * clods(2, 2)   # tilled clump scale (~1.5-6 cm of the tile at 1024px/4m -> 5-60 cm)
clod /= clod.std()
v = 1 + .06 * grain + .07 * clod                           # total std ~ .092 => +-9-10 %
# tint inside the photo: keep a hint of its own colour variation, slim
chroma = a / np.maximum(lum[..., None], 1e-3)
chroma = (chroma - chroma.mean((0, 1))) * .5 + chroma.mean((0, 1))
base = np.array([.90, .74, .54])                           # warm brown, light-free
base = base / base @ np.array([.299, .587, .114])
alb = np.clip(v[..., None] * base * chroma / (chroma.mean((0, 1)) @ np.array([.299, .587, .114])) * .62, 0, 1)
im = Image.fromarray((alb * 255 + .5).astype(np.uint8)).resize((256, 256), Image.Resampling.LANCZOS)
im.save(out / 'soil_albedo.png')
# lighter packed path variant
path = np.clip(alb * np.array([1.18, 1.14, 1.06]), 0, 1)
Image.fromarray((path * 255 + .5).astype(np.uint8)).resize((256, 256), Image.Resampling.LANCZOS).save(out / 'path_albedo.png')
arr = np.asarray(im).astype(float)
L = arr @ np.array([.299, .587, .114])
info = {'source': 'art/textures/2026-10-26-bayloop-terrain/sources/dirt_diff_1k.png (Poly Haven "Dirt", Charlotte Baglioni, CC0-1.0; art/provenance.csv / dirt_metadata.json)',
        'method': 'flat-field divide (gauss sigma 40px) + grain + procedural clod noise, 256px tile = 4 m',
        'albedo_rgb_mean': arr.reshape(-1, 3).mean(0).round(1).tolist(),
        'luminance_std_pct': round(float(L.std() / L.mean() * 100), 2),
        'luminance_p5_p95_pct': [round(float(np.percentile(L, 5) / L.mean() * 100 - 100), 1), round(float(np.percentile(L, 95) / L.mean() * 100 - 100), 1)]}
(out / 'soil_albedo.json').write_text(json.dumps(info, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(info, ensure_ascii=False, indent=2))
