# 🔧 차고 / F1 부품 복원 — 실시간 기획 캔버스

> 세션 27 (2026-05-19) · W3 USP 핵심 작업.
>
> **사용법** — 옵시디언에서 이 파일 열어둠. 내(AI)가 Edit 저장 → 옵시디언 자동 리로드. 도현은 직접 타이핑 또는 채팅으로 의견.
>
> **섹션 마커**: 🤖 AI 제안 / 👤 도현 / ✅ 결정 / ❓ 미해결
>
> **참조**: [[2026-05-15 garage-parts-reference]] · [[decision-channels]] · [[overview]]

---

## 1. ✅ 이미 확정된 것

- **컨셉 (D-051)**: 망한 F1 팀 → 배달로 자금 → 부품 복원 → 그랑프리 복귀
- **메타 루프 모델**: Hades 스타일 (반복 = 서사 누적, 같은 콘텐츠지만 매번 누적)
- **부품 복원 = 한 번의 결정**: Car Mechanic Simulator 교훈 — 인터랙티브 분해 미니게임 금지. "선택 → 확정 → 짧은 컷씬" 한 클릭.
- **차고 = 물리 공간** (Pacific Drive): 차고는 별도 화면, F1 머신 시각적으로 자리잡음.
- **시각 변화 = 서사 진행** (Spiritfarer): 부품 복원하면 머신 외형이 눈에 띄게 바뀜.

---

## 2. ✅ 새로 확정된 결정 (2026-05-19 도현)

### D-073 — 부품 트리 깊이 = **B. 6 부품 × 1 티어**
풀게임에서 티어 추가는 가능. 일단 6 카테고리 각각 1번씩 복원.

### D-075 — 차고 시각 = **A. 사이드뷰 분해도**
빠진 자리 회색 / 복원되면 색칠. 단순·임팩트 큼.

### D-089 — 부품별 수치/키바인딩 확정 (2026-05-19)

| 부품 | 수치/입력 | 확정값 |
|---|---|---|
| 엔진 | 최고속/가속 | **+20% 최고속 · +30% 가속** |
| 변속기 | 수동 키바인딩 | **K=다운시프트 / L=업시프트** (오른손, WASD와 분리) |
| 서스 | 점프대 활성 | TrackLayout slope tile 의도 위치만 활성 (Day 5 확정) |
| 에어로 | 부스트 시스템 | **게이지 소모·재충전** (Space 누르면 +50% 가속 / 2초 / 재충전 5초) |
| 타이어 | 선택 메뉴 | 잡 출발 전 4종 선택 (드라이/웨트/소프트/하드) |
| 도색 | 외형 + 모드 | 머신 도색 + 그랑프리 모드 unlock |

### D-077 — 부품 효과 = **메커니즘/인터랙션 추가형** (수치 boost X)
도현 방향: 부품 = "유저가 다른 경험을 하게 / 지루함 늦추기". 단순 +5km/h X.

| 부품  | 추가되는 메커니즘                          | 새 입력/선택            | 시각                  |
| --- | --------------------------------- | ----------------- | ------------------- |
| 엔진  | 최고속도/가속 boost (운전 즉시 체감)          | (없음)             | 보닛 색칠 + 시동음 변화      |
| 변속기 | **수동 변속 모드 toggle** (자동→수동 해금)    | Shift+W/S 기어 변경 | 기어 박스 가시            |
| 서스  | **점프대/험지 통과 해금** — 도시에 새 지형 변종 활성 | (자동)            | 서스 부품 가시            |
| 에어로 | **부스트 버튼** — 직선 가속 게이지 unlock     | Space = 부스트     | 윙 부착                |
| 타이어 | **타이어 종류 선택** — 드라이/웨트/소프트/하드 메뉴  | 잡 출발 전 선택       | 타이어 머티리얼 변종         |
| 도색  | **그랑프리 모드 unlock 트리거** (서사 클라이맥스)  | (자동)            | 머신 도색 + 그랑프리 메뉴 점등 |

핵심: 각 부품 복원 = 새 동사/선택지 unlock. 풀게임에서 "엔진+변속기 둘 다 복원 시 수동 변속 + 부스트 콤보" 같은 시너지 자연 발생.

### D-078 — 풀게임 완성 목표 = **6 부품 모두 구현**
도현: "사실 게임 완성이 목표라서 어떤 거만 구현할지는 안 정해도 됨".
→ W3 6일은 6 부품 골조 다 박는 걸 목표. 깊은 폴리시는 후속.

### D-079 — 부품 가격 = **별도 밸런싱 문서**
도현: "처음에는 무조건 엔진 업그레이드 하게 엔진가격 싸게. 별도 문서로".

✅ 결정 1: **첫 부품 = 엔진. 가격 낮음 (1~2 런 안에 도달 가능)** — 튜토리얼 역할.
나머지 5 부품은 **밸런싱 문서**에서 결정. Day 1 작업 항목 추가.

