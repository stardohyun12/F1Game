# 게임 제작 작업실 첫 버전

진입: `/studio` (기존 `/` 화면 상단 **게임 제작 작업실**). 기존 대화·설정 화면은 보존한다.

## 구현

- 전체 제작, 아트, 조작감 작업실. 기존 팀/담당 대화에 연결하고 요청한 담당자만 실행한다. 처음 준비할 때 같은 이름의 팀장이 있으면 재사용한다. 새 담당자는 Codex 작업 모드로 준비되며, 모델·권한은 기존 설정에서 바꾼다.
- 작업실별 초안·스크롤·전체 제작의 요청 대상은 프로젝트를 구분해 브라우저에 저장한다. 팀 작업은 전체 제작 현황에도 나타난다.
- 프로젝트 공통 목표와 채택된 결정은 다음 실행의 컨텍스트에 포함한다. 제안·실험은 해당 팀에 남으며 채택 버튼 없이 공통 방향으로 승격되지 않는다. 원문 메시지 링크를 보존한다.
- 명시한 영향 팀에 연결 작업을 요청할 수 있다. 원본 작업·결과 발췌(최대 4,000자)·메시지 참조·등록 파일을 공유한다. 전체 대화 복사는 하지 않는다. 영향 자동 추론/자동 실행은 이번 버전에 없다.
- 공유 프로젝트 전체를 한 번에 한 실행만 사용하는 보수적 직렬화. 작업실 실행 중 기존 화면에서 다른 실행을 시작해도 거절한다. 기존 실행이 있으면 작업실 요청은 명시적으로 예약한다. 다른 앱의 Unity/에디터나 외부 Codex 실행까지 잠그지는 않는다.
- 지원하는 추가 요청은 **다음 작업으로 예약**, **현재 작업 중단 후 새 요청**이다. 실행 중 문맥 삽입은 지원하지 않는다. 오류·취소 뒤 예약은 자동 진행하지 않고 재개 버튼을 제공한다.
- 예약 10건, 원본당 연결 작업 4건, 실행당 600초 중단 요청. 제공자 프로세스가 중단에 응답하지 않으면 잠금을 유지하며 다음 작업을 시작하지 않는다. 서비스 재시작 후 진행 중 작업은 중단으로 표시하고 자동 재실행하지 않는다.
- 작업 완료는 실행 종료만 의미한다. 기술 검증과 게임 통합·플레이 검증을 자동 통과 처리하지 않는다. 플레이 임베드는 미지원으로 표시한다.

## 산출물 계약

`register_artifact({path})`를 Claude SDK MCP와 Codex stdio MCP에 공통 제공한다. 등록이 반환한 URL만 사용한다.

- 프로젝트 내부 일반 파일, 최대 64 MiB. 숨김 파일·폴더, UNC, ADS, 경계 탈출, symlink/junction 거절. Windows 열린 핸들의 최종 경로를 재확인한다.
- 등록 실행은 같은 대화의 저장된 사용자 메시지와 연결되어야 한다. ID·프로젝트·대화·실행·크기·형식·시각·SHA-256을 기록한다.
- `.runtime/.../deliverables/<opaque-id>`에 불변 사본을 저장한다. 원본 수정·삭제는 기존 다운로드를 바꾸지 않는다. 사본 누락·변조·명시적 만료는 오류로 안내한다. 자동 보존 기간 삭제는 없다.
- `/deliverables/<id>?project=<id>`만 파일 바이트를 제공한다. 요청 경로로 로컬 파일을 직접 다운로드하지 않는다. 프로젝트가 다르면 같은 ID도 거절한다.
- 답변 아래와 결과 목록에 동일 파일 카드. 입력 첨부·변경 코드와 구분한다. 한글 파일명은 RFC 5987 `filename*`, ZIP은 `application/zip`, `nosniff`, attachment 헤더를 사용한다.
- 64 MiB 상한 내에서 런타임→웹 전달은 base64 JSON을 사용하므로 대용량 스트리밍 대비 메모리 사용량이 크다. 더 큰 게임 빌드 전달은 후속 개선 대상이다.

## 참고와 적용

