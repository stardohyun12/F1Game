using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class DeliveryPoint : MonoBehaviour
{
    [SerializeField] private string label;
    [SerializeField] private float radius = 3f;
    [SerializeField] private Color activeColor = new Color(0.2f, 1f, 0.4f, 0.6f);
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    public string Label => label;
    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }

    SphereCollider trigger;
    DeliveryManager manager;

    void Awake()
    {
        trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = radius;
    }

    public void Bind(DeliveryManager mgr, string lbl)
    {
        manager = mgr;
        label = lbl;
    }

    public void SetActive(bool v) { IsActive = v; }
    public void SetCompleted() { IsCompleted = true; IsActive = false; }

    void OnTriggerEnter(Collider other)
    {
        if (!IsActive || IsCompleted) return;
        if (other.attachedRigidbody == null) return;
        if (manager != null) manager.OnDeliveryReached(this);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsCompleted ? new Color(0.3f, 0.3f, 0.3f, 0.2f)
                     : IsActive ? activeColor
                     : inactiveColor;
        Gizmos.DrawSphere(transform.position + Vector3.up * 1f, radius);
    }
}
