"""Package A1 documentation and the four explicitly authorized trial PNG copies."""
import csv
import hashlib
import html
import json
from pathlib import Path
import shutil
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/textures/2026-10-24-kp'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def rel(p):return Path(p).relative_to(ROOT).as_posix()
def write(name,text): (OUT/name).write_text(text,encoding='utf-8')
manifest=json.loads((OUT/'manifest.json').read_text(encoding='utf-8'))
check=json.loads((OUT/'check_asset.json').read_text(encoding='utf-8'))
assert check['pass'] and check['manifest_sha256']==sha(OUT/'manifest.json')
assert all(sha(ROOT/t['albedo'])==c['albedo_sha256'] for t,c in zip(manifest['tiles'],check['tiles']))

# Quarantine superseded crop trials so only four final PNGs live in albedo/.
archive=OUT/'research/first-crop-trial';archive.mkdir(parents=True,exist_ok=True)
for key in ['wood','stone','plaster','roof']:
    for folder,name in [('albedo',f'A1_KP_{key}_64.png'),('baseline',f'{key}_photo_to_pixel_64.png'),('baseline',f'{key}_photo_to_pixel_64.json'),('original_reduced',f'{key}_photo_64.png')]:
        source=(OUT/folder/name).resolve();target=(archive/name).resolve()
        assert source.is_relative_to(OUT.resolve()) and target.is_relative_to(OUT.resolve())
        if source.exists() and not target.exists():source.rename(target)

dest=ROOT/'F1/Assets/Art/ProtoCourse/Textures';dest.mkdir(parents=True,exist_ok=True)
copies=[]
for t in manifest['tiles']:
    src=ROOT/t['albedo'];dst=dest/src.name
    if dst.exists() and sha(dst)!=sha(src):raise RuntimeError(f'Refusing to overwrite a different existing asset: {dst}')
    shutil.copy2(src,dst)
    copies.append({'source':rel(src),'destination':rel(dst),'sha256':sha(src),'copy_sha256':sha(dst),'match':sha(src)==sha(dst),'id':t['id']})
(OUT/'trial-copy.json').write_text(json.dumps({'pass':all(c['match'] for c in copies),'copies':copies,'scope':'PNG copies only. No Unity, .meta, material, scene, shader, code or atlas edits.'},indent=2)+'\n',encoding='utf-8')

# Preserve unrelated rows; record actual copied bytes as separate lineage rows.
csvpath=ROOT/'art/provenance.csv'
with csvpath.open(encoding='utf-8-sig',newline='') as f:
    reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
rows=[r for r in rows if not (r['asset_id'].startswith('a1_kp_') and r['asset_id'].endswith('_trial_copy'))]
for t,c in zip(manifest['tiles'],copies):
    rows.append(dict(asset_id=f'a1_kp_{t["id"]}_trial_copy',path=c['destination'],source_url=t['source']['url'],license='CC0-1.0',accessed='2026-10-24',tool='kp_handoff.py; photo_to_pixel.py; kp_textures.py',ai_generated='no',sha256=c['sha256'],notes='Byte-identical checked palette albedo trial copy only; Unity Point import not performed; '+'; '.join(t['process'])))
with csvpath.open('w',encoding='utf-8',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=fields);writer.writeheader();writer.writerows(rows)

lines=['# A1 사진 출처·재현 기록\n','Powered by Poly Haven. 확인일 2026-10-24.\n',
    '공식 API의 Diffuse 1k PNG를 CDN에서 직접 다운로드했다. 최종 네 원본 모두 공급자 MD5와 일치하며 SHA-256은 아래 원본 메타데이터 및 `art/provenance.csv`에 기록했다. 사이트 미리보기 이미지를 질감으로 사용하지 않았다.\n',
    'Poly Haven은 자산을 [CC0](https://polyhaven.com/license)로 배포하며, [공식 FAQ](https://docs.polyhaven.com/en/faq#-do-you-use-photogrammetryphotoscanning)는 사진 기반 질감 제작과 비생성형 방식을 설명한다. 공급자의 설명을 근거로 채택했으며, 개별 자산의 photogrammetry/photometric-stereo 하위 방식이나 촬영 장소는 독립 확인하지 못했다. 교토·포지타노 현지 촬영 사진이라고 주장하지 않는다.\n',
    '| 용도 | 공식 원본 / 작가 | 원본 반복 크기 | 직접 다운로드·검증 |\n|---|---|---|---|']
