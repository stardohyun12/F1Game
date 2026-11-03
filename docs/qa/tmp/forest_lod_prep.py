"""forest_grove_cap.cs 에서 LOD1 만 보이게 하는 캡처용 forest_lod_cap.cs 를 만든다 (artlive 20261031-2009). ForceLOD 는 다음 프레임에야 먹어서 LODGroup 을 끄고 자식을 직접 켜고 끈다."""
import pathlib
T = pathlib.Path('D:/Github/F1Game/docs/qa/tmp'); s = (T / 'forest_grove_cap.cs').read_text(encoding='utf-8')
a = s.index('foreach (var g in new[] { "F1", "F2", "MIX", "F1_old" })'); b = s.index('Only("GROVE_F1");\nforeach (var kv in was)')
new = ('foreach (var lg in fo.GetComponentsInChildren<LODGroup>(true)) { lg.enabled = false; foreach (Transform ch in lg.transform) ch.gameObject.SetActive(ch.name == "LOD1"); }\n'
       'Only("GROVE_MIX"); Shot("Cam_FO_far", "s" + nn + "_MIX_lod1_far.png"); Shot("Cam_FO_kart", "s" + nn + "_MIX_lod1_kart.png");\n'
       'foreach (var lg in fo.GetComponentsInChildren<LODGroup>(true)) { foreach (Transform ch in lg.transform) ch.gameObject.SetActive(true); lg.enabled = true; }\n')
(T / 'forest_lod_cap.cs').write_text(s[:a] + new + s[b:], encoding='utf-8')
