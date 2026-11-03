var l = SunsetLook.Find();
l.v10.sunAzimuth = __AZ__f;
l.v10.silhouette = new Color(__SIL__);
l.v10.seaPathFocus = new Vector4(__PF__);
l.Apply();
return "az=" + l.v10.sunAzimuth + " sil=" + l.v10.silhouette + " pf=" + l.v10.seaPathFocus;
