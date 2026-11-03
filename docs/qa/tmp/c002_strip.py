# C-002: 내 클립 프레임 → bundle.sh 와 같은 방식의 4×2 strip (K = ceil(N/8))
import glob, os, subprocess, shutil, sys
d = os.path.join(os.path.dirname(os.path.abspath(__file__)), "c002_clip")
ff = shutil.which("ffmpeg") or (glob.glob(r"C:/Users/*/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg*/ffmpeg-*/bin/ffmpeg.exe") or [None])[0]
n = len(glob.glob(os.path.join(d, "frames", "f*.png")))
k = (n + 7) // 8
subprocess.run([ff, "-v", "error", "-y", "-framerate", "12", "-i", os.path.join(d, "frames", "f%03d.png"), "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "26", os.path.join(d, "clip.mp4")], check=True)
subprocess.run([ff, "-v", "error", "-y", "-i", os.path.join(d, "clip.mp4"), "-vf", f"select='not(mod(n\\,{k}))',scale=480:-1,tile=4x2", "-fps_mode", "vfr", "-frames:v", "1", os.path.join(d, "strip.png")], check=True)
print(n, k, os.path.join(d, "strip.png"))
