# 3D 포장 미니게임 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** PreRunSetupPanel의 4단계 PackingTier 선택을 3D 자유 랩핑 미니게임으로 갈아치움 — 마우스 드래그로 화물 표면에 일정 폭의 랩 띠를 펼치고, 충격 발생 시 그 지점의 wrap 상태로 손상 흡수율을 계산.

**Architecture:** RenderTexture(256×256, RGB=재료별 채널)에 마우스 raycast 경로의 strip을 그려 wrap 상태를 저장. 셰이더로 화물 표면에 시각 오버레이. 충격 시 충격 방향을 mesh에 raycast → hit UV의 RenderTexture 픽셀 → 최상위 재료 흡수율로 손상 차감. 기존 `PackingTier` enum + `PackingModifier`는 제거, `CargoBox`는 새 `WrapState` 컴포넌트와 통신.

**Tech Stack:** Unity 6 (URP), C#, RenderTexture + Blit shader, procedural UGUI (UITheme.Active 패턴), Service Locator, unity-cli (Editor 켠 채 검증).

**Verification model:** Unity Test Framework 설치돼 있으나 `Assets/Tests/` 없음. 본 플랜은 단위 테스트 대신 **unity-cli 기반 manual verification**으로 진행 — 각 태스크는 빨간 실패 시연을 unity-cli `exec` 또는 `editor play`로 먼저 보이고, 구현 후 같은 명령이 통과하도록 함.

**커밋 규칙:** 모든 step에서 `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` 라인 포함.

---

## File Structure (Create / Modify)

### Create
- `F1/Assets/Scripts/Packing/WrapMaterialDef.cs` — 재료 SO 정의 (이름, 단가, 흡수율, 색)
- `F1/Assets/Resources/WrapMaterials/Bubble.asset` — 부블랩 SO
- `F1/Assets/Resources/WrapMaterials/Foam.asset` — 폼 SO
- `F1/Assets/Resources/WrapMaterials/Hardcase.asset` — 하드케이스 SO
- `F1/Assets/Scripts/Packing/WrapState.cs` — Cargo prefab에 부착, RenderTexture 보유 + 충격 흡수 쿼리
- `F1/Assets/Scripts/Packing/WrapPainter.cs` — raycast + strip 그리기 로직 (RenderTexture에 Blit)
- `F1/Assets/Scripts/Packing/WrapStripBlit.shader` — 두 UV 점 사이 띠를 그리는 fragment shader
- `F1/Assets/Scripts/Packing/WrapOverlay.shader` — 화물 표면에 wrap 상태 표시 (Surface/Unlit, RenderTexture 샘플)
- `F1/Assets/Scripts/Packing/PackingScreen.cs` — 미니게임 UI 패널 (procedural UGUI)
- `F1/Assets/Scripts/Packing/PackingScreenInput.cs` — WASD 회전, 마우스/휠 입력
- `F1/Assets/Scripts/Packing/IPackingScreen.cs` — Service Locator 인터페이스
- `F1/Assets/Editor/_Recovery/PackingDevHarness.cs` — unity-cli 검증용 메뉴

### Modify
- `F1/Assets/Scripts/Cargo/CargoBox.cs` — `PackingTier packing` 필드 제거 → `WrapState wrapState` 참조
- `F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs:212-219` — `TryApplyBoxImpactDamage` 시 충격 방향 인자 받아 WrapState로 흡수율 쿼리
- `F1/Assets/Scripts/Cargo/Modifiers/PackingModifier.cs` — **삭제** (5 임계값 곱셈 더 이상 사용 X)
- `F1/Assets/Scripts/Cargo/PackingTier.cs` — **삭제**
- `F1/Assets/Scripts/Order/PreRunSetupPanel.cs` — **삭제** (PackingScreen으로 교체)
- `F1/Assets/Scripts/Order/PackingCardUI.cs` — **삭제**
- `F1/Assets/Scripts/Order/OrderJob.cs:17,45` — `pointCount` 1 고정 + `defaultPacking` 제거 + `itemCount` 신규 (1~3)
- `F1/Assets/Scripts/Order/OrderBoard.cs:198` — `setup.Show(lastSelectedJob)` → `packing.Show(lastSelectedJob)`
- `F1/Assets/Scripts/Order/RunController.cs:84,93,118-123` — `BeginRun(job, PackingTier?)` 오버로드 제거, `BeginRun(job)` 만 남기고 cargo 셋업에서 WrapState 사용
- `F1/Assets/Scripts/Order/OrderCardUI.cs:87` — `defaultPacking` 참조 제거
- `F1/Assets/Scripts/Core/Core.cs:100-139` — `Packing` 서비스 등록 추가
- `F1/Assets/Scripts/Core/GameContext.cs:41-52` — PackingScreen 등록 호출 추가

---

## Task 1: WrapMaterialDef SO + 3 자산

**Files:**
- Create: `F1/Assets/Scripts/Packing/WrapMaterialDef.cs`
- Create: `F1/Assets/Resources/WrapMaterials/Bubble.asset`
- Create: `F1/Assets/Resources/WrapMaterials/Foam.asset`
- Create: `F1/Assets/Resources/WrapMaterials/Hardcase.asset`

- [ ] **Step 1: WrapMaterialDef SO 작성**

`F1/Assets/Scripts/Packing/WrapMaterialDef.cs`:

```csharp
using UnityEngine;

namespace PitCrew.Packing
{
    [CreateAssetMenu(menuName = "PitCrew/WrapMaterial", fileName = "WrapMaterial")]
    public sealed class WrapMaterialDef : ScriptableObject
    {
        [Tooltip("표시 이름 (한글)")]
        public string displayName = "Bubble";

        [Tooltip("RGB 채널 인덱스. 0=R(부블랩), 1=G(폼), 2=B(하드케이스)")]
        [Range(0, 2)] public int channelIndex = 0;

        [Tooltip("단위 면적(픽셀)당 비용. 띠 한 strip 그릴 때 픽셀 수 × 이 값.")]
        public float costPerPixel = 0.05f;

        [Tooltip("충격 흡수율 0~1. 1이면 완전 흡수.")]
        [Range(0f, 1f)] public float absorption = 0.25f;

        [Tooltip("시각 색상 (UI 스와치 + 표면 오버레이)")]
        public Color displayColor = new Color(0.42f, 0.72f, 0.91f, 0.85f);

        [Tooltip("입력 키 (1/2/3)")]
        public KeyCode hotkey = KeyCode.Alpha1;
    }
}
```

- [ ] **Step 2: unity-cli로 컴파일 확인**

```bash
unity-cli console --errors
```

Expected: WrapMaterialDef.cs 컴파일 오류 0.

- [ ] **Step 3: 3종 SO 자산 생성 (Editor harness 통해)**

`F1/Assets/Editor/_Recovery/PackingDevHarness.cs`:

```csharp
using UnityEditor;
using UnityEngine;
using System.IO;
using PitCrew.Packing;

public static class PackingDevHarness
{
    [MenuItem("PitCrew/Packing/Create WrapMaterial Assets")]
    public static void CreateWrapMaterials()
    {
        Directory.CreateDirectory("Assets/Resources/WrapMaterials");
        Make("Bubble",    0, costPerPixel: 0.03f, absorption: 0.25f, color: new Color(0.42f, 0.72f, 0.91f, 0.85f), key: KeyCode.Alpha1);
        Make("Foam",      1, costPerPixel: 0.07f, absorption: 0.55f, color: new Color(0.91f, 0.84f, 0.42f, 0.95f), key: KeyCode.Alpha2);
        Make("Hardcase",  2, costPerPixel: 0.18f, absorption: 0.85f, color: new Color(0.33f, 0.33f, 0.36f, 1.0f),  key: KeyCode.Alpha3);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void Make(string name, int idx, float costPerPixel, float absorption, Color color, KeyCode key)
    {
        var so = ScriptableObject.CreateInstance<WrapMaterialDef>();
        so.displayName = name; so.channelIndex = idx;
        so.costPerPixel = costPerPixel; so.absorption = absorption;
        so.displayColor = color; so.hotkey = key;
        AssetDatabase.CreateAsset(so, $"Assets/Resources/WrapMaterials/{name}.asset");
    }
}
```

