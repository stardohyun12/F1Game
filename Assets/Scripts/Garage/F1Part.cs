using UnityEngine;

namespace PitCrew.Garage
{
    [CreateAssetMenu(menuName = "PitCrew/F1 Part", fileName = "F1Part")]
    public sealed class F1Part : ScriptableObject
    {
        [Header("ID")]
        public F1PartId id;
        public string displayName;

        [Header("Economy (D-080~082)")]
        [Min(0)] public int restoreCost = 1000;

        [Header("Cutscene (D-088)")]
        [TextArea(2,4)] public string flavorLine;     // "엔진이 돌아왔다. 시동 소리가 살아있다."
        [Range(1.0f, 4.0f)] public float cutsceneSeconds = 2.5f;

        [Header("Mechanic Effect (D-089)")]
        [TextArea(2,4)] public string mechanicDescription; // UI에 표시될 효과 설명
    }
}
