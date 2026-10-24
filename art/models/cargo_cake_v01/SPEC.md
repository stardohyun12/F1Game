# cargo_cake_v01
- 254×254×127mm white window box; bottom-centered pivot. 290 tris. 64×64 shared32 palette, Point, 64px/m orthonormal mapping for every polygon.
- Four real rails surround190×190mm opening; separate thin film alpha0.07, no refraction/normal map. Inside actual lowpoly cake and three subdued berries; transparency needs engine import review.
- Six directional packaging UV regions; front small barcode slip, right paired triangular ↑↑, thin muted bakery stripe. No unreadable fake lettering. Label width25% of face.
- Geometry only for silhouettes/window/interior; folds/contact AO and paper variation in texture. One ambient and warm directional light in local preview; final common render is root review pipeline.
- CC0 photo → downscale → palette → cream ladder → information stamps → sparse only-flat-window correction → fold/contact AO. Source/patch stages and provenance-row.json retained.
- Build: `py art/models/cargo_cake_v01/build_atlas.py cake`, then Blender `-b -P art/models/cargo_cake_v01/generate.py -- cake`.
- Unity not opened, engine import unverified. F1/Assets not changed.
