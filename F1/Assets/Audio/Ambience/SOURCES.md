# Ambience 출처 기록 (D-227 노을 장면)

전부 **BigSoundBank (Joseph Sardin, bigsoundbank.com)** 의 **CC0 1.0 / Public Domain** 소리. 표기 의무 없음(선택: "Additional sounds: Joseph SARDIN - BigSoundBank.com").
2026-10-31 에 각 소리 페이지를 직접 열어 `License: CC0 (public domain)` 문구와 [licenses.html](https://bigsoundbank.com/licenses.html) 의 CC0 1.0 / 상업 이용 허용 문구를 확인했다. AI 생성 소리 아님(현장 녹음 또는 폴리 제작).
내려받은 원본: `UPLOAD/bwf-en/<번호>.wav` (BWF 무손실). 가공: 44.1 kHz 스테레오로 변환, 16-bit WAV 저장, 아래 구간 자름. 이퀄라이저·노이즈 제거 없음.

| 파일 | 길이 | 원본 페이지 | 원본 파일 URL | 작가 | 라이선스 | 자른 구간(원본 시간) | 가공 |
|---|---|---|---|---|---|---|---|
| `amb_waves_01.wav` | 45.00 s | [Small waves, rocks and beach (s1445)](https://bigsoundbank.com/small-waves-rocks-and-beach-s1445.html) — "Noise of small waves crashing against small rocks." (원본 3:55) | https://bigsoundbank.com/UPLOAD/bwf-en/1445.wav | Joseph Sardin (2019-05-31 게시) | CC0 | 160.5 – 209.5 s (49 s) | 루프: 끝 4 s 를 앞 4 s 에 등전력(sin/cos) 크로스페이드. 피크 -1 dBFS 가 되게 -0.34 dB |
| `amb_wind_01.wav` | 45.00 s | [Wind (s0595)](https://bigsoundbank.com/wind-s0595.html) — "Wind noise, made with a technique Foley" (원본 1:30) | https://bigsoundbank.com/UPLOAD/bwf-en/0595.wav | Joseph Sardin (2013-01-05 게시) | CC0 | 15.0 – 65.0 s (50 s) | 루프: 끝 5 s 를 앞 5 s 에 등전력 크로스페이드. 게인 0 dB |
| `amb_gull_01.wav` | 2.10 s | [Gulls on the Harbor (s2573)](https://bigsoundbank.com/gulls-on-the-harbor-s2573.html) — "Soundscape on a small fishing port where gulls cry." (원본 1:55) | https://bigsoundbank.com/UPLOAD/bwf-en/2573.wav | Joseph Sardin (2022-04-18 게시) | CC0 | 15.15 – 17.25 s | 페이드 인 40 ms · 아웃 250 ms, 게인 0 |
| `amb_gull_02.wav` | 1.90 s | 위와 같음 | 위와 같음 | 위와 같음 | CC0 | 20.00 – 21.90 s | 위와 같음 |
| `amb_gull_03.wav` | 1.80 s | 위와 같음 | 위와 같음 | 위와 같음 | CC0 | 33.65 – 35.45 s | 위와 같음 |
| `amb_gull_04.wav` | 1.90 s | 위와 같음 | 위와 같음 | 위와 같음 | CC0 | 38.80 – 40.70 s | 위와 같음 |
| `amb_gull_05.wav` | 2.20 s | 위와 같음 | 위와 같음 | 위와 같음 | CC0 | 110.15 – 112.35 s | 위와 같음 (다른 두 마리 톤의 긴 울음 두 번 — 가장 가깝고 큼) |

## 알아둘 것
- **바람은 현장 녹음이 아니라 폴리(사람이 만든 소리)** 다. 해안 바람 현장 녹음 CC0 는 이번에 못 찾았다(Freesound 는 원본 다운로드에 로그인이 필요하고, 찾은 파도 후보는 CC-BY). 에너지의 77% 가 150 Hz–1 kHz 이고 45 s 동안 음량이 ±0.5 dB 로 거의 일정하다 → **돌풍이 없다.** 게임 쪽에서 볼륨을 천천히 흔들어 주는 게 맞다.
- **갈매기는 가까이서 녹음**돼 있다(배경 대비 +20 dB). "멀리서" 느낌은 AudioSource 거리 감쇠·로우패스로 낸다. 5개 모두 같은 녹음(작은 어항)에서 잘라서 목소리 톤이 비슷하다.
- 루프 이음매는 수치로만 검사했다(클릭·음량 단차 없음). **귀로 듣는 판정은 도현 몫.**
- Unity 가져오기: `.meta` 는 에디터가 만든다. 루프 파일은 AudioSource 의 `Loop` 를 켜고 Load Type 은 Streaming 이 아니라 Decompress On Load 로 하면 이음매가 안 틘다(권장, 미검증).

## sfx_splash_01 (11/01, PD)
- 원본: https://bigsoundbank.com/splash-small-2-s1530.html (Joseph Sardin, CC0, 페이지에서 라이선스 확인), raw 1530.wav 그대로(3초, 48kHz/24bit mono). 찌 착수 소리(FishingSpot.splashClip).
