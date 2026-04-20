using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class SetupDriftVfx
{
    [MenuItem("PitCrew/Setup Drift VFX")]
    public static void Run()
    {
        var race = GameObject.Find("Race");
        if (race == null)
        {
            Debug.LogError("[SetupDriftVfx] 'Race' GameObject not found in active scene.");
            return;
        }

        // Clear existing children if re-running.
        string[] names = { "SkidMark_L", "SkidMark_R", "Smoke_L", "Smoke_R" };
        foreach (var n in names)
        {
            var existing = race.transform.Find(n);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        var trailMat = new Material(Shader.Find("Sprites/Default"));

        var skidL = CreateSkid(race.transform, "SkidMark_L", new Vector3(-0.7f, 0.05f, -1.2f), trailMat);
        var skidR = CreateSkid(race.transform, "SkidMark_R", new Vector3(0.7f, 0.05f, -1.2f), trailMat);

        var smokeL = CreateSmoke(race.transform, "Smoke_L", new Vector3(-0.7f, 0.05f, -1.2f));
        var smokeR = CreateSmoke(race.transform, "Smoke_R", new Vector3(0.7f, 0.05f, -1.2f));

        var ctrl = race.GetComponent<DriftVfxController>();
        if (ctrl == null) ctrl = race.AddComponent<DriftVfxController>();

        var carComp = race.GetComponent<ArcadeCarController>();

        var so = new SerializedObject(ctrl);
        so.FindProperty("car").objectReferenceValue = carComp;

        var trailsProp = so.FindProperty("skidTrails");
        trailsProp.arraySize = 2;
        trailsProp.GetArrayElementAtIndex(0).objectReferenceValue = skidL.GetComponent<TrailRenderer>();
        trailsProp.GetArrayElementAtIndex(1).objectReferenceValue = skidR.GetComponent<TrailRenderer>();

        var smokeProp = so.FindProperty("smokeParticles");
        smokeProp.arraySize = 2;
        smokeProp.GetArrayElementAtIndex(0).objectReferenceValue = smokeL.GetComponent<ParticleSystem>();
        smokeProp.GetArrayElementAtIndex(1).objectReferenceValue = smokeR.GetComponent<ParticleSystem>();
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(race);
        if (race.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(race.scene);

        Debug.Log("[SetupDriftVfx] Drift VFX setup complete on Race.");
    }

    // Static entry for coplay-mcp execute_script.
    public static void Execute()
    {
        Run();
    }

    static GameObject CreateSkid(Transform parent, string name, Vector3 localPos, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;

        var tr = go.AddComponent<TrailRenderer>();
        tr.startWidth = 0.2f;
        tr.endWidth = 0.15f;
        tr.time = 2f;
        tr.minVertexDistance = 0.1f;
        tr.emitting = false;
        tr.sharedMaterial = mat;

        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.black, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        tr.colorGradient = grad;
        tr.startColor = new Color(0f, 0f, 0f, 0.8f);
        tr.endColor = new Color(0f, 0f, 0f, 0f);

        return go;
    }

    static GameObject CreateSmoke(Transform parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone upward

        var ps = go.AddComponent<ParticleSystem>();
        // Stop to safely configure.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.startLifetime = 0.8f;
        main.startSpeed = 1.5f;
        main.startSize = 0.6f;
        main.startColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.enabled = false;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.1f;

        ps.Play();
        return go;
    }
}
