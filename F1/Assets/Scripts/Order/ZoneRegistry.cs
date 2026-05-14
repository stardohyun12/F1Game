using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 채널 3 — 모든 지역 목록과 시작 지역 정의. ZoneUnlockSystem이 참조.
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/Zone Registry", fileName = "ZoneRegistry")]
public class ZoneRegistry : ScriptableObject
{
    [Tooltip("게임에 등장하는 모든 zone. 시작 zone은 첫 번째 또는 unlockCost=0.")]
    public List<Zone> allZones = new List<Zone>();

    public Zone Find(string id)
    {
        for (int i = 0; i < allZones.Count; i++)
        {
            var z = allZones[i];
            if (z != null && z.id == id) return z;
        }
        return null;
    }
}
