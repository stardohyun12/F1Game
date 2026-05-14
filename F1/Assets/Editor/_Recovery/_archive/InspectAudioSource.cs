using UnityEditor;
using UnityEngine;

public static class InspectAudioSource
{
    public static void Execute()
    {
        // Race 프리팹의 AudioSource + EngineAudio 직렬화 값 점검
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Race.prefab");
        if (prefab == null) { Debug.LogError("Race prefab not found"); return; }
        var src = prefab.GetComponent<AudioSource>();
        if (src == null) { Debug.LogError("No AudioSource on Race"); return; }
        Debug.Log($"[InspectAudioSource] Race AudioSource: clip={src.clip}, playOnAwake={src.playOnAwake}, mute={src.mute}, volume={src.volume}, loop={src.loop}");

        var ea = prefab.GetComponent<EngineAudio>();
        if (ea != null)
        {
            var so = new SerializedObject(ea);
            var clipProp = so.FindProperty("engineLoop");
            Debug.Log($"[InspectAudioSource] EngineAudio.engineLoop = {clipProp.objectReferenceValue}");
        }
    }
}
