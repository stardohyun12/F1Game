# UI 최종 검토 후보 — 프로그래밍 인계

**도현 승인 필요. 승인 전 게임 미적용. QA/프로그래밍 임포트 필요.** 4장 PNG는 실제 게임 캡처 위에 합성한 정적 목업이다. 숫자는 예시이며 `F1/Assets/`는 수정하지 않았다.

## 슬롯 계약

크기와 9-slice 여백은 [이전 D-123 sprites.json](../2026-10-23-d123/sprites.json)과 동일하다. 여백 순서는 **Left / Bottom / Right / Top**, PNG 원본 픽셀 단위다. 10개 모두 RGBA·Point·압축 없음·Mip Maps 없음·NPOT None·Mesh Full Rect·sRGB 켬·Alpha Source Input. 폴더 전체 자동 복사 금지: 승인된 `sprites/*.png` 10개만 선택한다.

| 소유자.슬롯 | PNG 크기 | L/B/R/T | Image.Type | 960에서 원본 고정 크기 |
|---|---:|---:|---|---:|
| UITheme.paperPanel | 256×256 | 16/24/16/16 | **Tiled** | 128×128 |
| UITheme.paperCard | 256×320 | 16/24/16/40 | **Tiled** | 128×160 |
| UITheme.slipHeader | 256×64 | 16/16/16/16 | **Tiled** | 128×32 |
| UITheme.stampRing | 256×256 | 0/0/0/0 | Simple | **128×128 고정** |
| UITheme.buttonPlate | 256×80 | 16/16/16/16 | Sliced | 128×40 |
| UITheme.enamelPlate | 256×128 | 20/20/20/20 | Sliced | 128×64 |
| UITheme.gaugeFill | 128×24 | 8/8/8/8 | Sliced | 64×12 |
| PackingScreen.stampSprite | 300×300 | 0/0/0/0 | Simple | **150×150 고정** |
| PackingScreen.gaugeTrackSprite | 256×32 | 12/12/12/12 | Sliced | 128×16 |
| PackingScreen.gaugeFillSprite | 128×24 | 8/8/8/8 | Sliced | 64×12 |

일반 패널은 표의 크기에서 9분할로 확장한다. 최소 크기는 0.5×(좌+우), 0.5×(위+아래) 이상, 위치·크기는 960 좌표에서 짝수 픽셀. 도장은 자유 크기 조절·회전하지 않는다. 128/150 고정 크기면 셀은 2px다. 300×300 등 NPOT는 기존 UI 슬롯 규격 유지에 따른 것이며 3D 64² 텍스처 규격과 별개다.

게이지 0%는 채움 숨김. 0~8px의 작은 채움은 최소 9-slice 크기를 억지로 표시하지 말고 마스크로 실제 비율만 잘라낸다. 목업의 85/50/45/45%는 108px 내부 트랙에서 92/54/48/48px로 2px 단위 반올림했고 최대 오차는 0.56%p다.

## 정수배와 종이 크기

제안 기준은 **960×540 논리 Canvas**, 1920×1080에서 2배, 2880×1620에서 3배 Point. 임의 화면 크기는 정수 배율로 맞추고 남는 공간을 레터박스 처리한다. 카메라 RenderTexture만 960으로 낮추고 별도 Overlay UI를 비정수 배율로 두면 이 시안과 일치하지 않는다.

960 Canvas의 referencePixelsPerUnit=100, Sprite Pixels Per Unit=200, Image pixelsPerUnitMultiplier=1을 기준 후보로 한다. 스프라이트 원본 4px 셀 / (200÷100) = 논리 2px 셀. border도 원본의 절반이다. Simple 도장은 RectTransform 128²/150². 이는 uGUI 계산에 맞춘 인계 값이며 **Unity에서 실측하지 않았다**. 실제 Canvas와 PNG 셀 폭을 캡처로 확인해야 한다.

현행 `UIRoot.cs:40`은 1920×1080, `PackingScreen.cs:306`의 자체 생성 Canvas는 1280×720이다. 두 Canvas를 960 기준으로 맞추거나 대응 배율을 계산해야 한다. 기존 좌표에 PNG만 교체하면 목업이 재현되지 않는다.

`paperPanel`·`paperCard`·`slipHeader`는 border를 유지한 **Tiled**. 종이 중앙을 Sliced로 늘리면 2px 섬유가 길쭉해진다. [Unity 공식 설명](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.Image.Type.html)을 따라 반복 구간과 고정 모서리를 Pillow로 재현했다. 도장/버튼/게이지의 단색 중앙은 기존 Sliced/Simple 사용.

## 완성 색과 기존 코드 조정

**모든 PNG는 완성 색: `Image.color=Color.white`**. 흰색 마스크용 에셋이 아니다. 투명도 0/255, 실제 RGB는 D-123 32색 안에 있다. 종이 `#E4D6BF`, 섬유/선 `#C4A687`, 잉크·게이지 `#30362F`, 선택 테두리 `#677B80`, 도장 `#8A4631`.

