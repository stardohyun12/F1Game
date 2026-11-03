using UnityEngine;

/// <summary>
/// D-290 완충재 롤(뽁뽁이·종이). 시선 + E = 한 장 당겨 손에. 장 수가 줄면 롤이 가늘어진다(실물 수량).
/// </summary>
public class Roll : PackThing
{
    public PackFiller kind = PackFiller.Bubble;
    public float thick = 0.02f;
    public int full = 12;
    [Tooltip("감긴 몸(가늘어지는 부분).")]
    public Transform body;
    public int Left { get; private set; }
    Vector3 body0;

    void Awake() { Left = full; if (body != null) body0 = body.localScale; }

    public override void Use(PackTable2 t) => t.UseRoll(this);

    internal bool Take()
    {
        if (Left <= 0) return false;
        Left--;
        if (body != null) { float k = Mathf.Lerp(0.35f, 1f, (float)Left / Mathf.Max(1, full)); body.localScale = new Vector3(body0.x * k, body0.y, body0.z * k); }
        return true;
    }

    internal void Refill() { Left = full; if (body != null) body.localScale = body0; }
}