---

## 3. 🤖 시스템 설계 1차 제안

### 3.1 데이터 모델

```csharp
[CreateAssetMenu(menuName = "PitCrew/F1 Part")]
public class F1Part : ScriptableObject {
    public string id;                 // "engine", "paint"
    public string displayName;        // "엔진" / "도색"
    public int restoreCost;           // 3000
    public Sprite portraitGray;       // 미복원 시
    public Sprite portraitColor;      // 복원 후
    public string flavorLine;         // "엔진이 돌아왔다. 시동 소리가 살아있다."
    [TextArea] public string mechEffect; // "최고 속도 +15km/h"
    public UpgradeEffectKind effect;  // 어느 채널·dial에 적용
    public float effectAmount;
}

[CreateAssetMenu(menuName = "PitCrew/F1 Restore Catalog")]
public class F1RestoreCatalog : ScriptableObject {
    public List<F1Part> parts;        // 데모: 2개 (엔진+도색)
}
```

런타임 상태:
```csharp
public class F1RestoreSystem : MonoBehaviour {
    HashSet<string> restoredIds = new();
    public bool IsRestored(F1Part p) => restoredIds.Contains(p.id);
    public bool TryRestore(F1Part p) {
        if (IsRestored(p)) return false;
        if (Services.Upgrade.Wallet < p.restoreCost) return false;
        Services.Upgrade.Spend(p.restoreCost);
        restoredIds.Add(p.id);
        ApplyEffect(p);
        OnRestored?.Invoke(p);
        return true;
    }
}
```

### 3.2 UI — 차고 화면 (D-075 A: 사이드뷰 분해도)

**D-086 레이아웃** (2026-05-19 확정): **머신 가운데 고정 + 부품 카드 하단 가로 나열** (Hades 거울 패턴).
**D-087 잠긴 카드**: **회색 + "X원 부족" 태그**. 클릭하면 "더 벌어야 함" 살짝 흔들림 X (희망 표현). 절대 자물쇠 X.
**D-088 복원 컷씬**: **2~3초**. 회색 → 색칠 페이드 + flavorLine 한 줄 + 살짝 엔진음. 카메라 줌인 X.



```
┌──────────────────────────────────────────────┐
│  지갑: ₩4500                          [닫기] │
├──────────────────────────────────────────────┤
│                                              │
│                                              │
│        ╔══════════════════════════╗          │
│        ║                          ║          │
│        ║    [F1 머신 사이드뷰]    ║          │ ← 화면 중앙 ~60% 차지
│        ║   엔진✓  변속기  서스    ║          │   미복원 = 회색 실루엣
│        ║   ←회색→  ←회색→  ←회색→ ║          │   복원 = 색칠
│        ║                          ║          │
│        ╚══════════════════════════╝          │
│                                              │
├──────────────────────────────────────────────┤
│  ▼ 부품 카드 (가로 6장, 화면 하단)              │
│  ┌────┐┌────┐┌────┐┌────┐┌────┐┌────┐      │
│  │엔진│ │변속│ │타이│ │서스│ │에어│ │도색│  │
│  │ ✓  │ │4000│ │4000│ │8000│ │X 부│ │X 부│  │ ← 회색 + 부족 태그
│  │    │ │ ₩  │ │ ₩  │ │ ₩  │ │족  │ │족  │  │
│  └────┘└────┘└────┘└────┘└────┘└────┘      │
└──────────────────────────────────────────────┘
```

- 머신은 화면 중앙 60% 차지 → 시각 임팩트.
- 카드 6장 항상 보임 (스크롤 X, 한 줄). 가격은 카드 안에.
- 자금 부족 카드: 회색 톤 + "X원 부족" 작은 태그. 클릭 가능 (디테일 보임), 복원 버튼만 disabled.
- 디테일 패널은 카드 클릭 시 머신 위쪽에 짧게 뜸 (Tooltip 형태).

- 사이드뷰 일러스트는 6 부품 자리 표시 (현재는 2개만 active)
- 부품 카드 클릭 → 디테일 패널 → "복원" 버튼 → 자금 충분 시 활성
- 복원 시 화면 가운데 짧은 컷씬 (2~3초): 회색 → 색칠 애니메이션 + flavorLine 텍스트

### 3.3 정산 → 차고 흐름

```
[정산 ResultPanel 닫음]
   ↓
"오늘 ₩2000 벌었음. 차고 가시겠어요?"  ← Yes/No 단순 prompt
   Yes ↓
[차고 화면 진입]
   - 사이드뷰 + 부품 카드
   - 살 수 있는 부품 강조 (테두리 깜빡)
   - 살 거 선택 or 닫기
   ↓
[허브 / 다음 런 준비]
```

미구현 시: 정산 후 자동 차고 진입 (선택지 없음, 간단)

### 3.4 부품별 메커니즘 unlock (D-077 메커니즘형)

