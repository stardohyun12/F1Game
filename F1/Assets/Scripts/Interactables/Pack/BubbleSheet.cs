using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.PackBench
{
    /// <summary>
    /// 뽁뽁이 한 장(2판, D-253) — 롤에서 끌어 당긴 길이만큼 풀려 나오고, 놓으면 끊긴다. 천처럼 바닥·물건 모양을 따라 접힌다.
    /// 천 = 격자 점(가로 7 × 세로 길이/5 cm) 베를레 스프링. Unity Cloth 는 상자(BoxCollider)와 안 부딪혀서 직접 만든다.
    /// 점마다 반지름 8 mm 공으로 주변 콜라이더(Box·Sphere)에서 밀려난다.
    /// 다 내려앉으면(0.5초 동안 거의 안 움직임) 시뮬레이션을 멈추고 칸마다 얇은 충돌 상자를 굽는다 → 달걀판이 그 위에 얹힌다.
    /// 상자 안에서 멈추면 상자 자식이 된다(상자를 들고 흔들면 같이 움직임 = 눌려 박힌 완충재).
    /// </summary>
    public class BubbleSheet : MonoBehaviour
    {
        public const float Width = 0.30f, Spacing = 0.05f, MaxLength = 1.0f, MinLength = 0.10f;
        public const float Radius = 0.008f, Thick = 0.008f;
        const int Cols = 7;

        public float Length { get; private set; }
        public bool Settled { get; private set; }
        public bool Pulling { get; private set; }
        public bool Held => pinRow >= 0;
        public static int SimSteps; // QA: 누적 시뮬레이션 칸 수
        public static float LastSimMs;

        Vector3[] pos, prev; int rows;
        Mesh mesh; Vector3[] verts; int[] tris;
        readonly List<BoxCollider> cells = new List<BoxCollider>();
        int pinRow = -1; Vector3 pinCenter, pinCross;
        int still;
        PackItem item;
        static readonly Collider[] buf = new Collider[32];

        public static BubbleSheet Create(Material mat)
        {
            var g = new GameObject("BubbleSheet");
            var s = g.AddComponent<BubbleSheet>();
            g.AddComponent<MeshFilter>();
            var mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            s.item = g.AddComponent<PackItem>(); s.item.kind = PackItemKind.Bubble; s.item.sheet = s;
            s.mesh = new Mesh { name = "BubbleSheet" }; s.mesh.MarkDynamic();
            g.GetComponent<MeshFilter>().sharedMesh = s.mesh;
            return s;
        }

        // ── 롤에서 풀기 ──
        /// <summary>롤 출구(exit)에서 hand 까지 직선으로 풀린 상태. cross = 시트 가로 방향(롤 축).</summary>
        public void Pull(Vector3 exit, Vector3 hand, Vector3 cross)
        {
            Pulling = true; Settled = false;
            Vector3 d = hand - exit;
            float len = Mathf.Clamp(d.magnitude, 0.02f, MaxLength);
            Vector3 dir = d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.left;
            cross = Vector3.ProjectOnPlane(cross, dir).normalized;
            int r = Mathf.Max(2, Mathf.CeilToInt(len / Spacing) + 1);
            if (r != rows) Alloc(r);
            Length = len;
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < Cols; j++)
                {
                    // 롤 쪽은 롤 겉면을 따라 살짝 늘어지게(가운데가 처짐)
                    float t = i / (float)(rows - 1);
                    Vector3 p = exit + dir * (t * len) + cross * ((j / (float)(Cols - 1) - 0.5f) * Width) + Vector3.down * (Mathf.Sin(t * Mathf.PI) * len * 0.06f);
                    pos[Idx(i, j)] = prev[Idx(i, j)] = p;
                }
            UpdateMesh();
        }

        /// <summary>놓음 = 끊김. 이제 혼자 떨어져 내려앉는다.</summary>
        public void Tear() { Pulling = false; Settled = false; still = 0; }

        void Alloc(int r)
        {
            rows = r;
            pos = new Vector3[rows * Cols]; prev = new Vector3[rows * Cols];
            verts = new Vector3[rows * Cols * 2];
            var t = new List<int>();
            int n = rows * Cols;
            for (int i = 0; i < rows - 1; i++)
                for (int j = 0; j < Cols - 1; j++)
                {
                    int a = Idx(i, j), b = Idx(i, j + 1), c = Idx(i + 1, j), d = Idx(i + 1, j + 1);
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                    // 뒷면(양면으로 보이게)
                    t.Add(n + a); t.Add(n + b); t.Add(n + c); t.Add(n + b); t.Add(n + d); t.Add(n + c);
                }
            tris = t.ToArray();
            mesh.Clear();
        }

        static int Idx(int i, int j) => i * Cols + j;

        // ── 손으로 집기(한 줄을 잡음 = 가장자리를 두 손으로 쥔 것) ──
        public void Grab(Vector3 at)
        {
            Unbake();
            transform.SetParent(null, true);
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < rows; i++)
            {
                float dd = (pos[Idx(i, Cols / 2)] - at).sqrMagnitude;
                if (dd < bd) { bd = dd; best = i; }
            }
            // 끝줄을 잡는 게 펴기 쉽다 — 가운데를 집어도 가까운 끝줄로
            pinRow = best < rows / 2 ? 0 : rows - 1;
            pinCenter = pos[Idx(pinRow, Cols / 2)];
            pinCross = (pos[Idx(pinRow, Cols - 1)] - pos[Idx(pinRow, 0)]).normalized;
            Settled = false; still = 0;
        }

        /// <summary>잡은 줄을 hand 로(속도 상한). cross = 잡은 줄 방향.</summary>
        public void Hold(Vector3 hand, Vector3 cross, float maxSpeed, float dt)
        {
            if (pinRow < 0) return;
            pinCenter = Vector3.MoveTowards(pinCenter, hand, maxSpeed * dt);
            pinCross = Vector3.Slerp(pinCross, cross, 10f * dt).normalized;
        }

        public void Release() { pinRow = -1; still = 0; }

        public Vector3 HeldPoint => pinRow >= 0 ? pinCenter : Center;
        public Vector3 Center
        {
            get { Vector3 c = Vector3.zero; for (int i = 0; i < pos.Length; i++) c += pos[i]; return c / pos.Length; }
        }
        public Bounds WorldBounds
        {
            get { var b = new Bounds(pos[0], Vector3.zero); for (int i = 1; i < pos.Length; i++) b.Encapsulate(pos[i]); return b; }
        }

        // ── 시뮬레이션 ──
        void FixedUpdate()
        {
            if (Pulling || Settled || pos == null) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            const int Sub = 2;
            float h = Time.fixedDeltaTime / Sub;
            float maxMove = 0f;
            for (int s = 0; s < Sub; s++)
            {
                Vector3 g = Physics.gravity * h * h;
                for (int k = 0; k < pos.Length; k++)
                {
                    Vector3 v = (pos[k] - prev[k]) * 0.985f;
                    prev[k] = pos[k];
                    pos[k] += v + g;
                }
                for (int it = 0; it < 6; it++)
                {
                    if (pinRow >= 0)
                        for (int j = 0; j < Cols; j++)
                            pos[Idx(pinRow, j)] = pinCenter + pinCross * ((j / (float)(Cols - 1) - 0.5f) * Width);
                    float dx = Width / (Cols - 1), dy = Length / (rows - 1);
                    for (int i = 0; i < rows; i++)
                        for (int j = 0; j < Cols; j++)
                        {
                            int a = Idx(i, j);
                            if (j + 1 < Cols) Link(a, Idx(i, j + 1), dx, 1f);
                            if (i + 1 < rows) Link(a, Idx(i + 1, j), dy, 1f);
                            if (i + 1 < rows && j + 1 < Cols) { float dg = Mathf.Sqrt(dx * dx + dy * dy); Link(a, Idx(i + 1, j + 1), dg, 0.5f); Link(Idx(i, j + 1), Idx(i + 1, j), dg, 0.5f); }
                            // 굽힘 저항(약하게 — 뽁뽁이는 종이보다 뻣뻣)
                            if (j + 2 < Cols) Link(a, Idx(i, j + 2), dx * 2f, 0.08f);
                            if (i + 2 < rows) Link(a, Idx(i + 2, j), dy * 2f, 0.08f);
                        }
                }
                for (int k = 0; k < pos.Length; k++) Collide(k);
                if (pinRow >= 0)
                    for (int j = 0; j < Cols; j++)
                        pos[Idx(pinRow, j)] = pinCenter + pinCross * ((j / (float)(Cols - 1) - 0.5f) * Width);
                for (int k = 0; k < pos.Length; k++) maxMove = Mathf.Max(maxMove, (pos[k] - prev[k]).sqrMagnitude);
                SimSteps++;
            }
            if (pinRow < 0 && maxMove < 0.0004f * 0.0004f) still++; else still = 0;
            UpdateMesh();
            if (still > 25 || (pinRow < 0 && Center.y < -2f)) Bake();
            LastSimMs = (float)sw.Elapsed.TotalMilliseconds;
        }

        void Link(int a, int b, float rest, float k)
        {
            Vector3 d = pos[b] - pos[a];
            float m = d.magnitude;
            if (m < 1e-6f) return;
            Vector3 c = d * ((m - rest) / m * 0.5f * k);
            pos[a] += c; pos[b] -= c;
        }

        void Collide(int k)
        {
            Vector3 p = pos[k];
            int n = Physics.OverlapSphereNonAlloc(p, Radius, buf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = buf[i];
                if (c.transform.IsChildOf(transform)) continue;
                Vector3 push;
                if (c is BoxCollider bc) push = PushBox(bc, p);
                else if (c is SphereCollider sc) push = PushSphere(sc, p);
                else { Vector3 cp = c.ClosestPoint(p); Vector3 d = p - cp; float m = d.magnitude; push = m > 1e-5f && m < Radius ? d / m * (Radius - m) : Vector3.zero; }
                if (push == Vector3.zero) continue;
                p += push;
                // 마찰: 닿은 면 방향 속도를 깎는다(뽁뽁이가 상자 바닥에서 미끄러지지 않게)
                Vector3 nrm = push.normalized;
                Vector3 v = p - prev[k];
                Vector3 vt = v - Vector3.Dot(v, nrm) * nrm;
                prev[k] = p - vt * 0.4f;
            }
            pos[k] = p;
        }

        static Vector3 PushBox(BoxCollider bc, Vector3 p)
        {
            var t = bc.transform;
            Vector3 lp = t.InverseTransformPoint(p) - bc.center;
            Vector3 half = bc.size * 0.5f;
            Vector3 sc = t.lossyScale;
            Vector3 sc3 = new Vector3(Mathf.Abs(sc.x), Mathf.Abs(sc.y), Mathf.Abs(sc.z));
            // 바깥 거리를 월드 단위로
            Vector3 q = new Vector3(Mathf.Clamp(lp.x, -half.x, half.x), Mathf.Clamp(lp.y, -half.y, half.y), Mathf.Clamp(lp.z, -half.z, half.z));
            bool inside = q == lp;
            if (!inside)
            {
                Vector3 w = t.TransformPoint(q + bc.center);
                Vector3 d = p - w; float m = d.magnitude;
                return m < Radius && m > 1e-6f ? d / m * (Radius - m) : Vector3.zero;
            }
            // 안: 제일 가까운 면으로(월드 거리 기준)
            float best = float.MaxValue; int ax = 0; float sg = 1f;
            for (int a = 0; a < 3; a++)
            {
                float dp = (half[a] - lp[a]) * sc3[a], dn = (lp[a] + half[a]) * sc3[a];
                if (dp < best) { best = dp; ax = a; sg = 1f; }
                if (dn < best) { best = dn; ax = a; sg = -1f; }
            }
            Vector3 dir = Vector3.zero; dir[ax] = sg;
            Vector3 wn = t.TransformDirection(dir).normalized;
            return wn * (best + Radius);
        }

        static Vector3 PushSphere(SphereCollider sc, Vector3 p)
        {
            var t = sc.transform;
            Vector3 c = t.TransformPoint(sc.center);
            float r = sc.radius * Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Max(Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z)));
            Vector3 d = p - c; float m = d.magnitude;
            if (m >= r + Radius) return Vector3.zero;
            if (m < 1e-6f) return Vector3.up * (r + Radius);
            return d / m * (r + Radius - m);
        }

        void UpdateMesh()
        {
            if (pos == null) return;
            int n = pos.Length;
            if (verts == null || verts.Length != n * 2) verts = new Vector3[n * 2];
            for (int k = 0; k < n; k++)
            {
                Vector3 lp = transform.InverseTransformPoint(pos[k]);
                verts[k] = lp; verts[n + k] = lp;
            }
            if (mesh.vertexCount != verts.Length) { mesh.Clear(); mesh.vertices = verts; mesh.triangles = tris; }
            else mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        // ── 굽기(멈춘 뒤 충돌 상자) ──
        void Bake()
        {
            Settled = true; still = 0;
            // 상자 안이면 상자 자식으로(들고 흔들면 같이)
            PackBox host = null;
            foreach (var b in FindObjectsByType<PackBox>(FindObjectsSortMode.None)) if (b.InsideInner(Center, 0.02f)) { host = b; break; }
            // 뿌리를 가운데로 옮기고(Contents 판정은 뿌리 위치로 한다) 점은 그대로
            Unbake();
            transform.SetParent(null, true);
            transform.SetPositionAndRotation(Center, Quaternion.identity);
            if (host != null) transform.SetParent(host.transform, true);
            for (int i = 0; i < rows - 1; i++)
                for (int j = 0; j < Cols - 1; j++)
                {
                    Vector3 a = pos[Idx(i, j)], b = pos[Idx(i, j + 1)], c = pos[Idx(i + 1, j)], d = pos[Idx(i + 1, j + 1)];
                    Vector3 cen = (a + b + c + d) * 0.25f;
                    Vector3 ax = ((b - a) + (d - c)) * 0.5f, az = ((c - a) + (d - b)) * 0.5f;
                    Vector3 nrm = Vector3.Cross(az, ax);
                    if (nrm.sqrMagnitude < 1e-10f) continue;
                    var go = new GameObject("Cell");
                    go.transform.SetParent(transform, false);
                    go.transform.SetPositionAndRotation(cen, Quaternion.LookRotation(az.normalized, nrm.normalized));
                    var col = go.AddComponent<BoxCollider>();
                    col.size = new Vector3(ax.magnitude + 0.004f, Thick, az.magnitude + 0.004f);
                    col.sharedMaterial = PackBench.SoftMaterial;
                    cells.Add(col);
                }
            UpdateMesh();
        }

        void Unbake()
        {
            foreach (var c in cells) if (c != null) Destroy(c.gameObject);
            cells.Clear();
        }

        /// <summary>근처 물건이 움직였을 때(집어 감) — 다시 떨어지게.</summary>
        public void Wake()
        {
            if (!Settled) return;
            Unbake();
            transform.SetParent(null, true);
            Settled = false; still = 0;
        }

        Vector3[] localPos;
        void LateUpdate()
        {
            // 굽힌 동안 부모(상자)를 따라 점도 같이 옮긴다
            if (!Settled || pos == null) { localPos = null; return; }
            if (localPos == null || localPos.Length != pos.Length)
            {
                localPos = new Vector3[pos.Length];
                for (int k = 0; k < pos.Length; k++) localPos[k] = transform.InverseTransformPoint(pos[k]);
                return;
            }
            for (int k = 0; k < pos.Length; k++) { pos[k] = transform.TransformPoint(localPos[k]); prev[k] = pos[k]; }
        }

        /// <summary>감싸기 — 필요한 길이(물건 한 바퀴)보다 짧으면 false.</summary>
        public static float WrapNeed(PackItem target)
        {
            var bc = target.GetComponent<BoxCollider>();
            if (bc == null) return 0.3f;
            Vector3 s = Vector3.Scale(bc.size, target.transform.lossyScale);
            float a = Mathf.Min(s.x, s.z);
            return 2f * (a + s.y);
        }
    }
}
