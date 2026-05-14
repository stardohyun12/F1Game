# PIT CREW — 자산/코드 관리 규약

> 이 문서는 산업 표준(Unity 매뉴얼, GitHub Unity gitignore 템플릿, 일반 게임 스튜디오 관행)에서 1인 4주 MVP에 필요한 부분만 추린 운영 가이드.
> 새로 합류하는 사람(또는 미래의 자신)이 이거 한 장만 읽고도 자산을 안전하게 추가/수정할 수 있게.

---

## 1. 폴더 구조

```
PitCrew/
├── F1/                              ← Unity 프로젝트 루트
│   ├── Assets/
│   │   ├── Scripts/                 ← 런타임 코드 (PitCrew.asmdef)
│   │   │   ├── Cargo/               ← 도메인별 폴더 (`기능별 그룹`)
│   │   │   ├── Audio/
│   │   │   ├── UI/
│   │   │   ├── Layout/
│   │   │   ├── Order/
│   │   │   ├── Route/
│   │   │   ├── Tires/
│   │   │   ├── Upgrades/
│   │   │   └── *.cs                 ← 도메인 미정 코드는 루트 (드물게)
│   │   ├── Editor/                  ← Editor 코드 (PitCrew.Editor.asmdef)
│   │   │   ├── *.cs                 ← Setup/Verify/Generate/Build 도구
│   │   │   └── _Recovery/           ← 응급 복구·진단 스크립트
│   │   ├── Prefabs/                 ← 모든 prefab
│   │   ├── Models/                  ← FBX/OBJ (Kenney 등)
│   │   ├── Materials/               ← .mat
│   │   ├── Audio/                   ← .wav/.mp3/.ogg
│   │   ├── Scenes/                  ← .unity (현재는 루트에 일부 — 정리 필요 시 이동)
│   │   ├── Resources/               ← Resources.Load 대상 (런타임 검색)
│   │   ├── Data/                    ← ScriptableObject 인스턴스 (.asset)
│   │   └── Settings/                ← URP/Quality 설정
│   ├── Packages/                    ← Unity 패키지 매니페스트
│   └── ProjectSettings/
├── GDD/                             ← 게임 디자인 문서
├── dev-plan/                        ← 일정 + 체크리스트
├── notes/                           ← 결정 이력, 세션 로그, 규약
└── .claude/                         ← Claude Code worktree·skill·hook
```

**원칙**
- 자산 종류별 → `Models/`, `Materials/`, `Prefabs/`, `Audio/`
- 스크립트 도메인별 → `Cargo/`, `Vehicle/`, `UI/` 등
- 둘이 겹치는 건 의도. 자산은 "타입으로 찾는 게 빠르고", 코드는 "기능으로 찾는 게 빠르다."

---

## 2. 네이밍 규약

| 대상 | 규약 | 예시 |
|---|---|---|
| C# 스크립트 | PascalCase, 클래스명과 일치 | `CargoShakeSimulator.cs` |
| ScriptableObject 인스턴스 | PascalCase + 타입 접미사 | `ComplexTrackLayout.asset`, `BasicCargoBox.asset` |
| Prefab | PascalCase | `Race.prefab`, `DeliveryPoint.prefab` |
| Material | PascalCase + 용도 | `CargoBoxMat`, `TrunkWallMat` |
| Scene | PascalCase | `ComplexTrack.unity`, `SampleScene.unity` |
| Kenney 원본 자산 | **원본 이름 유지** (kebab-case 등) | `kenney_car-kit/box.fbx` |
| 폴더 | PascalCase | `Cargo/`, `Editor/_Recovery/` |
| Editor 메뉴 | `PitCrew/<카테고리>/<동작>` | `PitCrew/Cargo/Setup Cargo View (CCTV)` |

---

## 3. Assembly Definitions (asmdef)

현재 2개:
- `Assets/Scripts/PitCrew.asmdef` — 런타임 코드 전체
- `Assets/Editor/PitCrew.Editor.asmdef` — Editor 코드 (Editor 플랫폼만, PitCrew 참조)

**효과**
- Editor 코드가 빌드에 안 섞임 (자동 보장)
- 한 쪽만 수정해도 다른 쪽 재컴파일 안 됨 → 컴파일 5~10초 단축
- 의존성 방향 명시: Editor → Runtime은 OK, 역방향 금지(컴파일 에러로 막힘)

