Application.runInBackground = true; Time.timeScale = 1f;
var r = UnityEngine.Object.FindObjectsByType<RoadSurface>(FindObjectsSortMode.None).First(x => x.roadId == "camo_309369480_161");
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb = car.GetComponent<Rigidbody>();
int i = 168; var p = r.centerline[i]; var f = r.centerline[i + 3] - p; f.y = 0;
rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = p + Vector3.up * .6f; rb.rotation = Quaternion.LookRotation(f.normalized); car.transform.SetPositionAndRotation(rb.position, rb.rotation);
typeof(ArcadeCarController).GetField("moveInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(car, new Vector2(0, 1));
var cs = car.GetComponentsInChildren<Collider>().Select(c => $"{c.GetType().Name}:{c.name}:{c.bounds.size} mat:{(c.sharedMaterial ? c.sharedMaterial.name + " f" + c.sharedMaterial.dynamicFriction : "none")}");
var rc = r.GetComponent<Collider>();
return $"placed {p} grade {(r.centerline[i+6].y - p.y)/6f:F2} | {string.Join(" ; ", cs)} | road {(rc ? rc.GetType().Name + " mat " + (rc.sharedMaterial ? rc.sharedMaterial.name + " f" + rc.sharedMaterial.dynamicFriction + " comb " + rc.sharedMaterial.frictionCombine : "none") : "nocol")} wheels {car.GetComponentsInChildren<WheelCollider>().Length}";
