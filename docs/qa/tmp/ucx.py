import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "..", "scripts"))
from fb_geom import ucli_exec
print(ucli_exec("D:/Github/F1Game/F1", open(sys.argv[1], encoding="utf-8").read()))
