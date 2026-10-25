UnityEditor.SessionState.SetString("i0226.tag", "before");
return "tag=before scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " ground=" + (GameObject.Find("TerrainMesh")?.GetComponent<Renderer>()?.sharedMaterial?.name ?? "?");
