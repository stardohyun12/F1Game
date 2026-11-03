# 2판 연속 프레임(<tag>_NNN.png) → 태그별 한 장 시트(6열) + 전체 이어 붙인 mp4(10 fps).
# 사용: python -I sheet.py <evidence 폴더> <tag> [<tag> ...]
import glob, os, re, subprocess, sys, shutil, tempfile
from PIL import Image, ImageDraw

d, tags = sys.argv[1], sys.argv[2:]
allf = []
for tag in tags:
    fs = sorted(glob.glob(os.path.join(d, f"{tag}_[0-9][0-9][0-9].png")))
    if not fs:
        print(tag, "no frames"); continue
    allf += fs
    ims = [Image.open(f).convert("RGB") for f in fs]
    w, h = ims[0].size
    tw = 320; th = int(h * tw / w)
    cols = 6; rows = (len(ims) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * tw, rows * th), (0, 0, 0))
    dr = ImageDraw.Draw(sheet)
    for i, (f, im) in enumerate(zip(fs, ims)):
        x, y = (i % cols) * tw, (i // cols) * th
        sheet.paste(im.resize((tw, th), Image.BILINEAR), (x, y))
        dr.text((x + 4, y + 4), os.path.basename(f)[:-4], fill=(255, 255, 0))
    out = os.path.join(d, f"sheet_{tag}.png")
    sheet.save(out)
    print(out, len(ims), "frames")
ff = shutil.which("ffmpeg") or next(iter(glob.glob(os.path.expandvars(
    r"C:/Users/rtx40/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_*/ffmpeg-*/bin/ffmpeg.exe"))), None)
if ff and allf:
    tmp = tempfile.mkdtemp()
    for i, f in enumerate(allf):
        shutil.copy(f, os.path.join(tmp, f"f{i:04d}.png"))
    out = os.path.join(d, "packbench-v2.mp4")
    subprocess.run([ff, "-y", "-loglevel", "error", "-framerate", "10", "-i", os.path.join(tmp, "f%04d.png"),
                    "-vf", "scale=trunc(iw/2)*2:trunc(ih/2)*2", "-pix_fmt", "yuv420p", out], check=True)
    print(out, len(allf), "frames =", len(allf) / 10, "s")
