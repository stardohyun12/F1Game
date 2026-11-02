using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.PackBench
{
    /// <summary>
    /// 포장 작업대의 골판지 상자 하나(회색 대역). 바닥·벽 4·뚜껑(뒤쪽 경첩)·테이프 띠·라벨 자리.
    /// 원점 = 바깥 바닥 한가운데(작업대 위에 그대로 놓인다). 상자 자체는 키네마틱 리지드바디 — 손(PackBench)이 MovePosition 으로 옮긴다.
    ///
    /// 결과 판정(숫자가 아니라 소리로 듣게 — PACKING-UI §3-3):
    ///  · 빈 부피 비율 = 안쪽을 8×5×8 점으로 찍어 물건이 없는 점의 비율(근사).
    ///  · 달걀판 면 받침 = 판의 6면마다 3×3 광선을 바깥으로 1.2 cm 쏴서 벽·완충재에 닿는 비율(WrapState.QueryFaceAbsorption 과 같은 3×3 평균 방식).
    ///    양쪽 면이 다 받쳐진 축은 상자에 잠근다(ConfigurableJoint) — 구긴 종이가 눌려 빈틈을 메운 것의 근사.
    ///    안 받쳐진 축은 리지드바디 그대로 놀아서 흔들면 벽·뚜껑에 부딪힌다 → 덜그럭.
    ///  · 흔드는 동안 부딪힘 세기 합 = 덜그럭 크기(rattle). 물건이 움직인 양(moved)도 같이 적는다.
    /// </summary>
    public class PackBox : MonoBehaviour
    {
        [Tooltip("안쪽 크기(m). 달걀 한 판 = 0.30×0.065×0.30.")]
        public Vector3 inner = new Vector3(0.315f, 0.15f, 0.315f);
        [Tooltip("보이는 벽 두께(m).")]
        public float wall = 0.012f;
        [Tooltip("충돌 벽 두께(m) — 바깥쪽으로 두껍게 해서 흔들 때 물건이 벽을 뚫지 않게.")]
        public float wallCollider = 0.05f;
        public Material boxMat, tapeMat, labelMat;
        public string sizeName = "M";

        [Header("판정(가설값)")]
        [Tooltip("이 거리(m) 안에 벽·완충재가 있으면 그 점은 받쳐졌다.")]
        public float supportTol = 0.012f;
        [Tooltip("면의 3×3 점 중 이 비율 이상 받쳐지면 그 면은 받쳐졌다.")]
        public float faceSupportedFrac = 0.5f;
        [Tooltip("이 상대 속도(m/s) 미만 부딪힘은 소리 없음.")]
        public float hitMinSpeed = 0.08f;
        [Tooltip("이 상대 속도(m/s)에서 소리 크기 1(무게 1 기준).")]
        public float hitFullSpeed = 0.7f;

        /// <summary>선반 자리(빌더가 넣는다). 작업대에서 내려놓을 때 돌아갈 곳.</summary>
        public Transform shelfSlot;
        [Tooltip("선반에서는 접힌(닫힌) 채로 — 작업대에 꺼내면 연다.")]
        public bool startClosed = true;

        public bool IsClosed { get; private set; }
        public float TapeCover { get; private set; }
        public bool Taped => TapeCover >= 0.8f;
        public bool Labeled => labelGo != null;
        public Rigidbody Body { get; private set; }
        /// <summary>뚜껑을 다시 연 횟수 — 흔들어 본 뒤 다시 열었으면 그 결과는 무효(보관소 작업대, D-251).</summary>
        public int OpenCount { get; private set; }

        Transform lidPivot; Collider lidCol; Transform tapeStrip; GameObject labelGo;
        float lidT = 1f, lidFrom, lidTo; // 0 = 닫힘, 1 = 열림
        float tapeMin = float.MaxValue, tapeMax = float.MinValue;
        static readonly Quaternion LidOpenRot = Quaternion.Euler(112f, 0f, 0f);

        public float InnerBottomY => wall;
        public float RimY => wall + inner.y;
        public Vector3 InnerCenterLocal => new Vector3(0f, wall + inner.y * 0.5f, 0f);
        public Vector3 InnerCenterWorld => transform.TransformPoint(InnerCenterLocal);

        // ── 만들기 ──
        void Awake()
        {
            Build();
            Body = GetComponent<Rigidbody>();
            if (Body == null) Body = gameObject.AddComponent<Rigidbody>();
            Body.isKinematic = true;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            IsClosed = startClosed;
            SetLidImmediate(IsClosed ? 0f : 1f);
        }

        /// <summary>회색 대역 모양을 만든다(이미 있으면 그대로). 빌더가 에디트 모드에서도 부른다.</summary>
        public void Build()
        {
            if (transform.Find("Shell") != null)
            {
                lidPivot = transform.Find("Shell/LidPivot");
                lidCol = lidPivot != null ? lidPivot.GetComponentInChildren<Collider>() : null;
                tapeStrip = transform.Find("Shell/Tape");
                return;
            }
            var shell = new GameObject("Shell").transform;
            shell.SetParent(transform, false);
            float w = wall, ix = inner.x, iy = inner.y, iz = inner.z;
            Part("Floor", shell, new Vector3(0f, w * 0.5f, 0f), new Vector3(ix + 2 * w, w, iz + 2 * w), 1, -1, boxMat);
            Part("Wall_L", shell, new Vector3(-(ix + w) * 0.5f, w + iy * 0.5f, 0f), new Vector3(w, iy, iz + 2 * w), 0, -1, boxMat);
            Part("Wall_R", shell, new Vector3((ix + w) * 0.5f, w + iy * 0.5f, 0f), new Vector3(w, iy, iz + 2 * w), 0, +1, boxMat);
            Part("Wall_F", shell, new Vector3(0f, w + iy * 0.5f, -(iz + w) * 0.5f), new Vector3(ix, iy, w), 2, -1, boxMat);
            Part("Wall_B", shell, new Vector3(0f, w + iy * 0.5f, (iz + w) * 0.5f), new Vector3(ix, iy, w), 2, +1, boxMat);

            lidPivot = new GameObject("LidPivot").transform;
            lidPivot.SetParent(shell, false);
            lidPivot.localPosition = new Vector3(0f, w + iy, (iz + 2 * w) * 0.5f);
            var lid = Part("Lid", lidPivot, new Vector3(0f, w * 0.5f, -(iz + 2 * w) * 0.5f), new Vector3(ix + 2 * w, w, iz + 2 * w), 1, +1, boxMat);
            lidCol = lid.GetComponent<Collider>();

            var tape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tape.name = "Tape";
            Object.DestroyImmediate(tape.GetComponent<Collider>());
            tape.transform.SetParent(shell, false);
            tape.transform.localPosition = new Vector3(0f, w * 2f + iy + 0.001f, -(iz + 2 * w) * 0.5f + 0.0005f);
            tape.transform.localScale = new Vector3(0.001f, 0.002f, 0.05f);
            if (tapeMat != null) tape.GetComponent<Renderer>().sharedMaterial = tapeMat;
            tape.SetActive(false);
            tapeStrip = tape.transform;
        }

        /// <summary>보이는 판 하나 + 바깥쪽(axis, sign)으로 두꺼운 충돌 상자.</summary>
        Transform Part(string name, Transform parent, Vector3 pos, Vector3 size, int axis, int sign, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = size;
            if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
            var bc = g.GetComponent<BoxCollider>();
            float thick = size[axis];
            float k = Mathf.Max(1f, wallCollider / Mathf.Max(1e-4f, thick));
            var s = Vector3.one; s[axis] = k;
            var c = Vector3.zero; c[axis] = sign * (k - 1f) * 0.5f;
            bc.size = s; bc.center = c;
            return g.transform;
        }

        // ── 뚜껑 ──
        void SetLidImmediate(float t)
        {
            lidT = lidFrom = lidTo = t;
            if (lidPivot != null) lidPivot.localRotation = Quaternion.Slerp(Quaternion.identity, LidOpenRot, t);
            if (lidCol != null) lidCol.enabled = t < 0.5f;
        }

        void Update()
        {
            if (Mathf.Approximately(lidT, lidTo)) return;
            lidT = Mathf.MoveTowards(lidT, lidTo, Time.deltaTime / 0.35f);
            if (lidPivot != null) lidPivot.localRotation = Quaternion.Slerp(Quaternion.identity, LidOpenRot, Mathf.SmoothStep(0f, 1f, lidT));
            if (lidCol != null) lidCol.enabled = lidT < 0.5f;
        }

        /// <summary>뚜껑 닫기. 테두리 위로 1 cm 넘게 솟은 물건이 있으면 못 닫는다(false).</summary>
        public bool TryClose()
        {
            if (IsClosed) return true;
            foreach (var it in Contents())
            {
                var b = WorldBounds(it);
                float topLocal = transform.InverseTransformPoint(b.center + Vector3.up * b.extents.y).y;
                if (topLocal > RimY + 0.01f) return false;
            }
            IsClosed = true; lidFrom = lidT; lidTo = 0f;
            return true;
        }

        /// <summary>뚜껑 열기 — 테이프는 뜯겨 없어진다(벌 없음, 다시 붙이면 된다). 라벨은 남는다.</summary>
        public void Open()
        {
            if (!IsClosed) return;
            IsClosed = false; lidFrom = lidT; lidTo = 1f;
            OpenCount++;
            ClearTape();
        }

        public void SnapLid() => SetLidImmediate(IsClosed ? 0f : 1f);

        // ── 테이프 ──
        /// <summary>뚜껑 위 이음선(앞뒤 가운데 줄, 상자 X 방향)에 world 점을 투영한 값(0~1). 뚜껑 위가 아니면 false.</summary>
        public bool SeamParam(Vector3 world, out float t)
        {
            var p = transform.InverseTransformPoint(world);
            float half = inner.x * 0.5f + wall;
            t = Mathf.InverseLerp(-half, half, p.x);
            return IsClosed && p.y > RimY - 0.01f && Mathf.Abs(p.x) <= half + 0.03f && Mathf.Abs(p.z) <= inner.z * 0.5f + wall + 0.03f;
        }

        /// <summary>테이프를 끌어서 덮은 구간을 넓힌다.</summary>
        public void AddTape(float t)
        {
            tapeMin = Mathf.Min(tapeMin, t); tapeMax = Mathf.Max(tapeMax, t);
            TapeCover = Mathf.Clamp01(tapeMax - tapeMin);
            if (tapeStrip == null) return;
            float full = inner.x + 2 * wall + 0.06f; // 양쪽으로 3 cm 씩 넘어가게(옆면으로 접히는 부분 근사)
            float a = Mathf.Lerp(-full * 0.5f, full * 0.5f, tapeMin), b = Mathf.Lerp(-full * 0.5f, full * 0.5f, tapeMax);
            tapeStrip.gameObject.SetActive(true);
            var lp = tapeStrip.localPosition;
            tapeStrip.localPosition = new Vector3((a + b) * 0.5f, lp.y, lp.z);
            tapeStrip.localScale = new Vector3(Mathf.Max(0.005f, b - a), 0.002f, 0.05f);
        }

        void ClearTape()
        {
            tapeMin = float.MaxValue; tapeMax = float.MinValue; TapeCover = 0f;
            if (tapeStrip != null) tapeStrip.gameObject.SetActive(false);
        }

        // ── 라벨 ──
        /// <summary>라벨을 상자 겉면 world 점에 붙인다(면에 납작하게). 글자는 없음 — 받는 사람 이름은 도현이 정할 때까지 빈 칸(체크리스트 7).</summary>
        public void StickLabel(Vector3 world, Vector3 normal)
        {
            if (labelGo != null) Destroy(labelGo);
            labelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            labelGo.name = "Label";
            Destroy(labelGo.GetComponent<Collider>());
            labelGo.transform.SetParent(transform, true);
            labelGo.transform.position = world + normal * 0.0015f;
            labelGo.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, normal).sqrMagnitude > 1e-4f ? Vector3.ProjectOnPlane(transform.forward, normal) : transform.right, normal);
            labelGo.transform.localScale = new Vector3(0.10f / transform.lossyScale.x, 0.002f, 0.07f / transform.lossyScale.z);
            if (labelMat != null) labelGo.GetComponent<Renderer>().sharedMaterial = labelMat;
            // 손글씨 자리 줄 3개(글자 대신 회색 선)
            for (int i = 0; i < 3; i++)
            {
                var ln = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(ln.GetComponent<Collider>());
                ln.transform.SetParent(labelGo.transform, false);
                ln.transform.localPosition = new Vector3(-0.08f * i, 0.6f, 0.25f - i * 0.22f);
                ln.transform.localScale = new Vector3(0.75f - i * 0.15f, 0.2f, 0.06f);
                if (tapeMat != null) ln.GetComponent<Renderer>().sharedMaterial = tapeMat;
            }
        }

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

        // ── 흔들기 ──
        public struct ShakeResult
        {
            public string box; public float seconds, rattle, peakHit, trayMoved, eggShock, gap, bottomCushion, bareFloor;
            public int hits, escaped; public bool upright, closed, taped, labeled;
            public string locks, faces;
            // 소리 나눠 보기: 달걀판이 낀 부딪힘 / 완충재끼리·완충재-벽 · 눌려 잠긴 완충재 수
            public float rattleTray, rattleSoft; public int hitsTray, hitsSoft, softPinned, softTotal;
            public string ToJson() =>
                $"{{\"box\":\"{box}\",\"sec\":{seconds:F2},\"rattle\":{rattle:F3},\"peakHit\":{peakHit:F3},\"hits\":{hits},\"trayMovedM\":{trayMoved:F4},\"eggShock\":{eggShock:F2}," +
                $"\"rattleTray\":{rattleTray:F3},\"hitsTray\":{hitsTray},\"rattleSoft\":{rattleSoft:F3},\"hitsSoft\":{hitsSoft},\"softPinned\":\"{softPinned}/{softTotal}\"," +
                $"\"gap\":{gap:F3},\"bottomCushion\":{bottomCushion:F2},\"bareFloor\":{bareFloor:F2},\"upright\":{(upright ? "true" : "false")},\"escaped\":{escaped}," +
                $"\"closed\":{(closed ? "true" : "false")},\"taped\":{(taped ? "true" : "false")},\"labeled\":{(labeled ? "true" : "false")},\"locks\":\"{locks}\",\"faces\":\"{faces}\"}}";
        }

        ShakeResult cur;
        PackItem tray;
        ConfigurableJoint trayJoint;
        Vector3 trayPrevLocal, prevVel, prevPos;
        float shakeT0;
        bool shaking;
        public bool IsShaking => shaking;

        /// <summary>상자를 들어 올린 순간 — 내용물 정리·판정·달걀판 축 잠금.</summary>
        public void BeginShake()
        {
            shakeSet.Clear(); shakeSet.AddRange(Contents());
            var dump = new System.Text.StringBuilder();
            foreach (var it in shakeSet) { var b = WorldBounds(it); dump.Append(it.kind).Append(" y=").Append(transform.InverseTransformPoint(b.min).y.ToString("F3")).Append("~").Append(transform.InverseTransformPoint(b.max).y.ToString("F3")).Append("; "); }
            LastBeginDump = dump.ToString();
            cur = new ShakeResult { box = sizeName, closed = IsClosed, taped = Taped, labeled = Labeled, locks = "", faces = "" };
            MeasureFill(out cur.gap, out cur.bottomCushion);
            tray = shakeSet.Find(i => i.kind == PackItemKind.Eggs);
            if (tray != null)
            {
                cur.upright = Vector3.Dot(tray.transform.up, transform.up) > 0.9f;
                var fr = MeasureSupport(tray, out cur.bareFloor);
                var sb = new System.Text.StringBuilder();
                for (int f = 0; f < 6; f++) sb.Append(FaceNames[f]).Append('=').Append(fr[f].ToString("F2")).Append(f < 5 ? " " : "");
                cur.faces = sb.ToString();
                // 뒤집혔으면(방향 틀림) 판 안의 달걀이 논다 → 잠그지 않는다
                bool lx = cur.upright && fr[0] >= faceSupportedFrac && fr[1] >= faceSupportedFrac;
                bool ly = cur.upright && fr[2] >= faceSupportedFrac && fr[3] >= faceSupportedFrac;
                bool lz = cur.upright && fr[4] >= faceSupportedFrac && fr[5] >= faceSupportedFrac;
                cur.locks = (lx ? "X" : "") + (ly ? "Y" : "") + (lz ? "Z" : "");
                if (lx || ly || lz)
                {
                    trayJoint = tray.gameObject.AddComponent<ConfigurableJoint>();
                    trayJoint.connectedBody = Body;
                    trayJoint.autoConfigureConnectedAnchor = true;
                    trayJoint.anchor = Vector3.zero;
                    trayJoint.axis = Vector3.right; trayJoint.secondaryAxis = Vector3.up;
                    trayJoint.xMotion = lx ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
                    trayJoint.yMotion = ly ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
                    trayJoint.zMotion = lz ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
                    // 두 축 이상 벽에 끼어 있으면 판이 상자 안에서 돌 수 없다(옆 틈 7.5 mm) → 회전도 잠근다. 위아래로는 뛸 수 있다.
                    int nl = (lx ? 1 : 0) + (ly ? 1 : 0) + (lz ? 1 : 0);
                    var am = nl >= 2 ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
                    trayJoint.angularXMotion = trayJoint.angularYMotion = trayJoint.angularZMotion = am;
                    trayLocked = nl == 3;
                    // 다 잠겼으면 상자의 일부 — 벽과 계속 비비면 관절과 싸워 가짜 덜그럭이 난다(앞 세션 full hits 1625 의 일부)
                    trayJoint.enableCollision = !trayLocked;
                }
                trayPrevLocal = transform.InverseTransformPoint(tray.Body.position);
            }
            // 완충재: 위·아래가 둘 다 막혀 있으면(뚜껑·판·바닥 사이에 눌림) 상자에 잠근다 — 구긴 종이가 눌려 제자리에 박힌 것의 근사.
            // 안 눌린 것(위가 비어 굴러다니는 공)은 그대로 놀아서 사각사각 작은 소리를 낸다.
            foreach (var it in shakeSet)
            {
                if (it.kind == PackItemKind.Eggs) continue;
                cur.softTotal++;
                // 뽁뽁이 시트(2판): 상자 안에서 내려앉으면 이미 상자 몸의 일부(굽힌 칸 콜라이더)
                if (it.sheet != null) { if (it.transform.IsChildOf(transform)) cur.softPinned++; continue; }
                if (!IsPinned(it)) continue;
                var j = it.gameObject.AddComponent<FixedJoint>();
                j.connectedBody = Body;
                softJoints.Add(j); pinned.Add(it);
                cur.softPinned++;
            }
            foreach (var it in shakeSet) { if (it.Body != null) it.Body.WakeUp(); }
            pairs.Clear();
            prevPos = Body.position; prevVel = Vector3.zero;
            shakeT0 = Time.time; shaking = true;
        }

        void FixedUpdate()
        {
            if (!shaking) return;
            float dt = Time.fixedDeltaTime;
            Vector3 vel = (Body.position - prevPos) / dt;
            Vector3 acc = (vel - prevVel) / dt;
            prevPos = Body.position; prevVel = vel;
            if (tray != null)
            {
                var lp = transform.InverseTransformPoint(tray.Body.position);
                cur.trayMoved += (lp - trayPrevLocal).magnitude;
                trayPrevLocal = lp;
                // 달걀 충격: 판이 상자 바닥에 맨살로 닿아 있으면 상자의 흔들림이 그대로 들어간다. 기준 = CargoTypePresets 달걀 longWarnG.
                float thr = CargoTypePresets.Get(CargoType.Eggs).longWarnG * 9.81f;
                float over = Mathf.Max(0f, acc.magnitude - thr);
                float trans = Mathf.Lerp(0.2f, 1f, cur.bareFloor) * (cur.upright ? 1f : 3f);
                cur.eggShock += over * dt * trans;
            }
        }

        /// <summary>내려놓은 순간 — 결과를 닫는다.</summary>
        public ShakeResult EndShake()
        {
            if (!shaking) return cur;
            shaking = false;
            cur.seconds = Time.time - shakeT0;
            foreach (var it in shakeSet) if (it != null && !InsideInner(it.transform.position, 0.01f)) cur.escaped++;
            if (trayJoint != null) { Destroy(trayJoint); trayJoint = null; }
            trayLocked = false;
            foreach (var j in softJoints) if (j != null) Destroy(j);
            softJoints.Clear(); pinned.Clear();
            shakeSet.Clear();
            return cur;
        }

        bool trayLocked;
        /// <summary>QA: 들어 올린 순간 내용물 높이(상자 좌표, 바닥~꼭대기).</summary>
        public static string LastBeginDump = "";
        readonly List<FixedJoint> softJoints = new List<FixedJoint>();
        readonly List<PackItem> pinned = new List<PackItem>();

        bool IsLocked(PackItem it) => it != null && (it.kind == PackItemKind.Eggs ? trayLocked : pinned.Contains(it) || (it.sheet != null && it.transform.IsChildOf(transform)));

        /// <summary>완충재 하나가 위·아래(상자 축) 둘 다 supportTol 안에서 막혀 있나.</summary>
        bool IsPinned(PackItem it)
        {
            var col = it.GetComponent<Collider>();
            if (col == null) return false;
            Vector3 c = col.bounds.center, up = transform.up;
            float h = Mathf.Abs(Vector3.Dot(col.bounds.extents, new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z))));
            return Blocked(it, c, up, h) && Blocked(it, c, -up, h);
        }

        bool Blocked(PackItem self, Vector3 c, Vector3 d, float h)
        {
            // 자기 콜라이더 안에서 쏘면 자기는 안 맞는다 — 밖으로 h + supportTol
            foreach (var hit in Physics.RaycastAll(c, d, h + supportTol, ~0, QueryTriggerInteraction.Ignore))
            {
                var o = hit.collider.GetComponentInParent<PackItem>();
                if (o == self) continue;
                if (o != null || hit.collider.GetComponentInParent<PackBox>() == this) return true;
            }
            return false;
        }

        /// <summary>QA: 흔드는 동안 부딪힌 짝별 (횟수, 소리 합) — "Eggs-Lid" 꼴.</summary>
        readonly Dictionary<string, Vector2> pairs = new Dictionary<string, Vector2>();
        public static Collider lastHitCollider;
        public string PairsJson()
        {
            var sb = new System.Text.StringBuilder("{");
            foreach (var kv in pairs) sb.Append('"').Append(kv.Key).Append("\":[").Append(kv.Value.x.ToString("F0")).Append(',').Append(kv.Value.y.ToString("F2")).Append("],");
            if (sb.Length > 1) sb.Length--;
            return sb.Append('}').ToString();
        }

        public void ReportHit(PackItem it, PackItem other, float relSpeed)
        {
            if (!shaking || relSpeed < hitMinSpeed) return;
            // 잠긴 것끼리·잠긴 것과 상자 = 한 덩어리 → 소리 없음
            if (IsLocked(it) && (other == null || IsLocked(other))) return;
            // 무른 것(신문지·뽁뽁이)에 닿으면 소리는 무른 쪽 크기 — 판이 뽁뽁이 위로 떨어지면 '퍽'이지 '딱'이 아니다(1153: 뽁뽁이만 깐 상자가 맨 상자보다 시끄럽던 원인)
            float w = it.SoundWeight;
            if (other != null) w = Mathf.Min(w, other.SoundWeight);
            float vol = w * Mathf.Clamp01((relSpeed - hitMinSpeed) / (hitFullSpeed - hitMinSpeed));
            if (vol <= 0.005f) return;
            cur.rattle += vol; cur.hits++; cur.peakHit = Mathf.Max(cur.peakHit, vol);
            string key = it.kind + "-" + (other != null ? other.kind.ToString() : (lastHitCollider != null ? lastHitCollider.name : "?"));
            pairs.TryGetValue(key, out var pv); pairs[key] = new Vector2(pv.x + 1f, pv.y + vol);
            bool trayHit = it.kind == PackItemKind.Eggs || (other != null && other.kind == PackItemKind.Eggs);
            if (trayHit) { cur.rattleTray += vol; cur.hitsTray++; } else { cur.rattleSoft += vol; cur.hitsSoft++; }
            PackBench.PlayRattle(other != null && other.SoundWeight < it.SoundWeight ? other : it, vol); // 소리 종류도 무른 쪽
        }
    }
}
