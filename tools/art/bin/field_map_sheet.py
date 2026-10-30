"""Compose unretouched Unity captures and measure screenshot pixels.
Writes only art/_review/2026-10-29-field_patch/{map*,index.html}.
"""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,colorsys,statistics,collections
root=Path(__file__).resolve().parents[3];out=root/'art/_review/2026-10-29-field_patch'
f=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
bg='#e7e3d9';ink='#303d36';muted='#50584f'
im=Image.new('RGB',(2560,2320),bg);d=ImageDraw.Draw(im)
d.text((34,20),'밭 / 실제 Map_TownV6 비교',font=f(43),fill=ink)
d.text((1880,30),'임시 배치 · 도현 승인 필요',font=f(29),fill='#725d48')
def panel(file,x,y,title,sub):
    d.text((x,y),title,font=f(29),fill=ink)
    pic=Image.open(file).convert('RGB');pic.thumbnail((1228,691),Image.Resampling.LANCZOS)
    im.paste(pic,(x,y+50));d.text((x,y+50+pic.height+10),sub,font=f(22),fill=muted)
panel(root/'tools/viewer/feedback/shots/unity-Map_TownV6-20261029-231130-94a3.png',34,95,'도현 원본 / 코멘트 자리','빨간 선을 포함한 원본 그대로')
panel(out/'map_baseline.png',1298,95,'현재 맵 / 배치 전 같은 카메라','집·창고·지형·조명은 실제 맵 그대로')
panel(out/'map_A_high.png',34,800,'A / 도현 카메라 · 곧은 이랑 4줄','10×8m · 지형 높이차 0.97m · 집 축과 같은 방향')
panel(out/'map_B_high.png',1298,800,'B / 도현 카메라 · 휜 이랑 5줄','12×9m · 지형 높이차 1.42m · 풀로 끊은 가장자리')
panel(out/'map_A_eye.png',34,1500,'A / 집 마당 높은 쪽 눈높이','지면 +1.65m · 집 오른쪽 벽과 창고를 함께 확인')
panel(out/'map_B_eye.png',1298,1500,'B / 같은 눈높이·같은 카메라','작물 줄 사이의 통로·곡선·접지 비교')
im.save(out/'map_sheet.png')
links=''.join(f'<a href="map_{n}.png"><img src="map_{n}.png" alt="{label}"><b>{label}</b></a>' for n,label in [('A_high','A 도현 카메라'),('B_high','B 도현 카메라'),('A_eye','A 눈높이'),('B_eye','B 눈높이')])
(out/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>밭 실제 맵 비교 · 승인 필요</title><style>body{max-width:1800px;margin:28px auto;padding:0 20px;background:#e7e3d9;color:#303d36;font:17px/1.7 sans-serif}img{display:block;width:100%}a{color:inherit}section{display:grid;grid-template-columns:1fr 1fr;gap:24px}h1{font-size:30px}b{display:block}@media(max-width:700px){section{grid-template-columns:1fr}}</style><h1>밭 / 실제 Map_TownV6 비교</h1><p>도현 승인 필요 — A 10×8m 4줄 / B 12×9m 5줄. 실제 맵의 지형·집·창고·빛으로 촬영했다. 위 시점은 코멘트 당시 카메라·시야각·1280×611 해상도 그대로, 눈높이는 집 마당 높은 쪽 지면 +1.65m이다.</p><a href="map_sheet.png"><img src="map_sheet.png" alt="도현 원본·현재 맵·A/B 비교"></a><h2>개별 원본</h2><section>'''+links+'''</section><p>촬영 후 임시 물체 삭제, dirty=False 확인. 씬 저장·에셋 등록·커밋 없음. 승인 후 본제작과 QA/프로그래밍 임포트 필요.</p><p>남은 점: 밭 경계가 아직 매끈한 판처럼 보이고 작물은 반복 덩어리다. 눈높이 위쪽 검은 표지는 기존 맵 물체이며 이번에 수정하지 않았다. 주행 중 깜빡임은 시험하지 않았다.</p><p><a href="map_render_log.json">접지·카메라·정리 증거</a> · <a href="map_metrics.json">화면색 표본</a> · <a href="../../../docs/team/reports/art-20261029-2354.md">보고서</a> · <a href="mock_sheet.png">직전 스튜디오 시트</a></p></html>''',encoding='utf-8')
# Same world-region comparison, exact source pixels (not the scaled sheet).
baseline=Image.open(out/'map_baseline.png').convert('RGB')
metrics={}
for name in ['A','B']:
    pic=Image.open(out/f'map_{name}_high.png').convert('RGB')
    changed=[]
    for y in range(235,344):
        for x in range(568,742):
            p=pic.getpixel((x,y));q=baseline.getpixel((x,y))
            if max(abs(p[i]-q[i]) for i in range(3))>8:changed.append(p)
    grass=list(baseline.crop((555,300,665,385)).getdata())
    def summary(pixels):
        hsv=[colorsys.rgb_to_hsv(*(c/255 for c in p)) for p in pixels]
        return {'pixels':len(pixels),'median_rgb':[statistics.median(p[i] for p in pixels) for i in range(3)],'saturation_median':statistics.median(p[1] for p in hsv),'saturation_p95':sorted(p[1] for p in hsv)[int(len(hsv)*.95)]}
    metrics[name]={'patch_roi':[568,235,742,344],'difference_threshold':8,'patch':summary(changed),'baseline_grass_roi':[555,300,665,385],'baseline_grass':summary(grass),'most_common_patch_colors':collections.Counter(changed).most_common(12)}
(out/'map_metrics.json').write_text(json.dumps(metrics,ensure_ascii=False,indent=2),encoding='utf-8')
for n in ['A','B']:print(n,json.dumps(metrics[n],ensure_ascii=False))
print('map_sheet.png',im.size)
