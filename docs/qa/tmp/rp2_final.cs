var s = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(); var r = GameObject.Find("Roadside");
int n = 0; foreach (Transform g in r.transform) foreach (Transform t in g) if (!t.name.StartsWith("_")) n++;
return $"scene {s.path} dirty={s.isDirty} play={EditorApplication.isPlaying} roadside groups={r.transform.childCount} props={n} paintOn={RoadPaintMode.IsOn} handJson={System.IO.File.Exists(RoadPaintData.FilePath)}";
