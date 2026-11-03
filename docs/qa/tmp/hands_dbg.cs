var all = UnityEngine.Object.FindObjectsByType<DepotPackSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
var sb = new System.Text.StringBuilder();
sb.Append("n=" + all.Length + "\n");
foreach (var spot in all)
{
    var bench = spot.Bench; var eye = spot.Eye;
    var board = bench != null ? bench.transform.Find("WorkBoard") : null;
    sb.Append(spot.name + " bench=" + (bench != null) + " spot=" + (bench != null && bench.benchSpot != null) + " eye=" + (eye != null) + " board=" + (board != null) + " r=" + (board != null && board.GetComponent<Renderer>() != null) + " scene=" + spot.gameObject.scene.name + "\n");
}
return sb.ToString();
