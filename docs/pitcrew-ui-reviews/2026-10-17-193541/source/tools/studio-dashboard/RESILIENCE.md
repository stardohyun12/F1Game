# 외부 CLI 앱 조사 및 연결 복구 개선

조사일: 2026-10-14 (한국 시간). 원본 구현과 테스트를 읽어 비교했다. 아래 앱 전체를 설치·실행하거나 원본 테스트 스위트를 재현한 것은 아니다. PitCrew에 적용한 구현은 별도 장애 주입으로 검증한다.

## 확인한 현재 문제

- 조사 시 기존 서버 PID 43048은 01:37부터 살아 있었고 bootstrap은 약 209ms에 응답했다. 사용자가 보았던 끊김의 단일 원인은 로그만으로 확정할 수 없다.
- 기존 fetch에는 기한이 없었다. bootstrap 또는 detail이 영원히 대기하면 `state.polling`이 해제되지 않아 후속 갱신도 막힌다. 단순 오류는 다시 조회했지만, 멈춘 요청을 복구하는 경로는 없었다.
- 기존 재시작 스크립트는 실행 상태 조회 실패를 무시하고 서버를 종료했다. 긴 작업이 살아 있는 상황도 구분하지 못했다.
- AI 실행은 서버의 Run에서 진행되며 화면 폴링과 독립적이다. 승인/질문 대기는 사용자 응답 또는 명시적 중단까지 유지한다. idle 시간만으로 세션을 종료하는 앱 코드는 없다.

## GitHub에서 채택할 부분

