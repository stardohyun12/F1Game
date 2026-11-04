using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// D-301 첫 포장 튜토리얼: 1일차 첫 포장(DayState.PackTutorialSeen=false)에서만 보드 왼쪽 위에 조작법 전부를 작은 흰 글씨로 띄우고,
/// 지금 할 단계 한 줄은 밝게 — 단계는 보드 상태에서 자동 판정(StepOf, 순수 함수). 다 하고 상자를 들고 나가면 본 것으로 적는다.
/// H 키로 언제든 다시 보기/접기, 접힌 동안은 구석에 작은 "H" 만. 글은 코드 상수(안내 글 0 규칙의 예외 — 도현 원문 D-301).
/// 보드 자료는 읽기만 한다(물리 값 변동 0). 그리기는 PackBoardView 가 맡겨 둔 root 아래 월드 글자.
/// </summary>
public sealed class PackTutorial
{
    public enum Step { PickBox, PutParcel, Fill, Tape, Shake, TakeOut }

    /// <summary>조작법 전부(항상 같이 보임) — 도현 원문 순서.</summary>
    public static readonly string[] Controls =
    {
        "상자 끌어 가운데 → 열림",
        "뽁뽁이 롤 → 물건 위에 놓으면 감싸짐 (2겹까지)",
        "종이 롤 → 집으면 구겨짐, 틈에 놓기",
        "알갱이 통 → 클릭 = 한 줌, 상자 위에서 놓기",
        "R 회전",
        "테이프 끌어 상자 가로지르기 → 닫힘",
        "닫힌 상자 쥐고 흔들기 → 덜그럭 확인",
        "상자를 판 아래로 끌면 들고 나감",
    };

    /// <summary>지금 할 단계 한 줄(Step 순서).</summary>
    public static readonly string[] Now =
    {
        "상자를 끌어 가운데 빈 자리에 놓기",
        "물건을 집어 열린 상자 안에 놓기",
        "틈 채우기: 뽁뽁이 감싸기 · 종이 · 알갱이 (안 채워도 됨)",
        "테이프를 끌어 상자 가로로 지나가기",
        "닫힌 상자를 쥐고 흔들기 — 덜그럭 소리 듣기",
        "상자를 판 아래로 끌어 들고 나가기",
    };

    /// <summary>보드 상태 → 지금 단계. box = 가운데 자리의 상자(없으면 null), shook = 이 상자를 충분히 흔들어 봤나.</summary>
    public static Step StepOf(PackPiece box, bool shook)
    {
        if (box == null || !box.active) return Step.PickBox;
        if (box.closed) return shook ? Step.TakeOut : Step.Shake;
        if (box.content == null) return Step.PutParcel;
        bool filled = box.content.wrap > 0 || box.bottom != PackFiller.None || box.top != PackFiller.None || box.wads.Count > 0 || box.beads.Count > 0;
        return filled ? Step.Tape : Step.Fill;
    }

    readonly PackBoard board;
    readonly TextMeshPro list, now, key;
    readonly TMP_FontAsset font;
    bool wasOn, show, shook, doneNow;
    PackPiece shookBox;

    public bool Showing => show;
    public Step Current { get; private set; }

    // 판 좌표(m): 왼쪽 위 모서리 — 상자 줄(u ≥ 0.28)과 가운데 빈 자리(v ≤ 0.21) 위쪽 띠
    // 카메라가 원근이라 글을 판에서 띄울수록 가장자리 글이 화면 밖으로 밀린다(0.22 m 에서 왼쪽 ~0.2 m 잘림) → 판 바로 위(TextY)에 둔다
    const float LeftU = -0.97f, NowV = 0.535f, ListV = 0.488f, TextY = 0.06f, FontSize = 0.38f;   // 화면 위 끝 ≈ v 0.546

    public PackTutorial(PackBoard b, Transform root, TMP_FontAsset f)
    {
        board = b; font = f;
        list = MakeText("튜토리얼 조작법", root, TextAlignmentOptions.TopLeft, new Color(0.8f, 0.8f, 0.8f));
        now =MakeText("튜토리얼 지금 할 일", root, TextAlignmentOptions.TopLeft, Color.white);
        key = MakeText("튜토리얼 H", root, TextAlignmentOptions.TopLeft, new Color(0.86f, 0.86f, 0.86f));
        key.text = "H";
        list.text = string.Join("\n", System.Array.ConvertAll(Controls, Fit));
        Put(list, LeftU, ListV); Put(now, LeftU, NowV); Put(key, LeftU, NowV);
    }

    TextMeshPro MakeText(string n, Transform root, TextAlignmentOptions al, Color c)
    {
        var g = new GameObject(n);
        g.transform.SetParent(root, false);
        var t = g.AddComponent<TextMeshPro>();
        if (font != null) t.font = font;
        t.fontSize = FontSize; t.alignment = al; t.textWrappingMode = TextWrappingModes.NoWrap;
        t.color = c; t.outlineWidth = 0.18f; t.outlineColor = new Color32(0, 0, 0, 255);
        t.rectTransform.sizeDelta = new Vector2(BoxW, BoxH);              // 가운데 기준 칸 — Put 이 왼쪽 위 모서리에 맞춘다
        return t;
    }

    const float BoxW = 1.3f, BoxH = 0.45f;

    /// <summary>글 칸의 왼쪽 위 모서리를 판 좌표 (u, v) 에.</summary>
    void Put(TextMeshPro t, float u, float v)
    {
        t.transform.SetPositionAndRotation(board.Origin + board.Right * (u + BoxW * 0.5f) + board.Up * (v - BoxH * 0.5f) + Vector3.up * TextY, Quaternion.LookRotation(Vector3.down, board.Up));
    }

    /// <summary>글꼴에 없는 화살표는 ASCII 로 바꾼다.</summary>
    string Fit(string s) => font != null && !font.HasCharacter('→') ? s.Replace("→", "->") : s;

    /// <summary>매 프레임(보드가 꺼져 있어도) — 켜질 때 보일지 정하고, 꺼질 때 상자를 들고 나갔으면 본 것으로 적는다.</summary>
    public void Tick(bool on)
    {
        if (on && !wasOn)
        {
            show = !DayState.PackTutorialSeen; shook = false; shookBox = null; doneNow = false;
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
        if (box != shookBox) { shookBox = box; shook = false; }
        if (box != null && box.closed && box.shakeSecs >= PackPhysics.Window) shook = true;
        Current = StepOf(box, shook);
        if (board.PendingTake) doneNow = true;                               // 상자를 판 밖으로 끌었다 — 이제 물러나는 중

        if (list.gameObject.activeSelf != show) { list.gameObject.SetActive(show); now.gameObject.SetActive(show); }
        if (key.gameObject.activeSelf == show) key.gameObject.SetActive(!show);
        if (show)
        {
            string s = Fit("지금 → " + Now[(int)Current]);
            if (now.text != s) now.text = s;
        }
    }
}
