if (!Application.dataPath.Replace("\\", "/").EndsWith("/F1Studio/Assets") || Application.isPlaying) throw new System.Exception("Studio edit mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="FieldMock")throw new System.Exception("FieldMock required");
var root=GameObject.Find("FIELD_REVIEW_ONLY");var log=new System.Text.StringBuilder();
foreach(string name in new[]{"A_aligned_8x6","B_curved_10x7"}) {
    var v=root.transform.Find(name);int tris=0,ridges=0,plants=0,missing=0;float minNormal=1;
    foreach(var f in v.GetComponentsInChildren<MeshFilter>(true)) {if(f.name.StartsWith("house"))continue;if(!f.sharedMesh||f.sharedMesh.vertexCount==0){missing++;continue;}
        if(f.transform.IsChildOf(v.Find("house_home_reference")))continue;
        tris+=f.sharedMesh.triangles.Length/3;
        foreach(var p in f.sharedMesh.vertices)if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))throw new System.Exception("nonfinite vertex "+f.name);
        if(f.name.StartsWith("rounded_ridge"))ridges++;
        if(f.name.StartsWith("crop_mass")) {plants++;var vs=f.sharedMesh.vertices;var ns=f.sharedMesh.normals;var center=f.sharedMesh.bounds.center;for(int i=0;i<vs.Length;i++) if(ns[i].sqrMagnitude>.1f) minNormal=Mathf.Min(minNormal,Vector3.Dot((vs[i]-center).normalized,ns[i]));}
    }
    var soil=v.Find("worked_soil").GetComponent<MeshFilter>().sharedMesh.bounds;
    log.AppendLine(name+": soil="+soil.size.ToString("F3")+"; ridges="+ridges+"; cropMasses="+plants+"; mockTrisIncludingGroundAndPoles="+tris+"; missingMesh="+missing+"; minOutwardDot="+minNormal);
    if(ridges!=4||missing!=0||minNormal<0)throw new System.Exception(log.ToString()+"GEOMETRY_CHECK_FAIL");
}
var lt=UnityEngine.Object.FindFirstObjectByType<A7LightToggles>();
log.AppendLine("painterly="+lt.painterly+"; radius="+lt.painterlyRadius+"; shaderError="+UnityEditor.ShaderUtil.ShaderHasError(lt.kuwaharaShader)+"; sceneRP="+UnityEditor.AssetDatabase.GetAssetPath(lt.sceneRP));
log.AppendLine("compiling="+UnityEditor.EditorApplication.isCompiling+"; playing="+Application.isPlaying+"; dirty="+scene.isDirty);
return log.ToString();
