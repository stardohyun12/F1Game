var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
UnityEditor.EditorWindow.FocusWindowIfItsOpen(t);
var kb=UnityEngine.InputSystem.Keyboard.current;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F1));
return "queued";
