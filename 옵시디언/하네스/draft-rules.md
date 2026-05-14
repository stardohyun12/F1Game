# AI 약점 회피 룰 — 초안 (적용 보류)

> **상태: 보류** — 도현이 코딩 잘하는 형한테 컨펌 받은 후 `.claude/rules/ai-blindspots.md`로 이동 + `CLAUDE.md` import 예정.
> 그때까지는 **참조용**. AI는 자동 적용 안 함. incidents.md에 사례만 누적.

## 정한 논리 (한 단락)

AI 약점 = "어디 못 보나" (시각·GPU·binary·손맛·재미) + "어디 잘못 추측하나" (성능·와이어링·경계·인스턴스). 게임 개발은 이 약점이 다 모이는 도메인. 각 약점 → 가장 자주 망치는 상황 1개 → 그 상황을 자동으로 의심하게 하는 짧은 룰 1개.

## 10개 룰 표

| # | 룰 | 무슨 상황에서 터지나 (게임 일반) | AI 한계 | 룰이 막는 것 | 출처 패턴 |
|---|---|---|---|---|---|
| 1 | 시각 결과 추측 X | 캐릭터 모델을 씬에 배치. 코드는 `transform.position = spawnPoint`라 멀쩡한데 실제론 카메라 시야 밖이거나 땅 아래 박힘 | GPU 결과 못 봄. 좌표 숫자만 보고 "맞다" 추측 | "실행해서 직접 봐라" 사용자에게 요청하도록 강제 | [[01-visual-blindness]] |
| 2 | Update 안 `Find`/`new` 금지 | 적 30마리 각자 매 프레임 `FindObjectsOfType<Player>`. 초당 1800회 풀씬 탐색. 30분 후 FPS drop | "한 번 호출"인지 "매 프레임"인지 헷갈림. profiler 없으면 hot path 모름 | Awake에 캐싱하도록 자동 의심 | [[08-perf-budget]] |
| 3 | `new Material/Texture/RT` 짝 `Destroy` | 적이 피격당할 때마다 `new Material(redFlash)`. 30분 후 메모리 700MB | Unity 특수: `material` getter도 instance 생성. AI는 일반 GC 가정 | "new = Destroy 짝" 자동 의식 | [[08-perf-budget]] |
| 4 | `[SerializeField]` 폴백 또는 명시 NullCheck | 새 컴포넌트 + 인스펙터 드래그 깜빡 → NullReferenceException으로 게임 죽음 | `.prefab` / `.unity` YAML fileID 참조 정독 안 함. "코드 OK니까 와이어링도 됐을 거" 가정 | 자가구성 폴백 또는 명시 에러 코드 자동 끼움 | [[02-scene-inspector-wiring]] |
| 5 | UI N/M 표시면 실제도 N | 인벤 UI에 "슬롯 1~20" 그렸지만 코드는 첫 슬롯만. 디버깅 1시간 날림 | 데모용 첫 케이스만 짜는 경향. UI는 미리 N개 다 그려놓고 시간 절약 → 거짓말 | UI N과 실제 처리 N 일치 또는 N=1로 숨김 | [[06-multi-instance-edges]] |
| 6 | 숫자 튜닝 한 번에 1개 | jumpHeight·gravity·jumpDuration 한 번에 바꿈. "더 좋아졌어요"는 됐지만 어느 게 효과인지 모름 → 다음 튜닝 불가 | "여러 조정 한 번에" 효율적이라 판단. 손맛 검증 분리 안 됨 | 변경 1개씩 → 검증 → 다음 | [[04-physics-tuning-feel]] |
| 7 | 재미 평가 위임 X | "이 메커닉 재밌을까요?" → AI generic 답 → 사용자 plays 없이 더 추가 → 안 재밌음 | 메커니즘 작동은 검증 가능하지만 즐거움 자체 경험 X | "5분 플레이 후 판단해라" 답으로 전환 | [[05-fun-judgment]] |
| 8 | 시스템 경계 양쪽 다 읽기 | save 시스템 새 필드 추가. save 측만 짜고 commit → 다음 빌드 로드 시 진행도 리셋. 출시 후 발견 | 한 시스템 컨텍스트에서 집중. 다른 시스템에서 같은 데이터 만지는 코드 놓침 | save↔load, event publish↔subscribe, transition entry↔exit 양쪽 같이 본다 | [[07-system-boundary-handoff]] |
| 9 | "정리"·"애매"엔 add 금지 | "이거 좀 정리해" → "리팩터링 진행" → 새 manager + 인터페이스 + 패턴 추가 → +500줄. dead code는 그대로 | 학습 bias: "도움 = 더 만들기". 삭제·축소는 불안 → 안전한 add 선택 | 모호어("정리"·"애매"·"청소") 감지 시 delete 모드 강제 | [[10-default-to-add]] |
| 10 | mesh size 가정 hardcode X | "박스 prefab 1×1×1이니까 scale 1" → 실제 1.5×0.8×1 → 찌그러져 보임. 충돌 박스 어긋남 | `.fbx`/`.mesh` binary 못 봄. 파일 이름만 보고 추측 | `mesh.bounds.size` 런타임 측정 또는 "확인해줘" 요청 | [[03-asset-opacity]] |

## 빠진 패턴

- `[[09-input-feel]]` — 입력 컨트롤러 손맛 (jump buffer, coyote time, dead zone). 현재 컨펌 대상 프로젝트에 즉시 trigger 적어 일단 보류. 입력 시스템 손볼 때 추가.

## 신규 패턴 후보 (incidents에서 — 아직 룰 아님)

- **검증 도구 부작용 맹점** — 검증 도구(unity-cli `EditorApplication.Step()`, batch exec 등)가 게임 상태를 바꾸거나 멈추는데, AI가 그 결과를 게임 버그로 오진. 기존 10개는 "게임을 못 본다"에 집중 — 이건 "검증 행위 자체가 관측을 오염시킨다". 출처: `incidents.md` 2026-05-14. 같은 패턴 2건째 더 모이면 `patterns/11`로 승격 검토. 후보 룰: "런타임 거동 검증 전 frameCount 증가부터 확인. 멈춰 보이면 도구 부작용부터 의심."

## 컨펌 받을 때 형한테 줄 자료

1. 이 표 (룰 + 시나리오 + 근거)
2. `옵시디언/하네스/patterns/01..10` 상세 docs
3. `옵시디언/하네스/incidents.md` — 실제 발생한 incident 모음 (룰 정당화 증거)

형 컨펌 사인 받으면 → 새 plan으로 자동화 적용 ([[mighty-conjuring-gray|plan 파일]] "컨펌 후" 섹션).
