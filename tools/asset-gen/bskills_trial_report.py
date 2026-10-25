"""Freeze the external-skill experiment evidence; no game-asset writes."""
from pathlib import Path
import json, hashlib, html
from PIL import Image, ImageDraw, ImageFont

ROOT=Path(__file__).resolve().parents[2]
R=ROOT/'art/_review/2026-10-25-bskills-trial'
O=ROOT/'art/models/bskills_trial_v01'
ids=['parked_car_01','parked_car_02','parked_car_03','three_wheeler_01','laundry_line_01']
old=[6,6,5.5,5.5,6]; first=[5.5,5.5,5,6.5,5]; final=[6,6,6,6.5,6.5]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
checks=json.loads((R/'glb-checks.json').read_text(encoding='utf8'))
byid={a['id']:a for a in checks['assets']}
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',28)
sheet=Image.new('RGB',(1920,5*610),(231,228,219));d=ImageDraw.Draw(sheet)
records=[]
for n,k in enumerate(ids):
    y=n*610
    d.text((24,y+12),f'{k} | 이전 {old[n]} / 10',font=font,fill=(40,40,40))
    d.text((984,y+12),f'최종 {final[n]} / 10 · 보류 (시험 1차 {first[n]})',font=font,fill=(40,40,40))
    sheet.paste(Image.open(R/'iterations/before'/f'{k}_front.png').convert('RGB'),(0,y+60))
    sheet.paste(Image.open(R/f'{k}_front.png').convert('RGB'),(960,y+60))
    report='cars-gate-r2.md' if k.startswith('parked') else ('three-gate-r2.md' if k.startswith('three') else 'laundry-gate-r2.md')
    evidence=[f'{k}_front.png']+([] if k.startswith('parked') else [f'{k}_side.png'])
    records.append(dict(id=k,baseline_score=old[n],trial_r1_score=first[n],final_score=final[n],approved=False,
        reason='Visual score below 7; additional technical failure where recorded',technical_pass=byid[k]['pass'],
        report=report,report_sha256=sha(R/report),images={p:sha(R/p) for p in evidence},model_sha256=sha(O/f'{k}.glb'),fbx_sha256=sha(O/f'{k}.fbx')))
sheet.save(R/'compare.png')
sheet.resize((960,1525),Image.Resampling.LANCZOS).save(R/'compare-960.png')
(R/'gates.json').write_text(json.dumps(dict(approved=[],held=ids,assets=records),ensure_ascii=False,indent=2),encoding='utf8')
baseline=json.loads((R/'existing-files-before.json').read_text(encoding='utf8'))
changed=[p for p,h in baseline.items() if not (ROOT/p).exists() or sha(ROOT/p)!=h]
delivery=dict(approved=[],held=ids,copied_files=[],unity_run=False,commit_created=False,
    existing_props_baseline_count=len(baseline),changed_existing_files=changed,
    note='No trial assets or atlas copied. Snapshot comparison detects shared-workspace changes without attributing them.')
(R/'delivery.json').write_text(json.dumps(delivery,ensure_ascii=False,indent=2),encoding='utf8')
sections=[]
for k in ids:
    links=' '.join(f'<a href="{k}_{a}.png">{a}</a>' for a in ['front','side','top','silhouette'])
    if k=='laundry_line_01':links+=f' <a href="{k}_mounted.png">설치 높이 참고(점수 제외)</a>'
    sections.append(f'<section><h2>{k}</h2><a href="briefs/{k}.md">Production brief</a> · {links}<img loading="lazy" src="{k}_front.png"></section>')
page='''<!doctype html><html lang="ko"><meta charset="utf-8"><title>Blender skills 시험</title>
<style>body{background:#e7e4db;color:#282827;font:16px sans-serif;max-width:1280px;margin:32px auto;padding:0 20px}img{width:100%;height:auto}a{color:#345e70}section{margin:32px 0}td,th{padding:10px;border:1px solid #aaa}table{border-collapse:collapse}</style>
<h1>외부 Blender 스킬팩 시험</h1><p>최종 0/5 통과. 전부 보류, F1/Assets 복사 없음. 실제 bpy/Cycles 렌더만 사용.</p>
<p>같은 960×540 촬영 설정. 이전 점수는 당시 독립 평가, 새 점수는 이전 점수·의도를 모르는 새 리뷰어의 평가. 인과 효과를 확정하는 실험은 아님.</p>
<p><a href="skill-usage.md">가져온 방법·충돌</a> · <a href="glb-checks.json">GLB 검사</a> · <a href="fbx-checks.json">FBX 재임포트</a> · <a href="gates.json">최종 게이트</a> · <a href="delivery.json">미투입 기록</a> · <a href="../../../docs/team/reports/art-20261025-2321.md">보고서</a></p>
<p><a href="cars-gate-r2.md">자동차 리뷰</a> · <a href="three-gate-r2.md">삼륜차 리뷰</a> · <a href="laundry-gate-r2.md">빨래 리뷰</a> · <a href="iterations/trial-r1/">첫 판 보존</a> · <a href="iterations/before/">이전 판 보존</a></p>
<a href="compare.png"><img src="compare-960.png" alt="이전과 최종 5종 비교"></a>'''+''.join(sections)+'</html>'
(R/'index.html').write_text(page,encoding='utf8')
print(json.dumps(delivery,ensure_ascii=False))
