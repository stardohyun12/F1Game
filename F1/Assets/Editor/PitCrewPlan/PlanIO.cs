using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PitCrewPlan
{
    /// <summary>
    /// 기획 창의 파일·씬 작업 (웹 뷰어 tools/viewer/serve.py 와 같은 파일 형식).
    /// 코멘트 = tools/viewer/feedback/unity-&lt;씬&gt;.jsonl, 스샷 = feedback/shots/&lt;id&gt;.png,
    /// 배치 = docs/design/tutorial/placements.yaml(places 만 다시 씀, 값이 같으면 안 씀), 잠금 = .claude/team/editor.lock.
    /// 좌표는 전부 Unity 월드(m). 배치의 피벗 = 프리팹 루트(웹 뷰어의 '바닥 가운데' 와 다르다).
    /// </summary>
    public static class PlanIO
    {
        public const string LibraryDir = "Assets/Art/Library/Buildings/";
        static readonly CultureInfo IC = CultureInfo.InvariantCulture;

        public static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        public static string FeedbackDir => Path.Combine(RepoRoot, "tools/viewer/feedback");
        public static string PlacementsPath => Path.Combine(RepoRoot, "docs/design/tutorial/placements.yaml");
        public static string LockPath => Path.Combine(RepoRoot, ".claude/team/editor.lock");
        public static string JsonlPath(string scene) => Path.Combine(FeedbackDir, "unity-" + scene + ".jsonl");

        // ───────── 숫자·JSON 쓰기 ─────────
        public static string F(float v, int nd = 2) => Math.Round(v, nd).ToString("0.##", IC);
        public static string Arr(Vector3 v, int nd = 3) => "[" + Math.Round(v.x, nd).ToString(IC) + "," + Math.Round(v.y, nd).ToString(IC) + "," + Math.Round(v.z, nd).ToString(IC) + "]";
        public static string Q(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder("\"");
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
                }
            }
            return sb.Append('"').ToString();
        }
        public static float Yaw(Transform t) { var y = Mathf.Repeat(t.eulerAngles.y + 180f, 360f) - 180f; return y <= -180f ? 180f : y; }
        public static string Now() => DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", IC);

        // ───────── 씬 조회 ─────────
        public static string PathOf(Transform t) { var p = t.name; for (var q = t.parent; q != null; q = q.parent) p = q.name + "/" + p; return p; }

        /// <summary>라이브러리 프리팹 인스턴스 루트(가장 가까운 것). 없으면 null.</summary>
        public static GameObject LibraryRoot(GameObject go)
        {
            for (var t = go.transform; t != null; t = t.parent)
                if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).StartsWith(LibraryDir))
                    return t.gameObject;
            return null;
        }
        /// <summary>코멘트의 building 필드: 라이브러리 프리팹 이름 → 바깥 프리팹 이름 → 씬 루트 이름.</summary>
        public static string BuildingName(GameObject go)
        {
            if (go == null) return null;
            var lib = LibraryRoot(go);
            if (lib != null) return Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(lib));
            var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            if (outer != null) { var p = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(outer); return string.IsNullOrEmpty(p) ? outer.name : Path.GetFileNameWithoutExtension(p); }
            return go.transform.root.name;
        }
        public static GameObject FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var parts = path.Split('/');
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r.name != parts[0]) continue;
                var t = r.transform;
                for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]);
                if (t != null) return t.gameObject;
            }
            return null;
        }
        public static bool TryBounds(GameObject go, out Bounds b)
        {
            b = default; bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any;
        }
        public static List<GameObject> LibraryInstances() => LibraryInstances(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        public static List<GameObject> LibraryInstances(UnityEngine.SceneManagement.Scene scene)
        {
            var list = new List<GameObject>();
            foreach (var r in scene.GetRootGameObjects())
                foreach (var t in r.GetComponentsInChildren<Transform>(true))
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject).StartsWith(LibraryDir))
                        list.Add(t.gameObject);
            return list;
        }
        public static List<string> LibraryPrefabs() =>
            AssetDatabase.FindAssets("t:Prefab", new[] { LibraryDir.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();

        // ───────── 접지 ─────────
        /// <summary>바운즈 가운데 아래로 레이캐스트(자기 콜라이더·트리거 제외) → 렌더러 바닥을 맞은 높이에 붙인다.</summary>
        public static bool Ground(GameObject go, out float dy)
        {
            dy = 0;
            if (!TryBounds(go, out var b)) return false;
            Physics.SyncTransforms();
            var origin = new Vector3(b.center.x, b.max.y + 200f, b.center.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 2000f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance);
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(go.transform)) continue;
                dy = h.point.y - b.min.y;
                Undo.RecordObject(go.transform, "접지");
                go.transform.position += Vector3.up * dy;
                return true;
            }
            return false;
        }

        // ───────── 코멘트 jsonl ─────────
        public class Comment
        {
            public string raw, id, time, building, comment, status, screenshot, nodePath;
            public Vector3? camPos, camTarget, point; public float fov = 60f;
            public bool Done => status == "done";
        }

        public static void AppendLine(string scene, string json)
        {
            Directory.CreateDirectory(FeedbackDir);
            File.AppendAllText(JsonlPath(scene), json + "\n", new UTF8Encoding(false));
        }

        public static List<Comment> ReadComments(string scene)
        {
            var list = new List<Comment>();
            var path = JsonlPath(scene);
            if (!File.Exists(path)) return list;
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                Dictionary<string, object> d;
                try { d = MiniJson.Parse(line) as Dictionary<string, object>; } catch { continue; }
                if (d == null) continue;
                var c = new Comment { raw = line, id = S(d, "id"), time = S(d, "time"), building = S(d, "building"), comment = S(d, "comment"), status = S(d, "status"), screenshot = S(d, "screenshot") };
                if (d.TryGetValue("nodes", out var n) && n is List<object> nl && nl.Count > 0 && nl[0] is Dictionary<string, object> n0) c.nodePath = S(n0, "path");
                c.point = V(d, "point");
                if (d.TryGetValue("camera", out var cam) && cam is Dictionary<string, object> cd)
                {
                    c.camPos = V(cd, "pos"); c.camTarget = V(cd, "target");
                    if (cd.TryGetValue("fov", out var f) && f is double fd) c.fov = (float)fd;
                }
                list.Add(c);
            }
            return list;
        }
        static string S(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) && v != null ? Convert.ToString(v, IC) : null;
        static Vector3? V(Dictionary<string, object> d, string k) =>
            d.TryGetValue(k, out var v) && v is List<object> l && l.Count == 3 ? new Vector3(Convert.ToSingle(l[0], IC), Convert.ToSingle(l[1], IC), Convert.ToSingle(l[2], IC)) : (Vector3?)null;

        /// <summary>그 id 줄의 status 만 바꾼다(나머지 글자는 그대로).</summary>
        public static bool SetStatus(string scene, string id, string status)
        {
            var path = JsonlPath(scene);
            if (!File.Exists(path)) return false;
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            bool hit = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("\"id\":" + Q(id))) continue;
                var d = MiniJson.Parse(lines[i]) as Dictionary<string, object>;
                var old = d != null && d.TryGetValue("status", out var s) && s != null ? Q(Convert.ToString(s, IC)) : "null";
                lines[i] = lines[i].Replace("\"status\":" + old, "\"status\":" + Q(status));
                hit = true;
            }
            if (hit) File.WriteAllText(path, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
            return hit;
        }

        // ───────── 캡처 ─────────
        /// <summary>source 카메라를 그대로 복사한 임시 카메라로 w×h PNG 를 굽는다(씬 뷰·게임 뷰 공통, URP 에서도 안전).</summary>
        /// strokes = 그린 선(0~1, 왼쪽 위 원점). 있으면 빨간 선을 그림 위에 합성한다.
        public static bool RenderPng(Camera source, string file, int w = 1280, int h = 720, List<List<Vector2>> strokes = null)
        {
            if (source == null) return false;
            var go = new GameObject("_PlanCapture") { hideFlags = HideFlags.HideAndDontSave };
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.CopyFrom(source);
                cam.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                cam.aspect = (float)w / h; cam.targetTexture = rt; cam.enabled = false;
                cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                RenderTexture.active = prev;
                if (strokes != null && strokes.Count > 0) DrawStrokes(tex, strokes, Mathf.Max(2, w / 320));
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllBytes(file, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                return true;
            }
            finally { RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(go); }
        }

        /// <summary>선마다 반지름 r 픽셀 원을 1px 간격으로 찍어 굵은 빨간 선을 만든다(텍스처는 아래가 y=0).</summary>
        public static void DrawStrokes(Texture2D tex, List<List<Vector2>> strokes, int r)
        {
            int w = tex.width, h = tex.height; var px = tex.GetPixels32(); var red = new Color32(235, 40, 40, 255);
            void Dot(int cx, int cy)
            {
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r) continue;
                    int x = cx + dx, y = cy + dy; if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    px[y * w + x] = red;
                }
            }
            foreach (var s in strokes)
                for (int i = 0; i < s.Count; i++)
                {
                    var a = new Vector2(s[i].x * w, (1f - s[i].y) * h); var b = i + 1 < s.Count ? new Vector2(s[i + 1].x * w, (1f - s[i + 1].y) * h) : a;
                    int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
                    for (int k = 0; k <= n; k++) { var p = Vector2.Lerp(a, b, (float)k / n); Dot(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y)); }
                }
            tex.SetPixels32(px); tex.Apply();
        }

        // ───────── 배치 yaml ─────────
        public struct Place { public float x, z, y, yaw; }

        /// <summary>placements.yaml 의 places 블록만 읽는다(웹 뷰어가 쓰는 2칸 들여쓰기 형식).</summary>
        public static Dictionary<string, Place> ReadPlaces(out List<string> otherSections)
        {
            var map = new Dictionary<string, Place>(); otherSections = new List<string>();
            if (!File.Exists(PlacementsPath)) return map;
            string sec = null, cur = null; var other = new StringBuilder();
            foreach (var line in File.ReadAllLines(PlacementsPath, Encoding.UTF8))
            {
                if (line.StartsWith("#") || line.Trim().Length == 0) continue;
                if (!line.StartsWith(" "))
                {
                    if (other.Length > 0) { otherSections.Add(other.ToString()); other.Clear(); }
                    sec = line.Split(':')[0].Trim();
                    if (sec != "places" && sec != "plan" && sec != "saved") other.AppendLine(line);
                    continue;
                }
                if (sec != "places") { if (sec != "plan" && sec != "saved") other.AppendLine(line); continue; }
                var tl = line.Trim();
                if (line.StartsWith("  ") && !line.StartsWith("   ")) { cur = tl.TrimEnd(':'); map[cur] = new Place(); continue; }
                if (cur == null) continue;
                var p = map[cur];
                if (tl.StartsWith("world:")) { var a = tl.Substring(6).Trim().Trim('[', ']').Split(','); p.x = float.Parse(a[0], IC); p.z = float.Parse(a[1], IC); }
                else if (tl.StartsWith("yaw:")) p.yaw = float.Parse(tl.Substring(4), IC);
                else if (tl.StartsWith("y:")) p.y = float.Parse(tl.Substring(2), IC);
                map[cur] = p;
            }
            if (other.Length > 0) otherSections.Add(other.ToString());
            return map;
        }

        /// <summary>씬의 라이브러리 인스턴스 → places. 바뀐 게 없으면 파일을 안 건드린다(diff 0). 바뀐 줄 목록을 돌려준다.</summary>
        public static List<string> WritePlacements(UnityEngine.SceneManagement.Scene sc, out string error)
        {
            error = null; var scene = sc.name;
            var old = ReadPlaces(out var others);
            var now = new Dictionary<string, Place>();
            foreach (var go in LibraryInstances(sc))
            {
                var id = Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go));
                var key = id; for (int k = 2; now.ContainsKey(key); k++) key = id + "_" + k;
                var t = go.transform;
                now[key] = new Place { x = R(t.position.x), z = R(t.position.z), y = R(t.position.y), yaw = R(Yaw(t)) };
            }
            if (now.Count == 0) { error = "라이브러리 프리팹 인스턴스가 씬에 없음"; return new List<string>(); }
            // 씬에 없는 옛 항목은 남긴다(다른 씬/계획에서 쓴 자리일 수 있음)
            foreach (var kv in old) if (!now.ContainsKey(kv.Key)) now[kv.Key] = kv.Value;
            // 옛 파일 순서를 지킨다(같은 값이면 같은 글자)
            now = old.Keys.Concat(now.Keys.Where(k => !old.ContainsKey(k))).ToDictionary(k => k, k => now[k]);

            var delta = new List<string>();
            foreach (var kv in now)
            {
                var n = kv.Value;
                if (!old.TryGetValue(kv.Key, out var o)) { delta.Add(kv.Key + " 새로 (" + F(n.x) + ", " + F(n.z) + "), 높이 " + F(n.y) + ", yaw " + F(n.yaw)); continue; }
                if (o.x == n.x && o.z == n.z && o.y == n.y && o.yaw == n.yaw) continue;
                delta.Add(kv.Key + " (" + F(o.x) + ", " + F(o.z) + ")→(" + F(n.x) + ", " + F(n.z) + "), 높이 " + F(o.y) + "→" + F(n.y) + ", yaw " + F(o.yaw) + "→" + F(n.yaw));
            }
            if (delta.Count == 0) return delta;

            var stamp = Now();
            var sb = new StringBuilder();
            sb.Append("# 기획 '배치' 저장분 — 자리만. first-delivery.yaml 은 그대로 두고 뷰어가 이 값을 위에 덮어 씀.\n");
            sb.Append("# 좌표 = Unity 월드: world [x, z], y = 피벗 높이, yaw = Unity Y 회전(도). 피벗 = 프리팹 루트(Unity 기획 창 기준; 2026-10-29~).\n");
            sb.Append("# 마지막 저장 " + stamp + " (Unity 기획 창, 씬=" + scene + "). 자동 생성 — 손으로 고쳐도 되지만 다음 저장에 통째로 덮어 씀.\n");
            sb.Append("plan: first-delivery\nsaved: '" + stamp + "'\nplaces:\n");
            foreach (var kv in now)
                sb.Append("  " + kv.Key + ":\n    world: [" + F(kv.Value.x) + ", " + F(kv.Value.z) + "]\n    y: " + F(kv.Value.y) + "\n    yaw: " + F(kv.Value.yaw) + "\n");
            foreach (var o in others) sb.Append(o);
            var tmp = PlacementsPath + ".tmp";
            File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
            File.Copy(tmp, PlacementsPath, true); File.Delete(tmp);

            var id2 = "unity-" + scene + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", IC) + "-placement";
            AppendLine(scene, "{\"id\":" + Q(id2) + ",\"time\":" + Q(stamp) + ",\"building\":" + Q("plan-first-delivery") + ",\"kind\":\"placement\",\"target\":\"placements\",\"nodes\":[],\"point\":null,\"camera\":null,"
                + "\"comment\":" + Q("[배치] " + string.Join(" / ", delta)) + ",\"changes\":[" + string.Join(",", delta.Select(Q)) + "],\"screenshot\":null,\"file\":\"docs/design/tutorial/placements.yaml\",\"status\":\"open\"}");
            return delta;
        }
        static float R(float v) => (float)Math.Round(v, 2);

        // ───────── 에디터 잠금 ─────────
        /// <summary>도현이 창을 쓰는 중임을 팀 스크립트에 알린다. 다른 역할(팀원)이 잡고 있으면 덮어쓰지 않는다.</summary>
        public static string LockOwner() { try { return File.Exists(LockPath) ? File.ReadAllText(LockPath).Trim() : null; } catch { return null; } }
        public static bool TouchLock()
        {
            var owner = LockOwner();
            if (owner != null && !owner.StartsWith("director")) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(LockPath));
            File.WriteAllText(LockPath, "director " + Now() + "\n");
            return true;
        }
        public static void ReleaseLock()
        {
            var owner = LockOwner();
            if (owner != null && owner.StartsWith("director")) try { File.Delete(LockPath); } catch { }
        }

        // ───────── D-201 튜토리얼 물건 치우기 ─────────
        public const string HoldName = "_TUT_HOLD";
        /// <summary>TUT_* 와 OpeningTutorial 을 비활성 루트 _TUT_HOLD 아래로(위치 유지). 참조는 그대로 산다. 옮긴 개수.</summary>
        public static int HoldTutorial()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var hold = scene.GetRootGameObjects().FirstOrDefault(g => g.name == HoldName);
            if (hold == null) { hold = new GameObject(HoldName); Undo.RegisterCreatedObjectUndo(hold, "튜토리얼 치우기"); }
            Undo.RecordObject(hold, "튜토리얼 치우기"); hold.SetActive(false);
            var pick = new List<Transform>();
            foreach (var r in scene.GetRootGameObjects())
            {
                if (r == hold) continue;
                foreach (var t in r.GetComponentsInChildren<Transform>(true))
                {
                    bool want = t.name.StartsWith("TUT_") || t.GetComponent("OpeningTutorial") != null;
                    if (!want) continue;
                    bool parentPicked = false;
                    for (var p = t.parent; p != null; p = p.parent) if (pick.Contains(p)) { parentPicked = true; break; }
                    if (!parentPicked) pick.Add(t);
                }
            }
            foreach (var t in pick) Undo.SetTransformParent(t, hold.transform, "튜토리얼 치우기");
            return pick.Count;
        }
    }

    /// <summary>jsonl 한 줄 읽기용 최소 JSON 파서(객체·배열·문자열·숫자·true/false/null).</summary>
    public static class MiniJson
    {
        public static object Parse(string s) { int i = 0; var v = Val(s, ref i); return v; }
        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
        static object Val(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true) { Ws(s, ref i); var k = Str(s, ref i); Ws(s, ref i); i++; d[k] = Val(s, ref i); Ws(s, ref i); if (s[i++] == '}') return d; }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true) { l.Add(Val(s, ref i)); Ws(s, ref i); if (s[i++] == ']') return l; }
            }
            if (c == '"') return Str(s, ref i);
            if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
            if (s.Substring(i).StartsWith("false")) { i += 5; return false; }
            if (s.Substring(i).StartsWith("null")) { i += 4; return null; }
            int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
        }
        static string Str(string s, ref int i)
        {
            var sb = new StringBuilder(); i++;
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++; char e = s[i];
                    if (e == 'n') sb.Append('\n'); else if (e == 't') sb.Append('\t'); else if (e == 'r') sb.Append('\r');
                    else if (e == 'b') sb.Append('\b'); else if (e == 'f') sb.Append('\f');
                    else if (e == 'u') { sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; }
                    else sb.Append(e);
                    i++;
                }
                else sb.Append(s[i++]);
            }
            i++; return sb.ToString();
        }
    }
}
