Application.runInBackground = true;
string root = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/depot-orders"));
OnFootRouteBot.GrabDir = System.IO.Path.Combine(root, "grab");
System.IO.Directory.CreateDirectory(OnFootRouteBot.GrabDir);
return OnFootRouteBot.BeginFirstOrder(root, root) + " " + root;
