using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 기능 1 검증 봇(과제 20261030-2243): 방 → 방문 → 계단 → 현관 → 돌계단 → 인도 → 차고 → 카트 타기 → 진입로 → 큰길 → 내리기.
/// 키·마우스 대신 OnFootPlayer.sim* · OnFootCamera.Yaw · OnFootDirector.SimInteract · ArcadeCarController.SetMoveInput 에 값을 넣는다.
/// 3 초에 0.3 m 도 못 가면 「막힘」으로 적고 그 점으로 옮겨 계속(막힌 곳을 한 번에 다 모으려고).
/// Play 중 exec 로 OnFootRouteBot.Begin(증거폴더, 로그폴더). 결과 = 로그폴더/route.txt, 진행 = OnFootRouteBot.Status.
/// </summary>
public class OnFootRouteBot : MonoBehaviour
{
    public static string Status = "idle";

    struct Step { public string kind, label; public Vector2 p; public Step(string k, string l, float x, float z) { kind = k; label = l; p = new Vector2(x, z); } }

    // walk = 걸어서 그 점까지 · door = 근처 문 열기(E) · shot = 캡처 · board = 카트 타기 · drive = 차로 그 점까지 · exit = 서서 내리기
    static readonly Step[] Route =
    {
        new Step("shot", "01_room", 0, 0),
        // 20261030-2334 house_home 9차: 방문 = 방 뒷벽(집 z -0.5) 동쪽 끝, 계단참 통로 = RAIL_02 끝 ~ 동쪽 벽 0.86 m.
        //   점은 집 로컬 → 월드(집 (185, 25.27, -61) yaw 190). 방문은 복도 쪽으로 열려 통로를 반쯤 막으니 지나서 닫는다.
        //   지나는 점 = 문턱에서 0.65 m 복도 안쪽(로컬 z -1.15) — 문턱(z -0.6)에서 닫으면 문짝이 사람을 방 쪽으로 밀어냄(1회차 막힘).
        new Step("walk", "방문 앞", 187.94f, -61.62f),
        new Step("door", "방문", 0, 0),
        new Step("walk", "방문 지나", 188.06f, -60.37f),
        new Step("close", "방문 닫기", 0, 0),
        new Step("walk", "복도", 188.20f, -60.14f),
        new Step("walk", "계단참 입구", 188.82f, -60.05f),
        new Step("walk", "계단참", 189.14f, -59.39f),
        new Step("walk", "계단 위", 188.66f, -59.26f),
        new Step("shot", "02_landing", 0, 0),
        new Step("walk", "계단 아래", 184.62f, -58.55f),
        new Step("walk", "1층 계단 앞", 184.42f, -59.68f),
        new Step("walk", "현관 앞", 184.62f, -63.17f),
        new Step("door", "현관문", 0, 0),
        new Step("walk", "현관 밖", 184.6f, -65.0f),
        new Step("close", "현관문 닫기", 0, 0),
        new Step("walk", "마당", 184.8f, -66.2f),
        new Step("shot", "03_outside_door", 0, 0),
        new Step("walk", "마당 동쪽", 194.6f, -68.3f),
        new Step("walk", "돌계단 위", 196.3f, -68.6f),
        new Step("walk", "돌계단 아래", 197.0f, -76.9f),
        new Step("walk", "집 앞 인도", 197.3f, -77.9f),
        new Step("walk", "인도 200", 200.0f, -77.4f),
        new Step("walk", "인도 205", 205.0f, -76.5f),
        new Step("shot", "04_sidewalk", 0, 0),
        new Step("walk", "인도 210", 210.0f, -75.6f),
        new Step("walk", "인도 214", 214.3f, -74.9f),
        new Step("walk", "차고 계단 앞", 215.6f, -74.6f),
        new Step("walk", "차고 계단 아래", 215.6f, -73.7f),
        new Step("walk", "차고 계단 위", 215.4f, -65.0f),
        new Step("walk", "골목 222", 222.0f, -64.0f),
        new Step("walk", "골목 230", 230.0f, -63.3f),
        new Step("walk", "차고 마당", 229.4f, -57.6f),
        new Step("shot", "05_beside_cart", 0, 0),
        new Step("board", "카트 타기", 0, 0),
        new Step("drive", "진입로 시작", 233.5f, -48.0f),
        new Step("drive", "진입로 굽이", 238.5f, -46.8f),
        new Step("drive", "진입로 중간", 243.0f, -51.0f),
        new Step("shot", "06_cart_driving", 0, 0),
        new Step("drive", "진입로 아래", 245.5f, -57.0f),
        new Step("drive", "큰길 합류", 244.5f, -64.0f),
        new Step("drive", "큰길", 240.0f, -70.5f),
        new Step("drive", "큰길 서쪽", 230.0f, -72.0f),
        new Step("exit", "내리기", 0, 0),
    };

