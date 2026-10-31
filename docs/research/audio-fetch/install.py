"""Copy built wavs into the Unity folder and print size/duration/sha256. Usage: install.py STAGEDIR DESTDIR"""
import hashlib
import os
import shutil
import sys
import wave

src, dst = sys.argv[1], sys.argv[2]
os.makedirs(dst, exist_ok=True)
for name in sorted(os.listdir(src)):
    if not name.endswith(".wav"):
        continue
    shutil.copy2(os.path.join(src, name), os.path.join(dst, name))
    p = os.path.join(dst, name)
    with wave.open(p, "rb") as w:
        dur = w.getnframes() / w.getframerate()
        info = f"{w.getnchannels()}ch {w.getframerate()}Hz {8 * w.getsampwidth()}bit"
    h = hashlib.sha256(open(p, "rb").read()).hexdigest()
    print(f"{name}\t{os.path.getsize(p)}\t{dur:.2f}s\t{info}\t{h}")
