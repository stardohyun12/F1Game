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

## 2. ❓ 미해결 결정 (도현 결정 대기)

### D-073 — 부품 트리 깊이

| 옵션 | 단계 | 데모 사이즈 | 풀게임 확장성 |
|---|---|---|---|
| A | 6 부품 × 3 티어 = 18 | 큼 | OK |
| **B (추천)** | 6 부품 × 1 티어 = 6 | 데모 2개 복원 적합 | A로 확장 가능 |
| C | 3 부품 + 컷씬 위주 | 가장 작음 | 드라마틱 |

데모 vertical-slice spec = 2개 복원. B가 자연.

### D-075 — 차고 시각 디자인

| 옵션 | 구현 시간 | 임팩트 |
|---|---|---|
| **A (추천)** | 사이드뷰 분해도 — 빠진 자리 회색, 복원되면 색칠 | 짧음 | 큼 |
| B | 3D 회전 머신, 클릭으로 부품 정보 | 김 | 폴리시 |
| C | 카드 그리드 (Slay the Spire 풍) | 중간 | 카드게임 정서 |

A가 W3 1주 안에 충분, B는 풀게임 후속.

### D-077 (신규) — 부품 6 카테고리 분류

기존 리서치 후보 두 갈래:
- (a) F1 메타포 충실: 엔진·변속기·서스·에어로·타이어·도색
- (b) 게임플레이 영향 명확: 가속·코너·내구·연료효율·시각·???

**🤖 내 제안**: **하이브리드**. 이름은 F1 메타포 (a), 효과는 결정 채널 9 dial에 직접 매핑.

| 부품 | 효과 (채널) | 시각 효과 |
|---|---|---|
| 엔진 | 최고 속도 + (채널 1) | 보닛 색칠 |
| 변속기 | 가속 곡선 (채널 1) | 기어 박스 가시 |
| 서스 | 화물 흔들림 감소 (채널 5) | 서스 가시 |
| 에어로 | 코너 그립 (채널 1) | 윙 부착 |
| 타이어 | 그립 + 슬라이드 위험 (채널 1) | 타이어 새 모델 |
| 도색 | 외형만 (채널 2) | 머신 도색 |

도색은 마지막 = 그랑프리 출전 자격 의식.

### D-078 (신규) — 데모 슬라이스 어떤 2개 복원?

W3 = 2개 복원 가능해야. 어떤 2개?

- 첫 복원: 의미 강하고 게임플레이 즉시 체감 = **엔진** (최고 속도 ↑)
- 두 번째: 시각 임팩트 + 다른 채널 = **도색** (외형 변화, 그랑프리 dangling carrot)

또는: 엔진 + 타이어 (둘 다 운전 체감)

**🤖 추천**: 엔진 + 도색. 게임플레이 변화 1개 + 서사 변화 1개. 데모 영상에서 "엔진 살았다" 컷씬 + "도색 완료" 컷씬 둘 다 보임.

### D-079 (신규) — 부품 가격

데모 한 런 보상 ≈ 500~1800₩. 부품 가격은:
- 너무 싸면: 한 런으로 살 수 있음 → 무게감 없음
- 너무 비싸면: 데모 시간(5~15분 × 3~5 런)으로 못 도달 → 데모에 안 보임

**🤖 추천**: 첫 부품(엔진) = **3000₩** (안전 잡 6번 = 3000). 두 번째(도색) = **8000₩** (위험 잡 4~5번). 합 11000. 데모 시연 = 10 런 = 약 30~60분 분량.

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

```
┌──────────────────────────────────────────┐
│  CRAFT POINTS                          ₩4500 │
├──────────────────────────────────────────┤
│                                          │
│        [사이드뷰 F1 머신 일러스트]            │
│         ┌─엔진┐  ┌─변속기┐  ┌─서스┐     │
│         │ ✓  │  │ 회색 │   │ 회색│      │ ← 복원=색칠, 미복원=회색
│         └────┘  └────┘    └────┘       │
│                                          │
├──────────────────────────────────────────┤
│  부품 카드 (가로 슬라이드)                  │
│  [엔진 ✓] [도색 3000₩] [변속기 5000₩(잠금)] │
└──────────────────────────────────────────┘
```

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

### 3.4 채널 9 효과 적용

`F1Part.effect` → 적용 메서드:
- 엔진 → ArcadeCarController.maxSpeedMps += effectAmount
- 도색 → CarVisualMaterial swap (그랑프리 컬러)
- (확장) 변속기 → 가속 곡선 boost
- (확장) 서스 → CargoShakeSimulator.dampening +
- (확장) 에어로 → ArcadeCarController.cornerGrip +
- (확장) 타이어 → TireSystem 기본 그립 +

이미 있는 UpgradeSystem과 비슷한 구조 → 인터페이스 통일 가능.

---

## 4. 🤖 W3 구현 1주 일정 (5/19~26)

| 일 | 작업 |
|---|---|
| Day 1 (5/19) | 기획 확정 (이 파일) → spec 문서로 promote |
| Day 2 (5/20) | F1Part SO + 데이터 2개 (엔진/도색) + F1RestoreSystem 코드 |
| Day 3 (5/21) | 차고 화면 UI (사이드뷰 + 카드 + 디테일 패널) |
| Day 4 (5/22) | 정산 → 차고 → 허브 흐름 연결 |
| Day 5 (5/23) | 복원 컷씬 (회색→색칠 애니 + flavorLine) + 효과 적용 |
| Day 6 (5/24) | manual play + 페이싱 튜닝 (가격·보상 비율) |
| Day 7 (5/25) | 폴리시 + verifier dispatch |

---

## 5. 👤 도현 의견 (여기 적어줘)

(여기에 직접 타이핑하거나 채팅으로 알려줘. 옵션 A/B/C 선택, 가격 의견, 디자인 피드백 등)

---

## 6. ❓ 미정 / 후속

- W3 후 W4: 정산 → 차고 자동 진입 vs 선택? (지금은 선택 권장)
- 복원 컷씬 길이? (2~3초 권장)
- F1 머신 모델: 현재 빨간 머신 → 도색 전 회색 머신 자산 필요? (그레이스케일 머티리얼 swap으로 회피 가능)
- 도색 효과: 외형만? 아니면 "도색 완료 = 그랑프리 모드 unlock 트리거"?

---

## 7. 📚 참조

- 리서치: `옵시디언/research/garage-parts-reference.md`
- 데모 spec: `옵시디언/dev-plan/2026-05-13-vertical-slice-design.md`
- 결정 채널 9: `옵시디언/GDD/decision-channels.md`
- 마스터 plan: `옵시디언/GDD/master-plan-2026-05-17.md`
