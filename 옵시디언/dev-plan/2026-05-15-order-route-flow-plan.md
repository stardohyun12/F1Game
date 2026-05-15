# 주문→경로 계획 흐름 개편 — 구현 계획

> Spec: `[[2026-05-15-order-route-flow-design]]`
> 인라인 실행 (Claude가 자는 도현 대신 진행). 각 태스크 = 기능 단위 커밋 1개.

**Goal:** 주문 보드를 맵 중심으로 바꾸고, 잡이 진짜 다른 목적지를 갖게 하고, 잡을 고른 뒤 배달 순서를 정하는 경로 계획 화면을 추가한다.

**Architecture:** 데이터 모델 변경(OrderJob.destinationLabels, 물건1개=목적지1곳) → DeliveryManager 경로 기반(ActivateRoute) → 신규 RoutePlanScreen(맵 위 클릭 시퀀스) → OrderBoard 맵 중심 + 모든 잡 목적지 동시 표시 + 차고 마커 + 경로선.

**Tech Stack:** Unity 6000.3.9f1, C#, URP. 검증: `unity-cli` (Editor 켠 채, `Application.runInBackground=true`).

---

## File Structure

| 파일 | 책임 | 변경 종류 |
|---|---|---|
| `Scripts/Order/OrderJob.cs` | 잡 데이터 SO | destinationLabels 추가, itemCount 파생, pointCount 제거 |
| `Scripts/DeliveryManager.cs` | 배달 순서/완료 | route 리스트, ActivateRoute, FindByLabel, HasPlannedRoute |
| `Scripts/Order/RunController.cs` | 런 진행 | ActivateDeliveryPoints 분기, pointCount→itemCount |
| `Scripts/Order/DeliveryPointMarker.cs` (기존 위치는 `Scripts/Order/`) | 배달지 비주얼 | 순번 배지 메서드, label 노출 |
| `Scripts/Core/Core.cs` | 인터페이스/Services | IRoutePlanScreen 추가, Services.RoutePlan |
| `Scripts/Core/GameContext.cs` | 와이어링 | RoutePlanScreen 서비스 등록 |
| `Scripts/Order/RoutePlanScreen.cs` (신규) | 경로 계획 화면 | Show/Hide/UI 자체 구축 |
| `Scripts/Order/OrderBoard.cs` | 주문 보드 | label 기반 마커, 차고/라인, 카드 클릭 → RoutePlan |
| `Scripts/Order/OrderCardUI.cs` | 카드 표시 | "지점 N곳" 표시를 itemCount 기반으로 |
| `Editor/_Recovery/Channel8RouteSetup.cs` (신규) | 데이터 셋업 | 잡별 destinationLabels 저작 |
| `Editor/_Recovery/Channel8BoardLayoutSetup.cs` (신규) | 씬 조정 | cardsContainer 우측 스트립으로 |

---

## Task 1 — OrderJob: destinationLabels + itemCount 파생 + pointCount 제거

**Files:**
- Modify: `F1/Assets/Scripts/Order/OrderJob.cs`
- Modify: `F1/Assets/Scripts/Order/RunController.cs` (pointCount 읽는 곳)
- Modify: `F1/Assets/Scripts/Order/OrderCardUI.cs` (pointCount 읽는 곳)
- Modify: `F1/Assets/Scripts/Order/OrderBoard.cs` (pointCount 읽는 곳)

- [ ] **Step 1: OrderJob.cs — destinationLabels 추가, itemCount 파생, pointCount 제거**

`itemCount` 직렬화 필드 제거 → `destinationLabels.Count` 파생. `pointCount` 제거. 다른 필드는 그대로.

```csharp
// Items / Destinations (채널 8)
[Header("Items / Destinations")]
[Tooltip("배달 목적지 (씬 DeliveryPoint.label과 일치). 물건 1개 = 목적지 1곳.")]
public List<string> destinationLabels = new List<string>();

/// <summary>물건 수 = 목적지 수 (1~3). destinationLabels에서 파생.</summary>
public int itemCount => destinationLabels != null
    ? Mathf.Clamp(destinationLabels.Count, 1, 3)
    : 1;
```

- [ ] **Step 2: RunController.cs — pointCount → itemCount**

`ActivateDeliveryPoints(job)` 내 `Delivery.ActivateSubset(job.pointCount)` → 일단 `job.itemCount`로 변경 (Task 3에서 분기 추가). `EndRun`의 시간초과 비례보상 `CurrentJob.pointCount > 0 ? (float)CompletedCount / CurrentJob.pointCount` → `CurrentJob.itemCount`.

- [ ] **Step 3: OrderCardUI.cs — pointCount → itemCount**

`SetStatRow(0, "지점", $"{job.pointCount}곳")` → `$"{job.itemCount}곳"`. 의미상 "지점 = 들를 곳 = 물건 수"가 됨.

- [ ] **Step 4: OrderBoard.cs — pointCount → itemCount (마커는 일단 그대로)**

`ApplyHoverState` 내 `bool inSet = i < job.pointCount` → `i < job.itemCount`. 비슷하게 `Close()` 내. (Task 7에서 label 기반으로 전면 교체할 거지만, 그 전 단계 컴파일 통과용.)

- [ ] **Step 5: 컴파일 + 검증**

```bash
unity-cli editor refresh --compile
unity-cli console --type error,warning
```
Expected: `[]` (오류 없음). 잡 에셋의 기존 itemCount 직렬화 값은 무시됨 (필드 제거됨) — Task 2 복구 스크립트가 destinationLabels 채워서 itemCount 복원.

- [ ] **Step 6: 커밋**

```bash
git add F1/Assets/Scripts/Order/OrderJob.cs F1/Assets/Scripts/Order/RunController.cs F1/Assets/Scripts/Order/OrderCardUI.cs F1/Assets/Scripts/Order/OrderBoard.cs
git commit -m "refactor(order): OrderJob.destinationLabels + itemCount 파생, pointCount 제거"
```

---

## Task 2 — 복구 스크립트: 잡별 destinationLabels 저작

**Files:**
- Create: `F1/Assets/Editor/_Recovery/Channel8RouteSetup.cs`

- [ ] **Step 1: 복구 스크립트 작성**

