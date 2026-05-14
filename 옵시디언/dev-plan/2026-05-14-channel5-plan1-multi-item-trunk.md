# 채널 5 Plan 1 — 다물건 트렁크 물리 (기반) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development 또는 superpowers:executing-plans 로 task 단위 실행. 스텝은 체크박스(`- [ ]`)로 추적.

**Goal:** 한 잡에 화물이 N개(1~3)일 때, N개 큐브가 트렁크 시뮬에서 각자 슬라이드·서로 충돌·각자 손상 흡수하게 만든다.

**Architecture:** 기존 `CargoShakeSimulator`의 단일 박스 slide 모델(hand-rolled, 튜닝됨)을 N개 아이템 리스트로 확장. 각 아이템은 자기 slide 상태(localPos·velocity)를 갖고 기존 slide 수식 재사용. 아이템 간 충돌은 2D(XZ) AABB 밀어내기. 손상은 각 아이템 자기 `WrapState`로 흡수 후 공유 `CargoBox` HP에 합산. 초기 배치는 그리드 자동(임시 — Plan 2가 드래그로 대체).

**Tech Stack:** Unity 6000.3.9f1, C#, unity-cli 검증 (pytest 없음 — 검증은 unity-cli exec + Play).

**스코프 경계:** 이 플랜 = "N개가 물리적으로 작동"까지. 플레이어 드래그 배치 = Plan 2. capacity 업그레이드 = Plan 3.

**관련 결정:** 다물건 = 각자 따로 포장 / 최대 3개 / 플레이어가 배치 (도현, 2026-05-14). HP는 공유 1개 풀(아이템별 HP는 스코프 밖) — 각 아이템은 자기 wrap으로 흡수만 차등.

---

## File Structure

| 파일 | 책임 | 변경 |
|---|---|---|
| `F1/Assets/Scripts/Core/Core.cs` | `ICargoShakeSimulator` 인터페이스 | `ClearMountedItems()` 추가 |
| `F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs` | 트렁크 N 아이템 slide·충돌·손상 | 단일 박스 → 리스트. 핵심 변경 |
| `F1/Assets/Scripts/Order/RunController.cs` | 포장 핸드오프 | `TransferWrapStateToCargo` — 전체 마운트 |
| `F1/Assets/Data/Orders/Order_C_Risky.asset` | 테스트용 멀티 잡 | `itemCount` 1→3 |

---

## Task 1: 인터페이스 + 핸드오프 — N개가 시뮬레이터까지 도달

**Files:**
- Modify: `F1/Assets/Scripts/Core/Core.cs` (ICargoShakeSimulator)
- Modify: `F1/Assets/Scripts/Order/RunController.cs` (`TransferWrapStateToCargo`)

- [ ] **Step 1: `ICargoShakeSimulator`에 `ClearMountedItems` 추가**

`Core.cs` 의 `ICargoShakeSimulator`:
```csharp
public interface ICargoShakeSimulator
{
    void SetBoxColor(Color c);
    /// <summary>포장한 화물 큐브 1개를 트렁크에 마운트 (additive — 여러 번 호출 = 여러 개).</summary>
    void MountWrappedItem(UnityEngine.GameObject cube);
    /// <summary>마운트된 화물 전부 제거 (런 시작 시 이전 잔재 청소).</summary>
    void ClearMountedItems();
}
```

- [ ] **Step 2: `RunController.TransferWrapStateToCargo` — 전체 마운트로 변경**

`RunController.cs` 의 메서드 본문 교체. 핵심: `items[0]`만 → `Shake.ClearMountedItems()` 후 전체 루프. `items[1+] 폐기` 루프 삭제.
```csharp
void TransferWrapStateToCargo(System.Collections.Generic.List<GameObject> packedItems)
{
    if (Shake == null)
    {
        Debug.LogWarning("[RunController] Shake 미등록 — 포장 큐브 폐기");
        if (packedItems != null) foreach (var go in packedItems) if (go != null) UnityEngine.Object.Destroy(go);
        return;
    }

    Shake.ClearMountedItems(); // 이전 런 잔재 제거

    if (packedItems == null || packedItems.Count == 0) return; // 폴백 잡 — placeholder 그대로

    int totalCost = 0;
    foreach (var cube in packedItems)
    {
        if (cube == null) continue;

        var srcWrap = cube.GetComponent<PitCrew.Packing.WrapState>();
        if (srcWrap != null) totalCost += Mathf.RoundToInt(srcWrap.TotalCost);

        var painter = cube.GetComponent<PitCrew.Packing.WrapPainter>();
        if (painter != null) painter.enabled = false;

        // paint raycast용 collider 제거 (운전 중 불필요). SetParent 전에 즉시 제거.
        foreach (var col in cube.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(col);

        cube.name = "WrappedItem";
        Shake.MountWrappedItem(cube);
    }

    if (totalCost > 0 && UpgradeSystem.Instance != null) UpgradeSystem.Instance.Spend(totalCost);
    Debug.Log($"[RunController] 포장 큐브 {packedItems.Count}개 → 트렁크 마운트 (총 cost={totalCost})");
}
```