- [ ] **Step 4: 메뉴 실행으로 자산 생성**

```bash
unity-cli menu "PitCrew/Packing/Create WrapMaterial Assets"
```

Expected: 3개 .asset 파일이 `F1/Assets/Resources/WrapMaterials/`에 생성됨.

- [ ] **Step 5: 자산 검증**

```bash
unity-cli exec "var mats = UnityEngine.Resources.LoadAll<PitCrew.Packing.WrapMaterialDef>(\"WrapMaterials\"); return mats.Length + \" mats: \" + string.Join(\",\", System.Linq.Enumerable.Select(mats, m => m.displayName));"
```

Expected: `3 mats: Bubble,Foam,Hardcase` (순서는 무관).

- [ ] **Step 6: 커밋**

```bash
git add F1/Assets/Scripts/Packing/WrapMaterialDef.cs F1/Assets/Editor/_Recovery/PackingDevHarness.cs F1/Assets/Resources/WrapMaterials/
git commit -m "feat(packing): WrapMaterialDef SO + 3종 자산 (D-070)"
```

---

## Task 2: WrapState 컴포넌트 (RenderTexture 초기화)

**Files:**
- Create: `F1/Assets/Scripts/Packing/WrapState.cs`

- [ ] **Step 1: WrapState 작성 — 256×256 RGB RenderTexture 초기화**

`F1/Assets/Scripts/Packing/WrapState.cs`:

```csharp
using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>화물 prefab에 부착. wrap 상태를 RenderTexture에 보관.</summary>
    [DisallowMultipleComponent]
    public sealed class WrapState : MonoBehaviour
    {
        [SerializeField] private int textureSize = 256;
        [SerializeField] private Renderer targetRenderer;

        public RenderTexture WrapRT { get; private set; }
        public int Size => textureSize;

        void Awake()
        {
            WrapRT = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
            WrapRT.wrapMode = TextureWrapMode.Clamp;
            WrapRT.filterMode = FilterMode.Bilinear;
            WrapRT.Create();
            ClearAll();
            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        }

        public void ClearAll()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = WrapRT;
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            RenderTexture.active = prev;
        }

        void OnDestroy()
        {
            if (WrapRT != null) { WrapRT.Release(); WrapRT = null; }
        }
    }
}
```

- [ ] **Step 2: 검증 메뉴 추가**

`PackingDevHarness.cs`에 추가:

```csharp
[MenuItem("PitCrew/Packing/Spawn Test Cube With WrapState")]
public static void SpawnTestCube()
{
    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    go.name = "WrapTestCube";
    go.transform.position = new Vector3(0, 1, 0);
    go.AddComponent<PitCrew.Packing.WrapState>();
    Selection.activeGameObject = go;
}
```

- [ ] **Step 3: 검증**

```bash
unity-cli menu "PitCrew/Packing/Spawn Test Cube With WrapState"
unity-cli exec "var w = UnityEngine.Object.FindObjectOfType<PitCrew.Packing.WrapState>(); return w != null && w.WrapRT != null && w.WrapRT.IsCreated() ? \"OK \" + w.WrapRT.width + \"x\" + w.WrapRT.height : \"FAIL\";"
```

Expected: `OK 256x256`.

- [ ] **Step 4: 정리**

```bash
unity-cli exec "var go = GameObject.Find(\"WrapTestCube\"); if (go) UnityEngine.Object.DestroyImmediate(go); return \"cleaned\";"
```

- [ ] **Step 5: 커밋**

```bash
git add F1/Assets/Scripts/Packing/WrapState.cs F1/Assets/Editor/_Recovery/PackingDevHarness.cs
git commit -m "feat(packing): WrapState 컴포넌트 + RenderTexture 초기화"
```

---

## Task 3: WrapStripBlit 셰이더 — 두 UV 점 사이 띠 그리기

**Files:**
- Create: `F1/Assets/Scripts/Packing/WrapStripBlit.shader`

이 셰이더는 한 strip(이전 hit UV, 현재 hit UV)을 굵기 있는 띠로 RenderTexture에 그림. 출력 RGBA에서 active 채널만 1로 set, 다른 채널은 보존.

- [ ] **Step 1: 셰이더 작성**

`F1/Assets/Scripts/Packing/WrapStripBlit.shader`:

```shader
Shader "PitCrew/WrapStripBlit"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
        _UVPrev ("UV Prev", Vector) = (0,0,0,0)
        _UVCurr ("UV Curr", Vector) = (0,0,0,0)
        _Width  ("Strip Width (UV)", Float) = 0.06
        _ChannelMask ("Channel Mask (R/G/B/A 1=draw)", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _UVPrev;
            float4 _UVCurr;
            float  _Width;
            float4 _ChannelMask;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            // distance from point p to segment a-b in 2D
            float distSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / max(dot(ba, ba), 1e-6));
                return length(pa - ba * h);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
                float d = distSegment(i.uv, _UVPrev.xy, _UVCurr.xy);
                float inside = step(d, _Width * 0.5);
                // active channel: set to 1 inside the strip; other channels preserved
                return lerp(src, src + (_ChannelMask * inside), inside);
            }
            ENDCG
        }
    }
}
```

> 주의: `src + mask*1`은 채널이 이미 1이면 clamp 안 됨 → ARGB32 RT는 자동 saturate (0~1 범위). 안전.

- [ ] **Step 2: 컴파일 확인**

```bash
unity-cli console --errors
```

Expected: shader 컴파일 오류 0.

- [ ] **Step 3: 커밋**

```bash
git add F1/Assets/Scripts/Packing/WrapStripBlit.shader
git commit -m "feat(packing): WrapStripBlit 셰이더 — UV 띠 그리기"
```

---

## Task 4: WrapPainter — raycast + 셰이더로 strip 그리기

**Files:**
- Create: `F1/Assets/Scripts/Packing/WrapPainter.cs`

- [ ] **Step 1: WrapPainter 작성**

`F1/Assets/Scripts/Packing/WrapPainter.cs`:

```csharp
using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>마우스 클릭 누름 동안 화물 표면에 랩 띠를 그림. 카메라와 WrapState 참조.</summary>
    public sealed class WrapPainter : MonoBehaviour
    {
        [SerializeField] private Camera packingCamera;
        [SerializeField] private WrapState targetState;
        [SerializeField] private MeshCollider targetCollider;

        [Header("Painting")]
        [SerializeField] private float stripWidthUV = 0.06f;
        [SerializeField] private float minSampleDistUV = 0.005f;

        Material blitMat;
        RenderTexture scratch;
        WrapMaterialDef currentMat;
        bool stroking;
        Vector2 lastUV;

        public float StripWidthUV { get => stripWidthUV; set => stripWidthUV = Mathf.Clamp(value, 0.01f, 0.25f); }
        public void SetMaterial(WrapMaterialDef def) { currentMat = def; }

        void Awake()
        {
            blitMat = new Material(Shader.Find("PitCrew/WrapStripBlit"));
            scratch = new RenderTexture(targetState.Size, targetState.Size, 0, RenderTextureFormat.ARGB32);
            scratch.Create();
        }

        void OnDestroy() { if (scratch != null) scratch.Release(); if (blitMat != null) Destroy(blitMat); }

        public void BeginStroke(Vector2 screenPos)
        {
            if (TryRaycastUV(screenPos, out var uv)) { lastUV = uv; stroking = true; }
        }

        public void EndStroke() { stroking = false; }

        public void UpdateStroke(Vector2 screenPos)
        {
            if (!stroking || currentMat == null) return;
            if (!TryRaycastUV(screenPos, out var uv)) return;
            if ((uv - lastUV).sqrMagnitude < minSampleDistUV * minSampleDistUV) return;

            DrawStrip(lastUV, uv, currentMat);
            lastUV = uv;
        }

        bool TryRaycastUV(Vector2 screenPos, out Vector2 uv)
        {
            uv = default;
            var ray = packingCamera.ScreenPointToRay(screenPos);
            if (!targetCollider.Raycast(ray, out var hit, 100f)) return false;
            uv = hit.textureCoord;
            return true;
        }

        void DrawStrip(Vector2 prev, Vector2 curr, WrapMaterialDef mat)
        {
            var src = targetState.WrapRT;
            Vector4 mask = new Vector4(
                mat.channelIndex == 0 ? 1 : 0,
                mat.channelIndex == 1 ? 1 : 0,
                mat.channelIndex == 2 ? 1 : 0,
                0);

            blitMat.SetVector("_UVPrev", new Vector4(prev.x, prev.y, 0, 0));
            blitMat.SetVector("_UVCurr", new Vector4(curr.x, curr.y, 0, 0));
            blitMat.SetFloat("_Width", stripWidthUV);
            blitMat.SetVector("_ChannelMask", mask);

            Graphics.Blit(src, scratch, blitMat);
            Graphics.Blit(scratch, src);
        }
    }
}
```

