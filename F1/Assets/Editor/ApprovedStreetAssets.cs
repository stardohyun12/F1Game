using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using PitCrew.Garage;

// User approved the metre-consistent scale comparison on 2026-10-13.
public static class ApprovedStreetAssets
{
    public const string Dir="Assets/Art/StreetFurniture";
    public static bool IsFurniture(string name)=>name=="Bench"||name=="Bin";
    public static string PrefabPath(string name)=>IsFurniture(name)?Dir+"/"+name+".prefab":"Assets/Art/LofiPilot/"+name+".prefab";
    public static Material FurnitureMaterial=>AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Furniture.mat");
    public static Material RoadMaterial=>AssetDatabase.LoadAssetAtPath<Material>(Dir+"/Asphalt.mat");
    [MenuItem("PitCrew/Art/Street Furniture/Apply Approved Assets and Scale")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/ComplexTrack.unity")throw new Exception("Use saved ComplexTrack edit mode");
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/source/street-furniture"));
        var backup=output+"/ComplexTrack.before-approved-scale.unity";if(!File.Exists(backup))File.Copy(scene.path,backup);
        var town=GameObject.Find("CoastalTown");int count=0,stains=0;
        foreach(var t in town.transform.Cast<Transform>().ToArray()) {
            string name=t.name.Split('_')[0];
            if(IsFurniture(name)) {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name)),scene);
                go.name=t.name;go.transform.SetParent(town.transform,false);go.transform.SetPositionAndRotation(t.position,t.rotation);go.transform.localScale=Vector3.one;
                UnityEngine.Object.DestroyImmediate(t.gameObject);count++;
            }
            else if(name=="OilStain"||name=="Patch"){t.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);stains++;}
        }
        foreach(var r in GameObject.Find("Track").GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>RoadMaterial).ToArray();
        var car=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();var box=car.GetComponent<BoxCollider>();var rig=car.GetComponentInChildren<FormulaVehicleVisual>();
        var s=car.transform.lossyScale;if(Mathf.Abs(s.x-s.z)>.001f||Mathf.Abs(s.x-s.y)>.001f)throw new Exception("Vehicle root must have uniform scale");
        float bottom=box.center.y-box.size.y/2;
        box.size=new Vector3(1.96f/s.x,.65f/s.y,3.785f/s.z);box.center=new Vector3(0,bottom+box.size.y/2,0);
        rig.transform.localScale=Vector3.one/s.x;rig.transform.localPosition=new Vector3(0,bottom,-.1375f/s.z);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rig.transform);
        car.GetComponent<Rigidbody>().ResetInertiaTensor();
        var camera=UnityEngine.Object.FindFirstObjectByType<FollowCamera>();var so=new SerializedObject(camera);
        so.FindProperty("offset").vector3Value=new Vector3(0,3.2f,-7);so.FindProperty("lookHeight").floatValue=1.15f;so.ApplyModifiedPropertiesWithoutUndo();
        var data=AssetDatabase.LoadAssetAtPath<CoastalTownLayout>("Assets/Art/CoastalTown/TownLayout.asset");
        for(int i=0;i<data.placements.Length;i++)if(IsFurniture(data.placements[i].asset))data.placements[i].scale=Vector3.one;
        data.cameraOffset=new Vector3(0,3.2f,-7);data.cameraLookHeight=1.15f;EditorUtility.SetDirty(data);
        Physics.SyncTransforms();
        foreach(var wheel in rig.wheels) {
            float y=wheel.transform.GetComponent<Renderer>().bounds.min.y;
            if(Mathf.Abs(y-box.bounds.min.y)>.003f)throw new Exception("Wheel/collider floor mismatch");
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(output+"/approved-apply.txt",$"Approved furniture={count}\nDisabled repeated stains={stains}\nCar world dimensions=1.96 x 3.785; collider height=.65\nBench scale=1; Bin scale=1\nWheel/collider bottoms aligned.\n");
    }
}
