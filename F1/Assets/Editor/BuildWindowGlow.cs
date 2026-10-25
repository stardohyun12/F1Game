using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// D-137 "초저녁 창에 불" 최소 구현 — 셰이더에 창 마스크가 없어서, 건물 메시에서 '유리 칸' UV 를 쓴 세로 면 삼각형만 골라
/// 12cm 앞으로 띄운 복사본을 한 메시(WindowGlow)로 묶는다. 무드의 windowsLit 이 켜고 끈다(MoodController.SetWindowsLit).
///  - 유리 칸(2026-10-25 칸 번호 텍스처로 실측): 아트 4x4 아틀라스 = 위에서 2행 2열(칸 11), LofiAtlas 8x8 = 맨 아래 오른쪽(칸 77)
///  - 켜짐은 집마다 층(3m)·면 방향 단위로 60% — 한 층이 통째로 켜진 집, 꺼진 집이 섞이게(가설)
///  - 재질 두 개: 따뜻한 백열(대부분) + 흰 형광(일본 생활 층). URP/Unlit HDR 이라 블룸이 걸리고 Unity 안개를 받는다.
/// 빌더(BuildBayLoop) 끝에서 다시 만든다 — 아트 건물이 바뀌면 같이 따라간다.
/// </summary>
public static class BuildWindowGlow
{
    const string MatDir = "Assets/Scenes/Map_BayLoop_Mats";

    [MenuItem("PitCrew/Map/Rebuild Window Glow (BayLoop)")]
    public static void Menu() => Debug.Log(Run());

    // 텍스처 이름 → 유리 칸 UV 사각형(xMin, yMin, xMax, yMax). UV 원점은 아래.
    static bool GlassRect(Texture t, out Rect r)
    {
        r = default;
        if (t == null) return false;
        if (t.name == "LofiAtlas") { r = Rect.MinMaxRect(.875f, 0f, 1f, .125f); return true; }
        if (t.name.EndsWith("Atlas")) { r = Rect.MinMaxRect(.25f, .5f, .5f, .75f); return true; }   // BayLoop 아트 4x4 공통 배치
        return false;
    }

