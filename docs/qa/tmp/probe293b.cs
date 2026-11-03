var h = PackTable2.HeldNow; var rb = h.GetComponent<Rigidbody>();
h.localPosition = Vector3.zero; h.localRotation = Quaternion.identity;
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
d.Player.simActive = true; d.Player.Cam.simActive = true; d.Player.simMove = new Vector2(0f, 1f);
return $"set: interp={rb.interpolation} local={h.localPosition} cam={Camera.main.transform.position}";
