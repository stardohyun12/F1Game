# 타협 금지 — 목표 수행 룰 [project]

> 2026-05-19 도현 교정. 내 역할 = 목표 수행. 타협은 도현의 역할.
> 내가 자체적으로 "이 정도면 됐다" 또는 "안 되면 다음에" 판단 금지.

## 1. 목표 정의 — fix는 검증까지 끝나야 "완료"

코드 변경 commit ≠ "완료". "완료"의 정의:
- 컴파일 0건
- **재현 가능한 시나리오로 root cause 검증** (단순 "한 번 동작했음" X)
- **시각 확인** — screenshot 또는 자동 시뮬레이션으로 박힘/기울임 없음 직접 확인
- 도현 manual play 요청 전에 내가 먼저 자동 시나리오로 박힘 재현·해소 입증

이 셋 다 통과 X면 commit 금지. commit body의 `Verified-By:`는 단순 형식 통과가 X — 실제 검증 내용을 명시.

## 2. "안 됨" 보고 금지

다음 케이스는 보고로 끝내지 X. 우회·분해 후 계속 진행:

- subagent hang/timeout → Editor 재시작 요청 X, **subagent에게 작은 단위로 쪼개 다시 dispatch** (큰 진단 1회 → 작은 진단 5회)
- unity-cli timeout → 명령 단순화·세분화, 또는 다른 도구 시도
- "어느 옵션?" 형식 질문 → 진단 결과로 1순위 결정하고 시작하기. 도현 의견은 결과 보고 후

## 3. 도구 점검 룰

새 문제 진입 시 **반드시 먼저** 사용 가능 도구 점검:

- `unity-cli list` — 전체 명령 확인
- `unity-cli <cmd> --help` — 옵션 확인
- 본 문제에 관련 도구 (screenshot · menu · exec · console · 기타) 다 사용 여부 점검

특히 `unity-cli screenshot` — 시각 문제 (박힘·UI·카메라) 진단 시 reflection만으로 X, 반드시 screenshot 첨부.

세션 27 사례: spawn Y 4번 수정하면서 screenshot 한 번도 안 찍었음. 도현이 보는 것을 내가 한 번도 안 봤음.

## 4. Root Cause 찾을 때까지 표층 fix 금지

증상 ≠ 원인. 같은 증상 반복 = 원인 못 짚은 것.

세션 27 사례:
- 차 박힘 → spawn Y 변경 4회 (0.3, 1.5, 2.5, 0.5). 모두 실패. **root cause = 운전 중 wheel collider/도로 mesh 어긋남일 가능성, spawn 문제 X**. 진단 안 하고 표층 값만 만짐.

룰: **같은 증상에 같은 종류 fix 2회 실패 → 그 라인 중단, root cause 진단 dispatch**.

## 5. 진단 분해 — 큰 dispatch 1회 → 작은 5회

진단 subagent에게 "차 박힘 원인 찾아"라고 큰 prompt 주면:
- 12분+ hang 위험
- 결과가 부정확 (subagent도 ad-hoc 측정 반복)

해결: 진단을 **명시적 단계**로 분해:

1. "차 GameObject hierarchy + Rigidbody 모든 필드 dump"
2. "차 모든 Collider 종류·bounds·radius dump"
3. "WheelCollider × 4 suspension/radius/spring dump"
4. "도로 prefab 생성 코드 (BuildingPlacer/RoadBuilder) Read 후 mesh Y 결정 로직 요약"
5. "Play 시작 후 0초/5초/10초 시점 carY + isGrounded + rb.velocity screenshot 첨부"

각 단계 30초~2분. 합쳐 15분. 한 번에 큰 거 dispatch 시 12분 hang보다 빠름.

## 6. "도현에게 물어보기" 룰

도현에게 물어볼 수 있는 것:
- 기획 결정 (디자인 의도, 우선순위)
- 새로운 정보 요청 (manual play 결과만 본 것)

물어보면 안 되는 것 (내가 결정):
- 진단 결과 명확한데 fix 옵션 선택
- 도구 사용 여부 (다 써야 함)
- 검증 수준 (위 §1 셋 다 통과 의무)
- 다음 단계 (목표가 정해져 있으면 그 방향으로 계속)

## 8. 자체 진행 sprint — 내 영역 vs 도현 영역 구분

