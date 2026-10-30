"""Serial main-editor checks; keep the existing fb_geom thresholds and captured cameras unchanged."""
import argparse, json, pathlib, shutil, subprocess, sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'scripts'))
import fb_geom

p=argparse.ArgumentParser();p.add_argument('tag');p.add_argument('--shots',action='store_true');args=p.parse_args()
out=ROOT/'docs/qa/evidence/roadv2/codex'/args.tag;out.mkdir(parents=True,exist_ok=True)
results=[]
for key in ['45c3','268e','7da7','453e','4143','66ad','4100','0c17']:
    for attempt in range(3):
        run=subprocess.run([sys.executable,'-X','utf8','scripts/fb_geom.py',key,'--radius','40'],cwd=ROOT,capture_output=True,text=True,encoding='utf-8')
        if 'Errno 22' not in run.stdout+run.stderr and 'OSError: [Errno 22]' not in run.stdout+run.stderr: break
    (out/f'{key}.txt').write_text(run.stdout+run.stderr,encoding='utf-8')
    d=fb_geom.find(key);source=ROOT/'tools/viewer/feedback/geom'/f"{d['id']}.json"
    if source.exists():shutil.copy2(source,out/f'{key}.json')
    results.append({'id':key,'exit':run.returncode});print(key,'PASS' if run.returncode==0 else 'FAIL',flush=True)
(out/'summary.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
if args.shots:
    for key in ['4143','4100','0c17']:
        run=subprocess.run(['bash','scripts/fb_shot.sh',key],cwd=ROOT,capture_output=True,text=True,encoding='utf-8')
        (out/f'{key}_capture.txt').write_text(run.stdout+run.stderr,encoding='utf-8')
        if run.returncode:raise RuntimeError(run.stdout+run.stderr)
        d=fb_geom.find(key);shutil.copy2(ROOT/'tools/viewer/feedback/shots'/f"{d['id']}_after.png",out/f'{key}.png')
    from PIL import Image,ImageDraw
    sheet=Image.new('RGB',(1280,3*(611+32)),(28,32,35));draw=ImageDraw.Draw(sheet)
    for row,key in enumerate(['4143','4100','0c17']):
        shot=Image.open(out/f'{key}.png').convert('RGB');shot.thumbnail((1280,611));y=row*(611+32)
        draw.text((12,y+8),args.tag+' / '+key,fill='white');sheet.paste(shot,(0,y+32))
    sheet.save(out/'sheet.png')
