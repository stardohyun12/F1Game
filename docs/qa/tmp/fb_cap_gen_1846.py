# artlive 1846: 코멘트 카메라(glTF 좌표, FBX 원점) → Unity 캡처 스크립트 생성. 모델 루트(슬롯/<건물>/<fbx 자식>) 위치 + (−x, y, z) 월드 m.
# (depot·postoffice 루트는 rot −90°X·scale 100 이라 TransformPoint 를 쓰면 100배로 튄다)
# python -X utf8 docs/qa/tmp/fb_cap_gen_1846.py → docs/qa/tmp/fb_cap_1846.cs (SessionState "fb.cap" = 태그)
import json, glob, os
IDS = None  # 전부
jobs = []
for f in sorted(glob.glob('tools/viewer/feedback/*.jsonl')):
    b = os.path.basename(f)[:-6]
    for l in open(f, encoding='utf-8'):
        if not l.strip(): continue
        d = json.loads(l); c = d['camera']; jobs.append((d['id'], b, c['pos'], c['target'], c.get('fov', 50)))
V = lambda v: f'new Vector3({-v[0]}f, {v[1]}f, {v[2]}f)'
rows = ',\n'.join(f'  ("{i}", "{b}", {V(p)}, {V(t)}, {fv}f)' for i, b, p, t, fv in jobs)
cs = open('docs/qa/tmp/grain_cap_1722.cs', encoding='utf-8').read()
head = cs[:cs.index('var jobs')]
body = '''var jobs = new (string id, string asset, Vector3 c, Vector3 l, float fov)[] {
''' + rows + ''' };
tag = UnityEditor.SessionState.GetString("fb.cap", "x");
var dst = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var log = new System.Text.StringBuilder();
var cams = new System.Collections.Generic.List<Camera>(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
var was = new System.Collections.Generic.Dictionary<Camera, bool>(); foreach (var c in cams) if (c.GetComponent<LowResRenderTest>()) was[c] = c.gameObject.activeSelf;
var lt = UnityEngine.Object.FindFirstObjectByType<A7LightToggles>(); if (lt) { lt.painterly = false; lt.realtimeShadows = true; lt.Apply(); }
Camera cam = null; foreach (var c in cams) if (c.name == "Cam_ST_in") cam = c;
foreach (var c in was.Keys) c.gameObject.SetActive(c == cam);
var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
string outDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../docs/qa/evidence/feedback-1028/")); System.IO.Directory.CreateDirectory(outDir);
foreach (var j in jobs) {
  foreach (Transform t in slot.transform) t.gameObject.SetActive(t.name == j.asset);
  var mdl = slot.transform.Find(j.asset).GetChild(0);
  cam.transform.position = mdl.position + j.c; cam.transform.LookAt(mdl.position + j.l); cam.fieldOfView = j.fov;
  var lr = cam.GetComponent<LowResRenderTest>(); int h0 = lr.targetHeight; bool s0 = lr.smooth; lr.targetHeight = 720; lr.smooth = false; lr.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
  cam.Render(); lr.Present(dst);
  var prev = RenderTexture.active; RenderTexture.active = dst; var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
  System.IO.File.WriteAllBytes(outDir + j.id + "_" + tag + ".png", tx.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tx);
  lr.targetHeight = h0; lr.smooth = s0; log.Append(j.id + " "); }
cam.transform.position = p0; cam.transform.rotation = r0; cam.fieldOfView = f0;
foreach (var kv in was) if (kv.Key) kv.Key.gameObject.SetActive(kv.Value); dst.Release();
foreach (var kv in on) if (kv.Key) kv.Key.SetActive(kv.Value);
return "OK " + log;
'''
open('docs/qa/tmp/fb_cap_1846.cs', 'w', encoding='utf-8').write(head.replace('"grain.cap"', '"fb.cap"') + body)
print(len(jobs), 'jobs')