- [ ] **Step 2: dev harness — Cube 페인트 시연 메뉴**

`PackingDevHarness.cs`에 추가:

```csharp
[MenuItem("PitCrew/Packing/Paint Test Strip on Cube")]
public static void PaintStrip()
{
    var go = GameObject.Find("WrapTestCube");
    if (go == null) { Debug.LogError("WrapTestCube 없음. Spawn 먼저."); return; }
    var col = go.GetComponent<MeshCollider>() ?? go.AddComponent<MeshCollider>();
    var state = go.GetComponent<PitCrew.Packing.WrapState>();
    var painter = go.GetComponent<PitCrew.Packing.WrapPainter>() ?? go.AddComponent<PitCrew.Packing.WrapPainter>();
    var bubble = UnityEngine.Resources.Load<PitCrew.Packing.WrapMaterialDef>("WrapMaterials/Bubble");
    // 직접 strip 그리기 (마우스 입력 없이) — 셰이더 동작 확인용
    typeof(PitCrew.Packing.WrapPainter)
        .GetMethod("DrawStrip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        ?.Invoke(painter, new object[] { new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f), bubble });
    Debug.Log("strip drawn");
}
```

> dev harness니까 reflection 사용 OK. 프로덕션 코드 아님.

- [ ] **Step 3: 검증 — 텍스처에 픽셀 칠해졌는지 확인**

```bash
unity-cli menu "PitCrew/Packing/Spawn Test Cube With WrapState"
unity-cli menu "PitCrew/Packing/Paint Test Strip on Cube"
unity-cli exec @"
var w = UnityEngine.Object.FindObjectOfType<PitCrew.Packing.WrapState>();
var tex = new UnityEngine.Texture2D(w.WrapRT.width, w.WrapRT.height, UnityEngine.TextureFormat.RGBA32, false);
var prev = UnityEngine.RenderTexture.active;
UnityEngine.RenderTexture.active = w.WrapRT;
tex.ReadPixels(new UnityEngine.Rect(0, 0, w.WrapRT.width, w.WrapRT.height), 0, 0);
tex.Apply();
UnityEngine.RenderTexture.active = prev;
int red = 0;
var pixels = tex.GetPixels();
foreach (var p in pixels) if (p.r > 0.5f) red++;
return ""red px: "" + red;
"@
```

Expected: `red px: <0보다 큰 수>` (대략 256 * 256 * 0.06 ≈ 3900 정도).

- [ ] **Step 4: 정리 + 커밋**

```bash
unity-cli exec "var go = GameObject.Find(\"WrapTestCube\"); if (go) UnityEngine.Object.DestroyImmediate(go); return \"cleaned\";"
git add F1/Assets/Scripts/Packing/WrapPainter.cs F1/Assets/Editor/_Recovery/PackingDevHarness.cs
git commit -m "feat(packing): WrapPainter — raycast + strip Blit"
```

---

## Task 5: WrapOverlay 셰이더 — 표면에 wrap 상태 시각화

**Files:**
- Create: `F1/Assets/Scripts/Packing/WrapOverlay.shader`

화물 본체에 적용. 메인 색 위에 WrapState.WrapRT를 샘플해 R/G/B 채널을 각 재료 색으로 합성.

- [ ] **Step 1: 셰이더 작성**

`F1/Assets/Scripts/Packing/WrapOverlay.shader`:

```shader
Shader "PitCrew/WrapOverlay"
{
    Properties
    {
        _BaseColor   ("Base Color", Color) = (0.55, 0.45, 0.32, 1)
        _WrapTex     ("Wrap State RT", 2D) = "black" {}
        _ColorR      ("Channel R Color (Bubble)", Color) = (0.42, 0.72, 0.91, 0.85)
        _ColorG      ("Channel G Color (Foam)",   Color) = (0.91, 0.84, 0.42, 0.95)
        _ColorB      ("Channel B Color (Hardcase)", Color) = (0.33, 0.33, 0.36, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            sampler2D _WrapTex;
            fixed4 _ColorR, _ColorG, _ColorB;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 nrm : TEXCOORD1; };

            v2f vert (appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv;
                o.nrm = UnityObjectToWorldNormal(v.normal); return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 w = tex2D(_WrapTex, i.uv);
                // 최상위 재료 = 채널 우선순위 B > G > R
                fixed3 col = _BaseColor.rgb;
                if (w.r > 0.5) col = lerp(col, _ColorR.rgb, _ColorR.a);
                if (w.g > 0.5) col = lerp(col, _ColorG.rgb, _ColorG.a);
                if (w.b > 0.5) col = lerp(col, _ColorB.rgb, _ColorB.a);
                // 간단한 lambert (URP 의존 줄임)
                float ndl = saturate(dot(normalize(i.nrm), normalize(float3(0.3, 1, 0.2))));
                col *= 0.6 + 0.4 * ndl;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
```

- [ ] **Step 2: 검증 — 매뉴 추가**

`PackingDevHarness.cs`:

```csharp
[MenuItem("PitCrew/Packing/Apply Overlay Material To Cube")]
public static void ApplyOverlay()
{
    var go = GameObject.Find("WrapTestCube");
    if (go == null) { Debug.LogError("Cube 없음"); return; }
    var state = go.GetComponent<PitCrew.Packing.WrapState>();
    var rend = go.GetComponent<Renderer>();
    var mat = new Material(Shader.Find("PitCrew/WrapOverlay"));
    mat.SetTexture("_WrapTex", state.WrapRT);
    rend.sharedMaterial = mat;
}
```

- [ ] **Step 3: 검증**

```bash
unity-cli menu "PitCrew/Packing/Spawn Test Cube With WrapState"
unity-cli menu "PitCrew/Packing/Apply Overlay Material To Cube"
unity-cli menu "PitCrew/Packing/Paint Test Strip on Cube"
unity-cli screenshot --view scene --output strip_overlay.png
```

Expected: 스크린샷에 큐브 가로로 옅은 파랑 띠 보임.

- [ ] **Step 4: 정리 + 커밋**

```bash
unity-cli exec "var go = GameObject.Find(\"WrapTestCube\"); if (go) UnityEngine.Object.DestroyImmediate(go); return \"cleaned\";"
git add F1/Assets/Scripts/Packing/WrapOverlay.shader F1/Assets/Editor/_Recovery/PackingDevHarness.cs
git commit -m "feat(packing): WrapOverlay 셰이더 — 표면 시각화"
```

---

## Task 6: 커버리지 + 비용 쿼리

**Files:**
- Modify: `F1/Assets/Scripts/Packing/WrapState.cs` — 커버리지 % 계산 + 비용 누적 캐시

- [ ] **Step 1: WrapState에 비용/커버리지 API 추가**

`WrapState.cs`에 추가:

```csharp
// 마지막 strip 그릴 때 누적된 비용 (재료 채널별)
public float[] CostByChannel = new float[3];

// 평균 커버 비율 (0~1). 픽셀 비싸므로 정해진 주기에만 호출.
public float ComputeCoverage()
{
    var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
    var prev = RenderTexture.active;
    RenderTexture.active = WrapRT;
    tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
    tex.Apply();
    RenderTexture.active = prev;
    int covered = 0;
    var px = tex.GetPixels();
    foreach (var p in px) if (p.r > 0.5f || p.g > 0.5f || p.b > 0.5f) covered++;
    Object.Destroy(tex);
    return (float)covered / px.Length;
}

public void AccumulateCost(int channelIndex, float amount)
{
    if (channelIndex < 0 || channelIndex > 2) return;
    CostByChannel[channelIndex] += amount;
}

public float TotalCost => CostByChannel[0] + CostByChannel[1] + CostByChannel[2];
```

