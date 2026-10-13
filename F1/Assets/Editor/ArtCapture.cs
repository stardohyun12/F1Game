using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 아트 스타일 비교용 결정적 캡처 (2026-10-12). Play 없이 Edit 모드에서 같은 자리·같은 렌즈로 찍는다.
///  - chase : 추적캠 자리 (차 뒤 6m, 높이 3m, FOV 50 — FollowCamera 기본값과 동일)
///  - town  : 차 위 3/4 부감 (디오라마 느낌이 읽히는 각도)
/// UI 캔버스는 안 찍힌다(별도 카메라 → RenderTexture). 후처리는 Volume 그대로 반영.
/// 호출: eval 로 ArtCapture.CaptureSet("D:/.../before") → before_chase.png, before_town.png
/// </summary>
public static class ArtCapture
{
    public static string CaptureSet(string prefix, int w = 1920, int h = 1080)
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) return "ERROR: ArcadeCarController 없음";
        var t = car.transform;
        var yawRot = Quaternion.Euler(0f, t.eulerAngles.y, 0f);

        var a = Capture(prefix + "_chase.png", w, h, 50f,
            t.position + yawRot * new Vector3(0f, 3f, -6f), t.position + Vector3.up * 1.2f);
        // 부감: 건물 지붕(≈12m)보다 충분히 위, 앞쪽 도로를 내려다본다
        var b = Capture(prefix + "_town.png", w, h, 38f,
            t.position + yawRot * new Vector3(8f, 30f, -26f), t.position + yawRot * new Vector3(0f, 0f, 14f));
        return a + "\n" + b;
    }

    /// <summary>차 자체 판정용 근접 3/4 뷰 두 장 (앞 3/4, 뒤 3/4). 차 크기(≈1.8m)에 맞춘 거리.</summary>
    public static string CaptureCar(string prefix, int w = 1920, int h = 1080)
    {
        var car = Object.FindFirstObjectByType<ArcadeCarController>();
        if (car == null) return "ERROR: ArcadeCarController 없음";
        var t = car.transform; var yaw = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
        var c = t.position + Vector3.up * 0.3f;
        var a = Capture(prefix + "_front34.png", w, h, 35f, c + yaw * new Vector3(-2.6f, 1.3f, 3.2f), c);
        var b = Capture(prefix + "_rear34.png", w, h, 35f, c + yaw * new Vector3(2.6f, 1.3f, -3.2f), c);
        return a + " / " + b;
    }

    /// <summary>임의 지점에서 추적캠 구도 — 구역별(공업/항구) 팔레트 확인용. yawDeg = 바라보는 방향.</summary>
    public static string CaptureChaseAt(string path, float x, float z, float yawDeg, int w = 1920, int h = 1080)
    {
        var yawRot = Quaternion.Euler(0f, yawDeg, 0f);
        var car = new Vector3(x, 0f, z);
        return Capture(path, w, h, 50f, car + yawRot * new Vector3(0f, 3f, -6f), car + Vector3.up * 1.2f + yawRot * Vector3.forward * 2f);
    }

    static string Capture(string path, int w, int h, float fov, Vector3 pos, Vector3 lookAt)
    {
        var main = Camera.main;
        var go = new GameObject("~ArtCaptureCam");
        go.hideFlags = HideFlags.HideAndDontSave;
        var cam = go.AddComponent<Camera>();
        if (main != null)
        {
            cam.CopyFrom(main);
            var srcData = main.GetUniversalAdditionalCameraData();
            var dstData = cam.GetUniversalAdditionalCameraData();
            if (srcData != null && dstData != null)
            {
                dstData.renderPostProcessing = true;
                dstData.antialiasing = srcData.antialiasing;
                dstData.renderShadows = true;
            }
        }
        else
        {
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }
        cam.fieldOfView = fov;
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation(lookAt - pos, Vector3.up);
        cam.targetTexture = null;

        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, tex.EncodeToPNG());

        cam.targetTexture = null;
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
        return "OK " + path;
    }

    [MenuItem("PitCrew/Art/Capture Chase+Town (Captures/art)")]
    public static void MenuCapture()
    {
        var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Captures", "art"));
        Debug.Log(CaptureSet(Path.Combine(dir, System.DateTime.Now.ToString("yyyyMMdd_HHmmss"))));
    }
}
