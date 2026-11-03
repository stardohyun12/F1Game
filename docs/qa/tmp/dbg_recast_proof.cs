var d=UnityEngine.Object.FindFirstObjectByType<OnFootDirector>(); var s=d.DebugSpot;
bool before=s.Landed; string r=DebugWindow.Recast(); bool after=s.Landed;
return "before.Landed="+before+" recast='"+r+"' after.Landed(same frame)="+after+" seated="+s.Seated;
