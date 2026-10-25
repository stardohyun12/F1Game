from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[2];R=ROOT/'art/_review/2026-10-25-car-candidates'
g=json.loads((R/'gates.json').read_text(encoding='utf8'))
body='''<!doctype html><html lang="ko"><meta charset="utf-8"><title>D142 F1 후보 A / C</title>
<style>body{font:17px sans-serif;background:#e5e1d7;color:#292c2b;max-width:1380px;margin:30px auto;padding:0 24px}img{max-width:100%;height:auto}table{border-collapse:collapse}td,th{border:1px solid #999;padding:12px}a{color:#365569}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px}</style>
<h1>D-142 · 동네에 어울리는 F1</h1><p>A 정비소가 고쳐 쓴 중고 F1 / C 올드 랠리 개조 F1. 도현 선택 필요.</p>
<p>마을 원본은 실제 게임 캡처. 왼쪽 기존 차는 지우지 않았고 후보를 빈 도로에 합성했다. Unity 인게임 캡처가 아니며 원근·빛·접촉은 근사값이다. 배경 복원·AI 이미지 없음.</p>
<p><a href="../../../docs/team/reports/art-20261025-2345.md">최종 보고</a> · <a href="../../models/car_candidates_v01/SPEC.md">프로그래머 인계</a> · <a href="../../models/car_candidates_v01/current-contract.md">기존 물리·외형 계약</a> · <a href="composition.json">합성 기록</a> · <a href="delivery.json">시험 복사 기록</a></p>
<div class="pair"><a href="A_village-960.png"><img src="A_village-960.png"></a><a href="C_village-960.png"><img src="C_village-960.png"></a></div>
<h2>기존 차 / A / C · 같은 Blender 촬영</h2><a href="views-compare.png"><img src="views-compare-960.png"></a>
<p>기존 차의 드라이버는 원본 포함. A/C는 빈 콕핏 외형이므로 게임 적용 시 기존 드라이버를 별도로 유지해야 한다. 바퀴4개는 각각 회전용 원점 유지.</p>
<table><tr><th>후보</th><th>tris</th><th>최종 독립 평가</th><th>증거</th></tr>'''
for k in ['A','C']:
 c=json.loads((R/f'check-{k}.json').read_text(encoding='utf8'));v=g[k]
 body+=f'<tr><td>{k}</td><td>{c["tris"]}</td><td>{v["score"]}/10 · {"예" if v["yes"] else "아니오"}</td><td><a href="{v["report"]}">리뷰</a> · <a href="check-{k}.json">GLB 검사</a> · <a href="fbx-{k}.json">FBX 검사</a> · <a href="briefs/{k}.md">브리프</a></td></tr>'
body+='</table><p><a href="iterations/r1/">첫 판</a> · <a href="iterations/r2/">두 번째 판</a> · <a href="gate-r1.md">첫 평가</a> · <a href="gate-r2.md">두 번째 평가</a></p>'
for k in ['A','C']:
 body+=f'<h2>{k} · 개별 원본</h2><div class="pair">'+''.join(f'<a href="{k}_{v}.png"><img loading="lazy" src="{k}_{v}.png"></a>' for v in ['front','side','threequarter','top'])+'</div>'
(R/'index.html').write_text(body+'</html>',encoding='utf8')
