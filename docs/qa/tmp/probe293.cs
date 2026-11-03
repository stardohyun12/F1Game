var h = PackTable2.HeldNow; var rb = h.GetComponent<Rigidbody>();
string a = $"before: held={h.position} parent={h.parent.position} local={h.localPosition} interp={rb.interpolation} kin={rb.isKinematic} rb.pos={rb.position}";
rb.interpolation = RigidbodyInterpolation.None;
return a;
