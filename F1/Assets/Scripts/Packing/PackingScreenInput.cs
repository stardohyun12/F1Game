using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.Packing
{
    public sealed class PackingScreenInput : MonoBehaviour
    {
        [SerializeField] private float yawSpeedDegPerSec = 90f;
        [SerializeField] private float pitchSpeedDegPerSec = 90f;
        [SerializeField] private float minWidthUV = 0.02f;
        [SerializeField] private float maxWidthUV = 0.18f;
        [SerializeField] private float wheelStep = 0.01f;

        public float MinWidthUV => minWidthUV;
        public float MaxWidthUV => maxWidthUV;

        public Transform RotateTarget { get; set; }
        public WrapPainter Painter { get; set; }
        public PackingScreen Screen { get; set; }

        // InputAction 기반 — Keyboard.current 직접 폴링보다 안정적
        InputAction _wasdAction;
        InputAction _resetAction;
        InputAction _selectMat1, _selectMat2, _selectMat3;
        InputAction _mouseClickAction;
        InputAction _mousePosAction;
        InputAction _wheelAction;
        // 채널 5 — 배치 단계 회전 (WASD/QE 직접 폴링으로 대체, InputAction 불필요)

        bool _loggedFirstInput;

        // 씬 로드 전에 InputSystem이 포커스에 의존하지 않도록 강제
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ConfigureInputSystemEarly()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        }

        void OnEnable()
        {
            _wasdAction = new InputAction("wasd");
            _wasdAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _wasdAction.Enable();

            _resetAction = new InputAction("reset", binding: "<Keyboard>/r");
            _resetAction.Enable();

            _selectMat1 = new InputAction("mat1", binding: "<Keyboard>/1");
            _selectMat2 = new InputAction("mat2", binding: "<Keyboard>/2");
            _selectMat3 = new InputAction("mat3", binding: "<Keyboard>/3");
            _selectMat1.Enable(); _selectMat2.Enable(); _selectMat3.Enable();

            _mouseClickAction = new InputAction("click", binding: "<Mouse>/leftButton", type: InputActionType.Button);
            _mouseClickAction.Enable();

            _mousePosAction = new InputAction("pos", binding: "<Mouse>/position");
            _mousePosAction.Enable();

            _wheelAction = new InputAction("wheel", binding: "<Mouse>/scroll/y");
            _wheelAction.Enable();

            // Q/E/A/D 회전은 Keyboard.current 직접 폴링 (Update에서 처리)
        }

        void OnDisable()
        {
            _wasdAction?.Disable(); _wasdAction?.Dispose();
            _resetAction?.Disable(); _resetAction?.Dispose();
            _selectMat1?.Disable(); _selectMat1?.Dispose();
            _selectMat2?.Disable(); _selectMat2?.Dispose();
            _selectMat3?.Disable(); _selectMat3?.Dispose();
            _mouseClickAction?.Disable(); _mouseClickAction?.Dispose();
            _mousePosAction?.Disable(); _mousePosAction?.Dispose();
            _wheelAction?.Disable(); _wheelAction?.Dispose();
            // Q/E/A/D 직접 폴링 — dispose 불필요
        }

        void Update()
        {
            if (Screen == null) return;
            if (!Screen.IsVisible) return;

            // 채널 5 Plan 2 — 배치 단계: 마우스 드래그로 트렁크에 물건 놓기 (포장 입력 차단).
            if (Screen.IsPlacing)
            {
                if (_mouseClickAction != null && _mousePosAction != null)
                {
                    Vector2 sp = _mousePosAction.ReadValue<Vector2>();
                    if (_mouseClickAction.WasPressedThisFrame()) Screen.BeginTrunkDrag(sp);
                    else if (_mouseClickAction.IsPressed()) Screen.UpdateTrunkDrag(sp);
                    else if (_mouseClickAction.WasReleasedThisFrame()) Screen.EndTrunkDrag();
                }
                        // 채널 5 — 드래그 중 큐브 회전.
                // W/S = X축 90도 스냅 (pitch), A/D = Y축 90도 스냅 (yaw),
                // Q/E = Z축 90도 스냅 (roll), 마우스 우클릭 드래그 = 자유 회전 (3D 표준).
                if (Screen.IsDraggingTrunkItem)
                {
                    // W/S = X축 pitch (위아래), A/D = Y축 yaw (좌우), Q/E = Z축 roll
                    // WASD 회전만 (Q/E roll · 마우스 우클릭 자유 회전 제거 — 도현 요청)
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.wKey.wasPressedThisFrame) Screen.RotateDraggedItemAxis(Vector3.right, -90f);
                        if (Keyboard.current.sKey.wasPressedThisFrame) Screen.RotateDraggedItemAxis(Vector3.right, 90f);
                        if (Keyboard.current.aKey.wasPressedThisFrame) Screen.RotateDraggedItemAxis(Vector3.up, -90f);
                        if (Keyboard.current.dKey.wasPressedThisFrame) Screen.RotateDraggedItemAxis(Vector3.up, 90f);
                    }
                }
                return;
            }

            if (RotateTarget == null) return;

            // WASD 회전
            Vector2 wasd = _wasdAction != null ? _wasdAction.ReadValue<Vector2>() : Vector2.zero;
            if (wasd.sqrMagnitude > 0.0001f)
            {
                if (!_loggedFirstInput)
                {
                    Debug.Log($"[PackingScreenInput] WASD detected wasd={wasd}");
                    _loggedFirstInput = true;
                }
                // 드래그 중에도 회전 가능 — 시작점은 큐브 local 좌표로 저장되어 자동 따라감
                RotateTarget.Rotate(Vector3.up, wasd.x * yawSpeedDegPerSec * Time.unscaledDeltaTime, Space.World);
                RotateTarget.Rotate(Vector3.right, -wasd.y * pitchSpeedDegPerSec * Time.unscaledDeltaTime, Space.World);
            }

            if (_resetAction != null && _resetAction.WasPressedThisFrame()) RotateTarget.rotation = Quaternion.identity;
            if (_selectMat1 != null && _selectMat1.WasPressedThisFrame()) Screen.SelectMaterialByChannel(0);
            if (_selectMat2 != null && _selectMat2.WasPressedThisFrame()) Screen.SelectMaterialByChannel(1);
            if (_selectMat3 != null && _selectMat3.WasPressedThisFrame()) Screen.SelectMaterialByChannel(2);

            // 마우스
            if (Painter != null && _mouseClickAction != null && _mousePosAction != null)
            {
                Vector2 screenPos = _mousePosAction.ReadValue<Vector2>();
                if (_mouseClickAction.WasPressedThisFrame()) Painter.BeginStroke(screenPos);
                else if (_mouseClickAction.IsPressed()) Painter.UpdateStroke(screenPos);
                else if (_mouseClickAction.WasReleasedThisFrame()) Painter.EndStroke();

                float wheel = _wheelAction != null ? _wheelAction.ReadValue<float>() : 0f;
                if (Mathf.Abs(wheel) > 0.01f)
                {
                    // 휠 한 칸 = ±1 (scroll/y 는 OS 에 따라 ±120 이 올 수 있어 부호만 씀)
                    Painter.StripWidthUV = Mathf.Clamp(Painter.StripWidthUV + Mathf.Sign(wheel) * wheelStep, minWidthUV, maxWidthUV);
                    // 랩 폭 라벨·재료 카드 ₩/m 갱신 (예전엔 라벨이 고정 문자열이었다)
                    Screen.OnWrapWidthChanged(Painter.StripWidthUV,
                        Mathf.InverseLerp(minWidthUV, maxWidthUV, Painter.StripWidthUV));
                }
            }
        }
    }
}
