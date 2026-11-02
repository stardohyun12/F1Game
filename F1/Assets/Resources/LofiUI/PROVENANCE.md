# LofiUI 텍스처 출처 (D-258 UI A/B 비교판)

`art/source/depot-props/textures/` 의 128px 결과물을 그대로 복사했다. 처리 기록·sha256 은 `art/source/depot-props/texture_manifest.json`.

- `paper.png` — ambientCG CardboardSet001 (CC0-1.0)
- `cream.png`, `slate.png` — Poly Haven beige_wall_001 (CC0-1.0) https://polyhaven.com/a/beige_wall_001
- `oak.png`, `walnut.png` — Poly Haven fine_grained_wood (CC0-1.0) https://polyhaven.com/a/fine_grained_wood

처리: 밝기 크롭 → 저주파 제거 → 7단계 색 재지정. 빛·그림자 굽지 않음. AI 생성 아님.
런타임(DotScreenUI.Texture.cs)에서 1-2-1 3×3 흐림(세상 셰이더 PCTex 9탭과 비슷한 정도)과 가장자리 깎기만 더 한다.
