// 유리 HP 가 떨어진 프레임마다: 위치·속도·요레이트·기울기·차 주변 0.3m 안 겹친 콜라이더(도로·지형 제외 안 함)
Application.runInBackground = true;
float t0 = Time.time; string log = ""; float prev = 100; var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var bc = car.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).Select(c => c.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
UnityEditor.EditorApplication.CallbackFunction cb = null;
cb = () =>
{
    if (!Application.isPlaying) { UnityEditor.EditorApplication.update -= cb; return; }
    foreach (var b in UnityEngine.Object.FindObjectsByType<CargoBox>(FindObjectsSortMode.None))
    {
        if (Mathf.Abs(b.IntegrityHP - prev) >= 2f)
        {
            var tr = car.transform;
            var hits = Physics.OverlapBox(tr.position + tr.up * .3f, new Vector3(1.2f, .5f, 2.6f), tr.rotation)
                .Where(c => !c.transform.IsChildOf(tr)).Select(c => c.name).Distinct();
            float tilt = Vector3.Angle(tr.up, Vector3.up);
            log += "t" + (Time.time - t0).ToString("F1") + " " + prev.ToString("F0") + ">" + b.IntegrityHP.ToString("F0")
                + "@(" + rb.position.x.ToString("F1") + "," + rb.position.y.ToString("F1") + "," + rb.position.z.ToString("F1") + ")v" + (rb.linearVelocity.magnitude * 3.6f).ToString("F0")
                + " yaw" + (rb.angularVelocity.y * Mathf.Rad2Deg).ToString("F0") + " tilt" + tilt.ToString("F0") + " vy" + rb.linearVelocity.y.ToString("F1")
                + " ov[" + string.Join(",", hits) + "]; ";
            prev = b.IntegrityHP;
        }
    }
    var r = Services.Run as RunController; if (r.CurrentStage.ToString() == "Result") { UnityEditor.EditorApplication.update -= cb; }
    UnityEditor.SessionState.SetString("p.hp", "final=" + prev.ToString("F0") + " drops: " + log);
};
UnityEditor.EditorApplication.update += cb; return "hp2 watch on";
