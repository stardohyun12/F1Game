---
script: F1/Assets/Scripts/Cargo/CargoBox.cs
category: cargo
phase_applied: [phase1, phase3, phase4]
tags: [code/cargo, code/gameplay-core]
linecount: 359
signature: class CargoBox
last_synced: 2026-05-12
---

# CargoBox

> 한 줄: **USP 그 자체.** 차량 G/spin/tilt를 측정하고 무결성 HP를 깎는다. "화물이 운전을 결정한다"의 코드 표현.

## 책임 (Single Responsibility)
- **G/spin/tilt 측정** — 차량 Rigidbody.linearVelocity를 차 local frame으로 투영 → 종G/횡G. 피크(0.2s) + 누적(1s) 산출.
- **데미지 적용** — `ApplyDamage`. 임계 초과분에 multiplier 곱해 HP 감소.
- **충돌 핸들링** — `OnCollisionEnter` → `OnImpact` 이벤트 + AudioCueBus.Publish + `ApplyDamage`.
- **외부 데미지 진입** — `ApplyExternalDamage(dmg, flashSpeedKmh)` (신호등 위반 등).
- **프리셋 적용** — `ApplyPreset()`이 Phase 3 modifier chain으로 11 stat 계산. 새 modifier 추가 시 CargoBox 안 건드림.

## 의존 (들어오는 — 누가 나를 부르는가)
- [[Core]] — `Services.Cargo`로 등록.
- [[RunController]] — `BeginRun` 시 `cargoBox.SetJob(job)` → `ApplyPreset`.
- [[IntegrityHUD]] — `OnImpact`, `OnIntegrityChanged` 구독.
- [[FollowCamera]] — `OnImpact` 구독 (충돌 shake).
- [[CargoWarningHUD]] — `CurrentLongG`, `CurrentLatG`, `CurrentSpinDegPerSec`, `CurrentTiltDeg` 폴링.
- [[AudioManager]] — Phase 4 이전엔 직접 구독, 이후 AudioCueBus 경유.
- [[TrafficLightArea]] — `ApplyExternalDamage` 호출.

## 의존 (나가는 — 내가 부르는 것)
- [[CargoTypePresets]] — 베이스 stat 가져옴.
- [[Modifiers/ICargoModifier]] 리스트 — type → packing → upgrades 순서.
- [[Modifiers/CargoTypeModifier]], [[Modifiers/PackingModifier]], [[Modifiers/UpgradeModifiers]].
- [[AudioCueBus]] — `CargoImpact` / `CargoTotaled` 발행.

## 관련 결정
- [[notes/decisions-log#D-029]] 2주차 수치 — G 임계 기준.
- [[notes/decisions-log#D-030]] K1~K5 — 핵심 수치.
- [[notes/decisions-log#D-040]] 컨셉 전환 — 현실 배송.
- [[notes/decisions-log#D-041]] Packing 시스템.
- [[notes/decisions-log#D-042]] CargoShake 패턴 C.
- [[notes/decisions-log#D-052]] CargoWarningHUD 반응형 경고.
- [[notes/decisions-log#D-062]] 화물 손상도 입력 Spike 완화.

## 관련 학습
- [[phase1-dip-service-locator]] — Services 경유.
- [[phase3-modifier-chain]] — `ApplyPreset` OCP 해소. 이 시스템의 핵심 학습.
- [[phase4-audio-cue-bus]] — 충돌 발행.
- [[GDD/systems/cargo]] — 디자인 스펙.

## 핫스팟 (변경 가능성 높음)
- **현재 안정** — Phase 3 적용 후 새 modifier 추가가 Registry 한 줄.
- **튜닝**: longWarnG, latWarnG, spinWarnDegPerSec, tiltWarnDeg, impactWarnKmh 임계값 + multiplier 8개. CargoTypePresets에서 화물 타입별 차등.
- **D-063 (미래)**: 3D 회전 포장 단계. 새 dimension 추가 시 CargoBox + Modifiers 양쪽 영향.
