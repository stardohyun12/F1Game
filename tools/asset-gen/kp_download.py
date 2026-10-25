"""Download A1 source photos directly from the official Poly Haven API/CDN."""
import hashlib
import json
from pathlib import Path
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'art/textures/2026-10-24-kp'

def get(url):
    request = urllib.request.Request(url, headers={'User-Agent': 'PitCrew-A1-material-study/1.0 (Powered by Poly Haven)'})
    with urllib.request.urlopen(request, timeout=90) as response:
        return response.read()

def download(key, asset):
    folder = OUT / 'sources' / key
    folder.mkdir(parents=True, exist_ok=True)
    data = {}
    for endpoint in ['info', 'files']:
        raw = get(f'https://api.polyhaven.com/{endpoint}/{asset}')
        (folder / f'{asset}_{endpoint}.json').write_bytes(raw)
        data[endpoint] = json.loads(raw)
    info, files = data['info'], data['files']
    selected = files['Diffuse']['1k']['png']
    blob = get(selected['url'])
    expected = selected['md5'].lower()
    assert hashlib.md5(blob).hexdigest() == expected, 'Provider MD5 mismatch'
    source = folder / selected['url'].split('/')[-1]
    source.write_bytes(blob)
    metadata = {
        'id': asset, 'path': source.relative_to(ROOT).as_posix(),
        'url': f'https://polyhaven.com/a/{asset}', 'download_url': selected['url'],
        'license': 'CC0-1.0', 'license_url': 'https://polyhaven.com/license',
        'authors': info.get('authors'), 'dimensions_mm': info.get('dimensions'),
        'method': 'photography / photoscan; provider FAQ statement',
        'method_evidence_url': 'https://docs.polyhaven.com/en/faq#-do-you-use-photogrammetryphotoscanning',
        'sha256': hashlib.sha256(blob).hexdigest(), 'md5': expected,
        'provider_md5_match': True, 'accessed': '2026-10-24',
        'ai_generated': 'no', 'location': 'capture location unverified; material analogue only',
    }
    (OUT / f'source_{key}.json').write_text(json.dumps(metadata, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print(json.dumps(metadata, ensure_ascii=False))

if __name__ == '__main__':
    download('wood', 'wood_plank_wall')
    download('stone', 'stone_pavers')
    download('plaster', 'painted_plaster_wall')
    download('roof', 'clay_roof_tiles')
