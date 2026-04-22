using System.Collections.Generic;
using UnityEngine;

// Maps a mask character (e.g. 'S', 'C') to a pool of BuildingDefs.
// BuildingPlacer deterministically picks from the pool per cell.
[CreateAssetMenu(menuName = "PitCrew/Building Catalog", fileName = "BuildingCatalog")]
public class BuildingCatalog : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Single character used in the BuildingMask (case-sensitive). E.g. 'S' for suburban, 'C' for commercial.")]
        public string maskChar = "S";
        public List<BuildingDef> pool = new();
    }

    public List<Entry> entries = new();

    public BuildingDef Pick(char c, int hash)
    {
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.maskChar) || e.maskChar.Length == 0) continue;
            if (e.maskChar[0] != c) continue;
            if (e.pool == null || e.pool.Count == 0) return null;
            int idx = Mathf.Abs(hash) % e.pool.Count;
            return e.pool[idx];
        }
        return null;
    }
}
