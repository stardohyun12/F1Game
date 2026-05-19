using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.Garage
{
    [CreateAssetMenu(menuName = "PitCrew/F1 Restore Catalog", fileName = "F1RestoreCatalog")]
    public sealed class F1RestoreCatalog : ScriptableObject
    {
        public List<F1Part> parts = new List<F1Part>();

        public F1Part Find(F1PartId id)
        {
            foreach (var p in parts) if (p != null && p.id == id) return p;
            return null;
        }
    }
}
