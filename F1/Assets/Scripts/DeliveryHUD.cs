using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class DeliveryHUD : MonoBehaviour
{
    [SerializeField] private DeliveryManager manager;
    [SerializeField] private Camera cam;
    [SerializeField] private float arrowEdgePadding = 80f;
    [SerializeField] private float arrowSize = 60f;
    [SerializeField] private Color arrowColor = new Color(0.2f, 1f, 0.4f, 0.9f);
    [SerializeField] private Color returnColor = new Color(1f, 0.8f, 0.2f, 0.9f);
    [SerializeField] private Color doneColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

    Texture2D arrowTex;
    GUIStyle countStyle;
    GUIStyle msgStyle;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (manager == null) manager = FindFirstObjectByType<DeliveryManager>();
    }

    void EnsureStyles()
    {
        if (arrowTex == null)
        {
            arrowTex = new Texture2D(2, 2);
            arrowTex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            arrowTex.Apply();
        }
        if (countStyle == null)
        {
            countStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
        if (msgStyle == null)
        {
            msgStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.95f, 0.6f) }
            };
        }
    }

    void OnGUI()
    {
        if (manager == null || cam == null) return;
        EnsureStyles();

        // top-center count
        string countText;
        Color tint;
        Vector3 worldTarget = Vector3.zero;
        bool hasTarget = false;

        if (manager.Finished)
        {
            countText = "COMPLETE";
            tint = doneColor;
        }
        else if (manager.Returning)
        {
            countText = $"{manager.TotalCount}/{manager.TotalCount}  RETURN";
            tint = returnColor;
            if (manager.ReturnTarget != null) { worldTarget = manager.ReturnTarget.position; hasTarget = true; }
        }
        else
        {
            countText = $"{manager.CurrentIndex}/{manager.TotalCount}";
            tint = arrowColor;
            var t = manager.CurrentTarget;
            if (t != null) { worldTarget = t.transform.position; hasTarget = true; }
        }

        var rect = new Rect(Screen.width / 2f - 120, 20, 240, 56);
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(rect, arrowTex);
        GUI.color = Color.white;
        GUI.Label(rect, countText, countStyle);

        if (manager.Returning)
        {
            var msgRect = new Rect(Screen.width / 2f - 200, 80, 400, 30);
            GUI.Label(msgRect, "출발 지점으로 복귀", msgStyle);
        }

        if (!hasTarget) return;

        // arrow / on-screen marker
        Vector3 sp = cam.WorldToScreenPoint(worldTarget);
        bool behind = sp.z < 0;
        if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }

        // convert to GUI coords (y flip)
        Vector2 guiPos = new Vector2(sp.x, Screen.height - sp.y);
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);

        bool offScreen = behind || sp.x < 0 || sp.x > Screen.width || sp.y < 0 || sp.y > Screen.height;
        Vector2 dir = (guiPos - center);
        float angle;

        if (offScreen)
        {
            // clamp to edge
            Vector2 d = dir.normalized;
            float maxX = Screen.width / 2f - arrowEdgePadding;
            float maxY = Screen.height / 2f - arrowEdgePadding;
            float scale = Mathf.Min(maxX / Mathf.Max(0.001f, Mathf.Abs(d.x)), maxY / Mathf.Max(0.001f, Mathf.Abs(d.y)));
            guiPos = center + d * scale;
            angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90f;
        }
        else
        {
            angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
        }

        DrawArrow(guiPos, angle, tint);
    }

    void DrawArrow(Vector2 pos, float angleDeg, Color color)
    {
        var matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angleDeg, pos);
        GUI.color = color;
        var r = new Rect(pos.x - arrowSize / 2f, pos.y - arrowSize / 2f, arrowSize, arrowSize);
        // simple triangle approximation: a tall thin rect + a square head
        GUI.DrawTexture(new Rect(r.x + arrowSize * 0.4f, r.y, arrowSize * 0.2f, arrowSize * 0.7f), arrowTex);
        GUI.DrawTexture(new Rect(r.x + arrowSize * 0.2f, r.y - arrowSize * 0.1f, arrowSize * 0.6f, arrowSize * 0.3f), arrowTex);
        GUI.color = Color.white;
        GUI.matrix = matrix;
    }
}
