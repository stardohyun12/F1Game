"""정적 산출물의 링크·원본 해시·브라우저 이미지 로드와 렌더 확인."""
from pathlib import Path
from html.parser import HTMLParser
import hashlib,json,re,subprocess,tempfile
from PIL import Image

R=Path(__file__).resolve().parent
class Links(HTMLParser):
    def __init__(self): super().__init__(); self.links=[]
    def handle_starttag(self,tag,attrs):
        for k,v in attrs:
            if k in ('href','src') and v: self.links.append(v)

page=(R/'index.html').read_text(encoding='utf-8'); p=Links(); p.feed(page)
missing=[]
for link in p.links:
    if not link.startswith(('http:','https:','#','data:')) and not (R/link.split('#')[0]).exists(): missing.append(link)
sources=json.loads((R/'sources.json').read_text(encoding='utf-8'))
bad=[]; count=0
for s in sources:
    for im in s['shots']:
        f=R/im['local']; count+=1
        if not f.exists() or hashlib.sha256(f.read_bytes()).hexdigest()!=im['sha256']: bad.append(im['local'])

chrome='C:/Program Files/Google/Chrome/Application/chrome.exe'
profile=Path(tempfile.gettempdir())/'pitcrew-ui-refboard-chrome'
common=[chrome,'--headless','--disable-gpu','--no-first-run','--no-default-browser-check','--hide-scrollbars','--force-device-scale-factor=1','--allow-file-access-from-files','--user-data-dir='+str(profile),'--virtual-time-budget=4000']
probe='''<script>window.addEventListener('load',()=>{document.fonts.ready.then(()=>{const visible=[...document.images].filter(i=>!i.closest('details'));const filter=document.getElementById('filter');filter.value='풍경';filter.dispatchEvent(new Event('change'));const filterCount=[...document.querySelectorAll('article')].filter(x=>!x.classList.contains('hidden')).length;filter.value='all';filter.dispatchEvent(new Event('change'));document.getElementById('mark').click();const toggleOn=document.body.classList.contains('show');document.getElementById('mark').click();const out={visibleImageCount:visible.length,hiddenImagesCheckedByHashOnly:document.images.length-visible.length,brokenVisibleImages:visible.filter(i=>!i.naturalWidth).map(i=>i.getAttribute('src')),fontLoaded:document.fonts.check('12px Galmuri'),filterCount,toggleOn,viewportWidth:innerWidth,scrollWidth:document.documentElement.scrollWidth,scrollHeight:document.documentElement.scrollHeight};let pre=document.createElement('pre');pre.id='probe';pre.style.display='none';pre.textContent=JSON.stringify(out);document.body.appendChild(pre)})});</script>'''
probe_path=R/'_verify.html'; probe_path.write_text(page.replace('</body>',probe+'</body>'),encoding='utf-8')
def run(extra):
    return subprocess.run(common+extra,capture_output=True,timeout=50,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
results={}
for width in [1440,960,600]:
    r=run([f'--window-size={width},1000','--dump-dom',probe_path.as_uri()])
    text=r.stdout.decode('utf-8',errors='replace'); match=re.search(r'<pre id="probe"[^>]*>(.*?)</pre>',text,re.S)
    results[str(width)]=json.loads(match.group(1)) if match else {'error':'browser probe missing','exit':r.returncode,'stderr':r.stderr.decode('utf-8',errors='replace')[-1000:]}
height=results['1440'].get('scrollHeight',5100)+4
r=run([f'--window-size=1440,{height}',f'--screenshot={R / "refboard.png"}',(R/'index.html').as_uri()])
if (R/'refboard.png').exists():
    im=Image.open(R/'refboard.png'); im.crop((0,0,1440,min(1200,im.height))).save(R/'preview-top.png'); im.crop((0,max(0,im.height-1200),1440,im.height)).save(R/'preview-bottom.png')
validation={'local_links_missing':missing,'source_images_verified':count,'source_hash_mismatches':bad,'browser':results,'screenshot_exit':r.returncode,'screenshot_size':list(Image.open(R/'refboard.png').size) if (R/'refboard.png').exists() else None,'font':json.loads((R/'font-validation.json').read_text(encoding='utf-8')),'unity_executed':False,'original_games_executed':False,'new_ui_sprites':0}
(R/'validation.json').write_text(json.dumps(validation,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(validation,ensure_ascii=False))
