using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2026-10-13 Claude — 브리프 town-density ①②: 건물당 생활 소품 5개 안팎 + 도로 디테일.
/// 3차(같은 날, 바닥·원경): 연석·얼룩·가로등·쓰레기통·소화전, 빈 마당의 풀·덤불·바위·꽃, 부두(석축·계류·잔교·배), 마을 밖 나무 띠.
/// CoastalTownInstaller.Layout() 이 건물을 하나 놓을 때마다 ForBuilding 을, 끝에 ForRoads/ForGround/ForHarbor/ForOutskirts 를 부른다.
/// 규칙은 전부 결정적(hash) — 같은 레이아웃이면 같은 결과. 물리·경로·건물 배치는 건드리지 않는다.
///
/// 기하(실측 2026-10-13): 도로 셀 6m, 차도 ±1.7m, 보도 1.7~3.0m(높이 0.12), 그 밖 3.0~4.5m 는 잔디 띠, 건물 중심은 도로 중심에서 6m.
/// 앞 소품은 hd+0.55 → 도로 중심에서 ~3.95m = 잔디 띠. 보도 위 가구(가로등·쓰레기통·소화전)는 2.4~2.6m, y=0.12.
/// </summary>
public static class CoastalTownDensity
{
    public delegate void Adder(string asset, Vector3 pos, float yaw, float scale, int palette);
    /// <summary>벽걸이·데칼 — 바닥 높이 검사 제외 대상 (원점이 바닥이 아님)</summary>
    public static readonly HashSet<string> Mounted = new HashSet<string> { "Awning", "AcUnit", "WallLamp" };
    const float SidewalkTop = 0.12f, DecalY = 0.013f;

