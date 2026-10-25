"""Guarded A1 second-study handoff: trial PNGs only, never Unity/meta/atlas."""
from pathlib import Path
import csv, hashlib, json, shutil
from PIL import Image, ImageDraw

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/textures/2026-10-24-kp-v2'
def read(p): return json.loads(p.read_text(encoding='utf-8'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def rel(p): return p.relative_to(ROOT).as_posix()
def write(p,d): p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def main():
    m=read(OUT/'manifest.json'); checked=read(OUT/'check_asset.json')
    assert m['revision']==2 and checked['pass']
    assert checked['manifest_sha256']==sha(OUT/'manifest.json')
    before=read(OUT/'trial-before.json')['files']
    assert not (OUT/'trial-replacement.json').exists(), 'Already handed off; do not overwrite history'
    for f in before:
        assert sha(ROOT/f['path'])==f['sha256'], 'Concurrent trial change: '+f['path']
        if 'backup' in f: assert sha(ROOT/f['backup'])==f['sha256']
    # Preserve both reviewed editions before replacing any trial PNG.
    archive=OUT/'iterations/r2';archive.mkdir(parents=True,exist_ok=False)
    for name in ['albedo','original_reduced','baseline','proof','review']:
        shutil.copytree(OUT/name,archive/name)
    for name in ['manifest.json','check_asset.json','technical-audit.json','technical-audit.md','visual-gate-r2.md','compare-4x.png','compare-full-4x.png']:
        shutil.copy2(OUT/name,archive/name)
    write(archive/'snapshot-hashes.json',{p.relative_to(archive).as_posix():sha(p) for p in archive.rglob('*') if p.is_file()})
    prov=ROOT/'art/provenance.csv'
    with prov.open(encoding='utf-8-sig',newline='') as f:
        reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
    # Register every considered raw photograph, retaining existing unique source rows.
    sources=[]
    for p in OUT.glob('source_*.json'):
        data=read(p);sources.extend(data['candidates'] if 'candidates' in data else [data])
    for s in sources:
        if any(r['path']==s['path'] for r in rows):continue
        assert sha(ROOT/s['path'])==s['sha256']
        rows.append(dict(asset_id='a1_kp2_candidate_'+s['id'],path=s['path'],source_url=s['url'],license='CC0-1.0',accessed='2026-10-24',tool='kp_download.py; photo_to_pixel.py',ai_generated='no',sha256=s['sha256'],notes='Official raw diffuse photograph candidate; provider checksum verified; selection notes in SOURCES.md and ROOF-CANDIDATES.md'))
    result=[]
    for t in m['tiles']:
        src=ROOT/t['albedo'];dest=ROOT/'F1/Assets/Art/ProtoCourse/Textures'/src.name
        previous=next(f for f in before if f['path']==rel(dest))
        changed=sha(src)!=sha(dest)
        if changed:
            matches=[r for r in rows if r['path']==rel(dest)]
            assert len(matches)==1 and matches[0]['sha256']==previous['sha256']
            matches[0]['path']=previous['backup']
            matches[0]['notes']+='; Previous trial archived before PD 20261024-0308 replacement'
            sources=[t['source']]+t.get('additional_sources',[])
            rows.append(dict(asset_id=f'a1_kp2_{t["id"]}_trial_copy',path=rel(dest),source_url='; '.join(s['url'] for s in sources),license='CC0-1.0',accessed='2026-10-24',tool='kp_v2_handoff.py; photo_to_pixel.py; kp_textures_v2.py',ai_generated='no',sha256=sha(src),notes='Checked albedo byte-identical trial PNG replacement only; Unity not run; '+ '; '.join(t['process'])))
            shutil.copyfile(src,dest)
        assert sha(src)==sha(dest)
        result.append({'id':t['id'],'path':rel(dest),'replaced':changed,'sha256':sha(dest),'byte_identical':True})
    with prov.open('w',encoding='utf-8',newline='') as f:
        w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
    metas=[]
    for f in before:
        if f['path'].endswith('.meta'):
            assert sha(ROOT/f['path'])==f['sha256']
            metas.append({'path':f['path'],'sha256':f['sha256'],'unchanged':True})
    write(OUT/'trial-replacement.json',{'selected_revision':2,'files':result,'metas':metas,'unity_run':False,'scene_code_atlas_modified':False})
    # Comparison is composed exclusively of actual Blender renders and text.
    canvas=Image.new('RGB',(1920,580),(238,234,225));d=ImageDraw.Draw(canvas)
    for x,label,p in [(0,'A1 0136 / 6.0',ROOT/'art/textures/2026-10-24-kp/review/material-stage-960.png'),(960,'A1 0308 / R2 / 7.0',OUT/'review/material-stage-960.png')]:
        d.text((x+18,15),label,fill=(45,43,37));canvas.paste(Image.open(p).convert('RGB'),(x,40))
    canvas.save(OUT/'review/before-after.png')
    print(json.dumps({'trial_png_replaced':sum(t['replaced'] for t in result),'metas_unchanged':len(metas),'archive':'iterations/r2'},ensure_ascii=False))

if __name__=='__main__':main()
