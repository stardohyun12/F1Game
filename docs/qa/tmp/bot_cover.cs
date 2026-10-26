Application.runInBackground = true; foreach (var b in UnityEngine.Object.FindObjectsByType<TownPathBot>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(b.gameObject);
TownPathBot.Only = ""; TownPathBot.Status = "starting"; TownPathBot.Begin("cover", 8f); return "started";
