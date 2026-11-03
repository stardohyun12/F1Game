using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.PackBench
{
    // D-287: PackBench 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
    public partial class PackBench
    {
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
                    shakenBox = lifted; shakenOpen = lifted.OpenCount;
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
    }
}
