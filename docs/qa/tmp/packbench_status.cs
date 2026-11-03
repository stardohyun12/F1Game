var b = PitCrew.PackBench.PackBench.I;
var cur = UnityEngine.Object.FindFirstObjectByType<PitCrew.PackBench.PackBenchCursor>();
return PitCrew.PackBench.PackBench.BotStatus + " | t=" + UnityEngine.Time.time.ToString("F1") + " cursor=" + (cur != null ? cur.Visible : "none") + " lock=" + UnityEngine.Cursor.lockState + " look=" + (b != null ? b.LookName : "") + " prompt=" + (b != null ? b.Prompt : "") + "\nbegin: " + PitCrew.PackBench.PackBox.LastBeginDump;
