import re, yaml
p = 'world/world.yaml'
s = open(p, encoding='utf-8').read()
s = re.sub(r'role: (.+?),(\s+)(feet|pos|status):', lambda m: 'role: "%s",%s%s:' % (m.group(1), m.group(2), m.group(3)), s)
open(p, 'w', encoding='utf-8').write(s)
d = yaml.safe_load(s)
print({k: len(v) if hasattr(v, '__len__') else v for k, v in d.items()})
print(d['places'][1])
