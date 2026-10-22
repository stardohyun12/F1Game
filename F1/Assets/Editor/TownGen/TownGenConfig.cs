using PitCrew.TownGen;
using UnityEngine;

/// <summary>
/// 마을 생성기 튜닝값 에셋 — 인스펙터에서 바꾸면 재컴파일 없이 다음 생성에 반영된다.
/// 기본 에셋: Assets/Data/TownGen/TownGenConfig.asset (메뉴 PitCrew/TownGen/Create Config 가 만든다).
/// 값의 뜻·출처는 TownGenSettings 주석(스펙 §3·§4·§12). Editor 전용(배포 빌드에 안 들어감).
/// </summary>
[CreateAssetMenu(menuName = "PitCrew/TownGen Config", fileName = "TownGenConfig")]
public class TownGenConfig : ScriptableObject
{
    public TownGenSettings settings = new TownGenSettings();
    /// <summary>곡선 큰길 마을(스펙 2026-10-20-spline-town) 튜닝값.</summary>
    public PitCrew.TownGen.SplineTownSettings spline = new PitCrew.TownGen.SplineTownSettings();
}
