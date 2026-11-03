var car = GameObject.FindFirstObjectByType<ArcadeCarController>();
var t = typeof(ArcadeCarController);
var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
var sb = new System.Text.StringBuilder();
foreach (var n in new[]{"airDrag","engineAccelMul","qaForceHandbrake","isGrounded","accelForce","maxSpeedKmh","accelTime","accelFalloff","driveMode","weightInfluence","throttleLag","feelPreset","downforceAccel","coastDrag"})
{
    var f = t.GetField(n, bf); var p = f == null ? t.GetProperty(n, bf) : null;
    object v = f != null ? f.GetValue(car) : p != null ? p.GetValue(car) : "?";
    sb.Append(n + "=" + v + "; ");
}
sb.Append("mass=" + car.GetComponent<Rigidbody>().mass + " lin=" + car.GetComponent<Rigidbody>().linearDamping + " LoadMul=" + car.LoadMul);
return sb.ToString();
