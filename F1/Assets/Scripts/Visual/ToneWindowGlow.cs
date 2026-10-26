using UnityEngine;

// 2124(D-150 톤 구간): 창 불빛. 오후엔 어두운 유리, 노을·초저녁엔 따뜻한 불(PCLookScene 의 전역 _PCPool: 오후 0 · 노을 .7 · 초저녁 1 을 그대로 씀).
// 1 넘는 색은 Present.shader 블룸 문턱(.72)을 넘어 살짝 번진다.
[RequireComponent(typeof(Renderer))]
public class ToneWindowGlow : MonoBehaviour
{
    public Color off = new Color(0.30f, 0.34f, 0.40f);
    public Color on = new Color(1.55f, 1.18f, 0.66f);
    static readonly int PoolId = Shader.PropertyToID("_PCPool"), TintId = Shader.PropertyToID("_Tint");
    Renderer r; MaterialPropertyBlock mpb; float last = -1f;

    void OnEnable() { r = GetComponent<Renderer>(); mpb = new MaterialPropertyBlock(); last = -1f; }

    void Update()
    {
        // 매 프레임 다시 씀: PCLookScene.MarkRenderers 가 렌더러 MPB 를 새로 덮어쓸 수 있다(창 몇 장이라 비용 무시).
        float p = Mathf.Clamp01(Shader.GetGlobalFloat(PoolId));
        last = p;
        r.GetPropertyBlock(mpb); mpb.SetColor(TintId, Color.Lerp(off, on, Mathf.SmoothStep(0f, 1f, p / 0.7f))); r.SetPropertyBlock(mpb);
    }
}
