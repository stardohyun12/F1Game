using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// D-300 검증용: 포장 2D 보드에서 (1) 감싼 물건의 띠 (2) 구긴 종이를 든 손 (3) 쏟아지는 알갱이 호를 1× 로 찍고
/// (4) 합성음·덜그럭이 실제로 AudioSource.isPlaying 인지 로그로 남긴다.
/// 실행: exec 'return PackVisibleShots.Begin("D:/…/pack-visible");' → PackVisibleShots.Status 가 "done" 이면 끝, Log 에 줄들.
/// </summary>
public class PackVisibleShots : MonoBehaviour
{
    public static string Status = "idle";
    public static string Log = "";
    static string dir;
    OnFootDirector d;
    PackBoard b;
    PackBoardView view;
    object fx; AudioSource fxSrc; FieldInfo fiLast, fiPlayed;
    AudioSource[] benchPool;
    System.Type benchType;

    public static string Begin(string outDir)
    {
        var dd = FindFirstObjectByType<OnFootDirector>();
        if (dd == null) return "no dir";
        dir = outDir; Status = "run"; Log = "";
        new GameObject("PackVisibleShots").AddComponent<PackVisibleShots>().d = dd;
        return "OK";
    }

    void L(string s) { Log += s + "\n"; Debug.Log("[PackVisibleShots] " + s); }

    IEnumerator Wait(System.Func<bool> ok, float sec)
    {
        float w = 0f;
        while (!ok() && w < sec) { w += Time.unscaledDeltaTime; yield return null; }
    }

    IEnumerator Snap(string name)
    {
        yield return new WaitForEndOfFrame();
        System.IO.Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(dir + "/" + name + ".png");
        yield return new WaitForSecondsRealtime(0.4f);
        L("그림 " + name + ".png");
    }

    int SoundsPlayed => fiPlayed != null ? (int)fiPlayed.GetValue(fx) : -1;
    string LastSound => fiLast != null ? (string)fiLast.GetValue(fx) : "?";
    bool BenchPlaying()
    {
        if (benchPool == null) return false;
        foreach (var s in benchPool) if (s != null && s.isPlaying) return true;
        return false;
    }

    /// <summary>action 을 한 번 하고 같은 프레임·다음 프레임에 fx 의 AudioSource 가 재생 중인지 적는다.</summary>
    IEnumerator Probe(string label, System.Action action)
    {
        int n0 = SoundsPlayed;
        action();
        bool same = fxSrc != null && fxSrc.isPlaying; string last = LastSound; int dn = SoundsPlayed - n0;
        yield return null;
        bool next = fxSrc != null && fxSrc.isPlaying;
        L($"소리 {label}: 새 재생 {dn}번 · 이름='{last}' · isPlaying 같은 프레임={same} 다음 프레임={next}");
    }

