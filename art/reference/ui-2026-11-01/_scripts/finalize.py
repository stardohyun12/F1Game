"""저장소용 저해상도 선별본(ref/, 가로 640 JPEG q70) + SOURCES.md 생성, 원본 raw/ 는 저장소 밖(TEMP)으로 이동.
사용: python -I finalize.py <uidir>"""
import sys, os, json, shutil, tempfile, urllib.request
from PIL import Image

U = sys.argv[1]
RAW = os.path.join(U, "raw")
REF = os.path.join(U, "ref")
os.makedirs(REF, exist_ok=True)
APP = {"shorthike": 1055540, "liligator": 1586800, "jalopy": 446020, "toem": 1307580, "mailtime": 1607240,
       "wheelworld": 1497460, "deathstranding": 1850570, "webfishing": 3146520, "dredge": 1562430, "unpacking": 1135690}
PICK = {"shorthike": ["00", "03"], "liligator": ["01", "03"], "jalopy": ["01", "02", "04"], "toem": ["02", "04"],
        "mailtime": ["01", "04"], "wheelworld": ["00", "02", "04"], "deathstranding": ["15", "20", "21"],
        "webfishing": ["02", "05"], "dredge": ["01", "02"], "unpacking": ["02", "04"]}

def get(u):
    return urllib.request.urlopen(urllib.request.Request(u, headers={"User-Agent": "Mozilla/5.0"}), timeout=30).read()

rows = []
for g, nums in PICK.items():
    d = json.loads(get(f"https://store.steampowered.com/api/appdetails?appids={APP[g]}&l=english"))[str(APP[g])]["data"]
    urls = {f"{s['id']:02d}": s["path_full"].split("?")[0] for s in d["screenshots"]}
    for n in nums:
        im = Image.open(os.path.join(RAW, g, f"{n}.jpg")).convert("RGB")
        w, h = im.size
        im.resize((640, round(640 * h / w)), Image.LANCZOS).save(os.path.join(REF, f"{g}_{n}.jpg"), quality=70)
        rows.append((f"ref/{g}_{n}.jpg", d["name"], APP[g], f"{w}x{h}", urls[n]))
FIU = {"deathstranding_order": ("fiu_order.jpg", "https://original.fontsinuse.com/fontsinuse.com/use-images/N255/255100/255100.jpeg"),
       "deathstranding_complete": ("fiu_complete.jpg", "https://original.fontsinuse.com/fontsinuse.com/use-images/N255/255098/255098.jpeg")}
for k, (f, u) in FIU.items():
    im = Image.open(os.path.join(RAW, "deathstranding", f)).convert("RGB")
    im.save(os.path.join(REF, f"{k}.jpg"), quality=75)  # 이미 700px
    rows.append((f"ref/{k}.jpg", "DEATH STRANDING (fontsinuse 갤러리 캡처, 700px)", 1850570, "700x394", u))

with open(os.path.join(U, "SOURCES.md"), "w", encoding="utf-8") as f:
    f.write("# UI 레퍼런스 스크린샷 — 출처·이용 메모 (2026-11-01)\n\n")
    f.write("- **내부 참고·분석 전용. 게임에 넣지 않는다.** 저작권은 각 개발사·배급사에 있다. 저장소엔 가로 640 저해상도 선별본(`ref/`)과 연락판(`_sheets/`)·일부 1:1 잘라낸 조각(`_sheets/crops_*.jpg`, 한 장면의 4~8%)만 둔다.\n")
    f.write("- 출처 = Steam 스토어에 개발사가 올린 **공식 게시 스크린샷**(스토어 API `appdetails` 의 `screenshots[].path_full`). 직접 플레이 캡처가 아니다 — 개발사가 고른 홍보 컷이라 HUD 가 빠졌을 수 있다.\n")
    f.write("- Death Stranding 의 주문·결과 화면 2장은 스토어 컷에 없어 [fontsinuse 갤러리](https://fontsinuse.com/uses/67648/death-stranding-video-game) 의 게임 캡처(700px 판)를 썼다.\n")
    f.write("- Easy Delivery Co. 는 이미 `art/reference/edc-2026-10-26/` 에 9장이 있어 다시 받지 않았다(SOURCES.md 참조).\n")
    f.write("- 원본 1920×1080 은 저장소 밖(TEMP `ui-2026-11-01-raw`)으로 옮겼다. 다시 받으려면 `_scripts/fetch.py`.\n")
    f.write("- 박스 면적 측정: `measurements.json` + `_scripts/measure.py`.\n\n")
    f.write("| 파일 | 게임 | Steam app | 원본 크기 | 원본 URL |\n|---|---|---|---|---|\n")
    for r in rows:
        f.write(f"| {r[0]} | {r[1]} | {r[2]} | {r[3]} | {r[4]} |\n")
print(len(rows), "rows")
tgt = os.path.join(tempfile.gettempdir(), "ui-2026-11-01-raw")
shutil.move(RAW, tgt)
print("raw moved to", tgt)
