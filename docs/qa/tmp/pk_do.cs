// 같은 Press() 길을 한 호출 안에서 순서대로: LookAt → 카메라 회전 반영 → Gaze → Press
var F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
var h = UnityEngine.Object.FindFirstObjectByType<DepotPackSpot>().Hands;
var T = typeof(PitCrew.Hands.HandsBench);
var steps = System.IO.File.ReadAllText("D:/Github/F1Game/docs/qa/tmp/pk_steps.txt").Trim().Split(' ');
var log = new System.Text.StringBuilder();
foreach (var s in steps)
{
    if (s.Length == 0) continue;
    var kind = (PitCrew.Hands.HandsLayout.Kind)System.Enum.Parse(typeof(PitCrew.Hands.HandsLayout.Kind), s);
    h.LookAt(kind);
    float yaw = (float)T.GetField("yaw", F).GetValue(h), pitch = (float)T.GetField("pitch", F).GetValue(h);
    h.cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    T.GetMethod("Gaze", F).Invoke(h, null);
    T.GetMethod("Press", F).Invoke(h, null);
    log.Append(s + "→" + h.Holding + " ");
}
return log.ToString();
