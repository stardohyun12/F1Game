// 비교 카메라 고정: 방 남서쪽 구석에서 문·달력·포스터·창·책상 쪽(나쁜 예 그림과 비슷한 각)
var p = GameObject.Find("OnFoot_HomeGarage/OnFoot_Player");
var cc = p.GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
p.transform.position = new Vector3(186.1f, 28.5f, -63.25f);
if (cc != null) cc.enabled = true;
var oc = UnityEngine.Object.FindFirstObjectByType<OnFootCamera>();
oc.LookAt(new Vector3(189.6f, 28.7f, -61.2f));
return p.transform.position.ToString("F2") + " yaw " + oc.Yaw + " pitch " + oc.Pitch;
