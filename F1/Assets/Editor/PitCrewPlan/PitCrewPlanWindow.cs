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

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("코멘트 " + comments.Count(c => !c.Done) + " 남음 / " + comments.Count, EditorStyles.boldLabel);
                showDone = GUILayout.Toolbar(showDone ? 1 : 0, new[] { "남은 것만", "전부" }, GUILayout.Width(130)) == 1;
            }
            if (comments.Count == 0) EditorGUILayout.HelpBox("씬 뷰 위 [💬 코멘트](C)를 켜고 물건이나 땅을 클릭하세요.", MessageType.None);
            using (var s = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = s.scrollPosition;
                for (int i = comments.Count - 1; i >= 0; i--)
                {
                    var c = comments[i];
                    if (c.Done && !showDone) continue;
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        var label = (i + 1) + ".  " + (c.time != null && c.time.Length >= 16 ? c.time.Substring(5, 11).Replace('T', ' ') : "") + "  " + (c.building ?? "") + (c.Done ? "  ✓" : "") + "\n" + c.comment;
                        if (GUILayout.Button(label, EditorStyles.wordWrappedLabel)) PlanMode.Go(c);
                        if (!c.Done && GUILayout.Button("처리됨", GUILayout.Width(56))) { PlanIO.SetStatus(SceneName, c.id, "done"); Reload(); PlanMode.RefreshPins(true); }
                    }
                }
            }
            var owner = PlanIO.LockOwner();
            EditorGUILayout.LabelField(owner == null ? "에디터 잠금: 없음" : owner.StartsWith("director") ? "에디터 잠금: 도현" : "에디터 잠금: 팀원 " + owner + " 작업 중", EditorStyles.miniLabel);
        }
    }
}
