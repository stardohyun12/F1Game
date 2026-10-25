var g = GameObject.Find("Road_V_tunnel"); g.GetComponent<MeshCollider>().enabled = false;
var car = UnityEngine.Object.FindFirstObjectByType<ArcadeCarController>();
var bc = car.GetComponentsInChildren<Collider>().Select(c => c.GetType().Name + ":" + c.bounds.size.ToString("F2") + " min.y=" + c.bounds.min.y.ToString("F2"));
return "tunnel collider off | car cols " + string.Join(" ", bc);
