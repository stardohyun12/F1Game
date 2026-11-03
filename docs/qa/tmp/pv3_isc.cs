var t = System.Type.GetType("PackTable2, PitCrew.Interactables");
var h = System.Type.GetType("PackHands, PitCrew.Presentation");
var r = System.Type.GetType("PackRules, PitCrew.Core");
return (UnityEditor.EditorApplication.isCompiling ? "compiling " : "idle ") + (t != null) + " " + (h != null) + " " + (r != null);
