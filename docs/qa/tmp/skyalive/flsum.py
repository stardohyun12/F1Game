import json, pathlib
d = pathlib.Path(__file__).parents[4] / "docs/qa/evidence/sky-alive/flicker"
for f in sorted(d.glob("*_flicker.json")):
    j = json.load(open(f))
    print(f.stem, {k: v for k, v in j.items() if not isinstance(v, (list, dict))})