> `ComputeCoverage`는 readback이므로 매 프레임 호출 금지. PackingScreen에서는 strip 끝낼 때(EndStroke)만 호출.

- [ ] **Step 2: WrapPainter에서 strip 그릴 때 비용 누적**

`WrapPainter.DrawStrip` 마지막에:

```csharp
// 비용 추정: 띠 길이(UV 거리) × 폭 × 텍스처 면적 × 단가
float lenUV = (curr - prev).magnitude;
float pixelArea = lenUV * stripWidthUV * targetState.Size * targetState.Size;
targetState.AccumulateCost(mat.channelIndex, pixelArea * mat.costPerPixel);
```

- [ ] **Step 3: 검증**

```bash
unity-cli menu "PitCrew/Packing/Spawn Test Cube With WrapState"
unity-cli menu "PitCrew/Packing/Paint Test Strip on Cube"
unity-cli exec @"
var w = UnityEngine.Object.FindObjectOfType<PitCrew.Packing.WrapState>();
return $""coverage={w.ComputeCoverage():F3} cost={w.TotalCost:F1} R={w.CostByChannel[0]:F1}"";
"@
```

Expected: `coverage=0.0XX cost=<양수> R=<양수>` (G=B=0).

- [ ] **Step 4: 정리 + 커밋**

```bash
unity-cli exec "var go = GameObject.Find(\"WrapTestCube\"); if (go) UnityEngine.Object.DestroyImmediate(go); return \"cleaned\";"
git add F1/Assets/Scripts/Packing/WrapState.cs F1/Assets/Scripts/Packing/WrapPainter.cs
git commit -m "feat(packing): 커버리지 + 비용 쿼리"
```

---

## Task 7: 충격 흡수율 쿼리

**Files:**
- Modify: `F1/Assets/Scripts/Packing/WrapState.cs`

- [ ] **Step 1: 충격 점(world space) → 흡수율 쿼리 메서드 추가**

`WrapState.cs`에 추가:

```csharp
[Header("Damage Query")]
[SerializeField] private float queryRadiusUV = 0.05f;

WrapMaterialDef[] cachedMats;

WrapMaterialDef[] LoadMats() => cachedMats ??= Resources.LoadAll<WrapMaterialDef>("WrapMaterials");

/// <summary>충격 점 world position 받아 흡수율 0~1 반환.</summary>
public float QueryAbsorption(Vector3 worldHitPoint, Vector3 incomingDir)
{
    var col = targetRenderer != null ? targetRenderer.GetComponent<MeshCollider>() : null;
    if (col == null) col = GetComponentInChildren<MeshCollider>();
    if (col == null) return 0f;

    // 충격 방향의 반대 방향에서 raycast해 표면 UV 추출
    var origin = worldHitPoint - incomingDir.normalized * 0.5f;
    if (!col.Raycast(new Ray(origin, incomingDir.normalized), out var hit, 1f)) return 0f;

    var uv = hit.textureCoord;
    return SampleAbsorptionAtUV(uv);
}

float SampleAbsorptionAtUV(Vector2 uv)
{
    // 1픽셀 readback (4×4 영역 평균)
    int x = Mathf.Clamp(Mathf.RoundToInt(uv.x * textureSize), 2, textureSize - 3);
    int y = Mathf.Clamp(Mathf.RoundToInt(uv.y * textureSize), 2, textureSize - 3);
    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
    var prev = RenderTexture.active;
    RenderTexture.active = WrapRT;
    tex.ReadPixels(new Rect(x - 2, y - 2, 4, 4), 0, 0);
    tex.Apply();
    RenderTexture.active = prev;

    var pixels = tex.GetPixels();
    float maxR = 0, maxG = 0, maxB = 0;
    foreach (var p in pixels) { maxR = Mathf.Max(maxR, p.r); maxG = Mathf.Max(maxG, p.g); maxB = Mathf.Max(maxB, p.b); }
    Object.Destroy(tex);

    var mats = LoadMats();
    // 최상위 = B > G > R 우선
    if (maxB > 0.5f) return AbsOf(mats, 2);
    if (maxG > 0.5f) return AbsOf(mats, 1);
    if (maxR > 0.5f) return AbsOf(mats, 0);
    return 0f;
}

float AbsOf(WrapMaterialDef[] mats, int channel)
{
    foreach (var m in mats) if (m.channelIndex == channel) return m.absorption;
    return 0f;
}
```

- [ ] **Step 2: 검증**

```bash
unity-cli menu "PitCrew/Packing/Spawn Test Cube With WrapState"
unity-cli menu "PitCrew/Packing/Apply Overlay Material To Cube"
unity-cli menu "PitCrew/Packing/Paint Test Strip on Cube"
unity-cli exec @"
var w = UnityEngine.Object.FindObjectOfType<PitCrew.Packing.WrapState>();
var t = w.transform;
// 띠는 (0.2,0.5)~(0.8,0.5) → cube 정면 중앙. 정면 z- 방향에서 충격.
var hitPt = t.position + new Vector3(0, 0, -0.5f);
var dir = new Vector3(0, 0, 1);
float abs = w.QueryAbsorption(hitPt, dir);
return $""absorption={abs:F2}"";
"@
```

Expected: `absorption=0.25` (Bubble 흡수율).

- [ ] **Step 3: 정리 + 커밋**

```bash
unity-cli exec "var go = GameObject.Find(\"WrapTestCube\"); if (go) UnityEngine.Object.DestroyImmediate(go); return \"cleaned\";"
git add F1/Assets/Scripts/Packing/WrapState.cs
git commit -m "feat(packing): 충격 흡수율 쿼리 (world hit → UV → 최상위 재료)"
```

---

## Task 8: PackingScreen — 미니게임 UI (procedural UGUI)

**Files:**
- Create: `F1/Assets/Scripts/Packing/IPackingScreen.cs`
- Create: `F1/Assets/Scripts/Packing/PackingScreen.cs`

ResultPanel/PreRunSetupPanel 패턴 따라 동적 Canvas 생성. Show(OrderJob) 받으면 화물 prefab/primitive 생성 + 사이드 패널 빌드 + 입력 활성화.

- [ ] **Step 1: 인터페이스**

`F1/Assets/Scripts/Packing/IPackingScreen.cs`:

```csharp
using PitCrew.Orders;

namespace PitCrew.Packing
{
    public interface IPackingScreen
    {
        void Show(OrderJob job);
        void Hide();
        bool IsVisible { get; }
    }
}
```

> `OrderJob`의 namespace는 `PitCrew.Orders` 가정. 실제 코드 확인 후 맞춤 — `F1/Assets/Scripts/Order/OrderJob.cs` 1~10라인.

- [ ] **Step 2: PackingScreen 골격 — Canvas + 패널 컨테이너 + Show/Hide**

`F1/Assets/Scripts/Packing/PackingScreen.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PitCrew.Orders; // OrderJob

namespace PitCrew.Packing
{
    public sealed class PackingScreen : MonoBehaviour, IPackingScreen
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Camera packingCamera;
        [SerializeField] private Transform cargoSpawnPoint;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        OrderJob currentJob;
        List<GameObject> spawnedItems = new();
        int activeItemIndex;

        // UI
        TMP_Text titleText, totalCostText, coverageText;
        Image coverageBarFill;
        TMP_Text[] costPerMaterialText = new TMP_Text[3];

        public void Show(OrderJob job)
        {
            currentJob = job;
            BuildUIIfNeeded();
            ClearItems();
            int n = Mathf.Clamp(job.itemCount, 1, 3);
            for (int i = 0; i < n; i++) SpawnItem(i);
            ActivateItem(0);
            panelRoot.SetActive(true);
            packingCamera.gameObject.SetActive(true);
        }

        public void Hide()
        {
            panelRoot.SetActive(false);
            packingCamera.gameObject.SetActive(false);
            ClearItems();
        }

        void BuildUIIfNeeded()
        {
            if (panelRoot != null) return;
            // 최소 UGUI 트리 — 자세한 위젯은 Task 9에서 채움
            var canvasGO = new GameObject("PackingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            panelRoot = canvasGO;
        }

        void ClearItems()
        {
            foreach (var go in spawnedItems) if (go) Destroy(go);
            spawnedItems.Clear();
        }

        void SpawnItem(int idx)
        {
            // 데모: primitive cube. 추후 OrderJob.cargoPrefab.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"PackingItem_{idx}";
            go.transform.SetParent(cargoSpawnPoint, false);
            go.transform.localPosition = Vector3.zero;
            go.AddComponent<WrapState>();
            go.AddComponent<WrapPainter>();
            go.SetActive(false);
            spawnedItems.Add(go);
        }

        void ActivateItem(int idx)
        {
            for (int i = 0; i < spawnedItems.Count; i++) spawnedItems[i].SetActive(i == idx);
            activeItemIndex = idx;
        }

        public GameObject ActiveItem => spawnedItems.Count == 0 ? null : spawnedItems[activeItemIndex];
    }
}
```

