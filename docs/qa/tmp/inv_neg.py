import subprocess, sys
y = 'world/world.yaml'
orig = open(y, encoding='utf-8', newline='').read()
def sh(*a):
    r = subprocess.run(list(a), capture_output=True, text=True, encoding='utf-8')
    return (r.stdout + r.stderr).strip()
GEN = [sys.executable, '-X', 'utf8', 'tools/world/gen.py']
sys.path.insert(0, 'tools/world')
import gen as G
def CHKF(): return G.editor_exec('return WorldInvariants.Check();')
try:
    # 1) DP_tea 를 1.2 m 옮기고 에셋을 다시 만들면 좌표 불변이 FAIL
    bad = orig.replace('pos: [93.8, 4.6, -288.0]', 'pos: [95.0, 4.6, -288.0]')
    assert bad != orig
    open(y, 'w', encoding='utf-8', newline='').write(bad)
    print('gen:', sh(*GEN).splitlines()[-1][:80])
    print('--- 좌표 어긋남'); print(CHKF())
    # 2) yaml 은 되돌렸는데 에셋을 안 다시 만들면 해시 FAIL
    open(y, 'w', encoding='utf-8', newline='').write(orig)
    print('--- 에셋 낡음(yaml 되돌림, 에셋은 그대로)'); print(CHKF())
finally:
    open(y, 'w', encoding='utf-8', newline='').write(orig)
print('gen(복구):', sh(*GEN).splitlines()[-1][:80])
print('--- 복구 뒤'); print(CHKF())
