# 카트 v6 — 도현 승인 필요 / QA·프로그래밍 임포트 필요

`kart_v6.fbx` + `kart_v6_baked32.png`, 1,864 tris, 32색 팔레트 중28색,1024×256 PNG. 실제 사용색은 validation.json. 정비소 `scene-a-v5b-garage/palette32.gpl`과 바이트 동일. V5와 같은32texel/m(작은 면 최소2px), Point·sRGB·비압축·Clamp·UV0 한 개. Unity에서는 Unlit 텍스처 슬롯에 연결, tint white. FBX 자체는 호환용 diffuse 이미지 연결이므로 Unlit 자동 설정을 가정하지 말 것.

## 형태와 좌표

1 unit=1m, FBX Y-up, 앞=Unity +Z, 오른쪽=+X. **차체 전체1.28(X)×1.80(Z)×0.82(Y)m**. Blender X/Y/Z는 너비/앞/높이. UV·메시·피벗은 FBX 재임포트 검사함. 1,864 tris에는 바퀴와 랙/끈 모두 포함.

| 파트 | Unity 좌표(기존 Race 기준 실미터) | 반지름 | 폭 |
|---|---|---:|---:|
| k_tire_FL / k_hub_FL | (-0.44,0.24,0.54) | 0.148 | 0.16 |
| k_tire_FR / k_hub_FR | (+0.44,0.24,0.54) | 0.148 | 0.16 |
| k_tire_RL / k_hub_RL | (-0.52,0.34,-0.51) | 0.222 | 0.24 |
| k_tire_RR / k_hub_RR | (+0.52,0.34,-0.51) | 0.222 | 0.24 |

앞 실제 트랙0.88, 뒤1.04, 평균0.96m. 지시한 트랙0.96은 KartWheelVisual의 공통 파라미터이고 기존 앞/뒤 시각 중심은 다르므로 ±0.48로 옮기지 않았다. 휠베이스1.05m. 이름8개 그대로 분리, 각 원점=축 중심·로컬X=차축. tire/hub 쌍을 Wheel_FL/FR/RL/RR 부모에 묶어 `KartWheelVisual.wheels`에 순서대로 지정하고 나머지를 body에 연결할 것. 코드가 이름을 직접 찾는 것이 아니라 기존 씬 빌드가 접두어로 묶고 SerializeField를 연결하는 구조임을 읽기로 확인했다.

**중복 보정 금지:** 기존 ×1.47·길이 재축소·뒷바퀴 −0.08m 보정을 다시 적용하지 말 것. Race scale0.7 아래 붙일 경우 부모 스케일을 상쇄해 월드1m를 유지. 원점은 기존 Race 기준 좌표 보존을 위해 바닥 중심이 아니다. bounds는 X±.64, Z−.81~+.99, Y.092~.912. 앞 바퀴 바닥.092, 뒤.118m의 기존 높이 차를 그대로 보존했다. 새 접지·서스펜션·물리 조정은 이번에 하지 않았다.

구성: 낮은 튜브프레임, 플라스틱 노즈·옆포드·뒤범퍼, 번호07, 열린 버킷시트, 작은 핸들, 오른쪽 뒤 엔진·냉각핀·체인가드, 넓은 뒤타이어, 배달 개조는 뒤 소형랙+끈 하나. 브랜드 복제 없음.

## V5 재사용과 변경

기존 `scene_a_v5_bake.py`, `scene_a2_quantize.py`의 면별 UV, 낮은 해 energy2.4/color(1,.83,.60)/방향(.7,.65,-.45), 청록 하늘(.30,.52,.65)/강도.65, Cycles128samples·4bounces, diffuse+AO, AO(.70+.30AO), 청록 암부·노출+1stop·팔레트 최근접/디더 없음 방식을 재사용. 실내등 추가판 V5b의 실내 보정은 야외 카트에 적용하지 않음.
사진은 비율·부품 참고만. 새 표면은 코드로 칠한 바랜 청록/금속 칠 벗겨짐/큰 긁힘/번호판. 사진복사·AI 이미지 없음. 번호07은 베이크 필터에서 획이 뭉개져 동일 아틀라스 밀도에 팔레트 두 색을 마지막으로 스탬프했다(명시적 변경). 전체를 원본 V5의 무변경 재현이라고 부르지 않음.

## 미리보기와 검증 한계

시트 `art/_review/2026-10-27-kart-v6/sheet.png`, 뒤 상세, 위/옆/뒤, 검은 실루엣, 기존 kart_a2와 비교. 카메라 B는 이번 과제 숫자 우선: 뒤5.6m·높이2.8m·수직64°, lookahead6m/lookheight1m는 기존 렌더 코드 가정. 정비소 앞3m 위치. 기존 코드에서 이름B였던4.2/2.2m 설정은 사용하지 않음.
기존 kart_a2 비교는 원형의 길이만1.80m로 정규화했으며 게임의 개별 바퀴 내림까지 재현하지 않았다. 정비소 원형FBX/PNG 그대로 사용, Unity 조명·후처리·안개·주행은 재현하지 않음.
검증: fbx-validation.json·validation.json·texture-check.json 및 art/source/kart-v6/verify.log. Blender 외부 재임포트만 수행; Unity/Play·실제 KartWheelVisual 연결 미검증. 시안 승인 전 F1/Assets에 넣지 않음.

재생성: textures.py → Blender build.py → quantize.py → Blender export_render.py → Blender verify_fbx.py → package.py (`art/source/kart-v6/`, Python3.13 / Blender5.2.2).
