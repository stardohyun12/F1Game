# 화물 원물화 — 와인 파일럿 구현 Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development (권장) 또는 superpowers:executing-plans로 task별 구현. 스텝은 `- [ ]` 체크박스.

**Goal:** 포장 화물을 회색 큐브/카드보드 대신 **원물(와인병)** 로 보여주고, 버블랩 리본이 병 곡면에 붙어 "보호하는 게 보이게" 한다 — Glass 1종 파일럿으로 끝까지 검증.

**Architecture:** `CargoModelCatalog`(SO)가 `CargoType → 모델프리팹`을 들고, `PackingScreen.SpawnItem`이 큐브 대신 카탈로그 모델을 인스턴스. 페인트·배치 드래그 모두 원물 MeshCollider 하나로 처리(트렁크 물리는 rigidbody가 아닌 커스텀 AABB라 오목 콜라이더 무해). 카탈로그 미스/Box는 기존 큐브+카드보드로 폴백.

**Tech Stack:** Unity 6000.3.9f1 / URP / Meshy text-to-3D API / unity-cli 검증.

**검증 규약:** 이 프로젝트는 pytest 없음. 각 task의 "테스트"는 unity-cli(`C:/Users/stard/AppData/Local/unity-cli/unity-cli.exe`)로 compile/play/exec/screenshot. 씬·데이터 wiring 변경은 검증 서브에이전트 dispatch (session.md D-073).

**폴백 분기:** Task 5(커버리지·흡수)에서 UV 엉킴(한 획에 커버리지 급점프, 안 칠한 면 흡수>0)이 확인되면 → spec 2번(숨긴 실린더 프록시) 으로 전환. Task 7에 분기 명시.

---

## 파일 구조

- Create: `scripts/meshy-generate.ps1` — Meshy text-to-3D 호출·폴링·다운로드 (4종 재사용).
- Create: `F1/Assets/Model/AI/wine_bottle_01.fbx` — 생성 자산 (Meshy).
- Create: `F1/Assets/Scripts/Cargo/CargoModelCatalog.cs` — ScriptableObject (CargoType→모델).
- Create: `F1/Assets/Resources/CargoModels/CargoModelCatalog.asset` — SO 인스턴스 (Resources 로드용).
- Create: `F1/Assets/Editor/_Recovery/CargoCatalogSetup.cs` — [MenuItem] 카탈로그 빌드·와이어·검증.
- Modify: `F1/Assets/Scripts/Packing/PackingScreen.cs` — `SpawnItem`/`WirePainterFor`.

---

## Task 1: Meshy 와인병 생성 스크립트 + 자산 (내 영역, 도현 룩 승인)

**Files:**
- Create: `scripts/meshy-generate.ps1`
- Create: `F1/Assets/Model/AI/wine_bottle_01.fbx` (생성물)

- [ ] **Step 1: Meshy 생성 스크립트 작성**

```powershell
# scripts/meshy-generate.ps1
# 사용: ./scripts/meshy-generate.ps1 -Prompt "a single glass wine bottle, corked, realistic" -Name wine_bottle_01
param(
  [Parameter(Mandatory=$true)][string]$Prompt,
  [Parameter(Mandatory=$true)][string]$Name,
  [string]$OutDir = "F1/Assets/Model/AI"
)
$ErrorActionPreference = "Stop"
# .env.local 에서 키 로드
$key = (Get-Content .env.local | Where-Object { $_ -match '^MESHY_API_KEY=' }) -replace '^MESHY_API_KEY=',''
if (-not $key) { throw "MESHY_API_KEY .env.local 에 없음" }
$headers = @{ Authorization = "Bearer $key" }

# 1) preview 태스크 생성
$body = @{ mode="preview"; prompt=$Prompt; art_style="realistic"; should_remesh=$true } | ConvertTo-Json
$prev = Invoke-RestMethod -Method Post -Uri "https://api.meshy.ai/openapi/v2/text-to-3d" -Headers $headers -ContentType "application/json" -Body $body
$pid = $prev.result
Write-Host "preview task: $pid"

function Wait-Task($id) {
  while ($true) {
    $t = Invoke-RestMethod -Uri "https://api.meshy.ai/openapi/v2/text-to-3d/$id" -Headers $headers
    Write-Host "  status=$($t.status) progress=$($t.progress)"
    if ($t.status -eq "SUCCEEDED") { return $t }
    if ($t.status -eq "FAILED") { throw "task $id FAILED" }
    Start-Sleep -Seconds 8
  }
}
$null = Wait-Task $pid

# 2) refine (텍스처+UV 정식)
$rbody = @{ mode="refine"; preview_task_id=$pid } | ConvertTo-Json
$ref = Invoke-RestMethod -Method Post -Uri "https://api.meshy.ai/openapi/v2/text-to-3d" -Headers $headers -ContentType "application/json" -Body $rbody
$rid = $ref.result
Write-Host "refine task: $rid"
$done = Wait-Task $rid

# 3) fbx 다운로드
$url = $done.model_urls.fbx
if (-not $url) { throw "fbx URL 없음 (model_urls: $($done.model_urls | ConvertTo-Json))" }
$dest = Join-Path $OutDir "$Name.fbx"
Invoke-WebRequest -Uri $url -OutFile $dest
Write-Host "saved: $dest"
```

