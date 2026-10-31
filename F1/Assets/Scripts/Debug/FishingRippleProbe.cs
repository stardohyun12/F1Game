using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// QA 전용(D-235): 찌 ↔ 물 검산. Play 중 앉은 상태에서 Begin(dur, outDir, tag) — 화면에 올라가는 그대로(360 줄)를
///  · 매 프레임 깜빡임(FrameFlickerProbe 와 같은 정의: 밝기가 한 프레임 오르고 바로 내리는 화소, 차 > 24)
///  · 0.25 s 마다 PNG(연속 프레임)
///  · 0.25 s 마다 찌 높이 vs GPU 가 그린 수면 높이: SeaNear 를 DuskSea _Debug 5(wpos.y 출력)로 찌 바로 위에서 내려다본 직교 4×4 RT 에 그려 읽음
/// 결과 JSON. 게임 로직에는 안 낀다.
/// </summary>
public class FishingRippleProbe : MonoBehaviour
{
    public static string Summary = "";
    public static bool Done;
    float dur; string outDir, tag;
    Material dbg;

    public static void Begin(float dur, string outDir, string tag)
    {
        Done = false; Summary = "";
        var p = new GameObject("FishingRippleProbe").AddComponent<FishingRippleProbe>();
        p.dur = dur; p.outDir = outDir; p.tag = tag;
        p.StartCoroutine(p.Run());
    }

