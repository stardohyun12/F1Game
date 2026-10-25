var ot = UnityEngine.Object.FindFirstObjectByType<OpeningTutorial>();
if (ot == null) return "no OT";
ot.gameObject.SetActive(false);
return "OT off, stage=" + (Services.Run as RunController).CurrentStage;
