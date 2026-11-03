var sb = new System.Text.StringBuilder();
sb.Append("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + "\n");
foreach (var s in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
  var t = s.transform; string p = t.name; var q = t.parent; while (q != null) { p = q.name + "/" + p; q = q.parent; }
  sb.Append(p + " clip=" + (s.clip ? s.clip.name : "-") + " vol=" + s.volume + " blend=" + s.spatialBlend + " min=" + s.minDistance + " max=" + s.maxDistance + " roll=" + s.rolloffMode + " pos=" + t.position + " mixer=" + (s.outputAudioMixerGroup ? s.outputAudioMixerGroup.name : "-") + " active=" + s.gameObject.activeInHierarchy + " comps=");
  foreach (var c in s.GetComponents<Component>()) sb.Append(c.GetType().Name + ",");
  sb.Append("\n");
}
foreach (var w in UnityEngine.Object.FindObjectsByType<WindAmbience>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append("Wind " + w.name + " low=" + w.lowY + " high=" + w.highY + " q=" + w.quietVolume + " l=" + w.loudVolume + " ign=" + w.ignoreRun + " gust=" + w.gust + "\n");
foreach (var c in UnityEngine.Object.FindObjectsByType<CoastAmbience>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append("Coast " + c.name + " pos=" + c.transform.position + " waves=" + (c.waves? c.waves.name:"-") + " gulls=" + (c.gulls?c.gulls.name:"-") + " gr=" + c.gullRadius + "\n");
foreach (var l in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None)) sb.Append("Listener " + l.name + " active=" + l.isActiveAndEnabled + "\n");
return sb.ToString();