- [ ] **Step 3: 검증 — Show/Hide 동작**

```bash
unity-cli exec @"
var go = new UnityEngine.GameObject(""PackingHost"");
var s = go.AddComponent<PitCrew.Packing.PackingScreen>();
var job = UnityEngine.Resources.Load<PitCrew.Orders.OrderJob>(""Orders/Order_A_Safe"") ?? UnityEngine.ScriptableObject.CreateInstance<PitCrew.Orders.OrderJob>();
job.itemCount = 2;
s.Show(job);
bool v = s.IsVisible;
s.Hide();
UnityEngine.Object.DestroyImmediate(go);
return $""visible={v}"";
"@
```

Expected: `visible=True`. (OrderJob.itemCount 필드는 Task 12에서 추가 — 이 시점에는 컴파일 오류 가능. 본 검증 step은 Task 12 이후로 미뤄도 됨.)

- [ ] **Step 4: 커밋**

```bash
git add F1/Assets/Scripts/Packing/IPackingScreen.cs F1/Assets/Scripts/Packing/PackingScreen.cs
git commit -m "feat(packing): PackingScreen 골격 — Canvas + 다물건 spawn"
```

---

## Task 9: PackingScreen UI 위젯 — 좌측 재료, 우측 비용/커버, 상단 탭/출발

**Files:**
- Modify: `F1/Assets/Scripts/Packing/PackingScreen.cs`

UITheme.Active 색상 사용. ResultPanel.cs 패턴 그대로.

- [ ] **Step 1: UI 헬퍼 추가 — 사이드 패널 빌더**

`PackingScreen.cs`에 메서드 추가:

```csharp
[Header("Materials")]
[SerializeField] private WrapMaterialDef[] materials;

GameObject leftPanel, rightPanel, topBar;
GameObject[] materialCardGOs;
TMP_Text widthLabel;

WrapMaterialDef CurrentMaterial { get; set; }

void BuildSidePanels()
{
    var theme = UITheme.Active;
    materials = Resources.LoadAll<WrapMaterialDef>("WrapMaterials");

    // 좌측 재료 선반
    leftPanel = NewPanel("LeftPanel", new Vector2(-820, 0), new Vector2(320, 900));
    materialCardGOs = new GameObject[materials.Length];
    for (int i = 0; i < materials.Length; i++)
    {
        int idx = i;
        var card = NewButton($"Mat_{materials[i].displayName}", leftPanel.transform,
            new Vector2(0, 380 - i * 200), new Vector2(280, 170),
            label: $"{materials[i].displayName}\n₩{Mathf.RoundToInt(materials[i].costPerPixel * 1000)}",
            onClick: () => SelectMaterial(materials[idx]));
        materialCardGOs[i] = card;
    }
    widthLabel = NewLabel("WidthLabel", leftPanel.transform, new Vector2(0, -380), new Vector2(280, 60), "랩 폭 ▬▬", 22);

    // 우측 비용/커버
    rightPanel = NewPanel("RightPanel", new Vector2(820, 0), new Vector2(320, 900));
    for (int i = 0; i < 3; i++)
        costPerMaterialText[i] = NewLabel($"Cost_{i}", rightPanel.transform, new Vector2(0, 350 - i * 60), new Vector2(280, 50),
                                          $"{materials[i].displayName}  ₩0", 18);
    totalCostText = NewLabel("Total", rightPanel.transform, new Vector2(0, 100), new Vector2(280, 60), "합계  ₩0", 24);
    coverageBarFill = NewBar("CovBar", rightPanel.transform, new Vector2(0, -100), new Vector2(280, 30));
    coverageText = NewLabel("CovText", rightPanel.transform, new Vector2(0, -150), new Vector2(280, 60), "평균 커버  0%", 22);

    // 상단 바
    topBar = NewPanel("TopBar", new Vector2(0, 470), new Vector2(1700, 80));
    titleText = NewLabel("Title", topBar.transform, new Vector2(-700, 0), new Vector2(400, 60), "◀  물건 1/1  ▶", 22);
    NewButton("Go", topBar.transform, new Vector2(750, 0), new Vector2(200, 60), "출발하기", () => OnGoClicked());
}

GameObject NewPanel(string name, Vector2 anchored, Vector2 size) { /* RectTransform 생성 + Image fill */ throw new System.NotImplementedException(); }
GameObject NewButton(string name, Transform parent, Vector2 pos, Vector2 size, string label, System.Action onClick) { throw new System.NotImplementedException(); }
TMP_Text NewLabel(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize) { throw new System.NotImplementedException(); }
Image NewBar(string name, Transform parent, Vector2 pos, Vector2 size) { throw new System.NotImplementedException(); }

void SelectMaterial(WrapMaterialDef m) { CurrentMaterial = m; foreach (var p in ActiveItem.GetComponents<WrapPainter>()) p.SetMaterial(m); }
void OnGoClicked() { /* Task 11에서 RunController.BeginRun */ }
```

> 위 `NewPanel/NewButton/NewLabel/NewBar`은 ResultPanel.cs의 동일 헬퍼 패턴을 복사·이식. ResultPanel.cs 라인 끝 부근의 UGUI 헬퍼 (대략 line 200~340 사이) 참조.

- [ ] **Step 2: UGUI 헬퍼 메서드 본문 구현 (ResultPanel.cs에서 복사)**

ResultPanel.cs을 열어 `NewLabel`/`NewButton` 식의 헬퍼를 그대로 PackingScreen.cs로 가져옴. 시그니처만 위와 맞춤.

- [ ] **Step 3: BuildUIIfNeeded에서 BuildSidePanels 호출**

```csharp
void BuildUIIfNeeded()
{
    if (panelRoot != null) return;
    // ... Canvas 생성 ...
    panelRoot = canvasGO;
    BuildSidePanels();
}
```

- [ ] **Step 4: 검증 — 스크린샷**

```bash
unity-cli exec @"
var host = new UnityEngine.GameObject(""PackingHost"");
var s = host.AddComponent<PitCrew.Packing.PackingScreen>();
var job = UnityEngine.ScriptableObject.CreateInstance<PitCrew.Orders.OrderJob>();
job.itemCount = 1;
s.Show(job);
return ""shown"";
"@
unity-cli screenshot --view game --output packing_screen.png
```

Expected: 좌측 재료 카드 3개, 우측 비용 라벨 + 커버 바, 상단 타이틀 + 출발 버튼 보임.

- [ ] **Step 5: 정리 + 커밋**

```bash
unity-cli exec "var go = GameObject.Find(\"PackingHost\"); if (go) UnityEngine.Object.DestroyImmediate(go); return \"cleaned\";"
git add F1/Assets/Scripts/Packing/PackingScreen.cs
git commit -m "feat(packing): PackingScreen UI 위젯 — 재료 카드 + 비용/커버 패널"
```

---

## Task 10: PackingScreenInput — WASD 회전, 마우스 drag, 휠, 1/2/3

**Files:**
- Create: `F1/Assets/Scripts/Packing/PackingScreenInput.cs`
- Modify: `F1/Assets/Scripts/Packing/PackingScreen.cs` — input 컴포넌트 부착

- [ ] **Step 1: PackingScreenInput**

