using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 과제 20261104-1559(D-298 실내 = 디오라마): 2층 방을 기준 화면(world/taste/interior-candidates/07)의 「거친 저해상 결 · 잔소품 없음」으로.
/// (a) 재질: 17 의 단색 사본(*_roomflat)을 걷고 원본 텍스처로 돌린다. 단, 텍스처는 가져오기 설정만 다른 사본(*_dio.png = 원본 파일 그대로 복사)을 쓴다 —
///     디오라마 거리(RoomDiorama.distance)에서 한 텍셀 = 화면 약 TexelPx 픽셀이 되게 maxTextureSize 를 낮추고 필터 Point·밉맵 끔.
///     재질 사본(*_dio.mat)을 이 씬의 그 집 렌더러에만 꽂는다. 원본 .mat·.png·공유 재질은 건드리지 않는다.
/// (b) 잔소품: 방 상자 안 포스터·사진·달력 메시·연필·작은 책·옷걸이 등은 SetActive(false) — 지우지 않는다. 목록은 room_dio/disabled.txt, Restore() 로 되돌린다.
///     남김: 침대·책상·의자·창·문·일기장(NOTEBOOK)·선반·러그 등 큰 덩어리. 세계 안 달력(RoomCalendar)은 판 한 장으로 남는다(디테일 글자판은 RoomDiorama 가 끈다).
/// (c) 17 의 윤곽선(A7ShadeGlobals.roomOutline)은 끈 채로 둔다.
/// 집 빌더(library_import)·RoomFlatBuild 를 다시 돌리면 이 결과가 사라진다 — 이걸 다시 돌린다.
/// 실행: RoomDioramaBuild.Build() (메뉴 PitCrew/Room Diorama Build) · 되돌리기: RoomDioramaBuild.Restore()
/// </summary>
public static class RoomDioramaBuild
{
    public const string OutDir = "Assets/Art/Library/Buildings/room_dio";
    const float TexelPx = 3.5f;          // 한 텍셀 = 화면 3~4 픽셀(과제)
    const float BoxPad = 1.05f;

    [MenuItem("PitCrew/Room Diorama Build")]
    public static string Build()
    {
        var room = FindRoom("ROOM_2F");
        if (room == null) return "ROOM_2F 없음";
        if (!A7ShadeGlobals.TryRoomBox(room, out var box)) return "방 상자 못 잡음(레이)";
        var house = room.parent;
        var log = new System.Text.StringBuilder();
        Directory.CreateDirectory(OutDir);

        // 디오라마 거리에서 1 m 가 화면 몇 픽셀인가(세로 360)
        float fov = Camera.main != null ? Camera.main.fieldOfView : 60f;
        float dist = 4.5f;
        var dio = Object.FindFirstObjectByType<RoomDiorama>(FindObjectsInactive.Include);
        if (dio != null) dist = dio.distance;
        float pxPerM = 360f / (2f * dist * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad));
        float wantDens = pxPerM / TexelPx;   // 텍셀/m
        log.Append($"디오라마 {dist} m · fov {fov:F0} → {pxPerM:F0} px/m → 목표 {wantDens:F1} 텍셀/m\n");

        // (a) 원본 재질별 렌더러
        var byMat = new Dictionary<Material, List<Renderer>>();
        foreach (var r in house.GetComponentsInChildren<Renderer>(true))
        {
            var m = Original(r.sharedMaterial);
            if (m == null || m.shader == null || m.shader.name != "PitCrew/SceneA7/Lit" || m.GetTexture("_BaseMap") == null) continue;
            if (!byMat.TryGetValue(m, out var l)) byMat[m] = l = new List<Renderer>();
            l.Add(r);
        }
        var pairs = new List<string>();
        foreach (var kv in byMat)
        {
            var src = kv.Key;
            var srcTex = (Texture2D)src.GetTexture("_BaseMap");
            var srcPath = AssetDatabase.GetAssetPath(srcTex);
            // 방 안 면의 텍셀 밀도 중앙값(원본 해상도 기준)
            var dens = new List<float>();
            foreach (var r in kv.Value) Densities(r, box, srcTex.width, srcTex.height, dens);
            dens.Sort();
            float med = dens.Count > 0 ? dens[dens.Count / 2] : wantDens;
            int full = Mathf.Max(srcTex.width, srcTex.height);
            int max = Pow2Near(full * wantDens / Mathf.Max(1e-3f, med));
            max = Mathf.Clamp(max, 32, full);

            var dstPath = $"{OutDir}/{Path.GetFileNameWithoutExtension(srcPath)}_dio{Path.GetExtension(srcPath)}";
            File.Copy(srcPath, dstPath, true);
            AssetDatabase.ImportAsset(dstPath, ImportAssetOptions.ForceUpdate);
            var a = (TextureImporter)AssetImporter.GetAtPath(srcPath); var b = (TextureImporter)AssetImporter.GetAtPath(dstPath);
            var s = new TextureImporterSettings(); a.ReadTextureSettings(s); b.SetTextureSettings(s);
            b.filterMode = FilterMode.Point; b.mipmapEnabled = false; b.maxTextureSize = max;
            var ps = b.GetDefaultPlatformTextureSettings(); ps.maxTextureSize = max; ps.textureCompression = TextureImporterCompression.Uncompressed; b.SetPlatformTextureSettings(ps);
            b.SaveAndReimport();
            var dstTex = AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath);

