// 0338: 차 내부 값 — 왜 가속 안 하나(최고속·가속력·배율·접지·단계)
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
object F(string n) { var fi = typeof(ArcadeCarController).GetField(n, bf); if (fi != null) return fi.GetValue(car); var pi = typeof(ArcadeCarController).GetProperty(n, bf); return pi != null ? pi.GetValue(car) : "?"; }
var rc = Services.Run as RunController;
string r = $"stage {(rc != null ? rc.CurrentStage.ToString() : "null")} sameRc {(rc == UnityEngine.Object.FindFirstObjectByType<RunController>())} mode {car.Mode}\n";
foreach (var n in new[] { "isGrounded", "maxSpeedMps", "accelForce", "engineAccelMul", "engineMaxSpeedMul", "EffectiveMaxSpeedMps", "IsDrifting", "smoothedMove", "moveInput", "driveMode", "freeDrive", "qaForceHandbrake" }) r += $"{n}={F(n)} ";
var ts = car.GetComponent<TireSystem>() ?? Services.Tires as TireSystem; r += $"\ntire={(ts != null ? ts.Current + " spd" + ts.SpeedMultiplier + " grip" + ts.GripMultiplier : "null")} boost={(Services.Boost != null ? Services.Boost.IsActive.ToString() : "null")}";
r += $"\nrb mass {rb.mass} drag {rb.linearDamping} useGrav {rb.useGravity} detect {rb.detectCollisions} cons {rb.constraints} pos {rb.position:F2}";
return r;
