"""run.py <body.cs> [out] — hdr.cs + body.cs 를 이어 붙여 out(기본 _last_exec.cs)으로 쓴다.
이어서: bash scripts/ucli-http.sh exec @art/_review/2026-11-02-insta/_scripts/<out>
(파이썬 subprocess 의 bash 는 WSL 이라 @경로를 못 읽는다 → 실행은 Bash 도구로.)"""
import sys, pathlib
here = pathlib.Path(__file__).parent
body = pathlib.Path(sys.argv[1])
if not body.is_absolute():
    body = here / body
out = sys.argv[2] if len(sys.argv) > 2 else "_last_exec.cs"
src = (here / "hdr.cs").read_text(encoding="utf-8") + "\n" + body.read_text(encoding="utf-8")
(here / out).write_text(src, encoding="utf-8")
print("ok", out, len(src))
