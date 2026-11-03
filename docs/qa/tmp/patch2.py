p='tools/world/gen.py'
s=open(p,encoding='utf-8').read()
s=s.replace('// tools/world/gen.py 가 만든 에디터 exec 조각 — 에셋이 있으면 그대로(같은 guid) 덮어쓰고, 없으면 만든다.','// made by tools/world/gen.py: overwrite the asset in place (same guid), create it if missing. ASCII only (argv code page).')
s=s.replace('return "FAIL: Play 중";','return "FAIL: in Play mode";').replace('return "(빈 결과)";','return "(empty)";')
s=s.replace('''capture_output=True, text=True, encoding="utf-8")''','''capture_output=True, text=True, encoding="utf-8",
                       env={**os.environ, "PYTHONUTF8": "1", "PYTHONIOENCODING": "utf-8"})''')
s=s.replace('import json\nimport subprocess','import json\nimport os\nimport subprocess')
open(p,'w',encoding='utf-8').write(s)