names={'wood':'교토 세로 목재','stone':'교토 돌포장','plaster':'포지타노 회벽','roof':'포지타노 기와'}
for t in manifest['tiles']:
    s=t['source'];authors=', '.join(s['authors'])
    lines.append(f'| {names[t["id"]]} | [{s["id"]}]({s["url"]}) / {authors} | {t["physical_size_m"][0]:g}m 정사각 | [PNG]({s["download_url"]}) · [메타데이터](source_{t["id"]}.json) |')
lines += ['\n## 가져온 부분 / 바꾼 부분 / 검증 범위\n',
    '- 기존 `photo_to_pixel.py`를 수정 없이 실행해 BOX 축소 → 공용 32색 nearest 양자화 → 무디더 기준판을 `baseline/`에 보존했다. 외부 작가의 전체 제작 과정을 재현한 것은 아니다.',
    '- 최종본은 같은 사진 축소본에서 색 사다리를 재배치했다. 목재는 90° 회전과 갈색 밝기 사다리, 돌은 중성 회색 사다리와 **사진에 이미 있는 줄눈만 직접 따라 그린 1px 보정**, 회벽은 실제 얼룩 밝기 구간, 기와는 0.55px 저역 보정과 흙빛 사다리다. 좌표·수치는 `kp_textures.py`와 manifest.process에 있다.',
    '- 가장자리 3px의 대응 색을 평균하고 팔레트로 재양자화했다. 무작위 잡음·AI 생성 이미지·AI 배경·노멀/AO 재료 합성은 0이다.',
    '- 반복을 깨던 1m 크롭을 폐기하고 원본 반복 크기(1/2/2/4m)를 보존했다. 64²/128²/128²/256²는 모두 64px/m이다. `research/first-crop-trial/`은 미사용 이력이다.',
    '- 기준 장면: 직접 만든 Blender 시험면. 실제 게임 캡처·주행·원거리 안개·미프 동작을 재현하지 않았다. 32색 검사는 **원본 albedo PNG(조명 전 납품 텍스처)**에만 적용하며 고해상도 사진과 최종 화면 색 검사는 별도다.',
    '- 구도 참고는 `art/reference/mood-2026-10-23/board_kyoto_main.png`, `board_positano.png`의 실제 사진뿐이며, 이 사진을 픽셀 재료로 사용하지 않았다. AI 기준 원화도 사용하지 않았다.',
    '\n## 미사용 후보\n',
    '- Stone Pathway: 갈색 자연돌이 강함. Grey Stone Path: 불규칙 돌로 주요 관광길의 정형 판석과 다름. Wood Plank Wall과 Stone Pavers를 최종 채택.',
    '- Worn Plaster Wall: 큰 박락·이음새가 강함. Plastered Wall 02: 미장 패널 구분. White Plaster 02: 지나치게 균일. Painted Plaster Wall의 실제 잔얼룩을 채택.',
    '- 후보 원본·공식 info/files JSON은 `sources/`에 남겼으며 최종 manifest에는 채택한 4종만 들어 있다.',
    '\n## 재생성\n',
    '```powershell\npy -3.13 tools/asset-gen/kp_download.py\npy -3.13 tools/asset-gen/kp_textures.py\n& D:/Tools/Blender/blender.exe -b -t 8 --python tools/asset-gen/kp_texture_preview.py\npy -3.13 tools/asset-gen/kp_textures.py --provenance\npy -3.13 tools/asset-gen/check_asset.py --texture-manifest art/textures/2026-10-24-kp/manifest.json --out art/textures/2026-10-24-kp/check_asset.json\n```',
    '시험 복사는 위 검사 통과 후 `py -3.13 tools/asset-gen/kp_handoff.py`. 재생성은 art/ 산출만, 마지막 handoff가 허용된 PNG 4개만 F1에 복사한다. PNG 자체는 Point 필터를 강제할 수 없다. GLB 시험면은 Nearest/Repeat/unlit를 내보내고 실제 값을 검사했다. Unity 설정은 `ATLAS-PROPOSAL.md`를 인계한다.']