- [ ] **Step 2: 생성 실행**

Run: `pwsh ./scripts/meshy-generate.ps1 -Prompt "a single glass wine bottle, corked, dark green glass, realistic, isolated" -Name wine_bottle_01`
Expected: `saved: F1/Assets/Model/AI/wine_bottle_01.fbx` 출력, 파일 생성.

- [ ] **Step 3: import 후 UV 존재 확인**

Run: `unity-cli editor refresh --compile`
Run: `unity-cli exec "var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(\"Assets/Model/AI/wine_bottle_01.fbx\"); var mf = go.GetComponentInChildren<MeshFilter>(); return mf.sharedMesh.uv.Length + \" uvs / \" + mf.sharedMesh.vertexCount + \" verts\";"`
Expected: uv 개수 ≈ vertex 개수 (>0). 0이면 Meshy 출력에 UV 없음 → 재생성 또는 폴백(Task7) 사전 경고.

- [ ] **Step 4: 도현 룩 승인 (도현 영역)**

Run: `unity-cli exec "..."` 로 임시 씬에 배치 후 `unity-cli screenshot --view scene --output docs/screenshots/wine_bottle_01.png`
도현에게 채팅으로 스샷 1장 + "이 와인병 룩 OK? 재생성?" 제시. **승인 전 Task 2 진입 금지.**

- [ ] **Step 5: Commit**

```bash
git add scripts/meshy-generate.ps1 F1/Assets/Model/AI/wine_bottle_01.fbx F1/Assets/Model/AI/wine_bottle_01.fbx.meta
git commit -m "feat(cargo): Meshy 와인병 생성 스크립트 + wine_bottle_01 자산"
```

---

## Task 2: CargoModelCatalog ScriptableObject

**Files:**
- Create: `F1/Assets/Scripts/Cargo/CargoModelCatalog.cs`

- [ ] **Step 1: SO 클래스 작성**

```csharp
using System;
using UnityEngine;

/// <summary>
/// CargoType → 원물 모델 매핑 (B안, 2026-05-20). 코드 하드코드 금지 (asset.md).
/// modelPrefab=null 이면 기존 큐브+카드보드 폴백 (Box 등).
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Cargo Model Catalog", fileName = "CargoModelCatalog")]
public sealed class CargoModelCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public CargoType cargoType;
        public GameObject modelPrefab; // 원물 fbx 프리팹. null → 큐브 폴백.
        public float uniformScale;     // 포장 작업대에서 보기 좋은 균일 스케일.
    }

    [SerializeField] private Entry[] entries;

    /// <summary>해당 타입의 모델 프리팹 (없으면 null → 호출부가 큐브 폴백).</summary>
    public bool TryGet(CargoType type, out GameObject prefab, out float scale)
    {
        prefab = null; scale = 1f;
        if (entries == null) return false;
        foreach (var e in entries)
            if (e.cargoType == type && e.modelPrefab != null)
            {
                prefab = e.modelPrefab;
                scale = e.uniformScale > 0f ? e.uniformScale : 1f;
                return true;
            }
        return false;
    }
}
```

- [ ] **Step 2: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Run: `unity-cli console --type error,warning`
Expected: 0건.

- [ ] **Step 3: Commit**

```bash
git add F1/Assets/Scripts/Cargo/CargoModelCatalog.cs F1/Assets/Scripts/Cargo/CargoModelCatalog.cs.meta
git commit -m "feat(cargo): CargoModelCatalog SO — CargoType→원물 모델 매핑"
```

---

## Task 3: 카탈로그 에셋 빌드 + Glass→와인병 와이어 (에디터 스크립트)

**Files:**
- Create: `F1/Assets/Editor/_Recovery/CargoCatalogSetup.cs`
- Create: `F1/Assets/Resources/CargoModels/CargoModelCatalog.asset` (스크립트가 생성)

- [ ] **Step 1: 에디터 셋업 스크립트 작성**

