var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var t = d.PackSpot.V3Table;
if (d.State != OnFootDirector.Mode.Pack) d.SimInteract();
if (d.State != OnFootDirector.Mode.Pack) t.SimUse(t);
var b = d.PackSpot.Board2D;
return "state=" + d.State + " on=" + (b != null && b.On);
