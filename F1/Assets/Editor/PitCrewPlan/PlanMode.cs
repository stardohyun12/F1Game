using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace PitCrewPlan
{
    /// <summary>
    /// 씬 뷰 기획 모드(오버레이 "💬 코멘트" 또는 C). 클릭 = 그 자리 입력칸, 드래그 = 빨간 선, Enter = 저장(스샷 자동, 선 합성), Esc = 취소,
    /// Backspace = 마지막 선 지우기. 저장된 코멘트는 핀(번호)으로 보이고, 핀 클릭 = 말풍선(내용·썸네일·[처리됨]).
    /// 씬 저장마다 배치(라이브러리 프리팹 위치·yaw)를 placements.yaml 에 자동 기록. 잠금은 모드 켜짐 또는 기획 창 열림 + Unity 포커스.
    /// </summary>
    [InitializeOnLoad]
    public static class PlanMode
    {
        public const string TargetScene = "Map_TownV6";   // 배치 자동 기록을 하는 씬(placements.yaml = first-delivery 계획)
        static readonly int Hash = "PitCrewPlanMode".GetHashCode();
        static readonly Color Red = new Color(0.92f, 0.16f, 0.16f), PinOpen = Color.white, PinDone = new Color(0.55f, 0.55f, 0.55f), PinSel = new Color(1f, 0.45f, 0.1f);

        public static bool On
        {
            get => SessionState.GetBool("PCPlan.On", false);
            set { SessionState.SetBool("PCPlan.On", value); if (!value) { Strokes.Clear(); CloseInput(); bubble = null; } SceneView.RepaintAll(); }
        }
        public static bool HideDone { get => SessionState.GetBool("PCPlan.HideDone", true); set { SessionState.SetBool("PCPlan.HideDone", value); SceneView.RepaintAll(); } }
        public static string ArmedPrefab;

        /// 그린 선(씬 뷰 GUI 좌표, 포인트 단위). 저장 때 0~1 로 바꿔 스샷에 합성한다.
        public static readonly List<List<Vector2>> Strokes = new List<List<Vector2>>();
        static List<Vector2> cur;
        static Vector2 downPos; static bool dragging;

        static bool inputOpen => PlanInputPopup.Current != null && !PlanInputPopup.Current.IsReply;
        static string inputText => PlanInputPopup.Current != null ? PlanInputPopup.Current.Text : "";
        static GameObject inputGo; static Vector3? inputPoint;
        static Rect bubbleRect;
        static PlanIO.Comment bubble; static int bubbleNo;

        static List<PlanIO.Comment> pins = new List<PlanIO.Comment>(); static DateTime pinsStamp; static string pinsScene;
        static readonly Dictionary<string, Texture2D> thumbs = new Dictionary<string, Texture2D>();
        static double lastTick, lastLock; static bool holding;

        static PlanMode()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += Tick;
            EditorSceneManager.sceneSaved += OnSaved;
        }

        static string SceneName => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // ───────── 잠금·핀 갱신 (1초마다) ─────────
        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - lastTick < 1) return;
            lastTick = now;
            RefreshPins(false);
            bool use = On || Resources.FindObjectsOfTypeAll<PitCrewPlanWindow>().Length > 0;
            if (!use) { if (holding) { PlanIO.ReleaseLock(); holding = false; } return; }
            if (!InternalEditorUtility.isApplicationActive) return;   // 자리 비우면 갱신 안 함 → 60초 뒤 팀원이 쓸 수 있음
            if (holding && now - lastLock < 30) return;
            lastLock = now; holding = PlanIO.TouchLock();
        }

        public static void RefreshPins(bool force)
        {
            var path = PlanIO.JsonlPath(SceneName);
            var stamp = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            if (!force && stamp == pinsStamp && pinsScene == SceneName) return;
            pinsStamp = stamp; pinsScene = SceneName;
            pins = PlanIO.ReadComments(SceneName);
            if (bubble != null) bubble = pins.FirstOrDefault(p => p.id == bubble.id);
            SceneView.RepaintAll();
        }

        // ───────── 씬 저장 → 배치 자동 기록 ─────────
        static void OnSaved(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene.name != TargetScene) return;
            var delta = PlanIO.WritePlacements(scene, out var err);
            if (err == null && delta.Count > 0) { Debug.Log("[기획] 배치 자동 기록 " + delta.Count + "건 → placements.yaml\n" + string.Join("\n", delta)); RefreshPins(true); }
            // 0433: 옮긴 건물이 비탈에 묻히거나 뜨면 알려만 준다(지형 자동 수정 없음)
            PlanIO.GroundWarnings = PlanIO.GroundCheck(scene);
            foreach (var w in PlanIO.GroundWarnings) Debug.LogWarning("[기획] " + w);
            foreach (var win in Resources.FindObjectsOfTypeAll<PitCrewPlanWindow>()) win.Repaint();
        }

        // ───────── 씬 뷰 ─────────
        static void OnSceneGUI(SceneView sv)
        {
            var e = Event.current;
            int id = GUIUtility.GetControlID(Hash, FocusType.Passive);
            bool active = On || !string.IsNullOrEmpty(ArmedPrefab);

            if (On && e.type == EventType.KeyDown) Keys(sv, e);
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.C && !e.control && !e.alt && !e.shift && !e.command && !EditorGUIUtility.editingTextField) { On = true; e.Use(); }
            if (!string.IsNullOrEmpty(ArmedPrefab) && e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { ArmedPrefab = null; e.Use(); }

            Handles.BeginGUI();
            DrawPins(sv);
            if (On) DrawStrokesAndHint(sv);
            if (bubble != null) DrawBubble(sv);
            if (!string.IsNullOrEmpty(ArmedPrefab)) Hint(sv, "클릭 = " + Path.GetFileNameWithoutExtension(ArmedPrefab) + " 놓기 · Esc = 취소");
            Handles.EndGUI();

            if (!active) return;
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);
            if (e.alt || e.button != 0) return;
            if (bubble != null && bubbleRect.Contains(e.mousePosition)) return;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (!string.IsNullOrEmpty(ArmedPrefab)) { PlacePrefab(e.mousePosition); e.Use(); return; }
                    GUIUtility.hotControl = id; downPos = e.mousePosition; dragging = false; e.Use();
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id) break;
                    if (!dragging && Vector2.Distance(downPos, e.mousePosition) > 4f) { dragging = true; cur = new List<Vector2> { downPos }; Strokes.Add(cur); bubble = null; }
                    if (dragging) cur.Add(e.mousePosition);
                    e.Use(); sv.Repaint();
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id) break;
                    GUIUtility.hotControl = 0;
                    if (!dragging) Click(e.mousePosition);
                    else { cur = null; if (!inputOpen) OpenInput(e.mousePosition, null, null); }
                    dragging = false; e.Use(); sv.Repaint();
                    break;
            }
        }

        static void Keys(SceneView sv, Event e)
        {
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                if (inputOpen || Strokes.Count > 0) { Save(sv); e.Use(); }
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                if (inputOpen) CloseInput(); else if (bubble != null) bubble = null; else Strokes.Clear();
                e.Use(); sv.Repaint();
            }
            else if (e.keyCode == KeyCode.Backspace && (!inputOpen || inputText.Length == 0) && Strokes.Count > 0)
            {
                Strokes.RemoveAt(Strokes.Count - 1); e.Use(); sv.Repaint();
            }
            else if (e.keyCode == KeyCode.C && !inputOpen && !EditorGUIUtility.editingTextField && !e.control && !e.alt && !e.command)
            {
                On = false; e.Use();
            }
        }

        static void Click(Vector2 gp)
        {
            // 핀 먼저
            for (int i = pins.Count - 1; i >= 0; i--)
            {
                var p = pins[i];
                if (!p.point.HasValue || p.Done && HideDone) continue;
                if (Vector2.Distance(HandleUtility.WorldToGUIPoint(p.point.Value), gp) < 13f) { bubble = p; bubbleNo = i + 1; CloseInput(); return; }
            }
            bubble = null;
            var go = HandleUtility.PickGameObject(gp, false);
            Vector3? point = null;
            Physics.SyncTransforms();
            if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(gp), out var hit, 5000f, ~0, QueryTriggerInteraction.Ignore)) point = hit.point;
            else if (go != null) point = PlanIO.TryBounds(go, out var b) ? b.center : go.transform.position;
            if (go != null) Selection.activeGameObject = go;
            OpenInput(gp, go, point);
        }

        // 입력칸 = 따로 뜨는 팝업 창(PlanInputPopup) — 씬 뷰 IMGUI 는 한글 IME 를 못 받는다. 이미 떠 있으면 글은 그대로 두고 자리만 바꾼다.
        static void OpenInput(Vector2 gp, GameObject go, Vector3? point)
        {
            if (go != null || point.HasValue || inputGo == null) { inputGo = go; inputPoint = point; }
            PlanInputPopup.Open(GUIUtility.GUIToScreenPoint(gp), inputGo != null ? inputGo.name : inputPoint.HasValue ? "땅" : "화면");
        }
        static void CloseInput()
        {
            inputGo = null; inputPoint = null;
            if (PlanInputPopup.Current != null) PlanInputPopup.Current.Close();
        }

        /// 팝업에서 Enter.
        public static void Submit(string text) { var sv = SceneView.lastActiveSceneView; if (sv != null) Save(sv, text); else CloseInput(); }
        /// 팝업에서 Esc.
        public static void CancelInput() { CloseInput(); SceneView.RepaintAll(); }

        static void Save(SceneView sv) => Save(sv, inputText);
        static void Save(SceneView sv, string raw)
        {
            var text = (raw ?? "").Trim();
            if (text.Length == 0 && Strokes.Count == 0) { CloseInput(); return; }
            var cam = sv.camera;
            float ppp = EditorGUIUtility.pixelsPerPoint, pw = cam.pixelWidth / ppp, ph = cam.pixelHeight / ppp;
            var norm = Strokes.Select(s => s.Select(p => new Vector2(p.x / pw, p.y / ph)).ToList()).ToList();
            int w = 1280, h = Mathf.Clamp(Mathf.RoundToInt(1280f * ph / pw), 360, 1280);
            var point = inputPoint;
            if (!point.HasValue && Strokes.Count > 0)
            {   // 그림만 있으면 선 가운데 아래 땅을 핀 자리로
                var all = Strokes.SelectMany(s => s).ToList(); var c = new Vector2(all.Average(p => p.x), all.Average(p => p.y));
                if (Physics.Raycast(cam.ScreenPointToRay(new Vector3(c.x * ppp, cam.pixelHeight - c.y * ppp, 0)), out var hit, 5000f, ~0, QueryTriggerInteraction.Ignore)) point = hit.point;
            }
            var id = SaveComment(cam, sv.pivot, "scene", inputGo, point, text.Length > 0 ? text : "(그림)", norm, w, h);
            Debug.Log("[기획] 코멘트 저장 " + id);
            Strokes.Clear(); CloseInput(); RefreshPins(true); sv.Repaint();
        }

        /// <summary>코멘트 한 건 저장: 스샷(선 합성) + jsonl 한 줄. 테스트에서도 직접 부른다.</summary>
        public static string SaveComment(Camera src, Vector3 camTarget, string mode, GameObject go, Vector3? point, string text, List<List<Vector2>> strokes, int w = 1280, int h = 720)
        {
            var scene = SceneName;
            var id = "unity-" + scene + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
            var shotRel = "shots/" + id + ".png";
            bool shot = PlanIO.RenderPng(src, Path.Combine(PlanIO.FeedbackDir, shotRel), w, h, strokes);
            var nodes = go == null ? "" : "{\"node\":" + PlanIO.Q(go.name) + ",\"path\":" + PlanIO.Q(PlanIO.PathOf(go.transform)) + ",\"building\":" + PlanIO.Q(PlanIO.BuildingName(go))
                + ",\"point\":" + PlanIO.Arr(go.transform.position) + ",\"yaw\":" + PlanIO.F(PlanIO.Yaw(go.transform), 1) + "}";
            var cam = src == null ? "null" : "{\"pos\":" + PlanIO.Arr(src.transform.position) + ",\"target\":" + PlanIO.Arr(camTarget) + ",\"mode\":" + PlanIO.Q(mode) + ",\"fov\":" + PlanIO.F(src.fieldOfView, 1) + "}";
            var st = strokes == null || strokes.Count == 0 ? "null" : "[" + string.Join(",", strokes.Select(s => "[" + string.Join(",", s.Select(p => "[" + PlanIO.F(p.x, 4) + "," + PlanIO.F(p.y, 4) + "]")) + "]")) + "]";
            var line = "{\"id\":" + PlanIO.Q(id) + ",\"time\":" + PlanIO.Q(PlanIO.Now()) + ",\"building\":" + PlanIO.Q(go != null ? PlanIO.BuildingName(go) : scene)
                + ",\"node\":" + PlanIO.Q(go != null ? go.name : null) + ",\"nodes\":[" + nodes + "],\"point\":" + (point.HasValue ? PlanIO.Arr(point.Value) : "null")
                + ",\"point_source\":" + (go != null ? "\"click-object\"" : point.HasValue ? "\"click-ground\"" : "\"none\"")
                + ",\"space\":\"unity-world\",\"scene\":" + PlanIO.Q(scene) + ",\"camera\":" + cam + ",\"strokes\":" + st
                + ",\"comment\":" + PlanIO.Q(text) + ",\"screenshot\":" + (shot ? PlanIO.Q(shotRel) : "null") + ",\"status\":\"open\"}";
            PlanIO.AppendLine(scene, line);
            return id;
        }

        // ───────── 그리기 ─────────
        static GUIStyle pinStyle, hintStyle;
        static void Styles()
        {
            if (pinStyle != null) return;
            pinStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 11 }; pinStyle.normal.textColor = Color.white;
            hintStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, padding = new RectOffset(10, 10, 5, 5) }; hintStyle.normal.textColor = Color.white;
        }

        static void Hint(SceneView sv, string s)
        {
            Styles();
            float ppp = EditorGUIUtility.pixelsPerPoint, pw = sv.camera.pixelWidth / ppp;   // 위 가운데 — 아래 구석은 Unity 기본 오버레이가 가린다
            var size = hintStyle.CalcSize(new GUIContent(s));
            var r = new Rect((pw - size.x) * 0.5f, 8, size.x, size.y);
            EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.72f));
            GUI.Label(r, s, hintStyle);
        }

        static void DrawStrokesAndHint(SceneView sv)
        {
            Hint(sv, "클릭=코멘트 · 드래그=그리기 · Enter=저장 · Esc=취소 · C=끄기" + (Strokes.Count > 0 ? "   (선 " + Strokes.Count + "개, Backspace=지우기)" : ""));
            if (Event.current.type != EventType.Repaint) return;
            Handles.color = Red;
            foreach (var s in Strokes) if (s.Count > 1) Handles.DrawAAPolyLine(4f, s.Select(p => (Vector3)p).ToArray());
        }

        static void DrawPins(SceneView sv)
        {
            Styles();
            var cam = sv.camera;
            for (int i = 0; i < pins.Count; i++)
            {
                var p = pins[i];
                if (!p.point.HasValue || p.Done && HideDone) continue;
                if (cam.WorldToViewportPoint(p.point.Value).z <= 0) continue;
                var gp = HandleUtility.WorldToGUIPoint(p.point.Value);
                if (Event.current.type == EventType.Repaint)
                {
                    Handles.color = new Color(0, 0, 0, 0.5f); Handles.DrawSolidDisc(gp + new Vector2(1, 1), Vector3.forward, 11f);
                    Handles.color = p.Done ? PinDone : p.Fixed ? PitCrewPlanWindow.Green : PinOpen; Handles.DrawSolidDisc(gp, Vector3.forward, 10f);
                    if (bubble != null && bubble.id == p.id) { Handles.color = PinSel; Handles.DrawWireDisc(gp, Vector3.forward, 12f); }
                }
                pinStyle.normal.textColor = p.Open ? Color.black : Color.white;   // 흰 핀엔 검은 번호
                GUI.Label(new Rect(gp.x - 12, gp.y - 10, 24, 20), (i + 1).ToString(), pinStyle);
            }
        }

        static void DrawBubble(SceneView sv)
        {
            if (!bubble.point.HasValue) { bubble = null; return; }
            float ppp = EditorGUIUtility.pixelsPerPoint, pw = sv.camera.pixelWidth / ppp, ph = sv.camera.pixelHeight / ppp;
            var gp = HandleUtility.WorldToGUIPoint(bubble.point.Value);
            var tex = Thumb(bubble);
            bool reply = bubble.thread.Count > 1;
            var r = new Rect(gp.x + 16, gp.y - 12, 300, (tex != null ? 168 : 0) + 96 + (reply ? 34 : 0));
            r.x = Mathf.Min(r.x, pw - r.width - 6); r.y = Mathf.Clamp(r.y, 4, ph - r.height - 40);
            bubbleRect = r;
            if (Event.current.type == EventType.Repaint && bubble.Fixed) EditorGUI.DrawRect(new Rect(r.x - 4, r.y, 4, r.height), PitCrewPlanWindow.Green);
            GUILayout.BeginArea(r, EditorStyles.helpBox);
            GUILayout.Label(bubbleNo + ". " + PitCrewPlanWindow.Stamp(bubble.time) + "  " + bubble.building + (bubble.Fixed ? "  ✅ 고쳤음" : bubble.Done ? "  (끝)" : "  할 일"), EditorStyles.miniBoldLabel);
            GUILayout.Label(bubble.comment, EditorStyles.wordWrappedLabel, GUILayout.MaxHeight(40));
            if (reply) GUILayout.Label("↳ " + PitCrewPlanWindow.Who(bubble.Last.who) + ": " + PitCrewPlanWindow.Two(bubble.Last.text), EditorStyles.wordWrappedMiniLabel, GUILayout.MaxHeight(30));
            var shotRel = bubble.Fixed && bubble.AfterShot != null ? bubble.AfterShot : bubble.BeforeShot;
            if (tex != null && GUILayout.Button(tex, GUIStyle.none, GUILayout.Width(288), GUILayout.Height(162)))
                EditorUtility.OpenWithDefaultApp(PlanIO.ShotPath(shotRel));
            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                if (bubble.Fixed && GUILayout.Button("넘기기")) PitCrewPlanWindow.Pass(bubble);
                if (bubble.Fixed && GUILayout.Button("다시")) PitCrewPlanWindow.Again(bubble, GUIUtility.GUIToScreenPoint(Event.current.mousePosition));
                if (GUILayout.Button("고르기")) Go(bubble);
                if (GUILayout.Button("닫기")) bubble = null;
            }
            GUILayout.EndArea();
        }

        static Texture2D Thumb(PlanIO.Comment c) => Thumb(c.Fixed && c.AfterShot != null ? c.AfterShot : c.BeforeShot);
        /// 스샷 썸네일(경로 규칙 = PlanIO.ShotPath). 없으면 null.
        public static Texture2D Thumb(string rel)
        {
            if (string.IsNullOrEmpty(rel)) return null;
            if (thumbs.TryGetValue(rel, out var t) && t != null) return t;
            var f = PlanIO.ShotPath(rel);
            if (!File.Exists(f)) return null;
            t = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave }; t.LoadImage(File.ReadAllBytes(f));
            thumbs[rel] = t; return t;
        }

        // ───────── 팔레트 ─────────
        static void PlacePrefab(Vector2 gp)
        {
            Physics.SyncTransforms();
            if (!Physics.Raycast(HandleUtility.GUIPointToWorldRay(gp), out var hit, 5000f, ~0, QueryTriggerInteraction.Ignore)) { Debug.Log("[기획] 그 자리에 땅이 없음"); return; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArmedPrefab);
            var parent = PlanIO.FindByPath("TownV6_Gray/D198_Buildings");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(go, "팔레트 배치");
            if (parent != null) go.transform.SetParent(parent.transform, true);
            go.transform.position = hit.point;
            PlanIO.Ground(go, out _);
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            ArmedPrefab = null;
        }

        /// <summary>그 코멘트의 물건을 고르고 씬 뷰를 그때 시점으로.</summary>
        public static void Go(PlanIO.Comment c)
        {
            var go = PlanIO.FindByPath(c.nodePath);
            if (go != null) { Selection.activeGameObject = go; EditorGUIUtility.PingObject(go); }
            var sv = SceneView.lastActiveSceneView;
            if (sv == null) return;
            if (c.camPos.HasValue && c.camTarget.HasValue && (c.camTarget.Value - c.camPos.Value).sqrMagnitude > 1e-4f)
            {
                var dir = c.camTarget.Value - c.camPos.Value;
                sv.LookAt(c.camTarget.Value, Quaternion.LookRotation(dir), dir.magnitude * Mathf.Tan(sv.cameraSettings.fieldOfView * 0.5f * Mathf.Deg2Rad), false, true);
            }
            else if (c.point.HasValue) sv.LookAt(c.point.Value);
            else if (go != null) sv.Frame(PlanIO.TryBounds(go, out var b) ? b : new Bounds(go.transform.position, Vector3.one * 4), false);
        }
    }
}