```csharp
using UnityEditor;
using UnityEngine;
using System.IO;

public static class CargoCatalogSetup
{
    const string CatalogDir  = "Assets/Resources/CargoModels";
    const string CatalogPath = CatalogDir + "/CargoModelCatalog.asset";
    const string WinePath    = "Assets/Model/AI/wine_bottle_01.fbx";

    [MenuItem("PitCrew/Cargo/Build Model Catalog")]
    public static void Build()
    {
        if (!Directory.Exists(CatalogDir)) Directory.CreateDirectory(CatalogDir);

        var cat = AssetDatabase.LoadAssetAtPath<CargoModelCatalog>(CatalogPath);
        if (cat == null)
        {
            cat = ScriptableObject.CreateInstance<CargoModelCatalog>();
            AssetDatabase.CreateAsset(cat, CatalogPath);
        }

        var wine = AssetDatabase.LoadAssetAtPath<GameObject>(WinePath);
        if (wine == null) { Debug.LogError($"[CargoCatalogSetup] {WinePath} 없음 — Task1 먼저"); return; }

        var entries = new CargoModelCatalog.Entry[]
        {
            new() { cargoType = CargoType.Glass, modelPrefab = wine, uniformScale = 0.5f },
        };
        var so = new SerializedObject(cat);
        // entries 배열 직렬화 주입
        var prop = so.FindProperty("entries");
        prop.arraySize = entries.Length;
        for (int i = 0; i < entries.Length; i++)
        {
            var el = prop.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("cargoType").enumValueIndex = (int)entries[i].cargoType;
            el.FindPropertyRelative("modelPrefab").objectReferenceValue = entries[i].modelPrefab;
            el.FindPropertyRelative("uniformScale").floatValue = entries[i].uniformScale;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cat);
        AssetDatabase.SaveAssets();
        Debug.Log("[CargoCatalogSetup] 카탈로그 빌드 완료 (Glass→wine_bottle_01)");
    }
}
```

- [ ] **Step 2: 컴파일 + 실행**

Run: `unity-cli editor refresh --compile`
Run: `unity-cli console --type error,warning` → 0건
Run: `unity-cli menu "PitCrew/Cargo/Build Model Catalog"`
Expected: 콘솔 "카탈로그 빌드 완료".

- [ ] **Step 3: 에셋 검증**

Run: `unity-cli exec "var c = Resources.Load<CargoModelCatalog>(\"CargoModels/CargoModelCatalog\"); GameObject p; float s; bool ok = c.TryGet(CargoType.Glass, out p, out s); return $\"Glass→{(p==null?\"null\":p.name)} scale={s} ok={ok}\";"`
Expected: `Glass→wine_bottle_01 scale=0.5 ok=True`.

- [ ] **Step 4: Commit**

```bash
git add F1/Assets/Editor/_Recovery/CargoCatalogSetup.cs F1/Assets/Editor/_Recovery/CargoCatalogSetup.cs.meta F1/Assets/Resources/CargoModels
git commit -m "feat(cargo): 모델 카탈로그 에셋 빌드 + Glass→와인병 와이어"
```

---

## Task 4: SpawnItem 카탈로그 조회 + 폴백

**Files:**
- Modify: `F1/Assets/Scripts/Packing/PackingScreen.cs` (`SpawnItem`, 필드 추가)

- [ ] **Step 1: 카탈로그 필드 + 로드 추가**

`PackingScreen` 클래스 필드 영역(예: `WrapMaterialDef[] materials;` 아래)에 추가:

```csharp
        // B안 (2026-05-20) — 원물 모델 카탈로그. null이면 큐브 폴백.
        CargoModelCatalog modelCatalog;
        CargoModelCatalog Catalog =>
            modelCatalog ??= Resources.Load<CargoModelCatalog>("CargoModels/CargoModelCatalog");
```

- [ ] **Step 2: SpawnItem 교체**

`SpawnItem(int idx)` 본문을 아래로 교체 (큐브 폴백 보존):

```csharp
        void SpawnItem(int idx)
        {
            var ctype = (currentOrders != null && idx < currentOrders.Count && currentOrders[idx] != null)
                ? currentOrders[idx].cargoType : CargoType.Box;

            GameObject go;
            if (Catalog != null && Catalog.TryGet(ctype, out var prefab, out var scale))
            {
                // 원물 모델 (B안) — fbx 프리팹 인스턴스. 메시는 자식에 있음.
                go = Object.Instantiate(prefab);
                go.transform.localScale = Vector3.one * scale;
            }
            else
            {
                // 폴백 — 회색 큐브 → 프로시저럴 카드보드 (Box / 카탈로그 미스).
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var rend = go.GetComponent<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = CargoBoxAppearance.CardboardMaterial(ctype);
            }

            go.name = $"PackingItem_{idx}";
            if (cargoSpawnPoint != null) go.transform.SetParent(cargoSpawnPoint, false);
            go.transform.localPosition = Vector3.zero;
            go.AddComponent<WrapState>();
            go.AddComponent<WrapPainter>();
            // painter.Init은 ActivateItem → WirePainterFor에서 처리

            go.SetActive(false);
            spawnedItems.Add(go);
        }
```

