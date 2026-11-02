using UnityEngine;

/// <summary>
/// 걷는 사람 전용 보이지 않는 난간(20261102-2305 ①): 부두 끝 같은 열린 가장자리에서 바다로 떨어지지 않게 막는다.
/// 걷는 주인공 오브젝트가 켜져 있을 때만 자식 콜라이더가 켜진다 → 카트·차는 그대로 지나간다(눈에 안 보이고 차를 안 막음).
/// 자식 BoxCollider 는 BuildPierRails 가 만든다(에디터 메뉴: F1/부두 난간 만들기).
/// </summary>
public class WalkOnlyRail : MonoBehaviour
{
    Collider[] cols;
    OnFootPlayer player;
    bool on = true;

    void Awake()
    {
        cols = GetComponentsInChildren<Collider>(true);
        player = FindFirstObjectByType<OnFootPlayer>(FindObjectsInactive.Include);
    }

    void Update()
    {
        if (player == null) { player = FindFirstObjectByType<OnFootPlayer>(FindObjectsInactive.Include); if (player == null) return; }
        bool want = player.gameObject.activeInHierarchy;   // 대화(enabled=false)는 서 있는 것이라 켠 채로, 카트(오브젝트 꺼짐)는 끈다
        if (want == on) return;
        on = want;
        foreach (var c in cols) if (c != null) c.enabled = want;
    }
}
