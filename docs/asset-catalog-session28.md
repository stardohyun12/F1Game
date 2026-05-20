# 제작 자산 카탈로그 — 세션 28 (2026-05-20)

> 이번 세션에 만든 비주얼 자산·시스템 모음. 스크린샷은 로컬(`docs/screenshots/auto/`)에 있어 이 PC에서 미리보기로 열면 이미지가 보임.
> **제작 전략**: 각진 것=프로시저럴/Kenney(룩 매칭·무료), 유기적 hero=무료팩 또는 Meshy(가끔), 2D=gpt-image-1.

---

## 1. 라이팅 · 하늘 (도시 전체 분위기)

**진단**: "진짜 게임 느낌" 안 나던 건 에셋이 아니라 **아트디렉션 부재**. 그림자 없음 + 기본 스카이박스 + 앰비언트 과다 + fog 꺼짐이 평평함의 80%.

| Before (평평·물빠짐) | After (낮 무드 + 그라데이션 하늘) |
|---|---|
| ![](screenshots/auto/auto_20260520_140623.png) | ![](screenshots/auto/auto_20260520_141748.png) |

**그라데이션 하늘** (지면 시점):

![](screenshots/auto/auto_20260520_142941.png)

- **MoodProfile SO + MoodController**: 한 "무드"(낮/황혼/야간)의 태양·앰비언트·fog·스카이박스·포스트를 데이터 한 덩어리로. **무드 추가 = 코드 0, SO 복제 후 값만 변경.**
- **Mood_Daytime**: 그림자 Soft + 따뜻한 태양 + Flat 앰비언트(대비) + 옅은 fog + 포스트(대비/채도/노출).
- **GradientSky.shader**: URP HLSL 스카이박스 자가 작성 (지평선→천정→지면 3색).
- 만든 것: 코드/셰이더 (다운로드 0, 크레딧 0).

---

## 2. 차고 F1 머신

placeholder 텍스트 `[ F1 머신 사이드뷰 ]` → **회전하는 3D 레이스카** (Kenney `race.fbx`를 RenderTexture에 렌더해서 차고 패널에 표시).

| F1 머신 (RT 렌더) | 차고 화면 in-panel |
|---|---|
| ![](screenshots/auto/machineRT_direct.png) | ![](screenshots/auto/auto_20260520_145106.png) |

- 메인 카메라가 못 보는 먼 곳에 모델+조명+전용 카메라 스테이지 → RT → 패널 RawImage.
- 차고 열릴 때만 렌더 + 천천히 회전(컷씬 느낌).
- 만든 것: 보유 Kenney 모델 + 코드.

---

## 3. 화물 박스 (포장 미니게임 — 회색 큐브 졸업)

**3D 화물은 AI-gen 대신 프로시저럴로.** (Meshy는 각진 박스에 흐물 → 부적합. §5 참조)

**카드보드 택배 박스** (텍스처 코드 생성 — 결+플랩+테이프+송장):

![](screenshots/auto/cardboard_box.png)

**화물 5종 구분** (색 틴트 + 종류색 핸들링 스티커 + 취급주의 "!"):

![](screenshots/auto/cargo_types.png)

| 종류 | 색 | 운전 민감도 |
|---|---|---|
| Box | 무지 카드보드 | 균형 |
| 계란 | 상아 | 충격(급가속·제동) |
| 유리 | 청 | 충돌 절대금지 |
| 활어 | 청록 | 회전(급커브·드리프트) |
| 케이크 | 핑크 | 기울기(경사) |

- 메시·콜라이더 불변 → 포장 리본 시스템 무관(안 깨짐). 머티리얼만 종류별.
- 만든 것: 코드 (`CargoBoxAppearance`).

---

## 4. AI 자산 파이프라인 (구축됨, 대기)

| 도구 | 상태 | 용도 | 교훈 |
|---|---|---|---|
| **Meshy** (3D) | 키 연결됨 (`.env.local`, 잔액 2080) | **유기적 hero만** | 각진 박스/크레이트엔 흐물 → Kenney/프로시저럴 우월 (크레딧 ~5로 검증) |
| **gpt-image-1** (2D) | 키 미설정 | 부품·화물·UI **아이콘 세트** | ROI 최고 지점. OpenAI 키 받으면 시작 |

**Meshy 크레이트 실험** (흐물 — 왜 박스는 프로시저럴이 나은지):

![](screenshots/auto/cargo_crate_01.png)

---

## 5. 파일 위치

| 자산 | 경로 |
|---|---|
| 무드 시스템 | `F1/Assets/Scripts/Visual/MoodProfile.cs`, `MoodController.cs` |
| 낮 무드 데이터 | `F1/Assets/Settings/Mood_Daytime.asset` |
| 하늘 셰이더 | `F1/Assets/Settings/GradientSky.shader` + `.mat` |
| 차고 머신 | `F1/Assets/Scripts/Garage/GarageScreen.cs` (BuildMachineStage) |
| 화물 외형 | `F1/Assets/Scripts/Cargo/CargoBoxAppearance.cs` |
| AI 키 | `F1/../.env.local` (gitignore됨) |

---

## 6. 다음 후보

- 화물 **종류별 심볼** ("!" → 냉장 눈송이 / THIS UP 위화살표 등)
- 포장 플로우 **실제 검증** (정산→포장 박스+리본)
- **OpenAI 키** → 2D 아이콘 세트 (부품·화물·UI)
- 무료 음식팩(Kenney Food/Quaternius) 조사 — 유기적 hero 화물
- 하늘에 태양 글로우/구름 한 겹
