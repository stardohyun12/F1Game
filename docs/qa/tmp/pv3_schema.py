import json
p = 'tools/world/schema.json'
s = json.load(open(p, encoding='utf-8'))
num = {"type": "number"}
v3 = {"type": "array", "items": num, "minItems": 3, "maxItems": 3}
s['properties']['packing'] = {
    "type": "object", "additionalProperties": False,
    "required": ["boxes", "fillers", "cargo_traits", "cushions", "snug_gap_max", "base_loss", "rattle_loss", "rattle_full", "loose_loss", "min_start"],
    "properties": {
        "boxes": {"type": "array", "minItems": 3, "maxItems": 3, "items": {"type": "object", "additionalProperties": False, "required": ["name", "inner"], "properties": {"name": {"type": "string"}, "inner": v3}}},
        "fillers": {"type": "array", "minItems": 2, "items": {"type": "object", "additionalProperties": False, "required": ["name", "kind", "thick", "roll"], "properties": {"name": {"type": "string"}, "kind": {"enum": ["Bubble", "Paper"]}, "thick": num, "roll": {"type": "integer", "minimum": 1}}}},
        "cargo_traits": {"type": "array", "items": {"type": "object", "additionalProperties": False, "required": ["cargo", "trait", "size"], "properties": {"cargo": {"type": "string"}, "trait": {"enum": ["깨짐", "젖음"]}, "size": v3}}},
        "cushions": {"type": "array", "items": {"type": "object", "additionalProperties": False, "required": ["trait", "Bubble", "Paper"], "properties": {"trait": {"enum": ["깨짐", "젖음"]}, "Bubble": num, "Paper": num}}},
        "snug_gap_max": num, "base_loss": num, "rattle_loss": num, "rattle_full": num, "loose_loss": num, "min_start": num}}
if 'packing' not in s['required']:
    s['required'].append('packing')
json.dump(s, open(p, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
print("ok")