    // 건물 몸통 로컬 치수 (lofi_pilot.py 의 box 치수, 돌출부 제외)
    /// <summary>4차: 건물 변형(CoastalHouseB 등)은 몸통이 같다 — 기본 이름으로.</summary>
    public static string BaseName(string asset) => asset.StartsWith("CornerShop") ? "CornerShop" : asset.StartsWith("RepairGarage") ? "RepairGarage" : "CoastalHouse";   // 그 밖(새 집 종류)은 집 규칙
    // 5차: 건물 치수는 art/source/coastal-town/building-catalog.json (Codex 가 건물마다 name/w/d/doorX 기록). 없으면 옛 3종 치수.
    static Dictionary<string, (float w, float d, float doorX)> catalog;
    static Dictionary<string, (float w, float d, float doorX)> Catalog()
    {
        if (catalog != null) return catalog;
        catalog = new Dictionary<string, (float, float, float)>();
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../art/source/coastal-town/building-catalog.json"));
        if (!System.IO.File.Exists(path)) return catalog;
        string text = System.IO.File.ReadAllText(path);
        var names = System.Text.RegularExpressions.Regex.Matches(text, @"""name""\s*:\s*""(\w+)""");
        for (int i = 0; i < names.Count; i++)
        {
            int start = names[i].Index; int end = i + 1 < names.Count ? names[i + 1].Index : text.Length;
            string chunk = text.Substring(start, end - start);
            float Num(string key, float fallback) { var m = System.Text.RegularExpressions.Regex.Match(chunk, "\"" + key + @"""\s*:\s*(-?[0-9.]+)"); return m.Success ? float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : fallback; }
            float w = Num("w", Num("width", 5f)), d = Num("d", Num("depth", 4.8f)), door = Num("frontDoorX", Num("doorX", Num("door_x", 0f)));
            catalog[names[i].Groups[1].Value] = (w, d, door);
        }
        return catalog;
    }
    static (float w, float d) Body(string asset)
    {
        if (Catalog().TryGetValue(asset, out var c)) return (c.w, c.d);
        string b = BaseName(asset);
        return b == "CornerShop" ? (6f, 5.2f) : b == "RepairGarage" ? (6.4f, 5.6f) : (5f, 4.8f);
    }
    static float DoorX(string asset, float s) => Catalog().TryGetValue(asset, out var c) ? c.doorX * s : (BaseName(asset) == "CornerShop" ? 2f * s : 0f);
    static float Yaw(Vector3 dir) => Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
    static int Hash(int c, int r, int salt = 0) => Math.Abs(c * 157 + r * 73 + salt * 31);

    public static void ForBuilding(Adder add, string asset, Vector3 p, Vector3 face, Vector3 right, float s, int hash, char zone, bool rightOpen, bool leftOpen)
    {
        string full = asset; asset = BaseName(asset);
        var (w, d) = Body(full);
        float hw = w * s / 2f, hd = d * s / 2f;
        float yaw = Yaw(face);                                          // 앞면이 도로를 보는 yaw (소품의 앞=+z 가 도로 쪽)
        float ps = s >= .95f ? 1f : Mathf.Clamp(s * 1.05f, .6f, .72f);   // 건물이 실제 치수(1.0)면 소품도 1.0. 아니면 살짝 크게
        int n = 0;
        // ---- 앞(잔디 띠): 2~3개. 문 앞은 비우고(집 문 x=0 / 상점 문 x=+2) 좌우로.
        float front = hd + 0.55f;
        string[] frontPool = asset == "CornerShop" ? new[] { "Planter", "SignTea", "Bin", "CrateStack", "Bench", "SignIce" }
                           : asset == "RepairGarage" ? new[] { "Barrel", "CrateStack", "SignRest", "Bollard", "Barrel", "Hydrant" }
                           : new[] { "Pots", "Planter", "Bench", "Pots", "Bollard", "Planter" };
        float doorX = DoorX(full, s);
        float[] slots = { -hw + .45f, hw - .45f, (doorX > 0 ? -hw * .35f : hw * .55f) };
        int take = 2 + (hash % 2);
        for (int i = 0; i < take; i++)
        {
            string a = frontPool[(hash / (i + 1) + i) % frontPool.Length];
            float x = slots[i]; if (Mathf.Abs(x - doorX) < 0.7f) x += (x < doorX ? -0.6f : 0.6f);
            add(a, p + face * front + right * x, yaw + (a.StartsWith("Sign") ? 0 : (hash + i) % 3 * 8 - 8), ps, 0); n++;
        }
        // ---- 옆(도로 없는 쪽): 1~2개. 상점은 열린 옆에 가판(Stall) 하나.
        var sides = new List<(Vector3 dir, bool open)> { (right, rightOpen), (-right, leftOpen) };
        bool stallDone = false;
        foreach (var (dir, open) in sides)
        {
            if (!open) continue;
            string a = asset == "RepairGarage" ? ((hash & 1) == 0 ? "TireStack" : "Barrel")
                     : asset == "CornerShop" ? (!stallDone ? "Stall" : ((hash & 1) == 0 ? "CrateStack" : "Crate"))
                     : ((hash % 3) == 0 ? "Barrel" : "Clothesline");
            float sideYaw = Yaw(dir);
            if (a == "Clothesline") add(a, p + dir * (hw + 2.4f * ps) - face * (hd * .3f), sideYaw, ps, 0);   // 벽끝이 벽에 닿게, 장대는 밖
            else if (a == "Stall") { add(a, p + dir * (hw + 1.3f) + face * (hd * .5f), yaw, ps, 0); stallDone = true; }   // 가판은 도로를 본다
            else add(a, p + dir * (hw + .5f) + face * (hd * .2f), sideYaw + 15, ps, 0);
            n++;
            if (n >= 4) break;
        }
        // ---- 벽걸이: 집은 창 위 차양, 전부 실외기 1 + 벽등 1 (상점은 차양 이미 있음)
        if (asset == "CoastalHouse")
        {
            float winX = ((hash >> 2) & 1) == 0 ? -1.4f * s : 1.4f * s;
            add("Awning", p + face * (hd + .46f) + right * winX, yaw, s, 0); n++;            // 원점=벽 접합 상단, 아래로 드리움
        }
        {
            var dir = rightOpen ? right : -right;
            add("AcUnit", p + dir * (hw + .16f) + face * (hd * .35f) + Vector3.up * (2.25f * s), Yaw(dir), s, 0); n++;
        }
        add("WallLamp", p + face * (hd + .12f) + right * (doorX + .65f * s) + Vector3.up * (2.15f * s), yaw, s, 0); n++;
    }

    static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    static int Degree(TrackLayout map, int c, int r) { int n = 0; foreach (var d in Dirs) if (map.IsRoad(c + d.x, r + d.y)) n++; return n; }
    static bool Straight(TrackLayout map, int c, int r, out Vector2Int axis)
    {
        axis = Vector2Int.zero; if (!map.IsRoad(c, r)) return false;
        var open = new List<Vector2Int>(); foreach (var d in Dirs) if (map.IsRoad(c + d.x, r + d.y)) open.Add(d);
        if (open.Count == 2 && open[0] == -open[1]) { axis = open[0]; return true; }
        return false;
    }

    /// <summary>
    /// 도로. 횡단보도는 "직선 셀이 교차로(이웃 3+)로 들어가는 끝"에만 — 2칸 폭 도로는 셀마다 이웃이 3이라 교차로로 잡혀 643개가 깔리던 것(1차)을 고침.
    /// 맨홀·배수구·얼룩·땜질은 직선 셀. 연석은 도로 셀의 도로 아닌 모든 변(보도 안쪽 모서리 1.7m). 가로등·쓰레기통·소화전은 보도 위.
    /// </summary>
    public static void ForRoads(Adder add, TrackLayout map)
    {
        for (int r = 0; r < map.Height; r++) for (int c = 0; c < map.Width; c++)
        {
            if (!map.IsRoad(c, r)) continue;
            var center = new Vector3(c * 6, DecalY, r * 6);
            int hash = Hash(c, r);
            // 연석: 도로 아닌 변마다 (보도가 있는 곳)
            foreach (var d in Dirs)
            {
                if (map.IsRoad(c + d.x, r + d.y)) continue;
                var side = new Vector3(d.x, 0, d.y);
                add("Curb", new Vector3(c * 6, 0, r * 6) + side * 1.76f, Yaw(side), 1f, 0);   // 긴 쪽(x)이 도로 축
            }
            if (!Straight(map, c, r, out var axisI)) continue;
            var axis = new Vector3(axisI.x, 0, axisI.y); var sideV = new Vector3(axis.z, 0, -axis.x);
            float ayaw = Yaw(axis);
            // 횡단보도: 이 직선 셀의 양 끝 중 교차로로 이어지는 쪽
            foreach (var e in new[] { axisI, -axisI })
                if (map.IsRoad(c + e.x, r + e.y) && Degree(map, c + e.x, r + e.y) >= 3)
                    add("Crosswalk", center + new Vector3(e.x, 0, e.y) * 1.95f, Yaw(new Vector3(e.x, 0, e.y)), 1f, 0);   // 3.4 가 차도 폭을 가로지르고 줄무늬는 도로 방향
            if ((c + r) % 3 == 0) add("Manhole", center + axis * 1.1f + sideV * .55f, ayaw, 1f, 0);
            if ((c + r) % 2 == 1) { add("Drain", center + sideV * 1.45f - axis * 1.6f, ayaw, 1f, 0); add("Drain", center - sideV * 1.45f + axis * 1.6f, ayaw, 1f, 0); }
            if (hash % 4 == 1) add("OilStain", center + axis * ((hash % 5) * .5f - 1f) + sideV * ((hash % 3) * .5f - .5f), hash % 360, .8f + hash % 3 * .2f, 0);
            if (hash % 5 == 3) add("Patch", center - axis * ((hash % 4) * .4f - .6f) + sideV * ((hash % 2) * .9f - .45f), ayaw + (hash % 2) * 90, 1f, 0);
            // 보도 가구: 도로 아닌 변에만. 가로등은 4셀마다 전신주와 엇갈리게, 쓰레기통·소화전은 드문드문.
            foreach (var d in Dirs)
            {
                if (map.IsRoad(c + d.x, r + d.y)) continue;
                if (Mathf.Abs(d.x) == Mathf.Abs(axisI.x) && Mathf.Abs(d.y) == Mathf.Abs(axisI.y)) continue;   // 축 방향 변은 보도 없음
                var side = new Vector3(d.x, 0, d.y); float faceYaw = Yaw(-side);
                var walk = new Vector3(c * 6, SidewalkTop, r * 6) + side * 2.5f;
                int pick = (c + r + (d.x > 0 || d.y < 0 ? 3 : 0)) % 6;
                if (pick == 2) add("StreetLamp", walk - axis * 1.2f, faceYaw, .95f, 0);
                else if (pick == 5) add("Bin", walk + axis * 1.4f, faceYaw + hash % 40, .9f, 0);
                else if (pick == 4 && hash % 2 == 0) add("Hydrant", walk + axis * .3f, faceYaw, .9f, 0);
            }
        }
    }

    /// <summary>빈 마당(도로 아님·건물 없음): 구역별로 풀·꽃·덤불·바위 2~4개. 공업은 바위·드럼통·상자, 항구는 바위·상자·풀, 다운타운은 꽃·풀·덤불.</summary>
    public static void ForGround(Adder add, TrackLayout map)
    {
        for (int r = 0; r < map.Height; r++) for (int c = 0; c < map.Width; c++)
        {
            if (map.IsRoad(c, r) || map.BuildingChar(c, r) != '.') continue;
            if (r * 6 > 116) continue;   // 마지막 두 행(z 120·126)은 바다 — 풀·상자가 물 위에 뜨던 것(3차 첫 캡처)
            char zone = map.GetZoneChar(c, r); int hash = Hash(c, r, 5);
            bool nearRoad = Degree(map, c, r) > 0;
            string[] pool = zone == 'I' ? new[] { "Rock", "GrassTuft", "Barrel", "Rock", "CrateStack", "GrassTuft", "Shrub" }
                          : zone == 'H' ? new[] { "GrassTuft", "Rock", "Crate", "GrassTuft", "Shrub", "Barrel", "Rock" }
                          : new[] { "FlowerPatch", "GrassTuft", "Shrub", "GrassTuft", "FlowerPatch", "Rock", "Shrub" };
            int count = 2 + hash % 3;
            for (int i = 0; i < count; i++)
            {
                int h = Hash(c, r, 11 + i);
                string a = pool[(h / 7) % pool.Length];
                // 셀 안 ±2.2m (보도는 도로 셀 쪽 1.7~3.0m 에 있어 빈 셀 전체가 잔디)
                float ox = (h % 9) * .55f - 2.2f, oz = ((h / 9) % 9) * .55f - 2.2f;
                var pos = new Vector3(c * 6 + ox, a == "FlowerPatch" ? DecalY : 0f, r * 6 + oz);
                float sc = a == "Rock" || a == "Shrub" ? .7f + (h % 4) * .15f : .85f + (h % 3) * .15f;
                add(a, pos, h % 360, sc, 0);
            }
            // 도로 옆 빈 셀 셋 중 하나엔 가로수
            if (nearRoad && hash % 3 == 1) add("CoastalTree", new Vector3(c * 6 + (hash % 5) * .4f - .8f, 0, r * 6 + ((hash / 5) % 5) * .4f - .8f), hash % 360, .72f + (hash % 3) * .06f, 0);
        }
    }

    /// <summary>
    /// 항구 가장자리: 마지막 도로 행(row 19, z=114) 남쪽은 모래 띠, 바다는 z=120 부터. 석축 12m 조각을 z=119.4 에 띠로, 6m 마다 계류 말뚝,
    /// 잔교 3개(x=36·90·144)가 바다로 14m, 배 5척은 잔교 옆에 떠 있다. 석축엔 MeshCollider 가 있어 차가 바다로 못 떨어진다.
    /// </summary>
    public static void ForHarbor(Adder add, TrackLayout map)
    {
        float z = 119.4f; float xMin = -9f, xMax = (map.Width - 1) * 6 + 9f;   // 땅 끝과 같은 폭
        for (float x = xMin + 6f; x <= xMax - 6f + 0.01f; x += 12f)
        {
            add("QuaySegment", new Vector3(x, -0.4f, z), 0f, 1f, 0);
            foreach (float mx in new[] { x - 3f, x + 3f }) add("Mooring", new Vector3(mx, 0.3f, z - .25f), 0f, 1f, 0);
        }
        foreach (float px in new[] { 36f, 90f, 144f })
        {
            add("Pier", new Vector3(px, -0.5f, z + .7f), 0f, 1f, 0);           // 앞(+z)으로 14m, 바다 쪽
            int h = Hash((int)px, 0, 3);
            add("Boat", new Vector3(px + 2.4f, -0.3f, z + 5f + h % 4), 90f + h % 20 - 10, 1f, 0);
            if (h % 2 == 0) add("Boat", new Vector3(px - 2.6f, -0.3f, z + 9f + h % 3), -90f + h % 16 - 8, .9f, 0);
            add("CrateStack", new Vector3(px - 3.5f, 0f, z - 2.4f), h % 90, .7f, 0); add("Barrel", new Vector3(px + 3.2f, 0f, z - 2.2f), 0f, .7f, 0);
        }
        add("Boat", new Vector3(12f, -0.3f, z + 7f), 70f, 1f, 0);
        add("Boat", new Vector3(170f, -0.3f, z + 6f), -110f, 1f, 0);
    }

    /// <summary>마을 밖 나무 띠: 땅 끝(x −9~183, z −9~117) 바깥 10~75m 를 9m 격자로, 셋 중 둘. 바다 쪽(z>117)은 없음. y=-0.06 은 FarGround 높이.</summary>
    public static void ForOutskirts(Adder add, TrackLayout map)
    {
        float xMin = -9f, xMax = (map.Width - 1) * 6 + 9f, zMin = -9f, zMax = 117f;
        for (float x = xMin - 75f; x <= xMax + 75f; x += 9f) for (float z = zMin - 75f; z <= zMax; z += 9f)
        {
            bool inside = x > xMin - 10f && x < xMax + 10f && z > zMin - 10f && z < zMax;
            if (inside) continue;
            int h = Hash((int)x, (int)z, 7); if (h % 3 == 0) continue;
            float jx = (h % 7) * .9f - 2.7f, jz = ((h / 7) % 7) * .9f - 2.7f;
            add("CoastalTree", new Vector3(x + jx, -0.06f, z + jz), h % 360, .9f + (h % 5) * .1f, 0);
        }
    }
}
