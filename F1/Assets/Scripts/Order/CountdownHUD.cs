using UnityEngine;

/// <summary>
/// 시간제한 카운트다운 표시 (상단 우측). 시간제한 없는 작업이면 미표시.
/// </summary>
public class CountdownHUD : MonoBehaviour
{
    [SerializeField] private RunController controller;

    [Header("Layout (우상단 기준)")]
    [SerializeField] private Vector2 anchor = new Vector2(-260, 30);
    [SerializeField] private Vector2 size = new Vector2(220, 56);

    Texture2D pixel;
    GUIStyle timeStyle;

    void Awake()
    {
        if (controller == null) controller = FindFirstObjectByType<RunController>();
    }

    void OnGUI()
    {
        if (controller == null || !controller.RunActive) return;
        float remaining = controller.TimeRemaining;
        if (remaining < 0f) return; // 무제한 작업

        if (pixel == null)
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }
        if (timeStyle == null)
        {
            timeStyle = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        }

        Rect r = new Rect(Screen.width + anchor.x, anchor.y, size.x, size.y);

        Color bgCol;
        if (remaining < 30f) bgCol = new Color(0.7f, 0.15f, 0.15f, 0.8f);
        else if (remaining < 60f) bgCol = new Color(0.8f, 0.6f, 0.1f, 0.7f);
        else bgCol = new Color(0, 0, 0, 0.55f);

        GUI.color = bgCol;
        GUI.DrawTexture(r, pixel);
        GUI.color = Color.white;

        int m = Mathf.FloorToInt(remaining / 60f);
        int s = Mathf.FloorToInt(remaining % 60f);
        GUI.Label(r, $"⏱ {m}:{s:D2}", timeStyle);
    }
}
