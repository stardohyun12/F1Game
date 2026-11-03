using UnityEngine;

/// <summary>
/// D-290 받는 곳: 보관소 안 선반(회색 대역). 아침마다 오늘 물건(Parcel)이 제자리에 놓인다. 선반 자체는 E 대상이 아니다(물건만).
/// </summary>
public class ReceiveShelf : MonoBehaviour
{
    public Parcel[] today;
    Vector3[] lp; Quaternion[] lr; Transform[] par;

    void Awake()
    {
        int n = today != null ? today.Length : 0;
        lp = new Vector3[n]; lr = new Quaternion[n]; par = new Transform[n];
        for (int i = 0; i < n; i++) { par[i] = today[i].transform.parent; lp[i] = today[i].transform.localPosition; lr[i] = today[i].transform.localRotation; }
    }

    /// <summary>다음 날 아침: 물건을 선반 제자리로.</summary>
    public void ResetForNextDay()
    {
        for (int i = 0; i < lp.Length; i++)
        {
            var p = today[i]; if (p == null) continue;
            p.Body.isKinematic = true;
            p.transform.SetParent(par[i], false);
            p.transform.localPosition = lp[i]; p.transform.localRotation = lr[i];
            foreach (var c in p.GetComponentsInChildren<Collider>()) c.enabled = true;
            p.OnShelf = true; p.gameObject.SetActive(true);
        }
    }
}
