var s = SunsetLook.Find(); var cam = Camera.main; var sb = new System.Text.StringBuilder();
foreach (Transform c in s.backdrop.transform) if (c.name.StartsWith("Dusk_Boat") || c.name.StartsWith("Dusk_Light")) { var v = cam.WorldToViewportPoint(c.position); var r = c.GetComponent<Renderer>(); sb.Append(c.name + " pos=" + c.position.ToString("F1") + " vp=" + v.ToString("F3") + " px=(" + (v.x*640).ToString("F0") + "," + ((1-v.y)*360).ToString("F0") + ") r=" + (r ? r.enabled.ToString() : "-") + "\n"); }
sb.Append("t=" + Time.time.ToString("F1"));
return sb.ToString();
