"""Validate review artifacts, mutated invalid sprites, and the local HTML board."""
from pathlib import Path
from PIL import Image
from html.parser import HTMLParser
from copy import deepcopy
import numpy as np
import json,sys,csv,hashlib,subprocess,tempfile,re,html

OUT=Path(__file__).resolve().parent;ROOT=OUT.parents[2]
sys.path.insert(0,str(ROOT/'tools/asset-gen'))
from check_ui_sprites import inspect_sprite,load_palette
def read(p):return json.loads(Path(p).read_text('utf-8'))
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
palette={tuple(c) for c in load_palette(ROOT/'art/source/palette/pitcrew_d123_32.json')}
specs=read(OUT/'sprites.json');screens=read(OUT/'screens.json')
with (ROOT/'art/provenance.csv').open(encoding='utf-8-sig',newline='') as f:rows=list(csv.DictReader(f))

# Mutation checks use isolated files and in-memory provenance; production CSV is never altered.
fixtures=OUT/'validation/negative-fixtures';fixtures.mkdir(exist_ok=True)
assert fixtures.resolve().is_relative_to(OUT.resolve())
tests=[]
base=next(s for s in specs if s['slot']=='paperPanel')
for kind,expected in [('palette','palette_outside_zero'),('grid','every_4px_cell_uniform'),('alpha','alpha_binary'),('size','existing_size_preserved'),('border','existing_border_preserved'),('hash','manifest_sha_matches_bytes'),('provenance','provenance_registered_non_ai'),('ai_flag','provenance_registered_non_ai')]:
    s=deepcopy(base);im=Image.open(ROOT/s['path']).convert('RGBA');px=np.array(im)
    if kind=='palette':px[32:36,32:36,:3]=[1,2,3]
    if kind=='grid':px[32,32,:3]=[48,54,47]
    if kind=='alpha':px[32:36,32:36,3]=128
    im=Image.fromarray(px)
    if kind=='size':im=im.crop((0,0,252,256))
    target=fixtures/(kind+'.png');im.save(target)
    s['path']=target.relative_to(ROOT).as_posix();s['sha256']=sha(target)
    if kind=='border':s['border_LBRT'][0]+=4
    if kind=='hash':s['sha256']='0'*64
    if kind=='ai_flag':s['ai_generated']=True
    fixture_row=deepcopy(next(r for r in rows if r['path']==base['path']))
    fixture_row.update(path=s['path'],sha256=sha(target))
    r=inspect_sprite(s,ROOT,palette,[] if kind=='provenance' else [fixture_row],fixtures)
    assert not r['pass'] and r['checks'][expected] is False,(kind,r)
    tests.append({'mutation':kind,'expected_rejection':expected,'rejected':True})
    target.unlink()  # Exact task-owned file only; no recursive deletion.
(OUT/'validation/negative-mutation-tests.json').write_text(json.dumps({'pass':True,'count':len(tests),'tests':tests},indent=2),encoding='utf-8')

results=[]
for s in screens:
    im=np.asarray(Image.open(OUT/s['path']));up=np.asarray(Image.open(OUT/s['zoom']))
    assert im.shape==(540,960,3) and up.shape==(1080,1920,3)
    assert np.array_equal(up,np.repeat(np.repeat(im,2,axis=0),2,axis=1))
    overlaps=[]
    for i,a in enumerate(s['text']):
        x,y,w,h=a['rect'];assert 0<=x and 0<=y and x+w<=960 and y+h<=540,(s['id'],a)
        for b in s['text'][:i]:
            xx,yy,ww,hh=b['rect']
            if min(x+w,xx+ww)>max(x,xx) and min(y+h,yy+hh)>max(y,yy):overlaps.append([a['text'],b['text']])
    assert not overlaps,(s['id'],overlaps)
    assert all(all(v%2==0 for v in p['rect']) for p in s['placements'])
    results.append({'screen':s['id'],'size':[960,540],'exact_nearest_2x':True,'text_bounds_overlap':overlaps,'sprite_placements_even':True,'sha256':sha(OUT/s['path'])})
# Quantified bar inspection after the independent review questioned the 50% bar.
packing=np.asarray(Image.open(OUT/'mockups/packing.png'))
bars=[]
for i,value in enumerate([85,50,45,45]):
    y=186+i*42+8
    width=int(np.sum(np.all(packing[y,746:854,:3]==[48,54,47],axis=1)))
    expected=max(8,2*round(108*value/100/2))
    assert width==expected,(i,width,expected)
    bars.append({'label':['앞','뒤','좌','우'][i],'value':value,'filled_pixels':width,'track_inner_pixels':108,'error_percentage_points':round(abs(width/108*100-value),3)})
