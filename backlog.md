# PitCrew 백로그 (PM 소유)

> PM이 소유하는 우선순위의 진실. 전원 읽음. 파이프라인 패널이 이 파일을 파싱.
> 형식: `## <칸>` 아래 `- [태그] 제목 — brief: ready|todo (커밋)`
> 태그: 화면 / 데이터 / 시스템 / 메타. brief: ready = 디스패치 브리프 준비됨.

## 진행중
(없음 — 메인 대기 중)

## 대기 (임계경로 순)

### 1순위 — 데모를 막는 진짜 블로커
- [시스템] 맵 넓히기 — 한 런 8~12초 vs 5~15분 갭 (200~900배) — brief: ready, **도현 결정 3건 대기** (`docs/studio/briefs/map-run-duration-gap.md`)
  - ① 항구 바닥 색온도 ② 공업 갈색 채도 수위 ③ 다운타운 바닥(콘크리트 vs 잔디 유지) — 버스 PM→도현 [05-26 15:00]
- [시스템] 차고 Task 12 통합 verifier — 구현 완료, 검증 0건 — brief: ready (`docs/studio/briefs/garage-task12-verifier.md`)

### 2순위 — 맵/차고 안정 후
- [데이터] 화물 고폴리 decimate (Eggs/Glass/Fish/Cake) — 회귀 유일 red (CargoPolyBudget FAIL) — brief: todo
- [시스템] 경로계획 경로선 방향 힌트화 (점선/화살표 — 도현 결정 C안 추천) — brief: todo

### 3순위 — 폴리싱 (맵/차고 완료 후)
- [화면] 항구 ground 균일 모래톤 (lot별 얼룩 제거) — brief: todo
- [화면] 바다↔도시 맞닿음 (떠보임 해소) — brief: todo
- [화면] 공업 카키 채도↑ ("흙"답게) — brief: todo
- [화면] 포장 화면 빈 상태 폴리시 — brief: todo

## 완료
- [메타] 에이전트 스튜디오 팀 Phase 1 — brief: ready (d35d4a8)
- [메타] 에이전트 스튜디오 팀 스펙 — brief: ready (67b60e4)
- [화면] 교차로 네모블럭 평탄화 — brief: ready (926234c)
- [시스템] OrderBoard wiring 자동 가드 — brief: ready (ac1250b)
