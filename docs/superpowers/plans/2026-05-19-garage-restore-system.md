# W3 Garage / F1 Restore System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** F1 머신 6 부품 복원 메타 시스템을 박는다 — 정산 자금으로 부품 구매 → 메커니즘 unlock → 매 런 새 동사·선택지 추가. D-073~093 결정 전체 반영.

**Architecture:**
- 데이터: `F1Part` ScriptableObject + `F1RestoreCatalog` SO (6 부품 데이터).
- 런타임: `F1RestoreSystem` (Services에 등록, wallet 차감, 영구 상태, OnRestored 이벤트). Hashset 기반 멱등 복원.
- 효과: 각 부품 = `ApplyEffect()` switch — 이미 있는 시스템(`TireSystem`, `GearSystem`, `ArcadeCarController`)에 toggle/배수 주입. 새 시스템 최소화.
- UI: `GarageScreen` (사이드뷰 + 6 카드 + 디테일 + 수리). 정산→차고는 ResultPanel에 Yes/No prompt.
- 핵심: 자금 carryover는 이미 `UpgradeSystem.Wallet`이 영구 → 추가 작업 0. 화물 손상 비례 페널티는 이미 `ComputeIntegrityMultiplier` 동작 — 검증만.

**Tech Stack:** Unity 6000.3.9f1 · C# · ScriptableObject · Services DI (Core.cs) · UGUI · Input System (Keyboard.current) · unity-cli (포트 8090, Editor 켠 채 검증).

**Verification 규칙 (D-073 즉시 발효):**
- 코드 변경 commit 전 매번 3종 자기 검증 통과 (compile / runtime / visual·numerical via `unity-cli exec`).
- 씬·SO·wiring 변경은 verifier subagent dispatch 후 commit.
- 모든 commit body는 `Verified-By: <self|subagent-id>` 트레일러 포함 (Stop hook 강제).
- 핵심 코드 변경은 `unity-cli editor refresh --compile` → `unity-cli console --type error` 결과 0건 확인.

**참조 spec:**
- `옵시디언/dev-plan/2026-05-19-garage-realtime-planning.md` (캔버스, 결정 D-073~093)
- `옵시디언/dev-plan/2026-05-19-restore-economy.md` (가격 표)
- 결정 이력: `옵시디언/notes/decisions-log.md` D-073~093

---

## File Map (변경 대상)

**신규:**
- `F1/Assets/Scripts/Garage/F1PartId.cs` — enum (Engine/Gearbox/Suspension/Aero/Tires/Paint)
- `F1/Assets/Scripts/Garage/F1Part.cs` — ScriptableObject (id, 가격, 텍스트, 컷씬 데이터)
- `F1/Assets/Scripts/Garage/F1RestoreCatalog.cs` — ScriptableObject (List<F1Part>)
- `F1/Assets/Scripts/Garage/IF1RestoreSystem.cs` — Services 인터페이스
- `F1/Assets/Scripts/Garage/F1RestoreSystem.cs` — MonoBehaviour, 복원 상태 + ApplyEffect
- `F1/Assets/Scripts/Garage/GarageScreen.cs` — UI 화면 (사이드뷰 + 카드 6 + 디테일 + 수리)
- `F1/Assets/Scripts/Garage/GarageRestoreCutscene.cs` — 2~3초 회색→색칠 컷씬
- `F1/Assets/Scripts/Garage/BoostSystem.cs` — 에어로 부스트 게이지 (Space)
- `F1/Assets/Scripts/Garage/TireWearStore.cs` — 4종 영구 마모 게이지 + 펑크 확률 (TireSystem 확장 X, 별도 store)
- `F1/Assets/Editor/_Recovery/SetupGarageSystem.cs` — 6 F1Part asset + Catalog 자동 생성 메뉴

**수정:**
- `F1/Assets/Scripts/Core/Core.cs` — `IF1RestoreSystem Restore`, `IBoostSystem Boost`, `ITireWearStore TireWear` 필드 추가
- `F1/Assets/Scripts/Core/GameContext.cs` — 인스펙터 필드 3개 + Awake 등록 + AutoFindMissing
- `F1/Assets/Scripts/Order/ResultPanel.cs` — "차고로" 버튼 추가, 클릭 시 GarageScreen.Show()
- `F1/Assets/Scripts/GearSystem.cs` — `bool ManualUnlocked`, K/L 입력으로 수동 시프트
- `F1/Assets/Scripts/ArcadeCarController.cs` — `BoostActive` 체크해서 가속/최고속 곱하기
- `F1/Assets/Scripts/Tires/TireSystem.cs` — `SelectionUnlocked` 토글, `OnPuncture` 이벤트
- `F1/Assets/Scripts/Cargo/CargoBox.cs` — 타이어 cargoDamageMul 곱 적용
- `F1/Assets/ComplexTrack.unity` — F1RestoreSystem / BoostSystem / TireWearStore / GarageScreen GameObject 추가 + GameContext wiring

**문서:**
- `옵시디언/notes/session-log.md` — 세션 27 블록 추가

---

## Task 1: F1Part 데이터 모델 + Catalog

**Files:**
- Create: `F1/Assets/Scripts/Garage/F1PartId.cs`
- Create: `F1/Assets/Scripts/Garage/F1Part.cs`
- Create: `F1/Assets/Scripts/Garage/F1RestoreCatalog.cs`

- [ ] **Step 1: F1PartId enum 작성**

```csharp
// F1/Assets/Scripts/Garage/F1PartId.cs
namespace PitCrew.Garage
{
    public enum F1PartId
    {
        Engine = 0,
        Gearbox = 1,
        Tires = 2,
        Suspension = 3,
        Aero = 4,
        Paint = 5,
    }
}
```

- [ ] **Step 2: F1Part SO 작성** (D-077 효과 채널, D-080~082 가격 인스펙터 노출)

```csharp
// F1/Assets/Scripts/Garage/F1Part.cs
using UnityEngine;

namespace PitCrew.Garage
{
    [CreateAssetMenu(menuName = "PitCrew/F1 Part", fileName = "F1Part")]
    public sealed class F1Part : ScriptableObject
    {
        [Header("ID")]
        public F1PartId id;
        public string displayName;

        [Header("Economy (D-080~082)")]
        [Min(0)] public int restoreCost = 1000;

        [Header("Cutscene (D-088)")]
        [TextArea(2,4)] public string flavorLine;     // "엔진이 돌아왔다. 시동 소리가 살아있다."
        [Range(1.0f, 4.0f)] public float cutsceneSeconds = 2.5f;

        [Header("Mechanic Effect (D-089)")]
        [TextArea(2,4)] public string mechanicDescription; // UI에 표시될 효과 설명
    }
}
```

- [ ] **Step 3: F1RestoreCatalog SO 작성**

```csharp
// F1/Assets/Scripts/Garage/F1RestoreCatalog.cs
using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.Garage
{
    [CreateAssetMenu(menuName = "PitCrew/F1 Restore Catalog", fileName = "F1RestoreCatalog")]
    public sealed class F1RestoreCatalog : ScriptableObject
    {
        public List<F1Part> parts = new List<F1Part>();

        public F1Part Find(F1PartId id)
        {
            foreach (var p in parts) if (p != null && p.id == id) return p;
            return null;
        }
    }
}
```

- [ ] **Step 4: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Then: `unity-cli console --type error --since 30s`
Expected: 0건.

- [ ] **Step 5: Commit**

```
git add F1/Assets/Scripts/Garage/
git commit -m "feat(garage): F1Part SO + Catalog + F1PartId enum (Task 1)

데이터 모델 골조. ScriptableObject 3개, 효과는 다음 Task부터.
D-077 D-080 D-082 D-088 반영.

Verified-By: self (compile clean)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: 6 F1Part Asset + Catalog Asset (Editor 자동 생성)

**Files:**
- Create: `F1/Assets/Editor/_Recovery/SetupGarageSystem.cs`
- Create: `F1/Assets/Data/Garage/F1Part_Engine.asset` (via menu)
- Create: `F1/Assets/Data/Garage/F1Part_Gearbox.asset`
- Create: `F1/Assets/Data/Garage/F1Part_Tires.asset`
- Create: `F1/Assets/Data/Garage/F1Part_Suspension.asset`
- Create: `F1/Assets/Data/Garage/F1Part_Aero.asset`
- Create: `F1/Assets/Data/Garage/F1Part_Paint.asset`
- Create: `F1/Assets/Data/Garage/F1RestoreCatalog.asset`

- [ ] **Step 1: SetupGarageSystem Editor 스크립트 작성**

```csharp
// F1/Assets/Editor/_Recovery/SetupGarageSystem.cs
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using PitCrew.Garage;

public static class SetupGarageSystem
{
    const string Dir = "Assets/Data/Garage";

