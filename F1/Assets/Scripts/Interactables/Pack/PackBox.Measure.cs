using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.PackBench
{
    // D-287: PackBox 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
    public partial class PackBox
    {
        // ── 내용물 ──
        static readonly Collider[] buf = new Collider[256];
        readonly List<PackItem> contents = new List<PackItem>();

        /// <summary>지금 안쪽 부피에 걸친 물건들(중심이 안에 있는 것).</summary>
        public List<PackItem> Contents()
        {
            contents.Clear();
            int n = Physics.OverlapBoxNonAlloc(InnerCenterWorld, inner * 0.5f, buf, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var it = buf[i].GetComponentInParent<PackItem>();
                if (it == null || contents.Contains(it)) continue;
                if (!InsideInner(it.transform.position, 0.005f)) continue;
                contents.Add(it);
            }
            return contents;
        }

        readonly List<PackItem> shakeSet = new List<PackItem>();
        public bool HasContent(PackItem it) => shakeSet.Contains(it);

        public bool InsideInner(Vector3 world, float margin)
        {
            var p = transform.InverseTransformPoint(world) - InnerCenterLocal;
            return Mathf.Abs(p.x) <= inner.x * 0.5f + margin && Mathf.Abs(p.y) <= inner.y * 0.5f + margin && Mathf.Abs(p.z) <= inner.z * 0.5f + margin;
        }

        static Bounds WorldBounds(PackItem it)
        {
            var cols = it.GetComponentsInChildren<Collider>();
            var b = cols.Length > 0 ? cols[0].bounds : new Bounds(it.transform.position, Vector3.zero);
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            return b;
        }

        /// <summary>빈 부피 비율(0~1) + 바닥 한 줄 중 완충재가 있는 비율.</summary>
        public void MeasureFill(out float gap, out float bottomCushion)
        {
            const int NX = 8, NY = 5, NZ = 8;
            int filled = 0, total = NX * NY * NZ, bottom = 0;
            float cell = Mathf.Min(inner.x / NX, Mathf.Min(inner.y / NY, inner.z / NZ));
            for (int y = 0; y < NY; y++)
                for (int x = 0; x < NX; x++)
                    for (int z = 0; z < NZ; z++)
                    {
                        var lp = new Vector3((x + 0.5f) / NX - 0.5f, (y + 0.5f) / NY - 0.5f, (z + 0.5f) / NZ - 0.5f);
                        lp = InnerCenterLocal + Vector3.Scale(lp, inner);
                        var wp = transform.TransformPoint(lp);
                        int n = Physics.OverlapSphereNonAlloc(wp, cell * 0.45f, buf, ~0, QueryTriggerInteraction.Ignore);
                        PackItemKind? kind = null;
                        for (int i = 0; i < n; i++) { var it = buf[i].GetComponentInParent<PackItem>(); if (it != null) { kind = it.kind; if (it.kind != PackItemKind.Eggs) break; } }
                        if (kind == null) continue;
                        filled++;
                        if (y == 0 && kind != PackItemKind.Eggs) bottom++;
                    }
            gap = 1f - (float)filled / total;
            bottomCushion = (float)bottom / (NX * NZ);
        }

        // 면 순서: +X −X +Y −Y +Z −Z (달걀판 자기 축)
        public static readonly string[] FaceNames = { "+X", "-X", "+Y(위)", "-Y(바닥)", "+Z", "-Z" };

        /// <summary>달걀판 6면 받침 비율(0~1)과 바닥이 상자 바닥에 맨살로 닿은 비율.</summary>
        public float[] MeasureSupport(PackItem tray, out float bareFloor)
        {
            var fr = new float[6];
            bareFloor = 0f;
            var bc = tray.GetComponent<BoxCollider>();
            if (bc == null) return fr;
            var t = tray.transform;
            Vector3 half = Vector3.Scale(bc.size, t.lossyScale) * 0.5f;
            Vector3 c = t.TransformPoint(bc.center);
            Vector3[] ax = { t.right, t.up, t.forward };
            for (int f = 0; f < 6; f++)
            {
                int a = f / 2; float sg = f % 2 == 0 ? 1f : -1f;
                Vector3 d = ax[a] * sg;
                Vector3 u = ax[(a + 1) % 3], v = ax[(a + 2) % 3];
                float hu = half[(a + 1) % 3], hv = half[(a + 2) % 3];
                int hit = 0, bare = 0;
                // 면을 3×3 칸으로 나눠 칸마다 바깥으로 supportTol 두께의 얇은 상자를 겹쳐 본다 — 광선 한 줄은 둥근 신문지 공 사이를 빠져나가서(1153) 칸 넓이로 본다
                var rot = Quaternion.LookRotation(v, d);
                Vector3 cellHalf = new Vector3(hu / 3f * 0.9f, supportTol * 0.5f, hv / 3f * 0.9f);
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                    {
                        Vector3 fc = c + d * half[a] + u * (i * hu * 2f / 3f) + v * (j * hv * 2f / 3f);
                        Vector3 sc = fc + d * (supportTol * 0.5f + 0.0005f);
                        int n = Physics.OverlapBoxNonAlloc(sc, cellHalf, buf, rot, ~0, QueryTriggerInteraction.Ignore);
                        bool ok = false;
                        for (int q = 0; q < n; q++)
                        {
                            var pi = buf[q].GetComponentInParent<PackItem>();
                            if (pi == tray) continue;
                            if (pi != null || buf[q].GetComponentInParent<PackBox>() == this) { ok = true; break; }
                        }
                        if (ok) hit++;
                        // 맨살 = 그 칸 한가운데에서 바로 아래 제일 가까운 것이 상자(완충재 아님)
                        Vector3 o = fc - d * 0.003f; float len = 0.003f + supportTol;
                        if (Physics.Raycast(o, d, len, ~0, QueryTriggerInteraction.Ignore) && !HasCushionBelow(o, d, len, tray)) bare++;
                    }
                fr[f] = hit / 9f;
                if (f == 3) bareFloor = bare / 9f;
            }
            return fr;
        }

        bool HasCushionBelow(Vector3 o, Vector3 d, float len, PackItem tray)
        {
            if (!Physics.Raycast(o, d, out var first, len, ~0, QueryTriggerInteraction.Ignore)) return false;
            // RaycastAll 은 순서가 없다 — 제일 가까운 것만 다시 본다
            var hits = Physics.RaycastAll(o, d, len, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; bool cushion = false;
            foreach (var h in hits)
            {
                var it = h.collider.GetComponentInParent<PackItem>();
                if (it == tray) continue;
                if (h.distance < best) { best = h.distance; cushion = it != null; }
            }
            return cushion;
        }
    }
}
