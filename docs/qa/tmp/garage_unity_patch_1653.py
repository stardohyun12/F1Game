# artlive 1653: (1) A7 셰이더 사본(정비소 폴더 전용) = 점광원(추가 광원) 받음 + _Emit (2) library_import.cs → garage 만, 새 재질·점광원 (3) 파일 복사
import re, shutil
R = 'D:/Github/F1Game'; LIB = R + '/F1/Assets/Art/Library/Buildings/garage'; EX = R + '/art/export/q2-garage'
s = open(R + '/F1/Assets/Art/SceneA7/SceneA7Lit.shader', encoding='utf8').read()
s = s.replace('Shader "PitCrew/SceneA7/Lit"', 'Shader "PitCrew/Library/A7LitPoint"', 1)
assert 'A7LitPoint' in s
s = re.sub(r'(Properties\s*\{)', r'\1\n        _Emit ("1653 Emit (lamp glow)", Float) = 0', s, count=1)
s = s.replace('#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN',
              '#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN\n'
              '            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS\n'
              '            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP', 1)
s = s.replace('            half4 frag(V i, bool front : SV_IsFrontFace) : SV_Target', '            float _Emit;\n            half4 frag(V i, bool front : SV_IsFrontFace) : SV_Target', 1)
A7END = '                    c = t.rgb * lerp(_A7Shade.rgb, ml.color, lv);\n                }\n'
assert A7END in s
s = s.replace(A7END, A7END + '''                // 1653: 추가 광원(점광원). 4단 계단 → 바닥에 둥근 빛 웅덩이(테두리가 픽셀 계단)
                {
                    InputData inputData = (InputData)0; inputData.positionWS = i.wp; inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.pos);
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light al = GetAdditionalLight(lightIndex, i.wp);
                        float a = al.distanceAttenuation * saturate(dot(n, al.direction));
                        a = floor(a * 4 + 0.35) / 4;
                        c += t.rgb * al.color * a;
                    LIGHT_LOOP_END
                }
                c = lerp(c, t.rgb * 1.15, _Emit);
''', 1)
open(LIB + '/A7LitPoint_garage.shader', 'w', encoding='utf8').write(s)

p = R + '/docs/qa/tmp/library_import.cs'; c = open(p, encoding='utf8').read()
c = c.replace('var ONLY = new[] { "depot", "postoffice" };  // 1623: 이번엔 둘만 다시 만듦(room·garage 는 그대로)',
              'var ONLY = new[] { "garage" };  // 1653: 정비소만 다시 만듦')
c = c.replace('if (nm == "garage") { if (s.Contains("albedo"))', 'if (nm == "garage") { if (s.Contains("lamp")) return "garage_lamp"; if (s.Contains("props")) return "garage_props"; if (s.Contains("albedo"))')
c = c.replace('        AssetDatabase.CreateAsset(m, mp); made[tn] = m; }',
              '        if (e.nm == "garage") { m.shader = Shader.Find("PitCrew/Library/A7LitPoint"); m.SetFloat("_Emit", tn == "garage_lamp" ? 1f : 0f); }  // 1653: 점광원 받는 사본 셰이더\n'
              '        AssetDatabase.CreateAsset(m, mp); made[tn] = m; }')
c = c.replace('  PrefabUtility.SaveAsPrefabAssetAndConnect(root, dir + "/" + e.nm + ".prefab"',
              '  foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("LAMP_")) { var lt = t.gameObject.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, 0.76f, 0.46f); lt.range = 4.5f; lt.intensity = 2.2f; lt.shadows = LightShadows.None; log.Append(" light " + t.name + " " + t.position.ToString("F2")); }  // 1653: 갓 아래 따뜻한 점광원\n'
              '  PrefabUtility.SaveAsPrefabAssetAndConnect(root, dir + "/" + e.nm + ".prefab"')
assert c.count('1653') >= 3 and 'garage_lamp' in c, c.count('1653')
open(p, 'w', encoding='utf8').write(c)
for f in ['garage_v5_combo.fbx', 'garage_props.png', 'garage_festival.png']: shutil.copy(EX + '/' + f, LIB + '/' + f)
shutil.copy(EX + '/garage_props.png', LIB + '/garage_lamp.png')
print('ok')