    [MenuItem("PitCrew/Garage/Setup F1Parts + Catalog")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder(Dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            AssetDatabase.CreateFolder("Assets/Data", "Garage");
        }

        // D-080~082, D-088 가격·flavor 표
        var data = new (F1PartId id, string name, int cost, string flavor, string mech)[]
        {
            (F1PartId.Engine,     "엔진",     1000,  "엔진이 돌아왔다. 시동 소리가 살아있다.",       "최고속 +20%, 가속 +30%"),
            (F1PartId.Gearbox,    "변속기",   4000,  "변속기. 수동 변속이 가능해졌다.",            "K=다운시프트 / L=업시프트 수동 모드 unlock"),
            (F1PartId.Tires,      "타이어",   4000,  "타이어 4종 사용 가능. 잡 출발 전 선택.",       "드라이/웨트/소프트/하드 선택 + 영구 마모"),
            (F1PartId.Suspension, "서스펜션", 8000,  "서스 복원. 점프대·험지가 도시에 활성.",        "외곽 점프 구간 통과 가능 (heightMask)"),
            (F1PartId.Aero,       "에어로",   12000, "에어로 윙. 부스트가 가능하다.",              "Space = 부스트 (+50% 가속 2초, 재충전 5초)"),
            (F1PartId.Paint,      "도색",     25000, "도색 완료. 그랑프리 복귀 준비가 끝났다.",      "외형 변화 + 엔드게임 서사 트리거"),
        };

        var catalog = ScriptableObject.CreateInstance<F1RestoreCatalog>();
        AssetDatabase.CreateAsset(catalog, $"{Dir}/F1RestoreCatalog.asset");

        foreach (var d in data)
        {
            var part = ScriptableObject.CreateInstance<F1Part>();
            part.id = d.id;
            part.displayName = d.name;
            part.restoreCost = d.cost;
            part.flavorLine = d.flavor;
            part.mechanicDescription = d.mech;
            part.cutsceneSeconds = 2.5f;
            AssetDatabase.CreateAsset(part, $"{Dir}/F1Part_{d.id}.asset");
            catalog.parts.Add(part);
        }

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = catalog;
        Debug.Log($"[SetupGarageSystem] 6 F1Part + Catalog 생성 완료: {Dir}");
    }
}
#endif
```

- [ ] **Step 2: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Then: `unity-cli console --type error --since 30s`
Expected: 0건.

- [ ] **Step 3: Editor 메뉴 실행**

Run: `unity-cli menu "PitCrew/Garage/Setup F1Parts + Catalog"`
Then: `unity-cli console --type log --since 10s | grep SetupGarageSystem`
Expected: `[SetupGarageSystem] 6 F1Part + Catalog 생성 완료`

- [ ] **Step 4: 자산 생성 검증**

Run: `unity-cli exec "var c = UnityEditor.AssetDatabase.LoadAssetAtPath<PitCrew.Garage.F1RestoreCatalog>(\"Assets/Data/Garage/F1RestoreCatalog.asset\"); return c != null && c.parts.Count == 6 ? \"OK \" + c.parts.Count : \"FAIL\";"`
Expected: `OK 6`

- [ ] **Step 5: Commit**

```
git add F1/Assets/Editor/_Recovery/SetupGarageSystem.cs F1/Assets/Data/Garage/
git commit -m "feat(garage): 6 F1Part assets + Catalog (Task 2)

엔진/변속기/타이어/서스/에어로/도색 6종, D-080~082 가격 표 그대로.
Editor 메뉴: PitCrew/Garage/Setup F1Parts + Catalog (idempotent).

Verified-By: self (unity-cli exec catalog.parts.Count == 6)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: F1RestoreSystem 런타임 (TryRestore + 이벤트, 효과는 placeholder)

**Files:**
- Create: `F1/Assets/Scripts/Garage/IF1RestoreSystem.cs`
- Create: `F1/Assets/Scripts/Garage/F1RestoreSystem.cs`
- Modify: `F1/Assets/Scripts/Core/Core.cs` — Services에 IF1RestoreSystem 추가

- [ ] **Step 1: IF1RestoreSystem 인터페이스**

```csharp
// F1/Assets/Scripts/Garage/IF1RestoreSystem.cs
using System;

namespace PitCrew.Garage
{
    public interface IF1RestoreSystem
    {
        bool IsRestored(F1PartId id);
        bool CanAfford(F1Part part);
        bool TryRestore(F1Part part);
        event Action<F1Part> OnRestored;
        F1RestoreCatalog Catalog { get; }
    }
}
```

- [ ] **Step 2: F1RestoreSystem 본체 (효과 ApplyEffect는 placeholder switch)**

```csharp
// F1/Assets/Scripts/Garage/F1RestoreSystem.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using PitCrew.Core;

namespace PitCrew.Garage
{
    public sealed class F1RestoreSystem : MonoBehaviour, IF1RestoreSystem
    {
        [SerializeField] private F1RestoreCatalog catalog;
        public F1RestoreCatalog Catalog => catalog;

        readonly HashSet<F1PartId> _restored = new HashSet<F1PartId>();

        public event Action<F1Part> OnRestored;

        public bool IsRestored(F1PartId id) => _restored.Contains(id);

        public bool CanAfford(F1Part part)
        {
            if (part == null) return false;
            var up = Services.Upgrade;
            return up != null && up.Wallet >= part.restoreCost;
        }

        public bool TryRestore(F1Part part)
        {
            if (part == null) return false;
            if (IsRestored(part.id)) return false;
            var up = Services.Upgrade;
            if (up == null || up.Wallet < part.restoreCost) return false;

            up.Spend(part.restoreCost);
            _restored.Add(part.id);
            ApplyEffect(part);
            OnRestored?.Invoke(part);
            Debug.Log($"[F1RestoreSystem] Restored {part.id} for ₩{part.restoreCost}. Wallet={up.Wallet}");
            return true;
        }

        // Task 4~8에서 채워짐. 일단 골조.
        void ApplyEffect(F1Part p)
        {
            switch (p.id)
            {
                case F1PartId.Engine:     break;
                case F1PartId.Gearbox:    break;
                case F1PartId.Tires:      break;
                case F1PartId.Suspension: break;
                case F1PartId.Aero:       break;
                case F1PartId.Paint:      break;
            }
        }
    }
}
```

- [ ] **Step 3: Services에 IF1RestoreSystem 등록 슬롯 추가**

Modify `F1/Assets/Scripts/Core/Core.cs`. 다른 인터페이스 등록 라인 옆(L130 부근, `IUpgradeSystem Upgrade` 다음 라인)에 추가:

```csharp
        public static PitCrew.Garage.IF1RestoreSystem Restore { get; internal set; }
```

`Reset()` 안에 다른 reset 옆에 추가:

```csharp
            Restore = null;
```

- [ ] **Step 4: GameContext에 F1RestoreSystem wiring**

Modify `F1/Assets/Scripts/Core/GameContext.cs`:

인스펙터 필드 (Line ~32 부근, 기존 packingScreen 다음에):
```csharp
        [SerializeField] private PitCrew.Garage.F1RestoreSystem restoreSystem;
```

Awake() 안 Services 할당 부분 (Line ~58 부근):
```csharp
            Services.Restore = restoreSystem;
```

`AutoFindMissing()` 안 (다른 FindObjectOfType들 옆):
```csharp
            if (restoreSystem == null) restoreSystem = FindFirstObjectByType<PitCrew.Garage.F1RestoreSystem>();
```

- [ ] **Step 5: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Then: `unity-cli console --type error --since 30s`
Expected: 0건.

- [ ] **Step 6: Commit**

```
git add F1/Assets/Scripts/Garage/IF1RestoreSystem.cs F1/Assets/Scripts/Garage/F1RestoreSystem.cs F1/Assets/Scripts/Core/Core.cs F1/Assets/Scripts/Core/GameContext.cs
git commit -m "feat(garage): F1RestoreSystem + Services 등록 (Task 3)

TryRestore + OnRestored 이벤트 + 멱등 HashSet. ApplyEffect placeholder
(Task 4~8에서 부품별 구현). Services.Restore + GameContext wiring 골조.

Verified-By: self (compile clean)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: 엔진 효과 — 최고속 +20%, 가속 +30% (D-089)

**Files:**
- Modify: `F1/Assets/Scripts/ArcadeCarController.cs` — 곱 적용 진입점 + 외부 셋터
- Modify: `F1/Assets/Scripts/Garage/F1RestoreSystem.cs` — Engine case 채움

- [ ] **Step 1: ArcadeCarController에 엔진 부스트 필드/적용 추가**

Modify `F1/Assets/Scripts/ArcadeCarController.cs`. `maxSpeedKmh` 필드 근처(L12)에 추가:

```csharp
        [Header("Engine Restore (D-089)")]
        [SerializeField] private float engineMaxSpeedMul = 1.0f;   // 1.20 after restore
        [SerializeField] private float engineAccelMul = 1.0f;       // 1.30 after restore

        public float EngineMaxSpeedMul => engineMaxSpeedMul;
        public float EngineAccelMul => engineAccelMul;

        public void ApplyEngineRestore(float maxSpeedMul = 1.20f, float accelMul = 1.30f)
        {
            engineMaxSpeedMul = maxSpeedMul;
            engineAccelMul = accelMul;
            Debug.Log($"[ArcadeCarController] Engine restored: maxSpeedMul={maxSpeedMul:F2} accelMul={accelMul:F2}");
        }
```

`EffectiveMaxSpeedMps` 프로퍼티(L104~105)에 엔진 배수 합치기. 기존:
```csharp
        public float EffectiveMaxSpeedMps => maxSpeedKmh / 3.6f * (Services.Tires?.SpeedMultiplier ?? 1f);
