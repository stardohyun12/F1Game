"""terrace3: fb_geom 45c3·66ad 를 돌려 결과를 docs/qa/evidence/terrace3/<tag>/ 에 남긴다.  사용: python -X utf8 geom_run.py geom"""
import pathlib, shutil, subprocess, sys
ROOT = pathlib.Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "scripts")); import fb_geom
tag = sys.argv[1]; out = ROOT / "docs/qa/evidence/terrace3" / tag; out.mkdir(parents=True, exist_ok=True)
for key in ["45c3", "66ad"]:
    r = subprocess.run([sys.executable, "-X", "utf8", "scripts/fb_geom.py", key, "--radius", "40"], cwd=ROOT, capture_output=True, text=True, encoding="utf-8")
    (out / f"fb_geom_{key}.txt").write_text(r.stdout + r.stderr, encoding="utf-8")
    d = fb_geom.find(key); src = ROOT / "tools/viewer/feedback/geom" / f"{d['id']}.json"
    if src.exists(): shutil.copy2(src, out / f"fb_geom_{key}.json")
    print(key, "exit", r.returncode, "|", (r.stdout + r.stderr).strip().splitlines()[-1][:300] if (r.stdout + r.stderr).strip() else "")
