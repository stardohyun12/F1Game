var dm=UnityEngine.Object.FindFirstObjectByType<DeliveryManager>(); var bf=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var route=(System.Collections.IList)typeof(DeliveryManager).GetField("route",bf).GetValue(dm); var fIdx=typeof(DeliveryManager).GetField("currentIndex",bf);
int n=0; while(route.Count>0 && (int)fIdx.GetValue(dm)<route.Count && n<10){ dm.OnDeliveryReached((DeliveryPoint)route[(int)fIdx.GetValue(dm)]); n++; }
var ret=typeof(DeliveryManager).GetField("startReturn",bf).GetValue(dm) as Transform;
if(ret!=null){ var c=UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>(); var rb=c.GetComponent<Rigidbody>(); rb.position=ret.position+Vector3.up*0.5f; rb.linearVelocity=Vector3.zero; Physics.SyncTransforms(); }
return "delivered "+n+" return="+(ret!=null)+" wallet="+UpgradeSystem.Instance.Wallet;