```
→ 변경:
```csharp
        public float EffectiveMaxSpeedMps => maxSpeedKmh / 3.6f * (Services.Tires?.SpeedMultiplier ?? 1f) * engineMaxSpeedMul;
```

가속 적용 부분 — `ApplyThrottle` 안 throttle 곱하는 라인 찾아 `engineAccelMul` 곱하기. accelTime이 작아질수록 가속이 빨라지므로 `accelTime / engineAccelMul`로 effective accelTime을 계산. ApplyThrottle 함수에서 accelTime을 직접 쓰는 곳을 찾아 모두 `effectiveAccelTime` 로컬 변수로 대체:

```csharp
        // ApplyThrottle 최상단
        float effectiveAccelTime = accelTime / Mathf.Max(0.01f, engineAccelMul);
        // 이후 accelTime 참조 → effectiveAccelTime 으로 교체
```

(실제 라인 수정은 파일 읽고 정확히 — accelTime 사용처가 1~2곳일 가능성 큼)

- [ ] **Step 2: F1RestoreSystem.ApplyEffect Engine case 구현**

```csharp
                case F1PartId.Engine:
                    Services.Car?.GetComponent<ArcadeCarController>()?.ApplyEngineRestore(1.20f, 1.30f);
                    break;
```

(주의: `Services.Car`는 `IArcadeCarController`. `ApplyEngineRestore`는 구체 클래스 메소드라 `as ArcadeCarController` 캐스팅. 인터페이스에 추가하는 게 깔끔하지만 잠재 부채로 미룬 캐스팅 5개 중 1개 늘림.)

대신 IArcadeCarController에 메소드 추가:

`F1/Assets/Scripts/ArcadeCarController.cs` (또는 별도 인터페이스 정의 파일 — 기존 위치 확인 후) 인터페이스에:

```csharp
        public interface IArcadeCarController
        {
            // ... 기존 ...
            void ApplyEngineRestore(float maxSpeedMul, float accelMul);
        }
```

F1RestoreSystem:
```csharp
                case F1PartId.Engine:
                    Services.Car?.ApplyEngineRestore(1.20f, 1.30f);
                    break;
```

- [ ] **Step 3: 컴파일 + 런타임 검증**

Run: `unity-cli editor refresh --compile`
Then: `unity-cli console --type error --since 30s`
Expected: 0건.

Play 시작 후 reflection 검증:
```bash
unity-cli editor play --wait
unity-cli exec "var c = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var before = c.EffectiveMaxSpeedMps; var r = UnityEngine.Object.FindFirstObjectByType<PitCrew.Garage.F1RestoreSystem>(); var p = r.Catalog.Find(PitCrew.Garage.F1PartId.Engine); PitCrew.Core.Services.Upgrade.AddReward(2000); r.TryRestore(p); var after = c.EffectiveMaxSpeedMps; return \"before=\" + before.ToString(\"F2\") + \" after=\" + after.ToString(\"F2\") + \" ratio=\" + (after/before).ToString(\"F3\");"
unity-cli editor stop
```
Expected: `ratio≈1.200`

- [ ] **Step 4: Commit**

```
git add F1/Assets/Scripts/ArcadeCarController.cs F1/Assets/Scripts/Garage/F1RestoreSystem.cs
git commit -m "feat(garage): 엔진 복원 효과 — 최고속 +20% 가속 +30% (Task 4 / D-089)

ArcadeCarController.engineMaxSpeedMul/engineAccelMul 도입. IArcadeCarController에
ApplyEngineRestore 추가. F1RestoreSystem Engine case 채움.

Verified-By: self (unity-cli exec ratio==1.200)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 5: 변속기 효과 — 수동 모드 + K/L 키바인딩 (D-089)

**Files:**
- Modify: `F1/Assets/Scripts/GearSystem.cs` — ManualUnlocked + K/L 입력 + 수동 분기
- Modify: `F1/Assets/Scripts/Garage/F1RestoreSystem.cs` — Gearbox case

- [ ] **Step 1: GearSystem 수동 모드 + 키 입력 추가**

Modify `F1/Assets/Scripts/GearSystem.cs`. 클래스 필드 영역에:

```csharp
        [Header("Manual Mode (D-089)")]
        [SerializeField] private bool manualUnlocked = false;
        [SerializeField] private int minGear = -1;
        [SerializeField] private int maxGear = 5;

        public bool ManualUnlocked => manualUnlocked;
        public bool IsManualMode => manualUnlocked;  // unlock 시 자동으로 수동 모드 (toggle 미지원, 데모 단순)

        public void SetManualUnlocked(bool v)
        {
            manualUnlocked = v;
            Debug.Log($"[GearSystem] ManualUnlocked={v}");
        }
```

기존 `FixedUpdate`(L69 부근, ComputeGear 호출처)를 다음과 같이 변경:

```csharp
        void FixedUpdate()
        {
            if (manualUnlocked) return;     // 수동 모드에서 자동 계산 X
            int prev = currentGear;
            int next = ComputeGear();
            if (next != prev) { currentGear = next; OnShift?.Invoke(prev, next); }
        }

        void Update()
        {
            if (!manualUnlocked) return;
            if (UnityEngine.InputSystem.Keyboard.current == null) return;
            if (UnityEngine.InputSystem.Keyboard.current.kKey.wasPressedThisFrame)
            {
                int prev = currentGear;
                currentGear = Mathf.Max(minGear, currentGear - 1);
                if (currentGear != prev) OnShift?.Invoke(prev, currentGear);
            }
            if (UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame)
            {
                int prev = currentGear;
                currentGear = Mathf.Min(maxGear, currentGear + 1);
                if (currentGear != prev) OnShift?.Invoke(prev, currentGear);
            }
        }
```

(currentGear 필드명이 정확히 무엇인지 파일 읽고 매칭. `_currentGear` 일 수도 있음.)

IGearSystem 인터페이스에 추가:
```csharp
        bool ManualUnlocked { get; }
        void SetManualUnlocked(bool v);
```

- [ ] **Step 2: F1RestoreSystem Gearbox case**

```csharp
                case F1PartId.Gearbox:
                    Services.Gears?.SetManualUnlocked(true);
                    break;
```

- [ ] **Step 3: 컴파일 + 런타임 검증**

Run: `unity-cli editor refresh --compile`
Then: `unity-cli console --type error --since 30s`
Expected: 0건.

Play 후:
```bash
unity-cli editor play --wait
unity-cli exec "PitCrew.Core.Services.Upgrade.AddReward(5000); var r = PitCrew.Core.Services.Restore; var p = r.Catalog.Find(PitCrew.Garage.F1PartId.Gearbox); r.TryRestore(p); return PitCrew.Core.Services.Gears.ManualUnlocked.ToString();"
unity-cli editor stop
```
Expected: `True`

- [ ] **Step 4: Commit**

```
git add F1/Assets/Scripts/GearSystem.cs F1/Assets/Scripts/Garage/F1RestoreSystem.cs
git commit -m "feat(garage): 변속기 복원 — 수동 모드 K/L 키바인딩 (Task 5 / D-089)

GearSystem.ManualUnlocked 토글. K=다운시프트 / L=업시프트.
unlock 시 자동→수동 전환 (toggle 데모 단순화).

Verified-By: self (unity-cli exec Gears.ManualUnlocked==True)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 6: 에어로 효과 — BoostSystem (Space 게이지 D-089)

**Files:**
- Create: `F1/Assets/Scripts/Garage/BoostSystem.cs`
- Modify: `F1/Assets/Scripts/Core/Core.cs` — `IBoostSystem Boost` 추가
- Modify: `F1/Assets/Scripts/Core/GameContext.cs` — wiring
- Modify: `F1/Assets/Scripts/ArcadeCarController.cs` — Boost 곱 적용
- Modify: `F1/Assets/Scripts/Garage/F1RestoreSystem.cs` — Aero case

- [ ] **Step 1: BoostSystem 신규**

```csharp
// F1/Assets/Scripts/Garage/BoostSystem.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using PitCrew.Core;

namespace PitCrew.Garage
{
    public interface IBoostSystem
    {
        bool IsUnlocked { get; }
        bool IsActive { get; }
        float Gauge01 { get; }
        void SetUnlocked(bool v);
        event Action<bool> OnActiveChanged;
    }

    public sealed class BoostSystem : MonoBehaviour, IBoostSystem
    {
        [SerializeField] private float boostDuration = 2.0f;   // 2초 분사
        [SerializeField] private float rechargeTime = 5.0f;     // 0→1 충전 5초
        [SerializeField] private float speedMul = 1.50f;        // +50%
        [SerializeField] private bool unlocked = false;

        float _gauge = 1f;
        float _activeUntil = -1f;
        bool _active;

        public bool IsUnlocked => unlocked;
        public bool IsActive => _active;
        public float Gauge01 => _gauge;
        public float SpeedMul => speedMul;

        public event Action<bool> OnActiveChanged;

        public void SetUnlocked(bool v)
        {
            unlocked = v;
            Debug.Log($"[BoostSystem] Unlocked={v}");
        }

