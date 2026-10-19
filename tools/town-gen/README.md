# tools/town-gen — 마을 생성기 실험 원본 캐시

스펙: `docs/superpowers/specs/2026-10-19-town-generator-experiment.md`

- `fetch_sources.py` — Overpass(OSM 도로) + Copernicus DEM 타일 캐시. 있으면 건너뜀, `--force` 로 다시 받기.
  `py -X utf8 tools/town-gen/fetch_sources.py`
- `osm/` — Overpass 원본 JSON + `.meta.json` + `LICENSE-ODbL.md`
- `dem/` — GLO-30 타일(.tif, LFS) + 라이선스 PDF + `ATTRIBUTION.md`
- roadgen(앞단 B) 소스는 `F1/Assets/Editor/ThirdParty/roadgen/` (Editor 전용 asmdef, `SOURCE.md` 에 출처·패치)