write('SOURCES.md','\n'.join(lines)+'\n')

write('ATLAS-PROPOSAL.md','''# LofiAtlas 교체 제안 — 시험안, 미적용

현재 `F1/Assets/Art/LofiPilot/LofiAtlas.png` 512², 8×8칸·칸당64px. 이름·번호는 `tools/asset-gen/lofi_pilot.py`에서 확인했다. 아래 좌표는 PNG 왼쪽 위가 원점이며 0부터 센다.

| 최종 재료 | 기존 칸 | PNG 사각형 x,y,w,h | 적용할 곳 | 공유 영향 / 제약 |
|---|---:|---|---|---|
| wood | 4 `wood` | 256,448,64,64 | 교토 세로 외벽 | 문틀·의자 등 공유. 전역 교체 전 별도 재료 슬롯 권장 |
| stone | 41 `quaystone` | 64,128,64,64 | 교토 관광길 판석 | 기존 석축까지 판석으로 바뀔 수 있음. 바닥 전용 분리 필요 |
| plaster | 0 `plaster` | 0,448,64,64 | 포지타노 밝은 외벽 | 건물 대부분 공유. 구간별 재료 분리 필요 |
| roof | 44 `roof_clay` | 256,128,64,64 | 포지타노 점토 기와 | 교토 회색 지붕용3 `roof`는 유지. 기와 시각 판정은 보류 |

**이번에는 기존 atlas·코드·씬을 바꾸지 않았다.** `atlas_cells/*_60plus2.png`는 중앙60px 발췌+2px 가장자리 확장안이다. 최종 전체 반복 타일과 동일한 완성도/연속성을 보증하는 파일이 아니다.

기존 생성기의 UV는 셀 [2,62]의60px를 사용한다. 64px/m를 유지하려면 이60px가 **0.9375m**여야 한다. 현재 최대2m 분할면은 30px/m이므로 단순 PNG 교체로 64px/m가 되지 않는다. 기존 월드32px/m 지침을 일괄 변경했다는 뜻도 아니다.

권장 시험은 이번 독립 PNG를 Repeat로 사용하고 1 UV 반복에 wood1m / stone2m / plaster2m / roof4m를 배정하는 것이다. 기존64칸을 반드시 쓴다면 60px 연속 조각 여러 칸과 대응 UV가 필요하다. 이를64px로 다시 축소해 한 칸에 넣으면 큰 석판·기와 반복 비례와 밀도가 깨진다. 새 칸을 사용하기 전 타 팀 예약 상태도 확인해야 한다.

## 프로그래밍/QA 임포트 인계

- 시험 파일: `F1/Assets/Art/ProtoCourse/Textures/A1_KP_{wood,stone,plaster,roof}_64ppm.png` 네 개. 바이트 일치 증거는 `trial-copy.json`.
- Texture Type Default / sRGB ON / Filter Mode **Point** / Wrap Mode Repeat / Compression None / 원본 크기 유지(64,128,128,256). 화면 시험 첫 기준은 Mipmap OFF. 이는 엔진 적용 전 권고 설정이며 직접 적용하지 않았다.
- GLB 시험면은 Nearest·미프 없음·Repeat·white factor·unlit를 실제 내보내고 검사했다. Blender 정지 렌더는 같은 PNG를 Closest로 사용하고 조명은 별도다.
- 960×540 실제 화면에서 정면·사선·원거리·주행 떨림/모아레를 확인한다. 필요하면 미프 ON과 비교하되 평균화된 최종 화면에 albedo32색 기준을 잘못 적용하지 않는다.
- QA/프로그래밍 임포트 필요. 이번 작업은 Unity 실행·자동 임포트 확인·`.meta` 편집·씬/코드 변경을 하지 않았다. 정적 검사 통과는 적용 승인이나 시각 통과가 아니다.
''')

