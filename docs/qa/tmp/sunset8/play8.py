"""Play 앉은 상태: python play8.py tag:yaw ... → pcap8.cs 보내고 8초 측정 기다려 요약 출력"""
import subprocess, sys, time, json, pathlib
here = pathlib.Path(__file__).parent
D = here.parents[2] / "qa/evidence/sunset8/play"
for a in sys.argv[1:]:
    tag, yaw = a.split(":")
    subprocess.run([sys.executable, str(here / "run.py"), "pcap8.cs", f"TAG={tag}", f"YAW={yaw}"])
    time.sleep(12)
    try:
        j = json.load(open(D / f"{tag}_flicker.json", encoding="utf-8"))
        print("  ", {k: j[k] for k in j if not isinstance(j[k], list)}, flush=True)
    except Exception as e:
        print("  no json", e)
