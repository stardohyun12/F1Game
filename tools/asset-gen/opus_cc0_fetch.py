"""CC0 사진 재료 받기 (아트2, 2026-10-27 과제 0412). ambientCG 1K-JPG(색 맵만 씀) + Poly Haven HDRI 톤맵 JPG(팔레트용).
    python -X utf8 tools/asset-gen/opus_cc0_fetch.py thumbs <id...>   # 후보 128px 미리보기 한 장(contact.png)
    python -X utf8 tools/asset-gen/opus_cc0_fetch.py get <id...>      # 1K-JPG zip → *_Color.jpg 만 풀어 둠
    python -X utf8 tools/asset-gen/opus_cc0_fetch.py hdri <id...>     # Poly Haven 톤맵 JPG
받은 것마다 source.json(URL·라이선스·날짜)을 남긴다 → provenance.csv 는 텍스처 만들 때 기록.
"""
import io, json, os, sys, urllib.request, zipfile, datetime
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
DST = os.path.join(ROOT, 'art', 'source', 'photos', 'scene-a')
UA = {'User-Agent': 'Mozilla/5.0 (F1Game art pipeline)'}


def fetch(u):
    return urllib.request.urlopen(urllib.request.Request(u, headers=UA), timeout=120).read()


def thumbs(ids):
    W = 160; sh = Image.new('RGB', (W * 6, (W + 16) * ((len(ids) + 5) // 6)), (30, 30, 30)); d = ImageDraw.Draw(sh)
    for i, a in enumerate(ids):
        try: im = Image.open(io.BytesIO(fetch(f'https://acg-media.struffelproductions.com/file/ambientCG-Web/media/thumbnail/256-JPG-242424/{a}.jpg'))).convert('RGB').resize((W, W))
        except Exception as e: print('fail', a, e); continue
        x, y = (i % 6) * W, (i // 6) * (W + 16); sh.paste(im, (x, y + 16)); d.text((x + 3, y + 2), a, fill=(240, 240, 200))
    os.makedirs(DST, exist_ok=True); sh.save(os.path.join(DST, 'contact.png')); print('contact', len(ids))


def get(ids):
    today = datetime.date.today().isoformat()
    for a in ids:
        out = os.path.join(DST, a); os.makedirs(out, exist_ok=True)
        url = f'https://ambientcg.com/get?file={a}_1K-JPG.zip'
        z = zipfile.ZipFile(io.BytesIO(fetch(url)))
        names = [n for n in z.namelist() if n.endswith('_Color.jpg')]
        for n in names: open(os.path.join(out, os.path.basename(n)), 'wb').write(z.read(n))
        json.dump(dict(asset=a, download_url=url, page=f'https://ambientcg.com/view?id={a}', license='CC0-1.0', accessed=today, files=names),
                  open(os.path.join(out, 'source.json'), 'w'), indent=1)
        print('ok', a, names)


def hdri(ids):
    today = datetime.date.today().isoformat()
    for a in ids:
        out = os.path.join(DST, 'hdri'); os.makedirs(out, exist_ok=True)
        url = f'https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/{a}.jpg'
        open(os.path.join(out, a + '.jpg'), 'wb').write(fetch(url))
        json.dump(dict(asset=a, download_url=url, page=f'https://polyhaven.com/a/{a}', license='CC0-1.0', accessed=today),
                  open(os.path.join(out, a + '.source.json'), 'w'), indent=1)
        print('ok', a)


if __name__ == '__main__':
    {'thumbs': thumbs, 'get': get, 'hdri': hdri}[sys.argv[1]](sys.argv[2:])
