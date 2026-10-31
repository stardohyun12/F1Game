using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// 검증 캡처(과제 20261101-0515): 화면에 올라가는 것과 같은 LowResRenderTest.Present 결과(도트 UI 포함 RT 그대로)를
/// interval 초마다 count 장 PNG 로 남긴다. Play 전용 — exec 에서 PresentGrab.Begin(...) 으로 부른다.
/// </summary>
public class PresentGrab : MonoBehaviour
{
    public static int Pending;
    public static string LastLog = "";

    /// <summary>afterFirst: 첫 장을 찍은 바로 뒤 부른다(예: 대화 시작 — 첫 장 = 시작 전 화면).</summary>
    public static string Begin(string dir, string prefix, int count, float interval, float delay = 0f, System.Action afterFirst = null)
    {
        var cam = Camera.main;
        var lr = cam != null ? cam.GetComponent<LowResRenderTest>() : null;
        if (lr == null || !lr.isActiveAndEnabled) return "no LowResRenderTest";
        Directory.CreateDirectory(dir);
        var g = new GameObject("PresentGrab_" + prefix).AddComponent<PresentGrab>();
        Pending++;
        g.afterFirst = afterFirst;
        g.StartCoroutine(g.Run(lr, dir, prefix, count, interval, delay));
        return "OK";
    }

    System.Action afterFirst;

    IEnumerator Run(LowResRenderTest lr, string dir, string prefix, int count, float interval, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        var sb = new System.Text.StringBuilder();
        var ui = FindFirstObjectByType<DotScreenUI>();
        float t0 = Time.unscaledTime;
        for (int i = 0; i < count; i++)
        {
            yield return new WaitForEndOfFrame();
            int w = lr.RenderSize.x, h = lr.RenderSize.y;
            var rt = RenderTexture.GetTemporary(w, h, 0);
            lr.Present(rt);
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var a = RenderTexture.active; RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = a;
            RenderTexture.ReleaseTemporary(rt);
            string name = count > 1 ? $"{prefix}_{i:00}.png" : prefix + ".png";
            File.WriteAllBytes(Path.Combine(dir, name), tex.EncodeToPNG());
            Destroy(tex);
            var cam = lr.GetComponent<Camera>();
            sb.Append($"{name} t={Time.unscaledTime - t0:F2}s yaw={cam.transform.eulerAngles.y:F1} pitch={cam.transform.eulerAngles.x:F1} ui='{(ui != null ? ui.Visible : "-")}'\n");
            if (i == 0 && afterFirst != null) afterFirst();
            if (i < count - 1 && interval > 0f) yield return new WaitForSecondsRealtime(interval);
        }
        File.AppendAllText(Path.Combine(dir, "grab_log.txt"), sb.ToString());
        LastLog = sb.ToString();
        Pending--;
        Destroy(gameObject);
    }
}