- [ ] **Step 3: 컴파일 검증**

Run: `unity-cli editor refresh --compile`
Run: `unity-cli console --type error,warning`
Expected: 0건.

- [ ] **Step 4: Commit**

```bash
git add F1/Assets/Scripts/Packing/PackingScreen.cs
git commit -m "feat(cargo): SpawnItem 카탈로그 조회 — Glass=원물, 미스=큐브 폴백"
```

(화면 변경 commit — `Feels-off:` + `Screenshot:` 는 Task 5 Play 검증에서 첨부)

---

## Task 5: WirePainterFor — 자식 메시 콜라이더 + 페인트/커버리지/흡수 검증

**Files:**
- Modify: `F1/Assets/Scripts/Packing/PackingScreen.cs` (`WirePainterFor`)

- [ ] **Step 1: WirePainterFor를 자식 메시 대응으로 교체**

기존은 `item`(루트)에 MeshCollider를 붙임. fbx는 메시가 자식에 있으니 렌더러 GameObject에 붙인다:

```csharp
        void WirePainterFor(GameObject item)
        {
            var state = item.GetComponent<WrapState>();
            var painter = item.GetComponent<WrapPainter>();
            if (state == null || painter == null) return;

            // 페인트 대상 = 실제 메시를 가진 GameObject (큐브=자기 자신, fbx=자식).
            var rend = item.GetComponentInChildren<MeshRenderer>();
            var meshGO = rend != null ? rend.gameObject : item;

            var box = meshGO.GetComponent<BoxCollider>();
            if (box != null) DestroyImmediate(box);
            var col = meshGO.GetComponent<MeshCollider>();
            if (col == null) col = meshGO.AddComponent<MeshCollider>();

            painter.Init(state, packingCamera, col);
            painter.OnStrokeEnded = OnStrokeCompleted;
        }
```

- [ ] **Step 2: 컴파일 검증**

Run: `unity-cli editor refresh --compile` → `unity-cli console --type error,warning` 0건.

- [ ] **Step 3: 페인트·커버리지·흡수 자동 측정 (UV 건전성 = 폴백 판정)**

Run: `unity-cli editor play --wait`
Run: `unity-cli exec` 로 PackingScreen.Show(Glass 잡) → WrapPainter로 작은 스트로크 1획 시뮬 → 다음 측정:

```
// 측정 1: 한 획 후 커버리지. 0 < cov < 0.4 정상. cov>0.6 = UV 겹침 의심.
// 측정 2: 칠한 면 QueryAbsorption > 0.
// 측정 3: 안 칠한 반대 면 QueryAbsorption == 0. (>0 이면 UV 겹침)
```

Expected: 측정1 < 0.4, 측정2 > 0, 측정3 == 0.
**실패(측정1>0.6 또는 측정3>0) → Task 7 폴백 분기로.**

- [ ] **Step 4: 시각 확인 (스샷)**

Run: Play 중 `unity-cli menu "PitCrew/Capture/Auto Screenshot"` → `docs/screenshots/auto/auto_*.png`
병 곡면에 버블랩 리본이 따라 붙는지 육안 확인. `unity-cli editor stop`.

- [ ] **Step 5: Commit (화면 변경)**

```bash
git add F1/Assets/Scripts/Packing/PackingScreen.cs
git commit -m "$(cat <<'MSG'
feat(cargo): WirePainterFor 자식 메시 대응 — 와인병 곡면 페인트

Feels-off: <Play 후 1개 — 예: 리본이 병목 좁은 곳에서 살짝 뜸 / 병이 작업대에 너무 큼>
Screenshot: docs/screenshots/auto/<파일명>.png
MSG
)"
```

---

## Task 6: 배치 + 트렁크 물리 검증 (와인병 콜라이더)

**Files:** (코드 변경 없을 가능성 높음 — 검증 + 필요 시 미세 수정)

- [ ] **Step 1: 배치 드래그 raycast가 와인병을 잡는지**

