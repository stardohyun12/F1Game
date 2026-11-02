#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// D-257 주행감 실측 — 같은 입력을 프리셋마다 넣고 숫자·연속 프레임을 남긴다.
/// 맵 위 높은 곳(y=400)에 임시 평지(턱 8cm·비탈 10°)를 깔아 길 모양과 무관하게 잰다. 끝나면 지우고 카트를 제자리로.
/// 사용(Play 중): DriveFeelProbe.Run("D:/.../drive-feel", 0|1|2, captureFrames) → Done/Result 를 폴링.
/// </summary>
public class DriveFeelProbe : MonoBehaviour
{
    public static bool Busy { get; private set; }
    public static string Result { get; private set; } = "";
    public static string Status { get; private set; } = "";

    string outDir; int preset; bool frames;
    ArcadeCarController car; Rigidbody rb; CartBodyMotion motion; CargoBox cargo;
    GameObject field; Camera sideCam; RenderTexture rt;
    readonly StringBuilder js = new StringBuilder();
    const float Y0 = 400f;

    public static string Run(string dir, int presetIdx, bool captureFrames)
    {
        if (Busy) return "busy";
        var go = new GameObject("DriveFeelProbe (dev)");
        var p = go.AddComponent<DriveFeelProbe>();
        p.outDir = dir; p.preset = presetIdx; p.frames = captureFrames;
        Busy = true; Result = ""; Status = "start";
        p.StartCoroutine(p.Main());
        return "started";
    }

    IEnumerator Main()
    {
        var d = Object.FindFirstObjectByType<OnFootDirector>();
        if (d != null && d.State != OnFootDirector.Mode.InCart) { DebugWindow.ToggleCart(); yield return new WaitForSeconds(0.5f); }
        car = d != null && d.Car != null ? d.Car : Object.FindFirstObjectByType<ArcadeCarController>();
        rb = car.GetComponent<Rigidbody>(); motion = car.GetComponent<CartBodyMotion>(); cargo = car.GetComponent<CargoBox>();
        var rc = Services.Run as RunController;
        if (rc != null && rc.CurrentStage != RunStage.Driving) rc.EnterFreeDrive();
        Vector3 homePos = rb.position; Quaternion homeRot = rb.rotation;
        car.SetFeelPreset((DriveFeelPreset)preset);
        BuildField();
        js.Append("{\"preset\":\"").Append(DriveFeel.Label(car.FeelPreset)).Append("\",\"mode\":\"").Append(car.Mode).Append("\"");

        // 1) 직진 가속 7초
        yield return Place(new Vector3(0, Y0, -250), 0);
        float t40 = -1, v5 = 0, vmax = 0, t = 0; ResetPeaks();
        car.SetMoveInput(new Vector2(0, 1));
        while (t < 7f) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; float k = Kmh(); if (t40 < 0 && k >= 40) t40 = t; if (t <= 5f) v5 = k; vmax = Mathf.Max(vmax, k); Sample(); }
        Put("accel", $"\"t0to40\":{t40:F2},\"kmhAt5s\":{v5:F1},\"kmhAt7s\":{vmax:F1}," + Peaks());

