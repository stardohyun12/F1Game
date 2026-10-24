from pathlib import Path
import csv,json,hashlib
repo=Path(__file__).resolve().parents[3];path=repo/'art/provenance.csv'
with path.open(encoding='utf-8-sig',newline='') as f:r=csv.DictReader(f);fields=r.fieldnames;rows=list(r)
for kind in ['cake','eggs','wine','fish']:
 row=json.loads((repo/f'art/models/cargo_{kind}_v01/provenance-row.json').read_text(encoding='utf-8'))
 actual=hashlib.sha256((repo/row['path']).read_bytes()).hexdigest();assert actual==row['sha256'],kind
 rows=[r for r in rows if r['asset_id']!=row['asset_id']];rows.append(row)
with path.open('w',encoding='utf-8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
print('Four final source records merged; previous records preserved')
