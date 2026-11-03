var id = "unity-Map_TownV6-20261029-011413-cd08";
var ok = PitCrewPlan.PlanIO.SetStatus("Map_TownV6", id, "done");
var l = PitCrewPlan.PlanIO.ReadComments("Map_TownV6");
return ok + " n=" + l.Count + " done=" + l[0].Done + " path=" + l[0].nodePath + " cam=" + l[0].camPos + " line=" + l[0].raw.Substring(l[0].raw.Length - 40);
