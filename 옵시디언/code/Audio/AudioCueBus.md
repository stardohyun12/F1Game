---
script: F1/Assets/Scripts/Audio/AudioCueBus.cs
category: audio
phase_applied: [phase4, phase6]
tags: [code/audio, code/event-bus]
linecount: 63
signature: enum AudioCueId + static class AudioCueBus
last_synced: 2026-05-12
---

# AudioCueBus

> 한 줄: 발행자(시스템)와 구독자(AudioManager) 사이의 정적 중계자. **새 SFX 추가 시 AudioManager가 그 시스템을 모름**.

## 책임 (Single Responsibility)
- **cue 카탈로그** — `AudioCueId` enum 6종 (CargoImpact, CargoTotaled, DeliveryCompleted, UpgradePurchased, RunResultSuccess, RunResultFail).
- **Publish/Subscribe API** — 정적 Dictionary<CueId, Action<float>>.
- **lifecycle reset** — `[RuntimeInitializeOnLoadMethod]`로 Play 진입 시 자동 Clear. "Enter Play Mode without Domain Reload" 옵션의 stale handler 회피.

`(id, float)` 단일 시그니처. value는 cue별 의미 (CargoImpact = speedKmh, 의미 없으면 무시).

## 의존 (들어오는 — 누가 나를 부르는가)
**발행자:**
- [[CargoBox]] — `CargoImpact` (2곳), `CargoTotaled`.
- [[DeliveryManager]] — `DeliveryCompleted`.
- [[UpgradeSystem]] — `UpgradePurchased`.
- [[AudioManager]] — `PlayResultSfx`가 RunResultSuccess/Fail 발행.

**구독자:**
- [[AudioManager]] — 6 cue 모두. `SubscribeCues` / `UnsubscribeCues`.

## 의존 (나가는 — 내가 부르는 것)
- 없음 — pure event bus. UnityEngine 의존은 RuntimeInitializeOnLoadMethod 한 줄.

## 관련 결정
- 세션 19 결정 (2026-05-11): AudioManager OCP 해소를 Mediator로.
- 팀 A 감사 발견 B (이전 architecture-review): "Phase 1 후에도 AudioManager가 시스템 직접 알고 있음".

## 관련 학습
- [[phase4-audio-cue-bus]] — 본 파일의 *왜* + 패턴 일반화.
- [[phase6-peer-review-patches]] — A8 (RuntimeInitializeOnLoadMethod 추가).
- [[📚 학습 지도]] — Pub/Sub Mediator 패턴 항목.

## 핫스팟 (변경 가능성 높음)
- **새 SFX 추가**: enum 항목 + 발행 한 줄 + AudioManager에서 Subscribe 한 줄. AudioManager가 시스템 자체 모름.
- **이벤트 시그니처 변경**: `(id, float)` 한정. 더 풍부한 페이로드 필요해지면 제네릭 채널로 확장 (지금은 MVP 스코프 외).
- **자동 Clear 신뢰**: A8 hook 덕분에 도메인 리로드 없이도 깨끗. 단 멀티 publisher 발생 시 검토.

## 노트
- 패턴은 **Mediator + Observer 변형**. C# `event` 직접 노출과의 차이는 *허브가 가운데 있어 발행/구독이 서로 안 보임*.
- 게임 관점에서 적합한 영역: 오디오, 도전과제, 통계, 튜토리얼 트리거. 응답 받아야 하는 호출(메서드)은 부적합.
- delegate combine으로 핸들러 누적. Unity 단일 스레드 가정이라 thread-safety 약해도 OK.
