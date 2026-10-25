"""Write the delivery report only after the guarded copy completed."""
from pathlib import Path
import json
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-25-bayloop-italy'
g=json.loads((V/'gate-summary.json').read_text());d=json.loads((V/'trial-delivery.json').read_text())
assets=[]
for folder in ['bayloop_italy_v01','bayloop_italy_props_v01']:assets+=json.loads((R/'art/models'/folder/'manifest.json').read_text())['assets']
lines=[]
for a in sorted(assets,key=lambda a:(a.get('asset_kind')=='prop',a['id'])):
 n=a['id'];prop=a.get('asset_kind')=='prop';passed=n in d['accepted'];folder='Props' if prop else 'Buildings'
 target=f'F1/Assets/Art/BayLoop/{folder}/{n}.fbx' if passed else a['fbx']
 lines.append(f'| {n} | {a["tris"]:,} | {g[n]["score"]}/10 · {"예" if g[n]["yes"] else "아니오"} | {"시험 복사" if passed else "보류"} · [FBX](../../../{target}) | [PNG](../../../art/_review/2026-10-25-bayloop-italy/{g[n]["render"]}) |')
body='''# 아트 — D-137 기억 속 항구

**새 건물7개·생활 소품7개를 독립 게이트7점 이상으로 시험 복사했다. 소품4개는 최종5~6점으로 보류. 기존 항구 창고2개는 유지했다. Unity·프리팹화·씬 배치·커밋·푸시 미실행.**

- [전체 검토판](../../../art/_review/2026-10-25-bayloop-italy/index.html): 마치야↔새 판 동일 카메라 비교·실사진·각 모델 실제 렌더·판정 원문.
- [비교 PNG](../../../art/_review/2026-10-25-bayloop-italy/compare.png), [960×540 거리 배치 PNG](../../../art/_review/2026-10-25-bayloop-italy/street-study-960.png). 모두 실제 Blender 렌더이며 인게임 캡처가 아니다. AI 이미지0.
- 원본 FBX·GLB·blend·geometry·manifest: `art/models/bayloop_italy_v01/`, `art/models/bayloop_italy_props_v01/`. [건물 SPEC](../../../art/models/bayloop_italy_v01/SPEC.md), [소품 SPEC](../../../art/models/bayloop_italy_props_v01/SPEC.md).
- 이전 마치야 모델·렌더·시험 파일은 `art/_review/2026-10-25-bayloop-italy/iterations/pre-d137/`에 보존.

## 형태와 생활의 층

| 요소 | 이탈리아/일본 | 적용·이유 |
|---|---|---|
| shop_house a/b/c | 형태 이탈리아 | 흰색·황토·청회색 회벽, 2/3/2층, 1층 아치와 짧은 천 차양, 녹색 덧창·철 발코니·옥상. 교토 격자·연속 처마·기와 지붕 제거. C는 아치 상층과 퍼골라로 구분. |
| terrace_house a/b/c | 형태 이탈리아 | 후퇴하는 층·옥상 테라스·아치·덧창. B는 3층 외부 계단, C는 옥상 출입실·빨래대로 변형. |
| cliff_villa_a | 형태 이탈리아 | 8×7m 2층, 3아치·후퇴한 상층·넓은 테라스·옥상. 독립 리뷰에서는 카페보다 수리점 겸 주택으로 읽혔다. 카페 용도 표현은 미달이다. |
| 작은 간판 겹침 | 생활 일본식 배치 | 문 옆 눈높이에 작은 표지를 겹침. 짧은 한국어만 사용하여 가짜 문자·한/일/이탈리아어 혼용을 피했다. `상점·수리·배달·영업`, 통과 독립 간판은 `수리·식료품`. |
| 자판기·실외기·화분 줄·우편함·자전거 | 생활 일본식 밀도 | 길가의 장사·거주·이동 단서. 소품이 일본 고유 물건이라는 뜻은 아니며 골목의 배치 방식에 적용했다. |
| 계량함·레몬 화분 | 계획했으나 보류 | 제작·수치검사는 완료했으나 최종 시각5점. 거리 배치 및 F1 복사에서 제외했다. |
| harbor_warehouse a/b | 작업용 항구 건물 유지 | 바랜 함석 지붕·셔터 유지. 주거용 테라코타로 일괄 교체하지 않는 제작 판단이며 이번 실사진으로 창고 재료를 고증한 것은 아니다. |

이탈리아8/생활 일본은 방향 가설이지 면적을 계측한 비율이 아니다. 관광 간판·상표를 복제하지 않았다. 공용 팔레트의 바랜 색, 실제 치수의 설계와 기존 CC0 사진 표면을 사용했다.

## type별 결과

| type / 변형 | tris | 독립 게이트 | 상태·경로 | 정면960 PNG |
|---|---:|---|---|---|
'''+ '\n'.join(lines)+'''
| harbor_warehouse_a | 1,432 | 이전 예·7/10 | 유지 · [FBX](../../../F1/Assets/Art/BayLoop/Buildings/harbor_warehouse_a.fbx) | [PNG](../../../art/_review/2026-10-25-bayloop-buildings/harbor_warehouse_a_front.png) |
| harbor_warehouse_b | 1,774 | 이전 예·7.5/10 | 유지 · [FBX](../../../F1/Assets/Art/BayLoop/Buildings/harbor_warehouse_b.fbx) | [PNG](../../../art/_review/2026-10-25-bayloop-buildings/harbor_warehouse_b_front.png) |

건물은 바닥 앞면 가운데·앞면+Z·1unit=1m, ≤4,000tris. 소품은 ≤800tris, 벽 부착물은 바닥 뒤면 가운데, 화분·자전거는 바닥 가운데. 전체 bounds와 개별 피벗은 manifest/SPEC 기준이며 계단·차양 돌출을 포함한다. 건물 4방향, 소품 앞·뒤 렌더 제공.

## 참고 → 재현 → 적용·검증

Procida·Atrani·Positano 실제 사진을 직접 확인하고 저자·라이선스·다운로드 원문을 [references](../../../art/_review/2026-10-25-bayloop-italy/references/REFERENCES.md)에 보존했다. Commons CC BY/SA 사진은 비례 참고만 했으며 게임 텍스처에 넣지 않았다. 사진 측량 복원이 아니라 기존 building-types 치수 계약에 맞춘 설계다.

기존 `bayloop_buildings.py`의 metre UV 분할·축 변환·실제 렌더를 재사용했다. 이전 마치야를 원래 크기로 새 상점과 동일 카메라·조명에 재렌더해 기준 비교했다. 추가 생성기는 `bayloop_italy_buildings.py`, `bayloop_italy_props.py`, 사진 아틀라스 생성기는 같은 접두사의 textures 스크립트다. 자판기·실외기 치수는 제조사 공개 도면을 참고했고 특정 로고를 복제하지 않았다.

A1 CC0 실제 사진을 32px/m로 축소한 기존 아틀라스를 재사용하고 공용 `pitcrew_d123_32`의 가까운 색 사다리로 치환했다. 추가 잎은 Poly Haven leafy_grass CC0 사진이다. 무작위 얼룩·AI 재질 생성0. 회벽의 2색 사진을 분위수로 다시 나눌 때 단색으로 소실되는 오류는 min/max 중간값 분리로 수정했다. 사진의 반복 마모 자체는 여전히 남은 시각 결함이다. 글자는 OFL Pretendard/Galmuri 실글꼴이며 Galmuri의 실제 원격자 크기와 UV bbox 증거도 보존했다.

| 확인 | 증거·범위 |
|---|---|
| 실제 GLB 검사 | [건물7/7](../../../art/_review/2026-10-25-bayloop-italy/building-checks.json), [소품11/11](../../../art/_review/2026-10-25-bayloop-italy/prop-checks.json) PASS. 실제 tris, 전 삼각형32px/m±10%, 축·피벗·bounds·Nearest·POT·팔레트 밖0·출처 SHA. |
| 단색 면 검사 | 실제 UV를 샘플링해 주요 삼각형(≥0.04㎡)2색 이상. 작은 면은 별도 보고하며 밀도검사에서는 제외하지 않음. 전체가 작은 소품도 적어도 한 삼각형의 사진 변화 요구. 모든 미세 면의 색 변화를 보장한다는 뜻은 아님. |
| 실제 FBX 재임포트 | [건물](../../../art/_review/2026-10-25-bayloop-italy/building-fbx-checks.json), [소품](../../../art/_review/2026-10-25-bayloop-italy/prop-fbx-checks.json). 외부 PNG 없는 임시 경로에서 내장 albedo 복구, GLB와 정점·tris·크기·단위·축 비교. |
| 검사기 회귀 | [29개 테스트](../../../art/_review/2026-10-25-bayloop-italy/checker-regression-tests.txt) 통과, 이전 건물6개 GLB/FBX 회귀 통과. 최종 FBX 비교 보완 내역은 검토 폴더의 감사 기록 참고. |
| 복사·출처 | [trial-delivery.json](../../../art/_review/2026-10-25-bayloop-italy/trial-delivery.json)에 통과14개·보류4개·복사 SHA. `art/provenance.csv`에 URL·라이선스·가공·원본/시험 경로 기록. 기존 창고 FBX/PNG 유지 확인. |

**32색 검사는 원본 albedo 기준**이다. 조명·그림자·AA를 거친 최종 화면 색수는 별도. Blender5.2.2 백그라운드 사용. FBX만으로 Unity Point 설정이나 외부 material remap 연결은 보장되지 않는다.

## 독립 게이트·남은 결함

리뷰어마다 제작의도·코드·이전 점수를 주지 않고 렌더만 제공했다. 파일명과 공통 운영규칙은 보이므로 완전 익명은 아니다. 예/아니오+결함3개+10점 원문은 [gate-summary](../../../art/_review/2026-10-25-bayloop-italy/gate-summary.json)에서 연결한다. 통과 판의 렌더 픽셀 SHA를 고정했으며 실패 후 교정은 최대2회에서 멈췄다.

| 대상 | 점수 흐름 | 최종 결함3개 요약 |
|---|---|---|
| shop C / terrace C | 5.5→7 / 6.5→7 | 균일한 회벽 마모, 막힌 창·가게, 일부 빈 측면/테라스 세부 부족. 원문 각각 참고. |
| cliff_villa | 6.5→7 | 균일한 마모·뒤쪽 빈 측벽·닫힌 전면 아치창. 측후면 창·보조문·탁자·부분 커튼을 보완했으나 카페 식별은 약함. |
| bicycle / planter_row | 5→7 / 5→6→7 | 자전거: 성긴 바구니·각진 안장·단순 손잡이. 화분: 직선 줄기·판형 잎·회색 흙. |
| small_signs_b — 보류 | 6→4→6 | 계단형 글자·반복 얼룩·무거운 외곽 두께. 글자는 복구됐으나 자연스러움 미달. |
| small_signs_d — 보류 | 6→4→6 | 블록형 획·글자 주변 각진 얼룩·두꺼운 옆면/하단. |
| meter_box — 보류 | 4→6→5 | 큰07이 게임 번호판처럼 보임·계기 의미 불명확·배관 연결이 막대 같음. |
| lemon_tree_pot — 보류 | 4→6→5 | 밝은 열매가 돌/종이처럼 보임·앙상한 수관·넓고 회색인 흙. |

7점은 시험 투입 기준이며 결함 해소나 최종 아트 승인이 아니다. 새 건물군은 독립 첫인상에서 남유럽 계열로 읽혔지만 반복 얼룩과 어두운 실내는 다음 인게임 개선 항목이다.

## 다음 단계·승인

**QA/프로그래밍 임포트 필요. 도현 최종 승인 필요 — 인게임960×540 캡처.** FBX14개와 사용 PNG만 허용된 Buildings/Props에 복사했다. 프리팹·콜라이더·LOD·Unity 재질·씬·코드·.meta는 수정하지 않았다. 새 아틀라스는 기존 프리팹의 외부 material remap을 명시적으로 다시 확인해야 한다. Point·sRGB·무압축·원본1024·scale1, 피벗·계단/필지 연결을 확인하고 실제 주행 카메라에서 평가한다. 이번 거리 배치 PNG는 Blender 정적 시험이며 게임 배치·충돌·가독성·성능 검증을 대신하지 않는다. 보류4개는 다음 제작 방향 판단 전 투입하지 않는다.
'''
(R/'docs/team/reports/art-20261025-1929.md').write_text(body,encoding='utf8')
print('REPORT_WRITTEN')
