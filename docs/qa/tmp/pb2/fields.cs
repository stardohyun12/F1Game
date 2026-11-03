var b = PitCrew.PackBench.PackBench.I;
var s = "";
foreach (var n in new[] { "holdMaxSpeed", "holdGain", "holdMaxForce", "holdMaxAccel", "holdBreakDist", "wheelStep", "holdLift", "holdDist" })
{
    var f = typeof(PitCrew.PackBench.PackBench).GetField(n, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    s += n + "=" + (f != null ? f.GetValue(b) : "?") + " ";
}
var rb = b.Tray().Body;
return s + " pos=" + rb.position.ToString("F3") + " v=" + rb.linearVelocity.ToString("F3") + " fixedDt=" + UnityEngine.Time.fixedDeltaTime;