import subprocess, pathlib, json
FF = "C:/Users/rtx40/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-8.1.2-full_build/bin/"
root = pathlib.Path("D:/Github/F1Game/art/_review/2026-11-02-insta/video")
tmp = pathlib.Path("D:/Github/F1Game/art/_review/2026-11-02-insta/_scripts/_probe"); tmp.mkdir(exist_ok=True)
for f in sorted(root.glob("*.mp4")):
    r = subprocess.run([FF + "ffprobe.exe", "-v", "error", "-count_frames", "-select_streams", "v:0", "-show_entries", "stream=width,height,r_frame_rate,nb_read_frames,duration,codec_name", "-of", "json", str(f)], capture_output=True, text=True)
    print(f.name, f.stat().st_size, "bytes", r.stdout.replace("\n", "").replace(" ", ""), r.stderr[:200])
    for t in (1, 7, 14):
        subprocess.run([FF + "ffmpeg.exe", "-y", "-v", "error", "-ss", str(t), "-i", str(f), "-frames:v", "1", str(tmp / (f.stem + f"_t{t}.png"))])
print(sorted(p.name for p in tmp.iterdir()))
