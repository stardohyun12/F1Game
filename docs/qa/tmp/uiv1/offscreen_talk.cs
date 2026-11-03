// offscreen.cs 0.3초 뒤 첫 장(사장 화면 밖, 대화 전)이 찍힌 다음에 부른다 — 대화 시작.
var d = UnityEngine.Object.FindFirstObjectByType<OnFootDirector>();
return "ForceTalk=" + d.ForceTalk() + " inTalk=" + d.InTalk;
