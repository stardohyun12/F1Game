DebugWindow.SetDusk(1f);
var sb = new System.Text.StringBuilder();
// 등대 실루엣: 바다 위 낮은 카메라(걷는 시점으로는 못 가는 자리), 동쪽에서 서쪽(해) 쪽 등대를 본다
sb.Append(Still("cand_lh0", new Vector3(-300, 3f, -345), new Vector3(-378, 6.5f, -361), 40f));
sb.Append(Still("cand_lh1", new Vector3(-330, 2.5f, -352), new Vector3(-378, 7f, -361), 40f));
sb.Append(Still("cand_lh2", new Vector3(-250, 4f, -330), new Vector3(-378, 8f, -361), 30f));
// 비탈 위에서 내려다본 바다: 능선 위로 올라 지붕 너머 남쪽
sb.Append(Still("cand_over5", new Vector3(228, 80f, 100), new Vector3(228, 25f, -290), 52f));
sb.Append(Still("cand_over6", new Vector3(180, 75f, 100), new Vector3(150, 15f, -290), 52f));
return sb.ToString();
