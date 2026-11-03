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

    }
}
