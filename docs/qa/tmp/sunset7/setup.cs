// 7판 씬 배선(편집 모드): 연출물 루트 + 실루엣 머티리얼(씬 전용) + SunsetLook·FishingSpot·바다 값. 저장은 save.cs 가 따로.
var look = SunsetLook.Find(); var root = look.transform;
var matPath = "Assets/Scenes/Map_TownV6_Mats/TV6_DuskSil.mat";
var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
if (mat == null) { mat = new Material(Shader.Find("PitCrew/Look/DuskSilhouette")) { name = "TV6_DuskSil" }; UnityEditor.AssetDatabase.CreateAsset(mat, matPath); }
mat.SetColor("_Color", new Color(__SILR__f, __SILG__f, __SILB__f));
var bt = root.Find("DuskBackdrop"); GameObject bg;
if (bt == null) { bg = new GameObject("DuskBackdrop"); bg.transform.SetParent(root, false); bg.AddComponent<DuskBackdrop>(); } else bg = bt.gameObject;
var bd = bg.GetComponent<DuskBackdrop>();
bd.origin = GameObject.Find("Sunset_D227/Cam_Sunset").transform; bd.material = mat;
bd.pieces = new System.Collections.Generic.List<DuskBackdrop.Piece> {
  new DuskBackdrop.Piece { kind = DuskBackdrop.Kind.Headland, azimuth = __HAZ__f, distance = 420, width = __HW__f, height = __HH__f, tipSide = -1, haze = __HHAZE__f, baseY = 0, seed = 3 },
  new DuskBackdrop.Piece { kind = DuskBackdrop.Kind.Lighthouse, azimuth = __LAZ__f, distance = 417, width = 1.0f, height = 0, tipSide = 0, haze = __HHAZE__f, baseY = __LY__f, seed = 0 },
  new DuskBackdrop.Piece { kind = DuskBackdrop.Kind.Island, azimuth = __IAZ__f, distance = 640, width = 140, height = 13, tipSide = 1, haze = 0.62f, baseY = 0, seed = 7 },
  new DuskBackdrop.Piece { kind = DuskBackdrop.Kind.Boat, azimuth = __BAZ__f, distance = __BD__f, width = 1.0f, height = 0, tipSide = 0, haze = 0.10f, baseY = 0, seed = 0 },
};
look.backdrop = bg; bg.SetActive(look.IsOn);
look.sunAzimuth = __SUNAZ__f;
look.shade = new Color(__SHR__f, __SHG__f, __SHB__f); look.a7Shade = new Color(__A7R__f, __A7G__f, __A7B__f);
look.fogStartEnd = new Vector2(__FS__f, __FE__f);
var spot = UnityEngine.Object.FindFirstObjectByType<FishingSpot>(); spot.castYaw = __CASTYAW__f; spot.seatedFov = __FOV__f; spot.seatLookDown = __DOWN__f;
var sea = look.seaMaterial; sea.SetVector("_Cool", new Vector4(__COOL__f, 0.6f, 0, 0)); sea.SetColor("_CoolCol", new Color(__CR__f, __CG__f, __CB__f));
UnityEditor.EditorUtility.SetDirty(look); UnityEditor.EditorUtility.SetDirty(bd); UnityEditor.EditorUtility.SetDirty(spot); UnityEditor.EditorUtility.SetDirty(sea); UnityEditor.EditorUtility.SetDirty(mat);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(look.gameObject.scene);
if (bg.activeInHierarchy) bd.Rebuild();
return "ok backdrop=" + bg.activeSelf + " pieces=" + bd.pieces.Count + " sunAz=" + look.sunAzimuth;
