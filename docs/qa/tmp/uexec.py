"""python -X utf8 docs/qa/tmp/uexec.py <file.cs> — fb_geom.ucli_exec 로 F1 에디터에 C# 파일 실행(bash 경유 0xC0000005 우회)"""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "..", "scripts"))
import fb_geom
sys.stdout.reconfigure(encoding="utf-8")
print(fb_geom.ucli_exec("D:/Github/F1Game/F1", open(sys.argv[1], encoding="utf-8").read()))