        void Update()
        {
            if (!unlocked) return;
            // RunStage 체크: Driving일 때만
            var rs = Services.Run?.CurrentStage ?? PitCrew.Order.RunStage.Board;
            if (rs != PitCrew.Order.RunStage.Driving) { SetActive(false); return; }

            // 활성 종료
            if (_active && Time.time >= _activeUntil)
            {
                SetActive(false);
            }

            // 입력
            if (!_active && _gauge >= 1f && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _gauge = 0f;
                _activeUntil = Time.time + boostDuration;
                SetActive(true);
            }

            // 재충전
            if (!_active && _gauge < 1f)
            {
                _gauge = Mathf.Min(1f, _gauge + Time.deltaTime / rechargeTime);
            }
        }

        void SetActive(bool v)
        {
            if (_active == v) return;
            _active = v;
            OnActiveChanged?.Invoke(v);
        }
    }
}
```

- [ ] **Step 2: Core.cs에 Boost 슬롯**

```csharp
        public static PitCrew.Garage.IBoostSystem Boost { get; internal set; }
```
Reset()에 `Boost = null;` 추가.

- [ ] **Step 3: GameContext에 BoostSystem wiring** (같은 패턴, Task 3 참고)

```csharp
        [SerializeField] private PitCrew.Garage.BoostSystem boostSystem;
// Awake:
            Services.Boost = boostSystem;
// AutoFindMissing:
            if (boostSystem == null) boostSystem = FindFirstObjectByType<PitCrew.Garage.BoostSystem>();
```

- [ ] **Step 4: ArcadeCarController에 Boost 곱 적용**

`EffectiveMaxSpeedMps` 변경:
```csharp
        public float EffectiveMaxSpeedMps =>
            maxSpeedKmh / 3.6f
            * (Services.Tires?.SpeedMultiplier ?? 1f)
            * engineMaxSpeedMul
            * ((Services.Boost?.IsActive ?? false) ? 1.50f : 1f);
```

가속도 마찬가지. `ApplyThrottle` 안의 effectiveAccelTime 계산에 Boost 적용:
```csharp
        float boostMul = (Services.Boost?.IsActive ?? false) ? 1.50f : 1f;
        float effectiveAccelTime = accelTime / Mathf.Max(0.01f, engineAccelMul * boostMul);
```

- [ ] **Step 5: F1RestoreSystem Aero case**

```csharp
                case F1PartId.Aero:
                    Services.Boost?.SetUnlocked(true);
                    break;
```

- [ ] **Step 6: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Then: `unity-cli console --type error --since 30s`
Expected: 0건.

- [ ] **Step 7: Commit**

```
git add F1/Assets/Scripts/Garage/BoostSystem.cs F1/Assets/Scripts/Core/Core.cs F1/Assets/Scripts/Core/GameContext.cs F1/Assets/Scripts/ArcadeCarController.cs F1/Assets/Scripts/Garage/F1RestoreSystem.cs
git commit -m "feat(garage): 에어로 복원 — BoostSystem Space 게이지 (Task 6 / D-089)

Space=부스트(+50% 2초, 재충전 5초). Driving stage에서만 작동.
ArcadeCarController.EffectiveMaxSpeedMps + 가속에 Boost 곱 합산.

Verified-By: self (compile clean, runtime는 Task 12 manual play에서)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: 타이어 효과 — 4종 선택 + 영구 마모 + 펑크 (D-091)

**Files:**
- Create: `F1/Assets/Scripts/Garage/TireWearStore.cs` — 4 종 영구 마모 게이지 + 펑크 발동
- Modify: `F1/Assets/Scripts/Tires/TireSystem.cs` — SelectionUnlocked 토글
- Modify: `F1/Assets/Scripts/Tires/TireTypePresets.cs` — 화물 손상 곱 추가
- Modify: `F1/Assets/Scripts/Cargo/CargoBox.cs` — 타이어 손상 곱 합산
- Modify: `F1/Assets/Scripts/Core/Core.cs`, `GameContext.cs` — wiring
- Modify: `F1/Assets/Scripts/Order/RunController.cs` — 펑크 발동 시 EndRun(success=false)
- Modify: `F1/Assets/Scripts/Garage/F1RestoreSystem.cs` — Tires case

- [ ] **Step 1: TireWearStore 신규**

```csharp
// F1/Assets/Scripts/Garage/TireWearStore.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using PitCrew.Core;

namespace PitCrew.Garage
{
    public interface ITireWearStore
    {
        float GetWear01(PitCrew.Tires.TireType type);  // 1.0 = 신품
        bool IsPunctureRisk(PitCrew.Tires.TireType type);
        int RepairCost(PitCrew.Tires.TireType type);
        bool TryRepair(PitCrew.Tires.TireType type);
        event Action OnWearChanged;
        event Action OnPunctured;  // 잡 도중 펑크 발동
    }

    public sealed class TireWearStore : MonoBehaviour, ITireWearStore
    {
        // 풀수리 가격 (D-091)
        static readonly Dictionary<PitCrew.Tires.TireType, int> FullRepairCost = new Dictionary<PitCrew.Tires.TireType, int>
        {
            { PitCrew.Tires.TireType.Dry,   500 },
            { PitCrew.Tires.TireType.Wet,   500 },
            { PitCrew.Tires.TireType.Soft,  800 },
            { PitCrew.Tires.TireType.Hard,  600 },
        };

        readonly Dictionary<PitCrew.Tires.TireType, float> _wear = new Dictionary<PitCrew.Tires.TireType, float>();

        public event Action OnWearChanged;
        public event Action OnPunctured;

        void Awake()
        {
            foreach (PitCrew.Tires.TireType t in Enum.GetValues(typeof(PitCrew.Tires.TireType)))
                _wear[t] = 1f;
        }

        public float GetWear01(PitCrew.Tires.TireType type) => _wear.TryGetValue(type, out var v) ? v : 1f;

        public bool IsPunctureRisk(PitCrew.Tires.TireType type) => GetWear01(type) <= 0.30f;

        public int RepairCost(PitCrew.Tires.TireType type)
        {
            float remaining = 1f - GetWear01(type);
            return Mathf.CeilToInt(FullRepairCost[type] * remaining);
        }

        public bool TryRepair(PitCrew.Tires.TireType type)
        {
            int cost = RepairCost(type);
            if (cost <= 0) return false;
            var up = Services.Upgrade;
            if (up == null || up.Wallet < cost) return false;
            up.Spend(cost);
            _wear[type] = 1f;
            OnWearChanged?.Invoke();
            return true;
        }

        // 잡 한 건 끝나면 사용한 타이어 마모 ↓ (RunController에서 호출)
        public void RecordJobUsage(PitCrew.Tires.TireType used, float distanceMeters, float peakG)
        {
            // 거리 0.5× + G 0.5× 정규화 — D-091: 잡당 -10~20%
            float wearDelta = Mathf.Clamp(distanceMeters / 500f * 0.10f + peakG / 5f * 0.10f, 0.05f, 0.25f);
            _wear[used] = Mathf.Max(0f, _wear[used] - wearDelta);
            OnWearChanged?.Invoke();
            Debug.Log($"[TireWearStore] {used} wear: -{wearDelta:F2} → {_wear[used]:F2}");
        }

        // 잡 도중 펑크 확률 평가 (RunController가 매 초 호출)
        public bool RollPuncture(PitCrew.Tires.TireType current)
        {
            float w = GetWear01(current);
            if (w > 0.30f) return false;
            // 30%→5%/초, 0%→20%/초 선형
            float rate = Mathf.Lerp(0.20f, 0.05f, w / 0.30f);
            if (UnityEngine.Random.value < rate)
            {
                Debug.Log($"[TireWearStore] PUNCTURE on {current} (wear={w:F2}, rate={rate:F2})");
                OnPunctured?.Invoke();
                return true;
            }
            return false;
        }
    }
}
```

- [ ] **Step 2: TireSystem에 SelectionUnlocked + 외부 곱 노출**

`F1/Assets/Scripts/Tires/TireSystem.cs`에:

```csharp
        [Header("Selection Unlock (D-091)")]
        [SerializeField] private bool selectionUnlocked = false;
        public bool SelectionUnlocked => selectionUnlocked;
        public void SetSelectionUnlocked(bool v) { selectionUnlocked = v; }
        public float CargoDamageMul => PitCrew.Tires.TireTypePresets.GetCargoDamageMul(Current);
```

- [ ] **Step 3: TireTypePresets에 cargoDamageMul 매핑 추가**

`F1/Assets/Scripts/Tires/TireTypePresets.cs`에 정적 메소드:

```csharp
        public static float GetCargoDamageMul(TireType t)
        {
            switch (t)
            {
                case TireType.Dry:  return 1.0f;
                case TireType.Wet:  return 0.8f;
                case TireType.Soft: return 1.5f;
                case TireType.Hard: return 0.5f;
                default: return 1.0f;
            }
        }
```

(TireType enum 값 4개 확인. 이미 있는지 검증 필요 — `Dry/Wet/Soft/Hard` 가 아니면 실존 이름으로 교체.)

- [ ] **Step 4: CargoBox.ApplyDamage에 타이어 손상 곱 적용**

`F1/Assets/Scripts/Cargo/CargoBox.cs`의 `ApplyDamage(float dmg)`(L282) 진입부에서:

