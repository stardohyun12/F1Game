// 저해상도 시험 컴포넌트 상태 + 주문 목록(화물 종류·보상) + 재료 단가.
var sb=new System.Text.StringBuilder();
foreach(var l in Resources.FindObjectsOfTypeAll<LowResRenderTest>()) if(l.gameObject.scene.IsValid()) sb.Append("LowRes on "+l.gameObject.name+" enabled="+l.enabled+" ; ");
foreach(var o in Resources.FindObjectsOfTypeAll<OrderJob>()) sb.Append(o.name+"("+o.cargoType+","+o.baseReward+") ");
foreach(var m in Resources.LoadAll<PitCrew.Packing.WrapMaterialDef>("WrapMaterials")) sb.Append(" | "+m.name+" ch="+m.channelIndex+" cpp="+m.costPerPixel);
return sb.ToString();
