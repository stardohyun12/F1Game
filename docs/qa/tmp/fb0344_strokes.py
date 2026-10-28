import json
L = open('tools/viewer/feedback/unity-Map_TownV6.jsonl', encoding='utf8').read().splitlines()
cs = []
for i in (7, 8, 9):
    d = json.loads(L[i]); c = d['camera']
    s = [p for st in d['strokes'] for p in st]
    s = s[::max(1, len(s) // 50)]
    flat = ','.join(f'{p[0]}f,{p[1]}f' for p in s)
    cs.append(f'R(new Vector3({c["pos"][0]}f,{c["pos"][1]}f,{c["pos"][2]}f), new Vector3({c["target"][0]}f,{c["target"][1]}f,{c["target"][2]}f), new float[]{{{flat}}});')
body = '\n'.join(cs)
code = '''var sb = new System.Text.StringBuilder();
var go = new GameObject("tmpcam"); var cam = go.AddComponent<Camera>(); cam.fieldOfView = 60; cam.aspect = 1280f/552f;
System.Action<Vector3, Vector3, float[]> R = (pos, tg, a) => {
  cam.transform.position = pos; cam.transform.LookAt(tg); sb.Append("#");
  for (int k = 0; k < a.Length; k += 2) {
    var ray = cam.ViewportPointToRay(new Vector3(a[k], 1f - a[k+1], 0));
    RaycastHit h; if (Physics.Raycast(ray, out h, 2000f)) sb.Append($"{h.point.x:F1},{h.point.y:F1},{h.point.z:F1},{h.collider.name.Substring(0, System.Math.Min(14, h.collider.name.Length))};");
  }
  sb.Append("\\n");
};
''' + body + '''
UnityEngine.Object.DestroyImmediate(go);
return sb.ToString();'''
open('docs/qa/tmp/fb0344_ray.cs', 'w', encoding='utf8').write(code)
