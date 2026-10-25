using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// QA 전용: 카메라 떨림 측정(과제 20261025-2338). Play 중 Begin() 으로 시작 —
/// delay 초 뒤부터 dur 초 동안 매 프레임 카메라·차를 적고, capAt 초에 연속 capN 프레임을 PNG 로 남긴다.
/// 떨림 지표 = "매끈한 움직임에서 벗어난 양": 프레임 간 변화량의 2차 차분(이번 변화 − 지난 변화).
///  - 카메라 회전(요·피치·롤, 도/프레임) · 카메라 위치(cm/프레임)
///  - 화면 속 차 위치(960×540 픽셀) — 도현이 보는 "차가 화면에서 떠는" 양 그대로
/// 게임 로직에는 안 낀다(실행 순서 맨 끝에서 읽기만).
/// </summary>
[DefaultExecutionOrder(32000)]
public class CamJitterProbe : MonoBehaviour
{
    public static CamJitterProbe I;
    public static string Summary = "";
    public static bool Done;

    float t0, delay, dur, capAt; int capN; string outDir, tag;
    Transform car; Camera cam; bool running;
    readonly List<float> T = new List<float>(), Dt = new List<float>(), Yaw = new List<float>(), Pitch = new List<float>(), Roll = new List<float>(), Spd = new List<float>();
    readonly List<Vector3> CamP = new List<Vector3>();
    readonly List<Vector2> CarPx = new List<Vector2>();

    public static void Begin(float delay, float dur, float capAt, int capN, string outDir, string tag)
    {
        if (I == null) I = new GameObject("CamJitterProbe").AddComponent<CamJitterProbe>();
        I.Setup(delay, dur, capAt, capN, outDir, tag);
    }

    void Setup(float d, float u, float c, int n, string o, string tg)
    {
        delay = d; dur = u; capAt = c; capN = n; outDir = o; tag = tg; t0 = Time.time; running = true; Done = false; Summary = "";
        T.Clear(); Dt.Clear(); Yaw.Clear(); Pitch.Clear(); Roll.Clear(); Spd.Clear(); CamP.Clear(); CarPx.Clear();
        var ac = FindFirstObjectByType<ArcadeCarController>(); car = ac != null ? ac.transform : null; cam = Camera.main;
        StopAllCoroutines(); if (capN > 0) StartCoroutine(Capture());
    }

    void LateUpdate()
    {
        if (!running || cam == null || car == null) return;
        float t = Time.time - t0;
        if (t < delay) return;
        if (t > delay + dur) { running = false; Finish(); return; }
        var e = cam.transform.eulerAngles;
        var vp = cam.WorldToViewportPoint(car.position + Vector3.up * 0.5f);
        T.Add(t - delay); Dt.Add(Time.deltaTime); Yaw.Add(e.y); Pitch.Add(e.x); Roll.Add(e.z); CamP.Add(cam.transform.position);
        CarPx.Add(new Vector2(vp.x * 960f, vp.y * 540f));
        var rb = car.GetComponent<Rigidbody>(); Spd.Add(rb ? rb.linearVelocity.magnitude : 0f);
    }

    IEnumerator Capture()
    {
        while (Time.time - t0 < delay + capAt) yield return null;
        System.IO.Directory.CreateDirectory(outDir);
        for (int k = 0; k < capN; k++)
        {
            yield return new WaitForEndOfFrame();
            var rt = new RenderTexture(960, 540, 24); var prev = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
            RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply(); RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(outDir, tag + "_f" + k + ".png"), tex.EncodeToPNG());
            Destroy(tex); rt.Release();
        }
    }

    static float P95(List<float> a) { if (a.Count == 0) return 0; var s = new List<float>(a); s.Sort(); return s[Mathf.Min(s.Count - 1, (int)(s.Count * 0.95f))]; }
    static float Mean(List<float> a) { float m = 0; foreach (var x in a) m += x; return a.Count > 0 ? m / a.Count : 0; }
    static float Max(List<float> a) { float m = 0; foreach (var x in a) m = Mathf.Max(m, x); return m; }

    void Finish()
    {
        var rj = new List<float>(); var pj = new List<float>(); var sj = new List<float>(); var sv = new List<float>();
        var sb = new StringBuilder("t,dt,speed,dYaw,dPitch,dRoll,rotJerkDeg,posJerkCm,carPxStep,carPxJerk\n");
        for (int i = 2; i < T.Count; i++)
        {
            float dy1 = Mathf.DeltaAngle(Yaw[i - 1], Yaw[i]), dy0 = Mathf.DeltaAngle(Yaw[i - 2], Yaw[i - 1]);
            float dp1 = Mathf.DeltaAngle(Pitch[i - 1], Pitch[i]), dp0 = Mathf.DeltaAngle(Pitch[i - 2], Pitch[i - 1]);
            float dr1 = Mathf.DeltaAngle(Roll[i - 1], Roll[i]), dr0 = Mathf.DeltaAngle(Roll[i - 2], Roll[i - 1]);
            float rot = new Vector3(dy1 - dy0, dp1 - dp0, dr1 - dr0).magnitude;
            float pos = ((CamP[i] - CamP[i - 1]) - (CamP[i - 1] - CamP[i - 2])).magnitude * 100f;
            var s1 = CarPx[i] - CarPx[i - 1]; var s0 = CarPx[i - 1] - CarPx[i - 2];
            float px = (s1 - s0).magnitude;
            rj.Add(rot); pj.Add(pos); sj.Add(px); sv.Add(s1.magnitude);
            sb.Append(T[i].ToString("F3")).Append(',').Append(Dt[i].ToString("F4")).Append(',').Append(Spd[i].ToString("F2")).Append(',')
              .Append(dy1.ToString("F4")).Append(',').Append(dp1.ToString("F4")).Append(',').Append(dr1.ToString("F4")).Append(',')
              .Append(rot.ToString("F4")).Append(',').Append(pos.ToString("F3")).Append(',').Append(s1.magnitude.ToString("F3")).Append(',').Append(px.ToString("F3")).Append('\n');
        }
        System.IO.Directory.CreateDirectory(outDir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, tag + "_frames.csv"), sb.ToString());
        Summary = tag + " frames=" + T.Count + " meanDt=" + (Mean(Dt) * 1000f).ToString("F1") + "ms meanSpeed=" + (Mean(Spd) * 3.6f).ToString("F0") + "km/h"
            + " | rotJerk(deg/f) mean " + Mean(rj).ToString("F3") + " p95 " + P95(rj).ToString("F3") + " max " + Max(rj).ToString("F2")
            + " | posJerk(cm/f) mean " + Mean(pj).ToString("F2") + " p95 " + P95(pj).ToString("F2")
            + " | carScreenJerk(px/f) mean " + Mean(sj).ToString("F2") + " p95 " + P95(sj).ToString("F2") + " max " + Max(sj).ToString("F1")
            + " | carScreenStep(px/f) mean " + Mean(sv).ToString("F2");
        Done = true;
    }
}
