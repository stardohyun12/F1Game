"""Fetch original CC0 diffuse maps and preserve provider metadata, without alteration."""
from pathlib import Path
import hashlib
import json
import urllib.request
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[5]
OUT = ROOT / 'art/textures/2026-10-24-kp'
UA = {'User-Agent': 'PitCrewAssetReview/1.0 (CC0 texture study)'}

def fetch(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60) as r:
        return r.read()

for material, asset in [('wood', 'wood_plank_wall'), ('stone', 'stone_pavers')]:
    target = OUT / 'sources' / material
    target.mkdir(parents=True, exist_ok=True)
    info_url = f'https://api.polyhaven.com/info/{asset}'
    files_url = f'https://api.polyhaven.com/files/{asset}'
    info_raw, files_raw = fetch(info_url), fetch(files_url)
    (target / f'{asset}_info.json').write_bytes(info_raw)
    (target / f'{asset}_files.json').write_bytes(files_raw)
    info, files = json.loads(info_raw), json.loads(files_raw)
    data = files.get('Diffuse', files.get('diff'))['1k']['png']
    blob = fetch(data['url'])
    md5 = hashlib.md5(blob).hexdigest()
    assert md5 == data['md5'], (asset, md5, data['md5'])
    path = target / data['url'].split('/')[-1]
    path.write_bytes(blob)
    record = {
        'id': asset,
        'provider': 'Poly Haven',
        'path': path.relative_to(ROOT).as_posix(),
        'url': f'https://polyhaven.com/a/{asset}',
        'download_url': data['url'],
        'info_url': info_url,
        'files_url': files_url,
        'license': 'CC0-1.0',
        'license_url': 'https://polyhaven.com/license',
        'authors': info['authors'],
        'dimensions_mm': info['dimensions'],
        'method': 'Photographic texture source; provider states all texture materials use photogrammetry or photometric stereo, not procedural generation or generative AI. Per-asset scan subtype not exposed in info metadata.',
        'method_evidence': ['https://docs.polyhaven.com/en/faq#-do-you-use-photogrammetryphotoscanning', 'https://docs.polyhaven.com/en/technical-standards/textures'],
        'ai_generated': False,
        'sha256': hashlib.sha256(blob).hexdigest(),
        'md5': md5,
        'provider_md5': data['md5'],
        'provider_md5_verified': True,
        'bytes': len(blob),
        'accessed': '2026-10-24',
        'downloaded_utc': datetime.now(timezone.utc).isoformat(),
        'processing': ['Direct 1k PNG diffuse download; original bytes preserved.'],
        'geography': 'Source capture location not established; selected for material suitability, not claimed to depict Kyoto.',
    }
    (OUT / f'source_{material}.json').write_text(json.dumps(record, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(material, path, info['dimensions'], record['sha256'], 'provider MD5 PASS')
