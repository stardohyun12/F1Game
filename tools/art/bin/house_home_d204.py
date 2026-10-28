"""D-204 evidence/archive and F1Studio-only inspection. No main F1 writes."""
import json, urllib.request, sys, shutil
from pathlib import Path

R = Path(__file__).resolve().parents[3]
OUT = R / 'docs/qa/tmp/build/house_home/d204_0446'
OUT.mkdir(parents=True, exist_ok=True)
IDS = ['unity-Studio_Building-20261029-' + x for x in ('033744-ad8b', '033830-1808', '033845-a0bc')]

def studio(code, command='exec'):
    instances = [json.loads(p.read_text(encoding='utf-8')) for p in (Path.home()/'.unity-cli/instances').glob('*.json')]
    match = [d for d in instances if d.get('projectPath','').replace('\\','/').rstrip('/').lower() == str(R/'F1Studio').replace('\\','/').lower() and d.get('state') != 'stopped']
    assert len(match) == 1, 'F1Studio connector must be unique'
    req = urllib.request.Request(f"http://127.0.0.1:{match[0]['port']}/command", data=json.dumps({'command':command,'params':{'code':code} if command == 'exec' else code}).encode(), headers={'Content-Type':'application/json'})
    d = json.loads(urllib.request.urlopen(req, timeout=120).read())
    assert d.get('success'), d
    return d.get('data')

if __name__ == '__main__':
    if sys.argv[1] == 'console':
        result=studio({'type':'error','lines':20,'stacktrace':'none'}, 'console')
        print(result); (OUT/'console_final_http.json').write_text(json.dumps(result,ensure_ascii=False,indent=1),encoding='utf-8')
    elif sys.argv[1] == 'inspect':
        result = studio('''var s=GameObject.Find("STUDIO/STUDIO_Slot");
var sc=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
return "scene="+sc.name+" dirty="+sc.isDirty+" play="+UnityEditor.EditorApplication.isPlaying+" compiling="+UnityEditor.EditorApplication.isCompiling+"\\n"+string.Join("\\n",s.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==s.transform || t.name.StartsWith("ROOM_shell")).Select(t=>t.name+" active="+t.gameObject.activeInHierarchy+" pos="+t.position+" prefab="+UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)));''')
        print(result)
        (OUT / ('inspect_' + sys.argv[2] + '.txt')).write_text(str(result), encoding='utf-8')
    elif sys.argv[1] == 'archive':
        dest=OUT/sys.argv[2]; dest.mkdir(exist_ok=True)
        for cid in IDS:
            for p in (R/f'tools/viewer/feedback/judge/{cid}.json', R/f'tools/viewer/feedback/shots/{cid}_after.png'):
                if p.exists(): shutil.copy2(p,dest/p.name)
        shutil.copy2(R/'tools/viewer/feedback/unity-Studio_Building.jsonl',dest/'feedback.jsonl')
    elif sys.argv[1] == 'verify':
        result=studio('''var s=GameObject.Find("STUDIO/STUDIO_Slot"); var h=s.transform.Find("house_home");
var rr=h.GetComponentsInChildren<Renderer>(true); var mm=rr.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().ToArray();
return "project="+Application.dataPath+"\\nactive="+string.Join(",",s.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==s.transform && t.gameObject.activeInHierarchy).Select(t=>t.name))+"\\nmissingMaterials="+rr.Count(r=>r.sharedMaterials.Any(m=>m==null))+"\\nglass="+h.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("GLASS_"))+"\\n"+string.Join("\\n",mm.Where(m=>m.name.StartsWith("M_house_home")).Select(m=>m.name+" SN="+(m.HasProperty("_SN")?m.GetFloat("_SN").ToString():"n/a")));''')
        print(result); (OUT/'verify_final.txt').write_text(str(result),encoding='utf-8')
        before=json.loads((OUT/'before/main_f1_hashes.json').read_text())
        import hashlib
        changed=[p for p,h in before.items() if not Path(p).exists() or hashlib.sha256(Path(p).read_bytes()).hexdigest()!=h]
        d={'checked':len(before),'changed':changed}; print(d)
        (OUT/'main_f1_hash_verify.json').write_text(json.dumps(d,indent=1),encoding='utf-8')
    elif sys.argv[1] == 'review':
        from PIL import Image, ImageDraw, ImageFont
        dest=R/'art/_review/2026-10-29-house-home-d204'; dest.mkdir(parents=True,exist_ok=True)
        font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',22)
        sheet=Image.new('RGB',(1920,3*494),'#e9e6dc'); draw=ImageDraw.Draw(sheet)
        cards=[]
        for row,(cid,title) in enumerate(zip(IDS,['실내','발코니','창문'])):
            j=json.loads((R/f'tools/viewer/feedback/judge/{cid}.json').read_text(encoding='utf-8'))
            desc=' / '.join(k+' '+v['verdict'] for k,v in j['judges'].items())
            for col,(suffix,label) in enumerate([('','도현 원본'),('_after','수정 후')]):
                filename=cid+suffix+'.png'; shutil.copy2(R/'tools/viewer/feedback/shots'/filename,dest/filename)
                im=Image.open(dest/filename).convert('RGB'); im.thumbnail((960,445))
                sheet.paste(im,(col*960,row*494+44))
                draw.text((col*960+12,row*494+8),title+' · '+label+(' · '+desc if col else ''),font=font,fill='#24332e')
            cards.append(f'<h2>{title} · {desc}</h2><p>NO = 문제 안 보임 / YES = 아직 보임</p><img src="{cid}_after.png">')
        sheet.save(dest/'sheet.png')
        (dest/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>house_home D-204 검토</title>
<style>body{max-width:1400px;margin:32px auto;background:#e9e6dc;color:#24332e;font:17px/1.6 sans-serif}img{width:100%;height:auto}a{color:#245e65}</style>
<h1>house_home — 같은 카메라 전후 비교</h1><p>도현 승인 필요 · F1Studio 검토본 · 본 F1 미적용</p>
<p>실내 중복 전시 모델 제거 · 미색 회벽 · 발코니 3.7×1.8m · 창틀과 유리 16장.</p>
<a href="sheet.png"><img src="sheet.png"></a>'''+''.join(cards)+'''<p><a href="../../../docs/team/reports/art-20261029-0446.md">검증 보고서</a></p></html>''',encoding='utf-8')
        print(dest/'index.html')
