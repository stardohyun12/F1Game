# PitCrew UI/UX · 파일 전달 검토 자료

## 핵심 요약

- **[이번에 재현] 파일은 있지만 다운로드는 연결되지 않는다.** 이전 인계 ZIP은 155,488 bytes, 25 entries이며 CRC 검사에 통과했다. 운영 `http://127.0.0.1:7878/D:/.../WORKFLOW_HANDOFF_BUNDLE.zip`은 **404 / text/html / `not found` 9 bytes**, `Content-Disposition` 없음. 원본 ZIP을 받은 것이 아니다. `evidence/original-bundle.json`, `http-live.json`.
- **[코드 확인 + 이번에 재현] 일반 파일 산출물 등록·전달 기능이 빠져 있다.** `Hub.artifacts()`는 이미지·편집 diff·사용자 첨부를 모은다. 답변의 일반 ZIP 링크를 다운로드 가능한 산출물로 등록하지 않는다. 임시 프로젝트에서 실제 ZIP을 만들고 답변에 링크를 넣어도 결과물 counts는 전부 0이었다. `evidence/artifact-registration-probe.json`.
- **[이번에 재현] 브라우저는 잘못된 경로 링크를 표시하고 클릭도 하지만 파일은 저장하지 못한다.** 배포 중인 Markdown 렌더러로 한글·공백 파일 링크를 재현하고 운영 7878로 클릭하자 `not found`, download 이벤트 0. 별도 진단용 정상 attachment 응답에서는 **62 bytes, SHA-256 일치, 한글 파일명 유지**를 확인했다. 정상 대조군은 제품 수정이 아니다.
- **[이번에 재현] 프런트엔드와 백엔드의 배포 확인 범위가 다르다.** 14개 HTML/JS/CSS HTTP 본문은 디스크와 CRLF→LF 정규화 후 일치한다. 디스크의 `/api/hub/runtime-identity`는 운영에서 404다. 최신 백엔드가 전부 반영됐다는 주장은 할 수 없다. 현재 디스크 코드에도 ZIP 다운로드 라우트는 없으므로 단순 재시작만으로 이 문제가 해결된다고 볼 수 없다.
- **제품 코드 수정, 서버 재시작, AI/Unity/Blender 호출, commit/push, 외부 업로드/터널: 이번 조사에서 수행하지 않음.** 운영 중 작업을 방해하지 않기 위해 백엔드 배포를 진행하지 않았다. 이 ZIP의 최종 전달은 로컬 파일이며 외부 ChatGPT가 localhost로 받을 수 있다는 의미가 아니다.

## 기준과 범위

- 조사 시작: 2026-10-17 19:35:41 KST. 개별 관측 시각은 evidence의 UTC 값을 참고한다. 종료·무결성 검사는 `evidence/final-checks.json`.
- 저장소: `D:\Github\F1Game`, branch `main`, HEAD `906f8994e9533f9953f775d292831f8f8586bf8c`.
- `source/`는 HEAD export가 아니라 **조사 시작 때의 실제 디스크 파일 123개**다. 미커밋·미추적 코드를 포함했다. 파일별 상태·원본 SHA는 `evidence/source-provenance.json` 및 `manifest.json`.
- `evidence/repository.json`에 대시보드 변경 목록을 담았다. `.runtime`/상태 파일 등 비공개 실행 자료는 경로별 원문 대신 제외 개수만 남겼다. 별도 원본 DB·전체 대화·인증 설정은 포함하지 않았다.
- 표기: `[코드 확인]`은 구현/정적 구조, `[로그 확인]`은 저장 기록, `[이번에 재현]`은 이번 실행 결과, `[추정]`은 해석, `[미확인]`은 증거 부족. 화면 예시와 운영 관측을 구분한다.

## 실행본과 의존성

`evidence/processes.json`, `listener.json`, `http-live.json`, `environment.json` 참조.

