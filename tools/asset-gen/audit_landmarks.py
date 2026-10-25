"""Merge source ledgers and audit task0155 exported assets. No engine mutations."""
from pathlib import Path
import json,csv,hashlib,subprocess,sys
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-26-landmarks'
FOLDERS={'harbor':'bayloop_harbor_landmarks_v01','chapel-market':'bayloop_chapel_market_v01','teahouse':'bayloop_teahouse_v01','deferred':'bayloop_deferred_v02'}
ledger=R/'art/provenance.csv'
with ledger.open(encoding='utf-8-sig',newline='') as f:rd=csv.DictReader(f);fields=rd.fieldnames;rows=list(rd)
add=[]
for label,folder in FOLDERS.items():
 p=R/'art/models'/folder/'provenance-rows.json'
 if not p.exists():continue
 for item in json.loads(p.read_text(encoding='utf8')):
  item=dict(item);item['sha256']=hashlib.sha256((R/item['path']).read_bytes()).hexdigest()
  item.setdefault('accessed','2026-10-26');item.setdefault('tool','bpy photo atlas pipeline');item.setdefault('notes',item.get('processing',''))
  if item.get('ai_generated') is False:item['ai_generated']='no'
  add.append({k:item.get(k,'') for k in fields})
# Reusing a shared atlas may emit repeated rows; merge by path, never duplicate.
merged={r['path']:r for r in add};rows=[r for r in rows if r['path'] not in merged]+list(merged.values())
with ledger.open('w',encoding='utf8',newline='') as f:w=csv.DictWriter(f,fieldnames=fields);w.writeheader();w.writerows(rows)
results=[]
for label,folder in FOLDERS.items():
 manifest=R/'art/models'/folder/'manifest.json'
 if not manifest.exists():continue
 out=V/label/'checks.json'
 run=subprocess.run([sys.executable,str(R/'tools/asset-gen/check_asset.py'),'--building-manifest',str(manifest),'--out',str(out)],capture_output=True,text=True,encoding='utf8')
 result=json.loads(out.read_text(encoding='utf8'));results.append({'group':label,'passed':result['pass'],'assets':[{'id':a['id'],'pass':a['pass'],'tris':a.get('tris'),'failed':[k for k,v in a.get('checks',{}).items() if not v],'error':a.get('error')} for a in result['assets']]})
(V/'audit-summary.json').write_text(json.dumps(results,indent=2),encoding='utf8');print(json.dumps(results,indent=2))
