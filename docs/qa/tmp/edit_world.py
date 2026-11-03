import json

p = 'tools/world/schema.json'
raw = open(p, encoding='utf8').read()
s = json.loads(raw)
pk = s['properties']['packing']
mat = {"mass": {"type": "number", "exclusiveMinimum": 0}, "bounce": {"type": "number", "minimum": 0, "maximum": 1}, "friction": {"type": "number", "minimum": 0}}
for k in ('boxes', 'fillers', 'cargo_traits'):
    it = pk['properties'][k]['items']
    it['properties'].update(mat)
    it['required'] = it['required'] + ['mass', 'bounce', 'friction']
pk['properties']['shake_full'] = {"type": "number", "exclusiveMinimum": 0}
pk['required'].append('shake_full')
open(p, 'w', encoding='utf8').write(json.dumps(s, ensure_ascii=False, indent=2) + "\n")

p = 'tools/world/gen.py'
g = open(p, encoding='utf8').read()
a = '"boxes": [{"name": b["name"], "inner": v3(b["inner"])} for b in pk["boxes"]],'
assert a in g
g = g.replace(a, '"boxes": [dict(name=b["name"], inner=v3(b["inner"]), **mat(b)) for b in pk["boxes"]],')
a = '"fillers": [{"name": f["name"], "kind": f["kind"], "thick": f["thick"], "roll": f["roll"]} for f in pk["fillers"]],'
assert a in g
g = g.replace(a, '"fillers": [dict(name=f["name"], kind=f["kind"], thick=f["thick"], roll=f["roll"], **mat(f)) for f in pk["fillers"]],')
a = '"traits": [{"cargo": t["cargo"], "trait": t["trait"], "size": v3(t["size"])} for t in pk["cargo_traits"]],'
assert a in g
g = g.replace(a, '"traits": [dict(cargo=t["cargo"], trait=t["trait"], size=v3(t["size"]), **mat(t)) for t in pk["cargo_traits"]],')
a = '"rattleFull": pk["rattle_full"], "looseLoss": pk["loose_loss"], "minStart": pk["min_start"],'
assert a in g
g = g.replace(a, a + ' "shakeFull": pk["shake_full"],')
g = g.replace("def build_json(text, data):", 'def mat(d):\n    """D-295 물성 세 값(질량·반발·마찰)."""\n    return {"mass": d["mass"], "bounce": d["bounce"], "friction": d["friction"]}\n\n\ndef build_json(text, data):', 1)
open(p, 'w', encoding='utf8').write(g)

p = 'F1/Assets/Scripts/World/WorldOrders.cs'
c = open(p, encoding='utf8').read()
for a, b in [
    ("public class Box { public string name; public Vector3 inner; }", "public class Box { public string name; public Vector3 inner; public float mass = 0.1f, bounce = 0.3f, friction = 0.4f; }"),
    ("public string kind; public float thick; public int roll; }", "public string kind; public float thick; public int roll; public float mass = 0.02f, bounce = 0.2f, friction = 0.7f; }"),
    ("public string trait; public Vector3 size; }", "public string trait; public Vector3 size; public float mass = 0.6f, bounce = 0.4f, friction = 0.8f; }"),
    ("looseLoss = 0.15f, minStart = 0.4f;", "looseLoss = 0.15f, minStart = 0.4f, shakeFull = 0.5f;"),
    ("PackRules.MinStart = p.minStart;", "PackRules.MinStart = p.minStart; PackRules.ShakeFull = p.shakeFull;"),
]:
    assert a in c, a
    c = c.replace(a, b)
open(p, 'w', encoding='utf8').write(c)
print('ok')
