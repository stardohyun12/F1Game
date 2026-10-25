using UnityEngine;

// Phase 2 — BarHUDBase Template Method 적용. 178줄 → 50줄.
// 학습: GDD/architecture/phase2-template-method.md
// IntegrityHUD는 BarHUDBase의 OnPostAwake/OnApplyEffects hook으로 충돌 flash 추가.

public class IntegrityHUD : BarHUDBase
{
    [SerializeField] CargoBox cargo;

    [Header("Impact Flash (D-043)")]
    [SerializeField] float flashDuration = 0.18f;

    float flashEndTime;
    Color baseBgColor;

    protected override void OnAcquireDependency()
    {
        if (cargo == null) cargo = Services.Cargo as CargoBox;
    }

    protected override bool HasDependency() => cargo != null;

    // 화물 무결성 HUD는 운전 단계에만 표시.
    // 과제 0344: 실은 화물이 있을 때(런 진행 중)만 — 자유 주행(튜토리얼 T0 MART 가는 길)에 기본 주문 "상자 100%" 가 보였다.
    protected override bool ShouldShow()
    {
        var run = Services.Run as RunController;
        return run != null && run.CurrentStage == RunStage.Driving && run.RunActive;
    }

    protected override void OnPostAwake() => HookCargoEvents();

    void HookCargoEvents()
    {
        if (cargo == null) return;
        cargo.OnImpact -= OnCargoImpact;
        cargo.OnImpact += OnCargoImpact;
    }

    void OnCargoImpact(float speedKmh)
    {
        float t = Mathf.Clamp01(speedKmh / 40f);
        flashEndTime = Time.unscaledTime + flashDuration * (0.5f + 0.5f * t);
    }

    protected override void OnApplyEffects()
    {
        if (bg == null) return;
        var theme = UITheme.Active;
        if (theme != null) baseBgColor = theme.colorPanel;
        float remaining = flashEndTime - Time.unscaledTime;
        if (remaining > 0f)
        {
            float t = Mathf.Clamp01(remaining / flashDuration);
            bg.color = Color.Lerp(baseBgColor, new Color(1f, 0.25f, 0.25f, 1f), t);
        }
    }

    protected override float OnLevel() => cargo.IntegrityPercent;

    protected override string OnLabel(float level)
    {
        string n = CargoTypePresets.DisplayName(cargo.Type);
        return cargo.IsTotaled
            ? $"{n} 전파"
            : $"{n}  {Mathf.RoundToInt(level * 100)}%";
    }
}
