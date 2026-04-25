using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 도현 "운전 안됨" 진단. Race 인스턴스 런타임 상태 한 번에 출력.
/// Play 모드에서 실행 → isGrounded / smoothedMove / accelForce / 위치 / Raycast 결과.
/// </summary>
public static class DiagDriveBlocker
{
    [MenuItem("PitCrew/Car/DIAG Drive Blocker")]
    public static void Execute()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[DIAG] Play 모드에서만 작동.");
            return;
        }

        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) { Debug.LogError("[DIAG] ArcadeCarController 없음."); return; }

        var t = car.transform;
        var rb = car.GetComponent<Rigidbody>();
        var col = car.GetComponent<BoxCollider>();

        var sb = new StringBuilder("[DIAG]\n");
        sb.AppendLine($"world pos = {t.position}  rot = {t.eulerAngles}");
        sb.AppendLine($"rb.mass = {rb.mass}  linearDamping = {rb.linearDamping}  angularDamping = {rb.angularDamping}");
        sb.AppendLine($"rb.linearVelocity = {rb.linearVelocity}  speedKmh = {rb.linearVelocity.magnitude * 3.6f:F1}");
        sb.AppendLine($"rb.useGravity = {rb.useGravity}  isKinematic = {rb.isKinematic}");
        sb.AppendLine($"rb.centerOfMass(world) = {rb.worldCenterOfMass}  local = {rb.centerOfMass}");

        if (col != null)
        {
            var b = col.bounds;
            sb.AppendLine($"BoxCollider world bounds: min={b.min} max={b.max} (bottom y={b.min.y:F3})");
        }

        // 입력 상태
        var raw = car.RawSteerInput;
        var rawT = car.RawThrottleInput;
        var sm = car.SteerInput;
        var smT = car.ThrottleInput;
        sb.AppendLine($"input raw=({raw:F2},{rawT:F2})  smooth=({sm:F2},{smT:F2})");

        // isGrounded reflect
        var fld = typeof(ArcadeCarController).GetField("isGrounded", BindingFlags.NonPublic | BindingFlags.Instance);
        var grounded = (bool)fld.GetValue(car);
        sb.AppendLine($"isGrounded(field) = {grounded}");

        // 직접 raycast 시뮬
        var maskFld = typeof(ArcadeCarController).GetField("groundLayer", BindingFlags.NonPublic | BindingFlags.Instance);
        LayerMask mask = (LayerMask)maskFld.GetValue(car);
        var distFld = typeof(ArcadeCarController).GetField("groundCheckDistance", BindingFlags.NonPublic | BindingFlags.Instance);
        float dist = (float)distFld.GetValue(car);
        var hit = Physics.Raycast(t.position, Vector3.down, out var info, dist, mask);
        sb.AppendLine($"raycast from {t.position} down dist={dist} mask={(int)mask.value} → hit={hit}");
        if (hit) sb.AppendLine($"  hit.point={info.point} hit.collider={info.collider.name} dist={info.distance:F3}");

        // 더 긴 raycast로 실제 땅 어디
        var hit2 = Physics.Raycast(t.position, Vector3.down, out var info2, 20f);
        sb.AppendLine($"long raycast(20m, all): hit={hit2}");
        if (hit2) sb.AppendLine($"  hit.point={info2.point} hit.collider={info2.collider.name} dist={info2.distance:F3} layer={info2.collider.gameObject.layer}");

        var accelFld = typeof(ArcadeCarController).GetField("accelForce", BindingFlags.NonPublic | BindingFlags.Instance);
        var maxFld = typeof(ArcadeCarController).GetField("maxSpeedMps", BindingFlags.NonPublic | BindingFlags.Instance);
        sb.AppendLine($"accelForce = {accelFld.GetValue(car)}  maxSpeedMps = {maxFld.GetValue(car)}");

        Debug.Log(sb.ToString());
    }
}
