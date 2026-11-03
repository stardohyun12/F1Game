try { return ParcelBuild.Run(); } catch (System.Exception e) { return "EXC " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace; }
