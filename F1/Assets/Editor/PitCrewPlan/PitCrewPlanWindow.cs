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
    /// PitCrew/기획 창 — 웹 기획 뷰어(tools/viewer/plan)를 Unity 안으로. 씬이 유일한 진실.
    /// 고른 물건 좌표 · 코멘트 보내기(스샷+jsonl) · 코멘트 목록 · 배치 기록(placements.yaml) · 접지 · 라이브러리 팔레트 · 에디터 잠금.
    /// 사용법: docs/design/tutorial/UNITY-PLAN-WINDOW.md
    /// </summary>
    public class PitCrewPlanWindow : EditorWindow
    {
        string comment = "";
        bool useGameView;
        bool showDone;
        Vector2 listScroll, paletteScroll;
        List<PlanIO.Comment> comments = new List<PlanIO.Comment>();
        string status = "";
        string armedPrefab;
        double lastLock;
        bool showPalette = true;

        [MenuItem("PitCrew/기획 창")]
        public static PitCrewPlanWindow Open()
        {
            var w = GetWindow<PitCrewPlanWindow>("PitCrew 기획");
            w.minSize = new Vector2(360, 480);
            return w;
        }

        [MenuItem("PitCrew/기획/튜토리얼 물건 치우기 (D-201)")]
        static void HoldTutorialMenu()
        {
            int n = PlanIO.HoldTutorial();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[기획] " + PlanIO.HoldName + " 아래로 " + n + "개 옮김(비활성). 되돌리기: Ctrl+Z 또는 자식을 원래 부모로 끌기.");
        }

        static string SceneName => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        void OnEnable()
        {
            EditorApplication.update += Tick;
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += Repaint;
            Reload();
        }
        void OnDisable()
        {
            EditorApplication.update -= Tick;
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= Repaint;
            PlanIO.ReleaseLock();
        }
        void OnFocus() => Reload();

        void Reload() => comments = PlanIO.ReadComments(SceneName);

        // 창이 열려 있고 에디터가 앞에 있으면 30초마다 잠금 갱신 — 팀원 [EDITOR] 과제가 이걸 보고 기다린다
        void Tick()
        {
            if (!InternalEditorUtility.isApplicationActive) return;
            if (EditorApplication.timeSinceStartup - lastLock < 30 && lastLock > 0) return;
            lastLock = EditorApplication.timeSinceStartup;
            PlanIO.TouchLock();
            Repaint();
        }

        void OnGUI()
        {
            DrawSelection();
            EditorGUILayout.Space(4);
            DrawSend();
            EditorGUILayout.Space(4);
            DrawTools();
            EditorGUILayout.Space(4);
            DrawPalette();
            EditorGUILayout.Space(4);
            DrawComments();
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
            var owner = PlanIO.LockOwner();
            EditorGUILayout.LabelField(owner == null ? "에디터 잠금: 없음" : owner.StartsWith("director") ? "에디터 잠금: 도현(이 창)" : "에디터 잠금: 팀원 " + owner + " 작업 중 — 손대면 충돌", EditorStyles.miniLabel);
        }

        void DrawSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null || !go.scene.IsValid()) { EditorGUILayout.LabelField("고른 물건 없음 — 화면 전체에 대한 코멘트", EditorStyles.boldLabel); return; }
            var t = go.transform;
            EditorGUILayout.LabelField(go.name + "  (" + PlanIO.BuildingName(go) + ")", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel("x " + PlanIO.F(t.position.x) + "   y " + PlanIO.F(t.position.y) + "   z " + PlanIO.F(t.position.z) + "   yaw " + PlanIO.F(PlanIO.Yaw(t), 1), GUILayout.Height(18));
        }

        void DrawSend()
        {
            comment = EditorGUILayout.TextArea(comment, GUILayout.MinHeight(54));
            using (new EditorGUILayout.HorizontalScope())
            {
                useGameView = GUILayout.Toolbar(useGameView ? 1 : 0, new[] { "씬 뷰 캡처", "게임 뷰 캡처" }) == 1;
                GUI.enabled = comment.Trim().Length > 0;
                if (GUILayout.Button("보내기", GUILayout.Width(80))) { Send(comment.Trim(), useGameView); comment = ""; GUI.FocusControl(null); }
                GUI.enabled = true;
            }
        }

        /// <summary>코멘트 한 건: 스샷 PNG + jsonl 한 줄. 웹 뷰어 줄과 같은 필드 이름(좌표는 Unity 월드).</summary>
        public string Send(string text, bool gameView)
        {
            var scene = SceneName;
            var now = DateTime.Now;
            var id = "unity-" + scene + "-" + now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
            var sv = SceneView.lastActiveSceneView;
            Camera src = gameView ? (Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>()) : sv != null ? sv.camera : null;
            var shotRel = "shots/" + id + ".png";
            bool shot = PlanIO.RenderPng(src, Path.Combine(PlanIO.FeedbackDir, shotRel));

            var sel = Selection.gameObjects.Where(g => g.scene.IsValid()).ToList();
            var nodes = string.Join(",", sel.Select(g => "{\"node\":" + PlanIO.Q(g.name) + ",\"path\":" + PlanIO.Q(PlanIO.PathOf(g.transform)) + ",\"building\":" + PlanIO.Q(PlanIO.BuildingName(g)) + ",\"point\":" + PlanIO.Arr(g.transform.position) + ",\"yaw\":" + PlanIO.F(PlanIO.Yaw(g.transform), 1) + "}"));
            Vector3 point;
            if (sel.Count > 0) point = PlanIO.TryBounds(sel[0], out var b) ? b.center : sel[0].transform.position;
            else point = sv != null ? sv.pivot : Vector3.zero;
            string cam = "null";
            if (src != null)
            {
                var target = gameView ? src.transform.position + src.transform.forward * 20f : sv.pivot;
                cam = "{\"pos\":" + PlanIO.Arr(src.transform.position) + ",\"target\":" + PlanIO.Arr(target) + ",\"mode\":" + (gameView ? "\"game\"" : "\"scene\"") + ",\"fov\":" + PlanIO.F(src.fieldOfView, 1) + "}";
            }
            var line = "{\"id\":" + PlanIO.Q(id) + ",\"time\":" + PlanIO.Q(PlanIO.Now()) + ",\"building\":" + PlanIO.Q(sel.Count > 0 ? PlanIO.BuildingName(sel[0]) : scene)
                + ",\"node\":" + PlanIO.Q(sel.Count > 0 ? sel[0].name : null) + ",\"nodes\":[" + nodes + "],\"point\":" + PlanIO.Arr(point) + ",\"point_source\":" + (sel.Count > 0 ? "\"selection\"" : "\"view-center\"")
                + ",\"space\":\"unity-world\",\"scene\":" + PlanIO.Q(scene) + ",\"camera\":" + cam + ",\"comment\":" + PlanIO.Q(text) + ",\"screenshot\":" + (shot ? PlanIO.Q(shotRel) : "null") + ",\"status\":\"open\"}";
            PlanIO.AppendLine(scene, line);
            status = "보냄 " + id + (shot ? "" : " (스샷 실패)");
            Reload(); Repaint();
            return id;
        }

        void DrawTools()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("배치 기록")) RecordPlacements();
                GUI.enabled = Selection.gameObjects.Length > 0;
                if (GUILayout.Button("고른 것 접지")) GroundSelection();
                GUI.enabled = true;
                if (GUILayout.Button("튜토리얼 치우기")) HoldTutorialMenu();
            }
        }

        public string RecordPlacements()
        {
            var delta = PlanIO.WritePlacements(SceneName, out var err);
            status = err ?? (delta.Count == 0 ? "배치 기록: 바뀐 것 없음 (placements.yaml 그대로)" : "배치 기록 " + delta.Count + "건\n" + string.Join("\n", delta));
            Reload(); Repaint();
            return status;
        }

        void GroundSelection()
        {
            var lines = new List<string>();
            foreach (var g in Selection.gameObjects.Where(g => g.scene.IsValid()))
                lines.Add(g.name + (PlanIO.Ground(g, out var dy) ? " " + (dy >= 0 ? "+" : "") + PlanIO.F(dy) + "m" : " 아래 땅 없음"));
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            status = "접지: " + string.Join(", ", lines);
        }

        void DrawPalette()
        {
            showPalette = EditorGUILayout.Foldout(showPalette, string.IsNullOrEmpty(armedPrefab) ? "라이브러리 팔레트" : "라이브러리 팔레트 — 씬 뷰를 클릭하면 놓음 (Esc 취소)", true);
            if (!showPalette) return;
            using (var s = new EditorGUILayout.ScrollViewScope(paletteScroll, GUILayout.MaxHeight(90)))
            {
                paletteScroll = s.scrollPosition;
                using (new EditorGUILayout.HorizontalScope())
                {
                    foreach (var p in PlanIO.LibraryPrefabs())
                    {
                        var name = Path.GetFileNameWithoutExtension(p);
                        bool on = armedPrefab == p;
                        bool now = GUILayout.Toggle(on, name, "Button", GUILayout.MinWidth(70));
                        if (now != on) armedPrefab = now ? p : null;
                    }
                }
            }
        }

        void OnSceneGUI(SceneView sv)
        {
            if (string.IsNullOrEmpty(armedPrefab)) return;
            var e = Event.current;
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { armedPrefab = null; e.Use(); Repaint(); return; }
            if (e.type != EventType.MouseDown || e.button != 0 || e.alt) return;
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Physics.SyncTransforms();
            if (!Physics.Raycast(ray, out var hit, 5000f, ~0, QueryTriggerInteraction.Ignore)) { status = "그 자리에 땅이 없음"; e.Use(); Repaint(); return; }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(armedPrefab);
            var parent = PlanIO.FindByPath("TownV6_Gray/D198_Buildings");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent != null ? parent.scene : UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Undo.RegisterCreatedObjectUndo(go, "팔레트 배치");
            if (parent != null) go.transform.SetParent(parent.transform, true);
            go.transform.position = hit.point;
            PlanIO.Ground(go, out var dy);
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
            status = "놓음 " + go.name + " (" + PlanIO.F(go.transform.position.x) + ", " + PlanIO.F(go.transform.position.z) + ") — 배치 기록을 눌러야 yaml 에 남음";
            armedPrefab = null; e.Use(); Repaint();
        }

        void DrawComments()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("코멘트 (" + comments.Count(c => !c.Done) + " 남음 / " + comments.Count + ")", EditorStyles.boldLabel);
                showDone = GUILayout.Toolbar(showDone ? 1 : 0, new[] { "남은 것만", "전부" }, GUILayout.Width(140)) == 1;
                if (GUILayout.Button("↻", GUILayout.Width(24))) Reload();
            }
            using (var s = new EditorGUILayout.ScrollViewScope(listScroll))
            {
                listScroll = s.scrollPosition;
                foreach (var c in Enumerable.Reverse(comments))
                {
                    if (c.Done && !showDone) continue;
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        var label = (c.time != null && c.time.Length >= 16 ? c.time.Substring(5, 11).Replace('T', ' ') : "") + "  " + (c.building ?? "") + (c.Done ? "  ✓" : "") + "\n" + c.comment;
                        if (GUILayout.Button(label, EditorStyles.wordWrappedLabel)) Go(c);
                        if (!c.Done && GUILayout.Button("처리됨", GUILayout.Width(56))) { PlanIO.SetStatus(SceneName, c.id, "done"); Reload(); }
                    }
                }
            }
        }

        /// <summary>그 코멘트의 물건을 고르고 씬 뷰를 그때 시점으로.</summary>
        void Go(PlanIO.Comment c)
        {
            var go = PlanIO.FindByPath(c.nodePath);
            if (go != null) { Selection.activeGameObject = go; EditorGUIUtility.PingObject(go); }
            var sv = SceneView.lastActiveSceneView;
            if (sv != null && c.camPos.HasValue && c.camTarget.HasValue)
            {
                var dir = c.camTarget.Value - c.camPos.Value;
                if (dir.sqrMagnitude > 1e-4f)
                    sv.LookAt(c.camTarget.Value, Quaternion.LookRotation(dir), dir.magnitude * Mathf.Tan(sv.cameraSettings.fieldOfView * 0.5f * Mathf.Deg2Rad), false, true);
            }
            else if (go != null && sv != null) sv.Frame(PlanIO.TryBounds(go, out var b) ? b : new Bounds(go.transform.position, Vector3.one * 4), false);
        }
    }
}
