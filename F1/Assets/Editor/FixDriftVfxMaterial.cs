using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixDriftVfxMaterial
{
    const string MaterialPath = "Assets/DriftSmoke.mat";
    const string TrailMaterialPath = "Assets/DriftSkid.mat";

    [MenuItem("PitCrew/Fix Drift VFX Material")]
    public static void Execute()
    {
        var smokeMat = EnsureParticleMaterial(MaterialPath, new Color(0.75f, 0.75f, 0.75f, 0.6f));
        var skidMat = EnsureParticleMaterial(TrailMaterialPath, new Color(0f, 0f, 0f, 0.85f));

        AssignSmoke("Race/Smoke_L", smokeMat);
        AssignSmoke("Race/Smoke_R", smokeMat);
        AssignTrail("Race/SkidMark_L", skidMat);
        AssignTrail("Race/SkidMark_R", skidMat);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FixDriftVfxMaterial] Done.");
    }

    static Material EnsureParticleMaterial(string path, Color color)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            EditorUtility.SetDirty(existing);
            return existing;
        }
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // alpha
        mat.renderQueue = 3000;
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static void AssignSmoke(string path, Material mat)
    {
        var go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning($"Not found: {path}"); return; }
        var psr = go.GetComponent<ParticleSystemRenderer>();
        if (psr != null) psr.sharedMaterial = mat;
        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            main.startColor = new Color(0.85f, 0.85f, 0.85f, 0.7f);
            main.playOnAwake = true;
            var emission = ps.emission;
            emission.enabled = false;
            ps.Play(true);
        }
    }

    static void AssignTrail(string path, Material mat)
    {
        var go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning($"Not found: {path}"); return; }
        var tr = go.GetComponent<TrailRenderer>();
        if (tr != null) tr.sharedMaterial = mat;
    }
}
