using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 부두 가장자리에 걷는 사람 전용 보이지 않는 난간을 만든다(20261102-2305 ①).
/// 0.5 m 격자로 "Piers" 판 위를 훑어서, 옆칸에 마른 땅(y&gt;0.5)이 없는 변마다 얇은 BoxCollider 를 놓고 같은 줄은 이어 붙인다.
/// 해변·잔디 가장자리는 건드리지 않는다(부두 판 칸만). 다시 돌려도 PierRails 를 지우고 새로 만든다.
/// ⚠ 부두 메시가 바뀌면 다시 돌려야 한다.
/// </summary>
public static class BuildPierRails
{
    const float Cell = 0.5f, Thick = 0.3f, Height = 1.2f;

    [MenuItem("F1/부두 난간 만들기")]
    public static string Run()
    {
        var old = GameObject.Find("PierRails"); if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject("PierRails");
        root.AddComponent<WalkOnlyRail>();
        // 부두 전체(손가락 부두·항구 부두 …)를 한 격자로 훑는다 — 구역을 나누면 이음매마다 가짜 난간이 선다.
        var piers = GameObject.Find("Piers"); if (piers == null) return "Piers 없음";
        var pb = new List<Bounds>(); Bounds all = default; bool first = true;
        foreach (var c in piers.GetComponentsInChildren<Collider>(true)) { pb.Add(c.bounds); if (first) { all = c.bounds; first = false; } else all.Encapsulate(c.bounds); }
        float x0 = Mathf.Floor(all.min.x) - 2f, x1 = Mathf.Ceil(all.max.x) + 2f, z0 = Mathf.Floor(all.min.z) - 2f, z1 = Mathf.Ceil(all.max.z) + 2f;
        int nx = Mathf.CeilToInt((x1 - x0) / Cell), nz = Mathf.CeilToInt((z1 - z0) / Cell);
        var deckY = new float[nx, nz]; var deck = new bool[nx, nz]; var ground = new bool[nx, nz];
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                var o = new Vector3(x0 + (i + .5f) * Cell, 2.2f, z0 + (j + .5f) * Cell);
                bool near = false; foreach (var b in pb) { var e = b; e.Expand(new Vector3(2f, 100f, 2f)); if (e.Contains(o)) { near = true; break; } }
                if (!near) continue;
                foreach (var h in Physics.RaycastAll(o, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.point.y < 0.5f) continue;
                    ground[i, j] = true;
                    if (h.collider.transform.parent != null && h.collider.transform.parent.name == "Piers") { deck[i, j] = true; deckY[i, j] = Mathf.Max(deckY[i, j], h.point.y); }
                }
            }
        // 변: dir 0=+x 1=-x 2=+z 3=-z
        var edge = new bool[4, nx, nz]; int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                if (!deck[i, j]) continue;
                for (int d = 0; d < 4; d++)
                {
                    int a = i + dx[d], b = j + dz[d];
                    bool g = a >= 0 && b >= 0 && a < nx && b < nz && ground[a, b];
                    edge[d, i, j] = !g;
                }
            }
        int boxes = 0, cells = 0;
        for (int d = 0; d < 4; d++)
        {
            bool alongX = d >= 2;   // ±z 쪽 변은 x 방향으로 이어진다
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    if (!edge[d, i, j]) continue;
                    int len = 1; float y = deckY[i, j];
                    while (true)
                    {
                        int a = alongX ? i + len : i, b = alongX ? j : j + len;
                        if (a >= nx || b >= nz || !edge[d, a, b]) break;
                        y = Mathf.Max(y, deckY[a, b]); len++;
                    }
                    for (int k = 0; k < len; k++) { edge[d, alongX ? i + k : i, alongX ? j : j + k] = false; }
                    cells += len;
                    float cx = x0 + (i + .5f) * Cell, cz = z0 + (j + .5f) * Cell;
                    float run = len * Cell;
                    Vector3 size, center;
                    if (alongX) { cx += (len - 1) * Cell * .5f; size = new Vector3(run, Height, Thick); center = new Vector3(cx, y + Height * .5f, cz + dz[d] * Cell * .5f); }
                    else { cz += (len - 1) * Cell * .5f; size = new Vector3(Thick, Height, run); center = new Vector3(cx + dx[d] * Cell * .5f, y + Height * .5f, cz); }
                    var go = new GameObject("Rail_" + d + "_" + boxes); go.transform.SetParent(root.transform, false); go.transform.position = center;
                    go.AddComponent<BoxCollider>().size = size;
                    boxes++;
                }
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        return "boxes=" + boxes + " edgeCells=" + cells;
    }
}