    string evDir, logDir;
    OnFootDirector dir;
    readonly StringBuilder log = new StringBuilder();
    readonly List<string> stuck = new List<string>();

    public static string Begin(string evidenceDir, string logDirectory)
    {
        var d = FindFirstObjectByType<OnFootDirector>();
        if (d == null) return "FAIL: OnFootDirector 없음";
        var old = FindFirstObjectByType<OnFootRouteBot>();
        if (old != null) Destroy(old.gameObject);
        var bot = new GameObject("_OnFootRouteBot").AddComponent<OnFootRouteBot>();
        bot.evDir = evidenceDir; bot.logDir = logDirectory; bot.dir = d;
        Status = "starting";
        return "OK";
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(evDir); Directory.CreateDirectory(logDir);
        float wait = 0f;
        while (dir.State == OnFootDirector.Mode.Starting && wait < 10f) { wait += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSeconds(0.5f);
        var pl = dir.Player; var cam = pl.Cam;
        pl.simActive = true; cam.simActive = true;
        float tStart = Time.time, tSeg = Time.time;
        Line($"시작 {P(pl.transform.position)}");
        foreach (var s in Route)
        {
            Status = s.label;
            switch (s.kind)
            {
                case "shot": yield return Shot(s.label); break;
                case "walk": yield return Walk(s); break;
                case "door":
                case "close":
                {
                    string want = s.kind == "door" ? "열기" : "닫기";
                    float w = 0f;
                    while (!dir.Prompt.Contains(want) && w < 2f) { w += Time.deltaTime; yield return null; }
                    if (!dir.Prompt.Contains(want)) { Stuck(s.label, $"안내 '{dir.Prompt}' ({want} 아님)"); break; }
                    dir.SimInteract();
                    yield return new WaitForSeconds(0.55f);
                    break;
                }
                case "board":
                {
                    pl.simMove = Vector2.zero;
                    float w = 0f;
                    while (dir.Prompt != "E 타기" && w < 2f) { w += Time.deltaTime; yield return null; }
                    if (dir.Prompt != "E 타기") { Stuck(s.label, $"안내 '{dir.Prompt}'"); Status = "FAIL board"; Finish(tStart); yield break; }
                    Seg("걷기 끝(카트 앞)", ref tSeg);
                    dir.SimInteract();
                    yield return null; yield return null;
                    if (dir.State != OnFootDirector.Mode.InCart) { Stuck(s.label, "타기 안 됨"); Status = "FAIL board"; Finish(tStart); yield break; }
                    yield return new WaitForSeconds(0.6f);
                    break;
                }
                case "drive": yield return Drive(s); break;
                case "exit":
                {
                    float w = 0f;
                    while (Mathf.Abs(dir.Car.CurrentSpeedMps) > 0.5f && w < 6f) { dir.Car.SetMoveInput(new Vector2(0f, -0.4f * Mathf.Sign(dir.Car.CurrentSpeedMps))); w += Time.deltaTime; yield return null; }
                    dir.Car.SetMoveInput(Vector2.zero);
                    yield return new WaitForSeconds(0.3f);
                    Seg("운전 끝", ref tSeg);
                    w = 0f;
                    while (dir.Prompt != "E 내리기" && w < 3f) { w += Time.deltaTime; yield return null; }
                    dir.SimInteract();
                    yield return null; yield return null;
                    Line(dir.State == OnFootDirector.Mode.OnFoot ? $"내림 OK 사람={P(pl.transform.position)}" : $"내리기 실패 안내='{dir.Prompt}' 속도={dir.Car.CurrentSpeedMps:F2}");
                    if (dir.State != OnFootDirector.Mode.OnFoot) stuck.Add("내리기");
                    break;
                }
            }
            if (s.kind == "walk" || s.kind == "drive") Line($"{s.label} 도착 t={Time.time - tStart:F1}s {P(s.kind == "walk" ? pl.transform.position : dir.Car.transform.position)}");
        }
        pl.simMove = Vector2.zero;
        Finish(tStart);
    }

    IEnumerator Walk(Step s)
    {
        var pl = dir.Player; var cam = pl.Cam;
        float best = float.MaxValue, lastImprove = Time.time, t0 = Time.time;
        while (true)
        {
            Vector3 p = pl.transform.position;
            Vector2 d = s.p - new Vector2(p.x, p.z);
            float dist = d.magnitude;
            if (dist < 0.3f) break;
            if (dist < best - 0.3f) { best = dist; lastImprove = Time.time; }
            if (Time.time - lastImprove > 3f || Time.time - t0 > 25f)
            {
                Stuck(s.label, $"남은 {dist:F2}m 사람={P(p)}");
                Vector3 to = new Vector3(s.p.x, p.y + 0.5f, s.p.y);
                // 천장에 안 걸리게 허리 높이에서 아래로(지붕 위로 올라간 적 있음)
                if (Physics.Raycast(new Vector3(s.p.x, p.y + 0.9f, s.p.y), Vector3.down, out var h, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) to.y = h.point.y + 0.05f;
                pl.Teleport(to, pl.transform.eulerAngles.y);
                yield return null;
                break;
            }
            float want = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            cam.Yaw = Mathf.MoveTowardsAngle(cam.Yaw, want, 360f * Time.deltaTime);
            float off = Mathf.Abs(Mathf.DeltaAngle(cam.Yaw, want));
            pl.simMove = new Vector2(0f, off > 60f ? 0.2f : (dist < 0.8f ? 0.6f : 1f));
            yield return null;
        }
        pl.simMove = Vector2.zero;
    }

    IEnumerator Drive(Step s)
    {
        var car = dir.Car;
        float best = float.MaxValue, lastImprove = Time.time, t0 = Time.time;
        while (true)
        {
            Transform c = car.transform;
            Vector2 d = s.p - new Vector2(c.position.x, c.position.z);
            float dist = d.magnitude;
            if (dist < 2.5f) break;
            if (dist < best - 0.3f) { best = dist; lastImprove = Time.time; }
            if (Time.time - lastImprove > 4f || Time.time - t0 > 30f)
            {
                Stuck(s.label, $"남은 {dist:F1}m 차={P(c.position)} 속도={car.CurrentSpeedMps:F1}");
                break;
            }
            float want = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
            float err = Mathf.DeltaAngle(c.eulerAngles.y, want);
            float steer = Mathf.Clamp(err / 35f, -1f, 1f);
            float v = car.CurrentSpeedMps;
            float target = Mathf.Abs(err) > 40f ? 4f : 8f;
            float thr = v < target ? 0.75f : (v > target + 2f ? -0.3f : 0.1f);
            car.SetMoveInput(new Vector2(steer, thr));
            yield return null;
        }
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        var cam = Camera.main; var lr = cam.GetComponent<LowResRenderTest>();
        var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var tx = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        if (lr != null && lr.enabled) { cam.Render(); lr.Present(rt); }
        else { var prevT = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prevT; }
        var prev = RenderTexture.active; RenderTexture.active = rt;
        tx.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tx.Apply(); RenderTexture.active = prev;
        File.WriteAllBytes(Path.Combine(evDir, name + ".png"), tx.EncodeToPNG());
        rt.Release(); Destroy(rt); Destroy(tx);
        Line($"캡처 {name} 안내='{dir.Prompt}'");
    }

    void Seg(string label, ref float tSeg) { Line($"구간 {label}: {Time.time - tSeg:F1}s"); tSeg = Time.time; }
    void Stuck(string label, string why) { stuck.Add(label); Line($"막힘 {label}: {why}"); }
    void Line(string s) { log.AppendLine(s); }
    static string P(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:F2},{1:F2},{2:F2})", v.x, v.y, v.z);

    void Finish(float tStart)
    {
        Line($"총 {Time.time - tStart:F1}s 막힘 {stuck.Count}곳: {string.Join(", ", stuck)}");
        Line("감독 기록:"); foreach (var l in dir.Log) Line("  " + l);
        File.WriteAllText(Path.Combine(logDir, "route.txt"), log.ToString(), new UTF8Encoding(false));
        if (!Status.StartsWith("FAIL")) Status = $"done stuck={stuck.Count}";
    }
}
