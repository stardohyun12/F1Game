var t=System.Type.GetType("LowResRenderTest, PitCrew"); var c=Camera.main.GetComponent(t) as Behaviour;
return "frame="+Time.frameCount+" dt="+(Time.unscaledDeltaTime*1000).ToString("F1")+"ms compEnabled="+c.enabled+" h="+t.GetField("targetHeight").GetValue(c)+" result="+UnityEditor.SessionState.GetString("qalr","");
