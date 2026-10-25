var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var ob=UnityEngine.Object.FindFirstObjectByType<OrderBoard>();
typeof(OrderBoard).GetMethod("OnCardClicked",bf).Invoke(ob,new object[]{0});
UnityEngine.ScreenCapture.CaptureScreenshot("D:/Github/F1Game/docs/qa/evidence/20261023-flow-1-board.png");
return (Services.Run as RunController).CurrentStage+"";
