using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Day 6 (2026-05-19): 메인 메뉴 + 타이틀.
/// ComplexTrack 씬 안에서 자가 구성 UGUI (별도 씬 X — transition 부담 회피).
/// 게임 시작 시 Show, 시작 버튼 클릭 시 Hide + RunController.OpenBoard 트리거.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuScreen : MonoBehaviour
{
    [Tooltip("게임 시작 시 자동 표시. false면 디버그용으로 OrderBoard 직행.")]
    [SerializeField] private bool showOnStart = true;

    Canvas _canvas;
    CanvasGroup _cg;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        EnsureBuilt();
        if (showOnStart) Show();
        else Hide();
    }

    void EnsureBuilt()
    {
        if (_canvas != null) return;

        var go = new GameObject("MainMenuCanvas");
        go.transform.SetParent(transform, false);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 6000;  // 차고(4000)·정산(3500) 등 모두 위
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.AddComponent<GraphicRaycaster>();
        _cg = go.AddComponent<CanvasGroup>();

        // 배경 — D-121 종이 바탕 (후속: 원화 자산)
        var bgGo = new GameObject("BG", typeof(RectTransform));
        bgGo.transform.SetParent(go.transform, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = UIPalette.Paper;
        var bgrt = bg.rectTransform;
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;

        // 타이틀
        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(go.transform, false);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "PIT CREW";
        title.alignment = TextAlignmentOptions.Center;
        // 캔버스 기준 해상도(800x600)에서 고정 144는 rect(640x90)를 넘쳐 두 줄로 쏟아짐.
        // auto-size + 한 줄 고정 → rect 안에 맞게 자동 축소 (다른 화면 비율에도 안전).
        title.enableWordWrapping = false;
        title.enableAutoSizing = true;
        title.fontSizeMin = 24f;
        title.fontSizeMax = 144f;
        title.fontStyle = FontStyles.Bold;
        UIPalette.Display(title, UIPalette.Ink);     // 제목체(Jua)
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0.1f, 0.60f); trt.anchorMax = new Vector2(0.9f, 0.80f);
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        // 서브 타이틀
        var subGo = new GameObject("Subtitle", typeof(RectTransform));
        subGo.transform.SetParent(go.transform, false);
        var sub = subGo.AddComponent<TextMeshProUGUI>();
        sub.text = "망한 F1 팀의 배달 — 머신 복원 → 그랑프리 복귀";
        sub.alignment = TextAlignmentOptions.Center;
        sub.fontSize = 26;
        UIPalette.Body(sub, UIPalette.InkMuted);
        var srt = sub.rectTransform;
        srt.anchorMin = new Vector2(0.1f, 0.53f); srt.anchorMax = new Vector2(0.9f, 0.60f);
        srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

        // 시작 버튼
        MakeButton(go.transform, "시작", new Vector2(0.35f, 0.32f), new Vector2(0.65f, 0.40f), OnStartClicked, UIPalette.Terracotta, UIPalette.Paper);
        // 종료 버튼
        MakeButton(go.transform, "종료", new Vector2(0.35f, 0.20f), new Vector2(0.65f, 0.28f), OnQuitClicked, UIPalette.PaperDeep, UIPalette.Ink);

        // 버전/크레딧
        var verGo = new GameObject("Version", typeof(RectTransform));
        verGo.transform.SetParent(go.transform, false);
        var ver = verGo.AddComponent<TextMeshProUGUI>();
        ver.text = "Demo · 2026-06-24";
        ver.alignment = TextAlignmentOptions.BottomRight;
        ver.fontSize = 16;
        UIPalette.Body(ver, UIPalette.InkMuted);
        var vrt = ver.rectTransform;
        vrt.anchorMin = new Vector2(0.8f, 0.02f); vrt.anchorMax = new Vector2(0.98f, 0.06f);
        vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;

        _canvas.enabled = false;
    }

    void MakeButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick, Color bgColor, Color textColor)
    {
        var go = new GameObject($"Btn_{label}", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        UIPalette.PaperPanel(img, bgColor, slot: UISlot.Button);
        var rt = img.rectTransform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        UIPalette.ButtonColors(btn, bgColor);
        btn.onClick.AddListener(() => onClick?.Invoke());

        var txtGo = new GameObject("Txt", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 36;
        txt.fontStyle = FontStyles.Bold;
        UIPalette.Body(txt, textColor);
        var trt = txt.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
    }

    public void Show()
    {
        EnsureBuilt();
        _canvas.enabled = true;
        IsOpen = true;
        if (_cg != null) { _cg.alpha = 1f; _cg.blocksRaycasts = true; _cg.interactable = true; }
        // 게임 첫 진입 시 OrderBoard 자동 열기 막기 — RunController는 자체 OpenBoard 안 함
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (_canvas != null) _canvas.enabled = false;
        IsOpen = false;
        if (_cg != null) { _cg.alpha = 0f; _cg.blocksRaycasts = false; _cg.interactable = false; }
        Time.timeScale = 1f;
    }

    void OnStartClicked()
    {
        Hide();
        // OrderBoard 열기 트리거 — RunController가 자동 처리 (RestartFromBoard 호출)
        var run = Services.Run as RunController;
        if (run != null)
        {
            run.RestartFromBoard();
        }
        else
        {
            Debug.LogWarning("[MainMenuScreen] Services.Run null — RunController wiring 확인");
        }
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
