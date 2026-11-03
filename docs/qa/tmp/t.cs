
// tools/world/gen.py 가 만든 에디터 exec 조각 — 에셋이 있으면 그대로(같은 guid) 덮어쓰고, 없으면 만든다.
var dir = "D:/Github/F1Game/tools/world/_out";
if (UnityEditor.EditorApplication.isPlaying) return "FAIL: Play 중";
if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/World")) UnityEditor.AssetDatabase.CreateFolder("Assets", "World");
var sb = new System.Text.StringBuilder();
var types = new System.Type[] { typeof(WorldPlaces), typeof(WorldPeople), typeof(WorldOrders), typeof(WorldNumbers) };
foreach (var ty in types)
return "end";
