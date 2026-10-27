var log = new System.Text.StringBuilder(); int off = 0, on = 0, nrecv = 0; var offN = new System.Collections.Generic.HashSet<string>();
foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) { if (r.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off) { off++; if (offN.Count < 12) offN.Add(r.transform.parent ? r.transform.parent.name : r.name); } else on++; if (!r.receiveShadows) nrecv++; }
log.Append("cast off " + off + " on " + on + " recvOff " + nrecv + " e.g. " + string.Join(",", offN) + "\n");
var root = GameObject.Find("SceneA4").transform; var sun = root.Find("A7_Sun").GetComponent<Light>();
var gb = new Bounds(); bool f0 = false; foreach (var r in root.Find("garage_v5_combo").GetComponentsInChildren<Renderer>()) { if (!f0) { gb = r.bounds; f0 = true; } else gb.Encapsulate(r.bounds); }
var p = gb.center + Vector3.down * 2f; bool hit = Physics.Raycast(p, -sun.transform.forward, out var h, 50f);
log.Append("garage floor→sun ray hit " + hit + (hit ? " " + h.collider.name + " d " + h.distance.ToString("F1") : "") + " | sun fwd " + sun.transform.forward.ToString("F2") + " sh " + sun.shadows);
return log.ToString();