| 항목 | 확인된 상태 |
|---|---|
| 운영 웹 | [이번에 재현] 127.0.0.1:7878 listener PID 39668, `tools/studio-dashboard/.venv/Scripts/pythonw.exe -X utf8 -u .../web_host.py --port 7878`. 실제 executable은 Python 설치 경로로 나타남. 2026-10-15T15:49:21.885624Z 시작 |
| runtime | [이번에 재현] runtime-health PID 19020, `.../.venv/Scripts/python.exe -X utf8 .../runtime.py --serve`, 프로세스 시작 2026-10-16T10:54:43.589...Z. health startedAt=1792148083.830326 |
| 감시/다른 runtime | [이번에 재현] 프로세스 목록에 함께 존재. 어느 PID도 종료하지 않음. 같은 파일 경로를 쓴다는 이유만으로 운영 runtime으로 단정하지 않음 |
| 로드된 버전 | [미확인] PID별 완전한 Python 코드 SHA를 운영 API에서 얻을 수 없음. 프로세스 시작 시각·명령·health만 확인. identity API 404 |
| 프런트엔드 | [이번에 재현] root HTML + 13 JS/CSS 파일이 디스크와 줄바꿈 정규화 후 동일. 원시 SHA 차이를 버전 차이로 오판하지 않음 |
| backend 미반영 | [코드 확인/이번에 재현] 디스크 server.py에 있는 identity 라우트가 운영에서는 없음. 최소 이 기능 차이는 확인. 나머지 기능 전체 차이는 미확인 |

실제 제품 진입점은 `source/tools/studio-dashboard/PitCrew.cmd` → `start.ps1` → watchdog/web_host/runtime이다. Python 가상환경·`requirements-lock.txt`·Claude/Codex 설치/로그인·프로젝트 경로가 필요하다. Python 3.13.14 응답, SDK 고정 `claude-agent-sdk==0.2.165`. 자세한 현재 설치 버전은 environment와 기존 workflow_context 참조.

**이 패키지를 처음 열 때 제품 start.ps1, runtime.py --restart, live_* 테스트를 실행하지 말 것.** 원본 동작 보존용으로 포함된 코드이고, 로컬 서비스/모델을 시작할 수 있다. 외부 검토는 아래의 격리 예시부터 시작한다.

### 모델 없이 화면을 보는 격리 예시

ZIP을 풀고 그 폴더에서:

```text
python diagnostics/offline_preview.py --port 7899
```

검토자의 PC에서 `http://127.0.0.1:7899/`를 연다. **사용자의 7878을 접속하는 것이 아니라 검토자 자신의 PC에서 실행하는 예시다.** Python 표준 라이브러리만 사용한다. 현재 프런트엔드 소스에 가상 프로젝트·작업자·요청/응답 데이터를 제공한다. POST/PUT/DELETE는 405로 막는다. 운영 DB, 사용자 프로필, 모델, Unity를 읽거나 호출하지 않는다. 노란 배너로 fixture임을 표시한다. 일부 부가 API는 503을 반환하므로 제품의 해당 기능 고장 증거로 쓰면 안 된다. 종료는 이 예시를 실행한 터미널의 Ctrl+C.

이번 시험: 가상 작업자 4명 표시, 대화 선택·결과물 열기·실패 상태 선택, 브라우저 page error 0 (`fixture-preview-result.json`). `diagnostics/fixture_capture.cjs`는 Playwright 설치 경로를 `PLAYWRIGHT_PATH`로 받는다. 로컬 절대 경로는 내보내기 시 비식별 처리했으므로 외부 환경에서는 직접 지정해야 한다.

## 최근 실패 사례의 증거

`evidence/download-conversation-excerpts.json`은 SQLite를 **mode=ro**로 조회해 ZIP 전달 관련 메시지 7건만 추출한 것이다. user의 반복된 컨텍스트/다른 대화 인용은 제거했다. 원본 DB와 전체 세션은 복사하지 않았다.

