using UnityEngine;

namespace PitCrew.Packing
{
    /// <summary>
    /// 마우스 누름 = 시작점 고정 + 고무줄 선 표시.
    /// 떼면 = 시작점부터 끝점까지 표면을 따라 strip 한 줄을 한 번에 생성.
    /// </summary>
    public sealed class WrapPainter : MonoBehaviour
    {
        [SerializeField] private Camera packingCamera;
        [SerializeField] private WrapState targetState;
        [SerializeField] private MeshCollider targetCollider;

        [Header("Painting")]
        [SerializeField] private float stripWidthUV = 0.06f;
        [SerializeField] private int snapSampleCount = 24;       // 떼었을 때 strip을 만들 sample 수
        [SerializeField] private int previewSampleCount = 24;    // 누르는 동안 고무줄 표시용 sample 수

        Material blitMat;
        RenderTexture scratch;
        WrapMaterialDef currentMat;
        bool stroking;

        // 고무줄 상태 — 시작점은 큐브 local 좌표로 저장. WASD로 회전해도 자동 따라감.
        Vector2 startScreenPos;
        Vector2 startUV;
        Vector3 startLocalPos;
        Vector3 startLocalNormal;
        LineRenderer rubberBand;

        // 메테리얼 캐시
        Material[] ribbonMaterialsByChannel;

        const float k_RaycastRange = 100f;

        public float StripWidthUV { get => stripWidthUV; set => stripWidthUV = Mathf.Clamp(value, 0.01f, 0.25f); }
        public void SetMaterial(WrapMaterialDef def) { currentMat = def; }
        public System.Action<WrapState> OnStrokeEnded;

        void Awake()
        {
            blitMat = new Material(Shader.Find("PitCrew/WrapStripBlit"));
            if (targetState != null) InitRT();
        }

        public void Init(WrapState state, Camera cam = null, MeshCollider col = null)
        {
            targetState = state;
            if (cam != null) packingCamera = cam;
            if (col != null) targetCollider = col;
            if (blitMat == null) blitMat = new Material(Shader.Find("PitCrew/WrapStripBlit"));
            InitRT();
        }

        void InitRT()
        {
            if (scratch != null) scratch.Release();
            scratch = new RenderTexture(targetState.Size, targetState.Size, 0, RenderTextureFormat.ARGB32);
            scratch.Create();
        }

        void OnDestroy()
        {
            if (scratch != null) scratch.Release();
            if (blitMat != null) Destroy(blitMat);
            if (rubberBand != null) Destroy(rubberBand.gameObject);
            if (ribbonMaterialsByChannel != null)
                foreach (var m in ribbonMaterialsByChannel)
                    if (m != null) Destroy(m);
        }

        Material GetOrCreateRibbonMaterial(WrapMaterialDef def)
        {
            if (ribbonMaterialsByChannel == null) ribbonMaterialsByChannel = new Material[3];
            int idx = Mathf.Clamp(def.channelIndex, 0, 2);
            if (ribbonMaterialsByChannel[idx] == null)
            {
                var shader = Shader.Find("PitCrew/WrapRibbon");
                var mat = new Material(shader);
                var col = def.displayColor; col.a = 1.0f;
                mat.SetColor("_BaseColor", col);
                mat.SetColor("_RimColor", Color.white);
                mat.SetFloat("_RimPower", 2.5f);
                mat.SetFloat("_EdgeBoost", 0.55f);
                ribbonMaterialsByChannel[idx] = mat;
            }
            return ribbonMaterialsByChannel[idx];
        }

        // ---------------------------------------------------------------
        // 입력 핸들러: 누름 → 끄는 동안 고무줄 → 떼면 strip 한 줄 생성
        // ---------------------------------------------------------------

        public void BeginStroke(Vector2 screenPos)
        {
            if (stroking) CancelStroke();
            if (currentMat == null) return;
            if (!TryRaycastUV(screenPos, out var uv, out var hit)) return;

            startScreenPos = screenPos;
            startUV = uv;
            // hit.point/normal은 world 좌표. 큐브가 WASD로 회전하므로 local로 저장.
            var t = targetCollider.transform;
            startLocalPos = t.InverseTransformPoint(hit.point);
            startLocalNormal = t.InverseTransformDirection(hit.normal);
            stroking = true;
            AudioCueBus.Publish(AudioCueId.PackingStrokeStart);

            EnsureRubberBand();
            var initPts = new System.Collections.Generic.List<Vector3> { hit.point };
            UpdateRubberBandPolyline(initPts);
        }

        // 현재 큐브 회전 기준 시작점 world 좌표
        Vector3 CurrentStartWorld()
        {
            return targetCollider != null
                ? targetCollider.transform.TransformPoint(startLocalPos)
                : startLocalPos;
        }
        Vector3 CurrentStartNormal()
        {
            return targetCollider != null
                ? targetCollider.transform.TransformDirection(startLocalNormal)
                : startLocalNormal;
        }

        public void UpdateStroke(Vector2 screenPos)
        {
            if (!stroking) return;
            // 시작점 → 마우스 사이를 N개 sample로 raycast.
            // 큐브가 WASD로 회전했을 수 있으니 시작점을 현재 화면 좌표로 재투영.
            var startWorld = CurrentStartWorld();
            var startScreenNow = packingCamera.WorldToScreenPoint(startWorld);
            Vector2 s0 = new Vector2(startScreenNow.x, startScreenNow.y);

            var pts = new System.Collections.Generic.List<Vector3>(previewSampleCount + 1);
            pts.Add(startWorld);

            int n = Mathf.Max(2, previewSampleCount);
            for (int i = 1; i <= n; i++)
            {
                float t = (float)i / n;
                var s = Vector2.Lerp(s0, screenPos, t);
                if (TryRaycastUV(s, out _, out var hit)) pts.Add(hit.point);
            }
            UpdateRubberBandPolyline(pts);
        }

