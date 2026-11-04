var T = typeof(PackBoard).Assembly.GetType("PitCrew.PackBench.PackBench");
var bf = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
var pool = (AudioSource[])T.GetField("pool", bf).GetValue(null);
string r = "pool=" + (pool == null ? "null" : pool.Length.ToString());
if (pool != null) for (int i = 0; i < pool.Length; i++) r += "\n[" + i + "] " + (pool[i] == null ? "destroyed" : (pool[i].gameObject.name + " enabled=" + pool[i].enabled + " active=" + pool[i].gameObject.activeInHierarchy + " mute=" + pool[i].mute + " vol=" + pool[i].volume));
foreach (var n in new[] { "sfxHard", "sfxLight", "sfxPick", "sfxClose", "sfxDeny", "sfxDone", "sfxBubble" }) { var c = (AudioClip)T.GetField(n, bf).GetValue(null); r += "\n" + n + "=" + (c == null ? "NULL" : c.name + " " + c.length.ToString("F2") + "s state=" + c.loadState); }
r += "\nRattlePlayed=" + T.GetField("RattlePlayed", bf).GetValue(null) + " paused=" + AudioListener.pause + " listenerVol=" + AudioListener.volume + " listeners=" + UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length;
// 지금 직접 한 번 쳐 본다
var play = T.GetMethod("PlayRattle", bf);
return r;
