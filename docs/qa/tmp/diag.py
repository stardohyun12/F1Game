import subprocess, sys, os
sys.path.insert(0, 'tools/world')
import importlib.util
spec = importlib.util.spec_from_file_location('gen', 'tools/world/gen.py'); g = importlib.util.module_from_spec(spec); spec.loader.exec_module(g)
b = g.git_bash(); print('bash =', b)
env = {**os.environ, "PYTHONUTF8": "1", "PYTHONIOENCODING": "utf-8"}
for arg in ['@tools/world/_out/apply.cs', '@D:/Github/F1Game/tools/world/_out/apply.cs']:
    r = subprocess.run([b, 'scripts/ucli-http.sh', 'exec', arg], capture_output=True, text=True, encoding='utf-8', env=env)
    print(arg, '->', (r.stdout + r.stderr).strip()[:200].encode('ascii', 'replace').decode())
