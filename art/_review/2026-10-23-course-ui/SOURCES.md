# 코스 UI 출처 — 2026-10-23

새 표지의 입력은 실제 CC0 사진·OFL 폰트·스크립트다. 원화와 게임 캡처는 검토 배경으로만 사용했다. 입력/출력 SHA256은 `sources.json`, `signs.json`, `screens.json`, `art/provenance.csv`에 기록했다. 기록은 입력 계보 확인이며 자동 AI 감지 판정은 아니다.

| 입력 | 원문·라이선스 | 가져온 것 / 바꾼 것 |
|---|---|---|
| Rusty Painted Metal, Amal Kumar | [Poly Haven 원문](https://polyhaven.com/a/rusty_painted_metal), [CC0](https://polyhaven.com/license) | diffuse PNG와 현장 context JPG 확인. diffuse의 일부를 BOX 64×64 → `photo_to_pixel.quantize_exact` 무디더 32색 → 볼트·모서리·하단 마스크로 제한. 원본 전체 사진을 표면에 붙이지 않음 |
| CardboardSet001 | [ambientCG 원문](https://ambientcg.com/view?id=CardboardSet001), [CC0 안내](https://docs.ambientcg.com/license/) | 앞선 ui-final의 CC0 사진 기반 64px 종이 패치 재사용. 새 안내문 밑면에만 반복. 소스 `art/source/photos/cargo-box/CardboardSet001_1K-PNG_Color.png` |
| Galmuri11 | [원본 저장소](https://github.com/quiple/galmuri), [제작자 안내](https://quiple.dev/font/galmuri) | OFL. 표지 12/24texel, HUD 24/36px. 파일·OFL 사본 `fonts/` |
| Pretendard | [원본 저장소](https://github.com/orioncactus/pretendard) | OFL. 긴 고객 대사와 일부 포장 본문. 12px 이진 마스크를 정확한 2×로 확대한 정적 목업. 런타임 TMP 일치 여부 미확인 |
| ui-final 스프라이트 10종 | `../2026-10-23-ui-final/sprites.json`, 그 폴더의 `SOURCES.md` | 기존 승인 대기 후보 그대로 사용. 도장·선은 스크립트, 종이는 CC0. 9-slice 여백 불변 |
| 현재 게임 캡처 11장 | `docs/qa/evidence/proto-course-*.png` 중 `sources.json` 목록 | 제공 파일을 그대로 스냅샷. 본 작업에서 Unity를 실행하거나 촬영하지 않았다. 최초 5장 + fork-sign/detour/overlook/meadow/lonehouse + packing-timeleft |
| D-096 원화 | `art/concept/lofi-styles-2026-10-13/02_warm_street.png` | 원화 참조 전용. 생성 계보는 이 작업에서 재감사하지 않음. 새 텍스처의 픽셀 입력으로 사용하지 않았으며 임포트 대상에서 제외 |
| 포장 미리보기의 상자 | ui-final `mockups/packing-ui.png`에 포함된 기존 아트팀 렌더 | 이전 화면 합성 그대로 재사용. UI팀은 3D 화물을 제작·수정하지 않음 |

원본 금속 파일은 `art/source/photos/course-signs/`에 보관했다. 직접 다운로드 URL·저자·날짜·용량은 그 폴더의 `sources.json`. diffuse SHA256: `8050abf23d77c3673368a4de3d4397a500e4b8b1c9593cf2711ca7d78e7560cb71d01ea`.

조사 근거는 [ui-04-course.md](../../../docs/research/ui-04-course.md). 화면 출처는 각 PNG의 원래 경로와 스냅샷 해시로 추적한다. 원화·리뷰 목업·거리 합성·사진 원본·레이어 작업 파일을 출시 에셋으로 임포트하지 않는다. 새 임포트 후보 허용 목록은 `signs.json`의 7개 `textures/*.png`뿐이며 도현 승인 전에는 이 역시 투입하지 않는다.