```csharp
using UnityEngine;

namespace PitCrew.Packing
{
    public sealed class PackingScreenInput : MonoBehaviour
    {
        [SerializeField] private float yawSpeedDegPerSec = 90f;
        [SerializeField] private float pitchSpeedDegPerSec = 90f;
        [SerializeField] private float minWidthUV = 0.02f;
        [SerializeField] private float maxWidthUV = 0.18f;
        [SerializeField] private float wheelStep = 0.01f;

        public Transform RotateTarget { get; set; }
        public WrapPainter Painter { get; set; }
        public PackingScreen Screen { get; set; }

        void Update()
        {
            if (Screen == null || !Screen.IsVisible || RotateTarget == null) return;

            // WASD 회전
            float yaw = 0, pitch = 0;
            if (Input.GetKey(KeyCode.A)) yaw -= 1;
            if (Input.GetKey(KeyCode.D)) yaw += 1;
            if (Input.GetKey(KeyCode.W)) pitch -= 1;
            if (Input.GetKey(KeyCode.S)) pitch += 1;
            RotateTarget.Rotate(Vector3.up, yaw * yawSpeedDegPerSec * Time.deltaTime, Space.World);
            RotateTarget.Rotate(Vector3.right, pitch * pitchSpeedDegPerSec * Time.deltaTime, Space.World);
            if (Input.GetKeyDown(KeyCode.R)) RotateTarget.rotation = Quaternion.identity;

            // 재료 전환
            if (Input.GetKeyDown(KeyCode.Alpha1)) Screen.SelectMaterialByChannel(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Screen.SelectMaterialByChannel(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Screen.SelectMaterialByChannel(2);

            // 마우스 입력
            if (Painter != null)
            {
                if (Input.GetMouseButtonDown(0)) Painter.BeginStroke(Input.mousePosition);
                else if (Input.GetMouseButton(0)) Painter.UpdateStroke(Input.mousePosition);
                else if (Input.GetMouseButtonUp(0)) Painter.EndStroke();

                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f)
                    Painter.StripWidthUV = Mathf.Clamp(Painter.StripWidthUV + wheel * wheelStep, minWidthUV, maxWidthUV);
            }
        }
    }
}
```

- [ ] **Step 2: PackingScreen에 input 연결 + SelectMaterialByChannel**

`PackingScreen.cs`에 추가:

```csharp
PackingScreenInput input;

void EnsureInput()
{
    if (input == null) input = gameObject.AddComponent<PackingScreenInput>();
    input.Screen = this;
}

public void SelectMaterialByChannel(int ch)
{
    foreach (var m in materials) if (m.channelIndex == ch) { SelectMaterial(m); break; }
}
```

`ActivateItem` 끝에:

```csharp
EnsureInput();
input.RotateTarget = spawnedItems[idx].transform;
input.Painter = spawnedItems[idx].GetComponent<WrapPainter>();
```

- [ ] **Step 3: 검증 — Play 모드에서 동작**

```bash
unity-cli editor play
# (15초 대기 — 도현이 마우스로 그림)
unity-cli editor stop
unity-cli exec @"
var s = UnityEngine.Object.FindObjectOfType<PitCrew.Packing.PackingScreen>();
var item = s != null ? s.ActiveItem : null;
var w = item != null ? item.GetComponent<PitCrew.Packing.WrapState>() : null;
return w != null ? $""coverage={w.ComputeCoverage():F3}"" : ""no item"";
"@
```

Expected: `coverage > 0` (마우스로 어떻게든 칠했다면).

- [ ] **Step 4: 커밋**

```bash
git add F1/Assets/Scripts/Packing/PackingScreenInput.cs F1/Assets/Scripts/Packing/PackingScreen.cs
git commit -m "feat(packing): PackingScreenInput — WASD/마우스/휠/1·2·3"
```

---

## Task 11: 실시간 비용/커버리지 표시 + 출발 흐름

**Files:**
- Modify: `F1/Assets/Scripts/Packing/PackingScreen.cs`

- [ ] **Step 1: LateUpdate에서 UI 갱신**

```csharp
void LateUpdate()
{
    if (!IsVisible) return;
    var w = ActiveItem != null ? ActiveItem.GetComponent<WrapState>() : null;
    if (w == null) return;
    for (int i = 0; i < 3; i++)
        costPerMaterialText[i].text = $"{materials[i].displayName}  ₩{Mathf.RoundToInt(w.CostByChannel[i])}";
    totalCostText.text = $"합계  ₩{Mathf.RoundToInt(w.TotalCost)}";
}
```

- [ ] **Step 2: 커버리지는 strip 끝(EndStroke)에만 갱신 — readback 비용 큼**

`WrapPainter.EndStroke()` 끝에 콜백:

```csharp
public System.Action<WrapState> OnStrokeEnded;

public void EndStroke()
{
    if (stroking) OnStrokeEnded?.Invoke(targetState);
    stroking = false;
}
```

`PackingScreen.ActivateItem`에서:

```csharp
input.Painter.OnStrokeEnded = (st) =>
{
    float cov = st.ComputeCoverage();
    coverageText.text = $"평균 커버  {Mathf.RoundToInt(cov * 100)}%";
    coverageBarFill.fillAmount = cov;
};
```

- [ ] **Step 3: 출발 버튼 클릭 → RunController.BeginRun**

```csharp
void OnGoClicked()
{
    Hide();
    Services.Run.BeginRun(currentJob); // PackingTier 인자 더이상 없음
}
```

> Services 위치: `F1/Assets/Scripts/Core/Core.cs:100`. `Services.Run`이 `IRunController` 반환.

- [ ] **Step 4: 검증**

```bash
unity-cli editor play
# 마우스 드래그 후 떼기 → 우측 패널 숫자 갱신 확인
unity-cli screenshot --view game --output packing_live.png
unity-cli editor stop
```

Expected: 스크린샷에 비용 ₩XXX, 커버 N% 보임.

- [ ] **Step 5: 커밋**

```bash
git add F1/Assets/Scripts/Packing/PackingScreen.cs F1/Assets/Scripts/Packing/WrapPainter.cs
git commit -m "feat(packing): 실시간 비용 + 커버 % + 출발 흐름"
```

---

## Task 12: OrderJob 정리 + itemCount 필드 + 다물건 탭

**Files:**
- Modify: `F1/Assets/Scripts/Order/OrderJob.cs:17,45`
- Modify: `F1/Assets/Scripts/Order/OrderCardUI.cs:87`
- Modify: `F1/Assets/Scripts/Packing/PackingScreen.cs` — 상단 탭 클릭으로 물건 전환

- [ ] **Step 1: OrderJob 갱신**

`OrderJob.cs:17` 부근:
- `pointCount` 제거 (또는 hard-coded 1 반환 property로 남김)
- `defaultPacking` 필드 제거 (Task 14의 PackingTier 삭제와 연동)
- 신규 필드 추가:

```csharp
[Header("Items")]
[Tooltip("이 잡에서 포장할 물건 수 (1~3)")]
[Range(1, 3)] public int itemCount = 1;
```

- [ ] **Step 2: OrderCardUI 갱신**

`OrderCardUI.cs:87`에서 `defaultPacking` 참조 제거. 카드 UI에 표시되던 packing 정보 → "물건 N개"로 교체.

- [ ] **Step 3: PackingScreen — 상단 탭 클릭 + 좌우 화살표**

`PackingScreen.cs`의 `titleText`를 두 화살표 버튼 + 라벨 3원소로 분리:

```csharp
Button prevItemBtn, nextItemBtn;
// BuildSidePanels에서:
prevItemBtn = NewButton("Prev", topBar.transform, new Vector2(-800, 0), new Vector2(60, 60), "◀", () => CycleItem(-1)).GetComponent<Button>();
titleText = NewLabel("Title", topBar.transform, new Vector2(-700, 0), new Vector2(400, 60), "물건 1/1", 22);
nextItemBtn = NewButton("Next", topBar.transform, new Vector2(-600, 0), new Vector2(60, 60), "▶", () => CycleItem(+1)).GetComponent<Button>();

void CycleItem(int delta)
{
    int n = spawnedItems.Count;
    if (n <= 1) return;
    int next = (activeItemIndex + delta + n) % n;
    ActivateItem(next);
}
```

`ActivateItem`에서 titleText 갱신:

```csharp
titleText.text = $"물건 {idx + 1} / {spawnedItems.Count}";
```

