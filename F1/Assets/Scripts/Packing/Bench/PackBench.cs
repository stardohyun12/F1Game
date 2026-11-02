using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.PackBench
{
    /// <summary>
    /// 포장 작업대 시제품(D-250 ②, 과제 20261102-1108) — 1인칭으로 서서 내려다보며 손으로 싼다. 씬 Proto_PackBench 전용.
    /// 순서(PACKING-UI §3-2, 강제하지 않음 — 단계 글자 없음): 선반에서 상자 고르기(E) → 바닥에 뽁뽁이·구긴 신문지 → 달걀 한 판 넣기(방향)
    ///   → 빈틈 채우기 → 뚜껑 닫기(E) → 테이프 끌어 붙이기 → 라벨 → 상자를 들어(클릭 유지) 마우스로 흔들어 본다 → 덜그럭 소리.
    /// 조작(2판, D-253): 시선 = 마우스(조준점 = 커서 하나) · 집기 = 왼쪽 클릭 유지, 놓기 = 떼기
    ///   · 휠 = 든 물건을 앞으로 멀리 보내기/당겨 오기(잡은 거리) · 돌리기 = 오른쪽 버튼을 누른 채 마우스(Shift = 15° 씩) · R = 옆으로 90°
    ///   · 든 물건은 물리로 끌려온다(속도 추종 + 힘 상한) — 벽·다른 물건에 막히면 멈추고, 너무 멀리 끌면 손에서 빠진다.
    ///   · 뽁뽁이 = 롤을 클릭한 채 끌면 끈 만큼 풀려 나오고 떼면 끊긴다 → 시트를 집어 깔기·덮기, 물건 위에 얹고 E = 감싸기.
    ///   · 신문지 묶음 = 꾹 누르는 동안 구겨진다(오래 누를수록 작고 단단하게) · 테이프 = 롤 클릭으로 들고, 뚜껑 위 이음선을 끌어서 · 라벨 = 들고 상자에 클릭.
    ///   · E = 상자 고르기 / 뚜껑 닫기·열기(열면 테이프는 뜯김, 벌 없음).
    /// 화면 UI 는 걷기와 같은 문법(조준점 2×2 + [E] 칩)만 — PackBenchCursor.
    /// 타이머 없음(D-238). 걸린 시간은 로그로만 잰다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public partial class PackBench : MonoBehaviour
    {
        public static PackBench I { get; private set; }

        [Header("장면(빌더가 채운다)")]
        public Camera cam;
        public Transform benchSpot;
        public Material paperMat, bubbleMat;

        [Header("시선")]
        [SerializeField] float mouseSensitivity = 0.12f;
        [SerializeField] float yawLimit = 65f, minPitch = -10f, maxPitch = 80f;
        [SerializeField] float startPitch = 48f;
        [SerializeField] float reach = 1.8f;

        [Header("손(가설값)")]
        [Tooltip("든 물건을 조준점 위 이만큼 띄워 든다(m).")]
        [SerializeField] float holdLift = 0.01f;
        [SerializeField] float holdMaxSpeed = 2.5f;
        [Tooltip("목표점까지 거리 1 m 당 따라오는 속도(1/s).")]
        [SerializeField] float holdGain = 14f;
        [Tooltip("손 힘 상한(N) — 무거운 판일수록 느리게 가속된다. 달걀판 2 kg → 최대 20 m/s².")]
        [SerializeField] float holdMaxForce = 40f;
        [SerializeField] float holdMaxAccel = 40f;
        [Tooltip("목표점과 이만큼(m) 넘게 0.3초 벌어지면 손에서 빠진다(벽에 막혀 끌 때).")]
        [SerializeField] float holdBreakDist = 0.22f;
        [Tooltip("휠 한 칸에 잡은 거리 변화(m).")]
        [SerializeField] float wheelStep = 0.03f;
        [Tooltip("오른쪽 버튼 돌리기: 마우스 1 픽셀당 도.")]
        [SerializeField] float rotateDegPerPixel = 0.35f;
        [Tooltip("든 상자가 따라오는 최고 속도(m/s) — 마우스를 휙 돌리면 이 속도로 흔들린다.")]
        [SerializeField] float boxMaxSpeed = 3.5f;
        [Tooltip("신문지: 이 시간(초) 동안 꾹 누르면 가장 작게(단단하게) 구겨진다.")]
        [SerializeField] float crumpleFullSec = 1.2f;
        [SerializeField] float paperRadiusLoose = 0.055f, paperRadiusTight = 0.024f;

        public PackBox Shaking => lifted != null && lifted.IsShaking ? lifted : null;
        public PackBox BenchBox => benchBox;
        /// <summary>손에 뭘 들었나(물건·도구·든 상자) — 커서 모양만 바뀐다.</summary>
        public bool HandBusy => held != null || tool != Tool.None || lifted != null || heldSheet != null || pullSheet != null;
        /// <summary>QA: 1판처럼 든 물건을 키네마틱으로 바로 옮긴다(벽 통과 비교용).</summary>
        public static bool LegacyHold;
        /// <summary>QA: 손 목표점을 직접 준다(마우스 대신).</summary>
        public Vector3? SimTarget;
        public PackItem Held => held;
        public BubbleSheet HeldSheet => heldSheet;
        public float HoldDist => holdDist;
        public string LookName { get; private set; } = "";
        /// <summary>[E] 칩 — 비면 안 보인다. "E 닫기" 꼴(걷기 OnFootDirector.Prompt 와 같은 문법).</summary>
        public string Prompt { get; private set; } = "";
        public Vector3? PromptAnchor { get; private set; }
        public static string LastShakeJson { get; private set; } = "";
        public static string LastPairs = "";
        public static readonly List<string> ShakeLog = new List<string>();
        public static float LastPackSeconds { get; private set; } = -1f;

        float yaw0, yaw, pitch;
        enum Tool { None, Tape, Label }
        Tool tool;
        PackSupply toolSupply;
        GameObject toolVis;
        PackItem held; Quaternion heldRot; float heldHalfH;
        float holdDist = 0.8f, breakT; Vector2 rotAcc;
        BubbleSheet heldSheet, pullSheet; Vector3 sheetCross = Vector3.right, rollExit, rollCross; float pullTick;
        bool rotating;
        bool crumpling; float crumpleT0;
        bool taping;
        PackBox benchBox, lifted, moving;
        readonly List<PackBox> boxes = new List<PackBox>();
        Pose movingTo;
        bool liftedReturning;
        float firstTouch = -1f; bool timed;
        RaycastHit lookHit; bool hasHit;
        // 봇·QA 입력(마우스 대신)
        bool simHold; float simShakeUntil = -1f, simShakeT0;
        public bool SimActive { get; set; }

        void Awake()
        {
            I = this;
            Application.runInBackground = true;
            if (cam == null) cam = Camera.main;
            boxes.AddRange(FindObjectsByType<PackBox>(FindObjectsSortMode.None));
            EnsureSfx();
        }

        void OnDestroy() { if (I == this) I = null; }

        void OnEnable()
        {
            yaw0 = yaw = transform.eulerAngles.y;
            pitch = startPitch;
            Lock(true);
        }

        void OnDisable() { Lock(false); }

        static void Lock(bool on) { Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !on; }

        // ── 입력 ──
        void Update()
        {
            var m = Mouse.current; var k = Keyboard.current;
            bool ui = OnFootCamera.UiCapture;
            rotating = !SimActive && !ui && m != null && Cursor.lockState == CursorLockMode.Locked && m.rightButton.isPressed && (held != null || heldSheet != null);
            if (!SimActive && !ui && m != null)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var raw = m.delta.ReadValue();
                    // 오른쪽 버튼을 누르고 있는 동안은 시선 대신 든 물건을 돌린다
                    if (rotating) RotateHeld(raw * rotateDegPerPixel, k != null && k.shiftKey.isPressed);
                    else { var d = raw * mouseSensitivity; yaw += d.x; pitch -= d.y; }
                }
                if (m.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) { Lock(true); return; }
            }
            if (k != null && k.escapeKey.wasPressedThisFrame) Lock(false);
            yaw = Mathf.Clamp(yaw, yaw0 - yawLimit, yaw0 + yawLimit);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            Look();
            if (ui) return;

            bool locked = Cursor.lockState == CursorLockMode.Locked || SimActive;
            bool down = SimActive ? simHold && !prevSimHold : locked && m != null && m.leftButton.wasPressedThisFrame;
            bool hold = SimActive ? simHold : locked && m != null && m.leftButton.isPressed;
            bool up = SimActive ? !simHold && prevSimHold : m != null && m.leftButton.wasReleasedThisFrame;
            prevSimHold = simHold;
            bool eKey = !SimActive && locked && k != null && k.eKey.wasPressedThisFrame;
            float wheel = !SimActive && m != null ? m.scroll.ReadValue().y : 0f;
            bool rKey = !SimActive && k != null && k.rKey.wasPressedThisFrame;

            if (eKey) PressE();
            if (down) PressLeft();
            if (hold) HoldLeft();
            if (up) ReleaseLeft();
            if (held != null || heldSheet != null || pullSheet != null)
            {
                // 휠 = 잡은 거리(앞으로 보내기 +, 당겨 오기 −). 노치가 ±120 으로 오는 장치도 있어 맞춘다.
                if (Mathf.Abs(wheel) > 0.01f) SimWheel(Mathf.Abs(wheel) > 20f ? wheel / 120f : wheel);
                if (rKey && held != null) RotateSide();
            }
            UpdatePrompt();
        }
        bool prevSimHold;

        /// <summary>휠 notches 칸만큼 잡은 거리를 바꾼다(봇·QA 도 이걸 부른다).</summary>
        public void SimWheel(float notches) => holdDist = Mathf.Clamp(holdDist + notches * wheelStep, 0.25f, reach);

        /// <summary>오른쪽 버튼 + 마우스: 가로 = 세로축으로 돌리기, 세로 = 화면 가로축으로 굴리기. snap 이면 15° 씩 끊어서.</summary>
        void RotateHeld(Vector2 deg, bool snap)
        {
            Vector2 apply;
            if (snap)
            {
                rotAcc += deg;
                apply = new Vector2(Mathf.Round(rotAcc.x / 15f) * 15f, Mathf.Round(rotAcc.y / 15f) * 15f);
                rotAcc -= apply;
            }
            else { apply = deg; rotAcc = Vector2.zero; }
            if (apply == Vector2.zero) return;
            Vector3 r = cam.transform.right; r.y = 0f; r = r.sqrMagnitude > 1e-4f ? r.normalized : Vector3.right;
            if (held != null) heldRot = Quaternion.AngleAxis(apply.x, Vector3.up) * Quaternion.AngleAxis(-apply.y, r) * heldRot;
            if (heldSheet != null) sheetCross = Quaternion.AngleAxis(apply.x, Vector3.up) * sheetCross;
        }

        void RotateSide()
        {
            Vector3 r = cam.transform.right; r.y = 0f;
            if (r.sqrMagnitude < 1e-4f) r = Vector3.right;
            heldRot = Quaternion.AngleAxis(90f, r.normalized) * heldRot;
        }

        void Look()
        {
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            hasHit = Physics.Raycast(ray, out lookHit, reach, ~(1 << 2), QueryTriggerInteraction.Ignore);
            LookName = hasHit ? lookHit.collider.name : "";
        }

        T LookAt<T>() where T : Component => hasHit ? lookHit.collider.GetComponentInParent<T>() : null;

        /// <summary>보고 있는 상자 — 상자 몸 또는 작업대 상자 안의 물건을 보고 있으면 그 상자.</summary>
        PackBox LookBox()
        {
            var b = LookAt<PackBox>();
            if (b != null) return b;
            var it = LookAt<PackItem>();
            if (it != null && benchBox != null && benchBox.InsideInner(it.transform.position, 0.01f)) return benchBox;
            return null;
        }

        void Touch() { if (firstTouch < 0f) firstTouch = Time.time; }

        void PressE()
        {
            if (TryWrapLook()) return;
            if (held != null || lifted != null || moving != null || heldSheet != null) return;
            var b = LookBox();
            if (b == null) return;
            Touch();
            if (b == benchBox)
            {
                if (!b.IsClosed) { if (b.TryClose()) Sfx(sfxClose, 0.7f, 0.9f); else Sfx(sfxDeny, 0.6f, 0.7f); }
                else { b.Open(); Sfx(sfxScratch[0], 0.6f, 0.8f); }
                return;
            }
            // 선반 상자 고르기 — 작업대 상자가 비어 있으면 선반으로 돌려놓고 바꾼다
            if (benchBox != null)
            {
                if (benchBox.Contents().Count > 0) { Sfx(sfxDeny, 0.6f, 0.7f); return; }
                var old = benchBox;
                old.Open(); old.TryClose(); old.SnapLid();
                if (old.shelfSlot != null) old.transform.SetPositionAndRotation(old.shelfSlot.position, old.shelfSlot.rotation);
            }
            benchBox = b;
            b.Open();
            MoveBox(b, new Pose(benchSpot.position, benchSpot.rotation));
            Sfx(sfxPick, 0.6f, 1f);
        }

        void PressLeft()
        {
            if (lifted != null || moving != null) return;
            if (tool == Tool.Tape)
            {
                var s = LookAt<PackSupply>();
                if (s != null && s.kind == PackSupplyKind.Tape) { PutToolBack(); return; }
                var b = LookBox();
                if (b != null && b == benchBox && b.SeamParam(lookHit.point, out float t)) { taping = true; b.AddTape(t); Sfx(sfxScratch[1], 0.5f, 1.3f); }
                return;
            }
            if (tool == Tool.Label)
            {
                var s = LookAt<PackSupply>();
                if (s != null && s.kind == PackSupplyKind.Label) { PutToolBack(); return; }
                var b = LookAt<PackBox>();
                if (b != null && b == benchBox) { b.StickLabel(lookHit.point, lookHit.normal); Sfx(sfxPick, 0.6f, 1.2f); PutToolBack(); }
                return;
            }
            var item = LookAt<PackItem>();
            if (item != null && item.sheet != null) { Touch(); GrabSheet(item.sheet, lookHit.point); return; }
            if (item != null) { Touch(); Grab(item); return; }
            var sup = LookAt<PackSupply>();
            if (sup != null)
            {
                Touch();
                switch (sup.kind)
                {
                    case PackSupplyKind.Newspaper: StartCrumple(); break;
                    case PackSupplyKind.Bubble: StartPull(sup); break;
                    case PackSupplyKind.Tape: TakeTool(Tool.Tape, sup); break;
                    case PackSupplyKind.Label: TakeTool(Tool.Label, sup); break;
                }
                return;
            }
            var box = LookAt<PackBox>();
            if (box != null && box == benchBox) { Touch(); Lift(box); }
        }

        void HoldLeft()
        {
            if (pullSheet != null)
            {
                Vector3 hand = HoldTargetFree();
                float before = pullSheet.Length;
                pullSheet.Pull(rollExit, hand, rollCross);
                // 풀릴 때 롤이 도는 소리(길이 4 cm 마다 작게)
                pullTick += Mathf.Max(0f, pullSheet.Length - before);
                if (pullTick > 0.04f) { pullTick = 0f; Sfx(sfxScratch[Random.Range(0, sfxScratch.Length)], 0.18f, Random.Range(0.7f, 0.85f)); }
            }
            if (crumpling && held != null)
            {
                float k = Mathf.Clamp01((Time.time - crumpleT0) / crumpleFullSec);
                float r = Mathf.Lerp(paperRadiusLoose, paperRadiusTight, k);
                // 펴진 한 장(납작) → 공으로
                float flat = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(k * 3f));
                held.transform.localScale = new Vector3(2 * r / flat, 2 * r * flat, 2 * r / flat);
                heldHalfH = r;
                if (Random.value < Time.deltaTime * 12f) Sfx(sfxScratch[Random.Range(0, sfxScratch.Length)], 0.35f, Random.Range(1.1f, 1.4f));
            }
            if (taping && benchBox != null && hasHit && LookBox() == benchBox && benchBox.SeamParam(lookHit.point, out float t))
            {
                bool was = benchBox.Taped;
                benchBox.AddTape(t);
                if (!was && benchBox.Taped) Sfx(sfxDone, 0.6f, 1f);
            }
        }

        void ReleaseLeft()
        {
            if (pullSheet != null) FinishPull();
            if (heldSheet != null) { heldSheet.Release(); heldSheet = null; }
            if (crumpling) FinishCrumple();
            if (taping) { taping = false; if (benchBox != null && benchBox.Taped) PutToolBack(); }
            if (held != null) Drop();
            if (lifted != null && !liftedReturning) PutDown();
        }

        // ── 집기·놓기 ──
        void Grab(PackItem it)
        {
            if (it == null) return;
            held = it;
            heldRot = it.Body.rotation;
            var b = Bounds(it);
            heldHalfH = b.extents.y;
            holdDist = Mathf.Clamp(Vector3.Distance(cam.transform.position, it.Body.position), 0.25f, reach);
            breakT = 0f;
            WakeSheetsNear(b);
            if (LegacyHold) it.Body.isKinematic = true;
            else
            {
                // 2판: 동적 그대로 — 손은 속도로 끌어당긴다(FixedUpdate). 들고 있는 동안만 중력 끔(손이 받쳐 줌)
                it.Body.isKinematic = false;
                it.Body.useGravity = false;
                it.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                it.Body.maxAngularVelocity = 20f;
                it.Body.WakeUp();
            }
            SetLayer(it.gameObject, 2);
            Sfx(sfxPick, 0.4f, Random.Range(0.95f, 1.1f));
        }

        void Drop()
        {
            var it = held; held = null; crumpling = false;
            it.Body.isKinematic = false;
            it.Body.useGravity = true;
            it.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            // 손을 떼면 손 속도가 조금 남는다(던지기는 없음 — 최대 0.6 m/s)
            it.Body.linearVelocity = Vector3.ClampMagnitude(it.Body.linearVelocity, 0.6f);
            it.Body.angularVelocity = Vector3.ClampMagnitude(it.Body.angularVelocity, 3f);
            SetLayer(it.gameObject, 0);
            it.Body.WakeUp();
        }

        static void SetLayer(GameObject g, int layer) { foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }

        static Bounds Bounds(PackItem it)
        {
            var cs = it.GetComponentsInChildren<Collider>();
            var b = cs.Length > 0 ? cs[0].bounds : new Bounds(it.transform.position, Vector3.one * 0.05f);
            for (int i = 1; i < cs.Length; i++) b.Encapsulate(cs[i].bounds);
            return b;
        }

        /// <summary>손 목표점 — 시선 위 잡은 거리(휠). 그 앞에 면이 있으면 면 위에 얹는다(물건 두께만큼 띄움).</summary>
        Vector3 HoldTarget()
        {
            if (SimTarget.HasValue) return SimTarget.Value;
            Vector3 o = cam.transform.position, f = cam.transform.forward;
            Vector3 p = o + f * holdDist;
            if (!hasHit) return p;
            Vector3 n = lookHit.normal;
            float ext = heldHalfH;
            if (held != null)
            {
                var e = Bounds(held).extents;
                ext = Mathf.Abs(n.x) * e.x + Mathf.Abs(n.y) * e.y + Mathf.Abs(n.z) * e.z;
            }
            else if (heldSheet != null) ext = 0.02f;
            // 휠로 보낸 점이 면에서 물건 두께 + 띄움보다 가까우면 면 위에 얹는다.
            // (전엔 면에 1 cm 까지 붙어야 얹어서, 그 사이엔 목표가 면 속 → 물건이 면에 걸려 휠이 안 먹는 구간이 있었다)
            float cos = Mathf.Max(0.2f, -Vector3.Dot(f, n));
            if ((lookHit.distance - holdDist) * cos < ext + holdLift) p = lookHit.point + n * (ext + holdLift);
            return p;
        }

        /// <summary>면에 얹지 않은 목표점(뽁뽁이 풀기 — 끈 거리 그대로).</summary>
        Vector3 HoldTargetFree() => SimTarget ?? (hasHit && holdDist >= lookHit.distance ? lookHit.point + Vector3.up * 0.02f : cam.transform.position + cam.transform.forward * holdDist);

        void WakeSheetsNear(Bounds b)
        {
            b.Expand(0.04f);
            foreach (var sh in FindObjectsByType<BubbleSheet>(FindObjectsSortMode.None))
                if (sh != heldSheet && sh.Settled && sh.WorldBounds.Intersects(b)) sh.Wake();
        }

        // ── 뽁뽁이(2판) ──
        void StartPull(PackSupply roll)
        {
            // 롤 = 눕힌 원기둥(축 = 롤의 up). 시트는 롤 윗면에서 손 쪽으로 풀려 나온다.
            var t = roll.transform;
            rollCross = t.up;
            float rad = t.lossyScale.x * 0.5f;
            Vector3 toCam = cam.transform.position - t.position; toCam = Vector3.ProjectOnPlane(toCam, rollCross).normalized;
            rollExit = t.position + Vector3.up * rad * 0.7f + toCam * rad * 0.7f;
            pullSheet = BubbleSheet.Create(bubbleMat);
            holdDist = Mathf.Clamp(Vector3.Distance(cam.transform.position, rollExit), 0.25f, reach);
            pullSheet.Pull(rollExit, rollExit + toCam * 0.02f, rollCross);
            pullTick = 0f;
            Sfx(sfxBubble, 0.45f, 0.9f);
        }

        void FinishPull()
        {
            var sh = pullSheet; pullSheet = null;
            if (sh.Length < BubbleSheet.MinLength) { Destroy(sh.gameObject); return; } // 거의 안 끌었으면 롤에 도로 감김
            sh.Tear();
            // 찢는 소리 — 짧은 긁힘 셋을 높게 겹친다
            Sfx(sfxScratch[0], 0.55f, 1.55f); Sfx(sfxScratch[1], 0.45f, 1.8f); Sfx(sfxScratch[2], 0.35f, 2.1f);
            Debug.Log($"[PackBench] 뽁뽁이 {sh.Length:F2} m 뜯음");
        }

        void GrabSheet(BubbleSheet sh, Vector3 at)
        {
            heldSheet = sh;
            sh.Grab(at);
            holdDist = Mathf.Clamp(Vector3.Distance(cam.transform.position, at), 0.25f, reach);
            Vector3 r = cam.transform.right; r.y = 0f;
            sheetCross = r.sqrMagnitude > 1e-4f ? r.normalized : Vector3.right;
            Sfx(sfxBubble, 0.35f, 1.1f);
        }

        /// <summary>E = 감싸기: 시트를 든 채 물건을 보거나, 물건 위에 얹힌 시트(또는 시트가 얹힌 물건)를 보고.</summary>
        bool TryWrapLook()
        {
            var look = LookAt<PackItem>();
            BubbleSheet sh = null; PackItem target = null;
            if (heldSheet != null && look != null && look.sheet == null) { sh = heldSheet; target = look; }
            else if (held == null && heldSheet == null && look != null) FindWrapPair(look, out sh, out target);
            if (sh == null || target == null) return false;
            return Wrap(sh, target);
        }

        /// <summary>물건 + 그 위에 얹힌 시트 짝 찾기(시트를 보든 물건을 보든).</summary>
        public static void FindWrapPair(PackItem look, out BubbleSheet sh, out PackItem target)
        {
            sh = null; target = null;
            if (look.sheet != null)
            {
                var sb = look.sheet.WorldBounds; sb.Expand(0.03f);
                foreach (var it in FindObjectsByType<PackItem>(FindObjectsSortMode.None))
                    if (it.sheet == null && !it.Wrapped && it.GetComponent<BoxCollider>() != null && sb.Intersects(Bounds(it))) { sh = look.sheet; target = it; return; }
                return;
            }
            if (look.Wrapped || look.GetComponent<BoxCollider>() == null) return;
            var b = Bounds(look); b.Expand(0.03f);
            // 닿은 시트가 여럿이면(밑에 깐 것 + 위에 덮은 것) 가장 긴 것 — 짧은 깐 시트를 골라 "모자람"으로 거절되던 것 고침
            foreach (var s in FindObjectsByType<BubbleSheet>(FindObjectsSortMode.None))
                if (s.Settled && s.WorldBounds.Intersects(b) && s.WorldBounds.max.y > b.center.y && (sh == null || s.Length > sh.Length)) { sh = s; target = look; }
        }

        public bool Wrap(BubbleSheet sh, PackItem target)
        {
            float need = BubbleSheet.WrapNeed(target);
            if (sh.Length < need * 0.9f) { Sfx(sfxDeny, 0.6f, 0.7f); Debug.Log($"[PackBench] 감싸기 실패 — 뽁뽁이 {sh.Length:F2} m < 한 바퀴 {need:F2} m"); return true; }
            if (heldSheet == sh) heldSheet = null;
            target.Wrap(bubbleMat, 0.005f);
            Destroy(sh.gameObject);
            Sfx(sfxBubble, 0.6f, 0.8f); Sfx(sfxScratch[1], 0.4f, 1.3f);
            Debug.Log($"[PackBench] 감쌈 {target.name} (뽁뽁이 {sh.Length:F2} m / 한 바퀴 {need:F2} m)");
            return true;
        }

        // ── 놓일 자리 그림자(점선 윤곽) ──
        Transform[] dots; Material dotMat;
        const int DotCount = 36;

        void LateUpdate()
        {
            bool show = (held != null || heldSheet != null) && !SimHideDots;
            // Play 진입 도메인 리로드가 private 배열을 null 이 아니라 빈 배열로 되살린다 → 길이로 판단
            if (dots == null || dots.Length != DotCount) { if (!show) return; MakeDots(); }
            if (!show) { foreach (var d in dots) d.gameObject.SetActive(false); return; }
            // 윤곽(바닥에 비친 네 귀퉁이) — 상자형은 자기 축, 시트는 경계 상자
            Vector3 c0, ax, az;
            if (held != null)
            {
                var bc = held.GetComponent<BoxCollider>();
                var t = held.transform;
                if (bc != null)
                {
                    Vector3 hx = t.TransformVector(new Vector3(bc.size.x * 0.5f, 0f, 0f)), hy = t.TransformVector(new Vector3(0f, bc.size.y * 0.5f, 0f)), hz = t.TransformVector(new Vector3(0f, 0f, bc.size.z * 0.5f));
                    // 바닥에 비친 넓은 두 축(제일 위를 향한 축을 뺀다)
                    float ux = Mathf.Abs(hx.normalized.y), uy = Mathf.Abs(hy.normalized.y), uz = Mathf.Abs(hz.normalized.y);
                    if (ux >= uy && ux >= uz) { ax = hy; az = hz; } else if (uy >= uz) { ax = hx; az = hz; } else { ax = hx; az = hy; }
                    c0 = t.TransformPoint(bc.center);
                }
                else { var b = Bounds(held); c0 = b.center; ax = new Vector3(b.extents.x, 0f, 0f); az = new Vector3(0f, 0f, b.extents.z); }
            }
            else { var b = heldSheet.WorldBounds; c0 = b.center; ax = new Vector3(b.extents.x, 0f, 0f); az = new Vector3(0f, 0f, b.extents.z); }
            ax.y = 0f; az.y = 0f;
            for (int i = 0; i < DotCount; i++)
            {
                float u = i / (float)DotCount * 4f; int side = (int)u; float f = u - side;
                Vector3 off = side == 0 ? Vector3.Lerp(-ax - az, ax - az, f) : side == 1 ? Vector3.Lerp(ax - az, ax + az, f) : side == 2 ? Vector3.Lerp(ax + az, -ax + az, f) : Vector3.Lerp(-ax + az, -ax - az, f);
                Vector3 o = c0 + off;
                bool hit = Physics.Raycast(o, Vector3.down, out var h, 1.2f, ~(1 << 2), QueryTriggerInteraction.Ignore);
                var d = dots[i];
                d.gameObject.SetActive(hit);
                if (hit) d.SetPositionAndRotation(h.point + h.normal * 0.0015f, Quaternion.LookRotation(-h.normal));
            }
        }

        public static bool SimHideDots;

        void MakeDots()
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            dotMat = new Material(sh != null ? sh : Shader.Find("Unlit/Color")) { name = "PB_Dot" };
            var col = new Color(0.08f, 0.08f, 0.09f);
            dotMat.SetColor("_BaseColor", col); dotMat.color = col;
            var root = new GameObject("PlaceDots").transform;
            dots = new Transform[DotCount];
            for (int i = 0; i < DotCount; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(root, false);
                q.transform.localScale = Vector3.one * 0.009f;
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = dotMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                dots[i] = q.transform;
                q.SetActive(false);
            }
        }

        // ── 신문지 ──
        void StartCrumple()
        {
            var it = SpawnPaper(lookHit.point + Vector3.up * 0.08f, paperRadiusLoose);
            Grab(it);
            crumpling = true; crumpleT0 = Time.time;
        }

        void FinishCrumple()
        {
            crumpling = false;
            if (held == null) return;
            float k = Mathf.Clamp01((Time.time - crumpleT0) / crumpleFullSec);
            float r = Mathf.Lerp(paperRadiusLoose, paperRadiusTight, k);
            held.transform.localScale = Vector3.one * 2f * r;
            heldHalfH = r;
        }

        public PackItem SpawnPaper(Vector3 at, float radius)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Paper";
            g.transform.position = at;
            g.transform.localScale = Vector3.one * 2f * radius;
            g.transform.rotation = Random.rotation;
            if (paperMat != null) g.GetComponent<Renderer>().sharedMaterial = paperMat;
            g.GetComponent<Collider>().sharedMaterial = SoftMat;
            var rb = g.AddComponent<Rigidbody>();
            // 실제 신문지 한 장 ≈ 20 g 이지만 2 kg 판과 67배 차이면 PhysX 가 판을 완충재 속으로 밀어 넣는다(1153 진단) → 질량은 풀이 안정용(가설), 소리 무게는 PackItem.SoundWeight 따로
            rb.mass = 0.15f; rb.linearDamping = 0.6f; rb.angularDamping = 2f;
            Setup(rb);
            var it = g.AddComponent<PackItem>(); it.kind = PackItemKind.Paper;
            return it;
        }

        public PackItem SpawnBubble(Vector3 at)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Bubble";
            g.transform.position = at;
            g.transform.rotation = Quaternion.Euler(0f, benchSpot != null ? benchSpot.eulerAngles.y : 0f, 0f);
            g.transform.localScale = new Vector3(0.30f, 0.02f, 0.30f);
            if (bubbleMat != null) g.GetComponent<Renderer>().sharedMaterial = bubbleMat;
            g.GetComponent<Collider>().sharedMaterial = SoftMat;
            // 뽁뽁이 알갱이(보기만)
            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 5; z++)
                {
                    var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(d.GetComponent<Collider>());
                    d.transform.SetParent(g.transform, false);
                    d.transform.localPosition = new Vector3((x - 2) * 0.19f, 0.5f, (z - 2) * 0.19f);
                    d.transform.localScale = new Vector3(0.12f, 0.9f, 0.12f);
                    if (bubbleMat != null) d.GetComponent<Renderer>().sharedMaterial = bubbleMat;
                }
            var rb = g.AddComponent<Rigidbody>();
            rb.mass = 0.4f; rb.linearDamping = 0.8f; rb.angularDamping = 2f; // 질량 = 풀이 안정용(위 신문지 주석)
            Setup(rb);
            var it = g.AddComponent<PackItem>(); it.kind = PackItemKind.Bubble;
            return it;
        }

        public static PhysicsMaterial SoftMaterial => SoftMat;

        public static void Setup(Rigidbody rb)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // 키네마틱(든 동안)에서도 경고 없음
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.solverIterations = 12; rb.solverVelocityIterations = 4;
        }

        static PhysicsMaterial softMat;
        static PhysicsMaterial SoftMat => softMat != null ? softMat : (softMat = new PhysicsMaterial("PackSoft") { dynamicFriction = 0.8f, staticFriction = 0.9f, bounciness = 0f, frictionCombine = PhysicsMaterialCombine.Maximum, bounceCombine = PhysicsMaterialCombine.Minimum });

        // ── 도구(테이프·라벨) ──
        void TakeTool(Tool t, PackSupply s)
        {
            tool = t; toolSupply = s;
            var r = s.GetComponentInChildren<Renderer>();
            toolVis = Instantiate(r.gameObject);
            foreach (var c in toolVis.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var c in toolVis.GetComponentsInChildren<PackSupply>()) Destroy(c);
            toolVis.transform.SetParent(cam.transform, false);
            toolVis.transform.localPosition = new Vector3(0.16f, -0.12f, 0.38f);
            toolVis.transform.localRotation = Quaternion.Euler(20f, -25f, 0f);
            toolVis.transform.localScale = r.transform.lossyScale;
            foreach (var rr in s.GetComponentsInChildren<Renderer>()) rr.enabled = false;
            Sfx(sfxPick, 0.5f, 0.9f);
        }

        void PutToolBack()
        {
            if (toolSupply != null) foreach (var rr in toolSupply.GetComponentsInChildren<Renderer>()) rr.enabled = true;
            if (toolVis != null) Destroy(toolVis);
            tool = Tool.None; toolSupply = null; taping = false;
        }

        // ── 상자 옮기기·들기 ──
        void MoveBox(PackBox b, Pose to) { moving = b; movingTo = to; }

        void Lift(PackBox b)
        {
            lifted = b; liftedReturning = false;
            b.BeginShake();
            Sfx(sfxPick, 0.5f, 0.8f);
            if (!timed && firstTouch >= 0f && b.IsClosed && b.Taped)
            {
                timed = true;
                LastPackSeconds = Time.time - firstTouch;
                Debug.Log($"[PackBench] 한 번 싸는 데 {LastPackSeconds:F1}초 (첫 손 → 닫고 테이프 붙여 들기, 라벨 {(b.Labeled ? "있음" : "없음")})");
            }
        }

        void PutDown() { liftedReturning = true; simShakeUntil = -1f; }

        Pose LiftPose()
        {
            var t = cam.transform;
            Vector3 p = t.position + t.forward * 0.55f - t.up * 0.12f;
            Vector3 fwd = t.forward; fwd.y = 0f;
            var rot = Quaternion.LookRotation(fwd.sqrMagnitude > 1e-4f ? fwd : Vector3.forward, Vector3.up);
            // 봇·QA 흔들기: 정해진 무늬(3 Hz 좌우 5 cm + 앞뒤 3.5 cm + 위아래 2.5 cm + 기울기 ±8°) — 손으로 흔드는 세기 근사(가설)
            if (Time.time < simShakeUntil)
            {
                float s = (Time.time - simShakeT0) * Mathf.PI * 2f * 3f;
                p += rot * new Vector3(0.05f * Mathf.Sin(s), 0.025f * Mathf.Sin(s * 1.7f + 0.6f), 0.035f * Mathf.Sin(s * 1.3f + 1.1f));
                rot = rot * Quaternion.Euler(8f * Mathf.Sin(s * 0.9f + 0.3f), 0f, 8f * Mathf.Sin(s * 1.1f));
            }
            return new Pose(p, rot);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (held != null && LegacyHold)
            {
                var rb = held.Body;
                rb.MovePosition(Vector3.MoveTowards(rb.position, HoldTarget(), holdMaxSpeed * dt));
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, heldRot, 720f * dt));
            }
            else if (held != null) DriveHeld(dt);
            if (heldSheet != null) heldSheet.Hold(HoldTarget(), sheetCross, holdMaxSpeed, dt);
            if (moving != null)
            {
                var rb = moving.Body;
                rb.MovePosition(Vector3.MoveTowards(rb.position, movingTo.position, 2f * dt));
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, movingTo.rotation, 360f * dt));
                if ((rb.position - movingTo.position).sqrMagnitude < 1e-6f) { moving.transform.SetPositionAndRotation(movingTo.position, movingTo.rotation); moving = null; }
            }
            if (lifted != null)
            {
                var rb = lifted.Body;
                Pose to = liftedReturning ? new Pose(benchSpot.position, benchSpot.rotation) : LiftPose();
                rb.MovePosition(Vector3.MoveTowards(rb.position, to.position, (liftedReturning ? 1.5f : boxMaxSpeed) * dt));
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, to.rotation, (liftedReturning ? 240f : 600f) * dt));
                if (liftedReturning && (rb.position - to.position).sqrMagnitude < 1e-6f && Quaternion.Angle(rb.rotation, to.rotation) < 0.5f)
                {
                    lifted.transform.SetPositionAndRotation(to.position, to.rotation);
                    var res = lifted.EndShake();
                    LastShakeJson = res.ToJson();
                    LastPairs = lifted.PairsJson();
                    ShakeLog.Add(LastShakeJson);
                    Debug.Log("[PackBench] 흔들기 결과 " + LastShakeJson);
                    Shaken?.Invoke(lifted, res);
                    if (res.escaped > 0) Debug.LogWarning($"[PackBench] 상자 밖으로 나간 물건 {res.escaped}개");
                    Sfx(sfxClose, 0.5f, 1.1f);
                    lifted = null; liftedReturning = false;
                }
            }
        }

        /// <summary>든 물건을 목표점까지 속도로 끌어당긴다 — 힘 상한이 있어서 벽·물건에 막히면 멈춘다(통과 없음).</summary>
        void DriveHeld(float dt)
        {
            var rb = held.Body;
            Vector3 err = HoldTarget() - rb.position;
            Vector3 vDes = Vector3.ClampMagnitude(err * holdGain, holdMaxSpeed);
            Vector3 dv = vDes - rb.linearVelocity;
            float maxDv = Mathf.Min(holdMaxForce / Mathf.Max(0.05f, rb.mass), holdMaxAccel) * dt;
            rb.AddForce(Vector3.ClampMagnitude(dv, maxDv), ForceMode.VelocityChange);
            // 돌리기도 같은 식(각속도 추종, 상한)
            Quaternion dq = heldRot * Quaternion.Inverse(rb.rotation);
            dq.ToAngleAxis(out float ang, out Vector3 axis);
            if (ang > 180f) ang -= 360f;
            Vector3 wDes = Mathf.Abs(ang) < 0.01f || float.IsNaN(axis.x) ? Vector3.zero : axis.normalized * (ang * Mathf.Deg2Rad * 12f);
            wDes = Vector3.ClampMagnitude(wDes, 10f);
            rb.AddTorque(Vector3.ClampMagnitude(wDes - rb.angularVelocity, 150f * dt), ForceMode.VelocityChange);
            // 손에서 빠짐: 막힌 채(손 쪽으로 거의 안 다가옴) 너무 멀리 끌면. 멀어도 따라오는 중이면 안 빠진다.
            float closing = Vector3.Dot(rb.linearVelocity, err.normalized);
            if (err.magnitude > holdBreakDist && closing < 0.25f) { breakT += dt; if (breakT > 0.35f) { Drop(); Sfx(sfxLight, 0.5f, 0.7f); SlipCount++; } }
            else breakT = 0f;
        }
        public static int SlipCount;

        // ── [E] 칩 ──
        void UpdatePrompt()
        {
            Prompt = ""; PromptAnchor = null;
            if (lifted == null && moving == null && tool == Tool.None && held == null)
            {
                var look = LookAt<PackItem>();
                BubbleSheet sh = null; PackItem target = null;
                if (look != null)
                {
                    if (heldSheet != null && look.sheet == null && !look.Wrapped && look.GetComponent<BoxCollider>() != null) { sh = heldSheet; target = look; }
                    else if (heldSheet == null) FindWrapPair(look, out sh, out target);
                }
                if (sh != null && target != null) { Prompt = "E 감싸기"; PromptAnchor = Bounds(target).center + Vector3.up * (Bounds(target).extents.y + 0.03f); return; }
            }
            if (held != null || lifted != null || moving != null || tool != Tool.None || heldSheet != null || pullSheet != null) return;
            var b = LookBox();
            if (b == null) return;
            if (b == benchBox) Prompt = b.IsClosed ? "E 열기" : "E 닫기";
            else Prompt = "E 고르기";
            PromptAnchor = b.transform.position + b.transform.up * (b.RimY + 0.02f);
        }

        // ── 소리(기존 Kenney CC0 클립 재사용 — Resources/WrapMaterials/Sfx) ──
        static AudioSource[] pool; static int poolNext; static float lastRattle;
        static AudioClip sfxHard, sfxLight, sfxPick, sfxClose, sfxDeny, sfxDone, sfxBubble;
        static AudioClip[] sfxScratch;
        /// <summary>QA: 이번 흔들기에서 실제로 낸 덜그럭 소리 수·크기 합.</summary>
        public static int RattlePlayed; public static float RattleVolumeSum;

        void EnsureSfx()
        {
            pool = new AudioSource[8];
            for (int i = 0; i < pool.Length; i++) { var s = gameObject.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 0f; pool[i] = s; }
            const string dir = "WrapMaterials/Sfx/";
            sfxHard = Resources.Load<AudioClip>(dir + "drop_002");
            sfxLight = Resources.Load<AudioClip>(dir + "tick_002");
            sfxPick = Resources.Load<AudioClip>(dir + "switch_002");
            sfxClose = Resources.Load<AudioClip>(dir + "drop_004");
            sfxDeny = Resources.Load<AudioClip>(dir + "tick_002");
            sfxDone = Resources.Load<AudioClip>(dir + "select_002");
            sfxBubble = Resources.Load<AudioClip>(dir + "scratch_003");
            sfxScratch = new[] { Resources.Load<AudioClip>(dir + "scratch_001"), Resources.Load<AudioClip>(dir + "scratch_002"), Resources.Load<AudioClip>(dir + "scratch_003") };
        }

        static void Sfx(AudioClip c, float vol, float pitch)
        {
            if (c == null || pool == null) return;
            var s = pool[poolNext]; poolNext = (poolNext + 1) % pool.Length;
            s.pitch = pitch; s.PlayOneShot(c, vol);
        }

        /// <summary>흔드는 중이 아닐 때 닿음(내려놓기·벽에 부딪힘) → 살짝 툭. 무른 쪽 소리.</summary>
        public static void PlayTouch(PackItem it, PackItem other, float speed)
        {
            float w = other != null ? Mathf.Min(it.SoundWeight, other.SoundWeight) : it.SoundWeight;
            float vol = Mathf.Clamp01((speed - 0.12f) / 0.9f) * (0.25f + 0.75f * w) * 0.55f;
            if (vol < 0.02f) return;
            TouchPlayed++;
            if (w >= 0.9f) Sfx(sfxHard, vol, Random.Range(0.95f, 1.05f));
            else Sfx(sfxLight, vol, Random.Range(0.75f, 0.9f));
        }
        public static int TouchPlayed;

        /// <summary>흔드는 중 부딪힘 한 번 → 덜그럭. 크기 = 부딪힌 세기 × 물건 무게(PackItem.SoundWeight).</summary>
        public static void PlayRattle(PackItem it, float vol)
        {
            if (Time.time - lastRattle < 0.025f) return;
            lastRattle = Time.time;
            RattlePlayed++; RattleVolumeSum += vol;
            if (it.kind == PackItemKind.Eggs) Sfx(sfxHard, Mathf.Clamp01(vol * 1.2f), Random.Range(1.15f, 1.35f));
            else if (it.kind == PackItemKind.Bubble) Sfx(sfxLight, vol, Random.Range(0.8f, 0.95f));
            else Sfx(sfxScratch[Random.Range(0, sfxScratch.Length)], vol * 0.8f, Random.Range(1.3f, 1.6f));
        }

        // ══ 보관소 작업대(D-251) — 걷기 감독이 켜고 끈다 ══
        /// <summary>흔들어 보고 작업대에 다시 내려놓은 순간(상자, 결과).</summary>
        public static event System.Action<PackBox, PackBox.ShakeResult> Shaken;
        /// <summary>작업대를 떠나도 되나 — 든 상자가 아직 공중이거나 선반 상자가 옮겨지는 중이면 아니다.</summary>
        public bool CanLeave => lifted == null && moving == null;
        /// <summary>손에 든 물건·종이·도구를 놓는다. 든 상자는 내려놓기 시작(CanLeave 가 될 때까지 기다릴 것).</summary>
        public void ReleaseAll()
        {
            ReleaseLeft();                                  // 왼손을 뗀 것과 같은 길(구기던 종이 마무리·뽁뽁이 뜯기·든 상자 내려놓기)
            if (tool != Tool.None) PutToolBack();
            simHold = false; prevSimHold = false;
        }

        // ══ 봇·QA (마우스 대신 — 같은 동작을 스크립트로) ══
        public PackBox FindBox(string size) => boxes.Find(b => b.sizeName == size);

        /// <summary>QA: 손으로 집기·놓기·뽁뽁이 풀기를 마우스 없이(같은 코드 경로). 목표점은 SimTarget.</summary>
        public void SimGrab(PackItem it) { if (held != null) Drop(); Grab(it); }
        public void SimDrop() { if (held != null) Drop(); if (heldSheet != null) { heldSheet.Release(); heldSheet = null; } }
        public void SimGrabSheet(BubbleSheet sh) { GrabSheet(sh, sh.Center); }
        public bool SimPullStart()
        {
            foreach (var s in FindObjectsByType<PackSupply>(FindObjectsSortMode.None)) if (s.kind == PackSupplyKind.Bubble) { StartPull(s); return true; }
            return false;
        }
        public void SimPullStep() { if (pullSheet != null) HoldLeft(); }
        public BubbleSheet SimPullEnd() { var sh = pullSheet; if (sh != null) FinishPull(); return sh; }
        public Vector3 RollExit => rollExit;

        public PackItem Tray()
        {
            foreach (var it in FindObjectsByType<PackItem>(FindObjectsSortMode.None)) if (it.kind == PackItemKind.Eggs) return it;
            return null;
        }

        /// <summary>시나리오: "bare" = 완충재 없이 달걀판만 · "bottom" = 뽁뽁이 바닥만(위 빈틈 그대로) · "full" = 뽁뽁이 바닥 + 달걀판 + 신문지 3×3 위 · "flip" = full 인데 판을 뒤집어 넣음. 끝나면 LastShakeJson.</summary>
        public static string BotRun(string scenario, float shakeSec = 3f)
        {
            if (I == null) return "no PackBench";
            I.StartCoroutine(I.BotRoutine(scenario, shakeSec));
            return "started " + scenario;
        }
        public static string BotStatus = "idle";
        /// <summary>QA: 비어 있지 않으면 봇이 단계마다 화면을 찍는다({dir}/{scenario}_{step}.png) + 흔드는 동안 QaFrameEvery 초마다.</summary>
        public static string QaShotDir = "";
        public static float QaFrameEvery = 0.5f;
        string botScenario;

        IEnumerator Shot(string step)
        {
            if (string.IsNullOrEmpty(QaShotDir)) yield break;
            ScreenCapture.CaptureScreenshot($"{QaShotDir}/{botScenario}_{step}.png");
            yield return null; yield return null;
        }

        static void SetLidSeeThrough(PackBox b, bool on)
        {
            var lid = b.transform.Find("Shell/LidPivot");
            if (lid != null) foreach (var r in lid.GetComponentsInChildren<Renderer>()) r.enabled = !on;
            var tape = b.transform.Find("Shell/Tape");
            if (tape != null) foreach (var r in tape.GetComponentsInChildren<Renderer>()) r.enabled = !on;
            foreach (var r in b.GetComponentsInChildren<Renderer>()) if (r.name == "Label" || (r.transform.parent != null && r.transform.parent.name == "Label")) r.enabled = !on;
        }

        IEnumerator BotRoutine(string scenario, float shakeSec)
        {
            BotStatus = "run " + scenario; botScenario = scenario;
            SimActive = true;
            float t0 = Time.time; firstTouch = Time.time; timed = false;
            var box = FindBox("M");
            if (benchBox != box)
            {
                if (benchBox != null) { benchBox.Open(); benchBox.TryClose(); benchBox.SnapLid(); if (benchBox.shelfSlot != null) benchBox.transform.SetPositionAndRotation(benchBox.shelfSlot.position, benchBox.shelfSlot.rotation); }
                benchBox = box; box.Open(); MoveBox(box, new Pose(benchSpot.position, benchSpot.rotation));
            }
            else if (box.IsClosed) box.Open();
            while (moving != null) yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("1_box");
            // 상자 안 정리: 이전 신문지·뽁뽁이 치우고 달걀판을 상자 위로
            foreach (var it in FindObjectsByType<PackItem>(FindObjectsSortMode.None)) if (it.kind != PackItemKind.Eggs) Destroy(it.gameObject);
            yield return null;
            var tr = box.transform;
            float floorY = box.InnerBottomY;
            bool full = scenario == "full" || scenario == "flip";
            if (full || scenario == "bottom")
            {
                var bub = SpawnBubble(tr.TransformPoint(new Vector3(0f, floorY + 0.02f, 0f)));
                bub.transform.rotation = tr.rotation;
                yield return new WaitForSeconds(0.6f);
                floorY += 0.02f;
                yield return Shot("2_bubble");
            }
            var tray = Tray();
            tray.Body.isKinematic = false;
            var trayAt = tr.TransformPoint(new Vector3(0f, floorY + 0.0325f + 0.01f, 0f));
            // "flip" = 꽉 채웠지만 판을 뒤집어 넣음(방향 틀림)
            var trayRot = scenario == "flip" ? tr.rotation * Quaternion.Euler(180f, 0f, 0f) : tr.rotation;
            tray.transform.SetPositionAndRotation(trayAt, trayRot);
            tray.Body.position = trayAt; tray.Body.rotation = trayRot;
            tray.Body.linearVelocity = Vector3.zero; tray.Body.angularVelocity = Vector3.zero;
            yield return new WaitForSeconds(0.8f);
            yield return Shot("3_tray");
            if (full)
            {
                // 뚜껑까지 채움 = 공 꼭대기가 테두리(RimY)에 닿거나 살짝 넘침(뚜껑이 눌러 줌, TryClose 허용 1 cm 안) — 6.2 cm 공은 1.2 cm 틈이 남아 3개가 굴렀다(1153)
                const float pr = 0.034f;
                float y = floorY + 0.065f + pr;
                for (int x = -1; x <= 1; x++)
                    for (int z = -1; z <= 1; z++)
                        SpawnPaper(tr.TransformPoint(new Vector3(x * 0.1f, y + 0.005f, z * 0.1f)), pr);
                yield return new WaitForSeconds(1.0f);
                yield return Shot("4_paper");
            }
            bool closed = box.TryClose();
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i <= 10; i++) box.AddTape(i / 10f);
            box.StickLabel(tr.TransformPoint(new Vector3(0.06f, box.RimY + box.wall * 2f, -0.06f)), tr.up);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("5_taped");
            RattlePlayed = 0; RattleVolumeSum = 0f;
            Lift(box);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("6_lifted");
            simShakeT0 = Time.time; simShakeUntil = Time.time + shakeSec;
            if (!string.IsNullOrEmpty(QaShotDir) && QaFrameEvery > 0f)
            {
                // 흔드는 동안 연속 프레임 — 뚜껑을 투명하게(보기만, 충돌은 그대로) 해서 속을 찍는다
                SetLidSeeThrough(box, true);
                int n = 0;
                while (Time.time < simShakeUntil)
                {
                    ScreenCapture.CaptureScreenshot($"{QaShotDir}/{scenario}_shake_{n++:D2}.png");
                    yield return new WaitForSeconds(QaFrameEvery);
                }
                SetLidSeeThrough(box, false);
                yield return new WaitForSeconds(0.2f);
            }
            else yield return new WaitForSeconds(shakeSec + 0.2f);
            PutDown();
            while (lifted != null) yield return new WaitForFixedUpdate();
            SimActive = false;
            yield return Shot("7_after");
            BotStatus = $"pairs={LastPairs} done {scenario} closed={closed} played={RattlePlayed} volSum={RattleVolumeSum:F2} botSec={Time.time - t0:F1} " + LastShakeJson;
            Debug.Log("[PackBench] 봇 " + BotStatus);
        }
    }
}