        // 2) 40km/h 에서 급제동
        yield return Place(new Vector3(0, Y0, -250), 0);
        yield return SpeedUp(40f);
        Vector3 p0 = rb.position; t = 0; ResetPeaks(); float startK = Kmh();
        car.SetMoveInput(new Vector2(0, -1));
        if (frames) StartCoroutine(Capture("brake", 24, sideCamOn: true));
        while (Kmh() > 0.5f && t < 8f) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; Sample(); }
        float brakeDist = Vector3.Distance(p0, rb.position);
        car.SetMoveInput(Vector2.zero);
        for (int i = 0; i < 40; i++) { yield return new WaitForFixedUpdate(); Sample(); }   // 멈춘 뒤 되흔들림까지
        Put("brake", $"\"fromKmh\":{startK:F1},\"distM\":{brakeDist:F2},\"timeS\":{t:F2}," + Peaks());

        // 3) 급조향 — 15·35 km/h 에서 핸들 끝까지 3초: 회전 반경 + 반응 시간
        foreach (float target in new[] { 15f, 35f })
        {
            yield return Place(new Vector3(-60, Y0, -200), 0);
            yield return SpeedUp(target);
            ResetPeaks(); t = 0; float tResp = -1; var rates = new List<float>(); float prevYaw = rb.rotation.eulerAngles.y;
            if (frames && target > 30f) StartCoroutine(Capture("turn35", 24, sideCamOn: false));
            float finalGuess = 0;
            while (t < 3f)
            {
                car.SetMoveInput(new Vector2(1, Hold(target)));
                yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime;
                float y = rb.rotation.eulerAngles.y; float rate = Mathf.DeltaAngle(prevYaw, y) / Time.fixedDeltaTime; prevYaw = y;
                rates.Add(rate); Sample();
            }
            int n = rates.Count; for (int i = n / 2; i < n; i++) finalGuess += rates[i]; finalGuess /= Mathf.Max(1, n - n / 2);
            for (int i = 0; i < n; i++) if (rates[i] >= finalGuess * 0.63f) { tResp = (i + 1) * Time.fixedDeltaTime; break; }
            float spd = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).magnitude;
            float radius = Mathf.Abs(finalGuess) > 0.1f ? spd / (Mathf.Abs(finalGuess) * Mathf.Deg2Rad) : -1;
            car.SetMoveInput(Vector2.zero);
            for (int i = 0; i < 40; i++) { yield return new WaitForFixedUpdate(); Sample(); }
            Put("turn" + target, $"\"kmh\":{spd * 3.6f:F1},\"yawDegS\":{finalGuess:F1},\"radiusM\":{radius:F1},\"resp63S\":{tResp:F2}," + Peaks());
        }

        // 4) 턱 8cm 넘기 (25km/h 유지)
        yield return Place(new Vector3(60, Y0, 105), 0);
        yield return SpeedUp(25f);
        while (rb.position.z < 140f) { car.SetMoveInput(new Vector2(0, Hold(25f))); yield return new WaitForFixedUpdate(); }
        ResetPeaks(); t = 0; int sa0 = car.StepAssists; float kIn = Kmh(), kMin = 99;
        if (frames) StartCoroutine(Capture("bump", 24, sideCamOn: true));
        while (rb.position.z < 150f + 12f && t < 8f) { car.SetMoveInput(new Vector2(0, Hold(25f))); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; Sample(); if (rb.position.z > 148f) kMin = Mathf.Min(kMin, Kmh()); }
        Put("bump8cm", $"\"kmhIn\":{kIn:F1},\"kmhMinAfter\":{kMin:F1},\"passed\":{(rb.position.z > 155f ? "true" : "false")},\"stepAssists\":{car.StepAssists - sa0}," + Peaks());

        // 5) 비탈 10° 오르기 — 정지 10m 전에서 풀 스로틀
        yield return Place(new Vector3(120, Y0, 230), 0);
        t = 0; float tTop = -1; car.SetMoveInput(new Vector2(0, 1)); ResetPeaks();
        float topZ = 240f + 30f * Mathf.Cos(10f * Mathf.Deg2Rad);
        while (t < 12f) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; Sample(); if (rb.position.z >= topZ) { tTop = t; break; } }
        Put("ramp10", $"\"timeToTopS\":{tTop:F2},\"kmhAtTop\":{Kmh():F1}," + Peaks());

        // 6) 핸드브레이크 — 40km/h 에서 조향 0.5 + 1초
        yield return Place(new Vector3(-120, Y0, -250), 0);
        yield return SpeedUp(40f);
        ResetPeaks(); t = 0; float yaw0 = rb.rotation.eulerAngles.y, slipMax = 0; float k0 = Kmh();
        typeof(ArcadeCarController).GetField("qaForceHandbrake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(car, true);
        while (t < 1f) { car.SetMoveInput(new Vector2(0.5f, 0)); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; Sample(); slipMax = Mathf.Max(slipMax, Slip()); }
        typeof(ArcadeCarController).GetField("qaForceHandbrake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(car, false);
        car.SetMoveInput(Vector2.zero);
        float yawTurn = Mathf.Abs(Mathf.DeltaAngle(yaw0, rb.rotation.eulerAngles.y));
        Put("handbrake", $"\"fromKmh\":{k0:F1},\"kmhAfter1s\":{Kmh():F1},\"yawDeg\":{yawTurn:F1},\"slipMaxDeg\":{slipMax:F1},\"drifting\":{(car.IsDrifting ? "true" : "false")}," + Peaks());

        // 7) 짐 무게 — 빈 차 / 달걀 6kg / 상자 45kg: 0→40 과 40→0 거리
        foreach (float kg in new[] { 0f, 6f, 45f })
        {
            car.LoadOverrideKg = kg;
            yield return Place(new Vector3(-180, Y0, -250), 0);
            t = 0; t40 = -1; car.SetMoveInput(new Vector2(0, 1)); ResetPeaks();
            while (t < 10f && t40 < 0) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; if (Kmh() >= 40) t40 = t; Sample(); }
            p0 = rb.position; car.SetMoveInput(new Vector2(0, -1)); t = 0;
            while (Kmh() > 0.5f && t < 8f) { yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; Sample(); }
            Put("load" + kg, $"\"t0to40\":{t40:F2},\"brakeDistM\":{Vector3.Distance(p0, rb.position):F2}," + Peaks());
        }
        car.LoadOverrideKg = -1f;
        car.SetMoveInput(Vector2.zero);

        js.Append("}");
        Result = js.ToString();
        System.IO.Directory.CreateDirectory(outDir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, $"probe_{"ABC"[preset]}.json"), Result);
        // 정리 — 카트 제자리
        yield return Place(homePos, homeRot.eulerAngles.y, raw: true);
        Destroy(field); if (sideCam != null) Destroy(sideCam.gameObject); if (rt != null) rt.Release();
        Status = "done"; Busy = false;
        Destroy(gameObject);
    }

    // ── 보조 ──
    float Kmh() => Vector3.Dot(rb.linearVelocity, car.transform.forward) * 3.6f;
    float Slip() { var h = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up); return h.magnitude < 1f ? 0f : Vector3.Angle(h, Vector3.ProjectOnPlane(car.transform.forward, Vector3.up)); }
    float Hold(float kmh) => Mathf.Clamp((kmh - Kmh()) * 0.25f, -0.3f, 1f);

    float pkLongG, pkLatG, pkPitch, pkRoll, pkBox, pkHeave;
    void ResetPeaks() { pkLongG = pkLatG = 0; motion?.ResetPeaks(); }
    void Sample()
    {
        if (cargo != null) { pkLongG = Mathf.Max(pkLongG, Mathf.Abs(cargo.SignedLongG)); pkLatG = Mathf.Max(pkLatG, Mathf.Abs(cargo.SignedLatG)); }
    }
    string Peaks()
    {
        float pp = motion != null ? motion.PeakBodyPitch : 0, pr = motion != null ? motion.PeakBodyRoll : 0, pb = motion != null ? motion.PeakBoxSway : 0, ph = motion != null ? motion.PeakHeave : 0;
        return $"\"bodyPitchMaxDeg\":{pp:F2},\"bodyRollMaxDeg\":{pr:F2},\"boxSwayMaxDeg\":{pb:F2},\"heaveMaxCm\":{ph * 100f:F2},\"cargoLongGMax\":{pkLongG:F3},\"cargoLatGMax\":{pkLatG:F3}";
    }
    void Put(string key, string body) { js.Append(",\"").Append(key).Append("\":{").Append(body).Append("}"); Status = key; }

    IEnumerator Place(Vector3 pos, float yaw, bool raw = false)
    {
        car.SetMoveInput(Vector2.zero);
        var rot = Quaternion.Euler(0, yaw, 0);
        if (!raw) pos.y += 0.3f;
        rb.position = pos; rb.rotation = rot; car.transform.SetPositionAndRotation(pos, rot);
        rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();
        for (int i = 0; i < 40; i++) { yield return new WaitForFixedUpdate(); rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); rb.angularVelocity = Vector3.zero; }
    }

    IEnumerator SpeedUp(float kmh)
    {
        float t = 0;
        while (Kmh() < kmh - 0.5f && t < 15f) { car.SetMoveInput(new Vector2(0, 1)); yield return new WaitForFixedUpdate(); t += Time.fixedDeltaTime; }
        for (int i = 0; i < 25; i++) { car.SetMoveInput(new Vector2(0, Hold(kmh))); yield return new WaitForFixedUpdate(); }
    }

    void BuildField()
    {
        field = new GameObject("DriveFeelProbeField (dev)");
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = "ground"; g.transform.SetParent(field.transform);
        g.transform.position = new Vector3(0, Y0 - 0.5f, 0); g.transform.localScale = new Vector3(600, 1, 700);
        var bump = GameObject.CreatePrimitive(PrimitiveType.Cube); bump.name = "bump8cm"; bump.transform.SetParent(field.transform);
        bump.transform.position = new Vector3(60, Y0 + 0.04f, 150); bump.transform.localScale = new Vector3(12, 0.08f, 0.6f);
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube); ramp.name = "ramp10"; ramp.transform.SetParent(field.transform);
        ramp.transform.rotation = Quaternion.Euler(-10, 0, 0);
        ramp.transform.position = new Vector3(120, Y0, 240) + ramp.transform.rotation * new Vector3(0, -0.5f, 15f);
        ramp.transform.localScale = new Vector3(10, 1, 30);
        foreach (var r in field.GetComponentsInChildren<Renderer>()) { r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); r.sharedMaterial.color = r.name == "ground" ? new Color(0.45f, 0.47f, 0.42f) : new Color(0.8f, 0.6f, 0.3f); }
    }

    IEnumerator Capture(string tag, int count, bool sideCamOn)
    {
        var main = Camera.main;
        if (rt == null) rt = new RenderTexture(480, 270, 24);
        Camera cam = main;
        if (sideCamOn)
        {
            if (sideCam == null) { sideCam = new GameObject("ProbeSideCam").AddComponent<Camera>(); sideCam.enabled = false; sideCam.fieldOfView = 40; }
            cam = sideCam;
        }
        var tex = new Texture2D(480, 270, TextureFormat.RGB24, false);
        string dir = System.IO.Path.Combine(outDir, "frames"); System.IO.Directory.CreateDirectory(dir);
        for (int i = 0; i < count; i++)
        {
            yield return new WaitForEndOfFrame();
            if (sideCamOn) { var c = car.transform; sideCam.transform.position = c.position + c.right * 4.5f + Vector3.up * 0.9f; sideCam.transform.LookAt(c.position + Vector3.up * 0.4f); }
            var prev = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
            var act = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 480, 270), 0, 0); tex.Apply(); RenderTexture.active = act;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"{"ABC"[preset]}_{tag}_{i:D2}.png"), tex.EncodeToPNG());
            float wait = 0.1f, e = 0; while (e < wait) { yield return null; e += Time.deltaTime; }
        }
        Destroy(tex);
    }
}
#endif
