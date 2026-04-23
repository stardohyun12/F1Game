using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 당 하나의 UI Canvas 루트. 1920×1080 기준 Scale With Screen Size.
/// 자식으로 HUD 컴포넌트들이 붙는다 (FuelHUD, GearHUD, CountdownHUD, ...).
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UIRoot : MonoBehaviour
{
    static UIRoot _inst;

    public static UIRoot Instance
    {
        get
        {
            if (_inst == null)
                _inst = FindFirstObjectByType<UIRoot>();
            return _inst;
        }
    }

    void Awake()
    {
        _inst = this;
    }

    /// <summary>런타임에 UIRoot를 보장하고, Canvas Transform 반환. Setup 스크립트에서 공통 호출.</summary>
    public static RectTransform Ensure()
    {
        var inst = Instance;
        if (inst != null) return inst.transform as RectTransform;

        var go = new GameObject("UIRoot");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        inst = go.AddComponent<UIRoot>();
        return go.transform as RectTransform;
    }
}
