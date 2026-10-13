using UnityEngine;

namespace PitCrew.Garage
{
    [CreateAssetMenu(menuName = "PitCrew/Formula Appearance")]
    public sealed class FormulaAppearanceProfile : ScriptableObject
    {
        public Material worn;
        public Material serviced;
        public Material restored;
    }
}
