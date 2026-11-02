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
    /// 조작: 시선 = 마우스(조준점 = 커서 하나) · 집기 = 왼쪽 클릭 유지, 놓기 = 떼기 · 돌리기 = 휠(15°)·R(옆으로 90°)
    ///   · 신문지 묶음 = 꾹 누르는 동안 구겨진다(오래 누를수록 작고 단단하게) · 테이프 = 롤 클릭으로 들고, 뚜껑 위 이음선을 끌어서 · 라벨 = 들고 상자에 클릭.
    ///   · E = 상자 고르기 / 뚜껑 닫기·열기(열면 테이프는 뜯김, 벌 없음).
    /// 화면 UI 는 걷기와 같은 문법(조준점 2×2 + [E] 칩)만 — PackBenchCursor.
    /// 타이머 없음(D-238). 걸린 시간은 로그로만 잰다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PackBench : MonoBehaviour
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
        [SerializeField] float holdLift = 0.05f;
        [SerializeField] float holdMaxSpeed = 2.5f;
        [Tooltip("든 상자가 따라오는 최고 속도(m/s) — 마우스를 휙 돌리면 이 속도로 흔들린다.")]
        [SerializeField] float boxMaxSpeed = 3.5f;
        [Tooltip("신문지: 이 시간(초) 동안 꾹 누르면 가장 작게(단단하게) 구겨진다.")]
        [SerializeField] float crumpleFullSec = 1.2f;
        [SerializeField] float paperRadiusLoose = 0.055f, paperRadiusTight = 0.024f;

        public PackBox Shaking => lifted != null && lifted.IsShaking ? lifted : null;
        public PackBox BenchBox => benchBox;
        /// <summary>손에 뭘 들었나(물건·도구·든 상자) — 커서 모양만 바뀐다.</summary>
        public bool HandBusy => held != null || tool != Tool.None || lifted != null;
        public string LookName { get; private set; } = "";
        /// <summary>[E] 칩 — 비면 안 보인다. "E 닫기" 꼴(걷기 OnFootDirector.Prompt 와 같은 문법).</summary>
        public string Prompt { get; private set; } = "";
        public Vector3? PromptAnchor { get; private set; }
        public static string LastShakeJson { get; private set; } = "";
        public static readonly List<string> ShakeLog = new List<string>();
        public static float LastPackSeconds { get; private set; } = -1f;

        float yaw0, yaw, pitch;
        enum Tool { None, Tape, Label }
        Tool tool;
        PackSupply toolSupply;
        GameObject toolVis;
        PackItem held; Quaternion heldRot; float heldHalfH;
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
            if (!SimActive && !ui && m != null)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    var d = m.delta.ReadValue() * mouseSensitivity;
                    yaw += d.x; pitch -= d.y;
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
            if (held != null)
            {
                if (wheel > 0.01f) heldRot = Quaternion.AngleAxis(15f, Vector3.up) * heldRot;
                if (wheel < -0.01f) heldRot = Quaternion.AngleAxis(-15f, Vector3.up) * heldRot;
                if (rKey) RotateSide();
            }
            UpdatePrompt();
        }
        bool prevSimHold;

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
            if (held != null || lifted != null || moving != null) return;
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
            if (item != null) { Touch(); Grab(item); return; }
            var sup = LookAt<PackSupply>();
            if (sup != null)
            {
                Touch();
                switch (sup.kind)
                {
                    case PackSupplyKind.Newspaper: StartCrumple(); break;
                    case PackSupplyKind.Bubble: Grab(SpawnBubble(lookHit.point + Vector3.up * 0.1f)); Sfx(sfxBubble, 0.5f, 1f); break;
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
            it.Body.isKinematic = true;
            SetLayer(it.gameObject, 2);
            Sfx(sfxPick, 0.4f, Random.Range(0.95f, 1.1f));
        }

        void Drop()
        {
            var it = held; held = null; crumpling = false;
            it.Body.isKinematic = false;
            it.Body.linearVelocity = Vector3.zero; it.Body.angularVelocity = Vector3.zero;
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

        Vector3 HoldTarget()
        {
            Vector3 o = cam.transform.position, f = cam.transform.forward;
            Vector3 p = hasHit ? lookHit.point + Vector3.up * (heldHalfH + holdLift) : o + f * 0.8f;
            Vector3 d = p - o;
            if (d.magnitude > reach) p = o + d.normalized * reach;
            return p;
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
            if (held != null)
            {
                var rb = held.Body;
                rb.MovePosition(Vector3.MoveTowards(rb.position, HoldTarget(), holdMaxSpeed * dt));
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, heldRot, 720f * dt));
            }
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
                    ShakeLog.Add(LastShakeJson);
                    Debug.Log("[PackBench] 흔들기 결과 " + LastShakeJson);
                    if (res.escaped > 0) Debug.LogWarning($"[PackBench] 상자 밖으로 나간 물건 {res.escaped}개");
                    Sfx(sfxClose, 0.5f, 1.1f);
                    lifted = null; liftedReturning = false;
                }
            }
        }

        // ── [E] 칩 ──
        void UpdatePrompt()
        {
            Prompt = ""; PromptAnchor = null;
            if (held != null || lifted != null || moving != null || tool != Tool.None) return;
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

        // ══ 봇·QA (마우스 대신 — 같은 동작을 스크립트로) ══
        public PackBox FindBox(string size) => boxes.Find(b => b.sizeName == size);

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
            BotStatus = $"done {scenario} closed={closed} played={RattlePlayed} volSum={RattleVolumeSum:F2} botSec={Time.time - t0:F1} " + LastShakeJson;
            Debug.Log("[PackBench] 봇 " + BotStatus);
        }
    }
}
