using System.Collections;
using UnityEngine;

/// <summary>
/// D-302 검증용: 첫 포장 튜토리얼을 처음(안 본 상태)으로 켜고 ① 1단계(상자 고르기) 화면 ② 마지막 단계(닫은 상자, 흔들지 않고 들고 나가기) 화면을 1× 로 찍는다.
/// 실행: exec 'return PackTutorialShots.Begin("D:/…/dir");' → PackTutorialShots.Status 가 "done" 이면 끝, Log 에 줄들.
/// </summary>
public class PackTutorialShots : MonoBehaviour
{
    public static string Status = "idle";
    public static string Log = "";
    static string dir;
    OnFootDirector d;

    public static string Begin(string outDir)
    {
        var dd = FindFirstObjectByType<OnFootDirector>();
        if (dd == null) return "no dir";
        dir = outDir; Status = "run"; Log = "";
        new GameObject("PackTutorialShots").AddComponent<PackTutorialShots>().d = dd;
        return "OK";
    }

    void L(string s) { Log += s + "\n"; Debug.Log("[PackTutorialShots] " + s); }

    IEnumerator Wait(System.Func<bool> ok, float sec)
    {
        float w = 0f;
        while (!ok() && w < sec) { w += Time.unscaledDeltaTime; yield return null; }
    }

    /// <summary>깜빡이 테두리가 켜진 박자(0.9 s 주기 중 앞 0.55 s)에 찍는다.</summary>
    IEnumerator Snap(string name)
    {
        yield return Wait(() => Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.25f, 2f);
        yield return new WaitForEndOfFrame();
        System.IO.Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(dir + "/" + name + ".png");
        yield return new WaitForSecondsRealtime(0.4f);
        L("그림 " + name + ".png");
    }

    IEnumerator Start()
    {
        yield return Wait(() => d.State != OnFootDirector.Mode.Starting, 10f);
        yield return new WaitForSecondsRealtime(1f);
        DayState.PackTutorialSeen = false;

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
        var b = spot.Board2D;
        yield return Wait(() => d.State == OnFootDirector.Mode.Pack && b.On, 3f);
        if (!(d.State == OnFootDirector.Mode.Pack && b.On)) { L("FAIL 2D 보드 안 켜짐"); Status = "done"; Destroy(gameObject); yield break; }
        yield return new WaitForSecondsRealtime(0.6f);                                  // 뷰는 보드가 켜진 뒤 첫 프레임에 만들어진다
        var tut = b.GetComponent<PackBoardView>().Tutorial;
        L($"보드 켜짐 · 튜토리얼 보임={tut.Showing} 단계={tut.Current}");
        yield return Snap("1-step1");

        // 단계를 따라: 중 상자 → 뽁뽁이 두 장 → 물건 → (틈 건너뜀) 테이프 → 닫힘 = 마지막 단계(흔들지 않음)
        PackPiece bM = null;
        for (int i = 0; i < 3; i++) { var x = b.Find(PackPieceKind.Box, i); if (x != null && x.name != null && x.name.Contains("중")) bM = x; }
        var bubble = b.Find(PackPieceKind.Roll, 0);
        var pc = b.Find(PackPieceKind.Parcel); var tape = b.Find(PackPieceKind.Tape);
        b.SimDrag(bM, PackBoard.SlotU, PackBoard.SlotV); yield return null; yield return null; L("상자 놓음 → " + tut.Current);
        b.SimDrag(bubble, pc.u, pc.v); yield return Wait(() => !b.Settling, 3f); L("뽁뽁이 한 장 → " + tut.Current);
        b.SimDrag(bubble, pc.u, pc.v); yield return Wait(() => !b.Settling, 3f); L("뽁뽁이 둘째 → " + tut.Current);
        b.SimDragThrow(pc, PackBoard.SlotU - 0.05f, PackBoard.SlotV + 0.04f, new Vector2(0.8f, 0.4f));
        yield return Wait(() => !b.Settling, 3f); yield return null; L("물건 넣음 → " + tut.Current);
        yield return Snap("2-gap");
        float cu = PackBoard.SlotU, cv = PackBoard.SlotV + 0.02f;
        b.SimDragPath(tape, new Vector2(cu - 0.34f, cv), new Vector2(cu - 0.1f, cv), new Vector2(cu, cv), new Vector2(cu + 0.1f, cv), new Vector2(cu + 0.34f, cv));
        yield return new WaitForSecondsRealtime(0.5f);
        L($"테이프 → {tut.Current} · 흔든 시간 {bM.shakeSecs:F1}s");
        yield return Snap("3-last");

        b.SimDrag(bM, PackBoard.SlotU, -PackBoard.HalfH - 0.12f);                      // 흔들지 않고 바로 판 아래로
        yield return new WaitForSecondsRealtime(1.0f);
        L($"들고 나감 → 상태={d.State} 들기={DayState.CarryingBox} 본 것으로 적힘={DayState.PackTutorialSeen} · {b.LastShakeHow}");
        Status = "done"; Destroy(gameObject);
    }
}
