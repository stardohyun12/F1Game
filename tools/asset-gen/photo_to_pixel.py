"""CC0 photo -> physical-density patch -> exact shared-palette PNG.

The caller verifies licensing and records source-id in SOURCES.md. This tool
records provenance; it cannot determine a photograph's license from its pixels.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageOps, __version__ as pillow_version


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def load_palette(path):
    path = Path(path)
    if path.suffix.lower() == '.gpl':
        rows = []
        for line in path.read_text(encoding='utf-8-sig').splitlines():
            parts = line.split()
            if len(parts) >= 3 and all(x.isdigit() for x in parts[:3]):
                rows.append([int(x) for x in parts[:3]])
    else:
        payload = json.loads(path.read_text(encoding='utf-8-sig'))
        rows = payload.get('colors') if isinstance(payload, dict) else payload
    colors = []
    for row in rows:
        if isinstance(row, dict):
            row = row.get('rgb', row.get('hex'))
        if isinstance(row, str):
            h = row.lstrip('#')
            if len(h) != 6:
                raise ValueError('Palette hex entries must have 6 digits')
            row = [int(h[i:i+2], 16) for i in (0, 2, 4)]
        if len(row) != 3 or any(type(c) is not int or not 0 <= c <= 255 for c in row):
            raise ValueError('Palette must contain RGB integer triples in 0..255')
        color = tuple(row)
        if color not in colors:
            colors.append(color)
    if not 1 <= len(colors) <= 32:
        raise ValueError('Shared palette requires 1..32 unique colors')
    return colors


def quantize_exact(image, colors, dither='none'):
    """Squared RGB nearest color; optional left-to-right Floyd-Steinberg.

    Direct palette lookup avoids extra padded palette entries or approximate
    nearest-color caching. Tie-breaking follows palette order deterministically.
    """
    width, height = image.size
    work = [[float(c) for c in pixel] for pixel in image.convert('RGB').getdata()]
    result = []
    for y in range(height):
        for x in range(width):
            pos = y * width + x
            pixel = [min(255.0, max(0.0, c)) for c in work[pos]]
            color = min(colors, key=lambda q: sum((pixel[i]-q[i])**2 for i in range(3)))
            result.append(color)
            if dither == 'floyd-steinberg':
                error = [pixel[i]-color[i] for i in range(3)]
                for dx, dy, weight in ((1, 0, 7/16), (-1, 1, 3/16), (0, 1, 5/16), (1, 1, 1/16)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < width and 0 <= ny < height:
                        target = work[ny*width+nx]
                        for channel in range(3):
                            target[channel] += error[channel] * weight
    out = Image.new('RGB', image.size)
    out.putdata(result)
    return out


def convert(args):
    for name in ('width_m', 'height_m', 'density'):
        value = getattr(args, name)
        if not math.isfinite(value) or value <= 0:
            raise ValueError(f'{name} must be finite and positive')
    if not args.source_id.strip():
        raise ValueError('source-id must refer to an entry in SOURCES.md')
    colors = load_palette(args.palette)
    width = max(1, math.floor(args.width_m * args.density + 0.5))
    height = max(1, math.floor(args.height_m * args.density + 0.5))
    if width * height > 4_194_304:
        raise ValueError('Patch exceeds 4 megapixels; check physical dimensions and density')
    if args.canvas_size is not None:
        size = args.canvas_size
        if size < 1 or size & (size-1) or max(width, height) > size:
            raise ValueError('Canvas must be a power of two and contain the patch without scaling')
    output = Path(args.output)
    metadata_path = Path(args.metadata) if args.metadata else output.with_suffix('.json')
    protected = {Path(args.input).resolve(), Path(args.palette).resolve()}
    if output.resolve() in protected or metadata_path.resolve() in protected or output.resolve() == metadata_path.resolve():
        raise ValueError('Output and metadata must not overwrite input, palette, or each other')
    with Image.open(args.input) as raw:
        photo = ImageOps.exif_transpose(raw).convert('RGB')
        source_size = photo.size
        crop = list(args.crop) if args.crop else [0, 0, *photo.size]
        left, top, right, bottom = crop
        if not (0 <= left < right <= photo.width and 0 <= top < bottom <= photo.height):
            raise ValueError('Crop must be within the EXIF-oriented photograph')
        if right-left < width or bottom-top < height:
            raise ValueError('Input crop is smaller than patch: provide a larger source; no upscaling')
        patch = photo.crop(tuple(crop)).resize((width, height),
            Image.Resampling.BOX if args.resample == 'box' else Image.Resampling.LANCZOS)
    patch = quantize_exact(patch, colors, args.dither)
    out = patch
    if args.canvas_size:
        out = Image.new('RGB', (args.canvas_size, args.canvas_size), colors[0])
        out.paste(patch, (0, 0))
    output.parent.mkdir(parents=True, exist_ok=True)
    out.save(output, format='PNG', optimize=False)
    actual = [width/args.width_m, height/args.height_m]
    metadata = {
        'schema_version': 1, 'tool': 'photo_to_pixel.py', 'pillow_version': pillow_version,
        'source': {'id': args.source_id, 'path': str(Path(args.input)), 'sha256': sha256(args.input),
                   'url': args.source_url, 'license_url': args.license_url,
                   'license_verification': 'caller responsibility; see SOURCES.md',
                   'oriented_size_px': source_size, 'crop_ltrb_px': crop},
        'palette': {'path': str(Path(args.palette)), 'sha256': sha256(args.palette), 'colors_rgb': colors},
        'physical_size_m': [args.width_m, args.height_m], 'target_density_px_m': args.density,
        'actual_density_px_m': actual,
        'density_deviation_fraction': [abs(d/args.density-1) for d in actual],
        'patch_size_px': [width, height], 'patch_rect_xywh': [0, 0, width, height],
        'output_size_px': out.size, 'output_sha256': sha256(output),
        'resample': args.resample, 'dither': args.dither, 'quantization': 'exact squared RGB distance',
        'sampling_intent': 'Point/Closest; configure in engine/material; PNG cannot enforce filtering',
    }
    metadata_path.parent.mkdir(parents=True, exist_ok=True)
    metadata_path.write_text(json.dumps(metadata, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    return metadata


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--palette', required=True, type=Path)
    parser.add_argument('--width-m', required=True, type=float)
    parser.add_argument('--height-m', required=True, type=float)
    parser.add_argument('--density', type=float, default=32)
    parser.add_argument('--dither', choices=['none', 'floyd-steinberg'], default='none')
    parser.add_argument('--resample', choices=['box', 'lanczos'], default='box')
    parser.add_argument('--source-id', required=True)
    parser.add_argument('--source-url')
    parser.add_argument('--license-url')
    parser.add_argument('--crop', type=int, nargs=4, metavar=('LEFT', 'TOP', 'RIGHT', 'BOTTOM'))
    parser.add_argument('--canvas-size', type=int)
    parser.add_argument('--metadata', type=Path)
    args = parser.parse_args()
    try:
        result = convert(args)
    except (ValueError, OSError, TypeError) as exc:
        parser.error(str(exc))
    print(json.dumps({'output': str(args.output), 'patch_size_px': result['patch_size_px'],
                      'actual_density_px_m': result['actual_density_px_m']}, ensure_ascii=False))


if __name__ == '__main__':
    main()