- [ ] **Step 3: 컴파일 — 아직 CargoShakeSimulator가 인터페이스 미충족이라 에러 정상**

Run: `unity-cli editor refresh --compile` → `unity-cli console --type error`
Expected: `CargoShakeSimulator does not implement ClearMountedItems` 류 에러 (Task 2에서 해소).

- [ ] **Step 4: 커밋 보류** — Task 2 후 함께 컴파일 통과시키고 커밋.

---

## Task 2: CargoShakeSimulator — 단일 박스 → N 아이템 리스트

**Files:**
- Modify: `F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs`

기존 단일 박스 모델(`boxTransform` + `restPos` + `simVelLocal`)을 `List<TrunkItem>`로 교체. 각 아이템은 CargoSimRoot의 직접 자식, 자기 velocity로 slide, 트렁크 AABB에 clamp.

- [ ] **Step 1: `TrunkItem` 내부 클래스 + 필드**

클래스 상단(필드 영역):
```csharp
// 트렁크에 마운트된 화물 1개의 slide 상태.
class TrunkItem
{
    public Transform t;
    public Vector3 velLocal;       // CargoSimRoot local frame 속도
    public PitCrew.Packing.WrapState wrap;  // 이 아이템의 포장 상태 (손상 흡수용). null 가능.
    public float halfExtent;       // XZ 충돌·clamp용 반경 (m)
}

readonly System.Collections.Generic.List<TrunkItem> items = new();

[Header("Trunk Bounds")]
[Tooltip("트렁크 내벽 기준 아이템 중심 이동 한계 (CargoSimRoot local, m). 벽 ±1.0 - 아이템 반경.")]
[SerializeField] float trunkHalfExtent = 1.0f;
[Tooltip("마운트 시 아이템 균일 스케일 (m). 3개가 트렁크에 들어가는 크기.")]
[SerializeField] float itemScale = 0.55f;
```
기존 `restPos`, `restRot`, `simVelLocal`, `captured`, `boxTransform` 관련 필드/메서드(`CaptureRest`)는 제거하거나 placeholder 숨김 용도만 남김.

- [ ] **Step 2: `MountWrappedItem` (additive) + `ClearMountedItems`**

```csharp
public void MountWrappedItem(GameObject cube)
{
    if (cube == null) return;
    EnsurePlaceholderHidden();

    cube.transform.SetParent(transform, false); // CargoSimRoot 직속
    cube.transform.localRotation = Quaternion.identity;
    cube.transform.localScale = Vector3.one * itemScale;

    var item = new TrunkItem
    {
        t = cube.transform,
        velLocal = Vector3.zero,
        wrap = cube.GetComponentInChildren<PitCrew.Packing.WrapState>(),
        halfExtent = itemScale * 0.5f,
    };
    // 초기 위치는 Task 4 (AutoArrange)에서. 일단 중앙.
    cube.transform.localPosition = new Vector3(0f, ItemFloorY(item), 0f);
    items.Add(item);
}

public void ClearMountedItems()
{
    foreach (var it in items) if (it.t != null) Destroy(it.t.gameObject);
    items.Clear();
}

// placeholder Box는 1회만 숨김 (포장 큐브가 그 자리 대신).
bool placeholderHidden;
void EnsurePlaceholderHidden()
{
    if (placeholderHidden) return;
    var box = transform.Find("Box");
    if (box != null) { var mr = box.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false; }
    placeholderHidden = true;
}

// 아이템이 트렁크 바닥에 놓이는 local Y (바닥 = TrunkFloor 윗면 근처).
float ItemFloorY(TrunkItem it) => 0.33f; // placeholder Box localPos.y와 동일 — 트렁크 바닥 높이
```

