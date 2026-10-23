# AI 출처 에셋 전수 점검 (2026-10-23)

결론: 게임에 실제로 들어가는 에셋 264개 중 **AI 출처는 5개**. Meshy 화물 모델 4개(케이크·계란판·생선·와인병)와 AI로 생성한 엔진 소리 1개다.
나머지 259개는 Kenney CC0 팩, 스크립트로 만든 것, 폰트다.
정할 것: Meshy 화물 4종을 교체할 순서(브리프 규칙 9 「출시 전 전수 감사」 통과 조건). 엔진 소리를 바꿀지도 정해야 한다.

## 방법
- 대상은 실제로 플레이하는 씬 3개 `Proto_CoastCourse`·`ComplexTrack`·`Slice_PostcardStreet`와 `Assets/Resources` 전체다(런타임 로드).
  Build Settings 목록에는 `SampleScene` 하나뿐이라 그 목록은 쓰지 않았다.
- 실행 중인 F1 에디터에서 `AssetDatabase.GetDependencies(recursive)`로 의존성을 뽑았다 → `deps.tsv`(735줄).
- 판정 근거: 경로, 최초 커밋 메시지, 생성 스크립트, 파일 내 AI 도구 문자열(meshy/openai/c2pa 등)이다.
  `art/provenance.csv`와 sha256도 대조했는데, 일치하는 것은 0건이었다. 장부에 오른 에셋은 아직 `F1/Assets`에 들어오지 않았다.
- 전체 표: `assets.csv`. 시트: `ai-cargo-sheet.png`(원본 FBX 미리보기).

## AI 출처 (5)
| 에셋 | 종류 | 쓰는 곳 | 근거 |
|---|---|---|---|
| Assets/Model/AI/cake_01.fbx | 모델 54k tris | CargoModelCatalog | Meshy, a1feb87a |
| Assets/Model/AI/eggs_carton_01.fbx | 모델 53k tris | CargoModelCatalog | Meshy, a1feb87a |
| Assets/Model/AI/fish_01.fbx | 모델 75k tris | CargoModelCatalog | Meshy, a1feb87a |
| Assets/Model/AI/wine_bottle_lowpoly.fbx | 모델 27k tris | CargoModelCatalog | Meshy, c4fb9633 |
| Assets/Sound/EngineLoop.mp3 | 오디오 | 씬 3개 모두 | a0df81a5 「AI 생성 EngineLoop.mp3」 (D-020) |

모델 4개는 회귀 검사의 BUG-001(화물 고폴리) FAIL 원인과 같은 파일이다.

## 게임 밖이지만 프로젝트 안에 있는 AI 산출물 (빌드 미포함)
- `Assets/Model/Generated/*.fbx` 7개: Hunyuan3D 파이프라인 결과물(a171809f)이다. 게임 씬에서 참조하지 않는다.
- `art/concept/**` 원화와 provenance 의 `ai_generated=yes` 3행: 비교·원화용이다. 규칙상 허용된다.

## 비AI 판정 요약
- Kenney(차·도시 키트, UI 팩, 인터페이스 소리): CC0. `Resources/WrapMaterials/Sfx` 9개는 Kenney 원본과 바이트가 같다.
- LofiPilot·CoastalTown·CoastalFormula·StreetFurniture: Codex가 Blender **코드**로 만든 형태와 아틀라스다
  (`tools/asset-gen/lofi_pilot.py`·`coastal_formula.py`·`street_furniture.py`, 이미지 입력 없음). AI 이미지나 3D 생성은 아니지만 장부(provenance)에는 아직 없다.
- 씬 `_Mats/Mesh_*`: 에디터 메뉴가 생성한 메시다.
- 폰트: Pretendard·Jua·LiberationSans(OFL), malgun.ttf. **malgun.ttf 는 AI 문제는 아니지만 재배포 라이선스를 확인해야 한다.**

## 안 본 범위
- 런타임 코드가 만든 텍스처·메시는 코드라서 대상에서 뺐다.
- 플레이 화면을 하나씩 캡처해 눈으로 보는 판정은 하지 않았다. 대신 의존성 그래프로 전수를 덮었다.
