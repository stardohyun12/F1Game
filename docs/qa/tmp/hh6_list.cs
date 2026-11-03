var sb = new System.Text.StringBuilder();
var root = GameObject.Find("STUDIO/STUDIO_Slot/house_home");
foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) { var mr = t.GetComponent<Renderer>(); if (!mr) continue; var b = mr.bounds; var lmin = root.transform.InverseTransformPoint(b.min); var lmax = root.transform.InverseTransformPoint(b.max);
  float x0 = Mathf.Min(lmin.x,lmax.x), x1 = Mathf.Max(lmin.x,lmax.x), y0 = Mathf.Min(lmin.y,lmax.y), y1 = Mathf.Max(lmin.y,lmax.y), z0 = Mathf.Min(lmin.z,lmax.z), z1 = Mathf.Max(lmin.z,lmax.z);
  if (y1 < 4.0f || y0 > 7f || x1 < -0.7f || x0 > 4.3f || z0 > 2.9f || z1 < -2.9f) continue;
  if (x1 - x0 > 3f || z1 - z0 > 3f) continue;
  sb.Append(t.name + " p=" + (t.parent ? t.parent.name : "") + " x " + x0.ToString("F2") + ".." + x1.ToString("F2") + ", y " + y0.ToString("F2") + ".." + y1.ToString("F2") + ", z " + z0.ToString("F2") + ".." + z1.ToString("F2") + "\n"); }
return sb.ToString();
