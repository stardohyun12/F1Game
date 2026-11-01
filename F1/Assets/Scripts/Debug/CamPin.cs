using UnityEngine;

/// <summary>
/// QA 전용(과제 20261102-0110 하늘 전/후): 메인 카메라 자세를 고정한다 — 다른 카메라 스크립트가 다 움직인 뒤(LateUpdate 맨 끝) 덮어쓴다.
/// Play 중 exec 에서 CamPin.Pin(위치, 오일러) / CamPin.Unpin(). 게임 로직에는 안 낀다.
/// </summary>
[DefaultExecutionOrder(32000)]
public class CamPin : MonoBehaviour
{
    public Vector3 pos, euler;

    public static string Pin(Vector3 pos, Vector3 euler)
    {
        var cam = Camera.main; if (cam == null) return "no cam";
        var p = cam.GetComponent<CamPin>(); if (p == null) p = cam.gameObject.AddComponent<CamPin>();
        p.pos = pos; p.euler = euler; p.enabled = true; p.LateUpdate();
        return "pinned " + pos.ToString("F1") + " " + euler.ToString("F1");
    }

    public static void Unpin() { var cam = Camera.main; var p = cam != null ? cam.GetComponent<CamPin>() : null; if (p != null) Destroy(p); }

    void LateUpdate() { transform.SetPositionAndRotation(pos, Quaternion.Euler(euler)); }
}