        public void EndStroke()
        {
            if (!stroking) return;
            Vector2 endScreenPos = GetLastMouseScreen();
            var startWorld = CurrentStartWorld();
            var startScreenNow = packingCamera.WorldToScreenPoint(startWorld);
            CommitStripPolyline(new Vector2(startScreenNow.x, startScreenNow.y), endScreenPos);
            OnStrokeEnded?.Invoke(targetState);
            AudioCueBus.Publish(AudioCueId.PackingStrokeEnd);
            HideRubberBand();
            stroking = false;
        }

        public void CancelStroke()
        {
            if (!stroking) return;
            HideRubberBand();
            stroking = false;
        }

        // 입력 폴링 안 하므로 마지막 화면 좌표는 RubberBand의 끝점에서 역추적
        Vector2 GetLastMouseScreen()
        {
            // 마우스 현재 위치는 PackingScreenInput이 ReadValue로 보내는 게 정석이지만,
            // EndStroke는 input이 호출하는 흐름이라 마지막 UpdateStroke의 screenPos를 그대로 쓰는 게 맞음.
            // 그러나 시그니처상 screenPos를 인자로 안 받으므로 InputSystem에서 직접 읽음.
            return UnityEngine.InputSystem.Mouse.current != null
                ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
                : startScreenPos;
        }

        // 시작 화면 좌표 → 끝 화면 좌표 사이를 N분할해 raycast.
        // 표면 닿은 sample들로 ribbon mesh + RT 페인트 (모서리에서 자연스럽게 꺾임).
        void CommitStripPolyline(Vector2 s0, Vector2 s1)
        {
            int n = Mathf.Max(2, snapSampleCount);

            var ribbonGO = new GameObject($"Ribbon_{currentMat.displayName}");
            ribbonGO.transform.SetParent(targetCollider != null ? targetCollider.transform : transform, false);
            var ribbon = ribbonGO.AddComponent<WrapRibbon>();
            ribbon.Init(GetOrCreateRibbonMaterial(currentMat), stripWidthUV * 3.0f);

            // 시작점은 큐브 회전 반영한 현재 world 좌표 사용
            ribbon.AddSample(CurrentStartWorld(), CurrentStartNormal());
            Vector2 prevUV = startUV;
            bool hasPrev = true;
            int hitCount = 1;

            for (int i = 1; i <= n; i++)
            {
                float t = (float)i / n;
                var s = Vector2.Lerp(s0, s1, t);
                if (TryRaycastUV(s, out var uv, out var hit))
                {
                    ribbon.AddSample(hit.point, hit.normal);
                    if (hasPrev) DrawStrip(prevUV, uv, currentMat);
                    prevUV = uv;
                    hasPrev = true;
                    hitCount++;
                }
                else
                {
                    // 화면이 큐브 밖으로 나간 sample은 polyline 단절 — 다음 hit 때 새 segment 시작
                    hasPrev = false;
                }
            }
            if (hitCount < 2) return; // strip 무효
        }

        bool TryRaycastUV(Vector2 screenPos, out Vector2 uv) => TryRaycastUV(screenPos, out uv, out _);

        bool TryRaycastUV(Vector2 screenPos, out Vector2 uv, out RaycastHit hit)
        {
            uv = default; hit = default;
            if (packingCamera == null || targetCollider == null) return false;
            var ray = packingCamera.ScreenPointToRay(screenPos);
            if (!targetCollider.Raycast(ray, out hit, k_RaycastRange)) return false;
            uv = hit.textureCoord;
            return true;
        }

        // ---------------------------------------------------------------
        // 고무줄 LineRenderer
        // ---------------------------------------------------------------

        void EnsureRubberBand()
        {
            if (rubberBand != null) { rubberBand.enabled = true; return; }
            var go = new GameObject("RubberBand");
            go.transform.SetParent(transform, false);
            rubberBand = go.AddComponent<LineRenderer>();
            rubberBand.useWorldSpace = true;
            rubberBand.positionCount = 0;
            rubberBand.startWidth = 0.04f;
            rubberBand.endWidth = 0.04f;
            rubberBand.numCapVertices = 4;
            rubberBand.numCornerVertices = 6;
            rubberBand.alignment = LineAlignment.View;
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = currentMat != null ? currentMat.displayColor : Color.white;
            rubberBand.material = mat;
        }

        void UpdateRubberBandPolyline(System.Collections.Generic.List<Vector3> pts)
        {
            if (rubberBand == null) return;
            rubberBand.positionCount = pts.Count;
            for (int i = 0; i < pts.Count; i++) rubberBand.SetPosition(i, pts[i]);
            if (currentMat != null && rubberBand.material != null) rubberBand.material.color = currentMat.displayColor;
        }

        void HideRubberBand()
        {
            if (rubberBand != null) rubberBand.enabled = false;
        }

        // ---------------------------------------------------------------
        // RT 페인트 (비용/커버리지 계산용 — 시각은 ribbon이 담당)
        // ---------------------------------------------------------------

        void DrawStrip(Vector2 prev, Vector2 curr, WrapMaterialDef mat)
        {
            if (targetState == null || targetState.WrapRT == null) return;
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

            float lenUV = (curr - prev).magnitude;
            float pixelArea = lenUV * stripWidthUV * targetState.Size * targetState.Size;
            targetState.AccumulateCost(mat.channelIndex, pixelArea * mat.costPerPixel);
        }
    }
}