```csharp
        public void ApplyDamage(float dmg)
        {
            if (Time.time < graceUntil) return;
            float tireMul = Services.Tires?.CargoDamageMul ?? 1f;
            dmg *= tireMul;
            // ... 기존 코드
        }
```

- [ ] **Step 5: RunController 펑크 처리 + 잡 종료 시 마모 기록**

`BeginRun(...)` 안에 마지막 위치에 펑크 구독:
```csharp
            if (Services.TireWear is TireWearStore tw)
                tw.OnPunctured += HandlePuncture;
```

EndRun 직전 정리 + 마모 기록:
```csharp
        void HandlePuncture()
        {
            Debug.LogWarning("[RunController] PUNCTURE → EndRun(success=false)");
            EndRun(success: false);
        }
```

EndRun 안(보상 계산 직전쯤)에 마모 기록:
```csharp
            if (Services.TireWear is TireWearStore tw2 && Services.Tires != null)
            {
                tw2.RecordJobUsage(Services.Tires.Current, _distanceTraveledMeters, Services.Cargo?.PeakLongG ?? 0f);
                tw2.OnPunctured -= HandlePuncture;
            }
```

(`_distanceTraveledMeters` 필드가 없으면 `Elapsed * MaxSpeedMps` 근사로 대체 또는 새 필드 추가 — `RunController.Update`에서 `Time.deltaTime * Services.Car.CurrentSpeedMps` 누적.)

Update() 안 매 초 펑크 roll:
```csharp
        float _punctureRollTimer;
        void RollPunctureTick()
        {
            if (CurrentStage != RunStage.Driving) return;
            _punctureRollTimer += Time.deltaTime;
            if (_punctureRollTimer < 1f) return;
            _punctureRollTimer = 0f;
            (Services.TireWear as TireWearStore)?.RollPuncture(Services.Tires?.Current ?? PitCrew.Tires.TireType.Dry);
        }
```
Update()에서 호출.

- [ ] **Step 6: Services.TireWear + GameContext wiring**

Core.cs:
```csharp
        public static PitCrew.Garage.ITireWearStore TireWear { get; internal set; }
```
Reset()에 `TireWear = null;` 추가.

GameContext:
```csharp
        [SerializeField] private PitCrew.Garage.TireWearStore tireWearStore;
// Awake:
            Services.TireWear = tireWearStore;
// AutoFindMissing:
            if (tireWearStore == null) tireWearStore = FindFirstObjectByType<PitCrew.Garage.TireWearStore>();
```

- [ ] **Step 7: F1RestoreSystem Tires case**

```csharp
                case F1PartId.Tires:
                    if (Services.Tires is PitCrew.Tires.TireSystem ts) ts.SetSelectionUnlocked(true);
                    break;
```

(또는 ITireSystem에 `SetSelectionUnlocked` 추가하는 게 깔끔.)

- [ ] **Step 8: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Then: `unity-cli console --type error --since 30s`
Expected: 0건.

- [ ] **Step 9: Commit**

```
git add F1/Assets/Scripts/Garage/TireWearStore.cs F1/Assets/Scripts/Tires/ F1/Assets/Scripts/Cargo/CargoBox.cs F1/Assets/Scripts/Core/Core.cs F1/Assets/Scripts/Core/GameContext.cs F1/Assets/Scripts/Order/RunController.cs F1/Assets/Scripts/Garage/F1RestoreSystem.cs
git commit -m "feat(garage): 타이어 복원 — 4종 선택 + 영구 마모 + 펑크=잡실패 (Task 7 / D-091)

- TireWearStore: 4종 독립 마모 게이지, 풀수리 가격 (500/500/800/600)
- 마모 30% 이하 → 매 초 5~20% 펑크 확률
- 펑크 = 즉시 EndRun(success=false)
- TireSystem.CargoDamageMul (Dry 1.0 / Wet 0.8 / Soft 1.5 / Hard 0.5)
- CargoBox.ApplyDamage에 타이어 손상 곱 합산

Verified-By: self (compile clean, 마모 시뮬레이션은 Task 12에서)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: 도색·서스 효과 (외형 / 후속 marker)

**Files:**
- Modify: `F1/Assets/Scripts/Garage/F1RestoreSystem.cs`

- [ ] **Step 1: Paint case — 머신 도색 swap (간단 색 변경) + 서사 로그**

머신 GameObject 이름 확인 후:

```csharp
                case F1PartId.Paint:
                    var carRoot = Services.Car?.Transform.gameObject;
                    if (carRoot != null)
                    {
                        foreach (var mr in carRoot.GetComponentsInChildren<Renderer>())
                        {
                            if (mr.material != null)
                                mr.material.color = new Color(0.85f, 0.10f, 0.10f); // F1 레드
                        }
                    }
                    Debug.Log("[Garage] 도색 완료. 그랑프리 복귀 준비가 끝났다. (D-092: 모드 unlock은 후속)");
                    break;
```

- [ ] **Step 2: Suspension case — 일단 flag만 (R1 점프대 cell은 D-093 후속)**

```csharp
                case F1PartId.Suspension:
                    Debug.Log("[Garage] 서스 복원. 점프대·험지 활성 — heightMask cell은 맵 디자인 트랙에서 배치 예정 (D-093).");
                    break;
```

- [ ] **Step 3: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Expected: 0건.

- [ ] **Step 4: Commit**

```
git add F1/Assets/Scripts/Garage/F1RestoreSystem.cs
git commit -m "feat(garage): 도색·서스 효과 (Task 8)

- 도색: 머신 머티리얼 컬러 swap (F1 레드). 그랑프리 모드는 후속 (D-092).
- 서스: flag log만. 점프대 cell 배치는 맵 디자인 트랙 (D-093).

Verified-By: self (compile clean)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: GarageScreen UI — 머신 사이드뷰 + 6 카드 + 디테일 + 수리 (D-086~088)

**Files:**
- Create: `F1/Assets/Scripts/Garage/GarageScreen.cs` (Canvas, UGUI 자가 구성 — ResultPanel 패턴)
- Create: `F1/Assets/Scripts/Garage/GarageRestoreCutscene.cs`

- [ ] **Step 1: GarageRestoreCutscene 신규 — 2~3초 회색→색칠 페이드**

```csharp
// F1/Assets/Scripts/Garage/GarageRestoreCutscene.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PitCrew.Garage
{
    public sealed class GarageRestoreCutscene : MonoBehaviour
    {
        Canvas _canvas;
        CanvasGroup _cg;
        Image _bg;
        TMP_Text _flavor;
        Image _sprite;

        public bool IsPlaying { get; private set; }

        void EnsureBuilt()
        {
            if (_canvas != null) return;
            var go = new GameObject("GarageCutsceneCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();
            _cg = go.AddComponent<CanvasGroup>();

            var bgGo = new GameObject("Dim", typeof(RectTransform));
            bgGo.transform.SetParent(go.transform, false);
            _bg = bgGo.AddComponent<Image>();
            _bg.color = new Color(0,0,0,0.55f);
            var rt = (RectTransform)bgGo.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var flavorGo = new GameObject("Flavor", typeof(RectTransform));
            flavorGo.transform.SetParent(go.transform, false);
            _flavor = flavorGo.AddComponent<TextMeshProUGUI>();
            _flavor.alignment = TextAlignmentOptions.Center;
            _flavor.fontSize = 48;
            _flavor.color = Color.white;
            var frt = (RectTransform)flavorGo.transform;
            frt.anchorMin = new Vector2(0.1f, 0.4f); frt.anchorMax = new Vector2(0.9f, 0.6f);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

            _canvas.enabled = false;
        }

        public IEnumerator Play(F1Part part)
        {
            EnsureBuilt();
            IsPlaying = true;
            _canvas.enabled = true;
            _flavor.text = part.flavorLine;

            float dur = part.cutsceneSeconds;
            float t = 0f;
            // 페이드 인 (0.3s)
            while (t < 0.3f) { t += Time.unscaledDeltaTime; _cg.alpha = Mathf.Lerp(0f, 1f, t/0.3f); yield return null; }
            _cg.alpha = 1f;
            // 유지
            yield return new WaitForSecondsRealtime(dur - 0.6f);
            // 페이드 아웃
            t = 0f;
            while (t < 0.3f) { t += Time.unscaledDeltaTime; _cg.alpha = Mathf.Lerp(1f, 0f, t/0.3f); yield return null; }
            _cg.alpha = 0f;
            _canvas.enabled = false;
            IsPlaying = false;
        }
    }
}
```

- [ ] **Step 2: GarageScreen 본체**

