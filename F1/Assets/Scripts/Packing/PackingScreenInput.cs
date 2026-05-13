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
        }

        void Update()
        {
            if (Screen == null) return;
            if (!Screen.IsVisible) return;
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
                // 드래그 중 회전 시작하면 현재 stroke 취소 — 회전 도중에는 새 strip 안 만듦
                if (Painter != null) Painter.CancelStroke();
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
                    Painter.StripWidthUV = Mathf.Clamp(Painter.StripWidthUV + wheel * wheelStep, minWidthUV, maxWidthUV);
            }
        }
    }
}
