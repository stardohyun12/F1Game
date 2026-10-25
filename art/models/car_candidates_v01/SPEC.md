# D-142 F1 visual candidates

Production briefs: `art/_review/2026-10-25-car-candidates/briefs/A.md`, `C.md`.

## Integration contract

Replace visual meshes only. Do not replace Race.prefab, its Rigidbody, scripts, BoxCollider, wheel physics parameters, or scene overrides. These exports have no collider or physics script. `current-contract.json` is the authoritative static YAML trace for both Proto_CoastCourse and Map_BayLoop.

- Blender: metres, Z up, nose -Y. GLB: Y up, nose +Z. Existing Unity FBX convention mirrors Blender X; **left/right names alone do not prove Unity imported coordinates**.
- Visual parent: local `(0, .1285497, -.19642855)`, uniform scale `1.4285712`, under Race scale `.7` (product approximately 1).
- Bounds in Blender: X ±.979454517; Y -2.03..1.755; Z 0..1.072. No centering or automatic scale normalization.
- Wheels are separate meshes with local-X spin axis and origins at their measured centres. Blender `wheel_front_left=(+.79,-1.2,.35)`, front_right=(-.79,-1.2,.35), rear_left=(+.79,1.1,.375), rear_right=(-.79,1.1,.375). Unity current prefab left X=-.79 and right X=+.79. Match measured import positions and wheel role, not GLB X signs.
- Current scene collider override: size `(2.7999997,.9285714,5.407142)`, centre `(0,.5928354,0)` in Race local coordinates. No new collider is exported. After Race .7 this is approximately 1.96×.65×3.785m.
- Existing driver is a separate mesh. Candidate exports intentionally contain an empty functional cockpit, not a newly authored character; retain/rebind existing driver separately if the programming integration wants the current driver. Cargo space is empty; attach gameplay cargo separately.

## Surface / budgets

≤6,000 triangles each. All geometry UVs are 32px/m, including small number sticker; no 64px/m exception requested or used. 512² atlas per candidate, 16 shared-layout cells, Point/Nearest, common pitcrew_d123_32 palette. Actual used colors and density are measured in review `check-A.json`/`check-C.json`.

Number is a real Galmuri glyph `7`, converted to a binary 4×5 pixel impression in the 10.24×6.4px sampled sticker; no invented letters or real brand. Tiny rear plate is plain metal. Photo material sources and license records: `TEXTURE-SOURCES.md`, `texture-contract.json`. Additional seams, tape and swapped panel are functional assembly information, not random procedural wear.

Final C revision lowers the rack onto a shorter roll hoop and carries a small strapped folded weather cover, not a roof or a gameplay cargo object. The cover and straps maintain the original overall height of 1.072m. It has two additional lamps on individual short mounts and one narrow ochre stripe. A remains frozen at its approved R3 geometry and texture.

## Method and limits

Directly read local MIT blender-skills director/vehicle-artist methods. Locked wheelbase, track, diameter and overhang first, built open cockpit/shell, then separate road/cargo/rally systems. Existing bpy face projection was reused at explicit 32px/m. MCP calls replaced with bpy and exported-file measurements; no global installation. Skill high-poly/PBR recommendations do not override project palette/photo/budget rules.

References: https://newsroom.porsche.com/en/press-kits/Porsche-Museum/Porsche-Typ-804.html (open-wheel single-seat principles); https://historicdb.fia.com/car/fiat-abarth-131-rally (period rally modification context). No external vehicle model or brand livery copied. Public reference photos are not textures.

Render images are Blender previews. Village images are direct composites over an unchanged real gameplay capture; no AI background/inpainting. Unity shader matching, animation, clipping, collider interaction and actual gameplay camera require programming/QA import and captures. Static geometry and round-trip checks cannot prove runtime integration.