각 부품 효과 = "새 시스템 활성". 단순 stat boost 아님.

```csharp
public enum F1PartId { Engine, Gearbox, Suspension, Aero, Tires, Paint }

public class F1RestoreSystem {
    void ApplyEffect(F1Part p) {
        switch (p.id) {
            case F1PartId.Engine:
                // 즉시 체감 — boost는 stat이지만 즉각적
                Services.Car.maxSpeedMps += 5f;
                Services.Car.accelMul *= 1.25f;
                break;
            case F1PartId.Gearbox:
                // 수동 변속 모드 unlock — 기존 GearSystem에 ManualEnabled toggle
                Services.Gears.SetManualUnlocked(true);
                break;
            case F1PartId.Suspension:
                // 도시에 점프대/험지 변종 활성 (TrackLayout heightMask?)
                CityTerrainSystem.UnlockJumps();
                break;
            case F1PartId.Aero:
                // 부스트 게이지 시스템 활성 (새 BoostSystem 또는 ArcadeCarController 내부)
                Services.Car.BoostEnabled = true;
                break;
            case F1PartId.Tires:
                // 타이어 선택 메뉴 unlock (기존 TireSystem에 SelectAllowed 토글)
                Services.Tires.SetSelectionUnlocked(true);
                break;
            case F1PartId.Paint:
                // 외형 + 그랑프리 모드 메뉴 점등 (엔드 시퀀스 trigger)
                CarVisuals.SwapToGrandPrixPaint();
                GrandPrixModeSystem.SetUnlocked(true);
                break;
        }
    }
}
```

새 시스템 필요:
- `CityTerrainSystem` (점프대 변종) — TrackLayout heightMask 활용 또는 spawn 변종 (서스 unlock 시)
- `BoostSystem` (부스트 게이지) — ArcadeCarController 확장
- `GrandPrixModeSystem` (도색 후 unlock) — 메뉴 진입점

W3 6일 안에 메커니즘 다 박는 건 빡셈. **각 메커니즘 1차는 골조만**, 폴리시는 W5 이후.

---

## 4. 🤖 W3 구현 1주 일정 (5/19~26) — 6 부품 모두 목표 (D-078)

| 일 | 작업 |
|---|---|
| Day 1 (5/19) | 기획 확정 + **밸런싱 문서** spec → `옵시디언/dev-plan/2026-05-19-restore-economy.md` |
| Day 2 (5/20) | F1Part SO + 데이터 6개 + F1RestoreSystem 코드 (전 부품 골조) |
| Day 3 (5/21) | 차고 화면 UI (사이드뷰 6 자리 + 카드 6장 + 디테일 패널) |
| Day 4 (5/22) | 메커니즘 1: 엔진(stat boost) + 변속기(수동 모드 toggle) + 도색(머티리얼 swap) |
| Day 5 (5/23) | 메커니즘 2: 타이어(선택 메뉴) + 에어로(부스트 시스템 골조) + 서스(점프대 활성 골조) |
| Day 6 (5/24) | 정산 → 차고 흐름 + 복원 컷씬 + manual play + verifier |
| Day 7 (5/25) | 폴리시 + 밸런싱 튜닝 + 최종 verifier |

**리스크**: 부품 6 메커니즘 × 폴리시 ≠ 1주. 메커니즘 골조만 박고 폴리시는 W5/W5b로 미루는 게 현실. Day 4~5는 "골조 작동" 기준 (수동 변속이 작동만, 사운드·튜닝은 X).

---

## 5. 👤 도현 의견 (여기 적어줘)

(여기에 직접 타이핑하거나 채팅으로 알려줘. 옵션 A/B/C 선택, 가격 의견, 디자인 피드백 등)

---

## 6. ✅ 추가 확정 (2026-05-19)

- **D-083 자금 carryover** = 완전 누적 (Hades 패턴, 메타 필수)
- **D-084 소비 페널티** = 정산액 비례 감소 (현재 구현 호환)
- **D-085 정산→차고 흐름** = Yes/No prompt (자동 강제 X)
- **도색 효과**: 외형 + 그랑프리 모드 unlock 트리거 (D-077 표 참조 — 확정)

## 7. ❓ 잔여 미정 / 후속

- 복원 컷씬 길이? (2~3초 권장 — Day 6에 박을 때 확정)
- F1 머신 모델: 도색 전 회색 머신 자산 필요? (그레이스케일 머티리얼 swap으로 회피 가능)
- 자금 상한선 (누적 무한대 vs 캡 99999₩)

---

## 7. 📚 참조

- 리서치: `옵시디언/research/garage-parts-reference.md`
- 데모 spec: `옵시디언/dev-plan/2026-05-13-vertical-slice-design.md`
- 결정 채널 9: `옵시디언/GDD/decision-channels.md`
- 마스터 plan: `옵시디언/GDD/master-plan-2026-05-17.md`
