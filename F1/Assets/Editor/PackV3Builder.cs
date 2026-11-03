using PitCrew.PackBench;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// D-290 포장 v3 빌더(SPEC §6-4): 보관소 안에 받는 곳 선반 + 새 포장대(상자 소·중·대 + 뽁뽁이 롤·종이 롤) + 첫날 직원.
/// 자리는 옛 PackSpot·OrderBoard 에서 계산한다(손으로 찍은 좌표 없음). 상자 크기·롤 두께 = world.yaml packing(WorldOrders).
/// 다시 돌리면 PackV3 를 지우고 새로 만든다 — 씬에서 손으로 고친 것은 사라진다.
/// 모두 회색 대역(옛 작업대 재질 재사용) — 아트 아님.
/// </summary>
public static class PackV3Builder
{
    const string RootName = "PackV3";
    const string FontPath = "Assets/UI/Fonts/Pretendard/Pretendard-Regular Dynamic SDF.asset";

    [MenuItem("PitCrew/World/Build Pack v3 (D-290)")]
    public static void BuildMenu() => Debug.Log(Build());

    public static string Build()
    {
        var spotGo = GameObject.Find("OnFoot_HomeGarage/OnFoot_Depot/PackSpot");
        if (spotGo == null) return "FAIL: PackSpot 없음";
        var spot = spotGo.GetComponent<DepotPackSpot>();
        var depot = spotGo.transform.parent;
        var old = depot.Find(RootName); if (old != null) Object.DestroyImmediate(old.gameObject);

        // 옛 작업대 재질(회색 대역) — 공유 재질은 참조만 하고 고치지 않는다
        var oldBox = spotGo.GetComponentInChildren<PackBox>(true);
        Material boxMat = oldBox != null ? oldBox.boxMat : null, tapeMat = oldBox != null ? oldBox.tapeMat : null;
        Material grey = null;
        foreach (var r in spotGo.GetComponentsInChildren<Renderer>(true)) if (r.sharedMaterial != null && r.GetComponentInParent<PackBox>(true) == null) { grey = r.sharedMaterial; break; }
        if (grey == null) grey = boxMat;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        // 바닥 높이 = 옛 작업대 렌더러 맨 아래
        float floorY = spotGo.transform.position.y - 1.17f;
        var rs = spotGo.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0) { float m = float.MaxValue; foreach (var r in rs) if (r.enabled) m = Mathf.Min(m, r.bounds.min.y); if (m < float.MaxValue) floorY = m; }

        var root = new GameObject(RootName).transform;
        root.SetParent(depot, false);
        root.position = new Vector3(spotGo.transform.position.x, floorY, spotGo.transform.position.z);
        root.rotation = Quaternion.Euler(0f, spotGo.transform.eulerAngles.y, 0f);   // +z = 일하는 사람이 보는 쪽(벽 쪽)

        var w = WorldData.Orders;
        var pk = w != null ? w.packing : null;

