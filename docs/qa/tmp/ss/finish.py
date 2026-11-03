# 과제 1428: 영상 인코딩(ffmpeg) + 구간별 시트(위 = 대역 없음, 아래 = 대역 있음)
import subprocess, os
from PIL import Image, ImageDraw, ImageFont
FF = r'C:/Users/rtx40/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-8.1.2-full_build/bin/ffmpeg.exe'
EV = 'docs/qa/evidence/streetscape-proxy'
try: font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
except Exception: font = ImageFont.load_default()
title = {'tomo': 'tomo 큰길 (tomo_1296983883_62, 25m 간격)', 'posi': 'posi 헤어핀 (posi_1567571332_429, 25m 간격)', 'ridge': '능선 핵심 4채 앞 (camo_309378634_165, 25m 간격)'}
for s in ('tomo', 'posi', 'ridge'):
    out = f'{EV}/{s}/drive_with.mp4'
    subprocess.run([FF, '-y', '-loglevel', 'error', '-framerate', '30', '-i', f'docs/qa/tmp/ss/frames_{s}/f%04d.png',
                    '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '20', out], check=True)
    print(out, os.path.getsize(out))
    w, h, top = 640, 360, 40
    sheet = Image.new('RGB', (w * 6, top + h * 2 + 30), (24, 24, 24)); d = ImageDraw.Draw(sheet)
    d.text((10, 8), title[s] + '   위 = 대역 없음 / 아래 = 대역 있음', fill=(235, 235, 235), font=font)
    for row, k in enumerate(('without', 'with')):
        for i in range(6):
            im = Image.open(f'{EV}/{s}/{k}_{i+1:02d}.png').resize((w, h), Image.LANCZOS)
            sheet.paste(im, (i * w, top + row * h))
            d.text((i * w + 8, top + row * h + 6), f'{k} {i+1:02d}', fill=(255, 255, 0), font=font)
    sheet.save(f'{EV}/sheet_{s}.png'); print(f'{EV}/sheet_{s}.png')