    public static string Run()
    {
        var bld = GameObject.Find("Buildings");
        if (bld == null) return "WindowGlow: Buildings 없음";
        var old = bld.transform.parent != null ? bld.transform.parent.Find("WindowGlow") : null;
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var verts = new List<Vector3>(); var warm = new List<int>(); var cool = new List<int>();
        int houses = 0, litBands = 0, bands = 0, tris = 0, unreadable = 0;
        var meshCache = new Dictionary<Mesh, (Vector3[] v, Vector2[] uv, int[][] subs)>();
        foreach (Transform h in bld.transform)
        {
            bool any = false;
            var hrs = h.GetComponentsInChildren<Renderer>(); if (hrs.Length == 0) continue;
            var hb = hrs[0].bounds; foreach (var r in hrs) hb.Encapsulate(r.bounds);
            var rnd = new System.Random(h.name.GetHashCode());
            var bandLit = new Dictionary<(int, int), int>();   // (층, 면 방향 4분면) → 0 꺼짐 / 1 백열 / 2 형광
            foreach (var mf in h.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh; var mr = mf.GetComponent<MeshRenderer>();
                if (mesh == null || mr == null) continue;
                if (!meshCache.TryGetValue(mesh, out var md))
                {
                    try { var s = new int[mesh.subMeshCount][]; for (int k = 0; k < s.Length; k++) s[k] = mesh.GetTriangles(k); md = (mesh.vertices, mesh.uv, s); }
                    catch { unreadable++; md = (null, null, null); }
                    meshCache[mesh] = md;
                }
                if (md.v == null || md.uv == null || md.uv.Length != md.v.Length) continue;
                var l2w = mf.transform.localToWorldMatrix;
                for (int k = 0; k < md.subs.Length; k++)
                {
                    var mat = k < mr.sharedMaterials.Length ? mr.sharedMaterials[k] : null;
                    if (mat == null || !mat.HasProperty("_MainTex") || !GlassRect(mat.GetTexture("_MainTex"), out var gr)) continue;
                    var t = md.subs[k];
                    for (int i = 0; i < t.Length; i += 3)
                    {
                        var c = (md.uv[t[i]] + md.uv[t[i + 1]] + md.uv[t[i + 2]]) / 3f;
                        c = new Vector2(Mathf.Repeat(c.x, 1f), Mathf.Repeat(c.y, 1f));
                        if (!gr.Contains(c)) continue;
                        Vector3 a = l2w.MultiplyPoint3x4(md.v[t[i]]), b = l2w.MultiplyPoint3x4(md.v[t[i + 1]]), d = l2w.MultiplyPoint3x4(md.v[t[i + 2]]);
                        var n = Vector3.Cross(b - a, d - a); if (n.sqrMagnitude < 1e-8f) continue; n.Normalize();
                        if (Mathf.Abs(n.y) > 0.4f) continue;   // 세로 면만(유리 지붕·바닥 제외)
                        if (Mathf.Max(a.y, b.y, d.y) - Mathf.Min(a.y, b.y, d.y) < 0.35f) continue;   // 납작한 띠 제외 — shop_house 처마·문턱 띠가 같은 칸 UV 를 써서 선으로 켜졌다
                        var mid = (a + b + d) / 3f;
                        // 바깥쪽으로 띄운다: 감김 순서·음수 스케일로 n 이 집 안쪽을 볼 수 있다(상점가 shop_house 가 안쪽으로 묻혀 안 보였음)
                        var outw = mid - hb.center; outw.y = 0f; if (Vector3.Dot(n, outw) < 0f) n = -n;
                        int floor = Mathf.FloorToInt((mid.y - h.position.y) / 3f);
                        int quad = Mathf.RoundToInt(Mathf.Atan2(n.x, n.z) / (Mathf.PI * .5f)) & 3;
                        var key = (floor, quad);
                        if (!bandLit.TryGetValue(key, out int lit))
                        {
                            double r = rnd.NextDouble(); lit = r < .40 ? 0 : r < .88 ? 1 : 2; bandLit[key] = lit;
                            bands++; if (lit > 0) litBands++;
                        }
                        if (lit == 0) continue;
                        var off = n * 0.12f; int baseI = verts.Count;   // 3cm 는 40m 밖에서 유리와 깊이가 겹쳐 묻혔다(2026-10-25 실측: 5m 앞에선 보이고 주행 시점에선 안 보임)
                        verts.Add(a + off); verts.Add(b + off); verts.Add(d + off);
                        var list = lit == 1 ? warm : cool; list.Add(baseI); list.Add(baseI + 1); list.Add(baseI + 2);
                        tris++; any = true;
                    }
                }
            }
            if (any) houses++;
        }

        var mesh2 = new Mesh { name = "WindowGlow", indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh2.SetVertices(verts); mesh2.subMeshCount = 2; mesh2.SetTriangles(warm, 0); mesh2.SetTriangles(cool, 1);
        mesh2.RecalculateNormals(); mesh2.RecalculateBounds();
        string mp = MatDir + "/Mesh_WindowGlow.asset";
        AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh2, mp);
        var go = new GameObject("WindowGlow"); go.transform.SetParent(bld.transform.parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh2;
        var rr = go.AddComponent<MeshRenderer>();
        rr.sharedMaterials = new[] { GlowMat("BL_WindowWarm", new Color(1f, .40f, .10f) * 1.6f), GlowMat("BL_WindowCool", new Color(.80f, .90f, 1f) * 1.3f) };   // ×4 는 하얗게 날았고, Dusk 포스트(푸른 필터·색온도 -12)가 주황을 연두로 민다 → 빨강 쪽으로 당김
        rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; rr.receiveShadows = false;
        go.SetActive(false);   // 기본 꺼짐 — 무드 Dusk_Lights 가 켠다
        AssetDatabase.SaveAssets();
        return $"WindowGlow: 집 {houses}채, 층·면 {bands} 중 켜짐 {litBands}, 삼각형 {tris}" + (unreadable > 0 ? $", 못 읽은 메시 {unreadable}" : "");
    }

    static Material GlowMat(string name, Color hdr)
    {
        string p = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(m, p); }
        m.SetColor("_BaseColor", hdr);
        m.SetFloat("_Cull", 0f);   // 양면(면 방향이 뒤집힌 아트 메시 대비)
        EditorUtility.SetDirty(m);
        return m;
    }
}
