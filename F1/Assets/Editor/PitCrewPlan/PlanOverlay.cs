using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace PitCrewPlan
{
    /// <summary>씬 뷰 오버레이: [💬 코멘트] 토글(C) · 건물 놓기 드롭다운 · 처리된 핀 숨김.</summary>
    [Overlay(typeof(SceneView), "pitcrew-plan", "PitCrew 기획", true)]
    public class PlanOverlay : IMGUIOverlay
    {
        public override void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                bool on = GUILayout.Toggle(PlanMode.On, "💬 코멘트 (C)", "Button", GUILayout.Width(110));
                if (on != PlanMode.On) PlanMode.On = on;

                var paths = PlanIO.LibraryPrefabs();
                var names = new[] { "건물 놓기…" }.Concat(paths.Select(Path.GetFileNameWithoutExtension)).ToArray();
                int cur = paths.IndexOf(PlanMode.ArmedPrefab) + 1;
                int sel = EditorGUILayout.Popup(cur, names, GUILayout.Width(100));
                if (sel != cur) { PlanMode.ArmedPrefab = sel == 0 ? null : paths[sel - 1]; SceneView.RepaintAll(); }

                bool hide = GUILayout.Toggle(PlanMode.HideDone, "처리된 핀 숨김");
                if (hide != PlanMode.HideDone) PlanMode.HideDone = hide;
            }
        }
    }
}
