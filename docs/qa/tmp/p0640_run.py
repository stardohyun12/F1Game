import sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "..", "..", "scripts"))
import fb_geom
print(fb_geom.ucli_exec("D:/Github/F1Game/F1", open(sys.argv[1], encoding="utf-8").read()))
