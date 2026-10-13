using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2026-10-13 Claude — 브리프 town-density ①②: 건물당 생활 소품 5개 안팎 + 도로 디테일.
/// CoastalTownInstaller.Layout() 이 건물을 하나 놓을 때마다 ForBuilding 을, 끝에 ForRoads 를 부른다.
/// 규칙은 전부 결정적(hash) — 같은 레이아웃이면 같은 결과. 물리·경로·건물 배치는 건드리지 않는다.
///
/// 기하: 건물 중심 p, 도로 쪽 단위벡터 face, 오른쪽 right, 설치 스케일 s. 도로 셀 중심까지 6m, 도로 가장자리 3m,
/// 보도 1.3m 가 1.7~3.0m 띠. 건물 앞면이 1.5~1.7m 라 앞 소품은 보도 위(1.9~2.5m), 길 가장자리에서 0.5m 이상 띄운다.
/// </summary>
public static class CoastalTownDensity
{
    public delegate void Adder(string asset, Vector3 pos, float yaw, float scale, int palette);
    /// <summary>벽걸이·데칼 — 바닥 높이 검사 제외 대상 (원점이 바닥이 아님)</summary>
    public static readonly HashSet<string> Mounted = new HashSet<string> { "Awning", "AcUnit", "WallLamp" };

    // 건물 몸통 로컬 치수 (lofi_pilot.py 의 box 치수, 돌출부 제외)
    static (float w, float d) Body(string asset) => asset == "CornerShop" ? (6f, 5.2f) : asset == "RepairGarage" ? (6.4f, 5.6f) : (5f, 4.8f);

    public static void ForBuilding(Adder add, string asset, Vector3 p, Vector3 face, Vector3 right, float s, int hash, char zone, bool rightOpen, bool leftOpen)
    {
        var (w, d) = Body(asset);
        float hw = w * s / 2f, hd = d * s / 2f;
        float yaw = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg;      // 앞면이 도로를 보는 yaw (소품의 앞=+z 가 도로 쪽)
        float ps = Mathf.Clamp(s * 1.05f, .6f, .72f);                  // 소품은 건물보다 살짝 크게 — 사람 크기가 읽히게
        int n = 0;
        // ---- 앞(보도): 2~3개. 문 앞은 비우고(집 문 x=0 / 상점 문 x=+2) 좌우로.
        float front = hd + 0.55f;
        string[] frontPool = asset == "CornerShop" ? new[] { "Planter", "SignTea", "Bench", "CrateStack", "Pots", "SignIce" }
                           : asset == "RepairGarage" ? new[] { "Barrel", "CrateStack", "SignRest", "Bollard", "Barrel", "Pots" }
                           : new[] { "Pots", "Planter", "Bench", "Pots", "Bollard", "Planter" };
        float doorX = asset == "CornerShop" ? 2f * s : 0f;
        float[] slots = { -hw + .45f, hw - .45f, (doorX > 0 ? -hw * .35f : hw * .55f) };
        int take = 2 + (hash % 2);
        for (int i = 0; i < take; i++)
        {
            string a = frontPool[(hash / (i + 1) + i) % frontPool.Length];
            float x = slots[i]; if (Mathf.Abs(x - doorX) < 0.7f) x += (x < doorX ? -0.6f : 0.6f);
            add(a, p + face * front + right * x, yaw + (a.StartsWith("Sign") ? 0 : (hash + i) % 3 * 8 - 8), ps, 0); n++;
        }
        // ---- 옆(도로 없는 쪽): 1~2개
        var sides = new List<(Vector3 dir, bool open)> { (right, rightOpen), (-right, leftOpen) };
        foreach (var (dir, open) in sides)
        {
            if (!open) continue;
            string a = asset == "RepairGarage" ? ((hash & 1) == 0 ? "TireStack" : "Barrel")
                     : asset == "CornerShop" ? ((hash & 1) == 0 ? "CrateStack" : "Crate")
                     : ((hash % 3) == 0 ? "Barrel" : "Clothesline");
            float sideYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (a == "Clothesline") add(a, p + dir * (hw + 2.4f * ps) - face * (hd * .3f), sideYaw, ps, 0);   // 벽끝이 벽에 닿게, 장대는 밖
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
            var dir = rightOpen ? right : -right; float sgn = rightOpen ? 1 : -1;
            add("AcUnit", p + dir * (hw + .16f) + face * (hd * .35f) + Vector3.up * (2.25f * s), Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, s, 0); n++;
        }
        add("WallLamp", p + face * (hd + .12f) + right * (doorX + .65f * s) + Vector3.up * (2.15f * s), yaw, s, 0); n++;
    }

    /// <summary>도로: 교차로 팔마다 횡단보도, 직선 셀에 맨홀·배수구. 충돌 없음(데칼).</summary>
    public static void ForRoads(Adder add, TrackLayout map)
    {
        var dirs = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        for (int r = 0; r < map.Height; r++) for (int c = 0; c < map.Width; c++)
        {
            if (!map.IsRoad(c, r)) continue;
            var open = new List<Vector2Int>(); foreach (var d in dirs) if (map.IsRoad(c + d.x, r + d.y)) open.Add(d);
            var center = new Vector3(c * 6, 0.013f, r * 6);
            if (open.Count >= 3)
            {
                foreach (var d in open)
                    add("Crosswalk", center + new Vector3(d.x, 0, d.y) * 2.35f, Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg + 90f, 1f, 0);
                continue;
            }
            if (open.Count == 2 && open[0] == -open[1])
            {
                var axis = new Vector3(open[0].x, 0, open[0].y); var side = new Vector3(axis.z, 0, -axis.x);
                float ayaw = Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg;
                if ((c + r) % 3 == 0) add("Manhole", center + axis * 1.1f + side * .55f, ayaw, 1f, 0);
                if ((c + r) % 2 == 1) { add("Drain", center + side * 1.45f - axis * 1.6f, ayaw, 1f, 0); add("Drain", center - side * 1.45f + axis * 1.6f, ayaw, 1f, 0); }
            }
        }
    }
}
