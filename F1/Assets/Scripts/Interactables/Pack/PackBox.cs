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
    public partial class PackBox : MonoBehaviour
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

        /// <summary>다음 날 아침(과제 20261102-1951): 선반에 막 놓인 새 상자처럼 — 뚜껑 처음 상태·테이프·라벨 없음·연 횟수 0. 위치는 부르는 쪽(DepotPackSpot)이 되돌린다.</summary>
        public void ResetForNextDay()
        {
            ClearTape();
            if (labelGo != null) { Destroy(labelGo); labelGo = null; }
            OpenCount = 0;
            IsClosed = startClosed;
            SetLidImmediate(IsClosed ? 0f : 1f);
        }

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

    }
}