- seq 10344/10346: 에이전트가 `/D:/Github/.../WORKFLOW_HANDOFF_BUNDLE.zip` 링크를 제공.
- seq 10347: 사용자가 “인계 ZIP눌러도 not found…”를 보고.
- seq 10356/10359: 다시 링크 요청 → `http://127.0.0.1:62116/...zip` 제공. 이 링크는 당시 별도 임시 파일 서버였으며 외부 ChatGPT가 접근할 공유 URL이 아니다.
- seq 10360/10361: 사용자가 파일 경로만 요청하고 로컬 경로를 받음.

에이전트가 링크를 제공했다고 말한 사실과 사용자 측 다운로드 성공은 별개다. 이전 메시지의 “다운로드 확인”은 당시 HTTP 확인이었으며 **원래 제품 화면의 브라우저 저장 성공을 증명하지 않는다.** 이번에는 실패 클릭과 정상 대조군 저장을 각각 기록했다.

| 단계 | 현재 증거와 판정 |
|---|---|
| 요청 | [로그 확인] 위 seq와 관련 텍스트 |
| 파일 생성 | [이번에 재현] 이전 ZIP 존재·155488 bytes·25 entries·CRC 정상·SHA 확보. Windows 작업 PC의 D: 저장소에 있음. 생성한 OS PID/원래 생성 순간은 [미확인] |
| 경로/산출물 등록 | [코드 확인/이번에 재현] 답변의 일반 ZIP 링크만으로 artifacts에는 등록되지 않음. 별도 DB의 일반 다운로드 artifact ID/URL 발급 흐름을 관련 코드에서 찾지 못함 |
| 서버 주소 | [코드 확인] server.py:284 `_local_image`는 이미지 확장자만; :463–483은 이미지 및 고정 웹 자산; :523은 미일치 경로 `not found`. `/api/hub/files`는 경로 검색 목록, 바이트 다운로드가 아님 (`hub.py:2120`) |
| Markdown 표시 | [이번에 재현] 일반 링크를 제거하지 않고 `<a>`로 렌더링, `target=_blank`만 붙임 (`web/reader.js:13`). 이미지일 때만 `/local-image`로 변환 (:18). file://는 링크가 되지 않음. sandbox:는 문자열 링크로 남지만 이 앱의 실제 파일 제공 방식이 아님 |
| 실제 클릭 | [이번에 재현] 한글·공백 진단 파일의 루트 경로 링크를 운영으로 클릭 → 오류 페이지. 원래 대화의 이전 ZIP 링크는 현재 로딩된 메시지 범위에서 찾지 못하여 직접 클릭은 [미확인]. 원래 URL은 HTTP로 404 확인했고, 브라우저 클릭은 같은 형태의 진단 파일로 재현 |
| 파일 저장 | [이번에 재현] 실패 링크에서 download 이벤트 0. 격리 대조군만 62 bytes 저장 및 SHA 일치. **제품의 ZIP 다운로드 성공은 아님** |

### URL·파일명·헤더 판정

`browser-results.json`의 diagnostics는 배포 중인 reader.js/markdown-it 결과다. `windows-root`는 진단 페이지의 origin이 달라서 동일 경로의 호스트만 운영 7878로 명시했다(브라우저 클릭을 실제 실패 서버로 보내기 위한 조치). 이 조치를 원본 Markdown 렌더러의 자동 주소 생성으로 주장하지 않는다.

