# 노을 낚시 자리 — 소리 소스 (Play 중 실측, 2026-10-31, 앉은 상태, listener = (-9.0, 2.3, -291.0))

`docs/qa/tmp/sunset/p3.cs` 출력 그대로:

- Amb_Wind — clip amb_wind_01, 재생 중, loop, vol 0.23 (gust 0.35 로 0.10~0.30 사이를 오감), spatialBlend 0.3, min/max 20/220 m, 거리 0 m — WindAmbience 재사용(ignoreRun 켬)
- Amb_Waves — clip amb_waves_01, 재생 중, loop, vol 0.55, 3D(1.0), Logarithmic min/max 4/60 m, 거리 5.4 m (자리 바로 앞 물가 (-8.6, 0.2, -296))
- Amb_Gulls — PlayOneShot 전용(clip 슬롯 비어 있는 게 정상), 3D, min/max 6/120 m, AudioLowPassFilter 3.8 kHz, 갈매기 5개 중 무작위, 20~40 s 간격, vol 0.22, 피치 0.92~1.08, 자리 둘레 반경 15~25 m · 높이 12 m. 8 s 측정 동안 1번 울음(GullCalls=1, 첫 울음은 8~24 s 사이로 당김)
- ⚠ Race — clip EngineLoop, 재생 중, loop, vol 0.35, **2D**, 336 m 떨어진 주차된 차 — 걷기·앉기 중에도 엔진 소리가 들린다(이번 과제 밖, 기존 걷기 기능의 동작)

소리 원본: F1/Assets/Audio/Ambience/SOURCES.md (BigSoundBank, CC0).