| 프로젝트 | 확인한 원본 | PitCrew에 적용하거나 채택할 점 |
|---|---|---|
| [Orca / stablyai](https://github.com/stablyai/orca) · MIT | [daemon 복구](https://github.com/stablyai/orca/blob/main/src/main/daemon/daemon-pty-daemon-recovery.ts), [실제 복구 테스트](https://github.com/stablyai/orca/blob/main/src/main/daemon/daemon-pty-adapter-daemon-recovery.test.ts) | UI 연결과 살아 있는 CLI 세션을 구분. 복구 요청은 하나로 합치고, 상태 불명인 실행을 강제 재시작하지 않음. 이번에 재시작 상태 확인 실패 시 종료 금지와 중복 갱신 합치기를 적용. 장기적으로 독립 daemon·세션 재부착 구조를 채택할 가치가 큼. |
| [Happy](https://github.com/slopus/happy) · MIT | [offline reconnection](https://github.com/slopus/happy/blob/main/packages/happy-cli/src/utils/setupOfflineReconnection.ts), [그 테스트](https://github.com/slopus/happy/blob/main/packages/happy-cli/src/utils/setupOfflineReconnection.test.ts), [session protocol](https://github.com/slopus/happy/blob/main/docs/session-protocol.md) | 연결 복구를 별도 계층으로 두고 세션 정체성을 유지. 다양한 제공자의 도구·하위 에이전트 이벤트를 공통 형식으로 정규화. 이번에는 복구 모듈을 분리하고 대화/초안을 보존. 공통 이벤트·부모 자식 에이전트 트리는 후속 구조 개선 후보. |
| [Vibe Kanban](https://github.com/BloopAI/vibe-kanban) · Apache-2.0 | [stream reconnect](https://github.com/BloopAI/vibe-kanban/blob/main/packages/web-core/src/shared/hooks/useJsonPatchWsStream.ts), [실행 로그](https://github.com/BloopAI/vibe-kanban/blob/main/crates/db/src/models/execution_process_logs.rs) | 제한된 간격 증가로 재연결, 연결 상태와 기존 데이터 분리. 이번에는 HTTP 폴링에 적용. 기존 SQLite 대화 저장 재사용. 작업별 worktree와 변경 검토 큐는 여러 에이전트가 파일을 수정할 때 유용. |
| [Claudia](https://github.com/getAsterisk/claudia) · AGPL-3.0 | [process registry](https://github.com/getAsterisk/claudia/blob/main/src-tauri/src/process/registry.rs) | PID·세션 ID·시작 시간·출력을 함께 관리하는 실행 목록. 프로세스 정보 화면의 설계 참고 후보이며 코드를 복사하지 않음. |
| [TanStack Query](https://github.com/TanStack/query) · MIT | [retryer](https://github.com/TanStack/query/blob/main/packages/query-core/src/retryer.ts), [focus manager](https://github.com/TanStack/query/blob/main/packages/query-core/src/focusManager.ts), [online manager](https://github.com/TanStack/query/blob/main/packages/query-core/src/onlineManager.ts) | 재시도 간격 증가, 화면 복귀/연결 복원 시 재조회. React 의존성을 추가하지 않고 작은 독립 모듈로 원리를 적용. |

Orca에는 [재연결 장애 조사 기록](https://github.com/stablyai/orca/blob/main/cloud/docs/relay-reconnect-2026-09-findings.md)도 있다. 서버가 실제로 죽은 경우와 상태 조회 자체가 지연된 경우를 구분해야 한다는 점을 참고했다. 이 기록은 해당 프로젝트의 보고이며 이 앱에서 같은 장애 원인이 발생했다는 증거는 아니다.

조회 시 main HEAD: Orca `b5d739ee193254fa07e204a570254c25aae31b01`, Happy `33606b99eba92408385b109df9d543e34f2c596f`, Vibe Kanban `d5cbb5380fa0b32e98ef9b8d987f63decce4be3a`, Claudia `d1ca30a3c0c39fff01fde2f86c2d5af6a1db658b`, TanStack `0fbbc6213641ef2d7cec7d4bfd2f1044c561dde8`. 링크는 가독성을 위해 main 기준이다.

## 실제 변경

- 2026-10-16 후속 변경: 실행 서비스의 POST 저장 요청이 처리 중이면 종료를 거절하고, 종료 수락 뒤 새 변경 요청은 처리하지 않는다. 실제 격리 업로드의 종료 전후 재현 및 기본/별도 프로젝트 바이트 보존 검증은 `../../.ai_logs/pitcrew-audit/runtime-upload-shutdown-result.md`에 기록한다. 코드만 반영했으며 운영 서비스 활성화와 독립 검토는 별도다.

- `web/connection.js`: 조회/응답 본문 전체에 12초 기한. 멈춘 fetch를 abort하고 다음 동기화를 허용한다. 모델 목록은 120초, 변경 요청은 60초를 허용한다. 이는 AI 실행 시간 제한이 아니다.
- 폴링은 동시에 하나만 실행하며 중복 요청은 같은 Promise를 기다린다. 장애 시 1/2/4/8/16/30초 간격으로 계속 재시도한다. 성공하면 정상 간격으로 돌아온다.
- 숨겨진 화면은 5초 간격으로 조회. focus/online/pageshow/visibilitychange에서 즉시 조회한다. localhost이므로 인터넷 offline 판정만으로 폴링을 중지하지 않는다.
- 마지막 대화와 작성 중 초안 유지. 작은 화면에서도 보이는 상태 안내, 마지막 동기화 시각, 수동 재연결 버튼을 추가했다.
- POST 명령은 자동 재전송하지 않는다. 응답 유실 시 실행 여부가 모호하므로 상태를 먼저 확인하도록 안내한다.
- `start.ps1 -Restart`: 작업 실행 중이거나 상태 조회가 실패하면 서버를 보존한다.

## 검증과 한계

- `node --test tools/studio-dashboard/test_connection.cjs`: 멈춘 요청 이후 복구, 가상 시간 1시간 이상 장애 후 복구, 요청 중복 방지, HTTP 503, 본문 중간 정지, POST 응답 유실 시 전송 1회, 재시도 상한과 stop.
- 기존 Python 백엔드 테스트 13개 통과.
- `test_fault_proxy.py`는 7879에서 GET만 전달하는 시험 도구. `.runtime/fault-mode.txt`의 online/offline/hang으로 장애를 주입하며 생산 UI/DB를 수정하지 않는다.
- 이번 변경은 브라우저와 서버 사이 연결 복구다. PC 종료/서버 프로세스 종료 후 자동 시작, 죽은 CLI의 진행 중 작업 자동 재실행은 구현하지 않았다. 서버가 살아 있으면 입력이 없다는 이유로 작업을 취소하지 않는다.
- upstream 전체 실행을 재현하지 않았으며 수 시간 실제 AI 호출 시험도 하지 않았다. 독립 구현·원리 비교만 했고 외부 코드를 복사하거나 신규 런타임 의존성/사용 비용을 추가하지 않았다.

## 다음 구조 개선 우선순위 (이번 적용 범위 밖)

1. Orca 방식: 서버와 별도의 실행 daemon, 세션 ID로 재부착, PID/시작 시각/마지막 이벤트/대기 이유 표시.
2. Happy 방식: 제공자 어댑터 공통 이벤트와 부모·자식 에이전트 ID. Claude 팀과 Codex 하위 작업을 같은 트리로 표시.
3. Vibe Kanban 방식: 작업별 파일 격리와 변경 검토. 공유 컨텍스트는 출처와 전송 시점 스냅샷을 유지.
4. 서버 수신 확인 ID와 영속 outbox: 전송 응답이 사라져도 중복 실행 없이 결과를 조회. 이를 먼저 만든 뒤 명령 자동 재전송을 검토.

## 독립 실행 서비스와 팀 보기 — 2026-10-14 후속 적용

이 절은 위의 ‘후속 후보’와 서버 수명 관련 한계를 갱신한다.

- `runtime.py`가 Hub/SQLite/CLI/승인 요청을 소유한다. `server.py`는 브라우저 UI와 API 전달만 담당한다. 웹 서버가 종료되어도 실행 서비스가 유지된다.
- 실행 서비스는 임의의 loopback 포트에 바인딩하고 비밀 토큰으로 인증한다. 연결 정보는 git에서 제외된 `.runtime/runtime.json`에만 저장하며 브라우저에 노출하지 않는다. 웹 서버의 Host/Origin/요청 토큰 검증도 유지한다.
- OS 파일 잠금으로 동일 데이터 디렉터리를 두 실행 서비스가 동시에 열지 못한다. 웹 재시작은 실행 서비스의 세션 토큰을 바꾸지 않는다. 명령은 재전송하지 않는다.
- `runtime.py --stop` 또는 `--restart`는 어느 프로젝트든 실행/승인 대기 작업 또는 실행 직후 후속 처리(보고 전달·대기열·다음 단계 시작)가 있으면 거절한다. 실제 실행 프로세스가 죽었던 경우에만 다음 서비스 시작 시 중단 상태로 정리하며 부분 응답·대화는 보존한다. 후속 처리 종료 보호는 2026-10-16 코드에 추가했으며 운영 서비스 재시작 전에는 적용되지 않는다. 검증과 한계는 `../../.ai_logs/pitcrew-audit/runtime-shutdown-result.md`에 기록한다.
- 팀 상태 창에 전체 역할·대기 요청·실행 시작/최근 출력 시각을 표시한다. 확인된 부모-자식 기록으로 Claude 하위 에이전트를 묶고 이미 연결한 팀원을 중복 표시하지 않는다. 대화 열기/현재 대화와 비교로 이동할 수 있다.
- 이 단계에서 모든 CLI의 하위 작업 이벤트를 공통 프로토콜로 바꾸지는 않았다. 트리는 실제 발견한 Claude subagent 기록만 연결한다. Codex 하위 작업에 대해 확인되지 않은 관계나 실행 상태를 만들어 표시하지 않는다.

검증: `test_runtime.py`가 실제 별도 웹/실행 프로세스와 로컬 SSE 모의 제공자를 띄운다. 응답 도중 웹 프로세스를 종료/재시작한 뒤 동일 실행 PID/토큰, 완료된 응답 BEFORE AFTER, 제공자 호출 1회, 인증 없는 서비스 접근 차단, 작업 중 서비스 종료 거절, 중복 실행 서비스 시작 방지를 확인했다. 외부 AI 호출이나 결제는 하지 않았다. Python 테스트 14개, Node 테스트 4개도 통과했다.

남은 한계: PC 전원/절전 중 실행을 보장하지 않는다. 실행 서비스 자체가 죽으면 진행 중인 CLI 턴을 자동 재실행하지 않는다. 재부팅 자동 시작과 제공자 전체의 공통 이벤트 트리·영속 전송 큐는 별도 작업이다.
