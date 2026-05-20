using UnityEngine;

/// <summary>
/// 크기 자동 검사 기준 (2026-05-20, game-craft-upgrade ②).
/// "버튼·클릭 영역·텍스트가 사람이 쓰기에 너무 작지 않은가"를 한 곳에 모은 기준값.
/// 코드 곳곳에서 Ergo.Check*() 호출 → 어기면 Console에 [Ergo] 경고.
///
/// 왜 SO인가: 기준값을 도현이 인스펙터에서 조정 가능하게. Resources에 1개 둠.
/// 런타임 생성 collider(핀 hover 등)는 OnValidate로 못 잡으므로, 값이 설정되는
/// 그 자리에서 Ergo.CheckWorldHoverRadius() 직접 호출하는 패턴 사용.
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/UI/ErgoConstraints", fileName = "ErgoConstraints")]
public class ErgoConstraints : ScriptableObject
{
    [Header("클릭 대상 (화면 UI, 픽셀)")]
    [Tooltip("버튼 등 클릭 대상의 최소 크기. Apple HIG 44px 기준.")]
    public float minClickTargetPx = 44f;

    [Header("3D 마커 (월드 단위)")]
    [Tooltip("핀 등 마우스 올리는 영역의 최소 반경 (월드 단위).")]
    public float minWorldHoverRadius = 3f;

    [Header("텍스트")]
    [Tooltip("읽을 수 있는 최소 폰트 크기.")]
    public float minTextSize = 14f;

    // ── 싱글톤 접근 ──
    static ErgoConstraints _active;
    public static ErgoConstraints Active
    {
        get
        {
            if (_active == null) _active = Resources.Load<ErgoConstraints>("ErgoConstraints");
            return _active;
        }
    }
}

/// <summary>
/// Ergo 검사 헬퍼. 값이 설정되는 자리에서 호출하면 어길 때 Console 경고.
/// 기준 SO가 없으면 (Resources에 없으면) 침묵 — 게임 깨지지 않게.
/// </summary>
public static class Ergo
{
    /// <summary>3D 마커 hover/클릭 영역 반경 검사 (월드 단위).</summary>
    public static void CheckWorldHoverRadius(float radius, string label, Object context = null)
    {
        var c = ErgoConstraints.Active;
        if (c == null) return;
        if (radius < c.minWorldHoverRadius)
            Debug.LogWarning($"[Ergo] {label} hover 반경 {radius:F2} < 최소 {c.minWorldHoverRadius:F2} (월드) — 마우스 올리기 어려움", context);
    }

    /// <summary>화면 UI 클릭 대상 크기 검사 (픽셀). 가로·세로 중 작은 쪽 기준.</summary>
    public static void CheckClickTarget(float widthPx, float heightPx, string label, Object context = null)
    {
        var c = ErgoConstraints.Active;
        if (c == null) return;
        float smaller = Mathf.Min(widthPx, heightPx);
        if (smaller < c.minClickTargetPx)
            Debug.LogWarning($"[Ergo] {label} 클릭 대상 {widthPx:F0}x{heightPx:F0}px (작은쪽 {smaller:F0}) < 최소 {c.minClickTargetPx:F0}px — 누르기 어려움", context);
    }

    /// <summary>텍스트 크기 검사 (폰트 size).</summary>
    public static void CheckTextSize(float fontSize, string label, Object context = null)
    {
        var c = ErgoConstraints.Active;
        if (c == null) return;
        if (fontSize < c.minTextSize)
            Debug.LogWarning($"[Ergo] {label} 폰트 {fontSize:F0} < 최소 {c.minTextSize:F0} — 읽기 어려움", context);
    }
}
