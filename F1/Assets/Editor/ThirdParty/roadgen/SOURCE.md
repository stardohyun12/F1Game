# roadgen (third-party, MIT)

- 원본: https://github.com/pboechat/roadgen
- 커밋: f36001e8def37fced90e01b8c7576b45aa780e58 (2024-03-02), 원 프로젝트 Unity 5.3.5f1
- 가져온 것: `Assets/RoadGen/Scripts/` 만 (Eppy·Mischel 포함). Editor/UnitTests·PerformanceTests·씬·프리팹은 제외.
- 라이선스: MIT — `LICENSE.txt` (원본 그대로)
- 용도: 마을 생성기 실험 E0~E2 (docs/superpowers/specs/2026-10-19-town-generator-experiment.md §6.2). Editor 전용 어셈블리라 배포 빌드에 안 들어감.
- 원본 대비 수정: 아래 "패치" 절에 줄 단위로 기록.

## 패치
1. `Tuple` 이름 충돌(CS0104: `RoadGen.Eppy.Tuple` vs `System.Tuple`, .NET 4 이후 생김) — `RoadDensityBasedSettlementSpawner.cs`, `RoadNetworkGenerator.cs`, `RoadNetworkGeometryBuilder.cs` 의 `Tuple<`/`Tuple.` 를 `Eppy.Tuple<`/`Eppy.Tuple.` 로 치환(sed, 의미 변화 없음). 전역 네임스페이스인 `RoadDensityBasedSettlementSpawner.cs` 는 `RoadGen.Eppy.Tuple` 로.
2. `GlobalSeeder.cs:20` `UnityEngine.Random.seed = seed;` → `UnityEngine.Random.InitState(seed);` (CS0618 경고 제거, 같은 시드 초기화).
