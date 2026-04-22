using UnityEngine;

/// <summary>
/// 기어 표시 HUD. 우하단 큰 박스.
/// gears.md K5 — "HUD 기어숫자 플래시"는 시프트 순간 0.15초 화이트→원색 페이드.
/// </summary>
public class GearHUD : MonoBehaviour
{
    [SerializeField] private GearSystem gears;

    [Header("Layout (우하단 기준 offset)")]
    [SerializeField] private Vector2 anchor = new Vector2(-160, -130); // 우하단에서 좌/상
    [SerializeField] private Vector2 size = new Vector2(120, 110);

    [Header("Style")]
    [SerializeField] private Color colorBg = new Color(0, 0, 0, 0.55f);
    [SerializeField] private Color colorText = new Color(0.95f, 0.95f, 0.95f);
    [SerializeField] private Color colorFlash = Color.white;
    [SerializeField] private float flashDurationSec = 0.15f;

    Texture2D pixel;
    GUIStyle gearStyle;
    GUIStyle labelStyle;
    float flashT;

    void OnEnable()
    {
        if (gears == null) gears = FindFirstObjectByType<GearSystem>();
        if (gears != null) gears.OnShift += OnShift;
    }

    void OnDisable()
    {
        if (gears != null) gears.OnShift -= OnShift;
    }

    void OnShift(int prev, int cur)
    {
        // 기어 변화 전부 플래시 (N→1 포함)
        flashT = flashDurationSec;
    }

    void Update()
    {
        if (flashT > 0f) flashT -= Time.unscaledDeltaTime;
    }

    void EnsureRes()
    {
        if (pixel == null)
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }
        if (gearStyle == null)
        {
            gearStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 68,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = colorText }
            };
        }
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
        }
    }

    void OnGUI()
    {
        if (gears == null) return;
        EnsureRes();

        Rect bgRect = new Rect(
            Screen.width + anchor.x,
            Screen.height + anchor.y - size.y,
            size.x,
            size.y
        );

        // 배경
        GUI.color = colorBg;
        GUI.DrawTexture(bgRect, pixel);

        // 라벨 "GEAR"
        Rect titleRect = new Rect(bgRect.x, bgRect.y + 6, bgRect.width, 20);
        GUI.color = Color.white;
        GUI.Label(titleRect, "GEAR", labelStyle);

        // 기어 숫자 (플래시 중에는 색상 interp)
        float flashAmt = flashDurationSec > 0 ? Mathf.Clamp01(flashT / flashDurationSec) : 0f;
        Color textCol = Color.Lerp(colorText, colorFlash, flashAmt);

        gearStyle.normal.textColor = textCol;
        Rect gearRect = new Rect(bgRect.x, bgRect.y + 20, bgRect.width, bgRect.height - 20);
        GUI.Label(gearRect, gears.GearLabel, gearStyle);
    }
}
