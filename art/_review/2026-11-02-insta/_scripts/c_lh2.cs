DebugWindow.SetDusk(1f);
var sb = new System.Text.StringBuilder();
// 해(남남서)를 등대 뒤에 두려고 등대 북북동쪽 바다 위에서 남남서로 본다
sb.Append(Still("cand_lh3", new Vector3(-363, 3f, -272), new Vector3(-378, 6f, -361), 40f));
sb.Append(Still("cand_lh4", new Vector3(-340, 3f, -265), new Vector3(-378, 7f, -361), 30f));
sb.Append(Still("cand_lh5", new Vector3(-375, 2.5f, -300), new Vector3(-378, 7f, -361), 36f));
return sb.ToString();
