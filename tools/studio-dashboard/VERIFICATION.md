# 검증 기록 — 2026-10-14

## 결과

- 최종 실행 주소: **http://localhost:7878/**. /legacy도 HTTP 200.
- Windows 런처 `start.ps1 -Restart`: 정상 종료 코드 0, 백그라운드 서버 시작 확인.
  초기 상태 확인이 localhost의 IPv6 재시도로 시간 초과하는 현상을 발견해, 런처 내부 확인 주소를 127.0.0.1로 변경하고 재검증했다.
- `node --check web/app.js`, Python py_compile 통과.
- `test_hub.py`: **13개 통과**. 영속성/재시작 복구, 중복 실행 차단, 참조의 출처·불변 스냅샷,
  선택한 컨텍스트만 전달, 승인 및 취소, 잘못된 모델 옵션, 외부 대화 읽기 전용,
  JSONL 부분 기록, tail 변경 시 메시지 ID 안정성, 요약의 발화자 구분, 보관/복원,
  호환 API 스트리밍, Host/Origin/토큰 검사를 다룬다.

## 실제 제공자와 원본 재현

먼저 앱 코드와 분리한 `probe.py`에서 공식 초기화·대화 순서를 재현했다.
두 제공자 모두 `READY` 응답 후 같은 세션에서 `PITCREW`를 기억해 답했다.
그 뒤 이 앱의 저장·맥락·승인 계층을 적용해 다음을 확인했다.

| 항목 | Claude | Codex |
| --- | --- | --- |
| 실제 카탈로그 조회 | SDK가 13개 모델을 반환 | app-server가 4개 공개 모델을 반환 |
| 명시적 모델 및 effort 실행 | sonnet / low → claude-sonnet-5-5 보고 | gpt-6-astra / low → 같은 모델 보고 |
| 프로세스 사이 resume | 이전 턴의 COPPER를 답함 | 이전 턴의 COPPER를 답함 |
| 파일 작업 | Write 승인 후 테스트 파일 VERIFIED | 파일 변경/명령 승인 후 테스트 파일 VERIFIED |
| 승인 대기 중 취소 | interrupted, 파일 미생성, 대기 요청 해제 | interrupted, 파일 미생성, 대기 요청 해제 |

실제 제공자 검증은 `live_check.py`와 `live_cancel_check.py`를 실행했다.
테스트 파일은 `.runtime/live-claude-check.txt`, `.runtime/live-codex-check.txt`이며 게임 파일 수정은 요청하지 않았다.
라이브 검증은 일반 제공자 사용량을 소비한다. 모든 모델과 모든 속도 티어를 호출한 것은 아니다.
카탈로그는 계정과 CLI 버전에 따라 달라진다.

## 브라우저 확인

- 실제 모델 목록과 모델별 추론 옵션을 표시하고 저장했다. Claude Sonnet/low 요청의 실제 응답을 확인했다.
- Claude 응답을 UI의 **전달** 버튼으로 Codex에 첨부했다. Codex가 `PITCREW | 출처 역할: 기획 · 조율`로 답했다.
- 당시 참조 내용과 출처가 **전달된 컨텍스트**에 남는 것을 확인했다.
- 두 대화를 나란히 표시하고, 비교 패널의 메시지에서도 컨텍스트를 열었다.
- 서버 재시작·새로고침 후 대화·모델 설정이 유지됐다.
- 현재 Codex 대화와 기존 Claude 대화를 실제 프로젝트 기록으로 연결했다.
  Claude 팀원 16개의 이름·작업 설명을 실제 `.meta.json`에서 표시하고, verifier-scale의 대화/결과를 직접 열람했다.
- 긴 컨텍스트 요약·앱 삽입 기록은 일반 사용자의 말과 구분해서 접어 표시했다.
- 1440×940 데스크톱과 390×844 좁은 화면을 검사했다. 좁은 화면의 documentWidth는 390이고 입력창이 화면 안에 있었다.
- 최종 브라우저 콘솔 error 0건, 서버 stderr 비어 있음.
- 화면 증거: `.runtime/workspace-proof.jpg`, `.runtime/models-proof.jpg` (로컬 파일, Git 제외).

## 검증 범위 밖

