using UnityEngine;

/// <summary>
/// 과제 0226: 주민 모델(아트 people_*.fbx, Generic 리그 Stand/Walk/Sit) 을 사람 자리(Person)에 붙인 것.
///  - 상태: Stand / Walk / Sit — Animator int "State"(0/1/2). Pedestrian 이 걸음·멈춤·앉기를 넣는다.
///  - 걷기 속도: Walk 클립 발 접지 속도 ClipWalkSpeed(m/s, 실측) 로 나눠 Animator "WalkSpeed" — 발 미끄러짐 최소.
///  - 손짓: 옛 임시 상자의 빈 'ArmPivot' 회전을 오른 윗팔 본에 그대로 옮긴다(OpeningTutorial.Wave 는 그대로 ArmPivot 을 돌린다).
/// </summary>
public class Resident : MonoBehaviour
{
    // people_young_01 Walk(40f@30fps) 발 접지 구간 z 속도 평균 — L 0.759 · R 0.806 m/s (docs/qa/tmp/i0226-walk.cs)
    public const float ClipWalkSpeed = 0.78f;
    static readonly int StateId = Animator.StringToHash("State"), SpeedId = Animator.StringToHash("WalkSpeed");

    public Animator anim;
    public Transform armPivot, armBone;
    Quaternion armRest; bool hasRest;

    void Awake()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (armPivot == null) armPivot = transform.Find("ArmPivot");
        if (armBone == null) foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "UpperArm_R") { armBone = t; break; }
        if (anim != null) anim.SetFloat(SpeedId, 1f);
    }

    /// <summary>0 서기 · 1 걷기(speed m/s) · 2 앉기</summary>
    public void Set(int state, float speed = 0f)
    {
        if (anim == null) return;
        anim.SetInteger(StateId, state);
        if (state == 1) anim.SetFloat(SpeedId, Mathf.Clamp(speed / ClipWalkSpeed, 0.5f, 3f));
    }

    void LateUpdate()
    {
        if (armPivot == null || armBone == null) return;
        // ArmPivot 은 사람 기준 X 축(오른쪽)으로만 돈다 → 같은 각을 세계 오른쪽 축으로 본에 얹는다(본 축 이름·방향과 무관).
        armPivot.localRotation.ToAngleAxis(out float ang, out var axis);
        if (ang < 0.5f) return;
        if (ang > 180f) ang -= 360f;
        armBone.rotation = Quaternion.AngleAxis(ang * Mathf.Sign(axis.x), transform.right) * armBone.rotation;
    }
}
