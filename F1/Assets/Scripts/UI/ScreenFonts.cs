using UnityEngine;

/// <summary>
/// D-287 6단계: 화면 글꼴 두 벌(본문·작은). 화면(Presentation 의 DotScreenUI)이 이걸 달고, 본체 물건(칠판·방 달력)은 화면을 직접 모르고 이걸로 찾는다.
/// 본체는 Presentation 을 참조할 수 없다(Presentation 이 본체를 참조) — 그래서 약속(인터페이스)만 본체에 둔다.
/// </summary>
public interface IScreenFonts
{
    Font BodyFont { get; }
    Font SmallFont { get; }
}

public static class ScreenFonts
{
    /// <summary>켜진 오브젝트 중 화면 글꼴을 가진 첫 컴포넌트(예전 FindFirstObjectByType&lt;DotScreenUI&gt;() 자리). 없으면 null.</summary>
    public static IScreenFonts Find()
    {
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb is IScreenFonts f) return f;
        return null;
    }
}
