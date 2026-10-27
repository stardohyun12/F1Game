using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 과제 1726 장면 A6d: 채도 깎기 대신 미세 노이즈 — Play 중 인스펙터에서 비교.
///  (a) surfaceGrain: 표면 고정 노이즈. 같은 오브젝트의 SceneA3Toggles.materials(씬 전용 사본)에 _SN·_SNDens 를 넣는다. 물체에 붙어 있어 움직여도 안 지글거림.
///  (b) screenGrain: 화면 그레인. 정적 노이즈(세로 720 기준 1텍셀)를 화면 위에 곱한다. 게임 화면(LowRes 캔버스 -32000) 바로 위·게임 UI 아래.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SceneA3Toggles))]
public sealed class A6dGrainToggles : MonoBehaviour
{
    [Header("(a) 표면 고정 노이즈 — 물체 표면 칸마다 명도 ±")]
    public bool surfaceGrain = true;
    [Range(0f, 0.15f)] public float surfaceAmount = 0.04f;
    [Tooltip("1m 당 칸 수(8 = 집 텍스처 8텍셀/m 와 같은 크기)")]
    public float surfaceCellsPerM = 8f;
    [Header("(b) 화면 그레인 — 정적, 세로 720 기준 1텍셀")]
    public bool screenGrain = false;
    [Range(0f, 0.15f)] public float screenAmount = 0.03f;
    public Shader grainShader;

    Material grainMat; Texture2D noise; GameObject canvasGO;
    string applied;

    void OnDisable() { if (canvasGO) Destroy(canvasGO); applied = null; }

    void Update()
    {
        string k = surfaceGrain + "/" + surfaceAmount + "/" + surfaceCellsPerM + "/" + screenGrain + "/" + screenAmount;
        if (k == applied) return;
        applied = k;
        var mats = GetComponent<SceneA3Toggles>().materials;
        if (mats != null)
            foreach (var m in mats)
                if (m && m.HasProperty("_SN")) { m.SetFloat("_SN", surfaceGrain ? surfaceAmount : 0f); m.SetFloat("_SNDens", surfaceCellsPerM); }
        if (!Application.isPlaying) return;
        EnsureGrain();
        if (grainMat) grainMat.SetFloat("_Amt", screenAmount);
        if (canvasGO) canvasGO.SetActive(screenGrain);
    }

    void EnsureGrain()
    {
        if (grainMat || !grainShader) return;
        // 1280×720 고정 씨앗 노이즈 — 매 프레임 같은 그림
        noise = new Texture2D(1280, 720, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "A6dGrainNoise" };
        var px = new Color32[1280 * 720]; var rnd = new System.Random(1726);
        for (int i = 0; i < px.Length; i++) { byte v = (byte)rnd.Next(256); px[i] = new Color32(v, v, v, 255); }
        noise.SetPixels32(px); noise.Apply(false, false);
        grainMat = new Material(grainShader) { name = "A6dGrain" }; grainMat.mainTexture = noise;
        canvasGO = new GameObject("A6dGrain_Canvas");
        var cv = canvasGO.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = -31999;
        var img = new GameObject("Grain", typeof(RectTransform)); img.transform.SetParent(canvasGO.transform, false);
        var r = img.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        var ri = img.AddComponent<RawImage>(); ri.raycastTarget = false; ri.texture = noise; ri.material = grainMat;
    }

    /// <summary>캡처용: 화면에 얹는 것과 같은 그레인을 dst 에 곱한다(screenGrain 이 켜졌을 때만).</summary>
    public void ApplyTo(RenderTexture dst)
    {
        if (!screenGrain) return;
        EnsureGrain();
        if (grainMat) Graphics.Blit(noise, dst, grainMat);
    }
}
