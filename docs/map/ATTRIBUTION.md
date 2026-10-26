# 지도 데이터 출처

**© OpenStreetMap contributors** — Open Database License (ODbL) 1.0, https://www.openstreetmap.org/copyright
고도: SRTM 30 m (NASA, 공개), Open Topo Data API 경유.

## 쓴 파일
- `docs/map/real-towns/*.json`: Overpass 원본 캐시. 그림은 `docs/map/real-towns/*.png`, 만든 코드는 `tools/map/real_towns.py`.
- `docs/map/town-v6osm.json`, `town-v6osm.png`, `v6osm-vs-real.png`: 아래 영역을 잘라 회전·이동해 합성했다. 코드는 `tools/map/compose_osm_town.py`.
  - 영역은 각 마을 캐시 1200 m 사방 안이다. 원본 중심(m) 기준 사각형이며, 정확한 값은 json `pieces`.
  - 도모노우라 (34.3840 N, 133.3815 E 중심 칸): 원본 (560,533) 기준 x −260..220, z −270..170.
  - 카몰리 (44.3500 N, 9.1555 E 중심 칸): 원본 (507,560) 기준 47.6° 회전, x −330..300, z −150..330.
  - 포지타노 (40.6285 N, 14.4855 E 중심 칸): 원본 (373,673) 기준 x −253..253, z −200..210.

## 지켜야 할 것
- ODbL 파생 데이터베이스다. 게임 안이나 크레딧에 "© OpenStreetMap contributors" 를 표기한다.
- 이 합성 데이터(json)를 공개 배포하면 같은 ODbL 로 공개해야 한다(share-alike).
- 렌더한 그림·게임 화면은 "Produced Work" 이므로 출처 표기만 하면 된다.
