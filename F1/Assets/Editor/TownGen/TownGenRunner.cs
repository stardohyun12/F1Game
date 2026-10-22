using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PitCrew.TownGen;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 마을 생성기 공통 뒷단의 Unity 쪽 — 실행·내보내기·검증 메뉴. 로직은 전부 TownGen.Core(엔진 독립).
/// 산출물:
///  - tools/town-gen/out/&lt;name&gt;/ : report.txt · report.json · grid.json(칸·연속 고도·배달지) · polylines.json(원본 보존 §4.3-6) · preview.png
///  - Assets/Data/TownGen/&lt;name&gt;_Layout.asset : 새 TrackLayout (기존 SO 수정 없음, asset.md "레이아웃은 SO")
/// 메인 ComplexTrack 씬은 건드리지 않는다. 씬 설치(⑨)·비교 그림(⑩)은 E3.
/// </summary>
public static class TownGenRunner
{
    public const string ConfigPath = "Assets/Data/TownGen/TownGenConfig.asset";
    const string LayoutDir = "Assets/Data/TownGen";

    static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
    public static string ToolsDir => Path.Combine(RepoRoot, "tools", "town-gen");

    public static TownGenSettings LoadSettings()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<TownGenConfig>(ConfigPath);
        return cfg != null ? cfg.settings.Clone() : new TownGenSettings();
    }

    [MenuItem("PitCrew/TownGen/Create Config")]
    public static void CreateConfig()
    {
        if (AssetDatabase.LoadAssetAtPath<TownGenConfig>(ConfigPath) != null) { Debug.Log("[TownGen] 설정 에셋 이미 있음: " + ConfigPath); return; }
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data", "TownGen"));
        var cfg = ScriptableObject.CreateInstance<TownGenConfig>();
        AssetDatabase.CreateAsset(cfg, ConfigPath);
        AssetDatabase.SaveAssets();
        Debug.Log("[TownGen] 설정 에셋 생성: " + ConfigPath);
    }

    /// <summary>도로망 JSON 한 개 → 뒷단 → 산출물. writeLayout 이면 TrackLayout 에셋도 만들고 TrackConnectivityVerifier 로 재확인.</summary>
    public static TownGenResult Run(string jsonPath, IHeightField heights, TownGenSettings s, string outName, bool writeLayout)
    {
        var errors = new List<string>(); var warnings = new List<string>();
        var net = PitCrew.TownGen.RoadNetwork.Parse(File.ReadAllText(jsonPath), errors, warnings);
        if (net == null) throw new InvalidDataException($"[TownGen] {jsonPath} 형식 오류: " + string.Join("; ", errors));

        var result = TownBackend.Run(net, heights, s);
        foreach (var w in warnings) result.report.Warn("입력: " + w);

        if (writeLayout && result.grid.start >= 0)
        {
            var layout = ExportLayout(result.grid, outName);
            var v = TrackConnectivityVerifier.Verify(layout, "TownGen/" + outName);
            result.report.Check("A1-verifier", v.reachable == v.totalRoad && v.totalRoad > 0,
                $"TrackConnectivityVerifier: 도로 {v.totalRoad}, 도달 {v.reachable} ({AssetDatabase.GetAssetPath(layout)})");
        }

        var dir = Path.Combine(ToolsDir, "out", outName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "report.txt"), result.report.ToText(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(dir, "report.json"), result.report.ToJson().ToString(), new UTF8Encoding(false));
        var gridJson = result.grid.ToJson();
        gridJson["settings"] = Newtonsoft.Json.Linq.JObject.FromObject(s);
        File.WriteAllText(Path.Combine(dir, "grid.json"), gridJson.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(dir, "polylines.json"), result.polylinesJson, new UTF8Encoding(false));
        File.WriteAllBytes(Path.Combine(dir, "preview.png"), Preview(result.grid, 8));
        Debug.Log(result.report.ToText());
        return result;
    }

    /// <summary>
    /// 새 TrackLayout 에셋. roadMask = 뒷단 결과, heightMask = 고도 단계(0~2) 표시용.
    /// cellHeightMeters = 0 으로 둔다 — 기존 설치기가 단계×높이로 계단을 만들지 않게. 연속 고도는 grid.json 에 있고 E3 설치가 쓴다.
    /// </summary>
    static TrackLayout ExportLayout(TownGrid g, string name)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Data", "TownGen"));
        var path = $"{LayoutDir}/{name}_Layout.asset";
        var layout = AssetDatabase.LoadAssetAtPath<TrackLayout>(path);
        bool isNew = layout == null;
        if (isNew) layout = ScriptableObject.CreateInstance<TrackLayout>();
        layout.roadMask = g.RoadMask();
        layout.heightMask = g.HeightMask();
        layout.buildingMask = null;
        layout.zoneMask = null;
        layout.cellHeightMeters = 0f;
        layout.startCell = new Vector2Int(g.Col(g.start), g.Row(g.start));
        layout.startYawDeg = g.startYawDeg;
        layout.deliveries = new List<TrackLayout.DeliveryCell>();
        foreach (var d in g.deliveries) layout.deliveries.Add(new TrackLayout.DeliveryCell { col = d.col, row = d.row, label = d.label });
        if (isNew) AssetDatabase.CreateAsset(layout, path); else EditorUtility.SetDirty(layout);
        AssetDatabase.SaveAssets();
        return layout;
    }

    /// <summary>톱다운 미리보기 PNG (위 = 북). 판정용 그림이 아니라 검사 확인용.</summary>
    public static byte[] Preview(TownGrid g, int px)
    {
        var tex = new Texture2D(g.W * px, g.H * px, TextureFormat.RGB24, false);
        double hmin = double.MaxValue, hmax = double.MinValue;
        foreach (var h in g.height) { hmin = Math.Min(hmin, h); hmax = Math.Max(hmax, h); }
        var deliveryCells = new HashSet<int>(); foreach (var d in g.deliveries) deliveryCells.Add(d.cell);
        for (int r = 0; r < g.H; r++)
            for (int c = 0; c < g.W; c++)
            {
                int i = g.Index(c, r);
                float t = hmax > hmin ? (float)((g.height[i] - hmin) / (hmax - hmin)) : 0f;
                Color col = Color.Lerp(new Color(.80f, .86f, .70f), new Color(.55f, .45f, .33f), t); // 땅: 낮음 연두 → 높음 갈색
                if (g.stepsDecor[i]) col = new Color(.55f, .75f, .95f);
                if (g.kind[i] == CellKind.Major) col = new Color(.20f, .20f, .22f);
                else if (g.kind[i] == CellKind.Minor)
                    col = g.origin[i] == CellOrigin.Connector ? new Color(.85f, .20f, .75f)
                        : (g.origin[i] == CellOrigin.PromotedA || g.origin[i] == CellOrigin.PromotedB) ? new Color(.95f, .55f, .10f)
                        : new Color(.48f, .48f, .52f);
                if (g.landmarks.Contains(i)) col = new Color(1f, .85f, .1f);
                if (deliveryCells.Contains(i)) col = new Color(.90f, .10f, .10f);
                if (i == g.start) col = new Color(.10f, .80f, .20f);
                for (int y = 0; y < px; y++)
                    for (int x = 0; x < px; x++)
                        tex.SetPixel(c * px + x, r * px + y, (x == 0 || y == 0) ? col * .92f : col);
            }
        tex.Apply();
        var png = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        return png;
    }

    // ------------------------------------------------------------------ 검증 메뉴 (E1)
    /// <summary>손으로 만든 시험 도로망 3개로 뒷단 전체를 돌린다. grid_flat 은 TrackLayout 내보내기 + TrackConnectivityVerifier 까지.</summary>
    [MenuItem("PitCrew/TownGen/Verify")]
    public static void VerifyMenu() => Debug.Log(Verify());

    public static string Verify()
    {
        var samples = Path.Combine(ToolsDir, "samples");
        var sb = new StringBuilder();
        var s = LoadSettings();
        s.seaLevelMeters = -1e6f; // 손 시험 JSON 은 높이 0 평지 — 바다 규칙 끔

        var flat = Run(Path.Combine(samples, "grid_flat.json"), new FlatHeightField(0), s, "sample_grid_flat", true);
        sb.AppendLine(Summary("grid_flat", flat));
        var again = TownBackend.Run(PitCrew.TownGen.RoadNetwork.Parse(File.ReadAllText(Path.Combine(samples, "grid_flat.json")), new List<string>(), new List<string>()), new FlatHeightField(0), s);
        sb.AppendLine($"  A7 결정성: {flat.grid.RoadMaskHash()} vs {again.grid.RoadMaskHash()} → {(flat.grid.RoadMaskHash() == again.grid.RoadMaskHash() ? "같음" : "다름")}");

        var small = s.Clone();
        small.boundsXMeters = 240; small.boundsZMeters = 96;
        small.nearMinMeters = 0; small.nearMaxMeters = 60; small.midMaxMeters = 150;
        small.nearCount = small.midCount = small.farCount = 1;
        small.deliveryLabels = new[] { "A", "B", "C" }; small.nearFixedLabel = "A"; small.midFixedLabel = "B"; small.farFixedLabel = "C";
        small.deliveryMinSpacingCells = 1;
        var isl = Run(Path.Combine(samples, "islands_footway.json"), new FlatHeightField(0), small, "sample_islands_footway", false);
        sb.AppendLine(Summary("islands_footway", isl));

        var hill = small.Clone(); hill.startCol = 1; hill.startRow = 7; hill.detourMaxRatio = 3f;
        var bump = new FuncHeightField((x, z) => 3.0 * Math.Exp(-((x - 124) * (x - 124) + (z - 60) * (z - 60)) / 576.0));
        var hd = Run(Path.Combine(samples, "hill_detour.json"), bump, hill, "sample_hill_detour", false);
        sb.AppendLine(Summary("hill_detour", hd));
        return sb.ToString();
    }

    static string Summary(string name, TownGenResult r)
    {
        int pass = 0; foreach (var c in r.report.checks) if (c.pass) pass++;
        var sb = new StringBuilder($"{name}: 검사 {pass}/{r.report.checks.Count}");
        foreach (var c in r.report.checks) if (!c.pass) sb.Append($" | FAIL {c.id}: {c.detail}");
        sb.Append($" | 도로 {r.grid.CountRoad()} · 승격a {r.report.MetricValue("promoted_footway_a")} · 승격b {r.report.MetricValue("promoted_footway_b")} · A* {r.report.MetricValue("connector_cells_astar")} · {r.report.MetricValue("backend_ms")}ms");
        return sb.ToString();
    }
}
