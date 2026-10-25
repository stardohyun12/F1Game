"""Copy only explicitly reviewed, audited landmark trial files; preserve replacements."""
from pathlib import Path
import json,csv,hashlib,shutil,html
R=Path(__file__).resolve().parents[2]; V=R/'art/_review/2026-10-26-landmarks'
folders={'harbor':'bayloop_harbor_landmarks_v01','chapel-market':'bayloop_chapel_market_v01','teahouse':'bayloop_teahouse_v01','deferred':'bayloop_deferred_v02'}
gates=json.loads((V/'gate-summary.json').read_text(encoding='utf8'))
audits={a['id']:a for g in json.loads((V/'audit-summary.json').read_text()) for a in g['assets']}
fbx={a['id']:a for a in json.loads((V/'fbx-check.json').read_text())['assets']}
ledger=R/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:rd=csv.DictReader(f);fields=rd.fieldnames;rows=list(rd)
lookup={x['path']:x for x in rows}; copied=[];cards=[];table=[]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
for group,folder in folders.items():
 for a in json.loads((R/'art/models'/folder/'manifest.json').read_text(encoding='utf8'))['assets']:
  key=a['id']; gate=gates[key];passed=gate['yes'] and gate['score']>=7
  assert audits[key]['pass'] and fbx[key]['passed'],key
  cat='Landmarks' if group in ('harbor','chapel-market') else ('Props' if key.startswith('tea_') else 'Buildings')
  if passed:
   for field in ('fbx','texture'):
    src=R/a[field];dst=R/'F1/Assets/Art/BayLoop'/cat/src.name
    if any(c['target']==dst.relative_to(R).as_posix() for c in copied):continue
    dst.parent.mkdir(parents=True,exist_ok=True)
    if dst.exists() and sha(dst)!=sha(src):
     backup=V/'iterations/pretrial-copy'/cat/dst.name;backup.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(dst,backup)
    shutil.copy2(src,dst);assert sha(dst)==sha(src)
    item=dict(lookup[a[field]]);item.update(path=dst.relative_to(R).as_posix(),asset_id=key+'_trial_'+field,sha256=sha(dst),notes=item.get('notes','')+';0155 trial copy, engine not run')
    lookup[item['path']]=item;copied.append({'id':key,'source':a[field],'target':item['path'],'sha256':item['sha256'],'score':gate['score']})
  rel=f'{group}/{key}_front.png';status='시험 복사' if passed else '보류'
  views=' · '.join(f'<a href="{group}/{p.name}">{p.stem.removeprefix(key+"_")}</a>' for p in sorted((V/group).glob(key+'_*.png')))
  cards.append(f'<figure><a href="{rel}"><img src="{rel}"></a><figcaption>{key} · {a["tris"]} tris · {gate["score"]}/10 · {status} · <a href="{gate["evidence"]}">최종 독립 원문</a><br>{views}</figcaption></figure>')
  table.append({'id':key,'group':group,'tris':a['tris'],'score':gate['score'],'copied':passed,'source_folder':'art/models/'+folder,'trial':f'F1/Assets/Art/BayLoop/{cat}/{key}.fbx' if passed else None})
with ledger.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(lookup.values())
(V/'copied.json').write_text(json.dumps(copied,indent=2,ensure_ascii=False),encoding='utf8')
(V/'delivery-summary.json').write_text(json.dumps(table,indent=2,ensure_ascii=False),encoding='utf8')
(V/'index.html').write_text('''<!doctype html><meta charset="utf-8"><title>0155 랜드마크·언덕 찻집</title><style>body{background:#e5dfd0;color:#34332d;font:16px sans-serif;margin:28px}main{display:grid;grid-template-columns:repeat(2,1fr);gap:16px}figure{margin:0;background:#f2eee4;padding:10px}img{width:100%;height:auto}a{color:#35594c}figcaption{padding:8px}header img{max-width:960px}</style><h1>랜드마크·언덕 찻집 / 2026-10-26</h1><p>실제 Blender 렌더. Unity 실행·인게임 확인 없음. 세계 32px/m · 공용 팔레트 · CC0 사진 기반. 도현 판정 / QA·프로그래밍 임포트 필요.</p><p><a href="PROGRAMMER-HANDOFF.md">배치 인계</a> · <a href="gate-summary.json">게이트 요약</a> · <a href="audit-summary.json">규격 검사</a> · <a href="fbx-check.json">FBX 재임포트 검사</a> · <a href="copied.json">시험 복사 증거</a></p><header><a href="teahouse/arrival.png"><img src="teahouse/arrival.png"></a><p>찻집 도착 배치 제안 — 인게임 캡처가 아님.</p></header><main>'''+''.join(cards)+'''</main><h2>전체 방향·거리와 독립 원문</h2><p>하위 폴더 harbor / chapel-market / teahouse / deferred의 front·back·side 및 distance PNG. 초판은 각 iterations/r1에 보존.</p><ul><li><a href="visual-gate-major-r1.md">주요 랜드마크 R1</a> / <a href="visual-gate-major-r2.md">R2</a></li><li><a href="teahouse/visual-gate-r1.md">찻집 R1</a> / <a href="teahouse/visual-gate-r2.md">R2</a></li><li><a href="deferred/visual-gate-r1.md">보류 시설 R1</a> / <a href="deferred/visual-gate-r2.md">R2</a></li></ul>''',encoding='utf8')
print(json.dumps(table,ensure_ascii=False,indent=2))
