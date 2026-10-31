"""Append provenance rows for the Ambience files (no-op for ids already present)."""
import csv
import io

PATH = "D:/Github/F1Game/art/provenance.csv"
BASE = "F1/Assets/Audio/Ambience/"
ACC = "2026-10-31"
TOOL = "python+numpy+ffmpeg decode, equal-power crossfade loop (docs/research/audio-fetch/build.py)"
rows = [
    ("amb_waves_01", "amb_waves_01.wav", "https://bigsoundbank.com/small-waves-rocks-and-beach-s1445.html", "e11f50812ea539bc1df7df47974002e960aba545c2351a60e65f292f1de73404",
     "BigSoundBank s1445 Small waves rocks and beach by Joseph Sardin; raw https://bigsoundbank.com/UPLOAD/bwf-en/1445.wav; cut 160.5-209.5s; 45s loop 4s crossfade; field recording"),
    ("amb_wind_01", "amb_wind_01.wav", "https://bigsoundbank.com/wind-s0595.html", "989d77d560dfe693b66353b7c0eb023036a322d701123143af83db525e36faa3",
     "BigSoundBank s0595 Wind by Joseph Sardin; raw https://bigsoundbank.com/UPLOAD/bwf-en/0595.wav; cut 15-65s; 45s loop 5s crossfade; Foley (human-made) not field recording"),
]
gulls = {
    1: ("530fcff771732054452a85f396724d1345c0ab49d279eb9822e3067679f02b67", "15.15-17.25"),
    2: ("6109a90c4453af5816253fc15d58db2cb230201fd60e68f9d860e8ebcc9ca0ba", "20.00-21.90"),
    3: ("693068d07660b1c0edde12e50cf93d930b97cf841fec2592a63b9331d716ba46", "33.65-35.45"),
    4: ("06aa96cdef97aab358dff4ddb2891778ea7df358cfa82f1769cde87304bbd75b", "38.80-40.70"),
    5: ("b30fd2e52e7169881ebb9723045d2aa033d9fec77b21cdb577d03d7c689a5d68", "110.15-112.35"),
}
for i, (sha, rng) in gulls.items():
    rows.append((f"amb_gull_{i:02d}", f"amb_gull_{i:02d}.wav", "https://bigsoundbank.com/gulls-on-the-harbor-s2573.html", sha,
                 f"BigSoundBank s2573 Gulls on the Harbor by Joseph Sardin; raw https://bigsoundbank.com/UPLOAD/bwf-en/2573.wav; cut {rng}s; fade 40ms in 250ms out; field recording"))

text = open(PATH, encoding="utf-8", newline="").read()
have = {r[0] for r in csv.reader(io.StringIO(text)) if r}
if not text.endswith("\n"):
    text += "\r\n"
buf = io.StringIO()
w = csv.writer(buf, lineterminator="\r\n")  # file is CRLF
n = 0
for asset_id, fname, url, sha, notes in rows:
    if asset_id in have:
        continue
    w.writerow([asset_id, BASE + fname, url, "CC0-1.0 (BigSoundBank)", ACC, TOOL, "no", sha, notes])
    n += 1
open(PATH, "w", encoding="utf-8", newline="").write(text + buf.getvalue())
print("appended", n)