- `/D:/.../진단 파일 공백.txt`: 브라우저가 한글·공백을 percent-encode해 요청했으나 라우트가 없어 실패. **이번 실패 원인은 인코딩 손실로 보이지 않는다.**
- `D:/...`: 드라이브 문자 형태가 href로 남는다. 실제 클릭/저장은 미시험.
- `file:///...`: 원문 텍스트로 남고 클릭 가능한 링크가 없음.
- `sandbox:/mnt/data/...`: href는 남지만 해당 스킴 지원/다운로드 성공은 미확인. 이 UI가 다른 서비스의 sandbox 파일을 제공한다는 증거 없음.
- 진단 대조군: `Content-Type: text/plain; charset=utf-8`, `Content-Disposition: attachment; filename="diagnostic.txt"; filename*=UTF-8''...` 사용, 한글 suggestedFilename 유지. ZIP에도 제품 측 허용된 산출물 등록/다운로드 라우트가 필요하다는 판단은 [추정/후속 설계]이며 구현하지 않았다.

## UI/UX 관측과 재현

운영 화면: `01-live-workspace.png`, `03-live-results.png`, `04-live-mobile.png`. 회색 영역은 개인정보 보호 마스크다. 상태·레이아웃은 실제 운영 DOM이다. 계정 사용량·대화·프로젝트/작업자 이름·목표·파일 경로를 가렸다. Unity API 요청은 브라우저 경로에서 차단했으므로 결과물 패널의 Unity/갱신 경고를 제품 장애라고 판정하지 않는다. 운영 화면에서 요청은 보내지 않았다. 짧은 영상은 ffmpeg 미설치로 실패했고 캡처만 제공한다.

예시 화면: `07-fixture-completed-request.png`는 초기 **작업 현황** 화면(파일명에 completed가 있으나 완료 검증 의미 아님), `08-fixture-request-and-link.png`, `09-fixture-results-empty.png`, `10-fixture-failure-status.png`. 모두 가상 데이터이며 운영 재현과 혼동하지 말 것.

| 행동 / 문제 | 재현 순서 | 기대 | 실제와 근거 | 판정 |
|---|---|---|---|---|
| 작업자 찾기 | 왼쪽 목록 → 이름/역할 검색 → 선택 | 대상/역할/상태를 구분 | 운영 12 카드·2 프로젝트가 로드됨. 검색 입력과 상태 표시가 존재. 가상 예시 4명의 작업 중/입력 대기/실패/완료는 구분됨 | [이번에 재현] 기능 존재. 실제 사용자의 탐색 시간·오선택률은 미측정. 역할 표기 개선 필요성은 가설 |
| 요청 보내기 | 작업자 선택 → 입력창 | 선택 대상과 보내기/대기열 차이가 명확 | 운영 작업 중 화면은 '대기열에 추가', 전체 권한·모델·목표 상태가 함께 있음. 운영 전송은 미시험 | [코드 확인/화면 관측] 버튼 존재. 혼잡도 개선은 [추정] |
| 상태 구분 | 목록/헤더/작업 현황 | 실행 실패와 사용자 입력 대기를 구분 | 코드 `app.js:7` 상태 매핑, fixture는 4 상태 표시. 입력 대기에 실제 질문이 있는지는 fixture 범위 밖 | 상태표시 자체는 확인. 실제 실패→회복 E2E는 [미확인] |
| 결과물 찾기 | 완료 답변에 ZIP 링크 → 결과물 | 생성 ZIP이 카드/목록에 나타남 | 실제 Hub 임시 시험에서 ZIP 존재에도 이미지/파일/첨부 counts=0. `hub.py:1735`, `composer.js:398–401`, artifact-registration-probe | [이번에 재현] 중대 누락. 생성 파일과 변경 파일·입력 파일의 의미가 서로 다름 |
| 파일 다운로드 | 답변 링크 클릭 | 파일 저장/실패 사유 안내 | 404 `not found` 새 페이지, ZIP 내용 없음. `05`, `06`, http-live/browser-results | [이번에 재현] 다운로드 경로 부재 + 복구 안내 부재 |
| 배포 상태 이해 | 결과물 → 파일 비교 | 기능 사용 가능 여부 명확 | 운영 결과물 패널에 '파일 전후 비교는 서버 업데이트 적용 후 사용할 수 있습니다.' 표시. identity API 404 | [화면/HTTP 확인] 화면과 backend capability 차이. 전체 backend 버전은 미확인 |

