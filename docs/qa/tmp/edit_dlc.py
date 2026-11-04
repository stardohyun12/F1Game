import sys
sys.path.insert(0, 'D:/Github/F1Game/docs/qa/tmp')
from rep import edit

P = 'D:/Github/F1Game/F1/Assets/Scripts/Debug/DayLoopCheck.cs'
pairs = []

pairs.append(('''    public static string PaperShot;
''', '''    public static string PaperShot;
    /// <summary>⑤ 알갱이 그림 파일 접두사 — 없으면 안 찍는다.</summary>
    public static string BeadShot;
    /// <summary>⑤ 마지막 측정(프레임 시간·표준 흔들기) — 보고용 한 줄.</summary>
    public static string BeadResult = "";
'''))

pairs.append(('''        b.DebugRestart(); beats0 = b.RattleBeats;
        pp = b.Find(PackPieceKind.Parcel);

        // ③ 중 상자: 한 판 끝까지''', '''        b.DebugRestart(); beats0 = b.RattleBeats;
        pp = b.Find(PackPieceKind.Parcel);

        // ⑤ D-296 스티로폼 알갱이: 통 → 삽 한 줌 → 상자 위에서 놓으면 쏟아져 쌓임 / 상자 밖이면 흩어져 사라짐 / 한 상자 최대 개수 / 표준 흔들기
        yield return Pack2DBeads(b, bM, bL);
        b.DebugRestart(); beats0 = b.RattleBeats;
        pp = b.Find(PackPieceKind.Parcel);

        // ③ 중 상자: 한 판 끝까지'''))

pairs.append(('''    /// <summary>
    /// 한 판: 상자 가운데 → 롤 한 장''', '''    /// <summary>
    /// ⑤ 알갱이. 대 상자에 18줌 → BeadMax 에서 멈추고 넘친 건 흩어짐(그때 프레임 시간 잼) / 삽을 상자 밖에서 놓으면 흩어져 사라짐 /
    /// 중 상자 + 달걀 + 2줌 → 개수 · 표준 흔들기가 알갱이 0 일 때의 50% 이하 · 그림.
    /// </summary>
    IEnumerator Pack2DBeads(PackBoard b, PackPiece bM, PackPiece bL)
    {
        var bin = b.Find(PackPieceKind.BeadBin);
        if (bin == null) { Step("⑤ 알갱이 FAIL 통 조각이 없음"); yield break; }
        var view = b.GetComponent<PackBoardView>();
        int scoop = PackBoard.BeadScoop, max = PackBoard.BeadMax;

        // 바깥에서 놓기
        b.SimDrag(bL, PackBoard.SlotU, PackBoard.SlotV);
        b.SimDrag(bin, -0.8f, -0.3f);
        int sc0 = b.scatter.Count;
        yield return Wait(() => b.scatter.Count == 0 && !b.Settling, 4f);
        bool outGone = sc0 == scoop && b.scatter.Count == 0 && bL.beads.Count == 0;

        // 대 상자 가득
        int pours = 0;
        for (int k = 0; k < max / scoop + 2; k++) { if (b.SimDrag(bin, PackBoard.SlotU, PackBoard.SlotV)) pours++; yield return Wait(() => !b.Settling, 4f); }
        int cap = bL.beads.Count; int spilled = b.LastSpilled;
        bool capOk = cap == max && spilled > 0;
        yield return new WaitForSecondsRealtime(0.5f);
        float dtSum = 0f, drawSum = 0f; int fr = 90;
        for (int i = 0; i < fr; i++) { yield return null; dtSum += Time.unscaledDeltaTime; if (view != null) drawSum += view.DrawMs; }
        float frameMax = dtSum / fr * 1000f, drawMax = drawSum / fr;
        b.DebugRestart();
        yield return null;
        float dt0 = 0f; float draw0 = 0f;
        for (int i = 0; i < fr; i++) { yield return null; dt0 += Time.unscaledDeltaTime; if (view != null) draw0 += view.DrawMs; }
        float frame0 = dt0 / fr * 1000f, drawN0 = draw0 / fr;

        // 중 상자 + 달걀 + 2줌
        var pc = b.Find(PackPieceKind.Parcel);
        b.SimDrag(bM, PackBoard.SlotU, PackBoard.SlotV);
        b.SimDragThrow(pc, PackBoard.SlotU - 0.05f, PackBoard.SlotV + 0.04f, new Vector2(0.8f, 0.4f));
        yield return Wait(() => !b.Settling, 3f);
        var phys0 = bM.phys != null ? bM.phys.Copy() : null;
        float s0 = phys0 != null ? phys0.MeasureStandard() : 0f;
        for (int k = 0; k < 2; k++) { b.SimDrag(bin, PackBoard.SlotU, PackBoard.SlotV); yield return Wait(() => !b.Settling, 4f); }
        yield return Snap(BeadShot, "-beads");
        float s2 = bM.phys != null ? bM.phys.MeasureStandard() : 0f; float fill = bM.phys != null ? bM.phys.fill : 0f;
        bool half = s0 > 0f && s2 <= s0 * 0.5f;
        bool good = outGone && capOk && bM.beads.Count == scoop * 2 && half && frameMax <= 8f;
        BeadResult = $"frameMax={frameMax:F2}ms frame0={frame0:F2}ms drawMax={drawMax:F3}ms draw0={drawN0:F3}ms";
        Step($"⑤ 알갱이 {(good ? "PASS" : "FAIL")} 상자 밖 한 줌 흩어져 사라짐={outGone} | 대 상자 {pours}번 부어 {cap}/{max}개(넘침 {spilled}) | 중 상자 2줌 {bM.beads.Count}개 틈 {fill * 100f:F0}% 메움 · 표준 흔들기 알갱이 0 {s0 * 100f:F2} → 2줌 {s2 * 100f:F2} cm | 프레임 최대 알갱이 {frameMax:F2} ms(알갱이 0 {frame0:F2} ms) · 알갱이 메시 {drawMax:F3} ms | {b.LastAct}");
    }

    /// <summary>
    /// 한 판: 상자 가운데 → 롤 한 장'''))

edit(P, pairs)