    IEnumerator Start()
    {
        yield return Wait(() => d.State != OnFootDirector.Mode.Starting, 10f);
        yield return new WaitForSecondsRealtime(1f);

        // 받는 곳 선반에서 물건 집기 → 포장대 [E] → 2D 보드 (DayLoopCheck.Pack2D 와 같은 길)
        var spot = d.PackSpot; var t = spot.V3Table; var parcel = t.receive.today[0];
        var pp0 = parcel.transform.position;
        d.DebugTeleport(new Vector3(pp0.x, 31.1f, pp0.z - 1.1f), 0f, null, 0f); yield return null;
        d.Player.Cam.LookAt(pp0); yield return null; yield return null; yield return null;
        d.SimInteract(); yield return null; yield return null;
        if (!t.Picked) { t.SimUse(parcel); yield return null; }
        yield return new WaitForSecondsRealtime(0.5f);
        d.DebugTeleport(new Vector3(273.2f, 31.1f, -29.5f), 90f, null, 0f); yield return null;
        d.Player.Cam.LookAt(t.workSpot.position); yield return null; yield return null; yield return null;
        d.SimInteract(); yield return null; yield return null;
        if (d.State != OnFootDirector.Mode.Pack) { t.SimUse(t); yield return null; }
        b = spot.Board2D;
        yield return Wait(() => d.State == OnFootDirector.Mode.Pack && b.On, 3f);
        if (!(d.State == OnFootDirector.Mode.Pack && b.On)) { L("FAIL 2D 보드 안 켜짐"); Status = "done"; Destroy(gameObject); yield break; }

        view = b.GetComponent<PackBoardView>();
        var fxF = typeof(PackBoardView).GetField("fx", BindingFlags.Instance | BindingFlags.NonPublic);
        fx = fxF != null ? fxF.GetValue(view) : null;
        if (fx == null) { L("FAIL PackBoardView.fx 없음"); Status = "done"; Destroy(gameObject); yield break; }
        var ft = fx.GetType();
        fxSrc = (AudioSource)ft.GetField("src", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fx);
        fiLast = ft.GetField("LastSound"); fiPlayed = ft.GetField("SoundsPlayed");
        benchType = typeof(PackBoard).Assembly.GetType("PitCrew.PackBench.PackBench");
        var poolF = benchType != null ? benchType.GetField("pool", BindingFlags.Static | BindingFlags.NonPublic) : null;
        benchPool = poolF != null ? (AudioSource[])poolF.GetValue(null) : null;
        var simF = typeof(PackBoard).GetField("simHold", BindingFlags.Instance | BindingFlags.NonPublic);
        L($"보드 켜짐 · fx AudioSource={(fxSrc != null)} · 덜그럭 풀={(benchPool != null ? benchPool.Length : 0)}");

        PackPiece BoxNamed(string ch) { for (int i = 0; i < 3; i++) { var x = b.Find(PackPieceKind.Box, i); if (x != null && x.name != null && x.name.Contains(ch)) return x; } return null; }
        var bM = BoxNamed("중"); var bL = BoxNamed("대");
        var bubble = b.Find(PackPieceKind.Roll, 0); var paper = b.Find(PackPieceKind.Roll, 1);
        var pc = b.Find(PackPieceKind.Parcel); var tape = b.Find(PackPieceKind.Tape);
        var bin = b.Find(PackPieceKind.BeadBin);

        // ① 감싼 물건의 띠 — 중 상자를 열고 뽁뽁이 두 장으로 물건을 감싼다(물건은 선반 자리 그대로)
        b.SimDrag(bM, PackBoard.SlotU, PackBoard.SlotV);
        yield return Probe("뽁뽁이(첫 겹)", () => b.SimDrag(bubble, pc.u, pc.v));
        yield return Wait(() => !b.Settling, 3f);
        yield return new WaitForSecondsRealtime(0.15f);
        yield return Probe("뽁뽁이(둘째 겹)", () => b.SimDrag(bubble, pc.u, pc.v));
        yield return Wait(() => !b.Settling, 3f);
        yield return new WaitForSecondsRealtime(0.4f);
        L($"물건 감은 겹={pc.wrap}");
        yield return Snap("wrap");
        b.DebugRestart(); yield return new WaitForSecondsRealtime(0.3f);
        pc = b.Find(PackPieceKind.Parcel);

        // ② 구긴 종이를 든 손 — 큰 상자를 열고, 롤을 집어(= 종이 구김) 상자 위로 옮긴 채 찍는다. 놓으면 툭.
        b.SimDrag(bL, PackBoard.SlotU, PackBoard.SlotV);
        yield return new WaitForSecondsRealtime(0.3f);
        paper = b.Find(PackPieceKind.Roll, 1);
        yield return Probe("종이 구김(롤 집기)", () => { b.Begin(paper.u, paper.v); if (simF != null) simF.SetValue(b, true); });
        b.Move(PackBoard.SlotU - 0.05f, PackBoard.SlotV + 0.12f);
        yield return new WaitForSecondsRealtime(0.35f);
        b.Move(PackBoard.SlotU - 0.05f, PackBoard.SlotV + 0.10f);
        yield return new WaitForSecondsRealtime(0.1f);
        L($"손 쥠={b.Gripping} 손 그려짐={b.HandDrawn}");
        yield return Snap("paper");
        yield return Probe("툭(종이 놓기)", () => b.SimRelease(PackBoard.SlotU - 0.05f, PackBoard.SlotV + 0.05f));
        yield return Wait(() => !b.Settling, 3f);
        b.DebugRestart(); yield return new WaitForSecondsRealtime(0.3f);

        // ③ 쏟아지는 알갱이 호 — 큰 상자 위에서 삽을 놓는 순간부터 0.12 s · 0.2 s 뒤에 찍는다
        b.SimDrag(bL, PackBoard.SlotU, PackBoard.SlotV);
        yield return new WaitForSecondsRealtime(0.3f);
        bin = b.Find(PackPieceKind.BeadBin);
        L("큰 상자 이름=" + bL.name);
        yield return Probe("삽 사락(통 집기)", () => { b.Begin(bin.u, bin.v); if (simF != null) simF.SetValue(b, true); });
        b.Move(PackBoard.SlotU, PackBoard.SlotV);
        yield return new WaitForSecondsRealtime(0.2f);
        yield return Probe("알갱이 사락(쏟기)", () => b.SimRelease(PackBoard.SlotU, PackBoard.SlotV));
        yield return new WaitForSecondsRealtime(0.05f);
        float tr0 = Time.unscaledTime;        // 쏟은 지 0.05 · 0.12 · 0.19 · 0.26 s 쯤에 연속으로 — 호가 가장 잘 보이는 한 장을 고른다
        for (int k = 0; k < 4; k++)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(dir + "/beads-" + k + ".png");
            L($"그림 beads-{k}.png (쏟은 뒤 {Time.unscaledTime - tr0 + 0.0f:F2}s)");
            yield return new WaitForSecondsRealtime(0.07f);
        }
        yield return Snap("beads");
        L($"알갱이 상자 안={bL.beads.Count} 흩어지는 중={b.scatter.Count}");
        yield return Wait(() => !b.Settling, 4f);

