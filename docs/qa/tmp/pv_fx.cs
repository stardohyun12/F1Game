var view = UnityEngine.Object.FindFirstObjectByType<PackBoardView>(UnityEngine.FindObjectsInactive.Include);
var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var fx = typeof(PackBoardView).GetField("fx", bf).GetValue(view);
var ft = fx.GetType();
var src = (AudioSource)ft.GetField("src", bf).GetValue(fx);
var cb = (AudioClip)ft.GetField("cBubble", bf).GetValue(fx);
string r = "src go=" + src.gameObject.name + " active=" + src.gameObject.activeInHierarchy + " enabled=" + src.enabled + " mute=" + src.mute + " vol=" + src.volume + " bubble=" + (cb == null ? "NULL" : cb.length.ToString("F2") + "s " + cb.loadState + " samples=" + cb.samples);
r += " | isPlaying(now)=" + src.isPlaying;
return r;