씬 DeliveryPoint 이름 8개를 잡 9개에 배분. 멱등.

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 채널 8 — 잡별 destinationLabels 저작. 물건 1개 = 목적지 1곳 (spec).
/// Order_05_StandardBox(2개), Order_C_Risky(3개)는 채널 5에서 정한 itemCount 유지.
/// 나머지 Pool 잡 7개는 1개씩. 잡들이 서로 다른 곳으로 가도록 분배.
/// </summary>
public static class Channel8RouteSetup
{
    // 씬 DeliveryPoint 이름 8개 (참조용)
    const string DowntownEntry = "DowntownEntry";
    const string FactoryNorth = "FactoryNorth";
    const string EastHub = "EastHub";
    const string LowerWest = "LowerWest";
    const string SuburbanGate = "SuburbanGate";
    const string CentralCross = "CentralCross";
    const string WestHub = "WestHub";
    const string PlateauTop = "PlateauTop";

    [MenuItem("PitCrew/Channel8/Route Setup — 잡별 destinationLabels")]
    public static void Run()
    {
        // 1물건 잡 7개 — 서로 다른 단일 목적지
        AssignLabels("Assets/Data/Orders/Pool/Order_01_Eggs.asset", new[] { CentralCross });
        AssignLabels("Assets/Data/Orders/Pool/Order_02_Glass.asset", new[] { EastHub });
        AssignLabels("Assets/Data/Orders/Pool/Order_03_Fish.asset", new[] { DowntownEntry });
        AssignLabels("Assets/Data/Orders/Pool/Order_04_WeddingCake.asset", new[] { PlateauTop });
        AssignLabels("Assets/Data/Orders/Pool/Order_06_PremiumWine.asset", new[] { WestHub });
        AssignLabels("Assets/Data/Orders/Pool/Order_07_SashimiTray.asset", new[] { LowerWest });
        AssignLabels("Assets/Data/Orders/Pool/Order_08_BirthdayCake.asset", new[] { SuburbanGate });

        // 2물건 잡 — 떨어진 2곳
        AssignLabels("Assets/Data/Orders/Pool/Order_05_StandardBox.asset",
            new[] { DowntownEntry, FactoryNorth });

        // 3물건 잡 (대량 배송) — 흩어진 3곳
        AssignLabels("Assets/Data/Orders/Order_C_Risky.asset",
            new[] { WestHub, CentralCross, PlateauTop });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Channel8RouteSetup] 완료 — 9개 잡 destinationLabels 저작.");
    }

    static void AssignLabels(string path, string[] labels)
    {
        var job = AssetDatabase.LoadAssetAtPath<OrderJob>(path);
        if (job == null) { Debug.LogWarning($"[Channel8RouteSetup] {path} 못 찾음"); return; }
        job.destinationLabels = new List<string>(labels);
        EditorUtility.SetDirty(job);
    }
}
```

- [ ] **Step 2: 메뉴 실행**

```bash
unity-cli menu "PitCrew/Channel8/Route Setup — 잡별 destinationLabels"
```

- [ ] **Step 3: 검증**

```bash
unity-cli exec "var sb=new System.Text.StringBuilder(); var paths=new[]{\"Pool/Order_01_Eggs\",\"Pool/Order_02_Glass\",\"Pool/Order_03_Fish\",\"Pool/Order_04_WeddingCake\",\"Pool/Order_05_StandardBox\",\"Pool/Order_06_PremiumWine\",\"Pool/Order_07_SashimiTray\",\"Pool/Order_08_BirthdayCake\",\"Order_C_Risky\"}; foreach(var p in paths){ var j=UnityEditor.AssetDatabase.LoadAssetAtPath<OrderJob>($\"Assets/Data/Orders/{p}.asset\"); sb.AppendLine($\"{j.name}: itemCount={j.itemCount} dests=[{string.Join(\",\",j.destinationLabels)}]\"); } return sb.ToString();"
```
Expected: 모든 잡 itemCount가 destinationLabels.Count와 일치, Order_05=2, Order_C=3, 나머지=1.

- [ ] **Step 4: 커밋**

```bash
git add F1/Assets/Editor/_Recovery/Channel8RouteSetup.cs F1/Assets/Editor/_Recovery/Channel8RouteSetup.cs.meta F1/Assets/Data/Orders/Pool/*.asset F1/Assets/Data/Orders/Order_C_Risky.asset
git commit -m "data(channel8): 9개 잡에 destinationLabels 저작 (잡별 다른 목적지)"
```

---

## Task 3 — DeliveryManager: 경로 기반 활성화 + RunController 분기

**Files:**
- Modify: `F1/Assets/Scripts/DeliveryManager.cs`
- Modify: `F1/Assets/Scripts/Order/RunController.cs`

- [ ] **Step 1: DeliveryManager — route 리스트 + ActivateRoute + FindByLabel + HasPlannedRoute**

`points` 마스터 리스트는 그대로. `route` 추가, 인덱싱을 `route`로 전환.

```csharp
// 채널 8 — 플레이어가 지정한 배달 순서 (RoutePlanScreen 출력)
readonly List<DeliveryPoint> route = new List<DeliveryPoint>();

public bool HasPlannedRoute => route.Count > 0 && !finished;

public DeliveryPoint FindByLabel(string label)
{
    if (string.IsNullOrEmpty(label)) return null;
    for (int i = 0; i < points.Count; i++)
        if (points[i] != null && points[i].Label == label) return points[i];
    return null;
}

/// <summary>경로 계획 화면이 정한 순서대로 활성화. route[0]만 active.</summary>
public void ActivateRoute(List<DeliveryPoint> orderedRoute)
{
    route.Clear();
    if (orderedRoute != null) foreach (var p in orderedRoute) if (p != null) route.Add(p);

    activeCount = route.Count;
    currentIndex = 0;
    returning = false;
    finished = false;

    // 모든 points는 일단 비활성, route에 든 것만 visible + 첫 번째만 active
    for (int i = 0; i < points.Count; i++)
    {
        if (points[i] == null) continue;
        points[i].Bind(this, points[i].Label);
        bool used = route.Contains(points[i]);
        points[i].gameObject.SetActive(used);
        points[i].SetActive(used && points[i] == route[0]);
    }
}
```

- [ ] **Step 2: DeliveryManager — `CurrentTarget`/`OnDeliveryReached`/`TotalCount` 경로 기반**

```csharp
public DeliveryPoint CurrentTarget => returning || finished || currentIndex >= route.Count
    ? null
    : route[currentIndex];

public int TotalCount => route.Count > 0 ? route.Count : points.Count;

public void OnDeliveryReached(DeliveryPoint p)
{
    if (finished || returning) return;
    if (route.Count == 0 || p != route[currentIndex]) return;

    p.SetCompleted();
    OnDeliveryCompleted?.Invoke(p);
    AudioCueBus.Publish(AudioCueId.DeliveryCompleted);
    currentIndex++;

    if (currentIndex >= route.Count)
    {
        returning = (startReturn != null);
        if (!returning) Finish();
    }
    else
    {
        route[currentIndex].SetActive(true);
    }
}
```

- [ ] **Step 3: DeliveryManager — `ActivateSubset` 리팩터 (route 채우는 방식으로)**

```csharp
/// <summary>폴백: 앞에서부터 N개 points로 route 구성.</summary>
public void ActivateSubset(int n)
{
    int clamped = Mathf.Clamp(n, 0, points.Count);
    var fallback = new List<DeliveryPoint>(clamped);
    for (int i = 0; i < clamped; i++) if (points[i] != null) fallback.Add(points[i]);
    ActivateRoute(fallback);
}
```

- [ ] **Step 4: DeliveryManager — `Finish`에서 route 클리어**

```csharp
void Finish()
{
    finished = true;
    returning = false;
    OnAllCompleted?.Invoke();
    route.Clear(); // 다음 런 폴백 위해
    Debug.Log("DeliveryManager: all deliveries complete + returned.");
}
```

- [ ] **Step 5: RunController.ActivateDeliveryPoints — HasPlannedRoute 분기**

```csharp
void ActivateDeliveryPoints(OrderJob job)
{
    if (Delivery == null) return;
    var mgr = Delivery as DeliveryManager;
    if (mgr != null && mgr.HasPlannedRoute) return; // RoutePlanScreen이 이미 세팅
    Delivery.ActivateSubset(job.itemCount); // 폴백
}
```

(`IDeliveryManager` 인터페이스에 HasPlannedRoute 추가하면 캐스팅 불필요 — Phase 7 정리. 일단 캐스팅으로.)

- [ ] **Step 6: 컴파일 + 검증**

```bash
unity-cli editor refresh --compile && unity-cli console --type error,warning
```
Expected: `[]`.

- [ ] **Step 7: 동작 검증 (Play, route 직접 호출)**

```bash
unity-cli editor play
unity-cli exec "Application.runInBackground=true; var dm=Services.Delivery as DeliveryManager; var p1=dm.FindByLabel(\"WestHub\"); var p2=dm.FindByLabel(\"CentralCross\"); var p3=dm.FindByLabel(\"PlateauTop\"); dm.ActivateRoute(new System.Collections.Generic.List<DeliveryPoint>{p1,p2,p3}); return $\"target={dm.CurrentTarget?.Label} total={dm.TotalCount}\";"
```
Expected: `target=WestHub total=3`.

```bash
unity-cli exec "var dm=Services.Delivery as DeliveryManager; dm.OnDeliveryReached(dm.FindByLabel(\"WestHub\")); return $\"after delivery1: target={dm.CurrentTarget?.Label}\";"
```
Expected: `target=CentralCross`.

```bash
unity-cli editor stop
```

- [ ] **Step 8: 커밋**

```bash
git add F1/Assets/Scripts/DeliveryManager.cs F1/Assets/Scripts/Order/RunController.cs
git commit -m "refactor(delivery): route 기반 활성화 — ActivateRoute, FindByLabel, HasPlannedRoute"
```

---

## Task 4 — DeliveryPointMarker: 순번 배지 + label 접근

**Files:**
- Modify: `F1/Assets/Scripts/Order/DeliveryPointMarker.cs`

- [ ] **Step 1: DeliveryPointMarker — Label 노출 + SetRouteOrder/Clear**

```csharp
public string Label => dp != null ? dp.Label : null;

GameObject orderBadge;
TMPro.TextMeshPro orderBadgeText;

public void SetRouteOrder(int n)
{
    EnsureOrderBadge();
    orderBadge.SetActive(true);
    orderBadgeText.text = n.ToString();
}

public void ClearRouteOrder()
{
    if (orderBadge != null) orderBadge.SetActive(false);
}

void EnsureOrderBadge()
{
    if (orderBadge != null) return;
    orderBadge = new GameObject("OrderBadge");
    orderBadge.transform.SetParent(transform, false);
    orderBadge.transform.localPosition = new Vector3(0f, pillarHeight + 1.2f, 0f);
    orderBadgeText = orderBadge.AddComponent<TMPro.TextMeshPro>();
    orderBadgeText.fontSize = 6f;
    orderBadgeText.alignment = TMPro.TextAlignmentOptions.Center;
    orderBadgeText.color = Color.white;
    orderBadgeText.fontStyle = TMPro.FontStyles.Bold;
    // billboard — Update에서 카메라 향함
}

void LateUpdate()
{
    if (orderBadge != null && orderBadge.activeSelf && Camera.main != null)
    {
        orderBadge.transform.rotation = Quaternion.LookRotation(
            orderBadge.transform.position - Camera.main.transform.position);
    }
}
```

- [ ] **Step 2: 컴파일 + 커밋**

```bash
unity-cli editor refresh --compile && unity-cli console --type error,warning
```
Expected: `[]`.

```bash
git add F1/Assets/Scripts/Order/DeliveryPointMarker.cs
git commit -m "feat(delivery): 마커 순번 배지 + Label 노출 (경로 계획 화면용)"
```

---

## Task 5 — Core.cs: IRoutePlanScreen + Services.RoutePlan

**Files:**
- Modify: `F1/Assets/Scripts/Core/Core.cs`

- [ ] **Step 1: 인터페이스 + Services 등록**

`Core.cs`의 인터페이스 region에 추가:

```csharp
public interface IRoutePlanScreen
{
    bool IsVisible { get; }
    void Show(OrderJob job);
    void Hide();
}
```

Services 클래스에 추가:

```csharp
public static IRoutePlanScreen RoutePlan { get; internal set; }
```

`Services.Reset()`에 `RoutePlan = null;` 추가.

- [ ] **Step 2: 컴파일**

```bash
unity-cli editor refresh --compile && unity-cli console --type error,warning
```
Expected: `[]` (인터페이스 빈 채 컴파일 통과).

(커밋은 Task 6과 함께 — 인터페이스만 박힌 상태로 두지 않음.)

---

## Task 6 — RoutePlanScreen + GameContext 와이어링

**Files:**
- Create: `F1/Assets/Scripts/Order/RoutePlanScreen.cs`
- Modify: `F1/Assets/Scripts/Core/GameContext.cs`

- [ ] **Step 1: RoutePlanScreen — 본체**

자체 ScreenSpaceOverlay 캔버스. PackingScreen 패턴 따라:
- `Show(job)`: 캔버스 빌드 + 잡 destinationLabels 해석 → 해당 DeliveryPoint들 강조 + 차고 마커. 시간 멈춤(Time.timeScale=0).
- 마우스 클릭 → ray → DeliveryPoint collider 잡으면 순서에 추가, 마커에 SetRouteOrder.
- "추천 순서" 버튼: 차고에서 최근접 이웃 순서로 자동 채움.
- "재설정" 버튼: 순서 클리어.
- "확정" 버튼 (모든 목적지 순서 들어가야 활성): `Services.Delivery.ActivateRoute(orderedList)` → `Hide()` → `Services.Packing.Show(job)`.
- `Hide()`: 캔버스 destroy, 마커 ClearRouteOrder, SetExternalControl(false), Time.timeScale 복원 X(다음 화면이 알아서).

핵심 구조:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class RoutePlanScreen : MonoBehaviour, IRoutePlanScreen
{
    GameObject panelRoot;
    OrderJob currentJob;
    Camera mapCamera; // OverviewCamera의 Camera
    DeliveryManager dm;
    Transform startPoint; // Race 오브젝트
    GameObject startMarker;

    // 잡의 목적지 마커들 (참조)
    readonly List<DeliveryPointMarker> jobMarkers = new();
    // 플레이어가 클릭 순서대로 쌓는 경로
    readonly List<DeliveryPoint> orderedRoute = new();

    // UI 참조
    TMP_Text titleText, listText, distanceText;
    Button confirmBtn, suggestBtn, resetBtn;
    Image lineImg; // 단순 라인 (또는 LineRenderer 별도)
    LineRenderer routeLine;

    public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

    public void Show(OrderJob job)
    {
        currentJob = job;
        EnsureRefs();
        BuildUI();
        ResolveJobMarkers();
        ShowStartMarker(true);
        ResetOrder();
        Time.timeScale = 0f;
        if (panelRoot != null) panelRoot.SetActive(true);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    public void Hide()
    {
        ClearOrderBadges();
        ShowStartMarker(false);
        if (routeLine != null) routeLine.positionCount = 0;
        if (panelRoot != null) { Destroy(panelRoot); panelRoot = null; }
        // SetExternalControl(false) — OrderBoard.Close가 이미 호출했지만 안전하게
        foreach (var m in jobMarkers) if (m != null) m.SetExternalControl(false);
        jobMarkers.Clear();
        orderedRoute.Clear();
    }

    void EnsureRefs()
    {
        if (mapCamera == null)
        {
            var ov = Services.Overview as OverviewCamera;
            if (ov != null) mapCamera = ov.GetComponent<Camera>();
        }
        dm = Services.Delivery as DeliveryManager;
        if (startPoint == null)
        {
            var race = GameObject.Find("Race");
            if (race != null) startPoint = race.transform;
        }
    }

    void ResolveJobMarkers()
    {
        jobMarkers.Clear();
        if (currentJob == null || dm == null) return;
        foreach (var label in currentJob.destinationLabels)
        {
            var dp = dm.FindByLabel(label);
            if (dp == null) continue;
            var m = dp.GetComponent<DeliveryPointMarker>();
            if (m == null) continue;
            m.SetExternalControl(true);
            m.SetVisible(true);
            m.SetColor(currentJob.color);
            m.SetDimmed(false);
            jobMarkers.Add(m);
        }
        // 잡에 없는 다른 마커들은 dim
        var allMarkers = UnityEngine.Object.FindObjectsByType<DeliveryPointMarker>(FindObjectsSortMode.None);
        foreach (var am in allMarkers)
        {
            if (jobMarkers.Contains(am)) continue;
            am.SetExternalControl(true);
            am.SetDimmed(true);
        }
    }

    void Update()
    {
        if (!IsVisible || mapCamera == null) return;
        if (UnityEngine.InputSystem.Mouse.current == null) return;
        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 sp = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            HandleMapClick(sp);
        }
    }

    void HandleMapClick(Vector2 screenPos)
    {
        Physics.SyncTransforms();
        var ray = mapCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 500f)) return;
        var marker = hit.collider.GetComponentInParent<DeliveryPointMarker>();
        if (marker == null) return;
        if (!jobMarkers.Contains(marker)) return;
        var dp = marker.GetComponent<DeliveryPoint>();
        if (dp == null || orderedRoute.Contains(dp)) return; // 이미 순서에 있음
        orderedRoute.Add(dp);
        marker.SetRouteOrder(orderedRoute.Count);
        RefreshAfterChange();
    }

    void OnSuggest()
    {
        // 최근접 이웃 from 차고
        orderedRoute.Clear();
        ClearOrderBadges();
        var remaining = new List<DeliveryPoint>();
        foreach (var m in jobMarkers) { var d=m.GetComponent<DeliveryPoint>(); if(d!=null) remaining.Add(d); }
        Vector3 cur = startPoint != null ? startPoint.position : Vector3.zero;
        while (remaining.Count > 0)
        {
            DeliveryPoint best = null; float bestDist = float.MaxValue;
            foreach (var d in remaining) {
                float dist = Vector2.Distance(new Vector2(cur.x,cur.z), new Vector2(d.transform.position.x, d.transform.position.z));
                if (dist < bestDist) { bestDist = dist; best = d; }
            }
            if (best == null) break;
            remaining.Remove(best);
            orderedRoute.Add(best);
            var m = best.GetComponent<DeliveryPointMarker>();
            if (m != null) m.SetRouteOrder(orderedRoute.Count);
            cur = best.transform.position;
        }
        RefreshAfterChange();
    }

    void OnReset()
    {
        orderedRoute.Clear();
        ClearOrderBadges();
        RefreshAfterChange();
    }

    void ClearOrderBadges()
    {
        foreach (var m in jobMarkers) if (m != null) m.ClearRouteOrder();
    }

    void RefreshAfterChange()
    {
        // 리스트 텍스트
        if (listText != null)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < orderedRoute.Count; i++)
                sb.AppendLine($"{i+1}. {orderedRoute[i].Label}");
            listText.text = sb.ToString();
        }
        // 거리 합 (XZ 직선)
        float total = 0f;
        Vector3 prev = startPoint != null ? startPoint.position : Vector3.zero;
        foreach (var d in orderedRoute)
        {
            total += Vector2.Distance(new Vector2(prev.x, prev.z), new Vector2(d.transform.position.x, d.transform.position.z));
            prev = d.transform.position;
        }
        if (distanceText != null) distanceText.text = $"총 거리 약 {total:F0}m";
        // 라인
        if (routeLine != null)
        {
            routeLine.positionCount = orderedRoute.Count + 1;
            int idx = 0;
            Vector3 sp = startPoint != null ? startPoint.position + Vector3.up * 0.5f : Vector3.zero;
            routeLine.SetPosition(idx++, sp);
            foreach (var d in orderedRoute) routeLine.SetPosition(idx++, d.transform.position + Vector3.up * 0.5f);
        }
        if (confirmBtn != null) confirmBtn.interactable = (orderedRoute.Count == jobMarkers.Count && orderedRoute.Count > 0);
    }

    void ResetOrder() { orderedRoute.Clear(); ClearOrderBadges(); RefreshAfterChange(); }

    void ShowStartMarker(bool show)
    {
        if (startPoint == null) return;
        if (startMarker == null)
        {
            startMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            startMarker.name = "RoutePlan_StartMarker";
            DestroyImmediate(startMarker.GetComponent<Collider>());
            startMarker.transform.position = startPoint.position + Vector3.up * 1.5f;
            startMarker.transform.localScale = new Vector3(2f, 3f, 2f);
            var mr = startMarker.GetComponent<MeshRenderer>();
            mr.material.color = new Color(1f, 0.9f, 0.2f, 1f);
        }
        startMarker.SetActive(show);
    }

    void OnConfirm()
    {
        if (orderedRoute.Count != jobMarkers.Count) return;
        if (dm == null) dm = Services.Delivery as DeliveryManager;
        if (dm != null) dm.ActivateRoute(new List<DeliveryPoint>(orderedRoute));
        var job = currentJob;
        Hide();
        if (Services.Packing != null) Services.Packing.Show(job);
        else if (Services.Run != null) Services.Run.BeginRun(job);
    }

    // ── BuildUI (간이) ───────────────────────────────
    void BuildUI()
    {
        if (panelRoot != null) return;
        var canvasGO = new GameObject("RoutePlanCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1f;
        panelRoot = canvasGO;
        EnsureEventSystem();
        BuildSidePanel(panelRoot.transform);
        EnsureRouteLine();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
    }

    void EnsureRouteLine()
    {
        var lrGO = new GameObject("RoutePlan_RouteLine");
        routeLine = lrGO.AddComponent<LineRenderer>();
        routeLine.material = new Material(Shader.Find("Sprites/Default"));
        routeLine.startColor = routeLine.endColor = currentJob != null ? currentJob.color : Color.yellow;
        routeLine.startWidth = routeLine.endWidth = 0.5f;
        routeLine.positionCount = 0;
        routeLine.useWorldSpace = true;
    }

    void BuildSidePanel(Transform canvas)
    {
        // 우측 세로 패널 (폭 320)
        var panelGO = new GameObject("RouteSidePanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(canvas, false);
        var prt = panelGO.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(1, 0); prt.anchorMax = new Vector2(1, 1); prt.pivot = new Vector2(1, 0.5f);
        prt.sizeDelta = new Vector2(340, 0); prt.anchoredPosition = new Vector2(0, 0);
        panelGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.92f);

        titleText = MakeLabel(panelGO.transform, "Title", currentJob != null ? $"경로 계획 — {currentJob.displayName}" : "경로 계획",
            new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(300, 60), 24, FontStyles.Bold, TextAlignmentOptions.Center);

        MakeLabel(panelGO.transform, "Hint", "맵의 목적지를 배달 순서대로 클릭",
            new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(300, 40), 14, FontStyles.Normal, TextAlignmentOptions.Center);

        listText = MakeLabel(panelGO.transform, "List", "",
            new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(300, 200), 18, FontStyles.Normal, TextAlignmentOptions.TopLeft);

        distanceText = MakeLabel(panelGO.transform, "Distance", "",
            new Vector2(0.5f, 0f), new Vector2(0, 240), new Vector2(300, 30), 16, FontStyles.Normal, TextAlignmentOptions.Center);

        suggestBtn = MakeButton(panelGO.transform, "Suggest", "추천 순서",
            new Vector2(0.5f, 0f), new Vector2(0, 200), new Vector2(140, 36));
        suggestBtn.onClick.AddListener(OnSuggest);

        resetBtn = MakeButton(panelGO.transform, "Reset", "재설정",
            new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(140, 36));
        resetBtn.onClick.AddListener(OnReset);

        confirmBtn = MakeButton(panelGO.transform, "Confirm", "확정",
            new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(280, 56));
        confirmBtn.onClick.AddListener(OnConfirm);
        var cbg = confirmBtn.GetComponent<Image>();
        if (cbg != null) cbg.color = new Color(0.88f, 0.04f, 0.0f, 1f);
        confirmBtn.interactable = false;
    }

    static TMP_Text MakeLabel(Transform parent, string name, string text, Vector2 anchor,
        Vector2 anchoredPos, Vector2 size, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, anchor.y);
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.raycastTarget = false;
        t.text = text; t.fontSize = fontSize; t.fontStyle = style; t.alignment = align;
        t.color = Color.white;
        return t;
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.18f, 0.20f, 0.25f, 1f);
        var txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.raycastTarget = false;
        txt.text = label; txt.fontSize = 20; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center; txt.color = Color.white;
        return go.GetComponent<Button>();
    }
}
```

- [ ] **Step 2: GameContext.cs 와이어링**

GameContext의 등록부에 추가:

```csharp
// RoutePlanScreen — 없으면 자동 생성
var routePlan = FindFirstObjectByType<RoutePlanScreen>(FindObjectsInactive.Include);
if (routePlan == null)
{
    var go = new GameObject("RoutePlanScreen");
    routePlan = go.AddComponent<RoutePlanScreen>();
}
Services.RoutePlan = routePlan;
```

(GameContext 코드 구조 확인 후 적절한 곳에 삽입.)

- [ ] **Step 3: 컴파일 + 검증**

```bash
unity-cli editor refresh --compile && unity-cli console --type error,warning
```
Expected: `[]`.

```bash
unity-cli editor play
unity-cli exec "Application.runInBackground=true; var r=Services.RoutePlan; return $\"RoutePlan registered={r!=null} type={(r!=null?r.GetType().Name:\"-\")}\";"
unity-cli editor stop
```
Expected: `RoutePlan registered=True type=RoutePlanScreen`.

- [ ] **Step 4: 커밋**

```bash
git add F1/Assets/Scripts/Core/Core.cs F1/Assets/Scripts/Core/GameContext.cs F1/Assets/Scripts/Order/RoutePlanScreen.cs F1/Assets/Scripts/Order/RoutePlanScreen.cs.meta
git commit -m "feat(route): RoutePlanScreen 신규 — 맵 클릭 시퀀스로 배달 순서 지정"
```

---

## Task 7 — OrderBoard: label 기반 마커 + 모든 잡 동시 표시

**Files:**
- Modify: `F1/Assets/Scripts/Order/OrderBoard.cs`

- [ ] **Step 1: ApplyHoverState — label 기반**

기존 `i < job.pointCount` → `job.destinationLabels.Contains(marker.Label)`. Open 시점에 **모든 offered 잡의 destinationLabels 합집합**을 visible로, 호버 안 한 동안엔 각 잡 색으로 약하게 (또는 중립). 호버 → 그 잡 마커만 강렬, 다른 잡 마커는 dim.

```csharp
void ApplyHoverState()
{
    if (allMarkers == null) return;
    for (int i = 0; i < allMarkers.Count; i++)
    {
        var marker = allMarkers[i]; if (marker == null) continue;
        string lbl = marker.Label;

        if (hoveredIndex >= 0 && hoveredIndex < currentOffer.Count)
        {
            var job = currentOffer[hoveredIndex];
            bool inJob = job.destinationLabels != null && job.destinationLabels.Contains(lbl);
            if (inJob) { marker.SetColor(job.color); marker.SetDimmed(false); marker.SetVisible(true); }
            else { marker.SetDimmed(true); marker.SetVisible(IsLabelInAnyOffer(lbl)); }
        }
        else
        {
            // 호버 없음 — 모든 offered 잡 마커를 각 잡 색으로 약하게 노출
            var owner = FirstJobUsingLabel(lbl);
            if (owner != null) { marker.SetColor(owner.color); marker.SetDimmed(true); marker.SetVisible(true); }
            else marker.SetVisible(false);
        }
    }
}

bool IsLabelInAnyOffer(string lbl)
{
    foreach (var j in currentOffer)
        if (j != null && j.destinationLabels != null && j.destinationLabels.Contains(lbl)) return true;
    return false;
}

OrderJob FirstJobUsingLabel(string lbl)
{
    foreach (var j in currentOffer)
        if (j != null && j.destinationLabels != null && j.destinationLabels.Contains(lbl)) return j;
    return null;
}
```

- [ ] **Step 2: Close()의 lastSelectedJob 마커 표시도 label 기반**

```csharp
if (allMarkers != null && lastSelectedJob != null && lastSelectedJob.destinationLabels != null)
{
    for (int i = 0; i < allMarkers.Count; i++)
    {
        if (allMarkers[i] == null) continue;
        bool inSet = lastSelectedJob.destinationLabels.Contains(allMarkers[i].Label);
        allMarkers[i].SetVisible(inSet);
        if (inSet) { allMarkers[i].SetColor(lastSelectedJob.color); allMarkers[i].SetExternalControl(false); }
    }
}
```

- [ ] **Step 3: 컴파일 + Play 검증**

```bash
unity-cli editor refresh --compile && unity-cli console --type error,warning
unity-cli editor play
unity-cli exec "Application.runInBackground=true; UnityEngine.ScreenCapture.CaptureScreenshot(\"C:/Users/stard/GameDevelope/PitCrew/F1/Screenshots/board_labelhover.png\"); return Time.frameCount;"
unity-cli exec "return Time.frameCount;"
```
Expected: 보드가 열려 있고 잡들의 destinationLabels에 해당하는 마커들이 보임 (호버 안 했을 때 각 잡 색으로 dim).

- [ ] **Step 4: 커밋**

```bash
git add F1/Assets/Scripts/Order/OrderBoard.cs
git commit -m "feat(board): 마커를 label 기반으로 + 모든 offered 잡 동시 표시"
```

---

## Task 8 — OrderBoard: 차고 마커 + 호버 시 경로선

**Files:**
- Modify: `F1/Assets/Scripts/Order/OrderBoard.cs`

- [ ] **Step 1: 차고 마커 + LineRenderer**

OrderBoard 상태에 추가:

```csharp
GameObject startMarker;
LineRenderer previewLine;

void EnsureStartMarkerAndLine()
{
    if (startMarker == null)
    {
        var race = GameObject.Find("Race");
        if (race != null)
        {
            startMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            startMarker.name = "OrderBoard_StartMarker";
            DestroyImmediate(startMarker.GetComponent<Collider>());
            startMarker.transform.position = race.transform.position + Vector3.up * 1.5f;
            startMarker.transform.localScale = new Vector3(2f, 3f, 2f);
            var mr = startMarker.GetComponent<MeshRenderer>();
            mr.material.color = new Color(1f, 0.9f, 0.2f);
        }
    }
    if (startMarker != null) startMarker.SetActive(true);

    if (previewLine == null)
    {
        var lrGO = new GameObject("OrderBoard_PreviewLine");
        previewLine = lrGO.AddComponent<LineRenderer>();
        previewLine.material = new Material(Shader.Find("Sprites/Default"));
        previewLine.startWidth = previewLine.endWidth = 0.4f;
        previewLine.positionCount = 0;
        previewLine.useWorldSpace = true;
    }
}

void HideStartMarkerAndLine()
{
    if (startMarker != null) startMarker.SetActive(false);
    if (previewLine != null) previewLine.positionCount = 0;
}

void DrawPreviewLineFor(OrderJob job)
{
    if (previewLine == null || job == null) return;
    if (job.destinationLabels == null || job.destinationLabels.Count == 0) { previewLine.positionCount = 0; return; }
    var race = GameObject.Find("Race");
    if (race == null) return;
    var dm = Services.Delivery as DeliveryManager;
    if (dm == null) return;
    previewLine.startColor = previewLine.endColor = job.color;
    var pts = new List<Vector3> { race.transform.position + Vector3.up * 0.5f };
    foreach (var lbl in job.destinationLabels)
    {
        var p = dm.FindByLabel(lbl);
        if (p != null) pts.Add(p.transform.position + Vector3.up * 0.5f);
    }
    previewLine.positionCount = pts.Count;
    for (int i = 0; i < pts.Count; i++) previewLine.SetPosition(i, pts[i]);
}
```

`Open()` 끝에 `EnsureStartMarkerAndLine();` 호출. `Close()` 시작에 `HideStartMarkerAndLine();`. `OnCardHoverEnter/Exit`/`ApplyHoverState`에서 `DrawPreviewLineFor(hovered job)` / `previewLine.positionCount = 0`.

- [ ] **Step 2: 컴파일 + Play 검증 (screenshot)**

```bash
unity-cli editor refresh --compile && unity-cli console --type error,warning
unity-cli editor play
unity-cli exec "Application.runInBackground=true; var ob=UnityEngine.Object.FindFirstObjectByType<OrderBoard>(); var sf=typeof(OrderBoard).GetField(\"spawnedCards\",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); var cards=(System.Collections.IList)sf.GetValue(ob); var hef=typeof(OrderBoard).GetMethod(\"OnCardHoverEnter\",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); hef.Invoke(ob,new object[]{0}); return Time.frameCount;"
unity-cli exec "UnityEngine.ScreenCapture.CaptureScreenshot(\"C:/Users/stard/GameDevelope/PitCrew/F1/Screenshots/board_routeline.png\"); return Time.frameCount;"
unity-cli exec "return Time.frameCount;"
```
Expected: 차고 마커(노란 기둥) + 호버한 잡 색 경로선.

- [ ] **Step 3: 커밋**

```bash
git add F1/Assets/Scripts/Order/OrderBoard.cs
git commit -m "feat(board): 차고 시작점 마커 + 호버 시 경로선 미리보기"
```

---

## Task 9 — OrderBoard: 카드 우측 스트립 배치 (recovery script)

**Files:**
- Create: `F1/Assets/Editor/_Recovery/Channel8BoardLayoutSetup.cs`

- [ ] **Step 1: 복구 스크립트 — cardsContainer 크기/앵커 조정**

씬의 OrderBoard.cardsContainer를 우측 얇은 스트립으로 재배치. 멱등.

```csharp
using UnityEditor;
using UnityEngine;

public static class Channel8BoardLayoutSetup
{
    [MenuItem("PitCrew/Channel8/Board Layout — 카드 우측 스트립")]
    public static void Run()
    {
        var board = UnityEngine.Object.FindFirstObjectByType<OrderBoard>(FindObjectsInactive.Include);
        if (board == null) { Debug.LogError("[Channel8BoardLayoutSetup] OrderBoard 못 찾음"); return; }
        var f = typeof(OrderBoard).GetField("cardsContainer",
            System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var cc = f.GetValue(board) as RectTransform;
        if (cc == null) { Debug.LogError("[Channel8BoardLayoutSetup] cardsContainer null"); return; }

        // 우측 세로 스트립 — 폭 280, 화면 우측 끝에서 16px
        cc.anchorMin = new Vector2(1, 0); cc.anchorMax = new Vector2(1, 1);
        cc.pivot = new Vector2(1, 0.5f);
        cc.anchoredPosition = new Vector2(-16, 0);
        cc.sizeDelta = new Vector2(280, -80); // 위아래 40 마진
        EditorUtility.SetDirty(cc);

        // 부모 panelRoot 패널이 cardsContainer만 감싸도록 — 부모 RectTransform도 조정
        var pf = typeof(OrderBoard).GetField("panelRoot",
            System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        var pr = pf.GetValue(board) as GameObject;
        if (pr != null)
        {
            var prt = pr.GetComponent<RectTransform>();
            if (prt != null)
            {
                // 패널이 우측 스트립만 차지하도록 (맵이 메인 화면)
                prt.anchorMin = new Vector2(1, 0); prt.anchorMax = new Vector2(1, 1);
                prt.pivot = new Vector2(1, 0.5f);
                prt.anchoredPosition = new Vector2(0, 0);
                prt.sizeDelta = new Vector2(320, 0);
                EditorUtility.SetDirty(prt);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[Channel8BoardLayoutSetup] OrderBoard 카드 우측 스트립으로 조정.");
    }
}
```

- [ ] **Step 2: 메뉴 실행 + 검증**

```bash
unity-cli editor refresh --compile && unity-cli menu "PitCrew/Channel8/Board Layout — 카드 우측 스트립"
unity-cli editor play
unity-cli exec "Application.runInBackground=true; return Time.frameCount;"
unity-cli exec "UnityEngine.ScreenCapture.CaptureScreenshot(\"C:/Users/stard/GameDevelope/PitCrew/F1/Screenshots/board_rightstrip.png\"); return Time.frameCount;"
unity-cli exec "return Time.frameCount;"
unity-cli editor stop
```
Expected: 카드 3장이 우측에 얇게 세로로, 맵이 화면 대부분.

- [ ] **Step 3: 커밋**

```bash
git add F1/Assets/Editor/_Recovery/Channel8BoardLayoutSetup.cs F1/Assets/Editor/_Recovery/Channel8BoardLayoutSetup.cs.meta F1/Assets/Scenes/*.unity
git commit -m "scene(board): 카드 우측 스트립 배치 (맵 중심 레이아웃 A)"
```

---

## Task 10 — OrderBoard → RoutePlanScreen 연결

**Files:**
- Modify: `F1/Assets/Scripts/Order/OrderBoard.cs`

- [ ] **Step 1: OnCardClicked 분기**

```csharp
void OnCardClicked(int cardIndex)
{
    if (!isOpen) return;
    if (cardIndex < 0 || cardIndex >= currentOffer.Count) return;
    if (currentOffer[cardIndex].itemCount > BoxCapacity) return; // 채널 5 게이팅
    lastSelectedJob = currentOffer[cardIndex];

    isOpen = false;
    if (panelRoot != null) panelRoot.SetActive(false);
    HideStartMarkerAndLine();

    if (allMarkers != null)
    {
        for (int i = 0; i < allMarkers.Count; i++)
            if (allMarkers[i] != null) allMarkers[i].SetExternalControl(false);
    }

    // 채널 8 — RoutePlanScreen 우선, 없으면 폴백
    if (Services.RoutePlan != null)
    {
        Services.RoutePlan.Show(lastSelectedJob);
    }
    else if (Services.Packing != null)
    {
        Services.Packing.Show(lastSelectedJob);
    }
    else if (runController != null)
    {
        runController.BeginRun(lastSelectedJob);
    }
}
```

- [ ] **Step 2: 컴파일 + 풀 플로우 Play 검증**

```bash
unity-cli editor refresh --compile && unity-cli console --type error,warning
unity-cli editor play
```

```bash
# OrderBoard 카드 클릭 시뮬 → RoutePlanScreen 떠야 함
unity-cli exec "Application.runInBackground=true; var ob=UnityEngine.Object.FindFirstObjectByType<OrderBoard>(); var sf=typeof(OrderBoard).GetField(\"currentOffer\",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); var offer=(System.Collections.Generic.List<OrderJob>)sf.GetValue(ob); int idx=-1; for(int i=0;i<offer.Count;i++) if(offer[i].itemCount<=Services.Upgrade.BoxCapacity){idx=i;break;} var oc=typeof(OrderBoard).GetMethod(\"OnCardClicked\",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); oc.Invoke(ob,new object[]{idx}); return $\"clicked idx={idx} job={offer[idx].displayName} RoutePlanVisible={Services.RoutePlan.IsVisible}\";"
```
Expected: `RoutePlanVisible=True`.

```bash
# 추천 순서 → 확정 시뮬
unity-cli exec "var rp=Services.RoutePlan as RoutePlanScreen; var sm=typeof(RoutePlanScreen).GetMethod(\"OnSuggest\",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); sm.Invoke(rp,null); var cm=typeof(RoutePlanScreen).GetMethod(\"OnConfirm\",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance); cm.Invoke(rp,null); return $\"PackingVisible={Services.Packing.IsVisible} DM.target={(Services.Delivery as DeliveryManager).CurrentTarget?.Label}\";"
```
Expected: `PackingVisible=True`, CurrentTarget이 추천 첫 목적지.

```bash
unity-cli editor stop
```

- [ ] **Step 3: 커밋**

```bash
git add F1/Assets/Scripts/Order/OrderBoard.cs
git commit -m "feat(board): 카드 클릭 → RoutePlanScreen (폴백 Packing 유지)"
```

---

## Task 11 — 풀 흐름 검증 + 스크린샷

**Files:** 없음 (검증만)

- [ ] **Step 1: 풀 플로우 시나리오**

Order_C_Risky(3물건) 잡으로 끝까지:
1. 보드 열림 (맵 중심, 카드 우측, 차고 마커)
2. 카드 호버 → 그 잡 마커들 강조 + 경로선 미리보기
3. 카드 클릭 → RoutePlanScreen
4. 추천 순서 클릭 → 3마커에 1,2,3 배지 + 경로선
5. 확정 → PackingScreen
6. 포장 → 배치 → 출발
7. 주행 중 DeliveryManager.CurrentTarget이 추천 순서대로 진행

```bash
unity-cli editor play
unity-cli exec "Application.runInBackground=true; return $\"play frame {Time.frameCount}\";"
# 스크린샷 3장
unity-cli exec "UnityEngine.ScreenCapture.CaptureScreenshot(\"C:/Users/stard/GameDevelope/PitCrew/F1/Screenshots/flow_1_board.png\"); return Time.frameCount;"
# (필요 시 OnCardClicked 시뮬해서 다음 단계로)
```

- [ ] **Step 2: 회귀 체크 — 폴백 경로**

OrderBoard 없는 씬(또는 RoutePlan 비활성)에서 RunController가 ActivateSubset 폴백으로 동작하는지 확인.

- [ ] **Step 3: activeContext 갱신 + 커밋**

```bash
# activeContext.md에 W2 후속 채널 8 완료 한 줄 추가
git add .claude/activeContext.md
git commit -m "docs: 채널 8 (경로 계획 흐름) 완료 기록"
```

---

## Self-Review 체크리스트 (작성자용 — 실행 전)

- [ ] Spec의 3 조각(데이터/OrderBoard/RoutePlanScreen) 다 태스크 있음 (Task 1-2/7-9/4-6).
- [ ] 폴백 경로(HasPlannedRoute false → ActivateSubset) Task 3 Step 5에 있음.
- [ ] 시그니처 일관성: `ActivateRoute(List<DeliveryPoint>)`, `FindByLabel(string)`, `HasPlannedRoute`, `IRoutePlanScreen.Show(OrderJob)` — 전체 태스크에서 동일.
- [ ] 멱등성: Channel8RouteSetup, Channel8BoardLayoutSetup 모두 멱등 (여러 번 실행 가능).
- [ ] 검증 단계가 매 태스크에 있음 (unity-cli compile + console + 필요 시 Play exec).
- [ ] 비포함(가는 길 위험, 자유 조합, 차고/부품)은 spec에서 명시 — 태스크 없음 정상.

## 미해결 (도현 기상 후)

- 경로 계획 화면 = **클릭 시퀀스** 선택. 드래그 리스트가 더 좋으면 후속.
- 추천 순서 = 단순 최근접 이웃. 더 똑똑한 게 필요하면 후속.
- 카드 우측 스트립 폭 280 — 화면에서 좁아 보이면 튜닝.
- 운전 중 미니맵 화살표(채널 8)는 이미 경로 따라 다음 목적지 가리킴 — 통합 동작 확인.
