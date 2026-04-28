using UnityEngine;

/// <summary>
/// 정산 화면. RunController.EndRun이 Show, "다시" 버튼으로 RestartFromBoard.
/// Day 24 — 업그레이드 영역 통합 (정산 → 구매 → 다음 런 흐름 완성).
/// </summary>
public class ResultPanel : MonoBehaviour
{
    [Header("Day 24 — 업그레이드")]
    [SerializeField] private UpgradeCatalog upgradeCatalog;

    bool isOpen;
    RunController controller;

    Texture2D pixel;
    GUIStyle titleStyle, statStyle, statValStyle, btnStyle;
    GUIStyle upgradeNameStyle, upgradeDescStyle, upgradeCostStyle, upgradeBtnStyle, walletStyle;

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
            // Day 24 업그레이드 스타일
            upgradeNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter, normal = { textColor = Color.white }, wordWrap = true };
            upgradeDescStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.UpperCenter, normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }, wordWrap = true };
            upgradeCostStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.95f, 0.5f) } };
            upgradeBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold };
            walletStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(1f, 0.95f, 0.5f) } };
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

        // 패널 (Day 24: 업그레이드 영역 추가로 height 460→700)
        bool hasUpgrades = upgradeCatalog != null && upgradeCatalog.upgrades != null && upgradeCatalog.upgrades.Count > 0 && UpgradeSystem.Instance != null;
        float w = 600f, h = hasUpgrades ? 700f : 460f;
        var panelRect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
        GUI.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);
        GUI.DrawTexture(panelRect, pixel);
        GUI.color = Color.white;

        // 색상 띠 (성공/실패) — Day 12: 화물 무결성도 반영
        bool perfectCargo = controller.LastIntegrityPercent >= 0.8f;
        Color stripCol = controller.LastRunSuccess && perfectCargo ? new Color(0.4f, 0.95f, 0.4f)
                       : controller.LastRunSuccess                  ? new Color(0.95f, 0.85f, 0.3f)
                                                                    : new Color(1f, 0.5f, 0.4f);
        GUI.color = stripCol;
        GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 8f), pixel);
        GUI.color = Color.white;

        // 제목
        var titleRect = new Rect(panelRect.x, panelRect.y + 30, panelRect.width, 50);
        GUI.Label(titleRect, controller.LastRunSuccess ? "배달 완료!" : "시간 초과", titleStyle);

        // 스탯
        float rowH = 36f;
        float rowsStartY = panelRect.y + 100;
        var job = controller.CurrentJob;
        string cargoName = job != null ? CargoTypePresets.DisplayName(job.cargoType) : "-";
        int integrityInt = Mathf.RoundToInt(controller.LastIntegrityPercent * 100f);
        string integrityGrade = RunController.IntegrityGrade(controller.LastIntegrityPercent);

        DrawRow(panelRect, rowsStartY + 0 * rowH, "주문", job != null ? job.displayName : "-");
        DrawRow(panelRect, rowsStartY + 1 * rowH, "완료", $"{controller.CompletedCount}/{(job != null ? job.pointCount : 0)}곳");
        DrawRow(panelRect, rowsStartY + 2 * rowH, "소요 시간", FormatTime(controller.LastElapsed));
        DrawRow(panelRect, rowsStartY + 3 * rowH, $"화물 ({cargoName})", $"{integrityInt}% · {integrityGrade}");
        DrawRow(panelRect, rowsStartY + 4 * rowH, "보상", $"₩{controller.LastReward:N0}");

        // 화물 이야기 한 줄 (Day 12)
        var storyRect = new Rect(panelRect.x + 30, rowsStartY + 5 * rowH + 8, panelRect.width - 60, 30);
        GUI.Label(storyRect, BuildStory(job, controller.LastIntegrityPercent), statStyle);

        // Day 24: 업그레이드 영역 (catalog 있을 때만)
        if (hasUpgrades)
        {
            DrawUpgradeSection(panelRect, rowsStartY + 5 * rowH + 50);
        }

        // 다시 버튼
        var btnRect = new Rect(panelRect.x + panelRect.width / 2f - 90, panelRect.y + panelRect.height - 70, 180, 50);
        if (GUI.Button(btnRect, "다시", btnStyle))
        {
            controller.RestartFromBoard();
        }
    }

    /// <summary>Day 24 — 업그레이드 5개 가로 카드 + Wallet.</summary>
    void DrawUpgradeSection(Rect panel, float startY)
    {
        var ups = UpgradeSystem.Instance;
        if (ups == null || upgradeCatalog == null) return;

        // Wallet 표시
        var walletRect = new Rect(panel.x + 20, startY, panel.width - 40, 28);
        GUI.Label(new Rect(walletRect.x, walletRect.y, 100, 28), "보유 자금", statStyle);
        GUI.Label(new Rect(walletRect.x + 100, walletRect.y, walletRect.width - 100, 28), $"₩{ups.Wallet:N0}", walletStyle);

        // 5개 카드 가로 배치
        var list = upgradeCatalog.upgrades;
        int n = Mathf.Min(5, list.Count);
        if (n == 0) return;
        float cardSpacing = 6f;
        float cardW = (panel.width - 40 - cardSpacing * (n - 1)) / n;
        float cardH = 140f;
        float cardY = startY + 38;

        for (int i = 0; i < n; i++)
        {
            var def = list[i];
            if (def == null) continue;
            var cardRect = new Rect(panel.x + 20 + i * (cardW + cardSpacing), cardY, cardW, cardH);
            DrawUpgradeCard(cardRect, def, ups);
        }
    }

    void DrawUpgradeCard(Rect r, UpgradeDef def, UpgradeSystem ups)
    {
        bool owned = ups.IsOwned(def.effect);
        bool affordable = ups.Wallet >= def.cost;

        // 배경 (보유=어두운 녹색 / 구매가능=짙은 회색 / 자금부족=더 어두움)
        Color bg = owned ? new Color(0.2f, 0.45f, 0.25f, 0.85f)
                : affordable ? new Color(0.18f, 0.20f, 0.25f, 0.95f)
                : new Color(0.10f, 0.10f, 0.12f, 0.95f);
        GUI.color = bg;
        GUI.DrawTexture(r, pixel);

        // 색상 띠 (UpgradeDef.tintColor)
        GUI.color = def.tintColor;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 4f), pixel);

        GUI.color = Color.white;

        // 이름
        GUI.Label(new Rect(r.x + 4, r.y + 8, r.width - 8, 30), def.displayName, upgradeNameStyle);

        // 설명
        GUI.Label(new Rect(r.x + 4, r.y + 36, r.width - 8, 50), def.description, upgradeDescStyle);

        // 가격
        GUI.Label(new Rect(r.x + 4, r.y + 86, r.width - 8, 18), $"₩{def.cost:N0}", upgradeCostStyle);

        // 상태/버튼
        var btnRect = new Rect(r.x + 8, r.y + 108, r.width - 16, 26);
        if (owned)
        {
            GUI.Label(btnRect, "✓ 보유", new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.7f, 1f, 0.7f) } });
        }
        else if (!affordable)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.4f);
            GUI.Label(btnRect, "자금 부족", new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.6f, 0.6f) } });
            GUI.color = Color.white;
        }
        else
        {
            if (GUI.Button(btnRect, "구매", upgradeBtnStyle))
            {
                ups.TryPurchase(def);
            }
        }
    }

    /// <summary>
    /// 화물 + 무결성 → 한 줄 이야기. 숫자 대신 감각적 피드백.
    /// "잘했다/못했다" 즉각 인지가 목적 (Phase 1 게이트 항목).
    /// </summary>
    static string BuildStory(OrderJob job, float integrityPercent)
    {
        if (job == null) return string.Empty;
        var name = CargoTypePresets.DisplayName(job.cargoType);
        float i = integrityPercent * 100f;
        if (i >= 95f) return $"\"{name}, 한 점 흠 없이 도착했음.\"";
        if (i >= 80f) return $"\"{name}, 잘 가져왔음.\"";
        if (i >= 50f) return $"\"{name}, 조금 흠집 났음.\"";
        if (i >= 20f) return $"\"{name}, 많이 망가졌음. 컴플레인 들어옴.\"";
        if (i > 0f)   return $"\"{name}, 거의 다 부서짐.\"";
        return $"\"{name}, 완전히 박살남. 환불 처리.\"";
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
