using UnityEngine;

/// <summary>
/// D-257 카트 주행감 비교판 — 프리셋 3개를 디버그 창 버튼으로 바꿔 탄다.
/// A = 지금(씬에 저장된 값 그대로 — Awake 때 찍어 둔 것으로 되돌린다)
/// B = 부드러운 배달(입력 늦게·가속 느긋·느릴 땐 잘 돌고 빠르면 넓게·드리프트 끔·차체 출렁임)
/// C = B + 무게감(바퀴 서스펜션·화물 무게 크게·출렁임 크게)
/// 기본은 B. 되돌리려면 ArcadeCarController 인스펙터의 Feel Preset 을 A 로(또는 디버그 창 A 버튼).
/// </summary>
public enum DriveFeelPreset { A_Now, B_Delivery, C_Heavy }

[System.Serializable]
public struct DriveFeelValues
{
    // 차 (ArcadeCarController 필드와 같은 뜻)
    public float maxSpeedKmh, accelTime, brakeDecel, steerLag, throttleLag, maxTurnDegPerSec;
    public float sideGripDamping, downforceGripGain, downforceAccel, coastDrag;
    public ArcadeCarController.DriveMode driveMode;
    public float wheelSpring, wheelDamper;
    public bool driftEnabled;
    /// <summary>회전 반경 하한(m) — 회전 속도 ≤ 속도/반경. 0 = 끔(속도와 무관하게 maxTurnDegPerSec 까지).</summary>
    public float minTurnRadius;
    /// <summary>최고속에 가까울수록 미는 힘이 이만큼 준다(0~1). 0 = 끝까지 같은 힘(예전).</summary>
    public float accelFalloff;
    /// <summary>바닥 마찰 보정(m/s²) — 페달을 밟는 동안 차가 바닥에서 받는 구름 저항만큼을 (1−페달) 몫으로 보탠다. 풀 페달 가속은 그대로, 반 페달도 "반만큼 순가속"이 나온다. 0 = 끔(예전).</summary>
    public float rollAssist;
    /// <summary>오르막 보정(0~1) — 같은 방식으로 비탈이 끌어내리는 힘(중력의 진행방향 성분)을 (1−페달) 몫으로 상쇄. 0 = 끔(예전).</summary>
    public float slopeAssist;
    /// <summary>조향을 놓을 때(중앙 복귀) 지연. 0 = steerLag 와 같게.</summary>
    public float steerReturnLag;
    /// <summary>화물 무게가 가속·제동·출렁임에 먹는 정도(0 = 무시, 1 = 그대로).</summary>
    public float weightInfluence;
    /// <summary>후진 최고속 = 이 비율 × 전진 최고속(D-284). 0 = 차의 reverseMaxKmh 필드 그대로(예전).</summary>
    public float reverseMaxRatio;
    /// <summary>후진 가속 = 전진 엔진 힘 × 이 값(D-284). 0 = 예전 0.5.</summary>
    public float reverseAccelMul;
    // 차체·짐칸 출렁임 (CartBodyMotion). bodyPitchPerG 0 = 끔.
    public float bodyPitchPerG, bodyRollPerG, bodyMaxDeg, bodyFreq, bodyDampRatio, bodyHeaveGain;
    public float boxSwayPerG, boxMaxDeg, boxFreq, boxDampRatio;
    // 카메라 (FollowCamera) — 0 이면 씬 값 그대로
    public float camPosSmooth, camYawSmooth, camHeightSmooth;
    // 엔진 소리 (EngineAudio) — 스로틀·오르막 힘쓰는 몫. 0 = 예전(속도만)
    public float engineLoadPitch, engineLoadVolume, engineSlopeLoad;
}

public static class DriveFeel
{
    /// <summary>화물 종류별 무게(kg, 차 기준 질량 100 에 더한다). 빈 차 = 런 밖.</summary>
    public static float CargoKg(CargoType t)
    {
        switch (t)
        {
            case CargoType.Box: return 45f;    // 무거운 상자
            case CargoType.Eggs: return 6f;    // 달걀 한 판
            case CargoType.Glass: return 15f;
            case CargoType.Fish: return 25f;   // 물 든 수조
            case CargoType.Cake: return 4f;
            default: return 10f;
        }
    }

    public const float CarRefKg = 100f;

