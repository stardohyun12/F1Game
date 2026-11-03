var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
var sb = new System.Text.StringBuilder();
sb.Append("probeDone=" + FrameFlickerProbe.Done + " " + FrameFlickerProbe.Summary + "\n");
var lis = UnityEngine.Object.FindFirstObjectByType<AudioListener>();
var lp = lis != null ? lis.transform.position : Vector3.zero;
sb.Append("listener=" + lp.ToString("F1") + "\n");
var coast = UnityEngine.Object.FindFirstObjectByType<CoastAmbience>();
sb.Append("gullCalls=" + (coast != null ? coast.GullCalls : -1) + "\n");
foreach (var s in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
    if (s.isActiveAndEnabled)
        sb.Append("src " + s.name + " clip=" + (s.clip != null ? s.clip.name : "-") + " playing=" + s.isPlaying + " loop=" + s.loop + " vol=" + s.volume.ToString("F2") + " blend=" + s.spatialBlend.ToString("F1") + " min/max=" + s.minDistance + "/" + s.maxDistance + " dist=" + Vector3.Distance(s.transform.position, lp).ToString("F1") + "\n");
if (FrameFlickerProbe.Done) d.SimInteract();
return sb.ToString();
