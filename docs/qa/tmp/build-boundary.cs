BuildMapBoundary.BuildAndSave();
var r=GameObject.Find("MapBoundary");
return "built children="+r.transform.childCount+" walls="+r.transform.Find("Walls").childCount+" scene dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
