typeof(RunController).GetMethod("EndRun", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(Services.Run as RunController, new object[]{true});
return (Services.Run as RunController).CurrentStage+"";