cards=[]
for t in manifest['tiles']:
    p=Path(t['albedo']).name;size=Image.open(ROOT/t['albedo']).size[0]
    cards.append(f'<article><h3>{names[t["id"]]}</h3><img class="tile" src="albedo/{p}" alt="{names[t["id"]]} 최종 albedo"><p>{size}² · {t["physical_size_m"][0]:g}m · {t["actual_used_colors"]}색</p><a href="review/{t["id"]}_repeat_3x3.png">3×3 반복</a> · <a href="proof/{t["id"]}_metric.glb">실측 GLB</a></article>')
write('index.html','''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>A1 사진 질감 시험</title>
<style>body{margin:0;background:#eee8dc;color:#30362f;font:16px/1.65 system-ui,sans-serif}main{max-width:1180px;margin:auto;padding:32px}h1{font-size:32px;margin:0}h2{margin-top:44px}a{color:#795244}.status{border-left:4px solid #8a4631;padding:12px 20px;background:#e4d6bf}.wide{display:block;width:100%;height:auto;background:#c4c7c9}.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:20px}.tile{display:block;width:100%;aspect-ratio:1;image-rendering:pixelated}article{border-top:1px solid #97826a}nav{margin:18px 0}small{color:#696263}@media(max-width:700px){main{padding:18px}.grid{grid-template-columns:repeat(2,1fr)}}</style>
<main><small>PITCREW / 2026.10.24 / A1</small><h1>사진에서 가져온 네 가지 표면</h1>
<p class="status"><b>수치 4/4 PASS · 독립 시각 게이트 6/10, 미통과 · 도현 승인 필요</b><br>직접 다운로드한 사진 재료 · AI 생성 0 · 전체64px/m · 공용32색<br>시험 PNG 복사만 완료. 실제 게임 화면·주행·Unity 임포트는 미검증.</p>
<nav><a href="SOURCES.md">출처·재생성</a> / <a href="ATLAS-PROPOSAL.md">아틀라스 제안</a> / <a href="check_asset.json">검사 원문</a> / <a href="visual-gate.md">독립 판정</a> / <a href="trial-copy.json">시험 복사</a></nav>
<h2>Blender 정지 시험 · 960×540</h2><p>실제 렌더만 사용. 게임 캡처나 AI 합성 배경이 아니다. 조명 이후 색은32색 제한 검사와 별개다.</p>
<img class="wide" src="review/material-stage-960.png" alt="네 재료의 Blender 시험 렌더"><details><summary>사선 보기</summary><img class="wide" src="review/material-stage-oblique-960.png" alt="사선 Blender 렌더"></details>
<h2>같은 사진, 기준판과 보정</h2><p>각 타일 중앙1m 영역을 4배 Nearest로 표시. <a href="compare-full-4x.png">전체 반복 단위 4배 비교판 원본</a></p><img class="wide" src="compare-4x.png" alt="원본 축소, 양자화 기준판, 보정 결과 비교">
<h2>최종 albedo 네 종</h2><p>32색 검사는 이 납품 albedo PNG에 적용한다. 1k 사진 원본이나 조명·안개가 포함된 화면 검사가 아니다.</p><section class="grid">'''+''.join(cards)+'''</section>
<h2>남은 일</h2><p>기와의 재료 구분, 목재의 기계적 반복, 회벽 박락의 평면성이 남았다. 현재 판은 시험용이며 본 적용을 승인하지 않았다. <a href="research/visual-r1/visual-gate.md">첫 판정과 수정 이력</a>을 보존했다.</p><p>Powered by <a href="https://polyhaven.com">Poly Haven</a>. 촬영 장소는 미확인; 교토·포지타노 재료 대응 시험.</p></main></html>''')
print(json.dumps({'trial_png_count':len(copies),'byte_match':all(c['match'] for c in copies),'provenance_a1_rows':sum(r['asset_id'].startswith('a1_kp_') for r in rows)},ensure_ascii=False))
