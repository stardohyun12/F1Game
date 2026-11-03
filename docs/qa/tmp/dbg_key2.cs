var kb=UnityEngine.InputSystem.Keyboard.current;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb,new UnityEngine.InputSystem.LowLevel.KeyboardState());
return "open="+DebugWindow.IsOpen+" cursor="+Cursor.lockState+" capture="+OnFootCamera.UiCapture;
