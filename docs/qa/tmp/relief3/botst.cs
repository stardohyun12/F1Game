var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
return TownPathBot.Status + " | t " + Time.time.ToString("F0") + " ts " + Time.timeScale + " car " + (car ? car.transform.position.ToString("F0") : "-") + " bots " + UnityEngine.Object.FindObjectsByType<TownPathBot>(FindObjectsSortMode.None).Length + " paused " + UnityEditor.EditorApplication.isPaused;
