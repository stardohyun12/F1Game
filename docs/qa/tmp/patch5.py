p='docs/qa/tmp/inv_neg.py'
s=open(p,encoding='utf-8').read()
s=s.replace("CHK = ['C:/Program Files/Git/bin/bash.exe', 'scripts/world_check.sh', 'neg-test']","sys.path.insert(0, 'tools/world')\nimport gen as G\ndef CHKF(): return G.editor_exec('return WorldInvariants.Check();')")
s=s.replace("sh(*CHK)","CHKF()")
open(p,'w',encoding='utf-8').write(s)
