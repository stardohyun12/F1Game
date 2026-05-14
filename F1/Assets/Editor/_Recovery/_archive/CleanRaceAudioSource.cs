using UnityEditor;
using UnityEngine;

public static class CleanRaceAudioSource
{
    public static void Execute()
    {
        var prefabPath = "Assets/Prefabs/Race.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogError("Race prefab not found"); return; }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var src = instance.GetComponent<AudioSource>();
        if (src != null)
        {
            src.playOnAwake = false;
            src.mute = true;       // 시작 muted, EngineAudio.Update가 풀어줌
            src.loop = true;
            src.spatialBlend = 0f;
            Debug.Log("[CleanRaceAudioSource] AudioSource: playOnAwake=false, mute=true, loop=true 직렬화");
        }
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        Debug.Log("[CleanRaceAudioSource] done.");
    }
}