배치 단계 진입(`EnterPlacement`)은 `spawnedItems`를 트렁크 자식으로 옮기고 `localScale = scale`로 덮어씀. `BeginTrunkDrag`의 raycast는 부모 체인으로 picked를 찾으므로 자식 메시 hit도 OK (기존 로직). 단 EnterPlacement가 `localScale = Vector3.one * trunkSim.ItemScale`로 강제하니 큐브 기준 스케일이 와인병에 맞는지 확인.

Run: `unity-cli editor play --wait` → 포장→배치 진입 → 드래그 시뮬 → 위치 변경 확인.
Expected: 와인병이 트렁크 바닥에서 드래그됨, 콘솔 에러 0.

- [ ] **Step 2: 트렁크 슬라이드 + concave 에러 0**

Run: `unity-cli exec` 로 BeginRun 후 trunkSim 강한 G 적용 → 콘솔 확인.
Run: `unity-cli console --type error --since 10s`
Expected: PhysX concave/rigidbody 에러 0 (커스텀 AABB라 무해 — 확인용).

- [ ] **Step 3: 손상 차등 (안 싼 병 > 싼 병)**

Run: `unity-cli exec` 로 동일 충격을 (a) 미포장 병 (b) 버블랩 풀커버 병에 적용 → CargoBox HP 감소 비교.
Expected: (a) 손상 > (b) 손상.

- [ ] **Step 4: 필요 시 미세 수정 후 Commit**

스케일/위치 미세 조정이 필요하면 PackingScreen 또는 카탈로그 scale 조정 후:

```bash
git add -A
git commit -m "$(cat <<'MSG'
fix(cargo): 와인병 트렁크 배치 스케일 정합

Feels-off: <1개>
Screenshot: docs/screenshots/auto/<파일명>.png
MSG
)"
```

조정 불필요(검증만)면 commit 생략.

---

## Task 7: 통합 검증 서브에이전트 (D-073) + 폴백 판정

**Files:** (없음 — 검증)

- [ ] **Step 1: 검증 서브에이전트 dispatch**

fresh subagent (sonnet)에게:

```
Editor 켜져 있고 Play 가능. 와인병 화물 파일럿 wiring·데이터 정합 검증, 표로 보고:
1. Resources CargoModelCatalog 로드 + TryGet(Glass) → wine_bottle_01, scale>0
2. PackingScreen.Show(Glass 잡) → spawnedItems[0]가 큐브 아닌 와인병 메시(자식 MeshFilter 존재)
3. WrapPainter 1획 → 커버리지 0<cov<0.4, 칠한 면 흡수>0, 반대 면 흡수==0
4. 배치 진입 → 드래그로 와인병 위치 변경 + 콘솔 에러 0
5. 미포장 vs 풀커버 동일 충격 → 손상 차등 (미포장>포장)
6. editor play --wait → 콘솔 에러 0건
각 항목 PASS/FAIL + FAIL이면 수치/좌표.
```

- [ ] **Step 2: 결과 분기**

- 전 항목 PASS → 파일럿 성공. Task 8로.
- 3번 FAIL(UV 겹침) → **폴백**: 카탈로그에 `paintColliderShape`(Cylinder) 필드 추가 + SpawnItem이 숨긴 실린더 프록시에 페인트, 와인병은 렌더만. (별도 plan으로 분리 — 이 plan 중단하고 재 brainstorm 불필요, 폴백은 spec에 정의됨)
- 그 외 FAIL → 해당 task로 돌아가 fix → 재 dispatch.

작성자(나)는 OK 가정 commit 금지. 서브에이전트 PASS 받고 진행.

---

## Task 8: 세션 기록 + 도현 보고

**Files:**
- Modify: `.claude/activeContext.md`
- Modify: `옵시디언/notes/session-log.md`
- Modify: `옵시디언/하네스/harness-usage-log.md` (B 트랙 한 블록)

- [ ] **Step 1: activeContext 갱신** — 파일럿 결과(성공/폴백), 다음 = 나머지 3종(Eggs/Fish/Cake) 복제.
- [ ] **Step 2: session-log 한 블록.**
- [ ] **Step 3: 도현 채팅 보고** — 스샷 1장 + 결과 1~3줄 (파일 검토 보내지 말 것 [[feedback_one_decision_surface]]).
- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "docs(active): 화물 와인 파일럿 결과 + 복제 계획 [skip-verifier]"
```

---

## 복제 단계 (파일럿 통과 후, 이 plan 범위 밖)

Eggs/Fish/Cake — 각각 `meshy-generate.ps1`로 생성 → 카탈로그 엔트리 추가 → 동일 검증. 별도 작업으로.
