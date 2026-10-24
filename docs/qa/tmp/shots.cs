var res=new System.Text.StringBuilder();
void Shot(string file, Vector3 pos, Vector3 look, float fov){
 var go=new GameObject("TmpShotCam"); var cam=go.AddComponent<Camera>(); cam.fieldOfView=fov; cam.farClipPlane=1000f; cam.clearFlags=CameraClearFlags.Skybox; cam.transform.position=pos; cam.transform.LookAt(look);
 var rt=new RenderTexture(1280,720,24); cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt; var tex=new Texture2D(1280,720,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1280,720),0,0); tex.Apply(); RenderTexture.active=null;
 System.IO.File.WriteAllBytes("D:/Github/F1Game/docs/qa/evidence/"+file,tex.EncodeToPNG()); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(go); res.Append(file+" ok; "); }
Shot("20261023-1443-outer-ground.png", new Vector3(120f,16f,-34f), new Vector3(190f,0f,6f), 60f);
Shot("20261023-1443-guardrail.png", new Vector3(166f,2.4f,66f), new Vector3(183f,0.5f,56f), 60f);
return res.ToString();
