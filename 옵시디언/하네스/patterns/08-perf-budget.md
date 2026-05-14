# 08. Perf Budget Blindness

## 증상
AI가 `Update`에서 `GetComponentInChildren`을 매 프레임 호출, `new Material(shader)`를 매 BeginRun마다 생성, `FindObjectsOfType`을 hot path에 둠. 코드는 작동. 30분 플레이 후 GC spike, FPS drop, Material 누수.

## 왜 발생 (AI 한계)
- AI는 정적 분석으로 빠르게 짜는 데 집중. 매 프레임 호출인지 1회인지 코드만 봐선 잊기 쉬움.
- profiler 없으면 hot path 모름. CPU·GPU·메모리 어디가 병목인지 추측 불가.
- 누수는 30분+ 플레이 후에야 보임. AI 단일 세션엔 안 잡힘.

## 회피책
- **즉시**: `Update`, `LateUpdate`, `FixedUpdate` 안에 `Find*`, `GetComponent*`, `new` 키워드 들어가면 캐싱 또는 한 번만 호출하도록 의심.
- **워크플로**: 마일스톤마다 profiler session — 1분 플레이 → 메모리·CPU 스냅샷.
- **도구**: 엔진 profiler CLI, frame debug, memory snapshot. Unity: `unity-cli exec` + Profiler API.
- **AI 룰**: Material/Texture/RenderTexture `new` 시 `OnDestroy` 또는 scene unload에서 `Destroy` 짝 보장. 매 BeginRun `new`라면 cache+swap 확인.

## 적용 신호
- 새 Update 메서드 추가
- material/texture/RT 생성 코드
- 매 프레임 cleanup 필요한 일회성 데이터 (e.g., temp lists)
- string concatenation/format 매 프레임
- iteration 중 list mutation
- coroutine·event 구독 cleanup 부재