```csharp
// F1/Assets/Scripts/Garage/GarageScreen.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PitCrew.Core;

namespace PitCrew.Garage
{
    public sealed class GarageScreen : MonoBehaviour
    {
        [SerializeField] private GarageRestoreCutscene cutscene;

        Canvas _canvas;
        CanvasGroup _cg;
        TMP_Text _walletText;
        Transform _cardRow;
        TMP_Text _detailTitle, _detailDesc, _detailCost;
        Button _restoreBtn, _closeBtn;
        F1Part _selected;
        readonly List<GameObject> _cardGos = new List<GameObject>();

        public bool IsOpen { get; private set; }

        void Awake()
        {
            EnsureBuilt();
            _canvas.enabled = false;
            if (Services.Upgrade != null) Services.Upgrade.OnWalletChanged += _ => RefreshWallet();
            if (Services.Restore != null) Services.Restore.OnRestored += _ => RefreshCards();
        }

        void EnsureBuilt()
        {
            if (_canvas != null) return;
            var go = new GameObject("GarageCanvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 4000;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.AddComponent<GraphicRaycaster>();
            _cg = go.AddComponent<CanvasGroup>();

            // BG dim
            var bg = new GameObject("BG", typeof(RectTransform)).AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            bg.color = new Color(0.05f, 0.05f, 0.07f, 0.95f);
            var bgrt = bg.rectTransform; bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one; bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;

            // Wallet top-left
            var wallet = new GameObject("Wallet", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            wallet.transform.SetParent(go.transform, false);
            wallet.fontSize = 36; wallet.color = Color.white; wallet.alignment = TextAlignmentOptions.TopLeft;
            var wrt = wallet.rectTransform; wrt.anchorMin = new Vector2(0.02f,0.92f); wrt.anchorMax = new Vector2(0.4f,0.98f); wrt.offsetMin = Vector2.zero; wrt.offsetMax = Vector2.zero;
            _walletText = wallet;

            // Close button top-right
            var closeGo = new GameObject("Close", typeof(RectTransform));
            closeGo.transform.SetParent(go.transform, false);
            var closeImg = closeGo.AddComponent<Image>(); closeImg.color = new Color(0.2f,0.2f,0.25f);
            _closeBtn = closeGo.AddComponent<Button>();
            var closeRt = closeImg.rectTransform; closeRt.anchorMin = new Vector2(0.92f,0.92f); closeRt.anchorMax = new Vector2(0.98f,0.98f); closeRt.offsetMin = Vector2.zero; closeRt.offsetMax = Vector2.zero;
            var closeTxt = new GameObject("Txt", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            closeTxt.transform.SetParent(closeGo.transform, false);
            closeTxt.text = "닫기"; closeTxt.alignment = TextAlignmentOptions.Center; closeTxt.color = Color.white; closeTxt.fontSize = 28;
            var ctxRt = closeTxt.rectTransform; ctxRt.anchorMin = Vector2.zero; ctxRt.anchorMax = Vector2.one; ctxRt.offsetMin = Vector2.zero; ctxRt.offsetMax = Vector2.zero;
            _closeBtn.onClick.AddListener(Hide);

            // 머신 자리 (placeholder 박스 — D-086 사이드뷰. 실제 도색 swap은 Task 8과 별개)
            var carPanelGo = new GameObject("CarPanel", typeof(RectTransform));
            carPanelGo.transform.SetParent(go.transform, false);
            var carImg = carPanelGo.AddComponent<Image>(); carImg.color = new Color(0.12f,0.12f,0.15f);
            var crt = carImg.rectTransform; crt.anchorMin = new Vector2(0.15f,0.35f); crt.anchorMax = new Vector2(0.85f,0.85f); crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            // 라벨 placeholder
            var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(carPanelGo.transform, false);
            label.text = "[ F1 머신 사이드뷰 ]"; label.alignment = TextAlignmentOptions.Center; label.color = new Color(0.5f,0.5f,0.55f); label.fontSize = 32;
            var lrt = label.rectTransform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            // 디테일 패널 (머신 위)
            var detailGo = new GameObject("Detail", typeof(RectTransform));
            detailGo.transform.SetParent(go.transform, false);
            var detailImg = detailGo.AddComponent<Image>(); detailImg.color = new Color(0.08f,0.08f,0.10f,0.9f);
            var drt = detailImg.rectTransform; drt.anchorMin = new Vector2(0.20f,0.85f); drt.anchorMax = new Vector2(0.80f,0.94f); drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;

            _detailTitle = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _detailTitle.transform.SetParent(detailGo.transform, false);
            _detailTitle.fontSize = 28; _detailTitle.color = Color.white; _detailTitle.alignment = TextAlignmentOptions.TopLeft;
            var dtrt = _detailTitle.rectTransform; dtrt.anchorMin = new Vector2(0.02f, 0.5f); dtrt.anchorMax = new Vector2(0.6f, 1f); dtrt.offsetMin = Vector2.zero; dtrt.offsetMax = Vector2.zero;

            _detailDesc = new GameObject("Desc", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _detailDesc.transform.SetParent(detailGo.transform, false);
            _detailDesc.fontSize = 20; _detailDesc.color = new Color(0.8f,0.8f,0.85f); _detailDesc.alignment = TextAlignmentOptions.TopLeft;
            var ddrt = _detailDesc.rectTransform; ddrt.anchorMin = new Vector2(0.02f, 0f); ddrt.anchorMax = new Vector2(0.6f, 0.5f); ddrt.offsetMin = Vector2.zero; ddrt.offsetMax = Vector2.zero;

            _detailCost = new GameObject("Cost", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _detailCost.transform.SetParent(detailGo.transform, false);
            _detailCost.fontSize = 24; _detailCost.color = Color.yellow; _detailCost.alignment = TextAlignmentOptions.TopRight;
            var dcrt = _detailCost.rectTransform; dcrt.anchorMin = new Vector2(0.6f, 0.5f); dcrt.anchorMax = new Vector2(0.78f, 1f); dcrt.offsetMin = Vector2.zero; dcrt.offsetMax = Vector2.zero;

            var restoreGo = new GameObject("Restore", typeof(RectTransform));
            restoreGo.transform.SetParent(detailGo.transform, false);
            var restoreImg = restoreGo.AddComponent<Image>(); restoreImg.color = new Color(0.30f,0.55f,0.30f);
            _restoreBtn = restoreGo.AddComponent<Button>();
            var rbrt = restoreImg.rectTransform; rbrt.anchorMin = new Vector2(0.80f, 0.10f); rbrt.anchorMax = new Vector2(0.98f, 0.90f); rbrt.offsetMin = Vector2.zero; rbrt.offsetMax = Vector2.zero;
            var rbTxt = new GameObject("Txt", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            rbTxt.transform.SetParent(restoreGo.transform, false);
            rbTxt.text = "복원"; rbTxt.alignment = TextAlignmentOptions.Center; rbTxt.color = Color.white; rbTxt.fontSize = 28;
            var rbtRt = rbTxt.rectTransform; rbtRt.anchorMin = Vector2.zero; rbtRt.anchorMax = Vector2.one; rbtRt.offsetMin = Vector2.zero; rbtRt.offsetMax = Vector2.zero;
            _restoreBtn.onClick.AddListener(OnRestoreClicked);

            // 카드 row (하단)
            var rowGo = new GameObject("Cards", typeof(RectTransform));
            rowGo.transform.SetParent(go.transform, false);
            var rowRt = (RectTransform)rowGo.transform;
            rowRt.anchorMin = new Vector2(0.05f, 0.05f); rowRt.anchorMax = new Vector2(0.95f, 0.30f); rowRt.offsetMin = Vector2.zero; rowRt.offsetMax = Vector2.zero;
            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12; hlg.padding = new RectOffset(8,8,8,8);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            _cardRow = rowGo.transform;
        }

        public void Show()
        {
            EnsureBuilt();
            _canvas.enabled = true;
            IsOpen = true;
            RefreshWallet();
            RefreshCards();
            _selected = null;
            RefreshDetail();
        }

        public void Hide()
        {
            if (_canvas != null) _canvas.enabled = false;
            IsOpen = false;
        }

        void RefreshWallet()
        {
            if (_walletText == null) return;
            _walletText.text = $"지갑: ₩{(Services.Upgrade?.Wallet ?? 0)}";
        }

        void RefreshCards()
        {
            foreach (var go in _cardGos) if (go != null) Destroy(go);
            _cardGos.Clear();
            var catalog = Services.Restore?.Catalog;
            if (catalog == null) return;
            foreach (var part in catalog.parts)
            {
                if (part == null) continue;
                var cardGo = new GameObject($"Card_{part.id}", typeof(RectTransform));
                cardGo.transform.SetParent(_cardRow, false);
                var cardImg = cardGo.AddComponent<Image>();
                bool owned = Services.Restore.IsRestored(part.id);
                bool afford = Services.Restore.CanAfford(part);
                cardImg.color = owned ? new Color(0.30f,0.55f,0.30f)
                                : (afford ? new Color(0.18f,0.18f,0.22f)
                                          : new Color(0.12f,0.12f,0.14f));
                var btn = cardGo.AddComponent<Button>();
                var captured = part;
                btn.onClick.AddListener(() => { _selected = captured; RefreshDetail(); });

                var name = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                name.transform.SetParent(cardGo.transform, false);
                name.text = part.displayName;
                name.alignment = TextAlignmentOptions.Center;
                name.color = owned ? Color.white : (afford ? Color.white : new Color(0.6f,0.6f,0.62f));
                name.fontSize = 24;
                var nrt = name.rectTransform; nrt.anchorMin = new Vector2(0f, 0.55f); nrt.anchorMax = new Vector2(1f, 0.95f); nrt.offsetMin = Vector2.zero; nrt.offsetMax = Vector2.zero;

                var cost = new GameObject("Cost", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                cost.transform.SetParent(cardGo.transform, false);
                if (owned) { cost.text = "✓ 복원됨"; cost.color = Color.white; }
                else if (afford) { cost.text = $"₩{part.restoreCost}"; cost.color = Color.yellow; }
                else { int short_ = part.restoreCost - (Services.Upgrade?.Wallet ?? 0); cost.text = $"₩{short_} 부족"; cost.color = new Color(0.85f,0.4f,0.4f); }
                cost.alignment = TextAlignmentOptions.Center;
                cost.fontSize = 18;
                var crt = cost.rectTransform; crt.anchorMin = new Vector2(0f, 0.10f); crt.anchorMax = new Vector2(1f, 0.50f); crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

                _cardGos.Add(cardGo);
            }
        }

        void RefreshDetail()
        {
            if (_selected == null)
            {
                _detailTitle.text = "부품을 선택하세요";
                _detailDesc.text = "";
                _detailCost.text = "";
                _restoreBtn.interactable = false;
                return;
            }
            _detailTitle.text = _selected.displayName;
            _detailDesc.text = _selected.mechanicDescription;
            bool owned = Services.Restore.IsRestored(_selected.id);
            bool afford = Services.Restore.CanAfford(_selected);
            _detailCost.text = owned ? "✓" : $"₩{_selected.restoreCost}";
            _restoreBtn.interactable = !owned && afford;
        }

        void OnRestoreClicked()
        {
            if (_selected == null) return;
            var part = _selected;
            if (Services.Restore.TryRestore(part))
            {
                if (cutscene != null) StartCoroutine(cutscene.Play(part));
                RefreshCards();
                RefreshDetail();
                RefreshWallet();
            }
        }
    }
}
```

