"""최종 12장 번호 매기기 + 나머지 후보는 stills/_cands 로 + 3x3 블록 균일 검사(정수 배율 증거)."""
import shutil, json
from pathlib import Path
import numpy as np
from PIL import Image

root = Path(__file__).resolve().parent.parent
st = root / "stills"
final = root / "final"
cands = st / "_cands"
final.mkdir(exist_ok=True); cands.mkdir(exist_ok=True)

PICK = [
    ("01_fishing_sunset", "01_fishing_sunset"),
    ("02_cart_cargo", "cand_cart1"),
    ("03_slope_alley_afternoon", "08_mainroad_afternoon"),
    ("04_garage", "cand_garage9"),
    ("05_depot", "06_depot_afternoon"),
    ("06_room_window", "cand_room130"),
    ("07_lighthouse", "cand_lh5"),
    ("08_slip", "09_slip"),
    ("09_sea_from_slope", "cand_over6"),
    ("10_cloud_shadow_road", "cand_cloud1"),
    ("11_mainroad_sunset", "03_mainroad_sunset"),
    ("12_jump", "cand_jump_a"),
]
res = {}
for new, old in PICK:
    for sz in ("1080x1350", "1080x1080"):
        src = st / f"{old}_{sz}.png"
        if not src.exists():
            res[f"{new}_{sz}"] = "MISSING"; continue
        shutil.copy2(src, final / f"{new}_{sz}.png")
        a = np.asarray(Image.open(src).convert("RGB"))
        h, w, _ = a.shape
        b = a.reshape(h // 3, 3, w // 3, 3, 3)
        uniform = bool((b == b[:, :1, :, :1, :]).all())
        res[f"{new}_{sz}"] = {"size": [w, h], "3x3_uniform": uniform}

picked_old = {o for _, o in PICK}
for f in list(st.glob("*.png")):
    base = f.name.rsplit("_1080x", 1)[0]
    if base not in picked_old:
        shutil.move(str(f), str(cands / f.name))
(root / "final_check.json").write_text(json.dumps(res, ensure_ascii=False, indent=1), encoding="utf-8")
bad = [k for k, v in res.items() if v == "MISSING" or not v["3x3_uniform"]]
print("final:", len(res), "bad:", bad)