        // ④ 덜그럭 — 중 상자 + 달걀 + 흔들기. PackBench 풀이 실제로 재생 중인지
        b.DebugRestart(); yield return new WaitForSecondsRealtime(0.3f);
        pc = b.Find(PackPieceKind.Parcel); tape = b.Find(PackPieceKind.Tape);
        b.SimDrag(bL, PackBoard.SlotU, PackBoard.SlotV);
        b.SimDragThrow(pc, PackBoard.SlotU - 0.05f, PackBoard.SlotV + 0.04f, new Vector2(0.8f, 0.4f));
        yield return Wait(() => !b.Settling, 3f);
        float cu = PackBoard.SlotU, cv = PackBoard.SlotV + 0.02f;
        b.SimDragPath(tape, new Vector2(cu - 0.34f, cv), new Vector2(cu - 0.1f, cv), new Vector2(cu, cv), new Vector2(cu + 0.1f, cv), new Vector2(cu + 0.34f, cv));
        yield return new WaitForSecondsRealtime(0.4f);
        if (b.SimGrab(bL))
        {
            int beats0 = b.RattleBeats; int rp0 = RattlePlayedNow(); int framesPlaying = 0, frames = 0; float tt = 0f;
            while (tt < 2.6f)
            {
                b.SimShakeTick(Time.unscaledDeltaTime, Time.unscaledTime); tt += Time.unscaledDeltaTime; yield return null;
                frames++; if (BenchPlaying()) framesPlaying++;
            }
            L($"소리 덜그럭: 부딪힘 {b.RattleBeats - beats0}번 · PackBench.RattlePlayed +{RattlePlayedNow() - rp0} · 풀 isPlaying 이던 프레임 {framesPlaying}/{frames}");
            b.SimRelease(PackBoard.SlotU, PackBoard.SlotV);
        }
        else L("FAIL 상자 못 쥠");

        b.DebugRestart();
        L("끝");
        Status = "done"; Destroy(gameObject);
    }

    int RattlePlayedNow()
    {
        var f = benchType != null ? benchType.GetField("RattlePlayed", BindingFlags.Static | BindingFlags.Public) : null;
        return f != null ? (int)f.GetValue(null) : -1;
    }
}
