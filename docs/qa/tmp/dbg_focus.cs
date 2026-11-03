var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
UnityEditor.EditorWindow.FocusWindowIfItsOpen(t);
return "focused="+Application.isFocused+" focusedWindow="+(UnityEditor.EditorWindow.focusedWindow!=null?UnityEditor.EditorWindow.focusedWindow.GetType().Name:"null");
