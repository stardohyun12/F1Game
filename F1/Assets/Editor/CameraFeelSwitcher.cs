using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 운전 카메라 프리셋 전환 (과제 20261023-1720). 메뉴 PitCrew/Camera/Preset …
/// 에디트 모드: 씬의 FollowCamera.preset 에 꽂고 씬 저장(ComplexTrack 씬 오버라이드가 곧 기본값).
/// Play 중: 런타임에만 꽂는다(Play 를 멈추면 씬 값으로 돌아감).
/// 값은 BuildPresets() 한 곳 — 전부 가설, 도현 플레이 판정 대상.
/// </summary>
public static class CameraFeelSwitcher
{
    const string Dir = "Assets/Settings/Camera";
    public const string Calm = Dir + "/CameraFeel_1_Calm.asset";
    public const string Mid = Dir + "/CameraFeel_2_Mid.asset";
    public const string F1 = Dir + "/CameraFeel_3_F1.asset";
    public const string Postcard = Dir + "/CameraFeel_4_Postcard.asset";
    [MenuItem("PitCrew/Camera/Preset 원화 (02_warm_street 구도)")] static void UsePostcard() => Use(Postcard);

    [MenuItem("PitCrew/Camera/Preset 얌전")] static void UseCalm() => Use(Calm);
    [MenuItem("PitCrew/Camera/Preset 중간 (기본)")] static void UseMid() => Use(Mid);
    [MenuItem("PitCrew/Camera/Preset F1")] static void UseF1() => Use(F1);
    [MenuItem("PitCrew/Camera/프리셋 끄기 (예전 FollowCamera 값)")] static void UseNone() => Use(null);
    [MenuItem("PitCrew/Camera/프리셋 에셋 다시 만들기")] static void Rebuild() => BuildPresets(true);

    public static void Use(string path)
    {
        BuildPresets(false);
        var preset = path != null ? AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(path) : null;
        var fc = Object.FindFirstObjectByType<FollowCamera>(FindObjectsInactive.Include);
        if (fc == null) { Debug.LogWarning("[CameraFeel] 씬에 FollowCamera 없음"); return; }
        if (Application.isPlaying) { fc.Preset = preset; }
        else
        {
            var so = new SerializedObject(fc);
            so.FindProperty("preset").objectReferenceValue = preset;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(fc.gameObject.scene);
            EditorSceneManager.SaveScene(fc.gameObject.scene);
        }
        Debug.Log("[CameraFeel] preset = " + (preset ? preset.name : "none") + (Application.isPlaying ? " (Play 중, 저장 안 함)" : " (씬 저장)"));
    }

    public static void BuildPresets(bool force)
    {
        if (!force && File.Exists(Calm) && File.Exists(Mid) && File.Exists(F1) && File.Exists(Postcard)) return;
        Directory.CreateDirectory(Dir);
        // 얌전: 지금 씬(높이 2.1·거리 5·FOV 56→72)보다 살짝 높고 멀다. 흔들림 없음.
        Make(Calm, 56f, 75f, Linear(), 3.0f, 6.0f, 2f, 0.5f, 0, 0.8f, 12f);
        // 중간: 고속에서 몰아서 넓어지는 곡선(x^1.5). 롤 3.5°, 2px 흔들림.
        Make(Mid, 56f, 85f, Pow(1.5f), 2.5f, 5.0f, 3.5f, 0.4f, 2, 0.7f, 14f);
        // F1: 낮고 가깝게, 저속은 좁고 최고속에서 100(x^2). 롤 5°, 4px(=2칸) 흔들림.
        Make(F1, 54f, 100f, Pow(2f), 2.0f, 4.0f, 5f, 0.3f, 4, 0.6f, 18f);
        // 원화(02_warm_street, 과제 1733): 차가 화면 아래쪽 ~16%, 앞길과 다음 커브가 보이게 멀리·앞을 본다. FOV 변화는 작게.
        Make(Postcard, 50f, 62f, Pow(1.5f), 1.9f, 4.2f, 2.5f, 0.4f, 2, 0.75f, 12f, 1.0f, 8f); // 1차 캡처(높이 2.3·거리 7)에서 차 폭 9% → 거리 4.2 로 ~16%
        AssetDatabase.SaveAssets();
    }

    static void Make(string path, float fov0, float fov1, AnimationCurve curve, float h, float d, float roll, float steerW, int shakePx, float shakeMin, float hz, float lookH = 1.05f, float lookAhead = 0f)
    {
        var p = AssetDatabase.LoadAssetAtPath<CameraFeelPreset>(path);
        if (p == null) { p = ScriptableObject.CreateInstance<CameraFeelPreset>(); AssetDatabase.CreateAsset(p, path); }
        p.baseFov = fov0; p.maxFov = fov1; p.fovBySpeed = curve; p.fovLerpSpeed = 5f;
        p.height = h; p.distance = d; p.lookHeight = lookH; p.lookAhead = lookAhead;
        p.maxRollDeg = roll; p.rollSteerWeight = steerW; p.rollFullLatAccel = 12f; p.rollMinSpeedNorm = 0.2f; p.rollLerpSpeed = 6f;
        p.shakeMaxPx = shakePx; p.shakeStepPx = 2; p.shakeMinSpeedNorm = shakeMin; p.shakeHz = hz;
        EditorUtility.SetDirty(p);
    }

    static AnimationCurve Linear() => AnimationCurve.Linear(0f, 0f, 1f, 1f);

    /// <summary>y = x^n 을 키 3개로. 접선은 도함수 n·x^(n-1).</summary>
    static AnimationCurve Pow(float n) => new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.5f, Mathf.Pow(0.5f, n), n * Mathf.Pow(0.5f, n - 1f), n * Mathf.Pow(0.5f, n - 1f)),
        new Keyframe(1f, 1f, n, n));
}
