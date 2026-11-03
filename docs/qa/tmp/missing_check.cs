var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
int gos = 0, comps = 0, missing = 0, broken = 0;
var sb = new System.Text.StringBuilder();
foreach (var root in scene.GetRootGameObjects())
{
    foreach (var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        gos++;
        foreach (var c in t.GetComponents<UnityEngine.Component>())
        {
            if (c == null) { missing++; if (missing <= 10) sb.Append(" MISSING@" + t.name); continue; }
            comps++;
            if (!(c is UnityEngine.MonoBehaviour)) continue;
            var so = new UnityEditor.SerializedObject(c);
            var p = so.GetIterator();
            while (p.NextVisible(true))
            {
                if (p.propertyType == UnityEditor.SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0)
                { broken++; if (broken <= 10) sb.Append(" BROKEN@" + t.name + "." + c.GetType().Name + "." + p.propertyPath); }
            }
        }
    }
}
return scene.path + " dirty=" + scene.isDirty + " 오브젝트=" + gos + " 컴포넌트=" + comps + " Missing=" + missing + " 끊긴참조=" + broken + sb;
