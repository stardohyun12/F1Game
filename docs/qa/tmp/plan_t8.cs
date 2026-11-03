var L = PitCrewPlan.PlanIO.LockPath; var sb = new System.Text.StringBuilder();
// 1) 빈 상태 → director 기록 → 해제
sb.Append("touch=" + PitCrewPlan.PlanIO.TouchLock() + " owner=" + PitCrewPlan.PlanIO.LockOwner());
PitCrewPlan.PlanIO.ReleaseLock(); sb.Append(" afterRelease exists=" + System.IO.File.Exists(L) + "\n");
// 2) 팀원(programmer)이 잡고 있으면 덮어쓰지도 지우지도 않음
System.IO.File.WriteAllText(L, "programmer\n");
sb.Append("touch=" + PitCrewPlan.PlanIO.TouchLock() + " owner=" + PitCrewPlan.PlanIO.LockOwner());
PitCrewPlan.PlanIO.ReleaseLock(); sb.Append(" afterRelease owner=" + PitCrewPlan.PlanIO.LockOwner() + "\n");
System.IO.File.Delete(L);
// 3) 팔레트 접지 시험: depot 프리팹을 보관소 옆에 놓고 접지 → 지움
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Art/Library/Buildings/depot/depot.prefab");
var go = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
go.transform.position = new UnityEngine.Vector3(-30f, 30f, -40f);
bool ok = PitCrewPlan.PlanIO.Ground(go, out var dy);
UnityEngine.Bounds b; PitCrewPlan.PlanIO.TryBounds(go, out b);
UnityEngine.RaycastHit h; UnityEngine.Physics.Raycast(new UnityEngine.Vector3(b.center.x, 200, b.center.z), UnityEngine.Vector3.down, out h, 400, ~0, UnityEngine.QueryTriggerInteraction.Ignore);
sb.Append("ground=" + ok + " dy=" + dy.ToString("F2") + " bottom=" + b.min.y.ToString("F2") + " hitBelowCenterAfter=" + h.point.y.ToString("F2") + " (" + h.collider.name + ")\n");
UnityEngine.Object.DestroyImmediate(go);
sb.Append("sceneDirty=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty);
return sb.ToString();
