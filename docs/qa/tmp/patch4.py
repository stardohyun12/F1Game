p='tools/world/gen.py'
s=open(p,encoding='utf-8').read()
a=s.index('def git_bash():'); b=s.index('def main():')
new='''def editor_exec(code):
    """F1 에디터 connector 에 C# 조각을 보낸다(scripts/ucli-http.sh 와 같은 HTTP). 돌려주는 글 또는 'FAIL: …'."""
    port = None
    for f in (Path.home() / ".unity-cli" / "instances").glob("*.json"):
        try:
            d = json.loads(f.read_text(encoding="utf-8"))
        except Exception:
            continue
        if d.get("projectPath", "").replace("\\\\", "/").rstrip("/").lower() == (ROOT / "F1").as_posix().lower() and d.get("state") != "stopped":
            port = d["port"]
            break
    if port is None:
        return "FAIL: F1 에디터 connector 를 못 찾음(에디터가 꺼져 있나?)"
    req = urllib.request.Request(f"http://127.0.0.1:{port}/command", headers={"Content-Type": "application/json"},
                                 data=json.dumps({"command": "exec", "params": {"code": code}}).encode("utf-8"))
    try:
        res = json.loads(urllib.request.urlopen(req, timeout=280).read())
    except Exception as e:
        return f"FAIL: request error {e}"
    return str(res.get("data")) if res.get("success") else "FAIL: " + str(res.get("message"))


'''
s=s[:a]+new+s[b:]
a=s.index('    r = subprocess.run('); b=s.index('\n\nif __name__')
s=s[:a]+'''    out = editor_exec(cs.read_text(encoding="utf-8"))
    print(out)
    return 1 if out.startswith("FAIL") else 0
'''+s[b:]
s=s.replace('import subprocess\n','import urllib.request\n').replace('import os\n','')
open(p,'w',encoding='utf-8').write(s)
