// QA 0313: 포장 단계 막힘 진단
var sb = new System.Text.StringBuilder();
var rc = Services.Run as RunController;
sb.Append("stage=" + rc.CurrentStage + " runActive=" + rc.RunActive + "\n");
var ot = UnityEngine.Object.FindFirstObjectByType<OpeningTutorial>();
sb.Append("ot=" + (ot ? ot.Stage : "null") + "\n");
var ps = Services.Packing as PitCrew.Packing.PackingScreen;
sb.Append("packing=" + (ps ? ps.name + " active=" + ps.gameObject.activeInHierarchy : "null(" + (Services.Packing == null ? "none" : Services.Packing.GetType().Name) + ")") + "\n");
if (ps)
    foreach (var f in ps.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
    {
        var v = f.GetValue(ps);
        if (v == null || v is string || v.GetType().IsPrimitive || v.GetType().IsEnum) sb.Append("  " + f.Name + "=" + v + "\n");
    }
var cargo = Services.Cargo;
sb.Append("cargo=" + (cargo == null ? "null" : cargo.IntegrityPercent.ToString("F2")) + "\n");
sb.Append("wallet=" + (Services.Upgrade != null ? Services.Upgrade.Wallet : -1));
return sb.ToString();
