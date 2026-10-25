using UnityEngine;

/// <summary>
/// 운전 카메라 손맛 프리셋 (과제 20261023-1720, 리서치 1655 '비용 낮음' 손잡이).
/// FollowCamera 에 꽂으면 아래 값이 FollowCamera 자체 필드보다 우선한다. 비우면 예전 동작.
/// 메뉴 PitCrew/Camera/Preset … 으로 얌전·중간·F1 전환. 에셋은 Assets/Settings/Camera/.
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Camera Feel Preset", fileName = "CameraFeel")]
public class CameraFeelPreset : ScriptableObject
{
    [Header("FOV — 속도 연동")]
    public float baseFov = 56f;
    [Tooltip("최고속에서의 FOV. FollowCamera 의 시험 상한(75/85/100)이 켜져 있으면 그쪽이 이긴다.")]
    public float maxFov = 85f;
    [Tooltip("가로: 속도/최고속(0~1), 세로: base→max 진행(0~1). 아래로 오목하면 고속에서 몰아서 넓어진다.")]
    public AnimationCurve fovBySpeed = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public float fovLerpSpeed = 5f;

    [Header("높이·거리 (차 기준, 거리 = 높이 × 2)")]
    public float height = 2.5f;
    public float distance = 5f;
    public float lookHeight = 1.05f;
    [Tooltip("차 앞쪽 몇 m 를 바라보나. 0 = 차 자체(차가 화면 가운데). 키우면 차가 화면 아래로 가고 길·다음 교차로가 보인다.")]
    public float lookAhead = 0f;

    [Header("코너 롤")]
    [Range(0f, 8f)] public float maxRollDeg = 3.5f;
    [Tooltip("롤 중 조향 입력 몫(0~1). 나머지는 횡가속.")]
    [Range(0f, 1f)] public float rollSteerWeight = 0.4f;
    [Tooltip("이 횡가속(m/s²)에서 횡가속 몫이 꽉 찬다.")]
    public float rollFullLatAccel = 12f;
    public float rollMinSpeedNorm = 0.2f;
    public float rollLerpSpeed = 6f;

    [Header("고속 미세 흔들림 — 화면 픽셀 단위(960×540 렌더 기준)")]
    [Tooltip("흔들림 최대 폭(렌더 픽셀). 0 이면 끔.")]
    public int shakeMaxPx = 2;
    [Tooltip("한 칸 크기(렌더 픽셀). 흔들림은 이 단위로만 움직인다.")]
    public int shakeStepPx = 2;
    [Range(0f, 1f)] public float shakeMinSpeedNorm = 0.7f;
    [Tooltip("초당 위치를 새로 뽑는 횟수. 뽑은 사이엔 멈춰 있다(픽셀 게임식 덜컥).")]
    public float shakeHz = 14f;
}
