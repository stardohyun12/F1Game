using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PitCrewPlan
{
    /// <summary>
    /// 코멘트 입력 팝업. 씬 뷰 Handles.BeginGUI 의 IMGUI TextField 는 한글 IME 조합을 못 받아서 따로 창을 띄운다(UI Toolkit TextField).
    /// Enter = 저장, Shift+Enter = 줄바꿈, Esc = 취소. ShowPopup 이라 씬 뷰를 클릭해도 안 닫힌다(그 사이 그리기 가능).
    /// </summary>
    public class PlanInputPopup : EditorWindow
    {
        public static PlanInputPopup Current;
        TextField field; Label head;
        string label = "";

        public string Text => field != null ? field.value : "";

        public static PlanInputPopup Open(Vector2 screenPos, string label)
        {
            var w = Current;
            if (w == null)
            {
                w = CreateInstance<PlanInputPopup>();
                w.ShowPopup();
                w.position = new Rect(screenPos.x + 12, screenPos.y + 12, 320, 96);   // ShowPopup 뒤에 줘야 먹는다(앞에 주면 0,0)
                Current = w;
            }
            w.SetLabel(label);
            w.FocusField();
            return w;
        }

        /// 테스트용: 입력값을 코드로 넣는다(IME 를 거친 것과 같은 결과).
        public void SetText(string s) { if (field != null) field.SetValueWithoutNotify(s); }

        public void SetLabel(string s) { label = s; if (head != null) head.text = s + "  —  Enter 저장 · Shift+Enter 줄바꿈 · Esc 취소"; }

        public void FocusField()
        {
            Focus();
            Ime();
            rootVisualElement.schedule.Execute(() => field?.Focus());
        }

        static void Ime() { try { Input.imeCompositionMode = IMECompositionMode.On; } catch { /* Input System 전용 설정이면 무시 */ } }

        void OnEnable() { Current = this; }
        void OnFocus() => Ime();
        void OnDestroy() { if (Current == this) Current = null; }

        void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = root.style.paddingRight = root.style.paddingTop = root.style.paddingBottom = 6;
            root.style.borderTopWidth = root.style.borderBottomWidth = root.style.borderLeftWidth = root.style.borderRightWidth = 1;
            root.style.borderTopColor = root.style.borderBottomColor = root.style.borderLeftColor = root.style.borderRightColor = new Color(0.9f, 0.25f, 0.2f);
            head = new Label { style = { fontSize = 10, marginBottom = 3 } };
            field = new TextField { multiline = true, selectAllOnFocus = false, selectAllOnMouseUp = false };   // 다시 포커스될 때 쓰던 글이 통째로 덮이지 않게
            field.style.flexGrow = 1;
            field.style.whiteSpace = WhiteSpace.Normal;
            root.Add(head); root.Add(field);
            SetLabel(label);
            field.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
            root.schedule.Execute(() => field.Focus());
        }

        void OnKey(KeyDownEvent e)
        {
            bool enter = e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || (e.keyCode == KeyCode.None && e.character == '\n');
            if (enter && !e.shiftKey)
            {
                e.StopImmediatePropagation();
                if (e.keyCode == KeyCode.None) return;   // Enter 의 글자 이벤트 — 줄바꿈이 안 들어가게 먹기만
                // 한글 조합 중인 마지막 글자가 값에 확정된 뒤 읽도록 한 박자 미룬다
                EditorApplication.delayCall += () => PlanMode.Submit(Text);
            }
            else if (e.keyCode == KeyCode.Escape)
            {
                e.StopImmediatePropagation();
                EditorApplication.delayCall += PlanMode.CancelInput;
            }
        }
    }
}
