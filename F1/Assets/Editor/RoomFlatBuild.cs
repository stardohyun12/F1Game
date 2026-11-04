using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 과제 20261104-1449(D-297 그림 문법): 2층 방(ROOM_2F)을 「단색 면 + 얇은 바램 얼룩」으로.
/// 텍스처를 흐리지 않고 뺀다 — 방 상자(A7ShadeGlobals.TryRoomBox) 안 면이 쓰는 아틀라스 칸만 그 칸의 평균색 한 색으로 칠하고,
/// 큰 면(벽·바닥·문·징두리·침대·책상 …)은 방 팔레트 6색 중 가까운 색으로 붙인다. 얼룩 = 저주파 값 노이즈를 문턱으로 자른 두 단(디더·결 없음).
/// 포스터·달력·책 표지는 8색 양자화 + (필요하면) 블록 저해상 + Nearest.
/// 원본 .mat·.png 는 건드리지 않는다 — 사본(*_roomflat)을 만들어 이 씬의 그 집 렌더러에만 꽂는다.
/// 다시 돌리면 같은 결과(시드 고정). 집 빌더(library_import)를 다시 돌리면 원본 재질로 돌아가므로 이걸 다시 돌린다.
/// 실행: RoomFlatBuild.Build() (메뉴 PitCrew/Room Flat Build)
/// </summary>
public static class RoomFlatBuild
{
    const string OutDir = "Assets/Art/Library/Buildings/room_flat";
    const int PaletteN = 6, PosterN = 8;
    const float StainMeters = 0.55f;   // 얼룩 한 덩이 크기(m)
    const float StainDark = 0.955f;    // 얼룩 단 = 바탕 × 이 값(밝기 약 4.5% 차)
    const float BoxPad = 1.06f;        // 방 상자(-1..1) 바깥으로 조금 — 벽 안쪽 면이 상자 경계에 걸침
    const float ViewDist = 2.0f;       // 포스터 저해상 기준 거리(m) — 한 텍셀 = 화면 약 2.5 px
    static readonly string[] PosterKeys = { "POSTER", "CALENDAR", "BOOK", "MAGAZINE", "PHOTO", "CERT", "NOTEBOOK", "FRAME" };
    static readonly string[] BigKeys = { "HOUSE_shell", "DOOR", "BED", "DESK", "FUTON", "SHELF", "CHAIR", "ROOM_props", "RAIL", "CURTAIN" };

    class Cell { public RectInt r; public string obj; public float area; public bool inBox; public float dens; public int group; }

