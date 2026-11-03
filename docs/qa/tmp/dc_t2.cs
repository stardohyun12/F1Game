var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
Vector3 feet = new Vector3(94.0f, 4.0f, -283.3f);
Vector3? cs = d.DebugFindCarSpot(feet, 180f);
Vector3 carPos = cs.HasValue ? cs.Value : feet + new Vector3(3.5f, 0f, 0f);
string r = d.DebugArriveForHandover(0.9f, carPos, 180f, feet, 180f);
return "car=" + carPos.ToString("F1") + " | " + r + " state=" + d.State + " carrying=" + d.CarryingBox + " day=" + DayLog.Day + " visits=" + DayLog.Visits;
