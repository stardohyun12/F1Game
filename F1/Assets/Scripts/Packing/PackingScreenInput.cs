using UnityEngine;

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

            // WASD 회전
            float yaw = 0, pitch = 0;
            if (Input.GetKey(KeyCode.A)) yaw -= 1;
            if (Input.GetKey(KeyCode.D)) yaw += 1;
            if (Input.GetKey(KeyCode.W)) pitch -= 1;
            if (Input.GetKey(KeyCode.S)) pitch += 1;
            RotateTarget.Rotate(Vector3.up, yaw * yawSpeedDegPerSec * Time.deltaTime, Space.World);
            RotateTarget.Rotate(Vector3.right, pitch * pitchSpeedDegPerSec * Time.deltaTime, Space.World);
            if (Input.GetKeyDown(KeyCode.R)) RotateTarget.rotation = Quaternion.identity;

            // 재료 전환
            if (Input.GetKeyDown(KeyCode.Alpha1)) Screen.SelectMaterialByChannel(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Screen.SelectMaterialByChannel(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Screen.SelectMaterialByChannel(2);

            // 마우스 입력
            if (Painter != null)
            {
                if (Input.GetMouseButtonDown(0)) Painter.BeginStroke(Input.mousePosition);
                else if (Input.GetMouseButton(0)) Painter.UpdateStroke(Input.mousePosition);
                else if (Input.GetMouseButtonUp(0)) Painter.EndStroke();

                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f)
                    Painter.StripWidthUV = Mathf.Clamp(Painter.StripWidthUV + wheel * wheelStep, minWidthUV, maxWidthUV);
            }
        }
    }
}