    [MenuItem("PitCrew/Room Flat Build")]
    public static string Build()
    {
        var room = FindRoom("ROOM_2F");
        if (room == null) return "ROOM_2F 없음";
        if (!A7ShadeGlobals.TryRoomBox(room, out var box)) return "방 상자 못 잡음(레이)";
        var house = room.parent;
        var log = new System.Text.StringBuilder();
        Directory.CreateDirectory(OutDir);

        // 렌더러를 원본 재질(아틀라스)별로 묶는다 — 이미 사본이 꽂혀 있으면 원본으로 거슬러 간다
        var byMat = new Dictionary<Material, List<Renderer>>();
        foreach (var r in house.GetComponentsInChildren<Renderer>(true))
        {
            var m = Original(r.sharedMaterial);
            if (m == null || m.shader == null || m.shader.name != "PitCrew/SceneA7/Lit" || m.GetTexture("_BaseMap") == null) continue;
            if (!byMat.TryGetValue(m, out var l)) byMat[m] = l = new List<Renderer>();
            l.Add(r);
        }

        // 1) 칸 모으기 + 평균색
        var jobs = new List<(Material src, Texture2D tex, Color32[] px, List<Cell> cells, List<Renderer> rs)>();
        foreach (var kv in byMat)
        {
            var srcTex = (Texture2D)kv.Key.GetTexture("_BaseMap");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(srcTex)));
            var cells = new List<Cell>();
            foreach (var r in kv.Value) CollectCells(r, box, tex.width, tex.height, cells);
            cells = MergeCells(cells);
            jobs.Add((kv.Key, tex, tex.GetPixels32(), cells, kv.Value));
            log.Append($"{kv.Key.name}: 렌더러 {kv.Value.Count} · 방 칸 {cells.Count(c => c.inBox)}\n");
        }

        // 2) 큰 면 평균색 → 팔레트 6색(면적 가중 k-means)
        var samples = new List<(Vector3 c, float w)>();
        foreach (var j in jobs)
            foreach (var c in j.cells.Where(c => c.inBox && c.group == 0))
                samples.Add((Mean(j.px, j.tex.width, c.r), c.area));
        var pal = KMeans(samples, PaletteN);
        log.Append("팔레트: " + string.Join(" ", pal.Select(p => "#" + ColorUtility.ToHtmlStringRGB(new Color(p.x, p.y, p.z)))) + "\n");

        // 3) 칠하기
        float fov = Camera.main != null ? Camera.main.fieldOfView : 60f;
        float pxPerM = 360f / (2f * ViewDist * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad));
        int nBig = 0, nSmall = 0, nPoster = 0;
        foreach (var j in jobs)
        {
            int W = j.tex.width;
            var px = j.px;
            // 텍셀마다 주인 칸(없으면 null) — 칸 둘레 1px 을 칠할 때 남의 칸을 안 덮게
            var owner = new Cell[px.Length];
            foreach (var c in j.cells)
                for (int y = Mathf.Max(0, c.r.yMin); y < Mathf.Min(j.tex.height, c.r.yMax); y++)
                    for (int x = Mathf.Max(0, c.r.xMin); x < Mathf.Min(W, c.r.xMax); x++) owner[y * W + x] = c;
            foreach (var c in j.cells.Where(c => c.inBox))
            {
                if (c.group == 2) { nPoster++; continue; } // 포스터는 물건 단위로 아래에서
                var mean = Mean(px, W, c.r);
                var col = c.group == 0 ? Nearest(pal, mean) : mean;
                if (c.group == 0) nBig++; else nSmall++;
                Fill(px, W, j.tex.height, c, col, owner);
            }
            // 포스터·책: 물건 하나의 칸들을 같이 8색으로
            foreach (var g in j.cells.Where(c => c.inBox && c.group == 2).GroupBy(c => c.obj))
            {
                var list = g.ToList();
                int blk = Mathf.Max(1, Mathf.RoundToInt(2.5f * list.Max(c => c.dens) / pxPerM));
                Posterize(px, W, list, blk);
                log.Append($"  {g.Key}: 칸 {list.Count} · 블록 {blk}텍셀 (밀도 {list.Max(c => c.dens):F0}/m)\n");
            }
            var outTex = new Texture2D(W, j.tex.height, TextureFormat.RGBA32, false);
            outTex.SetPixels32(px); outTex.Apply();
            var srcPath = AssetDatabase.GetAssetPath(j.src.GetTexture("_BaseMap"));
            var dstPath = $"{OutDir}/{Path.GetFileNameWithoutExtension(srcPath)}_roomflat.png";
            File.WriteAllBytes(dstPath, outTex.EncodeToPNG());
            AssetDatabase.ImportAsset(dstPath, ImportAssetOptions.ForceUpdate);
            CopyImporter(srcPath, dstPath);
            var dstTex = AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath);

            var matPath = $"{OutDir}/{j.src.name}_roomflat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(j.src); AssetDatabase.CreateAsset(mat, matPath); }
            else mat.CopyPropertiesFromMaterial(j.src);
            mat.name = j.src.name + "_roomflat";
            mat.SetTexture("_BaseMap", dstTex);
            if (mat.HasProperty("_SN")) mat.SetFloat("_SN", 0f);   // A6d 표면 결(±4% 점무늬) 끔 — 결 0
            EditorUtility.SetDirty(mat);
            foreach (var r in j.rs)
            {
                var ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) if (Original(ms[i]) == j.src) ms[i] = mat;
                Undo.RecordObject(r, "Room flat");
                r.sharedMaterials = ms;
                EditorUtility.SetDirty(r);
            }
            Object.DestroyImmediate(j.tex); Object.DestroyImmediate(outTex);
        }
        AssetDatabase.SaveAssets();
        // 원본으로 돌아가는 길: 사본 재질 → 원본 재질 짝을 남긴다
        File.WriteAllText($"{OutDir}/pairs.txt", string.Join("\n", jobs.Select(j => $"{AssetDatabase.GetAssetPath(j.src)}\t{OutDir}/{j.src.name}_roomflat.mat")));
        EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
        log.Append($"큰 면 {nBig} · 작은 소품 {nSmall} · 포스터류 {nPoster} 칸");
        return log.ToString();
    }

    /// <summary>사본(*_roomflat) 재질이면 원본을, 아니면 그대로.</summary>
    public static Material Original(Material m)
    {
        if (m == null || !m.name.EndsWith("_roomflat")) return m;
        var pairs = $"{OutDir}/pairs.txt";
        if (!File.Exists(pairs)) return m;
        foreach (var line in File.ReadAllLines(pairs))
        {
            var p = line.Split('\t');
            if (p.Length == 2 && Path.GetFileNameWithoutExtension(p[1]) == m.name) return AssetDatabase.LoadAssetAtPath<Material>(p[0]) ?? m;
        }
        return m;
    }

    static Transform FindRoom(string n)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.name == n && t.parent != null) return t;
        return null;
    }

    static int Group(string obj)
    {
        foreach (var k in PosterKeys) if (obj.StartsWith(k)) return 2;
        foreach (var k in BigKeys) if (obj.StartsWith(k)) return 0;
        return 1;
    }

    static void CollectCells(Renderer r, Matrix4x4 box, int W, int H, List<Cell> cells)
    {
        var mf = r.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;
        var mesh = mf.sharedMesh; var v = mesh.vertices; var uv = mesh.uv; var tri = mesh.triangles;
        if (uv == null || uv.Length != v.Length) return;
        var l2w = r.transform.localToWorldMatrix;
        int g = Group(r.name);
        for (int t = 0; t < tri.Length; t += 3)
        {
            Vector3 a = l2w.MultiplyPoint3x4(v[tri[t]]), b = l2w.MultiplyPoint3x4(v[tri[t + 1]]), c = l2w.MultiplyPoint3x4(v[tri[t + 2]]);
            var q = box.MultiplyPoint3x4((a + b + c) / 3f);
            bool inBox = Mathf.Abs(q.x) <= BoxPad && Mathf.Abs(q.y) <= BoxPad && Mathf.Abs(q.z) <= BoxPad;
            Vector2 u0 = uv[tri[t]], u1 = uv[tri[t + 1]], u2 = uv[tri[t + 2]];
            float x0 = Mathf.Min(u0.x, Mathf.Min(u1.x, u2.x)) * W, x1 = Mathf.Max(u0.x, Mathf.Max(u1.x, u2.x)) * W;
            float y0 = Mathf.Min(u0.y, Mathf.Min(u1.y, u2.y)) * H, y1 = Mathf.Max(u0.y, Mathf.Max(u1.y, u2.y)) * H;
            var rect = new RectInt(Mathf.FloorToInt(x0 + 0.01f), Mathf.FloorToInt(y0 + 0.01f), 0, 0);
            rect.width = Mathf.Max(1, Mathf.CeilToInt(x1 - 0.01f) - rect.x); rect.height = Mathf.Max(1, Mathf.CeilToInt(y1 - 0.01f) - rect.y);
            float wArea = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            float uvArea = Mathf.Abs((u1.x - u0.x) * (u2.y - u0.y) - (u2.x - u0.x) * (u1.y - u0.y)) * 0.5f * W * H;
            cells.Add(new Cell { r = rect, obj = r.name, area = wArea, inBox = inBox, dens = wArea > 1e-6f ? Mathf.Sqrt(uvArea / wArea) : 0f, group = g });
        }
    }

    /// <summary>겹치는 삼각형 칸을 합친다(사각 면 = 삼각형 2). 칸 하나라도 방 안이면 방 칸.</summary>
    static List<Cell> MergeCells(List<Cell> src)
    {
        var outL = new List<Cell>();
        foreach (var c in src.OrderBy(c => c.r.x).ThenBy(c => c.r.y))
        {
            Cell hit = null;
            foreach (var o in outL) if (o.r.Overlaps(c.r) && o.obj == c.obj) { hit = o; break; }
            if (hit == null) { outL.Add(new Cell { r = c.r, obj = c.obj, area = c.area, inBox = c.inBox, dens = c.dens, group = c.group }); continue; }
            int x0 = Mathf.Min(hit.r.xMin, c.r.xMin), y0 = Mathf.Min(hit.r.yMin, c.r.yMin), x1 = Mathf.Max(hit.r.xMax, c.r.xMax), y1 = Mathf.Max(hit.r.yMax, c.r.yMax);
            hit.r = new RectInt(x0, y0, x1 - x0, y1 - y0);
            hit.area += c.area; hit.inBox |= c.inBox; hit.dens = Mathf.Max(hit.dens, c.dens);
        }
        return outL;
    }

    static Vector3 Mean(Color32[] px, int W, RectInt r)
    {
        double R = 0, G = 0, B = 0; int n = 0;
        int H = px.Length / W;
        for (int y = Mathf.Max(0, r.yMin); y < Mathf.Min(H, r.yMax); y++)
            for (int x = Mathf.Max(0, r.xMin); x < Mathf.Min(W, r.xMax); x++) { var p = px[y * W + x]; R += p.r; G += p.g; B += p.b; n++; }
        return n == 0 ? Vector3.one : new Vector3((float)(R / n / 255), (float)(G / n / 255), (float)(B / n / 255));
    }

    static Vector3 Nearest(List<Vector3> pal, Vector3 c) => pal.OrderBy(p => (p - c).sqrMagnitude).First();

    static List<Vector3> KMeans(List<(Vector3 c, float w)> s, int k)
    {
        var cent = new List<Vector3>();
        if (s.Count == 0) return cent;
        cent.Add(s.OrderByDescending(x => x.w).First().c);
        while (cent.Count < k && cent.Count < s.Count)   // 가장 먼 점부터(면적 가중) — 작은 면 하나가 팔레트를 먹지 않게 √면적
            cent.Add(s.OrderByDescending(x => cent.Min(cc => (cc - x.c).sqrMagnitude) * Mathf.Sqrt(x.w)).First().c);
        for (int it = 0; it < 30; it++)
        {
            var sum = new Vector3[cent.Count]; var w = new float[cent.Count];
            foreach (var x in s)
            {
                int bi = 0; float bd = float.MaxValue;
                for (int i = 0; i < cent.Count; i++) { float d = (cent[i] - x.c).sqrMagnitude; if (d < bd) { bd = d; bi = i; } }
                sum[bi] += x.c * x.w; w[bi] += x.w;
            }
            for (int i = 0; i < cent.Count; i++) if (w[i] > 0) cent[i] = sum[i] / w[i];
        }
        return cent;
    }

    static float Hash(int x, int y) { unchecked { uint h = (uint)(x * 374761393 + y * 668265263) ^ 0x5bd1e995u; h = (h ^ (h >> 13)) * 1274126177u; return ((h ^ (h >> 16)) & 0xffffff) / 16777215f; } }
    static float VNoise(float x, float y)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y); float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy), Hash(ix + 1, iy), fx), Mathf.Lerp(Hash(ix, iy + 1), Hash(ix + 1, iy + 1), fx), fy);
    }

    /// <summary>칸 = 한 색. 얼룩 = 저주파 노이즈(덩이 StainMeters)를 문턱으로 자른 두 번째 단(× StainDark). 칸 둘레 1px 도 같이(다른 칸은 안 침범).</summary>
    static void Fill(Color32[] px, int W, int H, Cell c, Vector3 col, Cell[] owner)
    {
        var a = new Color32((byte)Mathf.RoundToInt(col.x * 255), (byte)Mathf.RoundToInt(col.y * 255), (byte)Mathf.RoundToInt(col.z * 255), 255);
        var d = new Color32((byte)Mathf.RoundToInt(col.x * 255 * StainDark), (byte)Mathf.RoundToInt(col.y * 255 * StainDark), (byte)Mathf.RoundToInt(col.z * 255 * StainDark), 255);
        float per = Mathf.Max(1f, c.dens * StainMeters);   // 덩이 한 개 = 텍셀 몇 개
        float ox = Hash(c.r.x, 7) * 97f, oy = Hash(c.r.y, 13) * 97f;
        for (int y = c.r.yMin - 1; y < c.r.yMax + 1; y++)
            for (int x = c.r.xMin - 1; x < c.r.xMax + 1; x++)
            {
                if (x < 0 || y < 0 || x >= W || y >= H) continue;
                bool edge = x < c.r.xMin || y < c.r.yMin || x >= c.r.xMax || y >= c.r.yMax;
                if (edge && owner[y * W + x] != null) continue;
                float n = 0.65f * VNoise(x / per + ox, y / per + oy) + 0.35f * VNoise(x / per * 2.1f + oy, y / per * 2.1f + ox);
                int k = y * W + x;
                var keepA = px[k].a;
                var o2 = n > 0.66f ? d : a; o2.a = keepA;
                px[k] = o2;
            }
    }

    /// <summary>포스터·책: 물건 하나의 칸을 같이 8색 k-means 로 양자화, blk 텍셀 블록 평균(저해상). 흐리지 않는다 — 블록 안은 한 색, 경계는 그대로 Nearest.</summary>
    static void Posterize(Color32[] px, int W, List<Cell> cells, int blk)
    {
        var smp = new List<(Vector3 c, float w)>();
        foreach (var c in cells)
            for (int y = c.r.yMin; y < c.r.yMax; y += blk)
                for (int x = c.r.xMin; x < c.r.xMax; x += blk)
                    smp.Add((BlockMean(px, W, c.r, x, y, blk), 1f));
        var pal = KMeans(smp, PosterN);
        foreach (var c in cells)
            for (int y = c.r.yMin; y < c.r.yMax; y += blk)
                for (int x = c.r.xMin; x < c.r.xMax; x += blk)
                {
                    var q = Nearest(pal, BlockMean(px, W, c.r, x, y, blk));
                    var o = new Color32((byte)Mathf.RoundToInt(q.x * 255), (byte)Mathf.RoundToInt(q.y * 255), (byte)Mathf.RoundToInt(q.z * 255), 255);
                    for (int yy = y; yy < Mathf.Min(y + blk, c.r.yMax); yy++)
                        for (int xx = x; xx < Mathf.Min(x + blk, c.r.xMax); xx++) { int k = yy * W + xx; if (k < 0 || k >= px.Length) continue; o.a = px[k].a; px[k] = o; }
                }
    }

    static Vector3 BlockMean(Color32[] px, int W, RectInt r, int x0, int y0, int blk)
    {
        float R = 0, G = 0, B = 0; int n = 0;
        for (int y = y0; y < Mathf.Min(y0 + blk, r.yMax); y++)
            for (int x = x0; x < Mathf.Min(x0 + blk, r.xMax); x++) { int k = y * W + x; if (k < 0 || k >= px.Length) continue; var p = px[k]; R += p.r; G += p.g; B += p.b; n++; }
        return n == 0 ? Vector3.zero : new Vector3(R / n / 255f, G / n / 255f, B / n / 255f);
    }

    static void CopyImporter(string src, string dst)
    {
        var a = (TextureImporter)AssetImporter.GetAtPath(src); var b = (TextureImporter)AssetImporter.GetAtPath(dst);
        var s = new TextureImporterSettings(); a.ReadTextureSettings(s); b.SetTextureSettings(s);
        b.SetPlatformTextureSettings(a.GetDefaultPlatformTextureSettings());
        foreach (var plat in new[] { "Standalone", "Android", "iPhone", "WebGL" })
        {
            var ps = a.GetPlatformTextureSettings(plat);
            if (ps.overridden) b.SetPlatformTextureSettings(ps);
        }
        b.SaveAndReimport();
    }
}