- [ ] **Step 3: `FixedUpdate` — 아이템별 slide (기존 수식 재사용)**

기존 단일 박스 FixedUpdate를 `foreach (var it in items)` 루프로 감쌈. 기존 inertialAccel + slopeAccel + 정지/운동 마찰 수식 그대로, 단 `boxTransform` → `it.t`, `simVelLocal` → `it.velLocal`. clamp는 restPos 기준이 아니라 **절대 트렁크 AABB**:
```csharp
void FixedUpdate()
{
    if (cargo == null) { cargo = Services.Cargo as CargoBox; if (cargo == null) return; }
    if (items.Count == 0) return;
    float dt = Time.fixedDeltaTime;

    // 두 힘은 모든 아이템 공통 (차 G + 사면 중력) — 1회 계산.
    Vector3 inertialAccel = new Vector3(-cargo.SignedLatG, 0f, -cargo.SignedLongG) * accelScale;
    Vector3 localGravity = cargo.transform.InverseTransformDirection(Physics.gravity);
    Vector3 slopeAccel = new Vector3(localGravity.x, 0f, localGravity.z) / 9.81f * accelScale;
    Vector3 totalAccel = inertialAccel + slopeAccel;
    float accelMag = Mathf.Sqrt(totalAccel.x * totalAccel.x + totalAccel.z * totalAccel.z);
    float dampFactor = Mathf.Max(0f, 1f - kineticFriction * dt);

    foreach (var it in items)
    {
        if (it.t == null) continue;
        if (accelMag >= staticFrictionG * accelScale) it.velLocal += totalAccel * dt;
        it.velLocal.x *= dampFactor; it.velLocal.z *= dampFactor;
        if (it.velLocal.sqrMagnitude < velocityDeadZone * velocityDeadZone) it.velLocal = Vector3.zero;

        Vector3 p = it.t.localPosition + it.velLocal * dt;
        float lim = trunkHalfExtent - it.halfExtent;

        if (Mathf.Abs(p.x) > lim)
        {
            float impactSpeed = Mathf.Abs(it.velLocal.x);
            p.x = Mathf.Sign(p.x) * lim;
            it.velLocal.x = -it.velLocal.x * wallBounce;
            TryApplyBoxImpactDamage(it, impactSpeed, new Vector3(Mathf.Sign(p.x), 0f, 0f));
        }
        if (Mathf.Abs(p.z) > lim)
        {
            float impactSpeed = Mathf.Abs(it.velLocal.z);
            p.z = Mathf.Sign(p.z) * lim;
            it.velLocal.z = -it.velLocal.z * wallBounce;
            TryApplyBoxImpactDamage(it, impactSpeed, new Vector3(0f, 0f, Mathf.Sign(p.z)));
        }
        p.y = it.t.localPosition.y; // 바닥 고정
        it.t.localPosition = p;
    }

    ResolveItemCollisions(); // Task 3
}
```

- [ ] **Step 4: `LateUpdate` — yaw 매칭만 유지, 박스 회전 강제 제거**

기존 `LateUpdate`의 trunk yaw 매칭 + simCamRig 추적 블록은 유지. `enableRotation`/`restRot` 강제 블록은 삭제 (아이템은 placed rotation 유지, 회전 sim 없음).

- [ ] **Step 5: `SetBoxColor` — 마운트된 모든 아이템 base material 색칠**
```csharp
public void SetBoxColor(Color c)
{
    foreach (var it in items)
    {
        if (it.t == null) continue;
        var mr = it.t.GetComponent<MeshRenderer>();
        if (mr != null) mr.material.color = c;
    }
}
```

- [ ] **Step 6: 컴파일 통과 확인**

Run: `unity-cli editor refresh --compile` → `unity-cli console --type error,warning`
Expected: `[]` (0 에러). Task 3의 `ResolveItemCollisions`가 아직 없으면 stub로 빈 메서드 먼저 두기.

- [ ] **Step 7: 커밋** (Task 1 + 2 함께)
```bash
git add F1/Assets/Scripts/Core/Core.cs F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs F1/Assets/Scripts/Order/RunController.cs
git commit -m "feat(W2): 채널 5 — 트렁크 N 아이템 slide 기반"
```

