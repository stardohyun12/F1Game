var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return "bot=" + OnFootRouteBot.Status + " t=" + Time.time.ToString("F0") + " state=" + d.State + " delivered=" + d.Delivered + " handed=" + d.HandedOver + " resultPanel=" + ResultPanel.ShowCount;
