var h = PackTable2.HeldNow; var rb = h.GetComponent<Rigidbody>();
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
d.Player.simMove = Vector2.zero; d.Player.simActive = false; d.Player.Cam.simActive = false;
return $"interp={rb.interpolation} local={h.localPosition} held={h.position} cam={Camera.main.transform.position} dist={Vector3.Distance(h.position, Camera.main.transform.position):F3}";
