using UnityEngine;

/// <summary>
/// 과제 0404(맵 코멘트 03:44·03:46): A7 셰이더(PitCrew/SceneA7/Lit · Library/A7LitPoint)는 그늘 색·3단 문턱을 전역값
/// _A7Shade·_A7T·_A7Mid 에서 읽는다. 그 값을 넣는 건 A7LightToggles 뿐인데, 그건 해(A7_Sun)까지 새로 만들어 씬 빛을 바꾼다.
/// 해가 이미 있는 씬(Map_TownV6)에는 이것만 둔다 — 전역값 셋만 넣고 빛은 안 건드린다. 없으면 그늘 면이 검정(0,0,0)이 된다.
/// 기본값 = Scene_A7_Light 의 A7LightToggles 값과 같은 결과.
/// </summary>
[ExecuteAlways]
public sealed class A7ShadeGlobals : MonoBehaviour
{
    [Range(0.3f, 1f)] public float ambientAmount = 0.62f;
    public Color skyFogColor = new Color(0.8f, 0.87f, 0.93f);
    public Color shadeTeal = new Color(0.55f, 0.76f, 0.80f);
    [Range(0f, 1f)] public float tealAmount = 0.5f;
    [Range(0f, 1f)] public float brightAbove = 0.3f;
    [Range(-0.5f, 0.5f)] public float sideAbove = 0.02f;
    [Range(0f, 1f)] public float sideLevel = 0.6f;

    static readonly int ShadeId = Shader.PropertyToID("_A7Shade"), TId = Shader.PropertyToID("_A7T"), MidId = Shader.PropertyToID("_A7Mid");

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    public void Apply()
    {
        var shade = Color.Lerp(skyFogColor, shadeTeal, tealAmount) * Mathf.Max(0.3f, ambientAmount);
        Shader.SetGlobalColor(ShadeId, new Color(shade.r, shade.g, shade.b, 1f));
        Shader.SetGlobalVector(TId, new Vector4(brightAbove, sideAbove, 0f, 0f));
        Shader.SetGlobalFloat(MidId, sideLevel);
    }
}
