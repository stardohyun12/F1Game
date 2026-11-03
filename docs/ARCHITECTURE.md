# 아키텍처 (2026-11-03, D-287) — 한 장

도현: "우리 게임에 알맞는 아키텍쳐 정하고 그 방식으로 스크립트를 나누자 그리고 구현할 때 하나씩 고칠 수 있는 쪽으로" → 아래로 확정("ㄱ"). 모든 새 코드는 이 층 중 하나에 들어간다. 층을 가로지르는 참조는 컴파일러(asmdef)가 막는다.

## 층 (의존은 아래로만)
```
5 Debug          봇·디버그 창·검사(WorldInvariants)      ← 게임 코드에 디버그 분기 금지
4 Presentation   도트 UI·노을·소리·손(그리기만)          ← 신호만 듣는다, 상태를 뒤지지 않는다
3 Stages         하루 흐름: 단계 클래스 8개 + DayDirector  ← Enter/Tick/Exit, 다른 단계를 모른다
3 Interactables  세상 속 물건: IInteractable(Prompt/CanUse/Use) ← 단계가 물건을 직접 부르지 않는다
2 World          데이터 에셋(장소·주민·물건·주문·수치)   ← world/SPEC.md 에서 생성기가 만든다
1 Core           순수 C#(UnityEngine 없음): 돈·파손·달력·주문 규칙 + 테스트
```
- **신호(Signals)**: 층 사이는 이벤트 버스 하나로만 잇는다. 예: `NotePicked` → Stage_Depot 이 다음 단계, UI 가 전표를 듦, 소리가 종이 소리. 서로 참조 0.
- 3층의 Stages 와 Interactables 는 같은 높이(서로 참조 금지, 신호로만).

## 폴더·어셈블리
```
F1/Assets/Scripts/
  Core/            PitCrew.Core.asmdef   (noEngineReferences = true) + Core.Tests/
  World/           PitCrew.World.asmdef  ScriptableObject 정의 + 생성된 에셋(Assets/World/)
  Signals/         PitCrew.Signals.asmdef 이벤트 정의·버스 (Core 만 참조)
  Interactables/   PitCrew.Interact.asmdef
  Stages/          PitCrew.Stages.asmdef  DayDirector + Stage_*.cs
  Presentation/    PitCrew.View.asmdef    DotScreenUI·SunsetLook·Audio·Hands 그리기
  Debug/           PitCrew.Debug.asmdef   (Editor 전용 아님 — Play 중 봇 때문에 런타임)
```
asmdef 참조: Core ← World ← Signals ← Interact/Stages ← View ← Debug. 거꾸로 참조하면 컴파일 에러 = 규칙 위반이 바로 보인다.

## 규칙
1. 숫자(돈·확률·속도·시간)는 Core 또는 World 에셋에만. 코드 리터럴 금지.
2. 단계는 다른 단계·UI·물건을 import 하지 않는다. 신호를 쏘고 듣는다.
3. 물건 하나 = 파일 하나(Note.cs, Box.cs, Tape.cs, Cart.cs, Door.cs, Bed.cs…). 포장을 고치면 Box/Tape/Bubble 파일만 연다.
4. Presentation 은 `View` 모델(신호로 갱신)만 그린다.
5. 파일 400줄 넘으면 나눈다.
6. 디버그·봇은 Debug 층. `#if DEBUG` 분기를 게임 코드에 넣지 않는다.
7. 과제 브리프는 "이 과제가 여는 층·파일"을 먼저 적는다. 그 밖은 손대지 않는다.

## 왜 (도현 질문 "Unity 없이 가도 괜찮은 이유")
Core 만 Unity 없음: 검사가 Play 없이 1초(`dotnet test`), 모델이 가장 안전하게 고치는 코드, 밸런싱이 숫자 몇 개. 게임은 그대로 Unity.

## 옮기기 (과제 05, 동작 불변)
지금 `OnFootDirector.cs`(1,747줄)·`PackBench.cs`(1,012)·`DotScreenUI.cs`(900) 를 위 층으로 자리만 옮긴다. 확인 = 분리 전·후 같은 Play 로그(단계 순서·시각 ±1초), 씬 참조 끊김 0, 모든 파일 400줄 이하, Core 테스트 10개 이상 통과.
