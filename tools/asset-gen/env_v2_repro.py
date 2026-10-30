"""Reproduce v1 unchanged into an isolated review folder, then repeat v2 and compare."""
from pathlib import Path
import hashlib,json,shutil,subprocess,sys
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-30-env-first-v2';E=R/'art/export/env-first-v2'
BL='D:/Tools/Blender/blender.exe';base=V/'_baseline';(base/'export').mkdir(parents=True,exist_ok=True)
for name in ('manifest.json','review-terrain.json'):shutil.copy2(R/'art/export/env-first'/name,base/'export'/name)
def run(cmd,log):
    p=subprocess.run(cmd,cwd=R,capture_output=True,text=True,encoding='utf8',errors='replace');(V/log).write_text(p.stdout+p.stderr,encoding='utf8')
    assert p.returncode==0 and 'Traceback' not in p.stdout+p.stderr,(cmd,log)
run([BL,'-b','-t','6','--factory-startup','--python',str(R/'tools/asset-gen/env_auto_bpy.py'),'--','--baseline-repro','--no-render'],'baseline-repro.log')
run([BL,'-b','--factory-startup','--python',str(R/'tools/asset-gen/env_verify_bpy.py'),'--','--baseline-repro'],'baseline-readback.log')
old=json.loads((R/'art/_review/2026-10-30-env-first/fbx-geometry.json').read_text(encoding='utf8'))
new=json.loads((base/'review/fbx-geometry.json').read_text(encoding='utf8'))
baseline=[dict(name=a['name'],mesh_uv_equal=a['mesh_uv_sha256']==b['mesh_uv_sha256']) for a,b in zip(old,new)]
first=json.loads((V/'fbx-geometry.json').read_text(encoding='utf8'))
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
glb1={p.name:sha(p) for p in E.glob('*.glb')}
run([sys.executable,'-X','utf8',str(R/'tools/asset-gen/env_v2.py'),'repeat'],'repeat-console.log')
run([sys.executable,'-X','utf8',str(R/'tools/asset-gen/env_verify.py'),'--v2'],'repeat-verify-console.log')
second=json.loads((V/'fbx-geometry.json').read_text(encoding='utf8'))
rep=[dict(name=a['name'],mesh_uv_equal=a['mesh_uv_sha256']==b['mesh_uv_sha256'],glb_bytes_equal=glb1[a['name']+'.glb']==sha(E/(a['name']+'.glb'))) for a,b in zip(first,second)]
out=dict(baseline_original_conditions=baseline,v2_repeat=rep,fbx_bytes='Not compared: FBX timestamp metadata varies',original_v1_artifacts='Preserved; baseline exported to isolated _baseline directory',status='PASS' if all(x['mesh_uv_equal'] for x in baseline) and all(x['mesh_uv_equal'] and x['glb_bytes_equal'] for x in rep) else 'FAIL')
out['source_sha256']={str(p.relative_to(R)).replace('\\','/'):sha(p) for p in [R/'tools/asset-gen/env_v2.py',R/'tools/asset-gen/env_auto_bpy.py',R/'tools/asset-gen/env_home_kit.py',R/'tools/asset-gen/env_verify.py',R/'tools/asset-gen/env_verify_bpy.py',R/'tools/asset-gen/env_v2_verify.py']}
(V/'reproducibility.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(out,ensure_ascii=False,indent=1))
raise SystemExit(0 if out['status']=='PASS' else 1)
