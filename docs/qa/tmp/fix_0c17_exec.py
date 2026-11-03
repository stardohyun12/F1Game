import sys
sys.path.insert(0, "scripts")
import fb_geom
print(fb_geom.ucli_exec("D:/Github/F1Game/F1", open(sys.argv[1], encoding="utf8").read()))
