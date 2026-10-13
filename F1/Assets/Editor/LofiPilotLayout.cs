using System;
using UnityEngine;

namespace PitCrew.ArtPilot
{
    [Serializable] public class LofiPlacement
    {
        public string asset;
        public Vector3 position;
        public float yaw;
        public Vector3 scale = Vector3.one;
    }

    public class LofiPilotLayout : ScriptableObject
    {
        public LofiPlacement[] placements;
        public Vector3 cameraPosition = new Vector3(0, 3.7f, -7);
        public Vector3 cameraTarget = new Vector3(0, 1.25f, 5);
        public float fieldOfView = 55;
    }
}
