# EDC 공식 스크린샷 — 출처 (내부 참고용, 게임에 넣지 않음)

- 게임: Easy Delivery Co. (Sam C. / Oro Interactive), Steam app 3293010
- 받은 곳: https://store.steampowered.com/app/3293010/?l=english 스토어 HTML 의 스크린샷 해시(`ss_<hash>`) → 원본 주소(크기 접미사 없는 `.jpg`).
- 받은 날: 2026-10-26. 9장. 원본 그대로(재압축 없음). 저작권은 개발사에 있다. 분석·비교용으로만 쓴다.
- 분석 문서: `docs/research/2026-10-26-edc-visual-grammar.md`. 측정 스크립트·수치·비교 시트: `_analysis/`.

| 파일 | 크기 | 원본 URL | 1920×1080 판 URL |
|---|---|---|---|
| edc_01.jpg | 1920×1080 | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/125f45762e597b3afb008aee8bbefac19ba02941/ss_125f45762e597b3afb008aee8bbefac19ba02941.jpg | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/125f45762e597b3afb008aee8bbefac19ba02941/ss_125f45762e597b3afb008aee8bbefac19ba02941.1920x1080.jpg |
| edc_02.jpg | 1920×1080 | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/cf5ed0c38426b0b7d592fd7d5d9006043e2fa948/ss_cf5ed0c38426b0b7d592fd7d5d9006043e2fa948.jpg | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/cf5ed0c38426b0b7d592fd7d5d9006043e2fa948/ss_cf5ed0c38426b0b7d592fd7d5d9006043e2fa948.1920x1080.jpg |
| edc_03.jpg | 1920×1080 | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/2df6e79b65c499cd45590df8b1cbf0a705a2a6a8/ss_2df6e79b65c499cd45590df8b1cbf0a705a2a6a8.jpg | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/2df6e79b65c499cd45590df8b1cbf0a705a2a6a8/ss_2df6e79b65c499cd45590df8b1cbf0a705a2a6a8.1920x1080.jpg |
| edc_04.jpg | 1920×1080 | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/287ddbe79e43b36d1ab74111609212f8cc7aeb64/ss_287ddbe79e43b36d1ab74111609212f8cc7aeb64.jpg | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/287ddbe79e43b36d1ab74111609212f8cc7aeb64/ss_287ddbe79e43b36d1ab74111609212f8cc7aeb64.1920x1080.jpg |
| edc_05.jpg | 1920×1080 | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/b44a25aaae8d91772613af7a3334084bbbb76c4c/ss_b44a25aaae8d91772613af7a3334084bbbb76c4c.jpg | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/b44a25aaae8d91772613af7a3334084bbbb76c4c/ss_b44a25aaae8d91772613af7a3334084bbbb76c4c.1920x1080.jpg |
| edc_06.jpg | 1920×1080 | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/ef75d56aced607ac06e8f01bb4eee91c22574149/ss_ef75d56aced607ac06e8f01bb4eee91c22574149.jpg | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/ef75d56aced607ac06e8f01bb4eee91c22574149/ss_ef75d56aced607ac06e8f01bb4eee91c22574149.1920x1080.jpg |
| edc_07.jpg | 1920×1080 | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/b18bce54e569db1c1dc1dc9748807be290368f9d/ss_b18bce54e569db1c1dc1dc9748807be290368f9d.jpg | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/b18bce54e569db1c1dc1dc9748807be290368f9d/ss_b18bce54e569db1c1dc1dc9748807be290368f9d.1920x1080.jpg |
| edc_08.jpg | 2560×1440 | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/9eb8f4e5b90e003fd593f111b2a13332ee682b30/ss_9eb8f4e5b90e003fd593f111b2a13332ee682b30.jpg | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/9eb8f4e5b90e003fd593f111b2a13332ee682b30/ss_9eb8f4e5b90e003fd593f111b2a13332ee682b30.1920x1080.jpg |
| edc_09.jpg | 1920×1080 | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/e4a98b2934d35ce7e20da63507f417ecaf11d3fc/ss_e4a98b2934d35ce7e20da63507f417ecaf11d3fc.jpg | https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3293010/e4a98b2934d35ce7e20da63507f417ecaf11d3fc/ss_e4a98b2934d35ce7e20da63507f417ecaf11d3fc.1920x1080.jpg |

## `_analysis/`
- `measure.py` 전체 통계, `period.py` 픽셀 계단 주기(분수 주기 DFT), `native.py` 내부 해상도로 되돌린 판(블록 중앙 3×3 중앙값) + 통계, `regions.py` 표면별 잡음.
- `m1.json`·`m2.json` 수치. `compare_native_x4.png` = EDC 와 우리 표면을 내부 해상도 그대로 4배 확대한 비교 시트.
- 실행: `py -3 _analysis/period.py <png/jpg...>` (numpy·PIL 필요 — Hermes 기본 python 에는 numpy 가 없다).
