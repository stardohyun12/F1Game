Application.runInBackground = true;
var z = AmbienceZones.Active;
if (z == null) return "no zones";
var spots = new[] {
  new AmbienceProbe.Spot("방", 0, Vector3.zero, 0),
  new AmbienceProbe.Spot("정비소 안", 9, Vector3.zero, 0),
  new AmbienceProbe.Spot("집 앞 골목", 1, Vector3.zero, 0),
  new AmbienceProbe.Spot("언덕 꼭대기", 10, Vector3.zero, 0),
  new AmbienceProbe.Spot("언덕 꺼짐(sink)", 7, Vector3.zero, 0),
  new AmbienceProbe.Spot("부두 끝", 5, Vector3.zero, 0),
  new AmbienceProbe.Spot("항구(새 자리)", 6, Vector3.zero, 0),
  new AmbienceProbe.Spot("큰길", 8, Vector3.zero, 0),
  new AmbienceProbe.Spot("찻집 앞", 4, Vector3.zero, 0),
};
return AmbienceProbe.Places("D:/Github/F1Game/docs/qa/evidence/ambience-zones", spots) + " mute=" + UnityEditor.EditorUtility.audioMasterMute + " rate=" + AudioSettings.outputSampleRate;
