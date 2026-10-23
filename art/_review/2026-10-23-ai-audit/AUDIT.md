# AI 출처 에셋 전수 점검 (2026-10-23, PD)

방법: `Scenes/Proto_CoastCourse.unity` + `Resources/` + `Data/Orders/` 에서 GUID 의존성을 재귀 추적(정적, 에디터 미실행). 시각 파일 39개, 머티리얼/프리팹/메시 에셋 포함 286개. 런타임 코드 생성 메시는 범위 밖.

## 1. 게임 안 (출시 빌드에 들어가는 것)
| 묶음 | 파일 | 출처 | AI |
|---|---|---|---|
| 화물 4종 | `Model/AI/cake_01, eggs_carton_01, fish_01, wine_bottle_lowpoly.fbx` | Meshy | **예** — 교체 대상 |
| 건물·소품 12종 + LofiAtlas | `Art/LofiPilot/*` | `tools/asset-gen/lofi_pilot.py` (bpy + 코드 픽셀) | 아니오 |
| 차 | `Art/CoastalFormula/*` | `coastal_formula.py` | 아니오 |
| 아스팔트 | `Art/StreetFurniture/AsphaltAggregate.png` | 스크립트 (원화는 imagegen 참고) | 아니오 |
| 씬 머티리얼·메시 82개 | `Scenes/Proto_CoastCourse_Mats/*` | 빌드 메뉴 생성 | 아니오 |
| UI | `UI/kenney_ui-pack` 10장, kenney car-kit 2 | Kenney CC0 | 아니오 (단, 범용 키트 티) |
| 엔진음 | `Sound/EngineLoop.mp3` ← `Prefabs/Race.prefab` | AI 효과음 서비스(ElevenLabs 추정, `Sound/LICENSE.txt`·session-log D-020) | **예** — 소리라 시각 점검에서 빠졌음 |
| 효과음 13 | `Resources/WrapMaterials/Sfx/*.ogg` 9, `Sound/kenney_interface-sounds` 4 | Kenney CC0 (Sfx 9개는 해시로 원본 일치 확인) | 아니오 |

→ 게임 안 AI 파일은 **Meshy 화물 4개 + 엔진음 1개**. 화물 4개는 프리팹 머티리얼이 단색(`Mat_Cargo_*`, 텍스처 없음)이라 FBX 안의 Meshy 텍스처는 화면에 안 나오고 **형태(54k·53k·75k·27k tris)만** AI.
교차 확인(2026-10-23 22:2x): 이 PC의 AI 생성 이미지 485장(`~/.codex/generated_images`, ComfyUI output)과 `F1/Assets` 이미지 전체(TownGen·Kenney·TMP 제외) 해시·dHash 대조 → 복사본·근사본 0. 대체품 `art/models/cargo_{cake,eggs,fish}_v02`, `cargo_wine_v01`, `cargo_box_final_v2` 는 만들어져 있고 임포트(BUG-001) 대기.

## 2. 도현이 보는 이미지 (AI 티의 실제 원인)
| 이미지 | AI 부분 |
|---|---|
| 기준 원화 `art/concept/lofi-styles-2026-10-13/02_warm_street.png` (D-096) | **전체가 imagegen.** 가짜 픽셀(칸 크기 제각각), 매끈한 그라데이션 하늘·바다, 의미 없는 잔디테일. 모든 아트 판정·비교의 기준으로 쓰였다 |
| 언덕 집 검토 `art/_review/2026-10-23-hillhouse/composite*.png` | 배경 `concept-empty.png` 가 imagegen. 3D 집만 진짜 |
| 차 검토 `art/_review/2026-10-23-car/coast-plate.png` | 실제 캡처 + 차 뒤 도로를 AI 인페인팅 |
| 화물 v2 몽타주 `cargo-set-v2` | 배경 = 위 기준 원화(AI) |

→ 팀은 "AI 원화처럼 보이게"를 목표로 일해 왔고, 검토판도 AI 배경 위에 얹혀 나갔다. 실제 게임 캡처는 AI 티보다 **비어 있고 평평한** 쪽 문제(들판·집·나무 한 그루, 단색 하늘).

## 3. 조치
1. 검토판 규칙: 배경은 실제 게임 캡처 또는 bpy 렌더만. AI 플레이트·인페인팅 금지.
2. 기준 원화 교체: AI 원화 대신 실사 기준(도현 사진/실제 게임 스크린샷 레퍼런스) — 도현 결정.
3. Meshy 화물 4개 → 대체품 임포트(BUG-001), `Model/AI/` 제거. 엔진음은 CC0 실녹음(freesound CC0, D-020 원래 조건)으로 교체 — 출시 전 Steam AI 공개 대상.
4. `check_asset.py` 에 "출시 의존성 중 provenance ai_generated=yes 또는 장부 누락 → FAIL" 추가.
