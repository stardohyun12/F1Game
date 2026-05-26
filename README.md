# PitCrew — 프로젝트 한눈 지도

> 폴더 열었을 때 "뭐가 어디 있나" 헷갈리면 여기부터. 헷갈리는 dotfiles는 맨 아래 "무시해도 됨"에.

## ⭐ 이것만 알면 됨 (자주 가는 5곳)

| 어디 | 뭐가 있나 |
|---|---|
| **`F1/`** | **게임 자체** (Unity 프로젝트). 실제 게임은 다 여기. |
| **`docs/CONTROL.md`** | **관제탑** — 지금 어디까지 왔나 한눈에. 헷갈리면 여기. |
| **`docs/mapreference/`** | 맵 레퍼런스 이미지 (목표 룩). |
| **`옵시디언/`** | 기획·설계 노트 (GDD, 결정 기록, 세션 로그). |
| **`tools/studio-dashboard/`** | 대시보드 서버 (localhost:7878). |

## 📂 루트 전체 (역할별)

**게임**
- `F1/` — Unity 게임. 안에: `Assets/Scripts`(게임 코드) · `Assets/Editor`(개발 도구) · `Assets/Model`(Kenney 에셋 라이브러리, 파일 大多数가 여기) · `Assets/Scenes`(씬)

**작업 문서·지식**
- `docs/` — 관제탑(CONTROL.md) · 레퍼런스(mapreference) · 스샷(screenshots) · 스펙/플랜(superpowers) · 문서지도
- `옵시디언/` — 기획서·결정 이력·세션 로그·하네스 노트
- `references/` — 참조 자료
- `PitCrew_기획서.pptx` / `PitCrew_3D포장_설계.pptx` — 기획 발표 자료

**도구·규칙 (작업 체계 = 얇은 하네스)**
- `tools/` — studio-dashboard (한눈 보기용 웹)
- `scripts/` — `regression-check.sh` 등 자동 검증
- `project_rules/` — 작업 규칙 (scope·asset·session·roles 등)
- `CLAUDE.md` — Claude Code 세션 지침 (자동 로드)

**무시해도 됨 (설정·자동생성)**
- `.git .gitignore .gitattributes` (버전 관리) · `.claude`(Claude 설정) · `.vscode`(에디터+UTF-8) · `.obsidian`(옵시디언 설정) · `.superpowers`(플러그인) · `.env.local`(API 키) · `PitCrew.sln`(VS 자동) · `captures` `.ai_logs`(로그)

## 🧭 작업 방식 (한 줄)

도현 = 창작·공간·시각·디자인 + 판정 / Claude = 시스템·코드·검증·잡일. 하네스(회귀·게이트·hook)는 얇게 유지, "에이전트 회사" 같은 코스튬은 안 만듦.
