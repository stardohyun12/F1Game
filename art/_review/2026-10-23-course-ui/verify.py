"""Static course UI review checks; no Unity, mesh import, or runtime claims."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
from html.parser import HTMLParser
import numpy as np
import csv,json,math,hashlib,subprocess,tempfile,re,html,sys
OUT=Path(__file__).resolve().parent;ROOT=OUT.parents[2]
def read(p):return json.loads(Path(p).read_text('utf-8'))
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
sys.path.insert(0,str(ROOT/'tools/asset-gen'))
from photo_to_pixel import load_palette
palette={tuple(c) for c in load_palette(ROOT/'art/source/palette/pitcrew_d123_32.json')}
signs=read(OUT/'signs.json');screens=read(OUT/'screens.json');samples=read(OUT/'readability.json')
with (ROOT/'art/provenance.csv').open(encoding='utf-8-sig',newline='') as f:provenance=list(csv.DictReader(f))
results=[]
font=ImageFont.truetype(str(OUT/'fonts/Galmuri11.ttf'),12)
assert len(signs)==7 and {x['id'] for x in signs}=={x.stem for x in (OUT/'textures').glob('*.png')}
for s in signs:
    p=ROOT/s['path'];im=Image.open(p);px=np.asarray(im);w,h=im.size;aw,ah=s['active_xywh'][2:]
    assert p.resolve().is_relative_to((OUT/'textures').resolve()) and im.mode=='RGBA'
    assert list(im.size)==s['size_px'] and all(v>0 and v&(v-1)==0 for v in im.size)
    visible=px[:,:,3]>0;used={tuple(map(int,c)) for c in px[:,:,:3][visible]}
    assert not used-palette and set(np.unique(px[:,:,3]))<={0,255}
    assert np.all(px[:ah,:aw,3]==255) and np.all(px[ah:,:,3]==0) and np.all(px[:,aw:,3]==0)
    assert np.allclose([aw/s['physical_m'][0],ah/s['physical_m'][1]],[32,32])
    assert s['uv_bounds_bl']==[0,1-ah/h,aw/w,1] and s['letter_density_exception'] is False
    a=Image.open(OUT/'layers'/f'{s["id"]}-base.png');b=Image.open(OUT/'layers'/f'{s["id"]}-letters.png')
    assert Image.alpha_composite(a,b).tobytes()==im.tobytes()
    masks=[];bbox_overlap=[]
    for i,l in enumerate(s['letters']):
        x,y,ww,hh=l['rect_texels'];assert x>=0 and y>=0 and x+ww<=aw and y+hh<=ah
        fb=font.getbbox(l['text']);m=Image.new('1',(fb[2]-fb[0],fb[3]-fb[1]));ImageDraw.Draw(m).text((-fb[0],-fb[1]),l['text'],font=font,fill=1)
        mask=Image.new('1',(w,h));mask.paste(m.resize((ww,hh),Image.Resampling.NEAREST),(x,y));ink=np.asarray(mask)
        assert all(not np.any(ink&other) for other in masks),(s['id'],'actual glyph ink collision')
        masks.append(ink)
        for q in s['letters'][:i]:
            xx,yy,qw,qh=q['rect_texels']
            if min(x+ww,xx+qw)>max(x,xx) and min(y+hh,yy+qh)>max(y,yy):bbox_overlap.append([q['text'],l['text']])
    rows=[r for r in provenance if r['path']==s['path']]
    assert len(rows)==1 and rows[0]['ai_generated']=='no' and rows[0]['sha256']==sha(p)==s['sha256']
    assert all(rows[0][k] for k in ['source_url','license','tool']) and s['ai_generated'] is False
    assert sha(OUT/'fonts/Galmuri11.ttf')==s['font_sha256']
    results.append({'id':s['id'],'pass':True,'png':list(im.size),'active_texels':[aw,ah],'declared_active_uv_density':[32,32],'outside_palette_colors':0,'glyph_ink_overlap':False,'glyph_bbox_overlap_only':bbox_overlap,'layer_composite_matches':True,'provenance_sha_matches':True,'sha256':sha(p)})
mockups=[]
assert len(screens)==10
for s in screens:
    im=Image.open(OUT/s['path']);zoom=Image.open(OUT/s['zoom']);ui=Image.open(OUT/s['overlay'])
    assert im.size==ui.size==(960,540) and zoom.size==(1920,1080)
    assert zoom.tobytes()==im.resize((1920,1080),Image.Resampling.NEAREST).tobytes() and sha(OUT/s['path'])==s['sha256']
    a=np.asarray(ui)[:,:,3]
    if s['id'].startswith('outbound'):
        assert not np.any(a[108:432,192:768]) and s['envelope_percent']<8
    if s['id']=='return-quiet':assert not np.any(a) and s['envelope_percent']==0
    if s['id']=='a-slip':assert not np.any(a[190:540,:])
    mockups.append({'id':s['id'],'pass':True,'exact_nearest_2x':True,'envelope_percent':s['envelope_percent'],'sha256':sha(OUT/s['path'])})
# Independently evaluate front-on height, and sample placement contracts.
cal=[]
assert len(samples)==42 and len({(s['id'],s['case'],s['car_distance_m']) for s in samples})==42
for s in samples:
    assert Image.open(OUT/s['path']).size==(960,540)
    spec=next(x for x in signs if x['id']==s['id'])
    if s['case']=='base':
        expected=540*spec['physical_m'][1]/(2*s['camera_depth_m']*math.tan(math.radians(s['vertical_fov_deg']/2)))
        measured=s['projected_quad_px'][3][1]-s['projected_quad_px'][0][1]
        assert abs(expected-measured)<1e-10
        cal.append({'id':s['id'],'distance':s['car_distance_m'],'expected_height_px':expected,'calculated_quad_height_px':measured})
    assert all(m['min_body_height_px']>0 for m in s['text_measurements'])
sources=[]
for s in read(OUT/'sources.json'):
    target=ROOT/s.get('snapshot',s['source']);assert sha(target)==s['sha256'],s
    sources.append({'source':s['source'],'snapshot_hash_matches':True,'original_still_matches':sha(ROOT/s['source'])==s['sha256']})
gate=read(OUT/'validation/independent-gate.json')
assert gate['score_threshold_met'] is True and gate['all_yes'] is False
assert all(sha(OUT/x['path'])==x['sha256'] for x in gate['reviewed_final_files'])

class Links(HTMLParser):
    def __init__(self):super().__init__();self.refs=[]
    def handle_starttag(self,tag,attrs):self.refs.extend(v for k,v in attrs if k in ('href','src') and v)
page=(OUT/'index.html').read_text('utf-8');parser=Links();parser.feed(page)
missing=[x for x in parser.refs if not x.startswith(('http:','https:','#')) and not (OUT/x.split('#')[0]).exists() and x!='validation/review.json']
assert not missing,missing
chrome=Path('C:/Program Files/Google/Chrome/Application/chrome.exe')
profile=Path(tempfile.gettempdir())/'pitcrew-course-ui-headless'
cmd=[str(chrome),'--headless','--disable-gpu','--no-first-run','--no-default-browser-check','--hide-scrollbars','--force-device-scale-factor=1','--allow-file-access-from-files','--user-data-dir='+str(profile),'--virtual-time-budget=2500']
probe='''<script>window.addEventListener('load',async()=>{await document.fonts.ready;let o={width:innerWidth,scrollWidth:document.documentElement.scrollWidth,images:document.images.length,broken:[...document.images].filter(x=>!x.complete||!x.naturalWidth).map(x=>x.src),fonts:document.fonts.status,galmuri:document.fonts.check('24px G'),pretendard:document.fonts.check('24px P'),native:[...document.querySelectorAll('.native')].map(x=>({width:x.getBoundingClientRect().width,naturalWidth:x.naturalWidth,naturalHeight:x.naturalHeight}))};document.getElementById('sign').value='mart';document.getElementById('case').value='base';document.getElementById('distanceSelect').value='10';document.getElementById('sign').dispatchEvent(new Event('change'));o.selectorPath=document.getElementById('distanceImage').getAttribute('src');let p=document.createElement('pre');p.id='browser-probe';p.textContent=JSON.stringify(o);document.body.append(p);});</script>'''
temp=OUT/'verify-page.html';browser=[]
try:
    temp.write_text(page.replace('</body>',probe+'</body>'),encoding='utf-8')
    for width in [1440,600]:
        p=subprocess.run(cmd+['--window-size='+str(width)+',1100','--dump-dom',temp.as_uri()],capture_output=True,timeout=50,creationflags=subprocess.CREATE_NO_WINDOW)
        m=re.search(r'<pre id="browser-probe">(.*?)</pre>',p.stdout.decode('utf-8',errors='replace'),re.S)
        assert p.returncode==0 and m,p.stderr[-500:]
        o=json.loads(html.unescape(m.group(1)))
        assert not o['broken'] and o['fonts']=='loaded' and o['galmuri'] and o['pretendard'] and o['scrollWidth']<=o['width'],o
        assert len(o['native'])==11 and all(x['width']==960 and x['naturalWidth']==960 and x['naturalHeight']==540 for x in o['native']),o
        assert o['selectorPath']=='readability/mart-base-10m.png'
        browser.append(o)
    temp.write_text(page,encoding='utf-8')
    p=subprocess.run(cmd+['--window-size=1440,1100','--screenshot='+str(OUT/'validation/board-preview.png'),temp.as_uri()],capture_output=True,timeout=50,creationflags=subprocess.CREATE_NO_WINDOW)
    assert p.returncode==0
finally:temp.unlink(missing_ok=True)
result={'pass':True,'scope':'Static PNG/declared UV contract, existing screenshot composites, mathematical projection and local browser only. No Unity or actual mesh density/import/Play.', 'world_signs':results,'mockups':mockups,'distance_samples':42,'front_projection_calibration':cal,'source_snapshots':sources,'independent_score_threshold_met':True,'independent_all_yes':False,'independent_issue':'20m small warning/copy remains unreadable on world signs alone','browser':browser,'missing_links':missing,'not_run':['Unity import','actual mesh UV density','real driving readability','TMP equivalence','animation/audio','return state transition','release build full AI audit']}
(OUT/'validation/review.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'pass':True,'world_signs':len(results),'mockups':len(mockups),'distance_samples':42,'browser_widths':[x['width'] for x in browser],'independent_all_yes':False},ensure_ascii=False))
