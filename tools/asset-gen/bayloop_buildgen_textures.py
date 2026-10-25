"""D140: eight plaster colours and localized wear in four shared CC0 atlases.

No bpy dependency. Existing metric UV layout (4 columns, 256px cells) remains
unchanged. select_style() is safe to import from Blender's bundled Python.
"""
from pathlib import Path
import json
import hashlib

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/models/bayloop_variety_v01'
COLORS = {
    'cream': (19, 14), 'chalk': (19, 14),
    'sand': (16, 0), 'ochre': (29, 30),
    'blue': (13, 19), 'mist': (13, 19),
    'rose': (10, 7), 'warm_grey': (17, 4),
}
COLOR_NAMES = tuple(COLORS)


def select_style(color='cream', wear='medium', shutter='green'):
    """Return one atlas and compatible b.TILES mapping; no file writes.

    Use plaster for regular walls and wear_wall only at plinths/thresholds or
    rainwater corners. Wear never changes the whole building to heavy damage.
    wood retains brown timber; teal chooses the requested shutter paint.
    """
    if color not in COLORS:
        raise ValueError('Unknown plaster colour: ' + str(color))
    if wear == 'normal':
        wear = 'medium'
    if wear not in ('light', 'medium', 'heavy'):
        raise ValueError('wear must be light, medium or heavy')
    if shutter not in ('green', 'brown'):
        raise ValueError('shutter must be green or brown')
    i = COLOR_NAMES.index(color)
    primary = (0, 10)[i % 2]
    damaged = (3, 15)[i % 2]
    ids = ['plaster', 'wood', 'wood_dark', 'roof', 'roof_clay', 'stone',
           'metal', 'glass', 'cloth', 'trim', 'rose_wall', 'teal',
           'stonewall', 'sign', 'rust', 'darkroof']
    tiles = dict(zip(ids, range(16)))
    tiles.update(plaster=primary, rose_wall=primary,
                 wear_wall=damaged if wear != 'light' else primary,
                 roof=4, darkroof=6, teal=11 if shutter == 'green' else 1)
    return {'atlas': str(OUT / ('BayLoopShared_%02d.png' % (i // 2 + 1))),
            'tiles': tiles, 'color': color, 'wear': wear, 'shutter': shutter,
            'wear_band_height_m': {'light': .12, 'medium': .25, 'heavy': .38}[wear],
            'palette_pair': list(COLORS[color])}


def main():
    import numpy as np
    from PIL import Image, ImageDraw, ImageFont
    OUT.mkdir(parents=True, exist_ok=True)
    old = ROOT / 'art/models/bayloop_buildings_v01'
    metadata = json.loads((old / 'texture-manifest.json').read_text(encoding='utf8'))
    palette = np.array(json.loads((ROOT / metadata['palette']).read_text())['colors'], dtype=np.uint8)
    base = np.asarray(Image.open(ROOT / 'art/models/bayloop_italy_v01/Italy_cream_Atlas.png').convert('RGB')).copy()
    plaster = Image.open(ROOT / 'art/textures/2026-10-24-kp-v2/original_reduced/plaster_photo_64ppm.png').convert('RGB').resize((64, 64), Image.Resampling.BOX)
    lum = np.asarray(plaster) @ np.array([.2126, .7152, .0722])
    # Raw photographic damage distribution, not per-pixel random wear.
    # Both normal and worn masks preserve the source photograph's local edges.
    # Use the actual photographic median rather than a sparse damage-only
    # tail. Small trim/parapet faces retain photographed tonal information.
    normal = lum < np.quantile(lum, .50)
    worn = lum < np.quantile(lum, .30)
    fontpath = ROOT / 'art/_review/2026-10-23-ui-final/fonts/Galmuri11.ttf'
    font = ImageFont.truetype(str(fontpath), 12)
    urls = sorted({t['source_url'] for t in metadata['tiles']})
    records = []
    checks = []
    atlases = []

    def cell(arr, index, patch):
        inner = np.tile(patch, (int(np.ceil(252 / patch.shape[0])), int(np.ceil(252 / patch.shape[1])), 1))[:252, :252]
        x, y = index % 4 * 256, index // 4 * 256
        arr[y:y+256, x:x+256] = np.pad(inner, ((2, 2), (2, 2), (0, 0)), mode='edge')

    for a in range(4):
        arr = base.copy()
        names = COLOR_NAMES[a*2:a*2+2]
        for n, name in enumerate(names):
            dark, light = COLORS[name]
            # Two lightness variants do not invent hues outside the shared
            # palette: chalk reverses whitewash's proportions; mist keeps the
            # close-value blue pair with its proportions reversed.
            normal_mask = ~normal if name in ('chalk', 'mist') else normal
            worn_mask = ~worn if name in ('chalk', 'mist') else worn
            for index, mask in [((0, 10)[n], normal_mask), ((3, 15)[n], worn_mask)]:
                patch = np.where(mask[:, :, None], palette[dark], palette[light])
                cell(arr, index, patch)
        # Brown and green shutters coexist, independently selectable per building.
        wood = base[0:256, 256:512]
        wlum = wood @ np.array([.2126, .7152, .0722])
        mask = wlum > (wlum.min() + wlum.max()) / 2
        arr[0:256, 256:512] = palette[[21, 24]][mask.astype(int)]
        # Galmuri is a native pixel font: integer size and binary glyph coverage.
        sign = Image.fromarray(base[0:256, 512:768].copy())
        d = ImageDraw.Draw(sign); d.fontmode = '1'
        for y, text in [(2, '\uc0c1\uc810'), (34, '\uc218\ub9ac'), (66, '\ubc30\ub2ec'), (98, '12')]:
            d.text((5, y), text, font=font, fill=tuple(map(int, palette[25])))
        arr[768:1024, 256:512] = np.asarray(sign)
        unique = set(map(tuple, np.unique(arr.reshape(-1, 3), axis=0)))
        assert unique <= set(map(tuple, palette))
        path = OUT / ('BayLoopShared_%02d.png' % (a + 1))
        Image.fromarray(arr).save(path)
        record = dict(asset_id='d140_' + path.stem, path=path.relative_to(ROOT).as_posix(),
                      source_url='; '.join(urls), license='CC0-1.0; Galmuri OFL-1.1',
                      accessed='2026-10-25', tool='bayloop_buildgen_textures.py; photo_to_pixel.py; bayloop_textures.py',
                      ai_generated='no', sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                      notes='Existing verified CC0 photographs at 32px/m; 8 shared-palette plaster ladders in 4 atlases; photograph-derived localized damage masks, no procedural noise; real Galmuri11 Korean labels; Point required')
        records.append(record)
        atlases.append({'path': record['path'], 'sha256': record['sha256'], 'colors': list(names), 'size': [1024, 1024], 'palette_colors': len(unique), 'outside_palette_colors': 0})
        counts = [len(np.unique(arr[j//4*256+2:j//4*256+254, j%4*256+2:j%4*256+254].reshape(-1, 3), axis=0)) for j in range(16)]
        checks.append(dict(path=record['path'], sha256=record['sha256'], size=[1024, 1024],
                           palette_colors=len(unique), outside_palette_colors=0,
                           all_cells_nonflat=all(v > 1 for v in counts), cell_color_counts=counts))
    plaster_source = dict(metadata['tiles'][0])
    for index, name in [(0, 'wall_primary'), (10, 'wall_secondary'), (3, 'wear_primary'), (15, 'wear_secondary')]:
        metadata['tiles'][index] = dict(plaster_source, id=name, index=index,
                                        atlas_rect_xywh=[index % 4 * 256 + 2, index // 4 * 256 + 2, 252, 252],
                                        palette_ladder='Per atlas / see colors',
                                        photographed_damage_fraction=.30 if index in (3, 15) else float(normal.mean()),
                                        threshold_method='photo 30th percentile' if index in (3, 15) else 'photo luminance median')
    metadata.update(atlases=atlases, colors={name: select_style(name) for name in COLORS},
                    color_family_note='Six muted palette families, plus two lightness variants: chalk is cream whitewash with reversed photographic coverage; mist is the blue family with reversed coverage. rose is muted warm-grey rose, not saturated pink: no suitable pale pink exists in the fixed shared palette.',
                    process='D140 reuses D137/A1 actual CC0 photographs; colour ladders and normal/worn local damage cells; four shared atlases; Galmuri11 OFL Korean signs',
                    tile_alias_notes='indices 3/15 repurposed to worn plaster; roof aliases 4 terracotta and 6 sheetmetal; per-building tiles mapping from select_style is authoritative',
                    wear_rule='Use wear_wall at ground splash band, threshold and rainwater corners only. light/normal/heavy changes band height, never stochastic whole-wall noise.')
    (OUT / 'texture-manifest.json').write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding='utf8')
    (OUT / 'texture-provenance-pending.json').write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf8')
    (OUT / 'texture-checks.json').write_text(json.dumps({'pass': all(r['all_cells_nonflat'] for r in checks),
        'atlas_count': 4, 'wall_colors': 8, 'rows': checks,
        'density_note': 'Source photo resampling remains 32 px/m; actual GLB UV density must be checked after model export. This report does not claim model check_asset PASS.'}, indent=2), encoding='utf8')
    # A non-AI raw texture proof sheet; it is not a visual architecture gate.
    board = Image.new('RGB', (1024, 640), (35, 31, 27))
    draw = ImageDraw.Draw(board)
    for i, name in enumerate(COLOR_NAMES):
        style = select_style(name)
        atlas = Image.open(style['atlas'])
        x, y = i % 4 * 256, i // 4 * 320
        idx = style['tiles']['plaster']
        board.paste(atlas.crop((idx % 4 * 256, idx // 4 * 256, idx % 4 * 256 + 256, idx // 4 * 256 + 256)), (x, y))
        draw.text((x+8, y+265), name + ' / 32 px per m', fill=(228, 214, 191))
        draw.text((x+8, y+284), 'photo-based, shared palette', fill=(228, 214, 191))
    board.save(OUT / 'texture-swatches.png')
    print(json.dumps({'atlases': 4, 'wall_colors': 8, 'size': '1024x1024', 'outside_palette_colors': 0, 'api': 'select_style(color, wear, shutter)'}, indent=2))


if __name__ == '__main__':
    main()
