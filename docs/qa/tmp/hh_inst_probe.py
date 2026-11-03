import re
s = open('F1/Assets/Scenes/Map_TownV6.unity', encoding='utf-8').read()
g = 'c620b01e0e2b6d945873512d2b9595d3'
docs = s.split('--- !u!')
inst = [d for d in docs if d.startswith('1001 ') and g in d][0]
print(inst[inst.find('m_AddedComponents'):][:1500])
iid = re.match(r'1001 &(\d+)', inst).group(1)
stripped = []
for d in docs:
    if 'stripped' in d.split('\n')[0] and ('m_PrefabInstance: {fileID: ' + iid + '}') in d:
        fid = re.match(r'\d+ &(\d+)', d).group(1)
        stripped.append((d.split('\n')[0], fid))
print(len(stripped), stripped[:40])
for hd, fid in stripped:
    users = [re.match(r'(\d+ &\d+)', d).group(1) + ' ' + d.split('\n')[1] for d in docs
             if ('fileID: ' + fid + '}') in d and not d.startswith(hd.split(' ')[0] + ' &' + fid)]
    print(fid, users[:6])
