using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-287 분리 전·후 비교용: 하루 한 바퀴(사장 → 쪽지 → 작업대 → 실음 → 도착 → 건넴 → 노을 → 침대 → 일기 → 아침)를
/// 순간이동 + Sim 입력으로 돌리고 감독 로그를 JSON 으로 남긴다. 걷기·운전은 하지 않는다(시각이 흔들리지 않게).
/// 포장 손동작은 따로 크다 — 작업대 들어가기·물러나기만 Sim 으로, 상자 들기는 감독의 TakePacked 를 바로 부른다.
/// 실행: exec 'return DayLoopCheck.Begin("D:/.../before.json");' → DayLoopCheck.Status 가 "done …" 이면 끝.
/// </summary>
public class DayLoopCheck : MonoBehaviour
{
    public static string Status = "idle";
    /// <summary>D-293 걷기 검사 결과(집은 뒤 걸어도 물건-카메라 거리가 안 변하나). 한 줄 JSON-ish.</summary>
    public static string WalkResult = "";
    public const float WalkHoldTol = 0.05f;
    static string outPath;
    bool walkOnly;
    OnFootDirector d;
    float t0;
    readonly List<string> steps = new List<string>();

    /// <summary>D-294: 값이 있으면 2D 보드가 켜진(상자가 가운데에 열린) 순간의 게임 화면을 이 경로에 저장한다.</summary>
    public static string Pack2DShot;

    public static string Begin(string path)
    {
        var dir = FindFirstObjectByType<OnFootDirector>();
        if (dir == null) return "no dir";
        outPath = path; Status = "run";
        var go = new GameObject("DayLoopCheck");
        go.AddComponent<DayLoopCheck>().d = dir;
        return "OK";
    }

    /// <summary>D-293: 선반 앞에서 물건만 집고 걷기 검사 한 번(포장 손동작 없이). exec 'return DayLoopCheck.BeginWalk();' → WalkResult.</summary>
    public static string BeginWalk()
    {
        var dir = FindFirstObjectByType<OnFootDirector>();
        if (dir == null) return "no dir";
        outPath = null; Status = "run"; WalkResult = "";
        var go = new GameObject("DayLoopCheck");
        var c = go.AddComponent<DayLoopCheck>(); c.d = dir; c.walkOnly = true;
        return "OK";
    }

    float T => Time.unscaledTime;
    void Step(string s) { steps.Add($"[{T - t0:F1}] {s} | 상태={d.State} 단계={d.DeliveryStep} 안내='{d.Prompt}' 지나온={d.StepTrail}"); }

    IEnumerator Wait(System.Func<bool> ok, float sec)
    {
        float w = 0f;
        while (!ok() && w < sec) { w += Time.unscaledDeltaTime; yield return null; }
    }

    IEnumerator Look(Vector3 at)
    {
        d.Player.Cam.LookAt(at);
        yield return null; yield return null; yield return null;
    }

    void Call(string method) =>
        typeof(OnFootDirector).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(d, null);

    IEnumerator Press()
    {
        d.SimInteract(); yield return null; yield return null;
    }