- [ ] **Step 4: 검증 — itemCount=3 잡으로 탭 전환**

```bash
unity-cli exec @"
var job = UnityEngine.ScriptableObject.CreateInstance<PitCrew.Orders.OrderJob>();
job.itemCount = 3;
var host = new UnityEngine.GameObject(""PackingHost"");
var s = host.AddComponent<PitCrew.Packing.PackingScreen>();
s.Show(job);
// 다음 물건으로 전환
s.SendMessage(""CycleItem"", 1);
return s.ActiveItem != null ? s.ActiveItem.name : ""none"";
"@
```

Expected: `PackingItem_1`.

- [ ] **Step 5: 정리 + 커밋**

```bash
unity-cli exec "var go = GameObject.Find(\"PackingHost\"); if (go) UnityEngine.Object.DestroyImmediate(go); return \"cleaned\";"
git add F1/Assets/Scripts/Order/OrderJob.cs F1/Assets/Scripts/Order/OrderCardUI.cs F1/Assets/Scripts/Packing/PackingScreen.cs
git commit -m "feat(packing): itemCount + 다물건 탭 전환"
```

---

## Task 13: OrderBoard → PackingScreen 전환 흐름

**Files:**
- Modify: `F1/Assets/Scripts/Order/OrderBoard.cs:198`
- Modify: `F1/Assets/Scripts/Core/Core.cs:100-139`
- Modify: `F1/Assets/Scripts/Core/GameContext.cs:41-52`

- [ ] **Step 1: Services에 Packing 추가**

`Core.cs` Services 클래스:

```csharp
public static IPackingScreen Packing { get; private set; }
public static void RegisterPacking(IPackingScreen p) => Packing = p;
```

`GameContext.Awake` 등록 흐름에 추가:

```csharp
var packing = FindObjectOfType<PackingScreen>();
if (packing != null) Services.RegisterPacking(packing);
```

- [ ] **Step 2: OrderBoard — PreRunSetupPanel 호출을 PackingScreen으로**

`OrderBoard.cs:198`:

```csharp
// 이전: setup.Show(lastSelectedJob);
Services.Packing.Show(lastSelectedJob);
```

(Polyfill 분기 `runController.BeginRun(lastSelectedJob)` line 203은 일단 유지 — Task 14에서 정리.)

- [ ] **Step 3: 검증 — 잡 카드 클릭 → 포장 화면 뜸**

```bash
unity-cli editor play
# 도현: 잡 카드 클릭
unity-cli screenshot --view game --output flow_to_packing.png
unity-cli editor stop
```

Expected: 잡 보드 사라지고 포장 화면 뜸.

- [ ] **Step 4: 커밋**

```bash
git add F1/Assets/Scripts/Order/OrderBoard.cs F1/Assets/Scripts/Core/Core.cs F1/Assets/Scripts/Core/GameContext.cs
git commit -m "feat(packing): OrderBoard → PackingScreen 흐름 + Services 등록"
```

---

## Task 14: PackingTier 시스템 제거

**Files:**
- Delete: `F1/Assets/Scripts/Cargo/PackingTier.cs`
- Delete: `F1/Assets/Scripts/Cargo/Modifiers/PackingModifier.cs`
- Delete: `F1/Assets/Scripts/Order/PreRunSetupPanel.cs`
- Delete: `F1/Assets/Scripts/Order/PackingCardUI.cs`
- Modify: `F1/Assets/Scripts/Cargo/CargoBox.cs:32,194` — packing 필드 + 참조 제거
- Modify: `F1/Assets/Scripts/Order/RunController.cs:84,93,118-123` — `BeginRun(OrderJob, PackingTier?)` 오버로드 제거

- [ ] **Step 1: 참조 확인**

```bash
unity-cli console --errors
grep -rn "PackingTier" F1/Assets/Scripts/
grep -rn "PackingModifier" F1/Assets/Scripts/
grep -rn "PreRunSetupPanel" F1/Assets/Scripts/
```

Expected: 사용처 목록. 각각 어디 남았는지 본 후 다음 step.

- [ ] **Step 2: CargoBox 정리**

`CargoBox.cs:32`:
```csharp
// 제거: [SerializeField] private PackingTier packing = PackingTier.Bubble;
// 신규: [SerializeField] private WrapState wrapState; (있으면 사용, 없으면 무방어)
```

`CargoBox.cs:194` 부근의 `packing` 사용처 제거. PackingModifier 참조도 제거 — CargoBox modifier chain에서 PackingModifier 등록 부분 삭제.

- [ ] **Step 3: RunController 정리**

`RunController.cs:84`:
```csharp
public void BeginRun(OrderJob job) { /* 기존 코드 유지 */ }
// 제거: public void BeginRun(OrderJob job, PackingTier? packingOverride)
```

`RunController.cs:118-123` `ApplyCargoSetup`에서 packing 오버라이드 부분 제거. WrapState는 cargo prefab에 부착돼 있으므로 추가 setup 불필요.

- [ ] **Step 4: 파일 4개 삭제**

```bash
git rm F1/Assets/Scripts/Cargo/PackingTier.cs
git rm F1/Assets/Scripts/Cargo/Modifiers/PackingModifier.cs
git rm F1/Assets/Scripts/Order/PreRunSetupPanel.cs
git rm F1/Assets/Scripts/Order/PackingCardUI.cs
# .meta 파일도 같이
```

- [ ] **Step 5: 컴파일 0 오류**

```bash
unity-cli console --errors
```

Expected: 0 errors. (있으면 남은 참조 정리.)

- [ ] **Step 6: 커밋**

```bash
git add -A F1/Assets/Scripts/
git commit -m "refactor(packing): PackingTier 시스템 제거 (WrapState로 갈아치움)"
```

---

## Task 15: CargoShakeSimulator 손상 계산 통합

**Files:**
- Modify: `F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs:212-219`

`TryApplyBoxImpactDamage`가 현재 `(speed)` 만 받음. 충격 방향 + 위치를 추가로 받아 WrapState로 흡수율 쿼리하도록.

- [ ] **Step 1: 메서드 시그니처 확장**

```csharp
void TryApplyBoxImpactDamage(float speed, Vector3 worldHitPoint, Vector3 incomingDir)
{
    if (speed < boxWallSpeedThreshold) return;
    float raw = (speed - boxWallSpeedThreshold) * boxWallDamageMul;

    float absorption = 0f;
    var wrap = cargo != null ? cargo.GetComponentInChildren<WrapState>() : null;
    if (wrap != null) absorption = wrap.QueryAbsorption(worldHitPoint, incomingDir);

    float dmg = raw * (1f - absorption);
    if (dmg > 0.01f) cargo.ApplyExternalDamage(dmg, flashSpeedKmh: speed * 3.6f);
}
```

- [ ] **Step 2: 호출처 — 충격 위치/방향 인자 추가**

CargoShakeSimulator 내부에서 `TryApplyBoxImpactDamage` 호출하는 곳 모두에 위치/방향 인자 추가. 박스 슬라이드 후 벽에 박는 지점이 어디인지 (대략 `cargo.transform.position + (slide direction normalized) * 0.5f`).

- [ ] **Step 3: 검증 — 충격 시뮬레이션 → 손상 차감 확인**

```bash
unity-cli editor play
# 도현: 짧은 런 한 번 (포장 0% → 풀 손상, 포장 100% B → 손상 거의 0)
unity-cli editor stop
unity-cli exec @"
var c = UnityEngine.Object.FindObjectOfType<PitCrew.Cargo.CargoBox>();
return c != null ? $""damage={c.Damage:F2}"" : ""no cargo"";
"@
```

Expected: 두 런 비교 시 손상 값이 흡수율에 따라 다름.

- [ ] **Step 4: 커밋**

```bash
git add F1/Assets/Scripts/Cargo/CargoShakeSimulator.cs
git commit -m "feat(packing): 충격 손상 계산에 WrapState 흡수율 통합"
```

---

## Task 16: 폴리시 — 사운드 cue + hover 피드백

**Files:**
- Modify: `F1/Assets/Scripts/Packing/PackingScreen.cs`
- Modify: `F1/Assets/Scripts/Packing/PackingScreenInput.cs`

기존 `AudioCueBus` (Phase 4)에 새 cue 추가.

