Time.timeScale = 1f;
var names = new System.Text.StringBuilder();
for (int i = 0; i < DebugPlaces.All.Length; i++) names.Append(i + ":" + DebugPlaces.All[i].name + " ");
return names + "\n→ " + DebugWindow.GoPlace(12);
