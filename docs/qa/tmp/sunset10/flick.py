"""Play 앉은 상태: python flick.py tag:v9(true/false):yaw ... → flick.cs 보내고 8초 측정 기다려 요약"""
import subprocess, sys, time, json, pathlib
here = pathlib.Path(__file__).parent
D = here.parents[1] / "evidence/sunset10/play"
for a in sys.argv[1:]:
    tag, v9, yaw = a.split(":")
    subprocess.run([sys.executable, str(here / "run.py"), "flick.cs", f"TAG={tag}", f"V9={v9}", f"YAW={yaw}"])
    time.sleep(12)
    try:
        j = json.load(open(D / f"{tag}_flicker.json", encoding="utf-8"))
        print(tag, {k: j[k] for k in j if not isinstance(j[k], list)}, flush=True)
    except Exception as e:
        print(tag, "no json", e, flush=True)
