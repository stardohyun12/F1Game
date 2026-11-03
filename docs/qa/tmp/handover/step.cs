// 한 단계: "knock" = 안내가 E 두드리기일 때만 E · "e" = E 한 번 · "away" = 문 반대쪽 보기 · "dknock" = 시선 무관 두드리기
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
string a = System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/handover/step.txt").Trim();
if (a == "knock") { if (d.Prompt != "E 두드리기") return "no prompt '" + d.Prompt + "'"; d.SimInteract(); return "knocked"; }
if (a == "e") { d.SimInteract(); return "E"; }
if (a == "away") { d.Player.Cam.Yaw = 0f; return "look away yaw=0"; }
if (a == "dknock") return "dknock=" + d.DebugKnock();
return "unknown " + a;
