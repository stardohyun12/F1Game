using UnityEngine;

/// <summary>
/// 연료 게이지 HUD. fuel.md 명세 따름.
///
/// 표시:
/// - 좌하단 가로 바
/// - 25% 이하: 황색
/// - 10% 이하: 적색 깜박임
/// - 0%: "연료 없음" 텍스트 + F12 안내 (debug)
/// </summary>
public class FuelHUD : MonoBehaviour
{
    [SerializeField] private FuelSystem fuel;

    [Header("Layout")]
    [SerializeField] private Vector2 anchor = new Vector2(28, -110); // 좌하단 기준 offset
    [SerializeField] private Vector2 size = new Vector2(420, 44);

    [Header("Colors")]
    [SerializeField] private Color colorOk = new Color(0.4f, 0.9f, 0.4f, 0.9f);
    [SerializeField] private Color colorWarn = new Color(1f, 0.85f, 0.3f, 0.95f);
    [SerializeField] private Color colorCrit = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color colorBg = new Color(0, 0, 0, 0.55f);

    [Header("Thresholds (0~1)")]
    [SerializeField] private float warnThreshold = 0.25f;
    [SerializeField] private float critThreshold = 0.10f;
    [SerializeField] private float critBlinkHz = 4f;

    Texture2D pixel;
    GUIStyle labelStyle;

    void Awake()
    {
        if (fuel == null) fuel = FindFirstObjectByType<FuelSystem>();
    }

    void EnsureRes()
    {
        if (pixel == null)
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }

    void OnGUI()
    {
        if (fuel == null) return;
        EnsureRes();

        float level = Mathf.Clamp01(fuel.FuelLevel);

        // 좌하단 기준 위치 계산
        Rect bgRect = new Rect(
            anchor.x,
            Screen.height + anchor.y - size.y,
            size.x,
            size.y
        );

        // 배경
        GUI.color = colorBg;
        GUI.DrawTexture(bgRect, pixel);

        // 채움 색상
        Color fill;
        if (level <= critThreshold)
        {
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * critBlinkHz * Mathf.PI * 2f);
            fill = Color.Lerp(colorCrit, new Color(1f, 0.6f, 0.6f, 1f), blink);
        }
        else if (level <= warnThreshold)
        {
            fill = colorWarn;
        }
        else
        {
            fill = colorOk;
        }

        // 채움 바
        Rect fillRect = new Rect(
            bgRect.x + 2,
            bgRect.y + 2,
            (bgRect.width - 4) * level,
            bgRect.height - 4
        );
        GUI.color = fill;
        GUI.DrawTexture(fillRect, pixel);
        GUI.color = Color.white;

        // 라벨
        Rect labelRect = new Rect(bgRect.x + 14, bgRect.y, bgRect.width - 28, bgRect.height);
        string text = fuel.IsDepleted
            ? "FUEL EMPTY (F12: 주유)"
            : $"⛽ {Mathf.RoundToInt(level * 100)}%";
        GUI.Label(labelRect, text, labelStyle);
    }
}
