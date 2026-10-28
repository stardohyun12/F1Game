import sys
M = 'F1/Assets/Scenes/Map_TownV6_Mats/'
for fn in ['Mesh_TownV6_Terrain_loopfix.asset', 'Mesh_ROAD_camo_1011718592_279.asset']:
    print('==', fn)
    with open(M + fn, encoding='utf-8') as f:
        for k, line in enumerate(f):
            if k > 90: break
            print(line.rstrip()[:130])
try:
    import numpy, scipy
    print(numpy.__version__, scipy.__version__)
except Exception as e:
    print('no numpy/scipy', e)
print(open(M + 'Mesh_TownV6_Terrain_loopfix.asset.meta').read())
