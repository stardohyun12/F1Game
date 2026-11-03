import os
p='tools/world/gen.py'
s=open(p,encoding='utf-8').read()
s=s.replace('''    r = subprocess.run(["bash", str(ROOT''','''    r = subprocess.run([git_bash(), str(ROOT''')
s=s.replace('''def main():''','''def git_bash():
    """Windows 에선 PATH 의 bash 가 WSL(System32\\bash.exe)일 수 있다 — Git for Windows 의 bash 를 먼저 찾는다."""
    for c in (os.environ.get("F1_BASH"), r"C:\Program Files\Git\bin\bash.exe", r"C:\Program Files\Git\usr\bin\bash.exe"):
        if c and os.path.exists(c):
            return c
    return "bash"


def main():''',1)
open(p,'w',encoding='utf-8').write(s)
print([c for c in (r"C:\Program Files\Git\bin\bash.exe", r"C:\Program Files\Git\usr\bin\bash.exe") if os.path.exists(c)])
