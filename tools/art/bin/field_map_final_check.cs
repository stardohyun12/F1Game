var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
int gos=0,meshes=0,mats=0;
foreach(var o in Resources.FindObjectsOfTypeAll<GameObject>())if(o.name.StartsWith("_FIELD2354")||o.name=="_FIELD_MAP_REVIEW_2354"||o.name=="_FieldMapBaseline")gos++;
foreach(var o in Resources.FindObjectsOfTypeAll<Mesh>())if(o.name.StartsWith("_FIELD2354_"))meshes++;
foreach(var o in Resources.FindObjectsOfTypeAll<Material>())if(o.name.StartsWith("_FIELD2354_"))mats++;
return "scene="+s.path+" dirty="+s.isDirty+" playing="+Application.isPlaying+" compiling="+UnityEditor.EditorApplication.isCompiling+" tempGameObjects="+gos+" tempMeshes="+meshes+" tempMaterials="+mats;
