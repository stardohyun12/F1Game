using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

// Opt-in isolated trial scene only. Uses the unchanged production vehicle controller.
public class PitCrewTrialDriver : MonoBehaviour
{
    public ArcadeCarController car;
    public Camera chase;
    public Material[] paletteA, paletteB;
    public Renderer[] paint;
    public Renderer sand;
    public Color sandA,sandB;
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
    bool fast,blue,smoke;
    Vector3 spawn;
    string evidence;
    void Awake()
    {
        Application.runInBackground=true;
        spawn=car.transform.position;
        var stage=gameObject.AddComponent<RunController>();stage.enabled=false;
        typeof(RunController).GetField("stage",Fields).SetValue(stage,RunStage.Driving);
        typeof(Services).GetProperty("Run").SetValue(null,stage); // Isolated driving gate; production assembly unchanged.
        Palette(true); // User selected B on 2026-10-18; steering remains original.
        smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"-pitcrew-smoke")>=0;
        evidence=Path.GetFullPath(Path.Combine(Application.dataPath,"../TrialEvidence"));
        if(smoke){Directory.CreateDirectory(evidence);StartCoroutine(Smoke());}
    }
    void Update()
    {
        if(smoke)return;
        var k=Keyboard.current;if(k==null)return;
        Input(new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),
            (k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0)));
        if(k.f1Key.wasPressedThisFrame)Palette(false);
        if(k.f2Key.wasPressedThisFrame)Palette(true);
        if(k.f3Key.wasPressedThisFrame)Feel(false);
        if(k.f4Key.wasPressedThisFrame)Feel(true);
        if(k.rKey.wasPressedThisFrame)ResetCar();
        if(k.escapeKey.wasPressedThisFrame)Application.Quit();
    }
    void LateUpdate(){chase.transform.position=car.transform.position+new Vector3(0,3.4f,-6.5f);chase.transform.LookAt(car.transform.position+Vector3.up*.6f);}
    void Input(Vector2 v)=>typeof(ArcadeCarController).GetField("moveInput",Fields).SetValue(car,v);
    void Feel(bool value){fast=value;typeof(ArcadeCarController).GetField("steerLag",Fields).SetValue(car,value?.015f:.025f);}
    void Palette(bool value){blue=value;for(int i=0;i<paint.Length;i++)paint[i].sharedMaterial=(value?paletteB:paletteA)[i];if(sand)sand.material.SetColor("_Tint",value?sandB:sandA);}
    void ResetCar(){var rb=car.GetComponent<Rigidbody>();rb.position=spawn;rb.rotation=Quaternion.identity;rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;typeof(ArcadeCarController).GetField("smoothedMove",Fields).SetValue(car,Vector2.zero);typeof(ArcadeCarController).GetField("smoothVel",Fields).SetValue(car,Vector2.zero);Input(Vector2.zero);}
    void OnGUI()
    {
        GUI.Box(new Rect(12,12,690,112),"PITCREW / COASTAL DELIVERY — ART B ADOPTED / STEERING EXPERIMENT");
        GUI.Label(new Rect(26,39,660,24),"WASD / arrows: drive   R: reset   Esc: exit   F1/F2: palette A/B");
        GUI.Label(new Rect(26,64,660,24),"F3: original steer lag 0.025 s   F4: experiment 0.015 s (other values unchanged)");
        GUI.Label(new Rect(26,89,660,24),$"Palette {(blue?"B":"A")} | Steering {(fast?"experiment":"original")} | {car.CurrentSpeedMps*3.6f:F1} km/h | Human play review pending");
    }
    void Capture(string name)
    {
        var rt=new RenderTexture(1280,800,24);var tex=new Texture2D(1280,800,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{chase.targetTexture=rt;chase.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(evidence,name),tex.EncodeToPNG());}
        finally{chase.targetTexture=null;RenderTexture.active=previous;Destroy(tex);rt.Release();Destroy(rt);}
    }
    IEnumerator Smoke()
    {
        string report="Actual Unity standalone, unchanged ArcadeCarController; isolated Driving gate.\n";
        for(int variant=0;variant<2;variant++)
        {
            Feel(variant==1);Palette(true);ResetCar();
            yield return new WaitForSeconds(.5f);
            Input(new Vector2(0,.45f));yield return new WaitForSeconds(1.5f);
            float speed=car.CurrentSpeedMps;var start=car.transform.position;
            Input(new Vector2(.5f,.45f));yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            float steer=car.SteerInput;
            yield return new WaitForSeconds(.7f);
            float distance=Vector3.Distance(start,car.transform.position);
            bool pass=speed>.2f&&distance>.2f&&steer>.1f;
            report+=$"{(variant==0?"baseline":"experiment")} speedMps={speed:F4} distance={distance:F4} steerAfter2FixedSteps={steer:F4} fixedDeltaTime={Time.fixedDeltaTime} PASS={pass} grounded={typeof(ArcadeCarController).GetField("isGrounded",Fields).GetValue(car)} throttle={car.ThrottleInput} friction={car.GetComponent<BoxCollider>().sharedMaterial.name}\n";
            Capture(variant==0?"play-baseline.png":"play-experiment.png");
            yield return new WaitForSeconds(.3f);
        }
        Input(Vector2.zero);File.WriteAllText(Path.Combine(evidence,"smoke.txt"),report+"No user adoption or subjective play validation claimed.\n");
        Application.Quit();
    }
}