- 추가 OpenAI 호환 제공자는 로컬 HTTP/SSE 테스트 서버로 요청 형식·모델 전달·스트리밍을 검증했다.
  실제 Ollama/Gemini/기타 외부 계정이 구성된 것은 아니며 해당 제공자의 인증·요금·도구 기능은 검증하지 않았다.
- 외부 앱 세션은 읽기 전용이다. 같은 세션을 두 프로세스가 동시에 조작하는 기능은 구현하지 않았다.
- 강제 프로세스 종료 중 이미 실행한 파일 작업의 자동 롤백, 복수 에이전트의 Git 충돌 병합,
  인터넷 공개 배포, 장시간 부하 테스트는 범위에 포함되지 않았다.

## Model picker UX verification — 2026-10-14
- Reused installed CLI/SDK model catalogs and existing backend validation; no static model catalog or fabricated capability claims added.
- Added focused searchable model picker from conversation model/effort buttons, model descriptions/IDs, supported reasoning choices, speed and adaptive thinking controls, and explicit next-turn apply/cancel.
- Browser verified: actual Codex four-model and Claude thirteen-entry catalogs; Luna removes unsupported ultra and reports its own Fast option; cancel retains saved settings; applying unchanged Codex settings succeeds.
- 390px mobile viewport: document width 390px, dialog width 358px, no horizontal overflow. Desktop 1440px checked.
- Node syntax and 13 existing backend tests passed. This UI-only change did not launch additional paid AI turns.

### Connection resilience — 2026-10-14
- Reviewed Orca (stablyai), Happy, Vibe Kanban, Claudia source and TanStack Query recovery primitives; source paths and adoption decisions in RESILIENCE.md.
- Node fault tests: 4 passed, including simulated outage exceeding one hour and a dropped POST sent exactly once. Existing Python tests: 13 passed.
- Browser with read-only fault proxy: HTTP 503 -> automatic recovery without reload; draft text unchanged. A 20-second stalled bootstrap triggered the 12-second timeout and retry banner.
- Fixed a browser-only timer receiver binding error discovered during verification; reran the fault tests and browser flows after the fix.
- Main server remained alive before investigation; exact historical disconnect cause is unconfirmed. No multi-hour paid AI run was performed.
- The stalled-request browser case also recovered automatically after the proxy returned online; draft survived. Production page reloaded successfully with no console errors. The temporary proxy was stopped and its browser tab closed.

### Independent execution runtime and team tree — 2026-10-14
- Real-process integration test: UI process terminated during a local mock provider stream; restarted UI reattached to the same runtime PID and token. Response completed, provider invoked once. Duplicate runtime launch exited without opening another owner; unauthenticated runtime access rejected; active runtime shutdown refused.
- Actual installed UI restarted with start.ps1 -Restart; runtime PID and token remained unchanged.
- 14 backend tests, 4 connection tests and the runtime integration test passed. Crash recovery preserves partial response and clears obsolete streaming/tool-running flags.
- Browser: team overview shows the linked Claude parent's 16 children, linked child not duplicated as a root; child navigation opens verifier-scale as a read-only conversation. Mobile 390px has no horizontal overflow (dialog 358px).
- No paid AI generation was used for these tests; external CLI/provider shutdown behavior is not a multi-hour soak test.
- A validation run exposed an intermittent 403 being surfaced as 503 while reading an upstream rejection. Bounded POST bodies are now consumed before returning authentication errors; the proxy also preserves a received HTTP error status if its body is reset. Backend and real-process tests passed after the fix.


## 2026-10-14 팀 구성 UI

- 기존 Orca/Happy 조사에서 채택한 실행 서비스와 UI 분리 원칙 위에 팀 구성/명시적 맥락 연결을 추가. 새 외부 코드는 복사하지 않음.
- 오프라인 Python 16개 + 연결 JS 4개 + 실제 프로세스 재시작 통합 1개 통과. 팀 저장 지속성, 팀 목표 범위, 새 에이전트 소속, 중복 가져오기 및 보관 복원 검증 추가.
- 별도 7879 테스트 DB에서 브라우저로 팀 생성 → Claude 기록 가져오기 → Codex의 참고 대화 연결 저장 → 동일 기록 재가져오기(2명 유지)를 검증. 실제 모델 호출 없음.
- 390×844 화면에서 팀 메뉴/가져오기 및 수평 넘침 없음 확인. 실제 7878에 UI 반영 확인. 실제 사용자 팀 구성은 변경하지 않음.
- 가져오기는 현재 프로젝트 로컬 기록의 읽기 전용 연결. 다른 프로젝트, 웹 Claude 대화, 원래 CLI 세션 인계는 이 변경의 지원 범위가 아님.


