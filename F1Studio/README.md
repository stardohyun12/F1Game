# F1Studio — 건물 스튜디오 전용 Unity 프로젝트

메인 `F1/` 에디터(맵·프로그래머)와 **동시에** 띄워 두고, 건물 라이브러리(artlive) 작업만 여기서 한다.
Unity 6000.3.19f1 · URP 17.3.0 · unity-cli-connector(HTTP 브리지). 같은 머신에서 두 에디터가 떠도 서로 상관없다(커넥터 포트 자동 할당: 메인 8090, 스튜디오 8091~).

## 구조 — 공유는 정션, 환경은 복사

| 경로 | 종류 | 실체 |
|---|---|---|
| `Assets/Art/Library` | **정션** (`mklink /J`) | `F1/Assets/Art/Library` — 건물 프리팹·재질·텍스처·fbx·WallClock.cs·A7LitPoint_garage.shader. 여기서 만든 프리팹이 곧 메인 라이브러리 |
| `Assets/Art/SceneA7` | **정션** | `F1/Assets/Art/SceneA7` — SceneA7Lit.shader, 재질 템플릿 `Mats/SA7_house_shop.mat`, A7_RPAsset |
| `Assets/Settings/` | 복사(.meta 포함 = 같은 GUID) | URP 파이프라인·렌더러·볼륨(PC_RPAsset·PC_Renderer·SampleSceneProfile…) |
| `Assets/Art/SceneA2|SceneA3/…` 7개 | 복사 | 스튜디오 씬 땅·길·하늘 재질이 쓰는 텍스처 2+2+1, `d_sky.asset`, `SceneA3Look.shader` |
| `Assets/Scenes/Studio_Building.unity` + `_Mats/` | 복사 → **git** | 스튜디오 씬(이쪽이 기준; 메인의 같은 이름 씬은 옛 사본) |
| `Assets/Scripts/Visual/A7LightToggles.cs`, `Scripts/Debug/LowResRenderTest.cs` | 복사 → **git** | 씬이 참조하는 스크립트 2개(GUID 동일) |
| `Packages/manifest.json`, `ProjectSettings/` | **git** | 메인과 같은 색공간(Linear)·파이프라인(PC_RPAsset)·품질. `productName: F1Studio`. 메인의 MCP 패키지(`com.unity.pipeline`)는 일부러 뺐다(메인 에디터의 MCP 가 "Multiple Editors" 로 막히지 않게) |

git 에는 씬·스크립트·manifest·ProjectSettings·이 README 만 들어간다(`.gitignore` 의 `F1Studio/*` 절). 나머지는 아래 스크립트가 다시 만든다.

## 다시 만들기 (새 체크아웃·정션이 깨졌을 때)

```bash
bash scripts/studio-link.sh            # 정션 2개 + 폴더 .meta + Settings/SceneA2/SceneA3 복사 + (없으면) ProjectSettings 복사. 멱등
bash scripts/studio-link.sh --check    # 상태만
powershell -NoProfile -File scripts/open-studio.ps1   # 에디터 띄우기(에이전트 Job 밖, WMI). 첫 실행은 패키지·임포트로 몇 분
UCLI_PROJ=D:/Github/F1Game/F1Studio bash scripts/ucli-http.sh status   # "F1Studio Editor connector: port 809x" 면 준비
```

정션은 git 이 추적하지 않으므로 `git clone` 직후엔 `Assets/Art/Library` 가 없다 — 반드시 `studio-link.sh` 를 먼저.

## 도구 라우팅

- `scripts/ucli-http.sh` — 환경변수 `UCLI_PROJ` 로 프로젝트를 고른다(없으면 메인 `F1`).
- `tools/art/build_asset.py` unity 단계, `tools/art/bin/studio_cap.sh` — 기본값이 F1Studio. 다른 씬이 열려 있으면 `Studio_Building` 을 자동으로 연다(`docs/qa/tmp/studio_open.cs`).
- `scripts/team.sh artlive …` — artlive 역할에 `UCLI_PROJ=F1Studio` 를 넣고, 메인 `editor.lock` 은 잡지 않는다.

## 주의

- 두 에디터가 같은 `Art/Library` 폴더를 본다. 스튜디오가 프리팹을 다시 쓰면 메인 에디터는 다음 포커스/Refresh 때 재임포트한다(정상). 같은 파일을 양쪽에서 동시에 고치지는 말 것.
- `Library/`·`Temp/`·`Logs/` 등 Unity 생성물은 전부 gitignore.
- 메인 `F1/ProjectSettings` 를 바꿨으면 스튜디오엔 자동 반영되지 않는다 — `F1Studio/ProjectSettings` 를 지우고 `studio-link.sh` 를 다시 돌리거나 필요한 파일만 복사.
