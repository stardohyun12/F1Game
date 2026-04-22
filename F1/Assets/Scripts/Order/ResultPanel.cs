using UnityEngine;

/// <summary>
/// 정산 화면. RunController.EndRun이 Show, "다시" 버튼으로 RestartFromBoard.
/// </summary>
public class ResultPanel : MonoBehaviour
{
    bool isOpen;
    RunController controller;

    Texture2D pixel;
    GUIStyle titleStyle, statStyle, statValStyle, btnStyle;

    public void Show(RunController ctrl)
    {
        controller = ctrl;
        isOpen = true;
    }

    public void Hide() { isOpen = false; }

    void EnsureStyles()
    {
        if (pixel == null)
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            statStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.85f, 0.85f, 0.85f) } };
            statValStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white } };
            btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
        }
    }

    void OnGUI()
    {
        if (!isOpen || controller == null) return;
        EnsureStyles();

        // 전체 어둡게
        GUI.color = new Color(0, 0, 0, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), pixel);
        GUI.color = Color.white;

        // 패널
        float w = 520f, h = 360f;
        var panelRect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        GUI.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        GUI.DrawTexture(panelRect, pixel);
        GUI.color = Color.white;

        // 색상 띠 (성공/실패)
        Color stripCol = controller.LastRunSuccess ? new Color(0.4f, 0.95f, 0.4f) : new Color(1f, 0.5f, 0.4f);
        GUI.color = stripCol;
        GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 8f), pixel);
        GUI.color = Color.white;

        // 제목
        var titleRect = new Rect(panelRect.x, panelRect.y + 30, panelRect.width, 50);
        GUI.Label(titleRect, controller.LastRunSuccess ? "배달 완료!" : "시간 초과", titleStyle);

        // 스탯
        float rowH = 36f;
        float rowsStartY = panelRect.y + 110;
        DrawRow(panelRect, rowsStartY + 0 * rowH, "주문", controller.CurrentJob != null ? controller.CurrentJob.displayName : "-");
        DrawRow(panelRect, rowsStartY + 1 * rowH, "완료", $"{controller.CompletedCount}/{(controller.CurrentJob != null ? controller.CurrentJob.pointCount : 0)}곳");
        DrawRow(panelRect, rowsStartY + 2 * rowH, "소요 시간", FormatTime(controller.LastElapsed));
        DrawRow(panelRect, rowsStartY + 3 * rowH, "보상", $"₩{controller.LastReward:N0}");

        // 다시 버튼
        var btnRect = new Rect(panelRect.x + panelRect.width / 2f - 90, panelRect.y + panelRect.height - 70, 180, 50);
        if (GUI.Button(btnRect, "다시", btnStyle))
        {
            controller.RestartFromBoard();
        }
    }

    void DrawRow(Rect panel, float y, string label, string val)
    {
        GUI.Label(new Rect(panel.x + 50, y, 200, 30), label, statStyle);
        GUI.Label(new Rect(panel.x + panel.width - 50 - 280, y, 280, 30), val, statValStyle);
    }

    static string FormatTime(float sec)
    {
        int m = Mathf.FloorToInt(sec / 60f);
        int s = Mathf.FloorToInt(sec % 60f);
        return $"{m}분 {s:D2}초";
    }
}
