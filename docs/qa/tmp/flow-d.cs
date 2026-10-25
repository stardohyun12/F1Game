var ps=UnityEngine.Object.FindFirstObjectByType<PitCrew.Packing.PackingScreen>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
ps.GetType().GetMethod("OnGoClicked",bf).Invoke(ps,null);
return (Services.Run as RunController).CurrentStage+"";
