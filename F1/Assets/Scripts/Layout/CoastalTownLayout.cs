using System;
using UnityEngine;

[CreateAssetMenu(menuName="PitCrew/Coastal Town Layout")]
public sealed class CoastalTownLayout : ScriptableObject
{
    [Serializable] public struct Placement
    {
        public string asset;
        public Vector3 position;
        public float yaw;
        public Vector3 scale;
        public int palette;
    }
    public TrackLayout streets;
    public Placement[] placements;
    public float cellSize=6f;
    public Vector3 cameraOffset=new Vector3(0,3.2f,-7f);   // 2026-10-13: 차 3.785m 기준 (다른 세션 승인값). 옛 (0,2,-3.5)
    public float cameraLookHeight=1.15f;
}
