// Play/Edit: 바다 노을 머티리얼 속성 읽기 + 임시 쓰기(__OP__ 자리에 C# 문장)
var m = SunsetLook.Find().seaMaterial; var sb = new System.Text.StringBuilder();
__OP__
foreach (var n in new[]{"_Foam","_Core","_Hot","_Glow","_RippleCol","_CrestCol","_CoolCol","_ShallowCol","_DeepCol"}) sb.Append(n+"="+m.GetColor(n).ToString("F2")+" ");
foreach (var n in new[]{"_FoamA","_FoamW","_FoamMax","_SceneRefl","_Cool","_Crest","_HorizonFade","_Cuts","_Lv"}) sb.Append(n+"="+m.GetVector(n).ToString("F3")+" ");
foreach (var n in new[]{"_Sparkle","_SparkleRate","_StepKeep","_Reflect","_Style"}) sb.Append(n+"="+m.GetFloat(n)+" ");
return sb.ToString();
