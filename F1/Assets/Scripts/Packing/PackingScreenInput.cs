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

        void Update()
        {
            if (Screen == null || !Screen.IsVisible || RotateTarget == null) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                // WASD 회전
                float yaw = 0, pitch = 0;
                if (kb.aKey.isPressed) yaw -= 1;
                if (kb.dKey.isPressed) yaw += 1;
                if (kb.wKey.isPressed) pitch -= 1;
                if (kb.sKey.isPressed) pitch += 1;
                RotateTarget.Rotate(Vector3.up, yaw * yawSpeedDegPerSec * Time.deltaTime, Space.World);
                RotateTarget.Rotate(Vector3.right, pitch * pitchSpeedDegPerSec * Time.deltaTime, Space.World);
                if (kb.rKey.wasPressedThisFrame) RotateTarget.rotation = Quaternion.identity;

                // 재료 전환
                if (kb.digit1Key.wasPressedThisFrame) Screen.SelectMaterialByChannel(0);
                if (kb.digit2Key.wasPressedThisFrame) Screen.SelectMaterialByChannel(1);
                if (kb.digit3Key.wasPressedThisFrame) Screen.SelectMaterialByChannel(2);
            }

            var mouse = Mouse.current;
            if (mouse != null && Painter != null)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame) Painter.BeginStroke(screenPos);
                else if (mouse.leftButton.isPressed) Painter.UpdateStroke(screenPos);
                else if (mouse.leftButton.wasReleasedThisFrame) Painter.EndStroke();

                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f)
                    Painter.StripWidthUV = Mathf.Clamp(Painter.StripWidthUV + wheel * wheelStep, minWidthUV, maxWidthUV);
            }
        }
    }
}
