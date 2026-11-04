using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-302 첫 포장 튜토리얼(D-301 다시): 1일차 첫 포장(DayState.PackTutorialSeen=false)에서만, 지금 할 한 단계만 판 위쪽 가운데 검은 반투명 띠 위에 크게 띄우고
/// 그 단계의 물건에 노란 테두리를 깜빡인다. 단계는 보드 상태에서 자동 판정(StepOf, 순수 함수)해 끝나면 다음 줄로 넘어간다.
/// 흔들기는 선택 — 닫은 상자를 흔들지 않고 바로 판 아래로 끌어 들고 나가도 된다(시작값은 PackBoard.TakeOut 이 표준 흔들기로 잰다).
/// 다 하고 상자를 들고 나가면 본 것으로 적는다. H 키로 언제든 다시 보기/접기, 접힌 동안은 구석에 작은 "H" 만.
/// 글은 코드 상수(안내 글 0 규칙의 예외 — 도현 원문 D-301·D-302). 보드 자료는 읽기만 한다(물리 값 변동 0). 그리기는 PackBoardView 가 맡겨 둔 root 아래.
/// </summary>
public sealed class PackTutorial
{
    public enum Step { PickBox, Cushion, PutParcel, Gap, Tape, TakeOut }

    /// <summary>지금 할 단계 큰 글 한 줄(Step 순서).</summary>
    public static readonly string[] Now =
    {
        "상자를 가운데로 끌어 놓자",
        "뽁뽁이로 물건을 감싸자",
        "물건을 상자 안에 넣자",
        "빈틈을 채워도 좋아 (선택)",
        "테이프로 상자를 닫자",
        "다 됐어. 판 아래로 끌어 나가자",
    };

    /// <summary>단계 아래 작은 보충 글(없으면 빈 글).</summary>
    public static readonly string[] Hint =
    {
        "",
        "롤을 끌어 물건 위에 놓으면 돼",
        "물건을 집어 열린 상자 위에 놓기",
        "종이·알갱이 · 건너뛰려면 테이프를 끌자",
        "테이프를 끌어 상자를 가로로 지나가기",
        "상자를 쥐고 흔들어 확인해도 돼 (선택)",
    };

    /// <summary>보드 상태 → 지금 단계. box = 가운데 자리의 상자(없으면 null), parcel = 물건 조각(상자 밖이면 감싼 겹을 본다).
    /// 완충재는 뽁뽁이로 감싸기든 종이·알갱이든 하나면 통과, 물건을 넣은 뒤 틈 채우기는 건너뛰어도 된다(테이프로 바로).</summary>
    public static Step StepOf(PackPiece box, PackPiece parcel)
    {
        if (box == null || !box.active) return Step.PickBox;
        if (box.closed) return Step.TakeOut;
        if (box.content == null)
        {
            bool cushioned = (parcel != null && parcel.wrap > 0) || box.bottom != PackFiller.None || box.wads.Count > 0 || box.beads.Count > 0;
            return cushioned ? Step.PutParcel : Step.Cushion;
        }
        bool gapFilled = box.top != PackFiller.None || box.wads.Count > 0 || box.beads.Count > 0;
        return gapFilled ? Step.Tape : Step.Gap;
    }

    readonly PackBoard board;
    readonly TextMeshPro now, hint, key;
    readonly Transform band;
    readonly System.Collections.Generic.List<Transform> marks = new System.Collections.Generic.List<Transform>();
    readonly TMP_FontAsset font;
    readonly Transform root;
    readonly Material bandMat, markMat;
    bool wasOn, show, doneNow;

    public bool Showing => show;
    public Step Current { get; private set; }

