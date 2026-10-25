// v2: 01 은 항구 직선을 거꾸로(서쪽 크레인 쪽) 보며 왼쪽(남쪽 바다)으로 20° 틈, 05 는 terrace_house 가 가장 몰린 본선 850. 01·06 은 HUD 판도.
UnityEditor.SessionState.SetString("bl.outdir", "bayloop-v2");
UnityEditor.SessionState.SetString("bl.shot",
  "main|45|01_harbor_straight|rev|yaw=-20|hud;" +
  "main|255|02_shop_street_climb;" +
  "main|668|03_hilltop_door;" +
  "main|712|04_hilltop_to_sea_downhill;" +
  "main|850|05_slope_village;" +
  "main|1215|06_coastal_straight|hud;" +
  "SC_oldroad|200|07_oldroad_gap;" +
  "SC_ridge|166|08_ridge_jump;" +
  "top|0|09_topdown");
return "ok";