        // ── 포장대 = 건물 카운터(도현이 놓은 것) 윗면 위: 앞 = 작업면, 뒤 = 낮은 선반(상자 3·롤 2) ──
        // 카운터 윗면 = PackSpot 아래 수평으로 겹치는 콜라이더(옛 작업대·PackV3 제외) 중 가장 높은 윗면
        float counterTop = floorY + 0.9f;
        {
            float best = float.MinValue; var sp = spotGo.transform.position;
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (c.isTrigger || c.transform.IsChildOf(spotGo.transform) || c.transform.IsChildOf(root)) continue;
                var cb = c.bounds;
                if (sp.x < cb.min.x || sp.x > cb.max.x || sp.z < cb.min.z || sp.z > cb.max.z) continue;
                if (cb.max.y < floorY + 0.4f || cb.max.y > sp.y + 0.5f) continue;
                best = Mathf.Max(best, cb.max.y);
            }
            if (best > float.MinValue) counterTop = best;
        }
        float topY = counterTop - floorY + 0.02f;
        const float depth = 0.93f, length = 2.10f;
        var table = Cube("PackTable", root, new Vector3(0f, topY - 0.01f, -0.02f), new Vector3(length, 0.02f, depth), grey);
        var riser = Cube("Shelf_Riser", root, new Vector3(0f, topY + 0.06f, 0.25f), new Vector3(length, 0.12f, 0.38f), grey);
        var t2 = table.gameObject.AddComponent<PackTable2>();
        var work = new GameObject("WorkSpot").transform; work.SetParent(root, false); work.localPosition = new Vector3(0f, topY, -0.22f);
        var rest = new GameObject("ParcelRest").transform; rest.SetParent(root, false); rest.localPosition = new Vector3(-0.75f, topY + 0.035f, -0.22f);
        t2.workSpot = work; t2.parcelRest = rest;

        // 상자 3(소·중·대) — 선반 위, 뚜껑 경첩은 벽 쪽
        var names = new[] { "소", "중", "대" };
        var inner = new[] { new Vector3(0.22f, 0.12f, 0.22f), new Vector3(0.315f, 0.15f, 0.315f), new Vector3(0.46f, 0.24f, 0.46f) };
        if (pk != null && pk.boxes != null) for (int i = 0; i < Mathf.Min(3, pk.boxes.Count); i++) { names[i] = pk.boxes[i].name; inner[i] = pk.boxes[i].inner; }
        float[] bx = { -0.85f, -0.48f, 0.0f };
        t2.boxes = new BoxPick[3];
        for (int i = 0; i < 3; i++)
        {
            var g = new GameObject($"Box_{names[i]}");
            g.transform.SetParent(root, false);
            g.transform.localPosition = new Vector3(bx[i], topY + 0.12f, 0.25f);
            var b = g.AddComponent<PackBox>();
            b.inner = inner[i]; b.sizeName = names[i]; b.boxMat = boxMat; b.tapeMat = tapeMat; b.startClosed = false;
            var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            b.Build();
            var lid = g.transform.Find("Shell/LidPivot"); if (lid != null) lid.localRotation = Quaternion.Euler(112f, 0f, 0f);
            var p = g.AddComponent<BoxPick>(); p.sizeIndex = i;
            t2.boxes[i] = p;
        }

        // 롤 2(뽁뽁이·종이) — 눕힌 원통
        t2.rolls = new Roll[2];
        for (int i = 0; i < 2; i++)
        {
            var g = new GameObject(i == 0 ? "Roll_Bubble" : "Roll_Paper");
            g.transform.SetParent(root, false);
            g.transform.localPosition = new Vector3(0.48f + i * 0.40f, topY + 0.12f + 0.09f, 0.25f);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            body.name = "Wound"; body.SetParent(g.transform, false);
            body.localRotation = Quaternion.Euler(0f, 0f, 90f); body.localScale = new Vector3(0.18f, 0.17f, 0.18f);
            body.GetComponent<Renderer>().sharedMaterial = i == 0 ? tapeMat != null ? tapeMat : grey : boxMat != null ? boxMat : grey;
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            core.name = "Core"; core.SetParent(g.transform, false);
            core.localRotation = Quaternion.Euler(0f, 0f, 90f); core.localScale = new Vector3(0.05f, 0.18f, 0.05f);
            core.GetComponent<Renderer>().sharedMaterial = grey; Object.DestroyImmediate(core.GetComponent<Collider>());
            var r = g.AddComponent<Roll>();
            r.kind = i == 0 ? PackFiller.Bubble : PackFiller.Paper; r.body = body;
            r.thick = i == 0 ? 0.02f : 0.01f; r.full = 12;
            if (pk != null && pk.fillers != null) foreach (var f in pk.fillers) if (f.kind == r.kind.ToString()) { r.thick = f.thick; r.full = f.roll; }
            t2.rolls[i] = r;
        }

        // ── 받는 곳 선반: 칠판 옆 뒷벽 쪽(칠판 → 앞마당 길을 막지 않는 쪽) ──
        var board = GameObject.Find("OnFoot_HomeGarage/OnFoot_Depot/OrderBoard");
        Vector3 shelfPos = board != null ? new Vector3(board.transform.position.x + 3.2f, floorY, -27.0f) : root.position + root.right * -4f;
        var shelfRoot = new GameObject("ReceiveShelf").transform;
        shelfRoot.SetParent(root, true); shelfRoot.position = shelfPos; shelfRoot.rotation = Quaternion.identity;
        Cube("Board_Low", shelfRoot, new Vector3(0f, 0.55f, 0f), new Vector3(1.0f, 0.03f, 0.42f), grey);
        Cube("Board_High", shelfRoot, new Vector3(0f, 0.98f, 0f), new Vector3(1.0f, 0.03f, 0.42f), grey);
        foreach (var px in new[] { -0.48f, 0.48f }) foreach (var pz in new[] { -0.19f, 0.19f })
            Cube("Post", shelfRoot, new Vector3(px, 0.6f, pz), new Vector3(0.03f, 1.2f, 0.03f), grey);
        var recv = shelfRoot.gameObject.AddComponent<ReceiveShelf>();

        var tr = w != null ? w.TraitOf("달걀 한 판") : null;
        Vector3 psize = tr != null ? tr.size : new Vector3(0.30f, 0.07f, 0.30f);
        var pgo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pgo.name = "Parcel_Eggs";
        pgo.transform.SetParent(shelfRoot, false);
        pgo.transform.localPosition = new Vector3(-0.15f, 0.995f + psize.y * 0.5f, 0f);
        pgo.transform.localScale = psize;
        pgo.GetComponent<Renderer>().sharedMaterial = grey;
        var prb = pgo.AddComponent<Rigidbody>(); prb.isKinematic = true; prb.mass = 0.6f;
        prb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;   // 든 상자가 움직여도 바닥판을 뚫고 빠지지 않게(D-290 버그)
        var item = pgo.AddComponent<PackItem>(); item.kind = PackItemKind.Eggs;
        var parcel = pgo.AddComponent<Parcel>();
        parcel.cargoName = tr != null ? tr.cargo : "달걀 한 판";
        var order = w != null && w.orders != null && w.orders.Count > 0 ? w.orders[0] : null;
        parcel.recipient = order != null && !string.IsNullOrEmpty(order.to) ? order.to : "줄리아";
        parcel.destLabel = "찻집";
        var places = WorldData.Places;
        if (places != null && order != null) foreach (var d in places.deliveryPoints) if (d.name == order.dest && !string.IsNullOrEmpty(d.label)) parcel.destLabel = d.label;

        // 종이 꼬리표(세상 속 종이 — 가까이 가면 켜짐)
        var tag = Cube("Parcel_Tag", shelfRoot, new Vector3(-0.15f, 0.995f + psize.y + 0.10f, -0.02f), new Vector3(0.20f, 0.13f, 0.004f), tapeMat != null ? tapeMat : grey);
        Object.DestroyImmediate(tag.GetComponent<Collider>());
        var txtGo = new GameObject("Text"); txtGo.transform.SetParent(shelfRoot, false);
        txtGo.transform.localPosition = new Vector3(-0.15f, 0.995f + psize.y + 0.10f, -0.025f);
        var tmp = txtGo.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.text = parcel.recipient + "\n" + parcel.destLabel; tmp.fontSize = 0.55f; tmp.color = new Color(0.12f, 0.1f, 0.08f);
        tmp.alignment = TextAlignmentOptions.Center; tmp.rectTransform.sizeDelta = new Vector2(0.19f, 0.12f);
        var tagRoot = new GameObject("TagRoot").transform; tagRoot.SetParent(shelfRoot, false);
        tag.SetParent(tagRoot, true); txtGo.transform.SetParent(tagRoot, true);
        parcel.tag3d = tagRoot.gameObject;
        recv.today = new[] { parcel };
        t2.receive = recv;

        // ── 첫날 직원(회색 기둥) + 말풍선(세상 속 글) ──
        var clerk = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
        clerk.name = "Depot_Clerk"; clerk.SetParent(root, true);
        clerk.position = shelfPos + new Vector3(1.4f, 0.85f, -0.3f); clerk.localScale = new Vector3(0.45f, 0.85f, 0.45f);
        clerk.GetComponent<Renderer>().sharedMaterial = grey;
        var bub = new GameObject("Clerk_Bubble"); bub.transform.SetParent(root, true);
        bub.transform.position = clerk.position + Vector3.up * 1.15f;
        var bt = bub.AddComponent<TextMeshPro>();
        if (font != null) bt.font = font;
        bt.fontSize = 1.1f; bt.color = Color.white; bt.alignment = TextAlignmentOptions.Center; bt.rectTransform.sizeDelta = new Vector2(2.4f, 0.4f);
        bt.text = t2.clerkLines[0];
        t2.clerk = clerk; t2.clerkBubble = bt;

        // 손 그리기
        var hands = root.gameObject.AddComponent<PackHands>(); hands.skin = grey;

        // DepotPackSpot.v3 배선
        if (spot != null)
        {
            var so = new SerializedObject(spot);
            so.FindProperty("v3").objectReferenceValue = t2;
            so.FindProperty("useV3").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var scene = spotGo.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"OK PackV3 root={root.position.ToString("F2")} floor={floorY:F2} counter={counterTop:F2} work={work.position.ToString("F2")} shelf={shelfPos.ToString("F2")} boxes={t2.boxes.Length} rolls={t2.rolls.Length} font={(font != null)} mat={(grey != null ? grey.name : "null")}";
    }

    static Transform Cube(string name, Transform parent, Vector3 lp, Vector3 size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name; g.transform.SetParent(parent, false);
        g.transform.localPosition = lp; g.transform.localScale = size;
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
        return g.transform;
    }
}
