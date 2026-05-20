using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 자산을 게임 씬에 통합하기 전, 중립 배경(도시 배경 X)에서 한 번에 렌더하는 검수 도구.
/// [[feedback-asset-review-before-integration]] — side-by-side(Kenney 박스) + 표준 라이팅 + 다각도.
/// SceneView 의존 없이 전용 카메라→RenderTexture→PNG. Edit 모드에서 즉시(Play 불필요).
///
/// unity-cli 한 줄 사용 예:
///   exec "return AssetPreviewTool.Render(\"Assets/Model/AI/wine_bottle_lowpoly.fbx\",
///         \"docs/screenshots/asset_review/wine.png\", \"#0C4721\", true);"
/// </summary>
public static class AssetPreviewTool
{
    const string KenneyBox = "Assets/Model/kenney_car-kit/Models/FBX format/box.fbx";

    /// <summary>
    /// 자산을 중립 배경에 렌더해 PNG로 저장. 반환: 상태 문자열(폴리수 포함).
    /// </summary>
    /// <param name="assetPath">미리볼 모델 에셋 경로 (Assets/...)</param>
    /// <param name="outPathRel">저장 경로 (프로젝트 루트 기준 상대, 예: docs/screenshots/asset_review/x.png)</param>
    /// <param name="tintHex">머티리얼 단색 틴트 (#RRGGBB). 빈 문자열이면 원본 머티리얼 유지.</param>
    /// <param name="withReference">true면 옆에 Kenney 박스 동반(톤 비교).</param>
    /// <param name="size">정사각 렌더 한 변 px (기본 768).</param>
    public static string Render(string assetPath, string outPathRel, string tintHex = "", bool withReference = true, int size = 768)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return $"ERROR: 에셋 없음 {assetPath}";

        var root = new GameObject("__ASSET_PREVIEW_TEMP__");
        Material tintMat = null;
        RenderTexture rt = null;
        Texture2D shot = null;
        try
        {
            // ── 자산 인스턴스 (정규화: 최대변 1.0 단위) ──
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            int tris = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
            // Renderer.bounds = 월드 AABB(transform 스케일 반영) → fbx(스케일1)·정규화 프리팹 둘 다 정확.
            Bounds bounds = default; bool boundsInit = false;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                if (!boundsInit) { bounds = r.bounds; boundsInit = true; }
                else bounds.Encapsulate(r.bounds);
            }
            float maxD = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxD <= 0f) maxD = 1f;
            go.transform.localScale = Vector3.one / maxD;
            // 바닥(y=-0.5)에 세움: 정규화 후 높이≈1 → 중심 y=0이면 바닥에 닿음.
            go.transform.localPosition = new Vector3(withReference ? -0.55f : 0f, 0f, 0f);

            if (!string.IsNullOrEmpty(tintHex) && ColorUtility.TryParseHtmlString(tintHex, out var col))
            {
                tintMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                tintMat.SetColor("_BaseColor", col);
                if (tintMat.HasProperty("_Smoothness")) tintMat.SetFloat("_Smoothness", 0.3f);
                if (tintMat.HasProperty("_Metallic")) tintMat.SetFloat("_Metallic", 0f);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = tintMat;
            }

            // ── 참조 Kenney 박스 ──
            if (withReference)
            {
                var refPf = AssetDatabase.LoadAssetAtPath<GameObject>(KenneyBox);
                if (refPf != null)
                {
                    var refGo = (GameObject)PrefabUtility.InstantiatePrefab(refPf, root.transform);
                    var rmf = refGo.GetComponentInChildren<MeshFilter>();
                    float rMax = 1f;
                    if (rmf != null && rmf.sharedMesh != null)
                    {
                        var rb = rmf.sharedMesh.bounds.size;
                        rMax = Mathf.Max(rb.x, Mathf.Max(rb.y, rb.z));
                    }
                    refGo.transform.localScale = Vector3.one * (0.85f / rMax);
                    refGo.transform.localPosition = new Vector3(0.55f, -0.075f, 0f);
                }
            }

            // ── 바닥 plane (중립 그레이) ──
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = Vector3.one * 0.6f;
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMat.SetColor("_BaseColor", new Color(0.55f, 0.56f, 0.58f));
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // ── 라이트 (표준 키 라이트) ──
            var lightGO = new GameObject("PreviewLight");
            lightGO.transform.SetParent(root.transform);
            lightGO.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.97f, 0.92f);

            // ── 카메라 (3/4 각도) → RenderTexture ──
            var camGO = new GameObject("PreviewCam");
            camGO.transform.SetParent(root.transform);
            // 두 물건(±0.55, 높이≈1)을 다 담는 3/4 시점.
            camGO.transform.position = new Vector3(0.4f, 0.95f, -2.7f);
            camGO.transform.LookAt(new Vector3(0, 0.05f, 0));
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.78f, 0.82f, 0.86f); // 옅은 하늘 그레이
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 50f;

            rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            shot = new Texture2D(size, size, TextureFormat.RGB24, false);
            shot.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            shot.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;

            var bytes = shot.EncodeToPNG();
            // Unity 작업 디렉토리는 프로젝트 폴더(F1/). outPathRel은 리포 루트 기준으로 해석.
            var repoRoot = Directory.GetParent(Application.dataPath).Parent.FullName;
            var full = Path.Combine(repoRoot, outPathRel);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, bytes);

            return $"OK saved={outPathRel} tris={tris} normalizedFrom maxDim={maxD:F3}";
        }
        finally
        {
            if (shot != null) Object.DestroyImmediate(shot);
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
            if (tintMat != null) Object.DestroyImmediate(tintMat);
            Object.DestroyImmediate(root);
        }
    }

    [MenuItem("PitCrew/Asset Review/Preview Selected (neutral)")]
    static void PreviewSelected()
    {
        var obj = Selection.activeObject;
        if (obj == null) { Debug.LogWarning("[AssetPreview] 선택된 에셋 없음"); return; }
        var path = AssetDatabase.GetAssetPath(obj);
        var name = Path.GetFileNameWithoutExtension(path);
        var res = Render(path, $"docs/screenshots/asset_review/{name}.png");
        Debug.Log($"[AssetPreview] {res}");
    }
}
