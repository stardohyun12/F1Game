# 과제 0616: C# 파일을 열린 F1 에디터에서 exec (fb_geom.ucli_exec 재사용). python -X utf8 docs/qa/tmp/p0616_run.py <file.cs>
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "..", "scripts"))
import fb_geom
print(fb_geom.ucli_exec("D:/Github/F1Game/F1", open(sys.argv[1], encoding="utf-8").read()))
