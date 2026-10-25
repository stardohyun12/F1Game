using System.Collections;
using UnityEngine;

/// <summary>
/// 임시(주문 배선 전): Map_BayLoop 에서 주문판을 건너뛰고 바로 주행 단계로 들어간다.
/// RunController 의 단계 전환이 private 이라 리플렉션으로 부른다 — 주문 배선이 들어오면 이 컴포넌트를 지운다.
/// </summary>
public class BayLoopDriveOnly : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null; yield return null;   // RunController.Start(Board) 다음
        var rc = FindFirstObjectByType<RunController>();
        if (rc == null) yield break;
        var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(RunController).GetMethod("SetStage", bf)?.Invoke(rc, new object[] { RunStage.Driving });
        typeof(RunController).GetMethod("StartFollowCameraAndCloseBoard", bf)?.Invoke(rc, null);
    }
}