---

## Task 3: 아이템 간 충돌 (2D AABB 밀어내기)

**Files:**
- Modify: `F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs`

- [ ] **Step 1: `ResolveItemCollisions` 구현**

FixedUpdate 끝에서 호출. 모든 쌍에 대해 XZ 평면 AABB 겹침 검사, 겹치면 절반씩 밀어내고 velocity 교환(간단 감쇠).
```csharp
void ResolveItemCollisions()
{
    for (int i = 0; i < items.Count; i++)
    for (int j = i + 1; j < items.Count; j++)
    {
        var a = items[i]; var b = items[j];
        if (a.t == null || b.t == null) continue;
        Vector3 pa = a.t.localPosition, pb = b.t.localPosition;
        float minDistX = a.halfExtent + b.halfExtent;
        float dx = pb.x - pa.x, dz = pb.z - pa.z;
        float overlapX = minDistX - Mathf.Abs(dx);
        float overlapZ = minDistX - Mathf.Abs(dz);
        if (overlapX <= 0f || overlapZ <= 0f) continue; // 안 겹침

        // 더 얕게 겹친 축으로 분리 (AABB MTV).
        if (overlapX < overlapZ)
        {
            float push = (dx >= 0f ? 1f : -1f) * overlapX * 0.5f;
            pa.x -= push; pb.x += push;
            float avg = (a.velLocal.x + b.velLocal.x) * 0.5f;
            a.velLocal.x = avg * 0.5f; b.velLocal.x = avg * 0.5f; // 충돌 감쇠
        }
        else
        {
            float push = (dz >= 0f ? 1f : -1f) * overlapZ * 0.5f;
            pa.z -= push; pb.z += push;
            float avg = (a.velLocal.z + b.velLocal.z) * 0.5f;
            a.velLocal.z = avg * 0.5f; b.velLocal.z = avg * 0.5f;
        }
        a.t.localPosition = pa; b.t.localPosition = pb;
    }
}
```
> 참고: 데모용 단순 모델. 동시 다중 겹침은 다음 FixedUpdate에 추가 해소됨 (반복 수렴). 3개라 1패스로 충분.

- [ ] **Step 2: 컴파일 통과**

Run: `unity-cli editor refresh --compile` → `unity-cli console --type error`
Expected: `[]`

- [ ] **Step 3: 커밋**
```bash
git add F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs
git commit -m "feat(W2): 채널 5 — 트렁크 아이템 간 충돌"
```

---

## Task 4: 아이템별 벽 충돌 손상 + 자동 그리드 배치

**Files:**
- Modify: `F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs`

- [ ] **Step 1: `TryApplyBoxImpactDamage` — 아이템별 시그니처로 변경**

기존 `TryApplyBoxImpactDamage(float speed, Vector3 dir)`를 `TrunkItem` 받게:
```csharp
void TryApplyBoxImpactDamage(TrunkItem it, float speed, Vector3 impactDirLocal)
{
    if (cargo == null || speed < boxWallSpeedThreshold) return;
    float raw = (speed - boxWallSpeedThreshold) * boxWallDamageMul;
    if (raw <= 0f) return;

    float absorption = 0f;
    if (it.wrap != null && it.t != null)
    {
        Vector3 worldHit = it.t.position;
        Vector3 worldDir = it.t.TransformDirection(impactDirLocal).normalized;
        absorption = it.wrap.QueryAbsorption(worldHit, worldDir);
    }
    float dmg = raw * (1f - absorption);
    if (dmg > 0.01f) cargo.ApplyExternalDamage(dmg, flashSpeedKmh: speed * 3.6f);
}
```
> N개 아이템이 각자 자기 `wrap`으로 흡수율 계산 → 공유 `CargoBox` HP에 합산 (설계: HP 1풀, 흡수만 차등).

- [ ] **Step 2: 자동 그리드 배치 — `MountWrappedItem`에서 빈 슬롯에 배치**

