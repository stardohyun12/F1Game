using UnityEngine;

/// <summary>
/// D-290 포장 v3 손 그리기: 무언가를 들고 있는 동안(PackTable2.HeldNow) 화면 아래에 회색 손 두 개가 그것을 양옆에서 잡는다.
/// 대역(회색 상자) — 아트 아님. 손은 그림만: 충돌체 없음.
/// </summary>
public class PackHands : MonoBehaviour
{
    public Material skin;
    Transform l, r;

    Transform Make(string n)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n;
        Destroy(g.GetComponent<Collider>());
        if (skin != null) g.GetComponent<Renderer>().sharedMaterial = skin;
        g.transform.localScale = new Vector3(0.06f, 0.05f, 0.14f);
        g.SetActive(false);
        return g.transform;
    }

    void LateUpdate()
    {
        var held = PackTable2.HeldNow;
        var cam = Camera.main;
        bool on = held != null && cam != null && held.gameObject.activeInHierarchy;
        if (on && l == null) { l = Make("v3_HandL"); r = Make("v3_HandR"); }
        if (l == null) return;
        if (l.gameObject.activeSelf != on) { l.gameObject.SetActive(on); r.gameObject.SetActive(on); }
        if (!on) return;
        // 든 것의 렌더러 경계 양옆(카메라 기준 좌우)에 손을 붙인다.
        var rs = held.GetComponentsInChildren<Renderer>();
        Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(held.position, Vector3.one * 0.1f);
        foreach (var x in rs) b.Encapsulate(x.bounds);
        var ct = cam.transform;
        float half = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(ct.right.x), Mathf.Abs(ct.right.y), Mathf.Abs(ct.right.z))));
        Vector3 c = b.center - ct.up * b.extents.y * 0.3f;
        l.SetPositionAndRotation(c - ct.right * (half + 0.025f), ct.rotation);
        r.SetPositionAndRotation(c + ct.right * (half + 0.025f), ct.rotation);
    }
}
