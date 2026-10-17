"""Opt-in live integration checks, using tiny prompts and a disposable local test file.
Usage: .venv/Scripts/python.exe live_check.py [claude|codex]
Consumes the selected provider's normal quota. Does not start teammates.
"""
import json
import sys
import tempfile
import time
from pathlib import Path
from hub import Hub, ROOT, HERE

def wait(h,aid,approve_path=None,timeout=100):
    deadline=time.time()+timeout
    while aid in h.runs and time.time()<deadline:
        for rid,item in list(h.approvals.items()):
            print('REQUEST',item['kind'],json.dumps(item['detail'],ensure_ascii=False)[:1200],flush=True)
            # Never approve unrelated writes/commands if a provider diverges from the test.
            allow=False;detail=item['detail']
            if approve_path:
                if detail.get('tool')=='Write':
                    inp=detail.get('input',{})
                    allow=Path(inp.get('file_path','')).resolve()==approve_path.resolve() and inp.get('content','').strip()=='VERIFIED'
                elif detail.get('method')=='item/fileChange/requestApproval':
                    proposal=json.loads((detail.get('proposal') or {}).get('content','{}'))
                    changes=proposal.get('changes',[])
                    allow=len(changes)==1 and Path(changes[0]['path']).resolve()==approve_path.resolve() and changes[0].get('diff','').strip()=='VERIFIED'
                elif detail.get('method')=='item/commandExecution/requestApproval':
                    expected=f"[System.IO.File]::WriteAllText('{approve_path}', 'VERIFIED', [System.Text.UTF8Encoding]::new($false))"
                    actions=detail.get('commandActions',[])
                    allow=len(actions)==1 and actions[0].get('command')==expected
            h.decide(rid,{'allow':allow})
        time.sleep(.2)
    if aid in h.runs:
        h.stop(aid);raise RuntimeError('Integration check exceeded deadline')
    a=h.store.agent(aid)
    print('STATE',a['status'],'MODEL',a.get('actualModel'),'ERROR',a.get('error'),flush=True)
    messages=h.store.messages(aid)
    print('LAST',json.dumps([{'role':m['role'],'text':m['text'][:1500]} for m in messages[-3:]],ensure_ascii=False),flush=True)
    if a['status']!='done':raise RuntimeError(a.get('error') or a['status'])
    return messages

def main():
    provider=sys.argv[1]
    with tempfile.TemporaryDirectory(prefix='pitcrew-live-') as tmp:
        h=Hub(tmp)
        try:
            catalog=h.models(provider);default=next((m for m in catalog['models'] if m.get('default')),catalog['models'][0])
            model='sonnet' if provider=='claude' else default['id']
            a=h.save_agent({'name':'Integration check','provider':provider,'model':model,'effort':'low','mode':'read'})
            print('CATALOG',len(catalog['models']),'MODEL',model,flush=True)
            h.send(a['id'],{'text':'Do not use tools. Remember the test word COPPER. Reply READY only.'})
            wait(h,a['id'])
            h.send(a['id'],{'text':'Do not use tools. Which test word did I ask you to remember? Reply only that word.'})
            msgs=wait(h,a['id']);assert 'COPPER' in [m for m in msgs if m['role']=='assistant'][-1]['text']
            scratch=HERE/'.runtime'/f'live-{provider}-check.txt'
            scratch.parent.mkdir(exist_ok=True)
            h.save_agent({'id':a['id'],'mode':'work'})
            h.send(a['id'],{'text':f'Integration test. Write exactly VERIFIED to this single disposable file using a file editing tool: {scratch}. Do not touch any other files. Then reply DONE.'})
            wait(h,a['id'],approve_path=scratch)
            assert scratch.read_text(encoding='utf-8').strip()=='VERIFIED'
            print('PASS: catalog, model/effort, streaming, persisted resume, permission-capable file work',flush=True)
        finally:h.close();h.store.db.close()

if __name__=='__main__':main()
