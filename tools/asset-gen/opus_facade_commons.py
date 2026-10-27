"""Wikimedia Commons 에서 CC0·퍼블릭 도메인 건물 정면 사진 찾기·받기 (아트2, 과제 1331). 기본 python.
    python tools/asset-gen/opus_facade_commons.py search "검색어" [...]   → 구조화 데이터 라이선스(P275)=CC0(Q6938433) 인 파일만 + 라이선스 표기 확인
    python tools/asset-gen/opus_facade_commons.py get "File:..." [...]    → 가로 1600px 판을 art/source/facades-cc0/ 에, 메타는 commons_meta.json 에
"""
import hashlib, json, os, sys, time, urllib.parse, urllib.request

R = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); DST = os.path.join(R, 'art', 'source', 'facades-cc0'); os.makedirs(DST, exist_ok=True)
UA = {'User-Agent': 'PitCrewArt2/1.0 (stardohyun11 project asset provenance check)'}
API = 'https://commons.wikimedia.org/w/api.php?'
get = lambda u: time.sleep(2) or urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=60).read()
api = lambda **p: json.loads(get(API + urllib.parse.urlencode(dict(p, format='json'))))
OK = ('cc0', 'public domain', 'pd')


def info(titles, width=None):
    p = dict(action='query', titles='|'.join(titles), prop='imageinfo', iiprop='url|size|extmetadata|sha1')
    if width: p['iiurlwidth'] = width
    return api(**p)['query']['pages'].values()


if sys.argv[1] == 'search':
    for q in sys.argv[2:]:
        r = api(action='query', list='search', srnamespace=6, srlimit=40, srsearch=q + ' haswbstatement:P275=Q6938433')
        ts = [s['title'] for s in r['query']['search']]; print('Q', q, len(ts))
        for i in range(0, len(ts), 20):
            for pg in info(ts[i:i + 20]):
                ii = pg.get('imageinfo', [{}])[0]; em = ii.get('extmetadata', {})
                lic = em.get('LicenseShortName', {}).get('value', '?')
                print(' ', lic, ii.get('width'), ii.get('height'), pg['title'])
else:
    meta_p = os.path.join(DST, 'commons_meta.json'); meta = json.load(open(meta_p, encoding='utf-8')) if os.path.exists(meta_p) else {}
    for pg in info(sys.argv[2:], 1600):
        ii = pg['imageinfo'][0]; em = ii.get('extmetadata', {}); lic = em.get('LicenseShortName', {}).get('value', '?')
        if not lic.lower().startswith(OK): print('SKIP license', lic, pg['title']); continue
        b = get(ii.get('thumburl') or ii['url']); name = 'commons_' + ''.join(c if c.isalnum() else '_' for c in pg['title'][5:].rsplit('.', 1)[0])[:60] + '.jpg'
        open(os.path.join(DST, name), 'wb').write(b)
        meta[name] = dict(title=pg['title'], page=ii.get('descriptionurl'), original=ii['url'], license=lic, license_url=em.get('LicenseUrl', {}).get('value', ''),
                          artist=em.get('Artist', {}).get('value', ''), sha256=hashlib.sha256(b).hexdigest(), size=[ii.get('thumbwidth'), ii.get('thumbheight')])
        print('GOT', lic, name, meta[name]['size'])
    json.dump(meta, open(meta_p, 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
