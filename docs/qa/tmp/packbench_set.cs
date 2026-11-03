// pb_next.txt 의 줄마다 "시나리오 찍기여부(1/0)" — 부를 때마다 다음 줄(pb.idx)로. 셸 인용 없이 시나리오를 바꾸려고
var lines = System.IO.File.ReadAllLines("D:/Github/F1Game/docs/qa/tmp/pb_next.txt");
int idx = UnityEditor.SessionState.GetInt("pb.idx", 0);
if (idx >= lines.Length) return "queue empty";
UnityEditor.SessionState.SetInt("pb.idx", idx + 1);
var line = lines[idx].Trim().Split(' ');
UnityEditor.SessionState.SetString("pb.scenario", line[0]);
UnityEditor.SessionState.SetString("pb.dir", line.Length > 1 && line[1] == "1" ? "D:/Github/F1Game/docs/qa/evidence/packbench-v1" : "");
return "set " + line[0] + " shots=" + (line.Length > 1 ? line[1] : "0");
