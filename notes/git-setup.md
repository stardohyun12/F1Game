# Git 세팅 가이드 (도현 터미널에서 실행)

> **Cowork 샌드박스에서는 `.git` 파일 권한 문제로 git init이 막힘.**
> 아래 명령을 도현 컴퓨터의 터미널에서 직접 실행.

---

## 준비

Cowork에서 이미 생긴 깨진 `.git` 폴더가 PitCrew/ 루트에 있음. 먼저 제거 후 새로 초기화.

---

## ⚠ OS별 명령어 차이

`rm`, `rmdir`, `Remove-Item` 등 **파일 삭제 명령이 터미널마다 다름.** 도현 환경은 **Windows cmd(명령 프롬프트)**.

| 동작 | Windows cmd | Git Bash / macOS / Linux | PowerShell |
|---|---|---|---|
| 폴더 삭제 | `rmdir /s /q .git` | `rm -rf .git` | `Remove-Item -Recurse -Force .git` |
| 경로 이동 | `cd C:\...` | `cd ~/...` | `cd C:\...` |

**추천:** 장기적으로 Git Bash 사용. 시작 메뉴 "Git Bash" 검색. Unix 스타일 명령이 표준이라 가이드/구글 자료 그대로 씀.

---

## 0단계: PitCrew 폴더로 이동

```cmd
cd <PitCrew 폴더 전체 경로>
REM 예: cd C:\Users\도현\Documents\PitCrew
```

## 1단계: 깨진 .git 제거 (cmd 기준)

```cmd
rmdir /s /q .git
```

## 2단계: 새로 초기화

```cmd
git init -b main
```

## 3단계: 저장소 전용 author 설정 (선택)

글로벌 설정이 있으면 건너뛰어도 됨. 없거나(commit 시 "Author identity unknown" 에러 뜨면) 이 리포만 다른 이름 쓰고 싶으면:

```cmd
git config user.name "Kim Dohyun"
git config user.email "stardohyun11@gmail.com"
```

(이 프로젝트 리포에만 적용됨. 다른 프로젝트는 영향 없음.)

## 4단계: 전체 스테이지 + 첫 커밋

`.gitignore`는 이미 준비돼 있음. Unity의 Library/, Temp/, *.csproj 등은 자동 제외됨.

```cmd
git add .
git status
```

`git status`로 확인: `CLAUDE.md`, `GDD/`, `dev-plan/`, `notes/`, `F1/Assets/`, `F1/Packages/`, `F1/ProjectSettings/`, `.gitignore` 정도가 올라와야 함. `F1/Library/`, `F1/*.csproj` 같은 건 절대 올라오면 안 됨.

```cmd
git commit -m "init: PIT CREW 프로젝트 초기 커밋 (GDD, dev-plan, notes, Unity 6 기본 프로젝트)"
```

## 5단계 (선택): GitHub 원격 연결

```cmd
REM GitHub에서 먼저 pit-crew 리포 생성 (비공개 권장)
git remote add origin https://github.com/stardohyun12/pit-crew.git
git push -u origin main
```

---

## 확인 체크리스트

커밋 후 다음이 맞는지 확인:

- [ ] `git log` 실행 시 "init: PIT CREW 프로젝트 초기 커밋" 하나 보임
- [ ] `git status` 결과가 "nothing to commit, working tree clean"
- [ ] Library/ 폴더가 커밋에 포함되지 않음 (용량 기가 단위라 포함되면 문제)
- [ ] F1/Assets/Scenes/SampleScene.unity 같은 에셋은 포함됨

---

## 이후 커밋 리듬 (영상 3단계 기반)

- 기능 하나 완료 = 커밋 하나.
- `dev-plan/feature-list.md` 체크박스 갱신과 함께 커밋.
- `notes/session-log.md` 블록 추가와 함께 세션 종료 커밋.
- 커밋 메시지 포맷:
  - `feat: [기능]` — 새 기능
  - `docs: [문서]` — 문서만
  - `fix: [버그]` — 수정
  - `chore: [정리]` — 메타 작업

---

## 자주 쓰는 Unity Git 패턴

- **바이너리 머지 금지:** `.unity` 씬 파일은 YAML로 저장돼 있지만 머지가 까다로움. 가능하면 솔로 작업 중엔 rebase로 한 줄로 유지.
- **Meta 파일:** Unity의 모든 에셋은 `.meta` 짝꿍 파일을 가짐. 둘 다 같이 커밋.
- **LFS:** Kenny 에셋은 대부분 작아서 LFS 불필요. 나중에 대형 텍스처/모델 생기면 도입 검토.
