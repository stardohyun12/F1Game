"""Compose real Unity captures; no generated imagery or retouching."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, html
root=Path(__file__).resolve().parents[3]
out=root/'art/_review/2026-10-29-field_patch'
font='C:/Windows/Fonts/malgun.ttf'
f=lambda n:ImageFont.truetype(font,n)
im=Image.new('RGB',(1920,1900),'#e7e3d9');d=ImageDraw.Draw(im)
d.text((28,20),'밭 / 게임 화면 목업',font=f(39),fill='#303d36')
d.text((1040,35),'원화 재검토 · 도현 승인 필요',font=f(27),fill='#725d48')
def panel(file,x,y,title,subtitle=None):
    d.text((x,y),title,font=f(25),fill='#303d36')
    img=Image.open(file).convert('RGB');img.thumbnail((920,518),Image.Resampling.LANCZOS)
    im.paste(img,(x,y+44))
    if subtitle:d.text((x,y+44+518+8),subtitle,font=f(20),fill='#50584f')
panel(root/'tools/viewer/feedback/shots/unity-Map_TownV6-20261029-231130-94a3.png',28,95,'도현 원본 / 집과 창고 사이 풀밭')
d.text((980,100),'주변에서 뽑은 색',font=f(29),fill='#303d36')
samples=json.loads((out/'color_samples.json').read_text())
for i,(key,label) in enumerate([('grass','풀밭'),('grass_light','밝은 풀'),('grass_shade','그늘 풀'),('soil','창고 앞 흙')]):
    x=980+i*226;d.rectangle((x,154,x+196,236),fill=samples[key]['hex'])
    d.text((x,246),label,font=f(22),fill='#303d36');d.text((x,280),samples[key]['hex'].upper(),font=f(23),fill='#50584f')
for j,text in enumerate(['A  8×6m · 집 축에 맞춘 4줄 · 디딤돌 진입', 'B  10×7m · 7% 비탈 · 휜 이랑 · 풀에 묻힌 가장자리', 'SceneA7Lit + A7 빛·그늘 + Kuwahara 반경 2', '각 원본 1280×720 · 집 왼쪽 · 실시간 Unity 캡처', '목업 전용: 원본 맵 배치·본제작·라이브러리 등록 전', '남은 점: 눈높이에서 줄 겹침 / 원본 맵 접지 미검증']):
    d.text((980,345+j*43),text,font=f(22),fill='#50584f')
panel(out/'A_high.png',28,680,'A / 상부 비스듬히','낮고 둥근 흙 두둑 4줄, 수확한 짧은 빈 구간')
panel(out/'A_eye.png',972,680,'A / 집 마당 눈높이 1.65m','작물은 잎 대신 큰 덩어리, 가는 지지대 4개')
panel(out/'B_high.png',28,1275,'B / 상부 비스듬히','넓은 비탈에 살짝 휜 줄, 가장자리는 풀과 겹침')
panel(out/'B_eye.png',972,1275,'B / 집 마당 눈높이 1.65m','같은 집·빛·카메라로 A와 비교')
im.save(out/'mock_sheet.png')
links=''.join(f'<a href="{n}.png"><img src="{n}.png" alt="{n}"><b>{label}</b></a>' for n,label in [('A_high','A 상부'),('A_eye','A 눈높이'),('B_high','B 상부'),('B_eye','B 눈높이')])
(out/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>밭 게임 화면 목업 · 승인 대기</title><style>body{margin:32px auto;max-width:1500px;padding:0 20px;background:#e7e3d9;color:#303d36;font:17px/1.7 sans-serif}a{color:inherit}img{width:100%;display:block}section{display:grid;grid-template-columns:1fr 1fr;gap:24px}h1{font-size:30px}p{max-width:1000px}b{display:block} @media(max-width:700px){section{grid-template-columns:1fr}}</style><h1>밭 / 게임 화면 목업</h1><p>도현 승인 필요 — A: 8×6m, 곧은 4줄 / B: 10×7m, 비탈에 휜 4줄. 모든 컷은 F1Studio의 실제 A7 조명·720p Kuwahara 화면이다. 본제작·원본 맵 적용은 하지 않았다.</p><a href="mock_sheet.png"><img src="mock_sheet.png" alt="원본과 A/B 4장 비교 시트"></a><h2>1280×720 원본 보기</h2><section>'''+links+'''</section><p>눈높이에서는 줄이 겹쳐 차이가 약하며, 실제 Map_TownV6 경사지 접지는 검증하지 않았다. 승인 후 본제작과 QA/프로그래밍 임포트 필요.</p><p><a href="concept.png">반려된 1차 도식</a> · <a href="color_samples.json">원본 색 표본</a> · <a href="verification.json">Unity 측정 결과</a> · <a href="../../../docs/team/reports/art-20261029-2328.md">보고서</a></p></html>''',encoding='utf-8')
for name in ['A_high','A_eye','B_high','B_eye']:
    assert Image.open(out/f'{name}.png').size==(1280,720)
print('mock_sheet.png 1920x1900; four 1280x720 captures; HTML links generated')