            var matPath = $"{OutDir}/{src.name}_dio.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(src); AssetDatabase.CreateAsset(mat, matPath); }
            else mat.CopyPropertiesFromMaterial(src);
            mat.name = src.name + "_dio";
            mat.SetTexture("_BaseMap", dstTex);
            EditorUtility.SetDirty(mat);
            foreach (var r in kv.Value)
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) if (Original(ms[i]) == src) ms[i] = mat;
                Undo.RecordObject(r, "Room diorama");
                r.sharedMaterials = ms;
                EditorUtility.SetDirty(r);
            }
            pairs.Add($"{AssetDatabase.GetAssetPath(src)}\t{matPath}");
            log.Append($"{src.name}: 렌더러 {kv.Value.Count} · 원본 {srcTex.width}×{srcTex.height} · 방 밀도 중앙 {med:F0}/m → max {max} (→ {med * max / full:F1}/m)\n");
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText($"{OutDir}/pairs.txt", string.Join("\n", pairs));

        // (b) 잔소품 끄기(지우지 않음)
        var off = new List<string>();
        if (File.Exists($"{OutDir}/disabled.txt")) off.AddRange(File.ReadAllLines($"{OutDir}/disabled.txt").Where(l => l.Length > 0));
        foreach (var r in house.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.gameObject.activeSelf || !IsSmall(r.name)) continue;
            var q = box.MultiplyPoint3x4(r.bounds.center);
            if (Mathf.Abs(q.x) > BoxPad || Mathf.Abs(q.y) > BoxPad || Mathf.Abs(q.z) > BoxPad) continue;
            Undo.RecordObject(r.gameObject, "Room diorama props");
            r.gameObject.SetActive(false);
            var p = PathOf(r.transform, house);
            if (!off.Contains(p)) off.Add(p);
        }
        File.WriteAllText($"{OutDir}/disabled.txt", string.Join("\n", off));
        log.Append($"잔소품 끔 {off.Count}: {string.Join(", ", off.Select(Path.GetFileName))}\n");

        // (c) 윤곽선 끔
        var g = Object.FindFirstObjectByType<A7ShadeGlobals>();
        if (g != null && g.roomOutline) { Undo.RecordObject(g, "outline off"); g.roomOutline = false; EditorUtility.SetDirty(g); }

        EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
        return log.ToString();
    }

    /// <summary>잔소품을 다시 켠다(재질은 그대로 — 재질을 되돌리려면 RoomFlatBuild 또는 집 빌더를 다시 돌린다).</summary>
    [MenuItem("PitCrew/Room Diorama Restore Props")]
    public static string Restore()
    {
        var room = FindRoom("ROOM_2F");
        var list = $"{OutDir}/disabled.txt";
        if (room == null || !File.Exists(list)) return "없음";
        int n = 0;
        foreach (var p in File.ReadAllLines(list))
        {
            var t = room.parent.Find(p);
            if (t == null) continue;
            Undo.RecordObject(t.gameObject, "restore"); t.gameObject.SetActive(true); n++;
        }
        File.Delete(list);
        EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
        return $"되살림 {n}";
    }

    public static bool IsSmall(string n) => RoomDiorama.IsSmallProp(n);

    /// <summary>사본(*_roomflat · *_dio) 재질이면 원본을.</summary>
    public static Material Original(Material m)
    {
        if (m == null) return null;
        if (m.name.EndsWith("_roomflat")) return RoomFlatBuild.Original(m);
        if (!m.name.EndsWith("_dio")) return m;
        var pairs = $"{OutDir}/pairs.txt";
        if (!File.Exists(pairs)) return m;
        foreach (var line in File.ReadAllLines(pairs))
        {
            var p = line.Split('\t');
            if (p.Length == 2 && Path.GetFileNameWithoutExtension(p[1]) == m.name) return AssetDatabase.LoadAssetAtPath<Material>(p[0]) ?? m;
        }
        return m;
    }

    static int Pow2Near(float v)
    {
        int p = 32;
        while (p < 16384 && v > p * 1.4142f) p *= 2;
        return p;
    }

    static void Densities(Renderer r, Matrix4x4 box, int W, int H, List<float> outL)
    {
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;
        var mesh = mf.sharedMesh; var v = mesh.vertices; var uv = mesh.uv; var tri = mesh.triangles;
        if (uv == null || uv.Length != v.Length) return;
        var l2w = r.transform.localToWorldMatrix;
        for (int t = 0; t < tri.Length; t += 3)
        {
            Vector3 a = l2w.MultiplyPoint3x4(v[tri[t]]), b = l2w.MultiplyPoint3x4(v[tri[t + 1]]), c = l2w.MultiplyPoint3x4(v[tri[t + 2]]);
            var q = box.MultiplyPoint3x4((a + b + c) / 3f);
            if (Mathf.Abs(q.x) > BoxPad || Mathf.Abs(q.y) > BoxPad || Mathf.Abs(q.z) > BoxPad) continue;
            Vector2 u0 = uv[tri[t]], u1 = uv[tri[t + 1]], u2 = uv[tri[t + 2]];
            float wArea = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            float uvArea = Mathf.Abs((u1.x - u0.x) * (u2.y - u0.y) - (u2.x - u0.x) * (u1.y - u0.y)) * 0.5f * W * H;
            if (wArea > 0.01f && uvArea > 0f) outL.Add(Mathf.Sqrt(uvArea / wArea));
        }
    }

    static string PathOf(Transform t, Transform root)
    {
        var s = t.name;
        for (var p = t.parent; p != null && p != root; p = p.parent) s = p.name + "/" + s;
        return s;
    }

    static Transform FindRoom(string n)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == n && t.parent != null) return t;
        return null;
    }
}
