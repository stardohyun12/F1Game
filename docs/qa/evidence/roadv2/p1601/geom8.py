"""fb_geom 8건을 차례로 돌려 PASS/FAIL 줄만 모은다. usage: geom8.py out.txt"""
import subprocess, sys
IDS = ['20261029-032740-45c3', '20261029-230810-268e', '20261029-230823-7da7', '20261029-230848-453e',
       '20261029-230939-4143', '20261029-231007-66ad', '20261030-123628-4100', '20261030-123604-0c17']
out = []
for i in IDS:
    r = subprocess.run([sys.executable, '-X', 'utf8', 'scripts/fb_geom.py', i, '--radius', '40'], capture_output=True, text=True, encoding='utf-8', errors='replace')
    txt = (r.stdout + r.stderr).strip()
    out.append(f'=== {i} rc={r.returncode}\n{txt}')
    last = [l for l in txt.splitlines() if l.strip()][-3:]
    print(i, 'rc', r.returncode, '|', ' / '.join(last)[:400])
open(sys.argv[1], 'w', encoding='utf-8').write('\n'.join(out))
