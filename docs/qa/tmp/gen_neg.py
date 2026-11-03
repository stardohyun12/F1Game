import shutil, subprocess, sys
y = 'world/world.yaml'
orig = open(y, encoding='utf-8', newline='').read()
bad = orig.replace('dest: DP_tea}', 'dest: DP_nowhere}').replace('{name: 큰길,', '{name: 집 방,')
try:
    open(y, 'w', encoding='utf-8', newline='').write(bad)
    r = subprocess.run([sys.executable, '-X', 'utf8', 'tools/world/gen.py', '--check'], capture_output=True, text=True, encoding='utf-8')
    print(r.stdout, r.stderr, 'rc=', r.returncode)
finally:
    open(y, 'w', encoding='utf-8', newline='').write(orig)
r = subprocess.run([sys.executable, '-X', 'utf8', 'tools/world/gen.py', '--check'], capture_output=True, text=True, encoding='utf-8')
print(r.stdout, 'rc=', r.returncode)
