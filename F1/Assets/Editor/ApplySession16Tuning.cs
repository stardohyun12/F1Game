using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 세션 16 도현 피드백 일괄 적용 (코드 변경 → 기존 씬 인스턴스 반영).
///
/// Unity SerializeField 기본값을 코드에서 바꿔도 이미 씬에 저장된 GameObject는 옛날 값 유지.
/// 이 메뉴가 SerializedObject로 새 값 강제 + HUD 자식 삭제(다음 Play에서 새 fontSize로 재빌드).
///
/// 적용 대상
/// - ArcadeCarController: 컨트롤 #1-2 + 드리프트 #4
/// - AudioManager + EngineAudio: 오디오 #3-2
/// - 5 HUD: anchoredPos/size #2-2 + 자식 삭제(BuildIfNeeded 재실행)
/// - PlaceSuburbanProps: #3 시각
/// - PlaceTrafficLight: #3 게임플레이
///
/// 메뉴: PitCrew/Phase 1/Apply Session 16 Tuning
/// 주의: Play 모드에서 실행 X (Editor 메뉴는 Play 중 씬 변경 못 함).
/// </summary>
public static class ApplySession16Tuning
{
    [MenuItem("PitCrew/Phase 1/Apply Session 16 Tuning")]
    public static void Execute()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Session16] Play 모드에서 실행 불가. 먼저 ▶ 종료 → 메뉴 재실행.");
            EditorUtility.DisplayDialog("Session 16 Tuning",
                "Play 모드에서는 씬 변경 불가.\n▶ 종료 후 다시 실행하세요.", "확인");
            return;
        }

        Debug.Log("[Session16] === 세션 16 일괄 튜닝 적용 시작 ===");

        ApplyArcadeCarController();
        ApplyAudioManager();
        ApplyEngineAudio();
        ApplyHUD<SpeedometerHUD>(new Vector2(40, 280), new Vector2(320, 110), new[] { "BG", "Speed", "Unit" });
        ApplyHUD<IntegrityHUD>(new Vector2(40, 200), new Vector2(560, 80), new[] { "BG", "Fill", "Label" });
        ApplyHUD<FuelHUD>(new Vector2(40, 100), new Vector2(560, 80), new[] { "BG", "Fill", "Label" });
        ApplyHUD<GearHUD>(new Vector2(-40, 100), new Vector2(200, 200), new[] { "BG", "Title", "Gear" });
        ApplyHUD<CargoWarningHUD>(new Vector2(0, -120), new Vector2(400, 92), new[] { "BG", "Label" });

        // 시각·게임플레이 보강
        if (Object.FindFirstObjectByType<TrackLayout>() != null || AssetDatabase.FindAssets("t:TrackLayout").Length > 0)
        {
            PlaceSuburbanProps.Execute();
            PlaceTrafficLight.Execute();
        }

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Session16] === 일괄 튜닝 완료. Play로 검증. ===");

        EditorUtility.DisplayDialog("Session 16 Tuning Applied",
            "✓ ArcadeCarController (컨트롤 + 드리프트)\n" +
            "✓ AudioManager + EngineAudio (볼륨)\n" +
            "✓ 5 HUD (사이즈/폰트 — 자식 삭제 → 다음 Play에서 새 값으로 재빌드)\n" +
            "✓ Suburban Props 자동 배치\n" +
            "✓ TrafficLight 4-way 교차로 배치\n\n" +
            "▶ 눌러서 평가.", "확인");
    }

    static void ApplyArcadeCarController()
    {
        var ctrl = Object.FindFirstObjectByType<ArcadeCarController>();
        if (ctrl == null) { Debug.LogWarning("[Session16] ArcadeCarController 없음."); return; }
        var so = new SerializedObject(ctrl);
        Set(so, "maxSpeedKmh", 110f);              // #1-3: 130→110
        Set(so, "accelTime", 1.6f);                // #1-3: 1.0→1.6
        Set(so, "maxTurnDegPerSec", 135f);
        Set(so, "steerLag", 0.025f);
        Set(so, "throttleLag", 0.01f);
        // 드리프트 #4
        Set(so, "driftGripMultiplier", 0.05f);
        Set(so, "driftYawBoost", 1.25f);
        Set(so, "driftBrakeThrottleScale", 0.95f);
        Set(so, "driftLongitudinalDrag", 0.6f);
        Set(so, "driftMaxSpeedKmh", 85f);
        Set(so, "driftAngularDamping", 0.5f);
        Set(so, "driftEntryYawImpulse", 2.5f);
        Set(so, "driftYawTorque", 11f);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);
        Debug.Log("[Session16] ArcadeCarController 적용 — accel/lag/조향/드리프트.");
    }

    static void ApplyAudioManager()
    {
        var am = Object.FindFirstObjectByType<AudioManager>();
        if (am == null) { Debug.LogWarning("[Session16] AudioManager 없음."); return; }
        var so = new SerializedObject(am);
        Set(so, "musicVolume", 0.18f);
        Set(so, "sfxMasterVolume", 0.55f);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(am);
        Debug.Log("[Session16] AudioManager 적용 — music 0.18, sfxMaster 0.55.");
    }

    static void ApplyEngineAudio()
    {
        var ea = Object.FindFirstObjectByType<EngineAudio>();
        if (ea == null) { Debug.LogWarning("[Session16] EngineAudio 없음."); return; }
        var so = new SerializedObject(ea);
        Set(so, "idleVolume", 0.16f);
        Set(so, "maxVolume", 0.42f);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ea);
        Debug.Log("[Session16] EngineAudio 적용 — idle 0.16, max 0.42.");
    }

    static void ApplyHUD<T>(Vector2 anchoredPos, Vector2 size, string[] childrenToDelete) where T : MonoBehaviour
    {
        var hud = Object.FindFirstObjectByType<T>();
        if (hud == null) { Debug.LogWarning($"[Session16] {typeof(T).Name} 없음."); return; }
        var so = new SerializedObject(hud);
        Set(so, "anchoredPos", anchoredPos);
        Set(so, "size", size);
        so.ApplyModifiedProperties();

        // 자식 삭제 → 다음 Awake/BuildIfNeeded에서 새 fontSize로 재생성
        foreach (var name in childrenToDelete)
        {
            var t = hud.transform.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        EditorUtility.SetDirty(hud);
        Debug.Log($"[Session16] {typeof(T).Name} 적용 — pos {anchoredPos}, size {size}, 자식 {childrenToDelete.Length}개 삭제.");
    }

    static void Set(SerializedObject so, string field, float v)
    {
        var p = so.FindProperty(field);
        if (p != null) p.floatValue = v;
    }

    static void Set(SerializedObject so, string field, Vector2 v)
    {
        var p = so.FindProperty(field);
        if (p != null) p.vector2Value = v;
    }
}
