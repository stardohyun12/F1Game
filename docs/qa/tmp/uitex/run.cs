// D-258 UI 비교: 봇 첫 주문 1회 + 지정 캡처마다 지금/A/B. 시간대 = dusk.txt (0 오후, 1 노을).
Application.runInBackground = true;
float dusk = float.Parse(System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/uitex/dusk.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
string root = "D:/Github/F1Game/docs/qa/evidence/uitex/" + (dusk > 0.5f ? "dusk" : "aft");
System.IO.Directory.CreateDirectory(root);
DotScreenUI.UiStyle = DotScreenUI.UIStyle.Now;
OnFootRouteBot.GrabDir = "";
OnFootRouteBot.StyleTripleShots = "d_03_board_view d_05_board_picked d_06_slip_on_foot f2_03_talk_1 h_05_greet";
string ds = DebugWindow.SetDusk(dusk);
return OnFootRouteBot.BeginFirstOrder(root, root) + " " + ds + " " + root;
