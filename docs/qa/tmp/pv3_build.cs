var t = System.Type.GetType("PackV3Builder, PitCrew.Editor");
if (t == null) return "FAIL: builder type 없음";
return (string)t.GetMethod("Build").Invoke(null, null);
