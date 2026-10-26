import json, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import road_check as RC
d = json.load(open(f'{RC.OUT}/town-v6e.json', encoding='utf-8'))
cs = [{**o, 'fix': {'merge': '합침', 'foot': '사람길로', 'stairs': '계단으로', 'push': '옆으로 밂', 'unresolved': '미해결'}[o['action']] + f" ({o['cut'] or '-'})"}
      for o in d['v6e']['overlaps']]
RC.crop_sheet(['v6b', 'v6e'], cs, os.path.join(RC.OUT, 'v6b-vs-v6e-overlaps.png'), box=80, scale=4)
print(len(cs))