    IEnumerator Start()
    {
        yield return Wait(() => d.State != OnFootDirector.Mode.Starting, 10f);
        yield return new WaitForSecondsRealtime(1f);
        t0 = T;
        Step("시작");

        if (walkOnly)
        {
            var t2 = d.PackSpot != null ? d.PackSpot.V3Table : null;
            if (t2 == null) { WalkResult = "FAIL no V3Table"; Status = "done"; Destroy(gameObject); yield break; }
            yield return PickFromShelf(t2);
            yield return WalkWhileHolding(t2);
            Status = "done"; Destroy(gameObject); yield break;
        }

        // 사장
        DebugWindow.GoPlace(2); yield return null;
        yield return Look(d.Boss.position + Vector3.up * 1.5f);
        Step("사장 앞"); yield return Press();
        yield return Wait(() => d.InTalk, 2f);
        while (d.InTalk) { yield return new WaitForSecondsRealtime(0.4f); d.SimInteract(); }
        yield return new WaitForSecondsRealtime(1.5f);
        Step("사장 끝 Talked=" + d.Talked);

        if (d.PackSpot != null && d.PackSpot.V3Table != null)
        {
            // D-294: 2D 보드가 켜져 있으면 그 길(롤 뜯기 → 넣기 → 덮기 → 테이프 → 흔들기 → 판 밖으로 = 상자 들기)로 끝까지 — 3D 손 흐름은 2D 가 꺼졌을 때만.
            bool two = d.PackSpot.Use2D;
            if (two) yield return Pack2D(d.PackSpot.V3Table);
            else yield return PackV3(d.PackSpot.V3Table);
        }
        else yield return OldPack();

        // 싣기
        DebugWindow.GoPlace(3); yield return null;
        yield return Look(d.Car.transform.position);
        Step("카트 옆"); yield return Press();
        yield return Wait(() => d.State == OnFootDirector.Mode.InCart, 2f);
        yield return new WaitForSecondsRealtime(1f);
        Step("카트 탐");

        // 도착 → 건넴
        d.DebugArriveForHandover(1f, new Vector3(97.5f, 4.4f, -281.0f), 270f, new Vector3(94.0f, 4.0f, -283.65f), 180f);
        yield return new WaitForSecondsRealtime(1f);
        Step("도착 Carrying=" + d.CarryingBox);
        yield return Look(d.Handover.DoorCenter);
        yield return Wait(() => d.Prompt == "E 두드리기", 1f);
        Step("문 앞"); yield return Press();
        yield return Wait(() => d.InTalk, 4f);
        while (d.InTalk) { yield return new WaitForSecondsRealtime(0.6f); d.SimInteract(); }
        yield return new WaitForSecondsRealtime(2f);
        Step("건넴 HandedOver=" + d.HandedOver);

        // 침대 → 일기 → 아침
        d.DebugTeleport(new Vector3(187.3f, 28.5f, -62.6f), 270f, null, 0f); yield return null;
        yield return Look(d.Bed.Anchor);
        Step("침대 앞"); yield return Press();
        yield return Wait(() => d.State == OnFootDirector.Mode.Diary && d.DiaryCanClose, 4f);
        yield return new WaitForSecondsRealtime(1f);
        Step("일기");
        d.SimDiaryClose();
        yield return Wait(() => d.State == OnFootDirector.Mode.OnFoot, 6f);
        yield return new WaitForSecondsRealtime(1f);
        Step("아침");

        var sb = new System.Text.StringBuilder("{\"steps\":[\n");
        for (int i = 0; i < steps.Count; i++) sb.Append("\"").Append(steps[i].Replace("\"", "'")).Append(i < steps.Count - 1 ? "\",\n" : "\"\n");
        sb.Append("],\n\"log\":[\n");
        for (int i = 0; i < d.Log.Count; i++) sb.Append("\"").Append(d.Log[i].Replace("\"", "'")).Append(i < d.Log.Count - 1 ? "\",\n" : "\"\n");
        sb.Append("]}\n");
        try
        {
            string dir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(outPath));
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);   // 상대 경로는 F1/ 기준 — 폴더가 없어도 상태가 "run" 에 걸리지 않게
            System.IO.File.WriteAllText(outPath, sb.ToString());
        }
        catch (System.Exception e) { Debug.LogWarning("[DayLoopCheck] 결과 저장 실패: " + e.Message); }
        Status = "done";
        Destroy(gameObject);
    }

    /// <summary>D-290 전: 칠판 쪽지 → 옛 작업대(PackBench 봇) → 상자 들기.</summary>
    IEnumerator OldPack()
    {
        // 쪽지
        DebugWindow.GoPlace(12); yield return null;
        yield return Look(d.OrderBoard.Center);
        yield return Wait(() => d.Prompt == "E 쪽지 집기", 1f);
        Step("칠판 앞"); yield return Press();
        yield return new WaitForSecondsRealtime(2f);
        Step("쪽지 OrderPicked=" + d.OrderPicked);

        // 작업대
        yield return new WaitForSecondsRealtime(1.5f);   // 쪽지 뒤 작업대 쪽 시선(0.5+0.8 s)이 끝나길
        d.DebugTeleport(new Vector3(273.2f, 31.1f, -29.5f), 90f, null, 0f); yield return null;
        yield return Look(d.PackSpot.Top);
        yield return Wait(() => d.Prompt.StartsWith("E "), 1f);
        Step("작업대 앞");
        Call("EnterPack");   // 시선 레이가 작업대 판을 못 잡을 때가 있다 — E 대신 감독의 작업대 들어가기를 바로
        yield return Wait(() => d.State == OnFootDirector.Mode.Pack && !d.PackMoving, 4f);
        Step("작업대 안");
        PitCrew.PackBench.PackBench.BotRun("full");
        yield return Wait(() => PitCrew.PackBench.PackBench.BotStatus.Contains(" done "), 40f);
        yield return new WaitForSecondsRealtime(1f);
        Step("포장 봇 끝 Ready=" + d.PackSpot.Ready);
        d.SimPackLeave();
        yield return Wait(() => d.State == OnFootDirector.Mode.OnFoot && !d.PackMoving, 6f);
        Step("작업대 물러남");
        Call("TakePacked");
        yield return new WaitForSecondsRealtime(2f);
        Step("상자 들기 Carrying=" + d.CarryingBox);
    }

    /// <summary>
    /// D-290 포장 v3: 받는 곳 선반에서 물건 E(시선) → 포장대 앞 → 사람과 같은 Use 순서(작업면 · 상자 중 · 뽁뽁이 바닥 · 물건 · 뽁뽁이 덮기 · 닫기 · 테이프 · 들어 흔들기 · 내려놓기 · 상자 들기).
    /// 첫 E 는 시선 레이로(안 맞으면 SimUse), 포장대 위 물건들은 SimUse 로 바로 — 시선 각도 흔들림 없이.
    /// </summary>
    IEnumerator PackV3(PackTable2 t, bool fromRest = false)
    {
        if (!fromRest) { yield return PickFromShelf(t); yield return WalkWhileHolding(t); }
        var parcel = t.receive.today[0];
        d.DebugTeleport(new Vector3(273.2f, 31.1f, -29.5f), 90f, null, 0f); yield return null;
        yield return Look(t.workSpot.position);
        Step("포장대 앞");
        if (fromRest) { t.SimUse(parcel); yield return null; }            // 2D 확인 뒤: 작업면에 있는 물건을 다시 손에
        t.SimUse(t); yield return null;                                   // 물건을 작업면에
        t.SimUse(t.boxes[1]); yield return null; t.SimUse(t); yield return null;   // 상자 중
        t.SimUse(t.rolls[0]); yield return null; t.SimUse(t); yield return null;   // 뽁뽁이 바닥
        t.SimUse(parcel); yield return null; t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(2f);   // 물건 넣기(떨어짐)
        t.SimUse(t.rolls[0]); yield return null; t.SimUse(t); yield return null;   // 뽁뽁이 덮기
        Step($"넣음 InBox={(t.InBox != null)} 바닥={t.Bottom} 덮기={t.Top} fit={t.Fit}");
        t.SimUse(t.WorkBox); yield return null;                            // 닫기
        t.SimTape(); yield return null; yield return null;
        t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(0.3f);   // 들기
        t.SimShake(2f); yield return new WaitForSecondsRealtime(2.5f);
        t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(0.5f);   // 내려놓기
        Step($"포장 v3 끝 Ready={d.PackSpot.Ready} 시작={t.StartIntegrity01 * 100f:F0}% rattle={t.Result.rattle:F2} | {t.LastAct}");
        t.SimUse(t.WorkBox); yield return new WaitForSecondsRealtime(2f);   // 상자 들기(BoxTaken)
        Step("상자 들기 Carrying=" + d.CarryingBox);
    }

    /// <summary>
    /// D-294 포장 2D: 물건 집고 걷기(순간이동 없음) → 포장대 [E](시선, 안 잡히면 SimUse) → 2D 보드 켜짐·물건 조각 →
    /// ① 소 상자: 물건이 안 들어가고 튕김 + R 돌림 ② 대 상자: 들어가되 헐거움 → 닫고 테이프 → 흔들어 덜그럭(소리 수·rattle) ③ 중 상자: 뽁뽁이 두 장으로 물건 감싸기(2겹) → 물건 → 테이프 → 흔들기 → 판 밖(아래)으로 = 상자 들기.
    /// 전부 사람과 같은 길(PackBoard 의 Begin/Move/End)로. ②는 시험 한 판이라 끝나면 보드를 처음으로 되돌린다(DebugRestart).
    /// </summary>
    IEnumerator Pack2D(PackTable2 t)
    {
        var spot = d.PackSpot; var parcel = t.receive.today[0];
        yield return PickFromShelf(t);
        yield return WalkWhileHolding(t);
        d.DebugTeleport(new Vector3(273.2f, 31.1f, -29.5f), 90f, null, 0f); yield return null;
        yield return Look(t.workSpot.position);
        Step("포장대 앞(2D)"); yield return Press();
        if (d.State != OnFootDirector.Mode.Pack) { Step("시선 E 안 잡힘 → SimUse"); t.SimUse(t); yield return null; }
        var b = spot.Board2D;
        yield return Wait(() => d.State == OnFootDirector.Mode.Pack && b.On, 3f);
        bool on = d.State == OnFootDirector.Mode.Pack && b.On;
        var pp = b.Find(PackPieceKind.Parcel);
        bool parcelOn = pp != null && pp.active && pp.parcel == parcel;
        Step($"2D 보드 {(on && parcelOn ? "PASS" : "FAIL")} 켜짐={on} 물건조각={parcelOn} 조각 상자{b.Count(PackPieceKind.Box)}·롤{b.Count(PackPieceKind.Roll)}·테이프{b.Count(PackPieceKind.Tape)} | {b.LastAct}");
        yield return new WaitForSecondsRealtime(0.3f);

        var bS = b.Find(PackPieceKind.Box, 0); var bM = b.Find(PackPieceKind.Box, 1); var bL = b.Find(PackPieceKind.Box, 2);
        var bubble = b.Find(PackPieceKind.Roll, 0);

        // ① 소 상자: 안 들어가고 튕김. 들고 R 로 돌려도 마찬가지(물건이 정사각이라 그대로)
        b.SimDrag(bS, PackBoard.SlotU, PackBoard.SlotV);
        bool sOpen = bS.open && b.InSlot == bS;
        b.SimGrab(pp); b.SimRotate(); bool rotated = pp.rot90; b.SimRelease(PackBoard.SlotU, PackBoard.SlotV);
        bool perched = pp.perch > 0f;                                         // D-295: 바로 사라지지 않고 벽에 걸려 있다가 튕겨 나온다
        yield return Wait(() => pp.perch <= 0f && !b.Settling, 3f);
        bool bounced = perched && bS.content == null && pp.holder == null && Mathf.Approximately(pp.u, pp.homeU) && !pp.rot90;
        Step($"① 소 상자 {(sOpen && rotated && bounced ? "PASS" : "FAIL")} 열림={sOpen} R돌림={rotated} 벽에 걸림={perched} 튕김={bounced} | {b.LastAct}");
        b.SimDrag(bS, bS.homeU, bS.homeV);                                    // 비운 상자는 도로 집어 치운다

        // ② 대 상자: 들어가되 헐거움 → 닫고 흔들어 덜그럭
        int beats0 = b.RattleBeats;
        yield return PackRun(b, bL, bubble, false, 0, Pack2DShot == null ? null : Pack2DShot + "-L");
        float rattleL = bL.rattle; int beatsL = b.RattleBeats - beats0; var fitL = bL.fit;
        Step($"② 대 상자 {(fitL == PackFit.Loose && bL.closed && bL.Taped && rattleL > 0.05f && beatsL > 3 ? "PASS" : "FAIL")} fit={fitL} 닫힘={bL.closed} 테이프={bL.strips.Count} 흔들림 {rattleL * 100f:F1} cm(흔든 {bL.shakeSecs:F1}s) 덜그럭 소리 {beatsL}번 | 시작값(참고) {PackRules.StartFromShake("깨짐", bL.bottom, bL.top, rattleL) * 100f:F0}%");
        b.DebugRestart(); beats0 = b.RattleBeats;
        pp = b.Find(PackPieceKind.Parcel);

        // ③ 중 상자: 한 판 끝까지
        int left0 = bubble.left;
        yield return PackRun(b, bM, bubble, true, 2, Pack2DShot == null ? null : Pack2DShot + "-M");
        float rattleM = bM.rattle; int beatsM = b.RattleBeats - beats0; float shakeM = bM.shakeSecs;
        float start = b.LastStart; int wrapM = pp.wrap;
        yield return Wait(() => d.State == OnFootDirector.Mode.OnFoot && !d.PackMoving && d.CarryingBox, 5f);
        bool carry = d.CarryingBox;
        float want = PackRules.StartFromShake("깨짐", PackFiller.None, PackFiller.None, b.LastShake, PackRules.WrapK(2));   // D-296: 감싼 2겹이면 흔들림에 겹 계수를 곱한 값
        bool good = wrapM == 2 && bM.fit == PackFit.Snug && bM.bottom == PackFiller.None && bM.top == PackFiller.None && bM.strips.Count == 1 && bM.phys != null && Mathf.Approximately(bM.phys.wrapK, PackRules.WrapK(2)) && b.LastShake > 0f && Mathf.Abs(start - want) < 0.005f && bubble.left == left0 - 2 && carry && !b.On;
        Step($"③ 중 상자 {(good ? "PASS" : "FAIL")} 감싸기 {wrapM}번 fit={bM.fit} 바닥={bM.bottom} 덮기={bM.top} 테이프 {bM.strips.Count}줄 뽁뽁이 롤 {left0}→{bubble.left} 흔들림 {b.LastShake * 100f:F2} cm(흔든 {shakeM:F1}s · 덜그럭 {beatsM}번 · {b.LastShakeHow}) 화물 시작 {start * 100f:F1}%(기대 {want * 100f:F1}) 들고 나감={carry} | {b.LastAct}");
        Step($"포장 결과 → 포장대 Ready={t.Ready} StartIntegrity01={t.StartIntegrity01 * 100f:F1}% 손에 든 상자(CarryingBox)={d.CarryingBox}");
    }

    /// <summary>
    /// 한 판: 상자 가운데 → 롤 한 장(바닥, 떨어져 자리 잡음) → 물건을 던져 넣기(손 속도 0.8×0.4 m/s, 그림 1) → 롤 한 장(덮기) → 테이프 가로지름 → 쥐고 2.6 s 흔들기(1.5 s 넘게 가속해야 직접 잰 값을 쓴다, 그림 2) → 놓기(takeOut 이면 판 밖 아래로).
    /// shot 은 그림 파일 접두사("….png" 앞) — 없으면 안 찍는다.
    /// </summary>
    IEnumerator PackRun(PackBoard b, PackPiece box, PackPiece roll, bool takeOut, int wraps, string shot)
    {
        var pc = b.Find(PackPieceKind.Parcel); var tape = b.Find(PackPieceKind.Tape);
        b.SimDrag(box, PackBoard.SlotU, PackBoard.SlotV);
        for (int k = 0; k < wraps; k++)                                       // D-296: 뜯은 뽁뽁이 한 장을 물건 위로 끌어다 놓는다 = 감싼다
        {
            b.SimDrag(roll, pc.u, pc.v);
            yield return Wait(() => !b.Settling, 3f);
            if (k == 0) yield return Snap(shot, "-wrap1");
        }
        yield return Snap(shot, "-wrapped");
        b.SimDragThrow(pc, PackBoard.SlotU - 0.05f, PackBoard.SlotV + 0.04f, new Vector2(0.8f, 0.4f));   // 감싼 물건 던져 넣기
        yield return null; yield return null;
        yield return Snap(shot, "-drop");
        yield return Wait(() => !b.Settling, 3f);
        float cu = PackBoard.SlotU, cv = PackBoard.SlotV + 0.02f;
        b.SimDragPath(tape, new Vector2(cu - 0.34f, cv), new Vector2(cu - 0.1f, cv), new Vector2(cu, cv), new Vector2(cu + 0.1f, cv), new Vector2(cu + 0.34f, cv));
        yield return new WaitForSecondsRealtime(0.4f);
        if (!b.SimGrab(box)) yield break;
        float tt = 0f; bool shot2 = false;
        while (tt < 2.6f)
        {
            b.SimShakeTick(Time.unscaledDeltaTime, Time.unscaledTime); tt += Time.unscaledDeltaTime; yield return null;
            if (!shot2 && tt > 0.9f) { shot2 = true; yield return Snap(shot, "-shake"); }
        }
        b.SimRelease(PackBoard.SlotU, takeOut ? -PackBoard.HalfH - 0.12f : PackBoard.SlotV);
        yield return null;
    }

    IEnumerator Snap(string shot, string tag)
    {
        if (string.IsNullOrEmpty(shot)) yield break;
        yield return new WaitForEndOfFrame();
        string f = shot + tag + ".png";
        ScreenCapture.CaptureScreenshot(f);
        yield return new WaitForSecondsRealtime(0.4f);
        Step("2D 보드 그림 " + f);
    }

    /// <summary>받는 곳 선반 앞에서 시선 E 로 물건을 집는다(안 잡히면 SimUse).</summary>
    IEnumerator PickFromShelf(PackTable2 t)
    {
        var parcel = t.receive.today[0];
        var pp = parcel.transform.position;
        d.DebugTeleport(new Vector3(pp.x, 31.1f, pp.z - 1.1f), 0f, null, 0f); yield return null;
        yield return Look(pp);
        Step("받는 곳 선반 앞 시선=" + d.GazeTarget);
        yield return Press();
        if (!t.Picked) { Step("시선 E 안 잡힘 → SimUse"); t.SimUse(parcel); yield return null; }
        yield return new WaitForSecondsRealtime(0.5f);
        Step("물건 집음 OrderPicked=" + d.OrderPicked + " 손=" + t.Holding);
    }

    /// <summary>
    /// D-293: 집은 뒤 순간이동 없이 Sim 입력 W 2초로 걷는다. 든 물건-카메라 거리가 걷기 전과 0.05 m 이상 다르면 실패(WalkResult "FAIL …").
    /// 들고 있지 않거나 실제로 1 m 도 못 걸었어도 실패 — 검사가 빈 채로 통과하지 못하게.
    /// </summary>
    IEnumerator WalkWhileHolding(PackTable2 t)
    {
        var held = PackTable2.HeldNow;
        var cam = Camera.main;
        if (held == null || cam == null || t.Holding != PackTable2.Hold.Parcel) { WalkResult = $"FAIL 손에 안 듦 held={(held != null)} cam={(cam != null)} Holding={t.Holding}"; Step(WalkResult); yield break; }
        var pl = d.Player;
        d.Player.Cam.AddLook(180f - d.Player.Cam.Yaw, 0f);   // 선반을 등지고(뒤는 열린 보관소 바닥)
        yield return null; yield return null;
        float d0 = Vector3.Distance(held.position, cam.transform.position);
        Vector3 p0 = pl.transform.position, rel0 = cam.transform.InverseTransformPoint(held.position);
        bool oldSim = pl.simActive, oldCamSim = pl.Cam.simActive;
        pl.simActive = true; pl.Cam.simActive = true; pl.simMove = new Vector2(0f, 1f);
        float maxDev = 0f; float w = 0f;
        while (w < 2f)
        {
            w += Time.unscaledDeltaTime; yield return new WaitForEndOfFrame();
            float dev = Vector3.Distance(cam.transform.InverseTransformPoint(held.position), rel0);   // 카메라 기준 자리 변화(거리+방향)
            if (dev > maxDev) maxDev = dev;
        }
        pl.simMove = Vector2.zero; pl.simActive = oldSim; pl.Cam.simActive = oldCamSim;
        float d1 = Vector3.Distance(held.position, cam.transform.position);
        float walked = Vector3.Distance(pl.transform.position, p0);
        bool ok = held == PackTable2.HeldNow && Mathf.Abs(d1 - d0) < WalkHoldTol && maxDev < WalkHoldTol && walked > 1f;
        WalkResult = $"{(ok ? "PASS" : "FAIL")} 걸음={walked:F2}m 거리 {d0:F3}→{d1:F3} (Δ{Mathf.Abs(d1 - d0):F3}) 카메라기준 최대이탈={maxDev:F3} 부모={(held.parent != null ? held.parent.name : "null")} 부모의부모={(held.parent != null && held.parent.parent != null ? held.parent.parent.name : "null")} 카메라={cam.name}";
        Step("걷기 검사 " + WalkResult);
    }
}
