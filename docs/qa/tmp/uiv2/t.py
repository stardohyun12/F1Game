import subprocess, sys
sys.stdout.reconfigure(encoding="utf-8")
for f in ("docs/qa/tmp/feature2/bot_status.cs", "docs/qa/tmp/uiv2/reset.cs", "docs/qa/tmp/uiv2/start_aft.cs"):
    r = subprocess.run(["bash", "scripts/ucli-http.sh", "exec", "@" + f], capture_output=True, text=True, encoding="utf-8")
    print(f, "->", (r.stdout + r.stderr).strip()[:200])
    print(repr(open(f, "rb").read()[:80]))
