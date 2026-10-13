using System;
using UnityEngine;

namespace PitCrew.Garage
{
    /// <summary>Visual-only restoration and wheel motion. Does not change vehicle physics.</summary>
    [DisallowMultipleComponent]
    public sealed class FormulaVehicleVisual : MonoBehaviour
    {
        [Serializable] public struct Part
        {
            public Renderer renderer;
            public F1PartId restoredBy;
            public bool cargo;
            public bool driver;
        }
        [Serializable] public struct Wheel
        {
            public Transform transform;
            public bool front;
            public float radius;
        }
        public FormulaAppearanceProfile profile;
        public Part[] parts = Array.Empty<Part>();
        public Wheel[] wheels = Array.Empty<Wheel>();
        public Transform chassis;
        ArcadeCarController car;
        CargoBox cargo;
        F1RestoreSystem restore;
        UpgradeSystem upgrades;
        Quaternion[] wheelRest;
        float[] wheelAngles;
        Quaternion chassisRest;

        void Awake()
        {
            car = GetComponentInParent<ArcadeCarController>();
            cargo = GetComponentInParent<CargoBox>();
            wheelRest = new Quaternion[wheels.Length];
            wheelAngles = new float[wheels.Length];
            for (int i=0;i<wheels.Length;i++) if(wheels[i].transform) wheelRest[i]=wheels[i].transform.localRotation;
            if(chassis) chassisRest=chassis.localRotation;
        }
        void OnEnable() { Bind(); Refresh(); }
        void Start() { Bind(); Refresh(); }
        void Bind()
        {
            var nextRestore=FindFirstObjectByType<F1RestoreSystem>();
            var nextUpgrades=UpgradeSystem.Instance;
            if(restore!=nextRestore){if(restore)restore.OnRestored-=Restored;restore=nextRestore;if(restore)restore.OnRestored+=Restored;}
            if(upgrades!=nextUpgrades){if(upgrades)upgrades.OnUpgradePurchased-=Purchased;upgrades=nextUpgrades;if(upgrades)upgrades.OnUpgradePurchased+=Purchased;}
        }
        void OnDisable()
        {
            if(restore)restore.OnRestored-=Restored;
            if(upgrades)upgrades.OnUpgradePurchased-=Purchased;
            restore=null;upgrades=null;
        }
        void Restored(F1Part _) => Refresh();
        void Purchased(UpgradeEffect _) => Refresh();
        public void Refresh()
        {
            if(!profile)return;
            foreach(var p in parts)
            {
                if(!p.renderer)continue;
                Material m=profile.worn;
                if(p.driver)m=profile.restored;
                else if(p.cargo){if(upgrades&&upgrades.IsOwned(UpgradeEffect.ReinforcedTrunk))m=profile.serviced;}
                else if(restore&&restore.IsRestored(p.restoredBy))m=profile.restored;
                else if(p.restoredBy==F1PartId.Engine&&restore&&restore.IsRestored(F1PartId.Gearbox))m=profile.serviced;
                // Swap references only: no material instantiation or mutation of shared assets.
                if(m)p.renderer.sharedMaterial=m;
            }
        }
        void LateUpdate()
        {
            if(!car||wheelRest==null)return;
            for(int i=0;i<wheels.Length;i++)
            {
                var w=wheels[i];if(!w.transform)continue;
                float worldRadius=w.radius*Mathf.Abs(w.transform.lossyScale.y);
                wheelAngles[i]=Mathf.Repeat(wheelAngles[i]+car.CurrentSpeedMps/Mathf.Max(.01f,worldRadius)*Mathf.Rad2Deg*Time.deltaTime,360);
                w.transform.localRotation=Quaternion.Euler(0,w.front?car.SteerInput*24:0,0)*wheelRest[i]*Quaternion.AngleAxis(wheelAngles[i],Vector3.right);
            }
            if(chassis&&cargo)
            {
                var lean=Quaternion.Euler(Mathf.Clamp(-cargo.SignedLongG*3,-4,4),0,Mathf.Clamp(cargo.SignedLatG*4,-4,4));
                chassis.localRotation=Quaternion.Slerp(chassis.localRotation,chassisRest*lean,1-Mathf.Exp(-6*Time.deltaTime));
            }
        }
    }
}