    // 판 좌표(m): 띠는 판 위 끝(HalfH 0.42) 바깥 탁자 쪽 — 상자 줄·가운데 빈 자리를 가리지 않는다. 글은 판 바로 위(TextY)에 둔다(원근이라 띄울수록 가장자리가 잘린다).
    // 화면 폭 = 판 폭 2.16 m ≈ 1× 640 px(≈296 px/m): 큰 글 0.76 ≈ 6.9 cm ≈ 20 px · 작은 글 0.55 ≈ 5 cm ≈ 15 px
    const float BandTopV = 0.54f, BandHalfW = 1.06f, MainH = 0.095f, HintH = 0.075f, TextY = 0.06f, BandY = 0.05f, MainSize = 0.76f, HintSize = 0.55f;

    public PackTutorial(PackBoard b, Transform r, TMP_FontAsset f)
    {
        board = b; font = f; root = r;
        bandMat = Unlit(new Color(0f, 0f, 0f, 0.66f));
        markMat = Unlit(new Color(1f, 0.88f, 0.25f, 1f));
        band = Quad("튜토리얼 띠", bandMat);
        band.GetComponent<Renderer>().sortingOrder = 1;
        now = MakeText("튜토리얼 지금 할 일", MainSize, Color.white, 2f * BandHalfW - 0.1f, MainH);
        hint = MakeText("튜토리얼 보충", HintSize, new Color(0.93f, 0.93f, 0.9f), 2f * BandHalfW - 0.1f, HintH);
        key = MakeText("튜토리얼 H", HintSize, new Color(0.95f, 0.95f, 0.95f), 0.5f, HintH);
        key.alignment = TextAlignmentOptions.Left;
        key.text = "H 안내";
        Put(key.transform, -BandHalfW + 0.27f, BandTopV - HintH * 0.5f, TextY);
    }

    static Material Unlit(Color c)
    {
        var m = new Material(Shader.Find("Sprites/Default")) { name = "pack tutorial(runtime)" };
        m.color = c;
        return m;
    }

    Transform Quad(string n, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Quad);
        g.name = n;
        UnityEngine.Object.Destroy(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().sharedMaterial = m;
        g.transform.SetParent(root, false);
        return g.transform;
    }

    TextMeshPro MakeText(string n, float size, Color c, float w, float h)
    {
        var g = new GameObject(n);
        g.transform.SetParent(root, false);
        var t = g.AddComponent<TextMeshPro>();
        if (font != null) t.font = font;
        t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.color = c; t.outlineWidth = 0.12f; t.outlineColor = new Color32(0, 0, 0, 255);
        t.rectTransform.sizeDelta = new Vector2(w, h);
        t.sortingOrder = 2;                                                  // 투명 정렬은 거리순이라 띠가 커지면 띠가 글 위에 덮인다 — 글은 늘 띠 위
        return t;
    }

    /// <summary>판 좌표 (u, v) 의 중심에, 판 위 높이 y 로 — 글·띠가 위에서 보아 똑바로 읽히게.</summary>
    void Put(Transform t, float u, float v, float y)
    {
        t.SetPositionAndRotation(board.Origin + board.Right * u + board.Up * v + Vector3.up * y, Quaternion.LookRotation(Vector3.down, board.Up));
    }

    void Mark(int i, float u, float v, float w, float d)
    {
        while (marks.Count <= i) marks.Add(Quad("튜토리얼 깜빡이 테두리", markMat));
        var t = marks[i];
        t.gameObject.SetActive(true);
        Put(t, u, v, 0.0055f);                                              // 판 바탕(윗면 0.004) 위, 조각 밑(0.012) — 조각 둘레로 테두리만 보인다
        t.localScale = new Vector3(w + 0.07f, d + 0.07f, 1f);
    }