위 표에서 관측된 결함과 개선 가설을 구분했다. 모든 UI 문제를 고쳤다는 주장, 모바일 실기기 확인, 접근성 전체 인증, 실제 AI 작업 완료 보장은 하지 않는다.

## 포함 자료와 확인 수준

- `source/tools/studio-dashboard/`: 최신 frontend, server/hub/runtime, 모듈·관련 테스트·실행 스크립트·requirements·vendor license·예시 provider 설정. 개인 홈 경로/이메일 등은 필요시 치환; manifest에 원본/내보낸 SHA와 치환 여부 기록.
- `evidence/live_web/`: 운영에서 GET으로 받은 HTML/JS/CSS. 비교 원본 SHA는 http-live.json에 있음.
- `evidence/`: 최소 대화 발췌, 마스킹 스크린샷, HTTP/브라우저/Hub 진단, 실패한 진단 실행 기록, 순수 reader 테스트 6/6.
- `workflow_context/`: 이전 WORKFLOW_HANDOFF.md와 참조된 23개 비식별 evidence 및 evidence_manifest.json. **이전 조사의 기록**이며 이번 UI 관측으로 갱신한 자료가 아니다.
- `diagnostics/`: 이번 재현/수집 스크립트. offline_preview.py만 별도 계정·도구 없이 화면 검토에 쓰도록 설계. 수집 스크립트는 원래 저장소/운영 서비스가 필요하므로 외부에서는 그대로 실행되지 않을 수 있음.
- 인증 파일, `.env`, 원본 SQLite/세션/대화 전체, 개인 설정, `.runtime` 전체, 에셋/Library/Temp/node_modules/.venv/.git: 제외.

추가 실패 기록: 첫 운영 화면 캡처는 과도한 중첩 마스크 처리에서 timeout; 마스크를 상위 컨테이너 단위로 바꿔 캡처. 다음 영상은 ffmpeg 부재로 실패, 무영상 진단으로 진행. fixture 최초 캡처는 '작업 현황'에 머문 상태에서 숨겨진 결과물 버튼을 눌러 timeout; 실제 작업자 카드를 선택한 뒤 3화면 캡처 성공. 이들은 진단 도구의 실패이고 제품 결함 수에 포함하지 않았다.

## 수정/전달 및 외부 검토 요청

제품을 고치지 않았다. 일반 파일 다운로드를 추가하려면 허용된 산출물 등록과 서버 라우트, 사용자에게 보이는 오류/만료 처리를 함께 검토해야 한다. 현재 작업 중인 운영 프로세스의 재시작 없이 기존 backend 라우트를 교체할 수 있다고 확인하지 못했으므로 이번에 배포하지 않았다. 임의 로컬 경로 전체를 노출하는 API·외부 터널·공개 업로드도 만들지 않았다.

ZIP은 로컬 폴더와 Windows 다운로드 폴더에 둔다. 파일 탐색기에서 파일을 선택해 **외부 ChatGPT 대화에 직접 첨부**하면 된다. `127.0.0.1`/localhost는 공유 URL이 아니며, 이 자료에 포함된 주소는 재현용이다. 최종 패키지 SHA/CRC/크기는 ZIP 옆 delivery-receipt.json을 참고한다.

외부 검토자는 (1) README → (2) manifest와 HTTP/브라우저 결과 → (3) source/server.py, hub.py, web/reader.js, composer.js → (4) 운영·fixture 스크린샷 순서로 확인하면 된다. 후속 설계는 일반 산출물 ID/다운로드 경계, 기존 결과물 분류와의 통합, 사용자 오류 안내, 배포 중 작업 보존을 검토하되 이 자료의 관측 결과와 구분해 제안해 달라.
