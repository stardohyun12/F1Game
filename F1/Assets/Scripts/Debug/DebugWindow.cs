#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-248 개발용 디버그 창. ` 키(또는 F1)로 여닫는다. 에디터·개발 빌드에만 있고 정식 빌드엔 이 파일 전체가 빠진다.
/// 도트 UI(저해상도 RT) 위가 아니라 화면 위 원해상도(IMGUI)에 그린다 — 글씨는 도트 UI 와 같은 Galmuri11(DotScreenUI.BodyFont).
/// 장면에 붙이지 않는다: Play 시작 때 스스로 하나 만든다(씬 어디서든 OnFootDirector 가 있으면 장소·낚시·카트, SunsetLook 이 있으면 시간대).
/// 열려 있는 동안 마우스를 풀고 시선을 잠근다(OnFootCamera.UiCapture). 닫으면 걷기·앉기 중에는 다시 잠근다.
/// QA·봇은 아래 정적 함수(GoPlace · Fish · Recast · SetDusk · ToggleCart · SetSpeed)를 직접 부른다 — 버튼과 같은 길이다.
/// </summary>
[DefaultExecutionOrder(1000)]
public class DebugWindow : MonoBehaviour
{
    public static DebugWindow Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;
    /// <summary>마지막 동작 결과 한 줄(창 맨 아래에도 보인다).</summary>
    public static string LastResult { get; private set; } = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("DebugWindow (dev)");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugWindow>();
    }

    bool open;
    OnFootDirector dir;
    SunsetLook look;
    DotScreenUI dot;
    Font font;
    GUIStyle sTitle, sText, sBtn, sBtnOn, sLabel;
    Texture2D texBg, texBtn, texBtnHot, texBtnOn;
    int styleScale;
    float lastLookup = -99f;
    Vector2 scroll;

    void Awake() { Instance = this; }
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        OnFootCamera.UiCapture = false;
    }

    // ── 동작: 버튼과 QA 가 같이 쓴다 ──
    static OnFootDirector Dir() => Instance != null ? Instance.FindDir() : Object.FindFirstObjectByType<OnFootDirector>();
    OnFootDirector FindDir() { if (dir == null) dir = Object.FindFirstObjectByType<OnFootDirector>(); return dir; }

    public static string GoPlace(int i)
    {
        var d = Dir();
        if (d == null) return Say("이 씬엔 감독(OnFootDirector)이 없음");
        if (i < 0 || i >= DebugPlaces.All.Length) return Say("없는 장소 " + i);
        var p = DebugPlaces.All[i];
        string r;
        if (p.car == DebugPlaces.CarRule.BesideCar) r = d.DebugGoToCar();
        else
        {
            Vector3? carPos = null;
            if (p.car == DebugPlaces.CarRule.CartSpot) carPos = d.DebugCartSpot;
            else if (p.car == DebugPlaces.CarRule.Near) carPos = d.DebugFindCarSpot(p.feet, p.yaw);
            r = d.DebugTeleport(p.feet, p.yaw, carPos, p.yaw + 90f);
            if (p.car == DebugPlaces.CarRule.Near && !carPos.HasValue) r += " (둘레가 막혀 카트는 그대로)";
        }
        return Say(p.name + ": " + r);
    }

    public static string Fish() { var d = Dir(); return Say(d == null ? "감독 없음" : "낚시: " + d.DebugFishNow()); }
    public static string Recast() { var d = Dir(); return Say(d != null && d.DebugRecast() ? "찌 다시 던짐" : "낚시 자리에 앉아 있을 때만"); }
    public static string ToggleCart() { var d = Dir(); return Say(d == null ? "감독 없음" : "카트: " + d.DebugToggleCart()); }
    public static string SetSpeed(float s) { Time.timeScale = s; return Say("시간 " + s + "배속"); }

    /// <summary>0 = 오후 · 1 = 노을. 꺼져 있던 SunsetLook 은 켠다.</summary>
    public static string SetDusk(float v)
    {
        var l = SunsetLook.Find();
        if (l == null) return Say("이 씬엔 SunsetLook 이 없음");
        if (!l.IsOn) l.SetOn(true);
        l.dusk = Mathf.Clamp01(v);
        l.Apply();
        return Say("시간대 dusk=" + l.dusk.ToString("F2"));
    }

    static string Say(string s) { LastResult = s; Debug.Log("[Debug창] " + s); return s; }

    // ── 열고 닫기 ──
    public void SetOpen(bool on)
    {
        if (open == on) return;
        open = on;
        OnFootCamera.UiCapture = on;
        if (on) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else
        {
            var d = FindDir();
            if (d != null && (d.State == OnFootDirector.Mode.OnFoot || d.State == OnFootDirector.Mode.Seated)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && (k.backquoteKey.wasPressedThisFrame || k.f1Key.wasPressedThisFrame)) SetOpen(!open);
    }

    void LateUpdate()
    {
        if (!open) return;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;   // 상태가 바뀔 때 걷기 카메라가 다시 잠그는 것을 이긴다
    }

    // ── 그리기 ──
    static Texture2D Solid(Color c) { var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point }; t.SetPixel(0, 0, c); t.Apply(); return t; }

    void EnsureStyles()
    {
        if (font == null)
        {
            if (dot == null) dot = Object.FindFirstObjectByType<DotScreenUI>(FindObjectsInactive.Include);
            if (dot != null) font = dot.BodyFont;
#if UNITY_EDITOR
            if (font == null) font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/Galmuri/Galmuri11.ttf");
#endif
        }
        int s = Screen.height >= 900 ? 2 : 1;   // Galmuri11 은 12px 가 기본 — 정수 배수로만 키워 획이 뭉개지지 않게
        if (sText != null && s == styleScale) return;
        styleScale = s;
        int fs = 12 * s;
        if (texBg == null) { texBg = Solid(new Color(0.07f, 0.08f, 0.10f, 0.94f)); texBtn = Solid(new Color(0.20f, 0.23f, 0.28f, 1f)); texBtnHot = Solid(new Color(0.34f, 0.40f, 0.50f, 1f)); texBtnOn = Solid(new Color(0.78f, 0.55f, 0.20f, 1f)); }
        sTitle = new GUIStyle { font = font, fontSize = fs, fontStyle = FontStyle.Normal, normal = { textColor = new Color(1f, 0.82f, 0.45f) }, padding = new RectOffset(0, 0, 0, 0) };
        sText = new GUIStyle(sTitle) { normal = { textColor = new Color(0.92f, 0.92f, 0.92f) }, wordWrap = true };
        sLabel = new GUIStyle(sTitle) { normal = { textColor = new Color(0.60f, 0.72f, 0.90f) } };
        sBtn = new GUIStyle(sTitle)
        {
            alignment = TextAnchor.MiddleCenter, wordWrap = false, padding = new RectOffset(4 * s, 4 * s, 2 * s, 2 * s),
            normal = { background = texBtn, textColor = Color.white }, hover = { background = texBtnHot, textColor = Color.white }, active = { background = texBtnOn, textColor = Color.white },
        };
        sBtnOn = new GUIStyle(sBtn) { normal = { background = texBtnOn, textColor = Color.black }, hover = { background = texBtnOn, textColor = Color.black } };
    }

    string StateLine()
    {
        var d = FindDir();
        if (d == null) return "감독 없음(이 씬은 걷기 없음)";
        string st = d.State.ToString();
        Vector3 p = d.State == OnFootDirector.Mode.InCart && d.Car != null ? d.Car.transform.position : (d.Player != null ? d.Player.transform.position : Vector3.zero);
        if (d.State == OnFootDirector.Mode.Seated && Camera.main != null) p = Camera.main.transform.position;
        return $"{st}  ({p.x:F1}, {p.y:F1}, {p.z:F1})" + (string.IsNullOrEmpty(d.Prompt) ? "" : $"  [{d.Prompt}]");
    }

    void OnGUI()
    {
        if (!open) return;
        EnsureStyles();
        GUI.depth = -1000;
        int s = styleScale;
        float u = 12f * s;                 // 한 글자 높이
        float w = Mathf.Min(Screen.width - 20f, 25f * u + 8f * s);   // 약 25 글자 폭 두 칸
        float rowH = u * 1.9f;
        var rect = new Rect(10f, 10f, w, Mathf.Min(Screen.height - 20f, 25f * rowH));
        GUI.DrawTexture(rect, texBg);
        GUILayout.BeginArea(new Rect(rect.x + 8f * s, rect.y + 6f * s, rect.width - 16f * s, rect.height - 12f * s));
        scroll = GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUIStyle.none);

        GUILayout.Label("디버그 창   ` / F1 닫기", sTitle);
        GUILayout.Label(StateLine(), sText);
        var l = SunsetLook.Find();
        GUILayout.Label($"시간대 dusk={(l != null ? l.dusk.ToString("F2") : "-")}   배속 {Time.timeScale:F1}×", sText);
        var amb = AmbienceZones.Active;   // D-248 ③ 지금 자리의 소리 구역
        if (amb != null) GUILayout.Label($"소리: {amb.Label}  바람 {amb.WindDb:F0}dB  파도 {amb.WaveDb:F0}dB  먹먹 {amb.CutoffHz:F0}Hz  바다 {Mathf.Min(amb.SeaDist, 999f):F0}m", sText);
        GUILayout.Space(4 * s);

        GUILayout.Label("장소 (걷기 상태로 이동 · 카트는 근처에)", sLabel);
        for (int i = 0; i < DebugPlaces.All.Length; i += 2)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < Mathf.Min(i + 2, DebugPlaces.All.Length); j++)
                if (GUILayout.Button(DebugPlaces.All[j].name, sBtn, GUILayout.Height(rowH))) GoPlace(j);
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(4 * s);

        GUILayout.Label("낚시", sLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("낚시 자리 가서 앉기", sBtn, GUILayout.Height(rowH))) Fish();
        if (GUILayout.Button("찌 다시 던지기", sBtn, GUILayout.Height(rowH))) Recast();
        GUILayout.EndHorizontal();
        GUILayout.Space(4 * s);

        GUILayout.Label("시간대", sLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("오후", sBtn, GUILayout.Height(rowH))) SetDusk(0f);
        if (GUILayout.Button("노을", sBtn, GUILayout.Height(rowH))) SetDusk(1f);
        GUILayout.EndHorizontal();
        float cur = l != null ? l.dusk : 0f;
        float nv = GUILayout.HorizontalSlider(cur, 0f, 1f, GUILayout.Height(rowH * 0.7f));
        if (l != null && !Mathf.Approximately(nv, cur)) SetDusk(nv);
        GUILayout.Space(4 * s);

        GUILayout.Label("그 밖에", sLabel);
        var d = FindDir();
        bool inCart = d != null && d.State == OnFootDirector.Mode.InCart;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(inCart ? "카트 내리기" : "카트 타기", sBtn, GUILayout.Height(rowH))) ToggleCart();
        if (GUILayout.Button("1×", Mathf.Approximately(Time.timeScale, 1f) ? sBtnOn : sBtn, GUILayout.Height(rowH))) SetSpeed(1f);
        if (GUILayout.Button("3×", Mathf.Approximately(Time.timeScale, 3f) ? sBtnOn : sBtn, GUILayout.Height(rowH))) SetSpeed(3f);
        GUILayout.EndHorizontal();
        GUILayout.Space(4 * s);
        GUILayout.Label(LastResult, sText);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
#endif