patch=np.asarray(Image.open(OUT/'materials/patch-new-128.png'))
assert np.array_equal(patch,np.repeat(np.repeat(patch[::2,::2],2,axis=0),2,axis=1))
assert all(sha(ROOT/s['original'])==s['sha256'] for s in read(OUT/'sources.json'))

class Links(HTMLParser):
    def __init__(self):super().__init__();self.refs=[]
    def handle_starttag(self,tag,attrs):
        self.refs.extend(v for k,v in attrs if k in ('src','href') and v)
page=(OUT/'index.html').read_text('utf-8');parser=Links();parser.feed(page)
missing=[s for s in parser.refs if not s.startswith(('https:','http:','#')) and not (OUT/s.split('#')[0]).exists() and s!='validation/review.json']
assert not missing,missing
chrome=Path('C:/Program Files/Google/Chrome/Application/chrome.exe')
profile=Path(tempfile.gettempdir())/'pitcrew-ui-final-headless'
assert profile.resolve().parent==Path(tempfile.gettempdir()).resolve()
cmd=[str(chrome),'--headless','--disable-gpu','--no-first-run','--no-default-browser-check','--hide-scrollbars','--force-device-scale-factor=1','--allow-file-access-from-files','--user-data-dir='+str(profile),'--virtual-time-budget=2500']
probe='''<script>window.addEventListener('load',async()=>{await document.fonts.ready;let o={width:innerWidth,scrollWidth:document.documentElement.scrollWidth,images:document.images.length,broken:[...document.images].filter(x=>!x.complete||!x.naturalWidth).map(x=>x.src),fonts:document.fonts.status,galmuri:document.fonts.check('24px G'),pretendard:document.fonts.check('24px P'),native:[...document.querySelectorAll('.native')].map(x=>({width:x.getBoundingClientRect().width,naturalWidth:x.naturalWidth,naturalHeight:x.naturalHeight}))};let p=document.createElement('pre');p.id='browser-probe';p.textContent=JSON.stringify(o);document.body.append(p);});</script>'''
temp=OUT/'verify-page.html';browser=[]
try:
    temp.write_text(page.replace('</body>',probe+'</body>'),encoding='utf-8')
    for width in [1440,600]:
        p=subprocess.run(cmd+['--window-size='+str(width)+',1100','--dump-dom',temp.as_uri()],capture_output=True,timeout=50,creationflags=subprocess.CREATE_NO_WINDOW)
        m=re.search(r'<pre id="browser-probe">(.*?)</pre>',p.stdout.decode('utf-8',errors='replace'),re.S)
        assert p.returncode==0 and m,p.stderr[-500:]
        o=json.loads(html.unescape(m.group(1)))
        assert not o['broken'] and o['fonts']=='loaded' and o['galmuri'] and o['pretendard'] and o['scrollWidth']<=o['width'],o
        assert len(o['native'])==4 and all(x['width']==960 and x['naturalWidth']==960 and x['naturalHeight']==540 for x in o['native']),o
        browser.append(o)
    for name,excerpt in [('preview-top.png',False),('preview-screens.png',True)]:
        phtml=page
        if excerpt:
            phtml=page.split('<body>')[0]+'<body><main><section id="screens">'+page.split('<section id="screens">')[1].split('<section id="grain">')[0]+'</main></body></html>'
        temp.write_text(phtml,encoding='utf-8')
        p=subprocess.run(cmd+['--window-size=1440,1100','--screenshot='+str(OUT/'validation'/name),temp.as_uri()],capture_output=True,timeout=50,creationflags=subprocess.CREATE_NO_WINDOW)
        assert p.returncode==0
        assert len(Image.open(OUT/'validation'/name).convert('RGB').getcolors(2000000) or [])>50
finally:
    temp.unlink(missing_ok=True)
result={'pass':True,'scope':'Static PNGs + local headless Chrome. No Unity/Play.','mockups':results,'pixel_paper_patch_2x2':True,'bar_measurements':bars,'negative_tests':len(tests),'missing_links':missing,'source_hashes_match':True,'browser':browser,'not_run':['Unity import','TMP runtime raster equivalence','input/state transitions','game audio','release-build full AI audit']}
(OUT/'validation/review.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
