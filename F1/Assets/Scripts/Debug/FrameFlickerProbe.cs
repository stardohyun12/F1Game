using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// QA 전용(D-227): 화면 깜빡임 측정. Play 중 Begin(dur, outDir, tag) — dur 초 동안 매 프레임, 화면에 올라가는 그대로(LowResRenderTest.Present, 360 줄)를 읽어
///  · diffP95 = 프레임 간 픽셀 밝기 차(0~255)의 95번째 값 (프레임마다 하나 → 중앙값·최댓값)
///  · flipFrac = 깜빡임: 밝기가 한 프레임 오르고 바로 다음 프레임 내리는(또는 반대) 픽셀 비율, 차 > 24 일 때만
/// 결과 JSON + 첫·가운데·끝 프레임 PNG. 게임 로직에는 안 낀다.
/// </summary>
public class FrameFlickerProbe : MonoBehaviour
{
    public static string Summary = "";
    public static bool Done;
    float dur; string outDir, tag;

    public static void Begin(float dur, string outDir, string tag)
    {
        Done = false; Summary = "";
        var p = new GameObject("FrameFlickerProbe").AddComponent<FrameFlickerProbe>();
        p.dur = dur; p.outDir = outDir; p.tag = tag;
        p.StartCoroutine(p.Run());
    }

    IEnumerator Run()
    {
        System.IO.Directory.CreateDirectory(outDir);
        var cam = Camera.main; var lr = cam != null ? cam.GetComponent<LowResRenderTest>() : null;
        int w = lr != null && lr.RenderSize.x > 0 ? lr.RenderSize.x : 640, h = lr != null && lr.RenderSize.y > 0 ? lr.RenderSize.y : 360;
        var rt = new RenderTexture(w, h, 0); var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        byte[] prev = null, prev2 = null; var cur = new byte[w * h];
        var p95s = new List<float>(); var flips = new List<float>(); var dts = new List<float>();
        var hist = new int[256];
        float t0 = Time.time; int frame = 0; int mid = -1;
        while (Time.time - t0 < dur)
        {
            yield return new WaitForEndOfFrame();
            if (lr != null && lr.enabled) lr.Present(rt);
            else { var o = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = o; }
            var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = a;
            var px = tex.GetPixels32();
            for (int i = 0; i < cur.Length; i++) cur[i] = (byte)((px[i].r * 54 + px[i].g * 183 + px[i].b * 19) >> 8);
            if (frame == 0) System.IO.File.WriteAllBytes(outDir + tag + "_f000.png", tex.EncodeToPNG());
            if (mid < 0 && Time.time - t0 > dur * 0.5f) { mid = frame; System.IO.File.WriteAllBytes(outDir + tag + "_fmid.png", tex.EncodeToPNG()); }
            if (prev != null)
            {
                System.Array.Clear(hist, 0, 256); int flip = 0;
                for (int i = 0; i < cur.Length; i++)
                {
                    int d1 = cur[i] - prev[i]; hist[d1 < 0 ? -d1 : d1]++;
                    if (prev2 != null) { int d0 = prev[i] - prev2[i]; if ((d0 > 24 && d1 < -24) || (d0 < -24 && d1 > 24)) flip++; }
                }
                int need = (int)(cur.Length * 0.95f), acc = 0, q = 0;
                for (; q < 256; q++) { acc += hist[q]; if (acc >= need) break; }
                p95s.Add(q); if (prev2 != null) flips.Add(flip / (float)cur.Length);
                dts.Add(Time.unscaledDeltaTime);
            }
            prev2 = prev; prev = (byte[])cur.Clone(); frame++;
        }
        System.IO.File.WriteAllBytes(outDir + tag + "_fend.png", tex.EncodeToPNG());
        rt.Release(); Destroy(rt); Destroy(tex);
        p95s.Sort(); flips.Sort(); dts.Sort();
        float Med(List<float> l) => l.Count > 0 ? l[l.Count / 2] : -1f;
        float Max(List<float> l) => l.Count > 0 ? l[l.Count - 1] : -1f;
        float P(List<float> l, float f) => l.Count > 0 ? l[Mathf.Min(l.Count - 1, (int)(l.Count * f))] : -1f;
        var sb = new StringBuilder();
        sb.Append("{\"tag\":\"").Append(tag).Append("\",\"res\":\"").Append(w).Append('x').Append(h).Append("\",\"seconds\":").Append(dur.ToString("F1"))
          .Append(",\"frames\":").Append(frame)
          .Append(",\"diffP95_median\":").Append(Med(p95s).ToString("F1")).Append(",\"diffP95_p95\":").Append(P(p95s, .95f).ToString("F1")).Append(",\"diffP95_max\":").Append(Max(p95s).ToString("F1"))
          .Append(",\"flipFrac_median\":").Append(Med(flips).ToString("F5")).Append(",\"flipFrac_p95\":").Append(P(flips, .95f).ToString("F5")).Append(",\"flipFrac_max\":").Append(Max(flips).ToString("F5"))
          .Append(",\"dt_median_ms\":").Append((Med(dts) * 1000f).ToString("F1")).Append('}');
        Summary = sb.ToString();
        System.IO.File.WriteAllText(outDir + tag + "_flicker.json", Summary);
        Done = true;
        Destroy(gameObject);
    }
}
