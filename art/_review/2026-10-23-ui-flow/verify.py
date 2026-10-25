"""Static artifact checks and headless browser preview; does not launch Unity."""
from pathlib import Path
from PIL import Image, ImageChops
from html.parser import HTMLParser
import json,re,subprocess,tempfile,hashlib,html

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
def read(name):return json.loads((OUT/name).read_text('utf-8'))
class Links(HTMLParser):
    def __init__(self):super().__init__();self.refs=[]
    def handle_starttag(self,tag,attrs):
        for k,v in attrs:
            if k in ('src','href') and v:self.refs.append(v)

parser=Links();page=(OUT/'index.html').read_text('utf-8');parser.feed(page)
missing=[]
for s in parser.refs:
    if s.startswith(('https:','http:','#')):continue
    target=(OUT/s.split('#')[0]).resolve()
    if not target.exists() and s!='validation.json':missing.append(s)
assert not missing,missing
audits=read('audits.json');wires=read('wireframes.json');hud=read('mockups.json')
assert len(audits)==5 and all(len(a['issues'])<=5 for a in audits)
assert len(wires)==6 and all(len(w['essentials'])==3 for w in wires)
assert len(hud)==4
checks=[]
palette={tuple(c) for c in json.loads((ROOT/'art/source/palette/pitcrew_d123_32.json').read_text('utf-8'))['colors']}
for h in hud:
    im=Image.open(OUT/h['image']);ov=Image.open(OUT/h['overlay']).convert('RGBA')
    assert im.size==(960,540) and ov.size==(960,540)
    assert ov.getchannel('A').crop((192,108,768,432)).getbbox() is None
    colors={c[:3] for c in ov.get_flattened_data() if c[3]>0}
    assert colors<=palette
    for scale in [2,3]:
        up=im.resize((960*scale,540*scale),Image.Resampling.NEAREST)
        assert ImageChops.difference(im,up.resize((960,540),Image.Resampling.NEAREST)).getbbox() is None
    checks.append({'file':h['image'],'size':list(im.size),'overlay_palette_outside':len(colors-palette),'central_hud_pixels':0,'nearest_2x_3x_roundtrip':'pass','sha256':hashlib.sha256((OUT/h['image']).read_bytes()).hexdigest()})
source_changes=[]
for it in read('sources.json')['items']:
    if hashlib.sha256((ROOT/it['path']).read_bytes()).hexdigest()!=it['sha256']:source_changes.append(it['path'])

chrome=Path('C:/Program Files/Google/Chrome/Application/chrome.exe')
profile=Path(tempfile.gettempdir())/'pitcrew-ui-flow-headless'
assert profile.resolve().parent==Path(tempfile.gettempdir()).resolve()
base=[str(chrome),'--headless','--disable-gpu','--no-first-run','--no-default-browser-check','--hide-scrollbars','--force-device-scale-factor=1','--allow-file-access-from-files','--user-data-dir='+str(profile),'--virtual-time-budget=3000']
probe='''<script>window.addEventListener('load',async()=>{await document.fonts.ready;const imgs=[...document.images];let o={width:innerWidth,scrollWidth:document.documentElement.scrollWidth,images:imgs.length,broken:imgs.filter(x=>!x.complete||!x.naturalWidth).map(x=>x.getAttribute('src')),fonts:document.fonts.status,galmuri:document.fonts.check('24px G'),pretendard:document.fonts.check('24px P'),native:[...document.querySelectorAll('.native')].map(x=>({width:x.getBoundingClientRect().width,naturalWidth:x.naturalWidth,naturalHeight:x.naturalHeight}))};let p=document.createElement('pre');p.id='browser-probe';p.textContent=JSON.stringify(o);document.body.appendChild(p);});</script>'''
temp=OUT/'verify-page.html';temp.write_text(page.replace('</body>',probe+'</body>'),encoding='utf-8')
browser=[]
try:
    for width in [1440,600]:
        proc=subprocess.run(base+['--window-size='+str(width)+',1100','--dump-dom',temp.as_uri()],capture_output=True,timeout=50,creationflags=subprocess.CREATE_NO_WINDOW)
        dom=proc.stdout.decode('utf-8',errors='replace')
        m=re.search(r'<pre id="browser-probe">(.*?)</pre>',dom,re.S)
        assert proc.returncode==0 and m,dom[-1000:]
        o=json.loads(html.unescape(m.group(1)));assert not o['broken'],o
        assert o['fonts']=='loaded' and o['galmuri'] and o['pretendard'],o
        assert o['scrollWidth']<=o['width'],o
        assert len(o['native'])==4 and all(x['width']==960 and x['naturalWidth']==960 and x['naturalHeight']==540 for x in o['native']),o
        browser.append(o)
    for name,fragment in [('preview-top.png',''),('preview-hud.png','hud')]:
        static_page=page.replace('scroll-behavior:smooth','scroll-behavior:auto')
        if fragment:
            # Headless Windows offscreen compositor gave an empty deep-scroll shot.
            # Render the unchanged HUD section at page top as an explicit section excerpt.
            head=static_page.split('<body>')[0]
            hud_section=static_page.split('<section id="hud">')[1].split('<section id="spec">')[0]
            static_page=head+'<body><main><section id="hud">'+hud_section+'</main></body></html>'
        temp.write_text(static_page,encoding='utf-8')
        r=subprocess.run(base+['--window-size=1440,1100','--screenshot='+str(OUT/name),temp.as_uri()],capture_output=True,timeout=50,creationflags=subprocess.CREATE_NO_WINDOW)
        assert r.returncode==0 and (OUT/name).exists(),r.stderr[-800:]
        assert len(Image.open(OUT/name).convert('RGB').getcolors(2000000) or [])>50,'Blank browser screenshot: '+name
finally:
    temp.unlink(missing_ok=True)

result={'date':'2026-10-23','scope':'Static artifacts + headless Chromium; no Unity/Play','evidence_20261023_count':len(list((ROOT/'docs/qa/evidence').glob('20261023-*.png'))),'audit_count':len(audits),'issues_per_screen':{a['id']:len(a['issues']) for a in audits},'wireframe_count':len(wires),'essential_count_each':3,'mockup_count':len(hud),'mockups':checks,'local_missing_links':missing,'source_changed_since_snapshot':source_changes,'browser':browser,'visual_review':'Author inspected artifact PNGs and browser previews. Independent graphics gate not run.','not_run':['Unity import','Play','input/state transitions','audio','human reading test','F1 motion/speed perception','independent UX review']}
(OUT/'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
