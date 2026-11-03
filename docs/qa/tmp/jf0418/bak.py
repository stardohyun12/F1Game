# 백업/복원: python -X utf8 bak.py save|restore|changed
import os, shutil, sys, hashlib
R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
B = os.path.join(os.path.dirname(os.path.abspath(__file__)), "bak")
ITEMS = ["F1/Assets/Scenes/Map_TownV6.unity", "F1/Assets/Scenes/Map_TownV6_Mats", "docs/map", "docs/qa/tmp/roadv2_disabled.txt", "docs/qa/tmp/roadpaint_last.txt"]
def files(root):
    if os.path.isfile(root): yield root; return
    for d, _, fs in os.walk(root):
        for f in fs: yield os.path.join(d, f)
def md5(p):
    h = hashlib.md5()
    with open(p, "rb") as f:
        for b in iter(lambda: f.read(1 << 20), b""): h.update(b)
    return h.hexdigest()
cmd = sys.argv[1]
if cmd == "save":
    if os.path.exists(B): shutil.rmtree(B)
    for it in ITEMS:
        s = os.path.join(R, it); d = os.path.join(B, it)
        if not os.path.exists(s): continue
        os.makedirs(os.path.dirname(d), exist_ok=True)
        (shutil.copytree if os.path.isdir(s) else shutil.copy2)(s, d)
    print("saved", sum(1 for it in ITEMS for _ in files(os.path.join(B, it)) if os.path.exists(os.path.join(B, it))))
elif cmd in ("changed", "restore"):
    ch, new = [], []
    for it in ITEMS:
        s = os.path.join(R, it); b = os.path.join(B, it)
        cur = set(os.path.relpath(p, R) for p in files(s)) if os.path.exists(s) else set()
        old = set(os.path.relpath(p, B) for p in files(b)) if os.path.exists(b) else set()
        for rel in sorted(cur | old):
            if rel not in old: new.append(rel)
            elif rel not in cur or md5(os.path.join(R, rel)) != md5(os.path.join(B, rel)): ch.append(rel)
    print("changed", len(ch), "new", len(new))
    for c in ch[:40]: print("  M", c)
    for c in new[:40]: print("  +", c)
    if cmd == "restore":
        for rel in ch: os.makedirs(os.path.dirname(os.path.join(R, rel)), exist_ok=True); shutil.copy2(os.path.join(B, rel), os.path.join(R, rel))
        for rel in new: os.remove(os.path.join(R, rel))
        print("restored")