| 읽은 코드 | 프로그래밍팀 적용 사항 |
|---|---|
| `UIPalette.cs:93`, `:115` | border 유무로 모두 Sliced를 고르는 경로에 종이 3슬롯 Tiled 분기. 기존 패널·헤더·도장·버튼의 틴트 곱을 이 후보에서는 흰색으로. |
| `UIPalette.cs:163` ButtonColors | ColorBlock이 다시 색을 곱한다. 기본/호버/눌림 색을 흰색으로 두고 별도 2px 강조선·선택 라벨로 상태를 표현. 비활성도 밝기 혼합 대신 상태 문자와 팔레트 색 도형 사용. |
| `PackingScreen.cs:559`, `:1441` | 재료색 틴트 보존 경로 해제. gaugeFillSprite는 잉크색 완성 PNG. 재료 구분은 목록 선택 테두리+‘선택’ 글자. |
| `UITheme.fontDisplay`, `UIPalette.Display` | Jua 연결·제목체 경로 교체. 가짜 Bold/Italic/Outline 없음. |
| `UIRoot` + Packing 별도 Canvas | 960 논리 좌표, 공용 시계 하나, 단일 ceil 표시. 포장/전화/주문판 별도 타이머 계산 금지. |
| 정산 다음 상태 | ‘돌아가기’는 D-125 무제한 귀로 진입 제안. 현행 Returning은 정산 전 단계이므로 상태 전환 구현 필요. |

모든 상태색을 이번 10장으로 표현했다는 뜻은 아니다. 오류/경고/호버·비활성 동작은 팔레트색 별도 문자/도형 구현이 필요하다. `stampRing`은 긴급·인수 라벨로 뜻을 나눈다. 글자·화살표·선택선은 코드/폰트 레이어이며 이미지에 문구를 굽지 않는다.

## 폰트 크기

아래 수치는 **960×540 논리 픽셀**. 목업은 Pillow 단색 마스크로 렌더한 실제 PNG 기준이며 TTF를 TMP에 연결하는 것만으로 동일 픽셀 결과가 보장되지는 않는다.

| 역할 | 폰트 | 제작 마스크 | 960 em 크기 | 1920 표시 | 행 간격 기준 |
|---|---|---:|---:|---:|---:|
| 큰 제목·목적지 | Galmuri11 | 12px × 3 | 36px | 72px | 44px 이상 |
| 시간·순이익 | Galmuri11 | 12px × 3 | 36px | 72px | 한 줄 |
| 버튼·라벨·도장·재료명 | Galmuri11 | 12px × 2 | 24px | 48px | 32px 이상 |
| 긴 전화 대사·내역·도움말 | Pretendard Regular | 12px 단색 × 2 | 24px | 48px | 32~40px |

Galmuri는 12px에서 글리프를 래스터하고 2/3배 Nearest. Pretendard는 이번 목업에서 비ASCII 글자 뒤 1원본픽셀(논리 2px) 여유를 더했다. TTF 자체를 수정하지 않았다. 실제 TMP는 고정 글리프/Point 비트맵 atlas 또는 동일 픽셀 경계를 보존하는 설정을 QA가 비교해야 한다. SDF 매끈함·한글 폴백·다른 언어·자동 축소 결과 **[미확인]**. OFL 문서를 동봉하고 글꼴 단독 판매·이름 변경 조건은 각 원문을 따른다.

## 화면과 흐름

| 화면 | 꼭 보여야 할 정보 3개 | 이 시안의 다음 행동 |
|---|---|---|
| 주문판 | 남은 시간 / 목적지·화물 / 선택 주문·보상 | 포장하러 → 포장 |
| 긴급 전화 | 요청자·화물 / 기본·추가금 / 공용 시간 | 추가 또는 거절 → 주문판 복귀 |
| 포장 | 남은 시간 / 현재 면·보호 / 포장비·보상 | 배치하기 → 적재 확인 → 출발 |
| 영수증 | 성공·화물 결과 / 순이익 / 다음 행동 | 돌아가기 → 시간 제한 없는 귀로 |

4장은 독립 순간의 합성 예시다. 전화 추가·거절·만료, 포장재 전환, 상자 회전·드래그, 손상/실패 영수증, 귀로 전환은 미구현·미검증이다. 이 태스크는 멈춘 화면 4종의 아트 후보까지다.

## 재현 / 확인

저장소 루트에서:

```powershell
py art/_review/2026-10-23-ui-final/build.py
py tools/asset-gen/check_asset.py --ui-manifest art/_review/2026-10-23-ui-final/ui-manifest.json --palette art/source/palette/pitcrew_d123_32.json --out art/_review/2026-10-23-ui-final/validation/check-asset-ui.json
py art/_review/2026-10-23-ui-final/verify.py
```

`build.py`는 검토 폴더와 해당 작업의 provenance 10행을 갱신한다. `verify.py`는 정적 PNG·2배 일치·문구 겹침·막대 길이·변조 8종 거부·로컬 브라우저를 검사한다. Unity/Play 실행 없음.

**도현 승인 필요:** 화면 4종의 위계·종이 거칠기·폰트, 완성 색/Tiled 계약, 정산 뒤 귀로 버튼. 승인 후 프로그래밍팀 적용 → QA가 960/1920/2880 실제 캡처, 폰트·공용 타이머·포장 입력·귀로를 확인한다.