## 2026-10-14 원래 세션 재개

- 공식 Codex App Server `thread/resume`와 Claude Agent SDK `resume` 계약을 적용. 기존 실행 어댑터를 재사용하고 가져온 세션의 metadata UUID/프로젝트 검증, UI 활성화, 기록 스냅샷, 중복 연결 및 세션 ID 변경 거부를 추가.
- 테스트 전용 대화를 네이티브 CLI로 생성 → 원래 Hub DB 폐기 → 새 Hub DB에서 발견/가져오기 → 동일 UUID로 재개. Codex/Claude 모두 랜덤 테스트 단어 기억, 원본 JSONL에 다음 턴 추가, 동일 UUID 검증 PASS. 사용자 기존 작업 세션에는 메시지를 보내지 않음.
- Codex 모델 gpt-6-astra / Claude 모델 claude-fable-5-1. 결과는 로컬 `.runtime/resume-codex-verified.json`, `.runtime/resume-claude-verified.json`.
- 오프라인 Hub 테스트 18개. 세션 metadata 검증/프로젝트 불일치/하위 에이전트 거부, 카드/팀/기록 유지, 재가져오기 중복 방지 검증 추가.
- 별도 7879 DB에서 브라우저로 원본 가져오기 → 재개 버튼 → 입력창 활성화 및 모델/원본 세션 배지, 새로고침 후 지속 확인. 실제 7878에 버튼 반영 확인.
- 미검증/미지원: 여러 외부 앱의 동시 원본 쓰기 제어, 원래 앱 전용 동적 도구, 실행 중 팀원 프로세스 인계. 화면의 원본 기록은 연결 당시 스냅샷이며 전체 세션 내역 내보내기는 아님.


## 2026-10-14 사용량 / 여러 프로젝트 / 디자인 완료 검증

- 자동 검증 29개: Hub 18, Workspace 5, 실제 프로세스 Runtime 2, 연결 JS 4. app.js/teams.js/workspace.js 구문 검사 통과.
- 실제 Codex app-server 사용량 응답과 Claude OAuth usage 응답 성공. UI에서 Codex 7일, Claude 5시간/전체 7일/Fable 7일과 각각 초기화 시각 확인. 값은 조회 당시 수치이며 계정 전체 사용량.
- 프로젝트 테스트: 한글/공백 폴더 등록, 정규 경로 중복 등록 방지, 별도 DB/규칙/대화/공유 컨텍스트, 재시작 후 기록 유지, 양쪽 제공자의 실행 cwd 확인.
- 실제 프로세스 테스트: 두 번째 프로젝트 모의 응답 실행 중 기본 프로젝트 전환 조회 → 서비스 종료 거절 → 웹 재시작 → 같은 daemon/인증 토큰/완전한 응답/제공자 호출 1회.
- 브라우저: 별도 7879 테스트 DB에서 샘플 폴더 추가 후 즉시 전환, 기본 프로젝트에서 초안 숨김, 재전환 시 원래 초안 복원, 새 프로젝트의 가져오기 목록에 기본 프로젝트 대화가 섞이지 않음 확인.
- 화면: 1440×1000 데스크톱 3열, 390×844 모바일 메뉴/다크 테마/사용량 모달 확인. 수평 넘침 없음. 콘솔 오류 없음. 초기 빠른 화면 캡처의 미완성 페인트는 DOM 및 후속 렌더로 다시 확인함.
- 최종 배포: 7878 재시작 후 프로젝트 관리 버튼, 밝은 화면, 실제 양쪽 계정 사용량과 초기화 시각 재확인. 사용자 프로젝트/대화 테스트 메시지는 추가하지 않음.
- 근거와 원본 코드/문서 링크, 재사용/변경/지원 한계는 PRODUCT_PLAN.md와 README.md에 기록.
