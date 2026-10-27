"""CC0 건물 정면 사진 찾기·받기 (아트2, 과제 1331). 기본 python(표준 라이브러리 + PIL).
    python tools/asset-gen/opus_facade_fetch.py list            → ambientCG 'facade' 검색 결과 목록
    python tools/asset-gen/opus_facade_fetch.py get ID [ID ...]  → 2K-JPG zip 받아 Color 맵만 art/source/facades-cc0/ 에
라이선스: ambientCG 전 자산 CC0 1.0 (https://docs.ambientcg.com/license/). 원문은 받을 때 license.html 로 함께 저장.
"""
import io, json, os, sys, urllib.request, zipfile, hashlib

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); DST = os.path.join(R, 'art', 'source', 'facades-cc0'); os.makedirs(DST, exist_ok=True)
UA = {'User-Agent': 'Mozilla/5.0 (PitCrew art2 asset fetch)'}
get = lambda u: urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=60).read()
if sys.argv[1] == 'list':
    for q in sys.argv[2:] or ['facade']:
        d = json.loads(get(f'https://ambientcg.com/api/v2/full_json?q={q}&limit=100&include=tagData,displayData'))
        print('Q', q, d.get('numberOfResults'))
        for a in d['foundAssets']: print(a['assetId'], a.get('dataType'), '|', ','.join(a.get('tags', [])[:10]))
else:
    if not os.path.exists(os.path.join(DST, 'license.html')):
        open(os.path.join(DST, 'license.html'), 'wb').write(get('https://docs.ambientcg.com/license/'))
    for aid in sys.argv[2:]:
        url = f'https://ambientcg.com/get?file={aid}_2K-JPG.zip'
        z = zipfile.ZipFile(io.BytesIO(get(url)))
        col = [n for n in z.namelist() if n.lower().endswith(('_color.jpg', '_color.png'))]
        if not col: print('NOCOLOR', aid, z.namelist()); continue
        b = z.read(col[0]); p = os.path.join(DST, aid + '_Color.jpg'); open(p, 'wb').write(b)
        from PIL import Image
        print('GOT', aid, url, Image.open(p).size, hashlib.sha256(b).hexdigest())