    /// <summary>B = 부드러운 배달. 숫자는 가설 — 도현이 타 보고 고른다.</summary>
    public static DriveFeelValues B => new DriveFeelValues
    {
        maxSpeedKmh = 72f,          // 96 → 72: 마을 골목·비탈에 맞춤
        // 차 상자 마찰(CarLowFriction 0.15 ≈ 1.5 m/s²)·공기 저항이 늘 빠진다 — 3.6·0.45 로는 0→40 이 9.6초였다(1차 실측).
        // D-284 두 번째 한 판 메모 3 "조작이 조금 무거운 것 같음" — 아래 값은 전부 가설(도현이 타 보고 고른다). 이전 값은 보고서 표.
        accelTime = 2.6f,           // 2.9 → 2.6: 엔진 힘 = 100kg × 20m/s ÷ 2.6 (A 는 ÷ 2.8 에 최고속 26.7)
        accelFalloff = 0.3f,        // 끝으로 갈수록 힘이 빠져 최고속엔 천천히 붙는다
        // 반 페달 봇이 오르막에서 33곳 섰다(D-257 4번 이슈). 실측: 45kg 상자 + 페달 0.5 는 평지에서도 0m — 엔진 힘(2.4 m/s²)이 바닥 마찰(≈2.6)과 같다.
        rollAssist = 2.6f,
        slopeAssist = 1f,
        brakeDecel = 9f,            // 13 → 9: 제동 거리 조금 길게
        steerLag = 0.10f,           // 0.16 → 0.10 (D-284): 핸들이 손을 따라오는 데 걸리는 시간
        steerReturnLag = 0.08f,     // 0.12 → 0.08
        throttleLag = 0.12f,        // 0.18 → 0.12: 발이 페달을 따라오는 시간
        reverseMaxRatio = 0.7f,     // 후진 최고속 = 전진의 70% (이전: 차 필드 20~25 km/h = 전진의 28~35%)
        reverseAccelMul = 1.0f,     // 후진 가속 힘 0.5 → 1.0 (2배)
        maxTurnDegPerSec = 110f,
        // 저속에서 반경 하한 — 4.5 는 1m/s 에서 초당 13°로 묶여 좁은 길·출발에서 봇이 29곳 끼었다(2차). A 는 15km/h 에서 1.8m.
        minTurnRadius = 2.5f,
        sideGripDamping = 8f,
        downforceGripGain = 0f, downforceAccel = 0f,   // F1 다운포스 끔
        coastDrag = 0.6f,
        driveMode = ArcadeCarController.DriveMode.Box,
        wheelSpring = 5000f, wheelDamper = 560f,
        driftEnabled = false,       // 핸드브레이크 = 짧은 미끄러짐만
        weightInfluence = 0.6f,
        bodyPitchPerG = 7f, bodyRollPerG = 9f, bodyMaxDeg = 5f, bodyFreq = 2.2f, bodyDampRatio = 0.45f, bodyHeaveGain = 0.012f,
        boxSwayPerG = 6f, boxMaxDeg = 6f, boxFreq = 3.2f, boxDampRatio = 0.3f,
        camPosSmooth = 0.12f, camYawSmooth = 0.22f, camHeightSmooth = 0.22f,
        engineLoadPitch = 0.25f, engineLoadVolume = 0.12f, engineSlopeLoad = 0.04f,
    };

    /// <summary>C = B + 무게감·서스펜션 강하게.</summary>
    public static DriveFeelValues C
    {
        get
        {
            var v = B;
            v.maxSpeedKmh = 66f;
            // 엔진은 B 와 같게 — 4.2 는 10° 비탈을 못 올랐고(1차), 3.3 도 0→40 7.8초(2차)로 너무 굼떴다.
            // 무게감은 최고속·핸들·발 지연·짐 영향(weightInfluence)으로 낸다.
            v.accelTime = 2.9f;
            v.brakeDecel = 8f;
            v.reverseMaxRatio = 0f; v.reverseAccelMul = 0f;   // C 는 예전 후진 그대로(D-284 는 B 만)
            v.steerLag = 0.22f;
            v.steerReturnLag = 0.16f;
            v.throttleLag = 0.25f;
            v.maxTurnDegPerSec = 100f;
            v.minTurnRadius = 3f;
            // Wheels(바퀴 4개 스프링) 검토 결과 1차 실측: 40→0 제동 2.6m(A 3.9m 보다 급정거)·10° 비탈 못 오름·턱 측정 실패
            // → Box 유지. 출렁임은 CartBodyMotion(그림만)이 맡는다. 바꿔 보려면 driveMode 만 Wheels 로.
            v.driveMode = ArcadeCarController.DriveMode.Box;
            v.wheelSpring = 3800f; v.wheelDamper = 380f;
            v.weightInfluence = 0.8f;   // 45kg 상자: 힘 100/(100+36) = 0.74배
            v.bodyPitchPerG = 10f; v.bodyRollPerG = 12f; v.bodyMaxDeg = 7f; v.bodyFreq = 1.8f; v.bodyDampRatio = 0.35f; v.bodyHeaveGain = 0.02f;
            v.boxSwayPerG = 9f; v.boxMaxDeg = 9f; v.boxFreq = 2.6f; v.boxDampRatio = 0.22f;
            v.camPosSmooth = 0.15f; v.camYawSmooth = 0.26f; v.camHeightSmooth = 0.3f;
            v.engineLoadPitch = 0.3f; v.engineLoadVolume = 0.15f; v.engineSlopeLoad = 0.06f;
            return v;
        }
    }

    public static string Label(DriveFeelPreset p) =>
        p == DriveFeelPreset.A_Now ? "A 지금" : p == DriveFeelPreset.B_Delivery ? "B 부드러운 배달" : "C 무게감";
}
