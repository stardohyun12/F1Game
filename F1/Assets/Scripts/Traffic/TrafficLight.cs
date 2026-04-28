using System;
using UnityEngine;

/// <summary>
/// 교차로 신호등 (Day 25, 도현 #3 맵 단조 처치 — 게임플레이).
///
/// 설계
/// - 상태: Red(5s) → Yellow(1s) → Green(5s) → Red 사이클 (총 11s).
/// - 시각: 3개 자식 Renderer ("Red", "Yellow", "Green") emission 색상 토글.
/// - 위반 판정: TrafficLightArea (별도 컴포넌트 부착)이 enter event 발생.
/// - Awake 시 시각 자동 생성 (sphere 3개) — 디자이너가 별도 visual 부착 안 해도 동작.
///
/// Phase 1 단순화
/// - 1교차로 1신호등 (방위 구분 X). 사면 모두 동일 사이클 — 개발자/플레이 검증 우선.
/// - 다음 Phase: 직교 사이클(NS=Green ↔ EW=Red) + 차량 방향 검출.
/// </summary>
[DisallowMultipleComponent]
public class TrafficLight : MonoBehaviour
{
    public enum State { Red, Yellow, Green }

    [Header("Cycle Times (s)")]
    [SerializeField] private float redDuration = 5f;
    [SerializeField] private float yellowDuration = 1f;
    [SerializeField] private float greenDuration = 5f;

    [Header("Visual (Awake에 자동 생성)")]
    [Tooltip("자동 생성 시 신호등 sphere 반경.")]
    [SerializeField] private float lampRadius = 0.6f;
    [Tooltip("기둥 위 첫 lamp Y (위에서부터 Red/Yellow/Green).")]
    [SerializeField] private float lampTopY = 5.5f;
    [Tooltip("lamp 간 간격 Y.")]
    [SerializeField] private float lampSpacingY = 1.4f;

    [Header("Cycle 시작 offset (s)")]
    [Tooltip("0 = Red 시작. yellowDuration 미만이면 Red 진행 중. 직교 신호 시뮬용.")]
    [SerializeField] private float startOffsetSec = 0f;

    public State CurrentState { get; private set; }
    public float TimeInState { get; private set; }

    /// <summary>State 변경 시 발동. 새 state 인자.</summary>
    public event Action<State> OnStateChanged;

    Renderer redLamp, yellowLamp, greenLamp;
    State prevState;
    float cycleTime;

    void Awake()
    {
        EnsureVisual();
        cycleTime = startOffsetSec;
        UpdateState();
        prevState = CurrentState;
        ApplyVisual();
    }

    void Update()
    {
        cycleTime += Time.deltaTime;
        float total = redDuration + yellowDuration + greenDuration;
        if (cycleTime >= total) cycleTime -= total;

        UpdateState();

        if (CurrentState != prevState)
        {
            ApplyVisual();
            OnStateChanged?.Invoke(CurrentState);
            prevState = CurrentState;
        }
    }

    void UpdateState()
    {
        // 사이클 순서: Red(5) → Yellow(1) → Green(5).
        // Yellow는 Green 직후로 두는 표준 신호등 모델도 있지만 본 게임은 단순화.
        if (cycleTime < redDuration)
        {
            CurrentState = State.Red;
            TimeInState = cycleTime;
        }
        else if (cycleTime < redDuration + yellowDuration)
        {
            CurrentState = State.Yellow;
            TimeInState = cycleTime - redDuration;
        }
        else
        {
            CurrentState = State.Green;
            TimeInState = cycleTime - redDuration - yellowDuration;
        }
    }

    void EnsureVisual()
    {
        // 이미 자식 lamp 있으면 재사용
        var redT = transform.Find("RedLamp");
        var yellowT = transform.Find("YellowLamp");
        var greenT = transform.Find("GreenLamp");

        if (redT == null) redT = CreateLamp("RedLamp", lampTopY, Color.red);
        if (yellowT == null) yellowT = CreateLamp("YellowLamp", lampTopY - lampSpacingY, Color.yellow);
        if (greenT == null) greenT = CreateLamp("GreenLamp", lampTopY - lampSpacingY * 2f, Color.green);

        redLamp = redT.GetComponent<Renderer>();
        yellowLamp = yellowT.GetComponent<Renderer>();
        greenLamp = greenT.GetComponent<Renderer>();
    }

    Transform CreateLamp(string name, float y, Color baseColor)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, y, 0f);
        go.transform.localScale = Vector3.one * lampRadius * 2f;

        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var r = go.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = $"TrafficLight_{name}_Mat" };
        mat.color = baseColor * 0.25f;
        // URP Lit emission 활성화
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.black);
        r.sharedMaterial = mat;
        return go.transform;
    }

    void ApplyVisual()
    {
        SetLamp(redLamp, CurrentState == State.Red, Color.red);
        SetLamp(yellowLamp, CurrentState == State.Yellow, new Color(1f, 0.85f, 0.15f));
        SetLamp(greenLamp, CurrentState == State.Green, new Color(0.15f, 1f, 0.30f));
    }

    static void SetLamp(Renderer r, bool on, Color c)
    {
        if (r == null || r.sharedMaterial == null) return;
        var mat = r.sharedMaterial;
        if (on)
        {
            mat.color = c;
            mat.SetColor("_EmissionColor", c * 2.5f);
        }
        else
        {
            mat.color = c * 0.20f;
            mat.SetColor("_EmissionColor", Color.black);
        }
    }
}
