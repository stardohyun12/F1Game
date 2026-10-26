// 2306: 주행 중 화물 흔들림 기록 — 화물 박스(CargoBox)마다 차 기준 기울기 최대·차 기준 속도 최대·내구도 시작/끝. 결과 SessionState "p01.cargo" (주행 끝 bl.drive 가 채워지면 멈춤)
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var boxes = UnityEngine.Object.FindObjectsByType<CargoBox>(FindObjectsSortMode.None);
if (boxes.Length == 0) return "no CargoBox";
var hp0 = boxes.Select(b => b.IntegrityPercent).ToArray();
float tiltMax = 0, relVMax = 0; int frames = 0;
var crb = car.GetComponent<Rigidbody>();
UnityEditor.SessionState.SetString("p01.cargo", "");
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    frames++;
    foreach (var b in boxes)
    {
        if (b == null) continue;
        tiltMax = Mathf.Max(tiltMax, Vector3.Angle(b.transform.up, car.transform.up));
        var brb = b.GetComponent<Rigidbody>(); if (brb != null) relVMax = Mathf.Max(relVMax, (brb.linearVelocity - crb.GetPointVelocity(brb.position)).magnitude);
    }
    if (frames > 30 && UnityEditor.SessionState.GetString("bl.drive", "") != "")
    {
        UnityEditor.EditorApplication.update -= cb;
        var s = "boxes=" + boxes.Length + " tiltMax=" + tiltMax.ToString("F1") + "deg relVMax=" + relVMax.ToString("F2") + "m/s integrity ";
        for (int i = 0; i < boxes.Length; i++) s += (boxes[i] ? boxes[i].name : "x") + ":" + (hp0[i] * 100).ToString("F0") + "->" + (boxes[i] ? boxes[i].IntegrityPercent * 100 : 0).ToString("F0") + "% ";
        UnityEditor.SessionState.SetString("p01.cargo", s);
    }
};
UnityEditor.EditorApplication.update += cb;
return "monitoring " + boxes.Length + " boxes: " + string.Join(",", boxes.Select(b => b.name));
