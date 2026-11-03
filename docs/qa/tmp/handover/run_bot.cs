Application.runInBackground = true;
string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/handover/bot"));
System.IO.Directory.CreateDirectory(root);
OnFootRouteBot.GrabDir = "";
return OnFootRouteBot.BeginFirstOrder(root, root) + " " + root;