`MountWrappedItem`의 `localPosition` 설정을 그리드 슬롯으로:
```csharp
// items.Count 기준 슬롯 (이미 추가된 후 호출 시 index = items.Count-1).
// 3슬롯: x = -0.5, 0, +0.5, z = 0. (Plan 2에서 플레이어 드래그로 대체)
int slot = items.Count - 1;
float[] slotX = { -0.5f, 0f, 0.5f };
float sx = slot < slotX.Length ? slotX[slot] : 0f;
cube.transform.localPosition = new Vector3(sx, ItemFloorY(item), 0f);
```
(MountWrappedItem 안에서 `items.Add(item)` **후에** 이 위치 설정 — Step 위치 주의.)

- [ ] **Step 3: 컴파일 통과**

Run: `unity-cli editor refresh --compile` → `unity-cli console --type error`
Expected: `[]`

- [ ] **Step 4: 커밋**
```bash
git add F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs
git commit -m "feat(W2): 채널 5 — 아이템별 손상 + 자동 그리드 배치"
```

---

## Task 5: 테스트용 멀티 잡 + Play 검증

**Files:**
- Modify: `F1/Assets/Data/Orders/Order_C_Risky.asset` (`itemCount` 1→3)

- [ ] **Step 1: Order_C_Risky를 itemCount=3으로**

unity-cli exec (또는 인스펙터):
```
unity-cli exec "var p=UnityEditor.AssetDatabase.GUIDToAssetPath(UnityEditor.AssetDatabase.FindAssets(\"Order_C_Risky\")[0]); var oj=UnityEditor.AssetDatabase.LoadAssetAtPath<OrderJob>(p); var so=new UnityEditor.SerializedObject(oj); so.FindProperty(\"itemCount\").intValue=3; so.ApplyModifiedProperties(); UnityEditor.AssetDatabase.SaveAssets(); return \"itemCount=\"+oj.itemCount;"
```
Expected: `itemCount=3`

- [ ] **Step 2: Play — Order_C 선택 → 3개 포장 → 출발**

`unity-cli editor play --wait` 후 exec로: OrderBoard에서 Order_C 픽 → PackingScreen에서 ◀▶로 3개 각각 wrap (또는 wrap 없이) → OnGoClicked. Step 다수.

- [ ] **Step 3: 트렁크에 3개 마운트 확인**
```
unity-cli exec "var css=UnityEngine.Object.FindFirstObjectByType<CargoShakeSimulator>(); var f=typeof(CargoShakeSimulator).GetField(\"items\",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); var list=f.GetValue(css) as System.Collections.IList; var sb=new System.Text.StringBuilder(\"items=\"+list.Count+\"\\n\"); foreach(var o in list){ var t=o.GetType().GetField(\"t\").GetValue(o) as Transform; sb.AppendLine(\"  \"+t.name+\" localPos=\"+t.localPosition); } return sb.ToString();"
```
Expected: `items=3`, 세 아이템 서로 다른 x 위치.

- [ ] **Step 4: 슬라이드·충돌 동작 확인**

운전 Step 다수 진행 후 아이템 localPos가 변하는지, 콘솔 에러 0인지. `unity-cli screenshot --view game` + CaptureFullScreen으로 트렁크 CCTV에 3개 보이는지 시각 확인.

- [ ] **Step 5: 커밋**
```bash
git add F1/Assets/Data/Orders/Order_C_Risky.asset
git commit -m "feat(W2): 채널 5 — Order_C 멀티 잡 (itemCount=3) 테스트 벡터"
```

---

## Self-Review

- **Spec coverage:** 다물건 N개 작동(Task 1·2) / 각자 따로 포장 = 각자 WrapState 흡수(Task 4 Step 1) / 최대 3개(Order itemCount Range(1,3), 그리드 3슬롯) / 서로 충돌(Task 3) ✓. 플레이어 배치 = Plan 2. capacity = Plan 3.
- **Placeholder scan:** 없음 — 모든 코드 스텝에 실제 코드.
- **Type consistency:** `TrunkItem.t/velLocal/wrap/halfExtent`, `items` 리스트, `MountWrappedItem`/`ClearMountedItems`/`ResolveItemCollisions`/`TryApplyBoxImpactDamage(TrunkItem,...)` 전 task 일관.
- **알려진 미결:** 아이템 base material 색칠이 `mr.material` 인스턴스 생성 → 런당 N개 누수 (기존 단일 박스도 동일 패턴, 데모 허용). 다물건 잡이 Order_C 1개뿐 — Plan 3에서 카탈로그 확장.
