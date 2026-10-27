from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json,hashlib
R=Path(__file__).resolve().parents[3];E=R/'art/export/kart-v6c';V=R/'art/_review/2026-10-27-kart-v6c';t=json.loads((E/'texture-validation.json').read_text());g=json.loads((E/'geometry-validation.json').read_text())
for p,h in t['source_hashes'].items():assert hashlib.sha256((R/p).read_bytes()).hexdigest()==h
g.update(source_files_unchanged=True,used_colors=t['used_colors'],texture_size=t['texture_size'],sha256={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [E/'kart_v6c.fbx',E/'kart_v6c.png']});(E/'validation.json').write_text(json.dumps(g,indent=2))
im=Image.new('RGB',(2400,2020),'#e6e1ce');d=ImageDraw.Draw(im)
def text(x,y,s,size=28):d.text((x,y),s,font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',size),fill='#343c3b')
def pic(p,x,y,w,h):
 a=Image.open(V/p).convert('RGBA');a.thumbnail((w,h),Image.Resampling.LANCZOS);im.paste(a,(x+(w-a.width)//2,y+(h-a.height)//2),a)
text(24,16,'KART v6c  /  v6의 결 + v6b의 배색 · 도현 승인 필요',42)
text(24,82,'동일 형태·UV·바퀴 피벗 · 1,864 tris · 1.80 × 1.28 × 0.82m · Blender unlit 비교',27)
for i,(tag,label) in enumerate([('v6','v6 — 원본 노이즈'),('v6b','v6b — 배색 기준'),('v6c','v6c — 결 + 배색')]):
 text(i*800+24,135,label,32);pic(tag+'_camera_B.png',i*800,185,800,450);pic(tag+'_close.png',i*800,690,800,640);pic(tag+'_rear.png',i*800,1380,800,490)
text(24,645,'게임 카메라 B: 뒤 5.6m / 높이 2.8m / 수직 FOV 64° · 정비소·카메라 동일',25)
text(24,1335,'근접 전면 / 후면 — 원본 블록 위치 유지 · 타이어/금속은 R=G=B · 번호판은 v6b 유지',25)
text(24,1880,'결 크기: 원본 가로 동일 명도 구간 중앙값 2 texel ≈ 6.25cm · 새 무작위 패턴 없음',25)
text(24,1925,'차체 상대 명도 표준편차 0.441 → 0.442 · 4096×1024 / 72색 · Unity 미실행',25)
text(24,1970,'도현 승인 후 QA/프로그래밍 임포트 필요. 실제 게임에서의 색·필터·거리별 결과는 미확인.',25)
im.save(V/'sheet.png');im.thumbnail((1200,1010));im.save(V/'sheet-small.png')
(V/'index.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>카트 v6c</title><style>body{background:#e6e1ce;color:#343c3b;font:18px sans-serif;margin:24px}img{max-width:100%}a{color:inherit}</style><h1>카트 v6c — 도현 승인 필요</h1><p>v6 결 + v6b 배색. Unity 미실행 / QA·프로그래밍 임포트 필요.</p><p><a href="../../export/kart-v6c/APPLY.md">적용 메모</a> · <a href="../../../docs/team/reports/art-20261027-1730.md">보고</a> · <a href="v6c_close.png">근접 원본</a></p><a href="sheet.png"><img src="sheet.png"></a></html>''',encoding='utf-8')
names={'plastic':'차체','tire':'타이어','metal':'금속·핸들 등','seat':'시트','warm':'엔진·끈 등'}
rows='\n'.join(f"| {names[k]} | {v['source_horizontal_run_texels_p50_p90'][0]:.0f} / {v['source_horizontal_run_texels_p50_p90'][1]:.0f} | {v['v6_relative_std']:.3f} → {v['v6c_relative_std']:.3f} | {v['luminance_correlation']:.5f} |" for k,v in t['groups'].items())
report='''# 아트 1 — 카트 v6c

v6의 블록 결을 같은 UV 위치에서 v6b 배색으로 옮겼다 — **도현 승인 필요**.

| 항목 | 경로 / 확인 |
|---|---|
| 산출물 | `art/export/kart-v6c/kart_v6c.fbx`, `kart_v6c.png`, `palette-v6c.gpl` |
| 미리보기 | [3종 비교 시트](../../../art/_review/2026-10-27-kart-v6c/sheet.png), [index.html](../../../art/_review/2026-10-27-kart-v6c/index.html) |
| 증거 | `art/export/kart-v6c/texture-validation.json`, `geometry-validation.json`, `validation.json` |
| 재현 | `art/source/kart-v6c/recolor.py`, `export_render.py`, `package.py` |

## 결 측정과 적용

기존 v6의 최종 PNG를 기준으로 명도 Y=0.2126R+0.7152G+0.0722B를 측정했다. 이 값은 sRGB 코드값의 가중 명도이며 물리적 선형 휘도가 아니다. 원본 표면의 명암·마모·노이즈가 합쳐진 결을 통째로 재사용했고, 조명과 확률적 노이즈를 따로 분리했다고 주장하지 않는다.

각 재질에서 `v6 명도 / v6 평균 명도`를 그대로 v6b 평균 명도에 곱했다. 무작위 패턴 재생성·블러·패턴 크기 변경 없음. 주황빨강/회갈 색차를 유지하되 밝은 색이 RGB 범위를 넘을 때만 채도를 줄였다. 타이어·금속은 항상 R=G=B다. 차체는 R≥G≥B 범위로 제한했다.

원본 1024×256을 최근접 4배로 옮겼으므로 출력은 v6b와 같은 4096×1024이지만 **결의 월드 크기는 원본 그대로**다. 아래 길이는 원본 아틀라스의 재질 내부 가로 동일 명도 연속 구간이다. 원본 약 32 texel/m 기준 중앙값 2 texel≈6.25cm. 면 경계·UV 방향에 영향을 받는 표면 결 지표이며 독립적인 브러시 크기 추정은 아니다.

| 재질 | 구간 길이 중앙값 / P90 (원본 texel) | 상대 명도 표준편차 v6 → v6c | 같은 위치 명도 상관 |
|---|---:|---:|---:|
'''+rows+'''

- 대비·분포: 정규화 명도의 P10/P90과 평균·표준편차를 JSON에 기록. 상관계수는 동일 블록의 밝고 어두운 위치가 유지되는지 확인한 값이다.
- 엔진·끈 계열의 극소수 64 출력 텍셀만 명도 250 상한 적용; 해당 부분 대비는 0.528→0.524. 차체·타이어·금속·시트에는 명도 상한 적용 없음.
- 흰 번호판/07은 v6b의 UV 내부 픽셀과 완전 동일. 글자를 흐리는 노이즈는 추가하지 않았다.
- 최종 사용 72색. v6b의 재질별 색 영역은 유지하되 v6의 대비를 위해 명도 단계가 늘어남.

## 보존·검증

- 원본 v6/v6b FBX·PNG 4개 SHA256 전후 일치. F1/Assets 투입 없음.
- FBX 재임포트: 59 mesh, 1,864 tris, 면 연결 동일. 정점·UV·행렬 차 최대 1.1921e−7.
- 길이 1.80 × 폭 1.28 × 높이 0.82m. k_tire_FL/FR/RL/RR·k_hub_FL/FR/RL/RR 이름과 피벗 8개 유지.
- 타이어·금속 UV 내부 텍셀의 RGB 최대 채널 차 0. 새 PNG 경로 연결을 위해 FBX 재출력했으며 형상 수정 없음.
- 같은 모델·정비소·카메라 B(뒤 5.6m, 높이 2.8m, 수직 FOV 64°)와 근접 앞/뒤 카메라로 v6/v6b/v6c를 다시 렌더했다.

## 승인·다음 단계

- **도현 승인 필요:** 시트의 v6c 결 세기와 주황빨강/회색 배색.
- **QA/프로그래밍 임포트 필요:** 승인 후 새 FBX/PNG 연결, Unlit·흰 Tint·Point·sRGB로 확인. 씬 추가 착색과 거리별 필터링 확인.
- Unity 실행·임포트·게임 검증, 커밋·푸시, AI 이미지 생성은 하지 않았다.
'''
(R/'docs/team/reports/art-20261027-1730.md').write_text(report,encoding='utf-8')
(E/'APPLY.md').write_text('''# 카트 v6c — 도현 승인 필요

`kart_v6c.fbx` + `kart_v6c.png`를 함께 사용. 승인 전 F1/Assets 투입 금지.
4096×1024 / 실제 72색. 기존 UV·형태·바퀴 이름·치수 유지. v6 블록 결 위치·크기 유지, v6b 재질 배색과 번호판 유지.
QA/프로그래밍 임포트 필요: Unlit, BaseMap=새 PNG, 흰 Tint, sRGB 켬, Point, 우선 무압축 비교. Unity 미실행.
원본 v6/v6b 파일 변경 없음. 재현 코드와 측정 수치는 source/kart-v6c 및 같은 export 폴더의 validation JSON 참조.
''',encoding='utf-8')
print('PACKAGED',g['tris'],t['used_colors'])