- [ ] **Step 1: AudioCueBus에 cue 등록**

`AudioCueBus` 정의 파일(자세한 경로는 `F1/Assets/Scripts/Audio/` grep) 확인 후 cue 식별자 추가:

- `Packing.MaterialSwitch` — 1/2/3 키
- `Packing.StrokeStart` — 마우스 누름
- `Packing.StrokeEnd` — 마우스 떼기
- `Packing.Confirm` — 출발 버튼

- [ ] **Step 2: PackingScreenInput/PackingScreen에서 cue publish**

- [ ] **Step 3: 재료 카드 hover 피드백 — 카드 outline 빨강**

ResultPanel 업그레이드 카드 hover 패턴 그대로 복제.

- [ ] **Step 4: 검증**

```bash
unity-cli editor play
# 1/2/3, 마우스, 출발 클릭 시 cue 로그 보이는지 console
unity-cli console --logs | findstr Packing
unity-cli editor stop
```

Expected: 4종 cue 로그 보임.

- [ ] **Step 5: 커밋**

```bash
git add F1/Assets/Scripts/Packing/
git commit -m "feat(packing): 사운드 cue + hover 피드백"
```

---

## Task 17: 풀 사이클 통합 검증

검증 외 신규 코드 없음. 한 잡을 잡 보드 → 포장 → 운전 → 정산까지 굴리고 손상↔보상 차감 확인.

- [ ] **Step 1: itemCount=2 잡 자산 하나 만들기 (Order_C_Risky.asset 갱신)**

`unity-cli exec`로 Order_C_Risky.asset의 itemCount 필드 2로 설정.

- [ ] **Step 2: 전체 흐름 한 번**

```bash
unity-cli editor play
# 도현:
# 1. Risky 잡 카드 클릭
# 2. 물건 1을 부블랩만, 물건 2를 하드케이스만 칠함
# 3. 출발 → 거칠게 운전 → 정산
unity-cli editor stop
unity-cli exec "var rp = UnityEngine.Object.FindObjectOfType<PitCrew.Order.ResultPanel>(); return rp != null ? ""result shown"" : ""no result"";"
```

Expected: 정산 패널에 손상 보상 차감 두 물건 합이 보임. 두 물건의 손상 정도가 흡수율 차이로 다름.

- [ ] **Step 3: 메모리 누수 확인**

```bash
unity-cli exec "return $""rts={System.Linq.Enumerable.Count(UnityEngine.Object.FindObjectsOfType<UnityEngine.RenderTexture>())}"";"
```

Expected: 잡 끝난 후에도 RenderTexture가 누적되지 않음 (≤ 5개 정도).

- [ ] **Step 4: 활성 컨텍스트 갱신 + 결정 로그 D-070 기록**

`옵시디언/notes/decisions-log.md`에 D-070 한 줄 추가:

```
- **D-070 (2026-05-13)**: 3D 포장 미니게임 도입. PackingTier 4단계 시스템 폐기, WrapState/WrapPainter + 자유 랩핑 (raycast wrap stroke) 패러다임. spec: [[2026-05-13-3d-packing-design]]. plan: [[2026-05-13-3d-packing-plan]].
```

`.claude/activeContext.md` 갱신:
- 지난 세션 요약에 세션 21 라인 추가
- 다음 세션 첫 작업을 "차고/부품 시스템 도현 결정 대기 → 결정 시 별도 spec 시작"으로

- [ ] **Step 5: 최종 커밋**

```bash
git add 옵시디언/notes/decisions-log.md .claude/activeContext.md
git commit -m "docs: D-070 + activeContext 갱신 — 3D 포장 완료"
```

---

## Self-Review (작성 후 본인 확인)

**Spec coverage:**
- WrapMaterialDef SO 3종 → Task 1 ✓
- WrapState + RenderTexture → Task 2 ✓
- texture painting / strip 그리기 → Task 3, 4 ✓
- 표면 오버레이 셰이더 → Task 5 ✓
- 커버리지/비용 → Task 6, 11 ✓
- 충격→UV→흡수율 → Task 7, 15 ✓
- PackingScreen UGUI → Task 8, 9 ✓
- 입력 모델 (랩 띠 메타포) → Task 10 ✓
- 다물건 1~3 탭 → Task 12 ✓
- OrderBoard 연결 → Task 13 ✓
- 기존 PackingTier 제거 → Task 14 ✓
- 폴리시 → Task 16 ✓
- 풀 사이클 검증 → Task 17 ✓

**Placeholder scan:**
- Task 9 Step 1의 `NewPanel/NewButton/NewLabel/NewBar` 4 메서드는 본문 미작성 — `throw new System.NotImplementedException()`. Step 2에서 ResultPanel.cs에서 복사하라고 명시했지만 실제 코드를 step에 박지 않음. **위험 신호**. 그러나 ResultPanel.cs:200~340 라인이 검증된 자료이므로 복사 작업은 단순. 단, 에이전트가 잘못된 헬퍼를 가져올 가능성 있음. → 실행 시 ResultPanel.cs 헬퍼를 먼저 읽고 그대로 복사하라고 명시함.
- Task 15 Step 2의 "호출처 — 충격 위치/방향 인자 추가" 부분이 모호. CargoShakeSimulator의 box wall impact 발생 지점에서 어떻게 위치/방향을 산출할지 명시 안 함. → 실행 시 슬라이드 방향 벡터를 그대로 incoming, 박스 위치 + (sliding dir * boxLocalHalfExtent)를 hit point로 쓰는 어림셈으로 시작. 정확도 부족하면 Task 17에서 fallback (mesh normal 일치 면).

**Type consistency:**
- `Services.Run.BeginRun(job)` — RunController가 `IRunController.BeginRun(OrderJob)` 메서드를 갖는지 확인 필요. 현재 `RunController.cs:84` public method 있으나 인터페이스에 노출됐는지는 미확인. Task 13/14에서 인터페이스 정리 필요 시 추가 작업.
- `OrderJob.itemCount` — Task 12에서 추가. Task 8 Step 3 검증 코드는 Task 12 이후 실행 가능. 본 플랜에 명시.

**Gap:**
- 본 spec엔 "출발 후 화물 prefab이 트렁크에 실리는 흐름"이 명시 안 됨. 현재 cargo는 RunController가 spawn하는데, PackingScreen이 spawn한 cargo가 RunController로 어떻게 인계되는지 미정. → Task 13 또는 신규 Task로 "PackingScreen의 spawnedItems[]를 RunController.BeginRun(job, items)로 전달"이 필요. 본 플랜에선 단순화: PackingScreen 종료 시 spawnedItems 파괴하고 RunController가 평소처럼 spawn (wrap state는 잃지만 데모는 그 흐름 가능). **이건 도현 검토 필요한 결정 — Task 13 실행 전 명확화.**

---

## 결정 (2026-05-13 도현 승인)

1. **포장 화물 인계 = A안.** PackingScreen이 wrap한 GameObject를 그대로 RunController로 넘김. 포장 화면 박스에 Rigidbody·콜라이더 등 운전 화면에서 필요한 컴포넌트 미리 부착. **Task 13에서 RunController.BeginRun 시그니처를 `BeginRun(OrderJob job, List<GameObject> packedItems)`로 확장.**

2. **`Services.Packing` 등록 = 기존 패턴 따라.** `GameContext.Awake()`에서 12개 등록 옆에 한 줄 추가. PackingScreen이 자기 등록하지 않음. **Task 13 Step 1 그대로.**

3. **AudioCueBus cue 이름 = 기존 따라.** Task 16의 첫 step으로 "AudioCueBus 코드 + 기존 cue 식별자 grep해서 컨벤션 확인" 추가. 추측한 `Packing.MaterialSwitch` 점 표기는 컨벤션 맞으면 유지, 아니면 일괄 교체.

---

## Execution Handoff

Plan complete and saved to `옵시디언/dev-plan/2026-05-13-3d-packing-plan.md`. Two execution options:

**1. Subagent-Driven (recommended)** — fresh subagent per task + 리뷰 체크포인트. Task 단위로 빠르게 반복.

**2. Inline Execution** — 이 세션에서 executing-plans 스킬로 batch 실행.

어느 쪽?
