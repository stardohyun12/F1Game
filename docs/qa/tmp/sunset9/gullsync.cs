// Play: 갈매기 날기 ↔ 울음 동기 상태 한 줄(JSON)
var look = SunsetLook.Find(); var db = look.backdrop.GetComponent<DuskBackdrop>();
var amb = UnityEngine.Object.FindFirstObjectByType<CoastAmbience>(); var cam = Camera.main;
string gv = ""; foreach (Transform c in look.backdrop.transform) if (c.name.StartsWith("Dusk_Gull") && c.gameObject.activeSelf) { var v = cam.WorldToViewportPoint(c.position); gv += "(" + v.x.ToString("F2") + "," + (1 - v.y).ToString("F2") + ")"; }
var cv = cam.WorldToViewportPoint(amb.LastCallPos);
var lis = UnityEngine.Object.FindFirstObjectByType<AudioListener>();
float dW = Vector3.Distance(lis.transform.position, amb.waves.transform.position), dG = Vector3.Distance(lis.transform.position, amb.LastCallPos);
return "{\"t\":" + Time.time.ToString("F2") + ",\"flights\":" + db.Flights + ",\"calls\":" + amb.GullCalls + ",\"lastCall\":" + amb.LastCallTime.ToString("F2")
  + ",\"callVp\":\"" + cv.x.ToString("F2") + "," + (1 - cv.y).ToString("F2") + "\",\"gulls\":\"" + gv + "\",\"leader\":" + (amb.leader == db ? 1 : 0)
  + ",\"dWaves\":" + dW.ToString("F1") + ",\"dCall\":" + dG.ToString("F1") + ",\"wavesPlaying\":" + (amb.waves.isPlaying ? 1 : 0) + "}";
