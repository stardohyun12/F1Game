import re, collections
A = 'F1/Assets/Art/Library/Buildings/house_home/'
S = 'F1Studio/Assets/Art/Review/house_home/'
def guid(p):
    try: return re.search(r'guid: (\w+)', open(p, encoding='utf-8').read()).group(1)
    except Exception as e: return 'ERR'
for f in ['house_home.fbx', 'house_home.png', 'house_home.prefab', 'M_house_home_glass.mat', 'M_house_home_house_home.mat']:
    print(f, 'F1=', guid(A + f + '.meta'), 'ST=', guid(S + f + '.meta'))
for p in [A + 'house_home.prefab', S + 'house_home.prefab']:
    print(p, collections.Counter(re.findall(r'guid: (\w+)', open(p, encoding='utf-8').read())))
