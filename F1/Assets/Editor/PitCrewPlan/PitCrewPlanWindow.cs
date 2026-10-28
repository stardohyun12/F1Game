using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PitCrewPlan
{
    /// <summary>
    /// PitCrew/기획 창 — 코멘트 목록만(남은 것만/전부, 클릭 = 그때 시점으로). 코멘트 쓰기·그리기·배치는 씬 뷰 오버레이(PlanMode).
    /// 창이 열려 있으면 에디터 잠금(PlanMode.Tick). 사용법: docs/design/tutorial/UNITY-PLAN-WINDOW.md
    /// </summary>
    public class PitCrewPlanWindow : EditorWindow
    {
        bool showDone;
        Vector2 scroll;
        List<PlanIO.Comment> comments = new List<PlanIO.Comment>();

        [MenuItem("PitCrew/기획 창")]
        public static PitCrewPlanWindow Open()
        {
            var w = GetWindow<PitCrewPlanWindow>("PitCrew 기획");
            w.minSize = new Vector2(300, 240);
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

        void OnEnable() { Reload(); EditorApplication.update += Poll; }
        void OnDisable() { EditorApplication.update -= Poll; }
        void OnFocus() => Reload();
        void Reload() => comments = PlanIO.ReadComments(SceneName);

        double last;
        void Poll() { if (EditorApplication.timeSinceStartup - last < 2) return; last = EditorApplication.timeSinceStartup; Reload(); Repaint(); }

        // 0 = 전부(끝난 것 빼고), 1 = 고쳤음만, 2 = 할 일만
        int filter;
        public static readonly Color Green = new Color(0.25f, 0.72f, 0.35f), Gray = new Color(0.55f, 0.55f, 0.55f);
        static GUIStyle text, faint, tag;

        static void Styles()
        {
            if (text != null) return;
            text = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = false }; text.normal.textColor = Color.white; text.hover.textColor = Color.white;
            faint = new GUIStyle(EditorStyles.wordWrappedMiniLabel); faint.normal.textColor = Gray; faint.hover.textColor = Gray;
            tag = new GUIStyle(EditorStyles.miniBoldLabel); tag.normal.textColor = Color.white;
        }

        public static string Stamp(string t) => t != null && t.Length >= 16 ? t.Substring(5, 11).Replace('T', ' ') : "";
        /// 마지막 글 2줄까지.
        public static string Two(string s) { if (s == null) return ""; var l = s.Split('\n'); var r = string.Join("\n", l.Take(2)); return r.Length > 140 ? r.Substring(0, 140) + "…" : r; }

        /// [다시]: 그 자리에 입력 팝업(한글 IME) → Enter 면 director 글 + open.
        public static void Again(PlanIO.Comment c, Vector2 screenPos)
        {
            var id = c.id;
            PlanInputPopup.OpenReply(screenPos, "다시: " + Two(c.comment).Split('\n')[0], t => { PlanIO.SetStatus(id, "open", t); PlanMode.RefreshPins(true); foreach (var w in Resources.FindObjectsOfTypeAll<PitCrewPlanWindow>()) { w.Reload(); w.Repaint(); } });
        }
        public static void Pass(PlanIO.Comment c) { PlanIO.SetStatus(c.id, "done"); PlanMode.RefreshPins(true); foreach (var w in Resources.FindObjectsOfTypeAll<PitCrewPlanWindow>()) { w.Reload(); w.Repaint(); } }

        void OnGUI()
        {
            Styles();
            int nFixed = comments.Count(c => c.Fixed), nOpen = comments.Count(c => c.Open);
            using (new EditorGUILayout.HorizontalScope())
            {
                filter = GUILayout.Toolbar(filter, new[] { "전부", "✅ 고쳤음(검토 대기) " + nFixed, "할 일 " + nOpen });
                showDone = GUILayout.Toggle(showDone, "끝난 것 보기", GUILayout.Width(90));
            }
            foreach (var w in PlanIO.GroundWarnings) EditorGUILayout.HelpBox(w, MessageType.Warning);
            if (comments.Count == 0) EditorGUILayout.HelpBox("씬 뷰 위 [💬 코멘트](C)를 켜고 물건이나 땅을 클릭하세요.", MessageType.None);
            using (var s = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = s.scrollPosition;
                for (int i = comments.Count - 1; i >= 0; i--)
                {
                    var c = comments[i];
                    if (c.Done && !showDone) continue;
                    if (filter == 1 && !c.Fixed || filter == 2 && !c.Open) continue;
                    if (c.IsPlacement) { if (filter == 0) EditorGUILayout.LabelField((i + 1) + ".  " + Stamp(c.time) + "  " + c.comment, faint); continue; }
                    Row(i, c);
                }
            }
            var owner = PlanIO.LockOwner();
            EditorGUILayout.LabelField((PlanIO.IsStudio ? "[studio.lock] " : "") + (owner == null ? "에디터 잠금: 없음" : owner.StartsWith("director") ? "에디터 잠금: 도현" : "에디터 잠금: 팀원 " + owner + " 작업 중"), EditorStyles.miniLabel);
        }

        public static string Who(string w) => w == "director" ? "도현" : string.IsNullOrEmpty(w) ? "팀원" : w;

        void Row(int i, PlanIO.Comment c)
        {
            var r = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (Event.current.type == EventType.Repaint && c.Fixed) EditorGUI.DrawRect(new Rect(r.x, r.y, 5, r.height), Green);
            using (new EditorGUILayout.HorizontalScope())
            {
                tag.normal.textColor = c.Fixed ? Green : c.Done ? Gray : Color.white;
                GUILayout.Label(c.Fixed ? "✅ 고쳤음" : c.Done ? "끝" : "할 일", tag, GUILayout.Width(64));
                GUILayout.Label((i + 1) + ".  " + Stamp(c.time) + "  " + (c.building ?? ""), c.Done ? faint : EditorStyles.miniLabel);
            }
            if (GUILayout.Button(c.comment ?? "", c.Done ? faint : text)) PlanMode.Go(c);
            if (c.thread.Count > 1)
            {
                var last = c.Last;
                faint.normal.textColor = c.Fixed ? Green : Gray;
                GUILayout.Label("↳ " + Who(last.who) + ": " + Two(last.text) + (c.thread.Count > 2 ? "   (글 " + c.thread.Count + ")" : ""), faint);
                faint.normal.textColor = Gray;
            }
            if (c.Fixed)
            {
                var b = PlanMode.Thumb(c.BeforeShot); var a = PlanMode.Thumb(c.AfterShot);
                if (b != null || a != null)
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        Shot("전", b, c.BeforeShot); Shot("후", a, c.AfterShot); GUILayout.FlexibleSpace();
                    }
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("넘기기", GUILayout.Width(64))) Pass(c);
                    if (GUILayout.Button("다시", GUILayout.Width(64))) Again(c, GUIUtility.GUIToScreenPoint(Event.current.mousePosition));
                }
            }
            EditorGUILayout.EndVertical();
        }

        static void Shot(string label, Texture2D t, string rel)
        {
            if (t == null) return;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(164)))
            {
                GUILayout.Label(label, EditorStyles.miniLabel);
                if (GUILayout.Button(t, GUIStyle.none, GUILayout.Width(160), GUILayout.Height(90))) EditorUtility.OpenWithDefaultApp(PlanIO.ShotPath(rel));
            }
        }
    }
}
