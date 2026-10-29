# 1e: C# 파일을 에디터에서 실행. python -X utf8 ex.py <file.cs.txt> [out.txt]
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "..", "scripts"))
import fb_geom
r = fb_geom.ucli_exec("D:/Github/F1Game/F1", open(sys.argv[1], encoding="utf-8").read())
if len(sys.argv) > 2:
    open(sys.argv[2], "w", encoding="utf-8").write(r)
print(r)
