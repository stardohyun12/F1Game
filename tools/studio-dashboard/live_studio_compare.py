"""Bounded real-model comparison on disposable code, never on the game checkout."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import hub
import studio
from probe import codex_command

PROMPT='Fix clamp_speed in speed.py: clamp to [0, limit], inclusive. Only edit speed.py. Run python check.py. Do not change tests or unrelated files. Report what passed. Do not delegate.'
BUG='def clamp_speed(value, limit):\n    return min(value, limit)\n'
CHECK='from speed import clamp_speed\nassert clamp_speed(-2,10)==0\nassert clamp_speed(0,10)==0\nassert clamp_speed(5,10)==5\nassert clamp_speed(12,10)==10\nprint("4 assertions passed")\n'
OUT=Path(__file__).resolve().parents[2]/'.ai_logs'/'pitcrew-studio'

def prepare(root):
    root.mkdir(parents=True);(root/'speed.py').write_text(BUG);(root/'check.py').write_text(CHECK)

def verify(root):
    result=subprocess.run([sys.executable,'check.py'],cwd=root,capture_output=True,text=True,timeout=10)
    return dict(passed=result.returncode==0,output=result.stdout.strip(),testUnchanged=(root/'check.py').read_text()==CHECK,
                changedCode=(root/'speed.py').read_text(),files=sorted(p.name for p in root.iterdir() if p.is_file()))

def wait(h,timeout=150):
    deadline=time.time()+timeout
    while time.time()<deadline:
        if not h.runs and not getattr(h,'post_runs',0) and not any(j['status'] in ('queued','running') for j in studio.state(h)['jobs']):return
        time.sleep(.25)
    for r in list(h.runs.values()):r.stop()
    raise TimeoutError('Bounded comparison exceeded 150 seconds')

if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True);rows=[]
    # Intentionally retained for reproducibility, separate from production runtime.
    base=Path(tempfile.mkdtemp(prefix='pitcrew-model-check-'))
    for variant in ['direct-codex','pitcrew-single','pitcrew-implementation-review']:
        root=base/variant;prepare(root);start=time.monotonic();row=dict(variant=variant,model='gpt-6-astra',effort='low',initialSha256=hashlib.sha256(BUG.encode()).hexdigest(),userRestatements=0)
        h=None
        try:
            if variant=='direct-codex':
                result=subprocess.run(codex_command()+['exec','--json','--skip-git-repo-check','-m','gpt-6-astra','-c','model_reasoning_effort="low"','-c','approval_policy="never"','-s','danger-full-access','-C',str(root),PROMPT],capture_output=True,text=True,encoding='utf-8',timeout=150)
                row['exitCode']=result.returncode
                events=[]
                for line in result.stdout.splitlines():
                    try:events.append(json.loads(line))
                    except ValueError:pass
                row['usage']=[e.get('usage') for e in events if e.get('usage')]
                row['errors']=[e for e in events if e.get('type') in ('error','turn.failed')]
            else:
                h=hub.Hub(base/(variant+'-runtime'),root);h.store.put('language','off');h.after_run=lambda *a:None
                studio.setup(h)
                rooms=studio.state(h)['rooms']
                for r in rooms.values():h.store.update(r['agent'],model='gpt-6-astra',effort='low',mode='full')
                studio.submit(h,dict(action='send',room='art',text=PROMPT))
                if variant.endswith('review'):
                    studio.submit(h,dict(action='queue',room='feel',text='Independently verify speed.py clamps to [0, limit]. Run python check.py. Do not edit source or tests. Create verification.txt with observed results, then register it using register_artifact. Do not delegate.'))
                wait(h)
                row['jobs']=[{k:j.get(k) for k in ('room','status','error')} for j in studio.state(h)['jobs']]
                row['reportedModels']=[h.store.agent(r['agent']).get('actualModel') for r in rooms.values()]
                row['usage']=[m['turnUsage'] for r in rooms.values() for m in h.store.messages(r['agent']) if m.get('turnUsage')]
                import deliverables
                row['deliveries']=deliverables.listing(h)
            row['verification']=verify(root)
        except Exception as e:row['error']=str(e)
        finally:
            if h:h.close()
        row['seconds']=round(time.monotonic()-start,2);rows.append(row)
        (OUT/'model-comparison.json').write_text(json.dumps(dict(root=str(base),results=rows,note='One small task; no general model ranking. CLI vs App Server surfaces differ. Costs unavailable unless provider reports them.'),ensure_ascii=False,indent=2),encoding='utf-8')
        print(json.dumps(row,ensure_ascii=False),flush=True)
