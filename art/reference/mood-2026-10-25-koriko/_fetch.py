import json, os, re, sys, time, urllib.parse, urllib.request, urllib.error
UA = 'PitCrewMoodBoard/1.0 (contact: local)'
ROOT = os.path.dirname(os.path.abspath(__file__))
CAND = os.path.join(ROOT, '_cand')
os.makedirs(CAND, exist_ok=True)
META = os.path.join(CAND, 'meta.json')
meta = json.load(open(META, encoding='utf-8')) if os.path.exists(META) else {}

def get(url, binary=False):
    for wait in (0, 8, 16, 32):
        if wait: time.sleep(wait)
        try:
            req = urllib.request.Request(url, headers={'User-Agent': UA})
            with urllib.request.urlopen(req, timeout=60) as r:
                d = r.read()
                return d if binary else json.loads(d)
        except urllib.error.HTTPError as e:
            print('  http', e.code, url[:80]); 
            if e.code != 429: return None
        except Exception as e:
            print('  err', e)
    return None

def strip(s):
    return re.sub(r'\s+', ' ', re.sub(r'<[^>]+>', '', s or '')).strip()

def run(group, query, keep=12):
    url = ('https://commons.wikimedia.org/w/api.php?action=query&format=json&generator=search&gsrnamespace=6'
           '&gsrsearch=' + urllib.parse.quote(query) + '&gsrlimit=30&prop=imageinfo&iiprop=url|size|extmetadata&iiurlwidth=1280')
    d = get(url); time.sleep(4)
    if not d or 'query' not in d: print(query, '0'); return
    pages = sorted(d['query']['pages'].values(), key=lambda p: p.get('index', 0))
    n = 0
    for p in pages:
        ii = (p.get('imageinfo') or [{}])[0]
        w, h = ii.get('width', 0), ii.get('height', 0)
        t = p['title']
        if not t.lower().endswith(('.jpg', '.jpeg')) or w < 1200 or w < h * 1.15: continue
        em = ii.get('extmetadata', {})
        key = re.sub(r'[^A-Za-z0-9]+', '_', group + '_' + query)[:40] + f'_{n:02d}'
        fn = os.path.join(CAND, key + '.jpg')
        if not os.path.exists(fn):
            b = get(ii['thumburl'], True); time.sleep(1.5)
            if not b: continue
            open(fn, 'wb').write(b)
        meta[key] = dict(group=group, query=query, title=t, page=ii.get('descriptionurl'), thumb=ii['thumburl'],
                         license=strip(em.get('LicenseShortName', {}).get('value')),
                         artist=strip(em.get('Artist', {}).get('value'))[:80],
                         desc=strip(em.get('ImageDescription', {}).get('value'))[:160],
                         date=strip(em.get('DateTimeOriginal', {}).get('value'))[:20])
        n += 1
        if n >= keep: break
    print(group, query, n, flush=True)
    json.dump(meta, open(META, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

if __name__ == '__main__':
    jobs = json.loads(sys.argv[1])
    for g, q in jobs: run(g, q)
