var t = PackTable2.I;
var p = t.receive.today[0];
var bx = t.WorkBox.Box;
t.SimShake(2f);
var go = new UnityEngine.GameObject("ShakeProbe");
var log = new System.Collections.Generic.List<string>();
var mb = go.AddComponent<PackV3Shots>();
mb.StartCoroutine(Probe());
System.Collections.IEnumerator Probe()
{
    for (int i = 0; i < 120; i++)
    {
        yield return new UnityEngine.WaitForFixedUpdate();
        if (i % 10 == 0) log.Add(i + " box=" + bx.transform.position.ToString("F2") + " plocal=" + bx.transform.InverseTransformPoint(p.transform.position).ToString("F3"));
    }
    t.SimUse(t.WorkBox);
    yield return new UnityEngine.WaitForFixedUpdate();
    log.Add("put box=" + bx.transform.position.ToString("F2") + " plocal=" + bx.transform.InverseTransformPoint(p.transform.position).ToString("F3") + " act=" + t.LastAct);
    for (int i = 0; i < 30; i++) yield return new UnityEngine.WaitForFixedUpdate();
    log.Add("after plocal=" + bx.transform.InverseTransformPoint(p.transform.position).ToString("F3"));
    System.IO.File.WriteAllLines("D:/Github/F1Game/docs/qa/tmp/shake.txt", log);
}
return "ok";