도현 부재 중 큰 작업 sprint 진행 시 (예: 7-day 폴리시):

**내 영역 (자체 결정·실행 가능)**:
- 코드 fix·수치 튜닝·UI 자가 구성 (UGUI 코드로 셋업)
- WebSearch로 free 자산 후보 5개 조사 + URL 정리 (선택은 도현)
- PostFX·파티클·셰이더 등 코드/asset 자체 셋업
- 임시 placeholder (시스템 폰트, 단색 배경, primitive mesh) 박고 후속 교체용 TODO 메모

**도현 영역 (대기 필요)**:
- 외부 자산 다운로드·계정 로그인 (Sketchfab DL, Google Fonts ZIP)
- 자산 선택 (5 후보 중 어느 것 적용)
- 시각 톤 결정 (warm vs cool, 다크 vs 라이트)
- 기획·메커니즘 추가·삭제·우선순위 변경
- 큰 시스템 추가 (새 모드·새 UI 패널)

**룰**: 도현 영역인데 부재 시 → placeholder + TODO 메모 + commit + 후속 대기. 자체 결정 X.

## 9. 큰 sprint 중 매 단계 끝 = B 트랙 한 블록

A 트랙 (게임) sprint를 자체 진행 중일 때, 매 Day(또는 큰 step) 끝마다 B 트랙(하네스)에 한 블록:
- 잘 작동한 룰 → `옵시디언/하네스/harness-usage-log.md`에 한 블록
- 헛짚음·incident → `옵시디언/하네스/incidents.md`에 4줄 블록
- 새 패턴 → `옵시디언/하네스/patterns/` 새 파일

룰 평가는 도현 형 검토 사후. 내가 자체 평가는 X.

## 7. 매 결정 체크리스트 (commit 또는 사용자 대응 직전)

- [ ] §1 검증 셋 다 통과했나? (compile / 재현 / 시각)
- [ ] §3 도구 다 썼나? screenshot 찍었나?
- [ ] §4 같은 종류 fix 2회 이상인가? → 진단 dispatch
- [ ] §5 dispatch가 hang 위험? → 작은 단위로 분해
- [ ] §6 도현에게 물어보는 게 진짜 필요? → 아니면 결정하고 진행
- [ ] §10 화면(UI/카메라/마커) 변경 commit인가? → `Feels-off:` 한 줄 채웠나?

하나라도 NO면 멈추고 그 항목 처리.

## 10. "아직 어색한 거 하나" — 화면 변경 commit 의무 (game-craft-upgrade ①, 2026-05-20)

> 문제: 코드는 통과하는데 사람이 보면 어색한 걸 자체 발견 못 함 (핀 범위·툴팁 위치·공포톤 등 전부 도현이 manual play로 발견).
> 원인: "컴파일 0 + 에러 0 = 완료" 본능. 자기 결과물을 한 번도 안 봄.
> 해법: "느껴봐" X / "안 보면 못 넘어가는 상황 강제" O. commit body에 어색한 점 1개를 의무로 적게.

**언제**: commit이 화면 관련 파일을 건드릴 때 — `.cs`(UI/Camera/Marker/Canvas/HUD/Screen/Tooltip 키워드 포함) / `.unity` / `.prefab`.

**무엇**: commit body에 아래 한 줄 의무. 빈칸·"없음"·형식만 채우기 금지.

```
Feels-off: <지금 이 변경에서 아직 어색하거나 아쉬운 점 1개 — 구체적으로>
```

- "다 좋음" / "없음" 보고 금지. 무조건 1개는 찾는다. 진짜 없으면 "더 나아질 수 있는 점 1개"라도.
- 빈약한 답("색이 칙칙함" 같은 모호한 것) 대신, 다음 commit 후보가 될 만큼 구체적으로.
- Stop hook이 화면 변경 commit에 `Feels-off:` 없으면 block (자동 강제).
- 사소한 docs/markdown/hook-only commit은 해당 X (`[skip-verifier]`와 별개로 화면 파일 안 건드리면 자동 면제).

**왜 이게 핵심**: 매 화면 commit마다 자기 결과물을 한 번씩 강제로 째려보게 만드는 게 목적. 검증 셋(§1)이 "통과/실패"라면 §10은 "더 나아질 여지"를 항상 1개 끌어낸다.
