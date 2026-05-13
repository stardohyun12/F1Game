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
        [SerializeField] private int snapSampleCount = 12; // 떼었을 때 strip을 만들 sample 수

        Material blitMat;
        RenderTexture scratch;
        WrapMaterialDef currentMat;
        bool stroking;

        // 고무줄 상태
        Vector2 startScreenPos;
        Vector2 startUV;
        RaycastHit startHit;
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
            startHit = hit;
            stroking = true;
            AudioCueBus.Publish(AudioCueId.PackingStrokeStart);

            EnsureRubberBand();
            UpdateRubberBand(hit.point, hit.point);
        }

        public void UpdateStroke(Vector2 screenPos)
        {
            if (!stroking) return;
            // 끝점: 마우스 위치를 표면에 raycast. 표면 빗나가면 카메라에서 일정 거리의 공중점 사용.
            Vector3 endWorld;
            if (TryRaycastUV(screenPos, out _, out var endHit))
                endWorld = endHit.point;
            else
                endWorld = packingCamera.ScreenPointToRay(screenPos).GetPoint(3f);

            UpdateRubberBand(startHit.point, endWorld);
        }

        public void EndStroke()
        {
            if (!stroking) return;
            // 떼는 순간 마우스 위치 raycast → 끝점 결정 후 strip 한 줄로 commit
            Vector2 endScreenPos = GetLastMouseScreen();
            if (TryRaycastUV(endScreenPos, out var endUV, out var endHit))
            {
                CommitStrip(startScreenPos, endScreenPos, startUV, endUV, startHit, endHit);
            }
            // 떼면 RT 갱신 + 콜백
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

        // 시작/끝 사이 화면 좌표를 N분할해 raycast — 표면을 따라 strip 생성
        void CommitStrip(Vector2 s0, Vector2 s1, Vector2 uv0, Vector2 uv1, RaycastHit hit0, RaycastHit hit1)
        {
            int n = Mathf.Max(2, snapSampleCount);

            // 3D 리본 메쉬
            var ribbonGO = new GameObject($"Ribbon_{currentMat.displayName}");
            ribbonGO.transform.SetParent(targetCollider != null ? targetCollider.transform : transform, false);
            var ribbon = ribbonGO.AddComponent<WrapRibbon>();
            ribbon.Init(GetOrCreateRibbonMaterial(currentMat), stripWidthUV * 3.0f);
            ribbon.AddSample(hit0.point, hit0.normal);

            Vector2 prevUV = uv0;
            bool anyMid = false;
            for (int i = 1; i < n; i++)
            {
                float t = (float)i / (n - 1);
                Vector2 s = Vector2.Lerp(s0, s1, t);

                if (TryRaycastUV(s, out var uvMid, out var hitMid))
                {
                    ribbon.AddSample(hitMid.point, hitMid.normal);
                    if (i > 1 || (uvMid - prevUV).sqrMagnitude > 1e-8f)
                    {
                        DrawStrip(prevUV, uvMid, currentMat);
                        prevUV = uvMid;
                        anyMid = true;
                    }
                }
            }
            // 끝점 보장
            ribbon.AddSample(hit1.point, hit1.normal);
            if (!anyMid) DrawStrip(uv0, uv1, currentMat);
            else DrawStrip(prevUV, uv1, currentMat);
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
            rubberBand.positionCount = 2;
            rubberBand.startWidth = 0.04f;
            rubberBand.endWidth = 0.04f;
            rubberBand.numCapVertices = 4;
            rubberBand.numCornerVertices = 2;
            // URP 호환 단순 머티리얼
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            mat.color = currentMat != null ? currentMat.displayColor : Color.white;
            rubberBand.material = mat;
        }

        void UpdateRubberBand(Vector3 a, Vector3 b)
        {
            if (rubberBand == null) return;
            rubberBand.SetPosition(0, a);
            rubberBand.SetPosition(1, b);
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
