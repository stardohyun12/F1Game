var m = typeof(OnFootDirector).GetMethod("ClearPreOrderRoute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
return "ClearPreOrderRoute=" + (m != null) + " playing=" + Application.isPlaying;