    /// <summary>GPU 가 그린 SeaNear 수면의 월드 y(찌 xz 바로 위). 실패하면 NaN. combo = 투영 보정·위 방향 조합 0..3.</summary>
    public float GpuSurfaceY(Renderer near, Vector3 at, int combo)
    {
        if (dbg == null) dbg = new Material(near.sharedMaterial) { hideFlags = HideFlags.HideAndDontSave };
        else dbg.CopyPropertiesFromMaterial(near.sharedMaterial);
        dbg.SetFloat("_Debug", 5f);
        var rt = RenderTexture.GetTemporary(4, 4, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        var eye = new Vector3(at.x, at.y + 20f, at.z);
        var view = Matrix4x4.TRS(eye, Quaternion.LookRotation(Vector3.down, (combo & 2) == 0 ? Vector3.forward : Vector3.back), new Vector3(1, 1, -1)).inverse;
        var proj = Matrix4x4.Ortho(-.01f, .01f, -.01f, .01f, 1f, 60f);
        if ((combo & 1) == 1) proj = GL.GetGPUProjectionMatrix(proj, true);
        var cb = new CommandBuffer { name = "FishingRippleProbe" };
        cb.SetRenderTarget(rt); cb.ClearRenderTarget(true, true, new Color(-999f, 0, 0, 0));
        cb.SetViewProjectionMatrices(view, proj);
        cb.DrawRenderer(near, dbg, 0, 0);
        Graphics.ExecuteCommandBuffer(cb); cb.Release();
        var tex = new Texture2D(4, 4, TextureFormat.RGBAFloat, false, true);
        var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 4, 4), 0, 0); tex.Apply(); RenderTexture.active = a;
        RenderTexture.ReleaseTemporary(rt);
        float s = 0; int n = 0;
        foreach (var c in tex.GetPixels()) if (c.r > -900f) { s += c.r; n++; }
        Destroy(tex);
        return n > 0 ? s / n : float.NaN;
    }

    IEnumerator Run()
    {
        System.IO.Directory.CreateDirectory(outDir);
        var cam = Camera.main; var lr = cam != null ? cam.GetComponent<LowResRenderTest>() : null;
        var spot = FindFirstObjectByType<FishingSpot>(); var look = SunsetLook.Find();
        int w = lr != null && lr.RenderSize.x > 0 ? lr.RenderSize.x : 640, h = lr != null && lr.RenderSize.y > 0 ? lr.RenderSize.y : 360;
        var rt = new RenderTexture(w, h, 0); var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        byte[] prev = null, prev2 = null; var cur = new byte[w * h];
        var flips = new List<float>(); var dts = new List<float>();
        var hs = new StringBuilder(); var diffs = new List<float>();
        float t0 = Time.time, nextShot = 0f; int frame = 0, shot = 0, combo = -1;
        while (Time.time - t0 < dur)
        {
            yield return new WaitForEndOfFrame();
            float el = Time.time - t0;
            if (lr != null && lr.enabled) lr.Present(rt);
            else { var o = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = o; }
            var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = a;
            var px = tex.GetPixels32();
            for (int i = 0; i < cur.Length; i++) cur[i] = (byte)((px[i].r * 54 + px[i].g * 183 + px[i].b * 19) >> 8);
            if (prev2 != null)
            {
                int flip = 0;
                for (int i = 0; i < cur.Length; i++)
                {
                    int d1 = cur[i] - prev[i], d0 = prev[i] - prev2[i];
                    if ((d0 > 24 && d1 < -24) || (d0 < -24 && d1 > 24)) flip++;
                }
                flips.Add(flip / (float)cur.Length);
            }
            if (prev != null) dts.Add(Time.unscaledDeltaTime);
            if (el >= nextShot)
            {
                nextShot += .25f;
                System.IO.File.WriteAllBytes(outDir + tag + "_" + shot.ToString("D3") + ".png", tex.EncodeToPNG()); shot++;
                if (spot != null && spot.Landed && look != null && look.seaNear != null && look.seaNear.enabled)
                {
                    var b = spot.BobberPosition;
                    float g = float.NaN;
                    if (combo < 0) { for (int c = 0; c < 4 && float.IsNaN(g); c++) { g = GpuSurfaceY(look.seaNear, b, c); if (!float.IsNaN(g)) combo = c; } }
                    else g = GpuSurfaceY(look.seaNear, b, combo);
                    float d = b.y - g;
                    if (!float.IsNaN(g)) diffs.Add(Mathf.Abs(d));
                    hs.Append(el.ToString("F2")).Append('\t').Append(b.x.ToString("F3")).Append('\t').Append(b.z.ToString("F3")).Append('\t').Append(b.y.ToString("F4"))
                      .Append('\t').Append(spot.SurfaceY.ToString("F4")).Append('\t').Append(g.ToString("F4")).Append('\t').Append(d.ToString("F4")).Append('\n');
                }
            }
            prev2 = prev; prev = (byte[])cur.Clone(); frame++;
        }
        rt.Release(); Destroy(rt); Destroy(tex);
        System.IO.File.WriteAllText(outDir + tag + "_height.tsv", "t\tx\tz\tbobberY\tcpuSurfY\tgpuSurfY\tbobber-gpu\n" + hs);
        flips.Sort(); dts.Sort(); diffs.Sort();
        float Med(List<float> l) => l.Count > 0 ? l[l.Count / 2] : -1f;
        float Max(List<float> l) => l.Count > 0 ? l[l.Count - 1] : -1f;
        float P(List<float> l, float f) => l.Count > 0 ? l[Mathf.Min(l.Count - 1, (int)(l.Count * f))] : -1f;
        var sb = new StringBuilder();
        sb.Append("{\"tag\":\"").Append(tag).Append("\",\"res\":\"").Append(w).Append('x').Append(h).Append("\",\"seconds\":").Append(dur.ToString("F1"))
          .Append(",\"frames\":").Append(frame).Append(",\"shots\":").Append(shot).Append(",\"gpuCombo\":").Append(combo)
          .Append(",\"heightSamples\":").Append(diffs.Count).Append(",\"absDiff_median_m\":").Append(Med(diffs).ToString("F4")).Append(",\"absDiff_max_m\":").Append(Max(diffs).ToString("F4"))
          .Append(",\"flipFrac_median\":").Append(Med(flips).ToString("F5")).Append(",\"flipFrac_p95\":").Append(P(flips, .95f).ToString("F5")).Append(",\"flipFrac_max\":").Append(Max(flips).ToString("F5"))
          .Append(",\"dt_median_ms\":").Append((Med(dts) * 1000f).ToString("F1")).Append('}');
        Summary = sb.ToString();
        System.IO.File.WriteAllText(outDir + tag + "_probe.json", Summary);
        if (dbg != null) DestroyImmediate(dbg);
        Done = true;
        Destroy(gameObject);
    }
}
