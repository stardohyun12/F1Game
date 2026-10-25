using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 한 "무드"(낮/황혼/야간 등)의 라이팅·분위기 데이터 한 덩어리.
/// 코드 0으로 무드 추가 = 이 SO 복제 후 값만 변경. MoodController가 씬에 적용.
/// 폴리시 아트디렉션의 단일 소스 (2026-05-20 game-craft).
/// </summary>
[CreateAssetMenu(fileName = "Mood_", menuName = "PitCrew/Mood Profile")]
public sealed class MoodProfile : ScriptableObject
{
    [Header("태양 (Directional Light)")]
    public Vector3 sunRotationEuler = new Vector3(48f, -35f, 0f);
    public Color sunColor = new Color(1f, 0.95f, 0.84f);
    [Min(0f)] public float sunIntensity = 1.5f;
    public bool castShadows = true;
    [Range(0f, 1f)] public float shadowStrength = 0.9f;

    [Header("앰비언트")]
    public AmbientMode ambientMode = AmbientMode.Flat;
    public Color ambientColor = new Color(0.40f, 0.44f, 0.52f);
    [Min(0f)] public float ambientIntensity = 0.55f; // Skybox 모드일 때만 사용

    [Header("Fog")]
    public bool fog = true;
    public FogMode fogMode = FogMode.ExponentialSquared;
    public Color fogColor = new Color(0.74f, 0.82f, 0.9f);
    [Min(0f)] public float fogDensity = 0.0035f;

    [Header("선택: 스카이박스 / 포스트 프로파일 (null이면 현재 유지)")]
    public Material skybox;
    public VolumeProfile postProfile;

    // D-123 규칙 11 "조명·안개 한 규칙": 월드 셰이더(LofiSurface·WorldSurface·Asphalt)는 RenderSettings 를 안 읽고
    // 머티리얼마다 _Ambient/_FogColor/_FogStart/_FogEnd 를 따로 갖는다. 켜면 전부 이 무드의 안개색·거리로 맞춘다.
    [Header("규칙 11 — 월드 머티리얼 동기화 (끄면 예전 동작)")]
    public bool syncWorld = false;
    [Min(0f)] public float fogStart = 75f;
    [Min(0f)] public float fogEnd = 240f;
    [Tooltip("월드 셰이더 _Ambient = fogColor × 이 값 (셰이더가 0.36 을 곱하므로 1 근처)")]
    [Min(0f)] public float worldAmbientScale = 1.1f;
    [Tooltip("노란 가드레일(BoundaryRail) _Tint. a=0 이면 안 바꿈")]
    public Color railTint = new Color(0, 0, 0, 0);
    [Tooltip("바다(Sea) _Tint. a=0 이면 안 바꿈")]
    public Color seaTint = new Color(0, 0, 0, 0);

    [Header("D-137 초저녁 — 창에 불")]
    [Tooltip("씬의 WindowGlow(창 위치에 붙인 발광 조각 묶음)를 켠다")]
    public bool windowsLit = false;
}