1. 기존 `docs/pitcrew-ui-reviews/2026-10-17-193541/README_REVIEW.md` 및 원본 코드: 일반 ZIP 등록/전달 누락, 운영 identity 404를 확인했다. 이번 변경 직전에도 운영 identity와 maintenance API는 404였다.
2. [GitHub Actions artifacts](https://docs.github.com/en/actions/tutorials/store-and-share-data): 불변 산출물·ID·해시 검증 원리를 로컬 파일 저장소에 적용했다. GitHub 서버 워크플로우 자체를 재현했다고 주장하지 않는다.
3. [OpenAI Agents handoff filter 원본 예제](https://github.com/openai/openai-agents-python/blob/main/examples/handoffs/message_filter.py): 선별된 맥락 전달을 참고했다. SDK를 추가하지 않고 기존 대화 참조 구조에 결과 발췌와 채택 결정을 넣었다. 외부 SDK 예제 자체는 실행하지 않았다.
4. [Codex App Server 공식 문서](https://learn.chatgpt.com/docs/app-server): thread/start·resume, turn/start, 모델·effort와 종료 이벤트를 기존 구현과 대조했다. 기존 app-server 경로를 유지했다.

외부 코드를 복사하거나 신규 패키지를 도입하지 않았다. 기존 MIT markdown-it을 재사용했다. 운영 인증/대화 전체를 검토 자료에 내보내지 않는다.

## 실행 품질 조사

확인한 사실: 모델/effort가 각각 thread 및 turn 요청에 전달되고 세션 ID 변경은 거절된다. 사용자 텍스트는 프롬프트 끝에 보존되지만 `.strip()`과 참조/첨부 설명 추가가 있다. 역할·공유 지침과 프로젝트 규칙이 추가된다. 기존 peer/mention 문맥은 12,000자로 줄이고 도구 결과 저장은 일부 경로에서 20,000자로 제한한다. 이는 직접 앱과 동일한 환경이라는 보장을 막는 차이다.

확인한 개선: 전달할 파일을 등록할 실제 도구가 없던 손실을 제거했다. 새 팀 컨텍스트는 전체 대화를 복사하지 않는다. 사용자 채택과 실행 종료를 분리했다. 실패는 작업/도구 상태와 오류 내용으로 표시한다.

미확인: 기존 체감 품질 저하의 단일 원인, 데스크톱 앱의 숨겨진 모든 지침과 도구 구성. 기존 지침을 대량 삭제하지 않았다. 단일 과제 결과로 모델 또는 제품의 일반적 성능 우열을 주장할 수 없다.

## 검증 재현

```powershell
cd tools/studio-dashboard
.venv/Scripts/python.exe -m unittest test_studio test_delivery_http test_hub test_artifact_browser test_full_history test_team_links test_queue test_team_lead test_codex_delegate test_runtime_identity test_maintenance_status test_runtime_shutdown
.venv/Scripts/python.exe run_studio_browser_check.py
# 실제 모델을 호출하는 선택적 비교; 임시 폴더만 수정, 각 변형 최대 150초
.venv/Scripts/python.exe -X utf8 live_studio_compare.py
```

첫 명령 146개 통과. 독립 runtime을 통한 실제 HTTP 파일 전달 포함. 브라우저는 별도 모의 제공자에서 팀 요청→결과→채택→다음 요청, 한글 ZIP 클릭·저장·해시, 실패·중단·연결 복구, 1440px/390px 레이아웃을 검증했다. 모의 모델 성공과 실제 모델 성공을 구분한다.

실제 `gpt-6-astra / low`, 동일 초기 speed.py 해시, 변형별 1회:

| 경로 | 관측 시간 | 외부 검증 | 재설명 |
|---|---:|---|---:|
| 직접 Codex CLI | 18.96초 | 4 assertions 통과, 테스트 불변 | 0 |
| PitCrew 단일 담당 | 15.66초 | 동일 통과 | 0 |
| PitCrew 구현→별도 검토 | 49.70초 | 동일 통과, verification.txt 실제 MCP 등록 | 0 |

직접 경로는 데스크톱 UI가 아닌 Codex CLI다. 두 팀은 별도 세션이지만 검토자의 OS 쓰기 권한도 full이었으므로 강제 읽기 전용 독립성은 아니다. 검토자에게 수정 금지를 요청했고 speed.py/테스트 결과를 사후 대조했다. 토큰은 제공자 보고 누적값이며 비용은 미제공. 상세 JSON은 `.ai_logs/pitcrew-studio/model-comparison.json`에 있다. 각 경로는 사용자 프로필/도구 컨텍스트가 완전히 동일하지 않다.

## 운영 반영

진행 작업/예약/승인/실행 목표가 없는 것을 두 프로젝트에서 확인하고 SQLite backup API로 로컬 백업했다. 기존 정상 `runtime.py --restart`와 `start.ps1 -Restart` 절차를 사용한다. 새 runtime identity로 추가 모듈까지 디스크 일치를 확인한다. commit/push·외부 업로드·Unity 게임 변경은 하지 않는다.