**언제 더 분리하나**
- 한 모듈 컴파일 시간이 거슬릴 때
- 모듈 간 의존성 사이클 발생 시
- 외부에 일부만 패키지로 배포할 때
- 4주 MVP 동안은 이 2개로 충분. 50+ 파일 넘어가면 `PitCrew.Core` (data/SO) 추출 고려.

**새 패키지 추가 시 (예: Cinemachine 임포트)**
1. Package Manager에서 임포트
2. asmdef 참조 부족하면 컴파일 에러 → 어셈블리명 확인 (Library/PackageCache/com.X/Runtime/*.asmdef)
3. `PitCrew.asmdef`의 `references`에 추가

---

## 4. Git 워크플로우

### 브랜치
- `main` — 항상 컴파일 + Play 가능 (출시 가능 상태)
- `claude/<자동생성-name>` — Claude Code worktree (단기 작업)
- `feature/<기능>` — 큰 기능(인프라, 시스템 도입 등)
- 머지: `--no-ff`로 feature 경계 보존

### 커밋 메시지 규약
- `feat(scope): 한국어 요약` — 신규 기능
- `fix(scope): 한국어 요약` — 버그 수정
- `chore(scope): 한국어 요약` — 잡일(인프라, .meta, 설정)
- `docs(scope): 한국어 요약` — 문서
- `refactor(scope): 한국어 요약` — 동작 동일, 구조 변경
- `scope` 예: `cargo`, `vehicle`, `ui`, `session-15`, `main`, `infra`

### 주의 사항
- **커밋 전 Unity 컴파일 통과 확인** (`Console`에 빨간 줄 없어야)
- **scene/prefab 큰 변경 후 단독 커밋** — 다른 코드 변경과 섞지 말 것 (충돌 추적 어려움)
- **`.meta` 파일은 항상 .cs/asset과 함께 커밋** — gitignore가 이미 보장

---

## 5. Git LFS — 바이너리 자산

### 정책 (Conservative — 4주 MVP)
- **신규 바이너리만 LFS** — `.gitattributes`로 자동 처리
- **기존 ~24MB 자산은 일반 git** (마이그레이션 보류)
- 4주차 검증 후 필요하면 `git lfs migrate import`로 일괄 변환

### LFS 대상 (`.gitattributes` 참조)
- 3D: `*.fbx *.blend *.obj *.dae`
- 텍스처: `*.png *.jpg *.tga *.psd *.exr *.hdr`
- 오디오: `*.wav *.mp3 *.ogg *.flac`
- 비디오: `*.mp4 *.mov *.webm`
- 폰트: `*.ttf *.otf`
- 패키지/RT: `*.unitypackage *.renderTexture *.cubemap`

### 새 머신에서 클론 시
```
git lfs install               # 1회
git clone <repo>              # LFS pointer 자동 다운로드
```

---

## 6. Unity Smart Merge (UnityYAMLMerge)

scene/prefab 머지 충돌은 자동 해소.

### 1회 설정 (사용자 환경 — `.git/config`)
```
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
```

`.gitattributes`에서 `*.unity *.prefab merge=unityyamlmerge` 지정 → 머지 시 자동 작동.

---

## 7. 자산 추가 시 체크리스트

- [ ] 적절한 폴더에 배치 (Models/, Audio/, Scripts/<도메인>/)
- [ ] 네이밍 규약 준수
- [ ] 자산 + .meta 같이 커밋
- [ ] (바이너리) `git status`에서 LFS pointer로 표시되는지 확인 → `git lfs ls-files`
- [ ] (스크립트) Unity 컴파일 통과 — `mcp__coplay-mcp__check_compile_errors`
- [ ] (Prefab/Scene 변경) 의도한 변경만 staged 됐는지 `git diff` 확인 (Unity가 무관 필드까지 dirty 만들 수 있음)

---

## 8. 도현 패턴 주의 (CLAUDE.md 보강)

- ExulMundi 때 스코프 부풀림으로 미완성 — 이 규약 자체도 4주 MVP 범위에서 벗어나면 후순위.
- 이 문서를 갱신할 때도 "MVP 안에서 필요한가" 체크. 안 그러면 `notes/ideas-parking.md`로.

---

## 변경 이력
- 2026-04-28: 최초 작성. `.gitattributes`(LFS) + asmdef 2개 + 본 규약 동시 도입.
