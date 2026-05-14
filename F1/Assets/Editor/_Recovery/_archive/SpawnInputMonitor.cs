using UnityEngine;

public static class SpawnInputMonitor
{
    public static void Execute()
    {
        var go = new GameObject("__InputLiveMonitor");
        go.AddComponent<InputLiveMonitor>();
        Debug.Log("[Live] Spawned monitor. CLICK GAME VIEW then press W/A/S/D and Space.");
    }
}
