# 20261030-2334: 본 F1 에디터용 house_home 프리팹 재생성 스크립트를 만든다.
# library_import.cs + build_asset.py h_unity 의 집 전용 치환(방 x -2.4·손잡이 제거·펜던트 빛·_SN 0)을 그대로 쓰고,
# 스튜디오 전용(STUDIO_Slot·땅 자르기·씬 저장)만 뺀다. 재질은 지우지 않고 덮어써 GUID 유지.
from pathlib import Path
R = Path(__file__).resolve().parents[3]
code = (R / 'docs/qa/tmp/library_import.cs').read_text(encoding='utf-8')
def rep(a, b):
    global code
    assert a in code, a[:60]
    code = code.replace(a, b)
rep('var ONLY = new[] { "depot" };', 'var ONLY = new[] { "house_home" };')
rep('var slotT = GameObject.Find("STUDIO/STUDIO_Slot").transform;', 'var slotGo = new GameObject("__hh_build_tmp"); var slotT = slotGo.transform;')
rep('m.SetFloat("_Sat", 1);', 'm.SetFloat("_Sat", 1); m.SetFloat("_SN", 0);')
rep('new Vector3(0, 3.2f, 1.35f) - off', 'new Vector3(-2.4f, 3.2f, 1.35f) - off')
rep('log.Append(" room shell dropped, lamp "',
    'foreach (var kn in ri.GetComponentsInChildren<Transform>(true).Where(t => t.name == "KNOB_01").ToArray()) UnityEngine.Object.DestroyImmediate(kn.gameObject);\n'
    '    foreach (var pn in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("PENDANT_") || t.name.StartsWith("BEDLIGHT_")).ToArray()) { var lg = new GameObject("Light"); lg.transform.SetParent(pn, false); lg.transform.position = pn.position + Vector3.up * 0.1f; var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(1f, .82f, .55f); li.range = pn.name.StartsWith("PENDANT_") ? 4.2f : 2.2f; li.intensity = pn.name.StartsWith("PENDANT_") ? 1.1f : 0.6f; li.shadows = LightShadows.None; }\n'
    '    log.Append(" room shell dropped, lamp "')
# 재질 GUID 유지: 지우고 새로 만들지 않고 있던 에셋에 값만 복사
rep('AssetDatabase.DeleteAsset(gp);', '')
rep('AssetDatabase.CreateAsset(gm, gp); made[tn] = gm;',
    '{ var ex = AssetDatabase.LoadAssetAtPath<Material>(gp); if (ex) { ex.shader = gm.shader; ex.CopyPropertiesFromMaterial(gm); ex.shaderKeywords = gm.shaderKeywords; ex.renderQueue = gm.renderQueue; ex.SetOverrideTag("RenderType", "Transparent"); EditorUtility.SetDirty(ex); gm = ex; } else AssetDatabase.CreateAsset(gm, gp); } made[tn] = gm;')
rep('AssetDatabase.DeleteAsset(mp);', '')
rep('AssetDatabase.CreateAsset(m, mp); made[tn] = m;',
    '{ var ex = AssetDatabase.LoadAssetAtPath<Material>(mp); if (ex) { ex.shader = m.shader; ex.CopyPropertiesFromMaterial(m); ex.shaderKeywords = m.shaderKeywords; EditorUtility.SetDirty(ex); m = ex; } else AssetDatabase.CreateAsset(m, mp); } made[tn] = m;')
rep('foreach (var e in POSE) AssetDatabase.ImportAsset(L + "/" + e.nm + "/" + e.nm + ".prefab", ImportAssetOptions.ForceUpdate);',
    'UnityEngine.Object.DestroyImmediate(slotGo); AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(L + "/house_home/house_home.prefab", ImportAssetOptions.ForceUpdate);')
rep('UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());', '')
(R / 'docs/qa/tmp/hh_f1_import.cs').write_text(code, encoding='utf-8')
print('ok', len(code))
