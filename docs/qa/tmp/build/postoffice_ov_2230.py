# 2230: 우체국 겹친 면 검사 두 가지(같은 방향 / 마주 보는 면 포함)를 돌리고 요약. python -X utf8 docs/qa/tmp/build/postoffice_ov_2230.py
import subprocess, json
R = 'D:/Github/F1Game'
for extra in ([], ['both']):
    subprocess.run(['D:/Tools/Blender/blender.exe', '-b', '--factory-startup', '-P', R + '/tools/asset-gen/fix_1846.py', '--', 'postoffice', 'check', *extra], capture_output=True)
    d = json.load(open(R + '/docs/qa/tmp/fix_1846_postoffice_check%s.json' % ('_both' if extra else ''), encoding='utf-8'))['before']
    print('both' if extra else 'same', 'pairs', d['pairs'], 'dups', d['same_island_dups'])
    for x in d['list'][:30]: print('   ', x)