- [ ] **Step 3: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Then: `unity-cli console --type error --since 30s`
Expected: 0건.

- [ ] **Step 4: Commit**

```
git add F1/Assets/Scripts/Garage/GarageScreen.cs F1/Assets/Scripts/Garage/GarageRestoreCutscene.cs
git commit -m "feat(garage): GarageScreen UI + RestoreCutscene (Task 9 / D-086 D-087 D-088)

레이아웃: 머신 중앙 60% + 카드 6장 하단 (HorizontalLayoutGroup).
잠긴 카드 = 회색 + 'X원 부족'. 컷씬 = 2~3초 페이드 + flavorLine.
자가 구성 UGUI (ResultPanel 패턴).

Verified-By: self (compile clean, 시각 검증은 Task 11에서)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: ResultPanel — "차고로" Yes/No prompt (D-085)

**Files:**
- Modify: `F1/Assets/Scripts/Order/ResultPanel.cs`

- [ ] **Step 1: ResultPanel에 GarageScreen 진입 버튼 추가**

ResultPanel.cs의 `EnsureBuilt` 안 "다시" 버튼 옆에 "차고로" 버튼 추가. (정확한 라인은 파일 읽고 매칭 — L425 근처 "다시" 클릭 핸들러 옆.)

```csharp
        Button _garageBtn;
        // EnsureBuilt 안 "다시" 버튼 생성 직후:
        {
            var btnGo = new GameObject("GarageBtn", typeof(RectTransform));
            btnGo.transform.SetParent(/* 같은 부모 */, false);
            var img = btnGo.AddComponent<Image>(); img.color = new Color(0.35f, 0.40f, 0.65f);
            _garageBtn = btnGo.AddComponent<Button>();
            var txt = new GameObject("Txt", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            txt.transform.SetParent(btnGo.transform, false);
            txt.text = "차고로"; txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white; txt.fontSize = 28;
            var trt = txt.rectTransform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            _garageBtn.onClick.AddListener(OnGarageClicked);
        }
        // Refresh 안 마지막에:
        if (_garageBtn != null)
        {
            // 자금이 가장 싼 부품(엔진 1000₩)보다 많을 때만 활성
            int wallet = Services.Upgrade?.Wallet ?? 0;
            _garageBtn.interactable = wallet >= 1000 && Services.Restore != null && Services.Restore.Catalog != null;
        }

        // 새 메소드:
        void OnGarageClicked()
        {
            Hide();
            var garage = UnityEngine.Object.FindFirstObjectByType<PitCrew.Garage.GarageScreen>();
            if (garage != null) garage.Show();
            else Debug.LogWarning("[ResultPanel] GarageScreen 없음");
        }
```

(부모 / 위치는 ResultPanel 기존 버튼 row와 동일하게.)

- [ ] **Step 2: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Expected: 0건.

- [ ] **Step 3: Commit**

```
git add F1/Assets/Scripts/Order/ResultPanel.cs
git commit -m "feat(garage): ResultPanel '차고로' 버튼 (Task 10 / D-085)

정산 후 Yes/No prompt 대신 '다시'/'차고로' 두 버튼.
자금이 최저 부품(엔진 1000₩) 미만이면 disabled.

Verified-By: self (compile clean)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: ComplexTrack 씬 wiring — GameObject 4개 추가 + GameContext SerializeField 매핑

**Files:**
- Modify: `F1/Assets/ComplexTrack.unity` (Editor 스크립트 또는 unity-cli exec로)
- Create: `F1/Assets/Editor/_Recovery/WireGarageSceneObjects.cs`

- [ ] **Step 1: 자동 wiring Editor 스크립트**

```csharp
// F1/Assets/Editor/_Recovery/WireGarageSceneObjects.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PitCrew.Garage;

public static class WireGarageSceneObjects
{
    [MenuItem("PitCrew/Garage/Wire Scene Objects")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) { Debug.LogError("No active scene"); return; }

        // GameContext 찾기
        var ctx = Object.FindFirstObjectByType<PitCrew.Core.GameContext>();
        if (ctx == null) { Debug.LogError("GameContext 없음"); return; }

        // F1RestoreSystem GO
        var restoreGo = GameObject.Find("F1RestoreSystem") ?? new GameObject("F1RestoreSystem");
        var restoreSys = restoreGo.GetComponent<F1RestoreSystem>() ?? restoreGo.AddComponent<F1RestoreSystem>();
        var catalog = AssetDatabase.LoadAssetAtPath<F1RestoreCatalog>("Assets/Data/Garage/F1RestoreCatalog.asset");
        var so = new SerializedObject(restoreSys);
        so.FindProperty("catalog").objectReferenceValue = catalog;
        so.ApplyModifiedProperties();

        // BoostSystem GO
        var boostGo = GameObject.Find("BoostSystem") ?? new GameObject("BoostSystem");
        var boostSys = boostGo.GetComponent<BoostSystem>() ?? boostGo.AddComponent<BoostSystem>();

        // TireWearStore GO
        var twGo = GameObject.Find("TireWearStore") ?? new GameObject("TireWearStore");
        var twSys = twGo.GetComponent<TireWearStore>() ?? twGo.AddComponent<TireWearStore>();

        // GarageScreen GO (+ cutscene 자식)
        var garageGo = GameObject.Find("GarageScreen") ?? new GameObject("GarageScreen");
        var garage = garageGo.GetComponent<GarageScreen>() ?? garageGo.AddComponent<GarageScreen>();
        var cutGo = GameObject.Find("GarageCutscene") ?? new GameObject("GarageCutscene");
        cutGo.transform.SetParent(garageGo.transform, false);
        var cut = cutGo.GetComponent<GarageRestoreCutscene>() ?? cutGo.AddComponent<GarageRestoreCutscene>();
        var sogs = new SerializedObject(garage);
        sogs.FindProperty("cutscene").objectReferenceValue = cut;
        sogs.ApplyModifiedProperties();

        // GameContext wiring
        var soCtx = new SerializedObject(ctx);
        soCtx.FindProperty("restoreSystem").objectReferenceValue = restoreSys;
        soCtx.FindProperty("boostSystem").objectReferenceValue = boostSys;
        soCtx.FindProperty("tireWearStore").objectReferenceValue = twSys;
        soCtx.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WireGarageSceneObjects] 완료 — 4 GameObject + GameContext wiring");
    }
}
#endif
```

- [ ] **Step 2: 컴파일 + 메뉴 실행**

```bash
unity-cli editor refresh --compile
unity-cli console --type error --since 30s
# Expected: 0건
unity-cli menu "PitCrew/Garage/Wire Scene Objects"
unity-cli console --type log --since 10s | grep WireGarageSceneObjects
# Expected: "완료 — 4 GameObject + GameContext wiring"
```

- [ ] **Step 3: Services 등록 검증**

```bash
unity-cli editor play --wait
unity-cli exec "return \"Restore=\" + (PitCrew.Core.Services.Restore != null) + \" Boost=\" + (PitCrew.Core.Services.Boost != null) + \" TireWear=\" + (PitCrew.Core.Services.TireWear != null);"
unity-cli editor stop
```
Expected: `Restore=True Boost=True TireWear=True`

- [ ] **Step 4: Commit**

```
git add F1/Assets/Editor/_Recovery/WireGarageSceneObjects.cs F1/Assets/ComplexTrack.unity
git commit -m "feat(garage): ComplexTrack 씬 wiring (Task 11)

4 GameObject (F1RestoreSystem, BoostSystem, TireWearStore, GarageScreen+Cutscene)
+ GameContext SerializedObject 매핑. Editor 메뉴 idempotent.

Verified-By: self (Services.Restore/Boost/TireWear == True via unity-cli exec)
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 12: 통합 검증 + 화물 손상 페널티 (D-084) 확인 — verifier subagent

**Files:**
- (코드 변경 없음, 검증만)

- [ ] **Step 1: verifier subagent dispatch**

다음 프롬프트로 fresh subagent (haiku) 호출:

```
Editor 켜져 있고 Play 가능 상태. W3 차고 시스템 통합 검증:

1. unity-cli exec로 Services.* 14+3개 (Restore/Boost/TireWear 포함) 모두 set 여부
2. F1RestoreCatalog.parts.Count == 6 + 가격 (1000/4000/4000/8000/12000/25000)
3. AddReward(60000) 후 6 부품 모두 TryRestore — 각자 IsRestored == True
4. 엔진 복원 후 EffectiveMaxSpeedMps ratio ≈ 1.20
5. 변속기 복원 후 Gears.ManualUnlocked == True
6. 에어로 복원 후 Boost.IsUnlocked == True
7. 타이어 복원 후 Tires.SelectionUnlocked == True
8. TireWearStore.RecordJobUsage(Dry, 1000, 3.0) 후 GetWear01(Dry) < 1.0
9. CanAfford 로직: Wallet=500일 때 엔진(1000) CanAfford == False
10. Play 시작 → 콘솔 에러 0건

결과 표 + 깨진 항목 좌표. 모두 OK면 "ALL GREEN" 한 줄 추가.
```

- [ ] **Step 2: 화물 손상 페널티 검증 (D-084 — 이미 동작하는지 확인 only)**

```bash
unity-cli exec "return PitCrew.Order.RunController.ComputeIntegrityMultiplier(1.0f).ToString(\"F2\") + \" / \" + PitCrew.Order.RunController.ComputeIntegrityMultiplier(0.5f).ToString(\"F2\") + \" / \" + PitCrew.Order.RunController.ComputeIntegrityMultiplier(0.0f).ToString(\"F2\");"
```
Expected: 1.0에서 0.0으로 비례 감소. (현재 동작 검증, 변경 X.)

- [ ] **Step 3: 자금 carryover 검증 (D-083 — 이미 영구 wallet)**

```bash
unity-cli exec "PitCrew.Core.Services.Upgrade.AddReward(5000); return PitCrew.Core.Services.Upgrade.Wallet;"
# Play stop
unity-cli editor stop
# Play 다시
unity-cli editor play --wait
unity-cli exec "return PitCrew.Core.Services.Upgrade.Wallet;"
```
Expected: 동일 (Play stop/start는 데이터 유지 X — 단일 Play 세션 내 누적 확인만으로 충분. 실 carryover는 manual play에서 확인.)

- [ ] **Step 4: Verifier 결과 받아 발견된 항목 fix → 재 dispatch → 클린 후 commit**

- [ ] **Step 5: Commit**

```
git add -A
git commit -m "test(garage): 통합 검증 + D-083/084 동작 확인 (Task 12)

verifier subagent로 12 항목 ALL GREEN.
자금 carryover (D-083) = 이미 UpgradeSystem.Wallet 영구 → 추가 작업 0.
화물 손상 페널티 (D-084) = ComputeIntegrityMultiplier 비례 → 이미 동작.

Verified-By: subagent-task12-haiku ALL GREEN
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 13: 세션 27 로그 + activeContext 갱신

**Files:**
- Modify: `옵시디언/notes/session-log.md`
- Modify: `.claude/activeContext.md`

- [ ] **Step 1: session-log 한 블록 추가**

```markdown
## 세션 27 (2026-05-19~) — W3 차고/F1 부품 복원 시스템

- 기획 12 결정 (D-073~093): 가격 곡선 지수형 / 첫 엔진 1000₩ / 자금 누적 / 비례 페널티 / 차고 UI 머신 중앙 / 잠긴 카드 회색+부족 / 컷씬 2~3초 / 엔진 +20%·+30% / 변속기 K·L / 부스트 Space 게이지 / 타이어 4종 영구 마모 + 펑크=실패 / 그랑프리 후속 / 서스 점프대 맵트랙으로
- 구현 (Task 1~12): F1Part SO + Catalog 6 / F1RestoreSystem + Services 등록 / 엔진·변속기·에어로·타이어·도색·서스 효과 / GarageScreen UI + 컷씬 / ResultPanel '차고로' 버튼 / ComplexTrack 씬 wiring / verifier subagent 통합 검증
- 다음: R1 맵 적용 후 5런 측정 → 가격/마모량 재튜닝
```

- [ ] **Step 2: activeContext 갱신**

`.claude/activeContext.md`의 "다음 세션" 영역을 갱신:

```markdown
## 다음 세션 (세션 28) — 첫 작업

W3 차고 시스템 구현 완료. 다음:

1. **R1 맵 적용 + 5런 측정** — RunMetricsRecorder. 한 런 시간 베이스 확보.
2. **가격 재튜닝** — 측정 결과 보고 1000/4000/8000/12000/25000 조정.
3. **타이어 마모 수치 튜닝** — 잡당 -10~20% 적절한지.
4. **manual play로 차고 흐름 확인** — 정산→차고 버튼→복원 컷씬→다음 런.
5. **W4 진입 준비** — 정산 → 복원 연결 + 데모 시나리오 3런.
```

- [ ] **Step 3: Commit**

```
git add 옵시디언/notes/session-log.md .claude/activeContext.md
git commit -m "docs(active): 세션 27 종료 — W3 차고 시스템 완료 + 세션 28 첫 작업 [skip-verifier]

Verified-By: self
Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage 점검:**
- D-073 (6 부품 × 1 티어) → Task 1~2 ✅
- D-075 (사이드뷰 분해도) → Task 9 ✅
- D-077 (메커니즘 unlock) → Task 4~8 ✅
- D-078 (6 부품 모두 구현) → Task 4~8 ✅
- D-079 (별도 가격 문서) → 참조만, 가격 표는 Task 2에서 그대로 ✅
- D-080 (지수형 가격) → Task 2 데이터 ✅
- D-081 (자유 게이팅) → 트리 게이팅 코드 없음 ✅
- D-082 (엔진 1000₩) → Task 2 ✅
- D-083 (자금 누적) → 이미 UpgradeSystem.Wallet 영구 → Task 12 검증만 ✅
- D-084 (비례 페널티) → 이미 ComputeIntegrityMultiplier → Task 12 검증만 ✅
- D-085 (Yes/No prompt) → Task 10 ✅
- D-086 (머신 중앙 레이아웃) → Task 9 ✅
- D-087 (잠긴 카드 회색) → Task 9 ✅
- D-088 (2~3초 컷씬) → Task 9 ✅
- D-089 (수치/키바인딩) → Task 4 (엔진) / Task 5 (K/L) / Task 6 (Space) / Task 7 (타이어 4종) ✅
- D-091 (타이어 마모 + 펑크) → Task 7 ✅
- D-092 (그랑프리 후속) → Task 8 — 도색 = 색 swap만 ✅
- D-093 (서스 점프대 맵 트랙) → Task 8 — flag log만 ✅

**Placeholder scan:** "TBD"/"add error handling" 없음. 라인 번호 일부는 "L130 부근" 식 근사 — 파일 읽고 정확 매칭 필요 (subagent에게 명시).

**Type consistency:**
- `F1PartId`/`F1Part`/`F1RestoreCatalog`/`F1RestoreSystem` 이름 일관 ✅
- `Services.Restore` / `Services.Boost` / `Services.TireWear` 이름 일관 ✅
- `IF1RestoreSystem`/`IBoostSystem`/`ITireWearStore` 인터페이스 이름 일관 ✅
- `ApplyEngineRestore` / `SetManualUnlocked` / `SetUnlocked` / `SetSelectionUnlocked` — 셋터 명명 통일 X (혼재) — 일부 SetX, 일부 Apply. 이건 의도적 (Apply = 1회성 효과, Set = 토글). 의도 통일.
- `TireType.Dry/Wet/Soft/Hard` — 기존 enum 값 그대로 가정. Task 7 Step 3에 "이름 다르면 교체" 명시 ✅

**남은 위험:**
- Task 4 Step 1의 `accelTime` 사용처가 다중일 경우 grep으로 확인 필요 — implementer에게 명시.
- Task 7 펑크 발동 → EndRun(success=false) 흐름이 기존 EndRun 시그니처와 맞는지 확인 (`bool success` 파라미터 존재 여부).
- Task 10 ResultPanel "다시" 버튼 부모 RectTransform 파악 필요 (file 읽고 매칭).

이런 항목들은 implementer subagent에 "파일 읽고 정확히 매칭" 지시로 핸들오프.

---

## Execution Handoff

**Plan complete and saved to `docs/superpowers/plans/2026-05-19-garage-restore-system.md`.** 13 task, W3 6일 일정.

두 가지 실행 옵션:

1. **Subagent-Driven (recommended)** — task별 fresh subagent dispatch + spec/quality 2단 review. 빠른 반복.
2. **Inline Execution** — 같은 세션에서 batch + checkpoint.

어느 쪽으로 갈까?