    /// <summary>이 단계에서 눈에 띄게 할 물건들(깜빡이는 테두리). 든 것·상자 속 물건은 뺀다.</summary>
    int Highlight(Step s, PackPiece box)
    {
        int n = 0;
        foreach (var p in board.pieces)
        {
            if (!p.active || p.holder != null || board.Held == p) continue;
            bool pick = false;
            switch (s)
            {
                case Step.PickBox: pick = p.kind == PackPieceKind.Box; break;
                case Step.Cushion: pick = p.kind == PackPieceKind.Parcel || (p.kind == PackPieceKind.Roll && p.fillerKind == PackFiller.Bubble); break;
                case Step.PutParcel: pick = p.kind == PackPieceKind.Parcel; break;
                case Step.Gap: pick = p.kind == PackPieceKind.Tape || p.kind == PackPieceKind.BeadBin || (p.kind == PackPieceKind.Roll && p.fillerKind == PackFiller.Paper); break;
                case Step.Tape: pick = p.kind == PackPieceKind.Tape; break;
                case Step.TakeOut: break;                                    // 가운데 자리의 상자는 아래 빈 자리 테두리로 가리킨다(상자 둘레 테두리는 빈 자리 판 밑에 깔려 안 보인다)
            }
            if (pick) Mark(n++, p.u, p.v, p.w, p.d);
        }
        if (s == Step.PickBox || s == Step.PutParcel || s == Step.TakeOut) Mark(n++, PackBoard.SlotU, PackBoard.SlotV, PackBoard.SlotW, PackBoard.SlotD);   // 놓을 자리
        if (s == Step.TakeOut) Mark(n++, 0f, -PackBoard.HalfH + 0.005f, PackBoard.HalfW * 2f - 0.07f, 0.03f);                          // 판 아래 가장자리
        return n;
    }

    /// <summary>매 프레임(보드가 꺼져 있어도) — 켜질 때 보일지 정하고, 꺼질 때 상자를 들고 나갔으면 본 것으로 적는다.</summary>
    public void Tick(bool on)
    {
        if (on && !wasOn)
        {
            show = !DayState.PackTutorialSeen; doneNow = false;
            Debug.Log("[PackTutorial] 보드 열림 — " + (show ? "튜토리얼 뜸 (처음)" : "튜토리얼 안 뜸 (이미 봤음, H 로 다시 보기)"));
        }
        else if (!on && wasOn && (doneNow || board.PendingTake) && !DayState.PackTutorialSeen)
        {
            DayState.PackTutorialSeen = true;
            Debug.Log("[PackTutorial] 상자 들고 나감 → packTutorialSeen=true");
        }
        wasOn = on;
        if (!on) return;

        var kb = Keyboard.current;
        if (kb != null && kb.hKey.wasPressedThisFrame) { show = !show; Debug.Log("[PackTutorial] H → " + (show ? "펼침" : "접힘")); }

        var box = board.InSlot != null && board.InSlot.kind == PackPieceKind.Box ? board.InSlot : null;
        Current = StepOf(box, board.Find(PackPieceKind.Parcel));
        if (board.PendingTake) doneNow = true;                               // 상자를 판 밖으로 끌었다 — 이제 물러나는 중

        if (key.gameObject.activeSelf == show) key.gameObject.SetActive(!show);
        if (band.gameObject.activeSelf != show) { band.gameObject.SetActive(show); now.gameObject.SetActive(show); }
        int used = 0;
        if (!show && hint.gameObject.activeSelf) hint.gameObject.SetActive(false);
        if (show)
        {
            int si = (int)Current;
            string h = Hint[si];
            bool hasHint = h.Length > 0;
            if (now.text != Now[si]) now.text = Now[si];
            if (hint.text != h) hint.text = h;
            if (hint.gameObject.activeSelf != hasHint) hint.gameObject.SetActive(hasHint);
            float bandH = MainH + (hasHint ? HintH : 0f);
            Put(band, 0f, BandTopV - bandH * 0.5f, BandY);
            band.localScale = new Vector3(BandHalfW * 2f, bandH, 1f);
            Put(now.transform, 0f, BandTopV - MainH * 0.5f, TextY);
            if (hasHint) Put(hint.transform, 0f, BandTopV - MainH - HintH * 0.5f, TextY);
            if (Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.55f) used = Highlight(Current, box);   // 깜빡임: 켜진 동안만 그린다
        }
        for (int i = used; i < marks.Count; i++) if (marks[i].gameObject.activeSelf) marks[i].gameObject.SetActive(false);
    }
}
