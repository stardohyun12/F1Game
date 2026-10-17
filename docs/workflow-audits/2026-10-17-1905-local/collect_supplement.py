"""Read-only final evidence: no product imports, restart, game or model execution."""
from pathlib import Path
import collections, datetime as dt, hashlib, json, re, sqlite3, subprocess, urllib.request, urllib.error
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent;HOME=Path.home();EV=OUT/'evidence'
mp=OUT/'evidence_manifest.json';m=json.loads(mp.read_text(encoding='utf-8'))
def clean(s):
    s=str(s).replace(str(HOME),'C:\\Users\\<USER>').replace(str(HOME).replace('\\','/'),'C:/Users/<USER>')
    return s.replace('도현','<USER>')
def add(n,data,src,method,label):
    p=EV/n;p.write_text(clean(data if isinstance(data,str) else json.dumps(data,ensure_ascii=False,indent=2))+'\n',encoding='utf-8')
    m['files']=[e for e in m['files'] if e['file']!='evidence/'+n]
    m['files'].append({'file':'evidence/'+n,'sources':src,'method':method,'claim_type':label,'redacted':True,'redaction':'Names masked; selected code/log/metadata only; no credentials or message bodies',
     'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size,'captured_at_utc':dt.datetime.now(dt.timezone.utc).isoformat()})
def git(*args):return subprocess.run(['git','-c','core.quotepath=false',*args],cwd=ROOT,capture_output=True,encoding='utf-8').stdout.rstrip()

# Correct leading-space handling in porcelain first row. Do not conflate unstaged M with staged M.
d=json.loads((EV/'01-repository.json').read_text(encoding='utf-8'))
lines=git('status','--porcelain','--untracked-files=all').splitlines();lines=[x for x in lines if 'docs/workflow-audits/2026-10-17-1905-local' not in x]
d['status_counts']=dict(collections.Counter(x[:2] for x in lines));d['staged_stat']=git('diff','--cached','--stat')
d['workflow_changes']=[x for x in lines if re.search(r'(\.claude/|scripts/|tools/|project_rules/|docs/gates/|art/_briefs/|art/source/.*\.(json|txt|md)$|F1/(Packages|ProjectSettings)|F1/Assets/(Editor|Scripts)/)',x)]
d['status_refreshed_utc']=dt.datetime.now(dt.timezone.utc).isoformat()
add('01-repository.json',d,['local git metadata'],'Read-only git metadata; preserve porcelain leading spaces','[이번에 재현]')

selections=[('tools/studio-dashboard/hub.py',[(400,413),(1060,1115),(1218,1239),(1377,1404),(1982,2013),(2605,2619)]),
 ('tools/studio-dashboard/watchdog.py',[(13,70)]),('.claude/hooks/session-start.sh',[(24,56)]),
 ('F1/Assets/Editor/CoastalTownSmoke.cs',[(1,55)]),('F1/Assets/Editor/LofiPilotBuilder.cs',[(22,59)]),
 ('tools/asset-gen/lofi_pilot.py',[(1,28),(585,625)]),('F1/Assets/Editor/CoastalTownDensity.cs',[(1,85)]),
 ('.ai_logs/pitcrew-audit/round30-closeout-transition.md',[(1,70)])]
blocks=[];sources=[]
for n,ranges in selections:
 p=ROOT/n
 if not p.exists():continue
 s={'path':n,'ranges':ranges,'baseline_HEAD':git('rev-parse','HEAD'),'git_status':git('status','--porcelain','--',n) or 'clean tracked','sha256':hashlib.sha256(p.read_bytes()).hexdigest()};sources.append(s)
 ls=p.read_text(encoding='utf-8-sig').splitlines();blocks.append(json.dumps(s,ensure_ascii=False)+'\n'+'\n'.join(f'{i+1}: {x}' for i,x in enumerate(ls) if any(a<=i+1<=b for a,b in ranges)))
add('17-supplement-code.txt','\n\n'.join(blocks),sources,'Numbered source excerpts; historical round30 explicitly not treated as current observation','[코드 확인]')

requests=[]
for endpoint in ['/api/hub/runtime-health','/api/hub/runtime-identity']:
 try:
  with urllib.request.urlopen('http://127.0.0.1:7878'+endpoint,timeout=5) as response:
   code=response.status;body=json.loads(response.read())
  # No raw response or headers; allowlisted identity only.
  allowed={k:body.get(k) for k in ['pid','port','started','startedAt','independent','protocol','error','capabilities'] if k in body}
  requests.append({'GET':endpoint,'status':code,'allowlisted_response':allowed,'response_field_names':list(body)})
 except urllib.error.HTTPError as e: requests.append({'GET':endpoint,'status':e.code,'body_not_exported':True})
 except Exception as e:requests.append({'GET':endpoint,'error_type':type(e).__name__})
command="Get-CimInstance Win32_Process | Where-Object { $_.ProcessId -in @(3536,19020,39668) -or $_.Name -eq 'blender.exe' } | Select-Object ProcessId,ParentProcessId,Name,ExecutablePath,CreationDate | ConvertTo-Json -Depth 3"
r=subprocess.run(['powershell','-NoProfile','-Command',command],capture_output=True,encoding='utf-8',errors='replace')
add('18-live-readonly-observation.json',{'requests':requests,'process_query':command,'process_result':json.loads(r.stdout) if r.stdout.strip() else [],'note':'GET only. No bootstrap, usage lookup, restart, editor or agent request. PID/process existence does not prove active work.'},
 ['http://127.0.0.1:7878/api/hub/runtime-health','http://127.0.0.1:7878/api/hub/runtime-identity','Windows Win32_Process'],
 'GET health/identity after source inspection; read-only process inventory','[이번에 재현]')

base=HOME/'.claude/projects/D--Github-F1Game/a3e29587-ae78-41dd-a321-ea4500e45403/subagents';passages=[]
for filename,wanted in [('agent-af517cafd7a1d3e12.jsonl',[69]),('agent-a19b84560ce837b0e.jsonl',[36])]:
 p=base/filename
 for i,l in enumerate(p.open(encoding='utf-8'),1):
  if i not in wanted:continue
  o=json.loads(l)
  for b in o.get('message',{}).get('content',[]):
   if b.get('type')=='tool_result':passages.append({'source':clean(p),'line':i,'timestamp':o.get('timestamp'),'tool_use_id':b.get('tool_use_id'),'content':b.get('content')})
add('19-warmgrade-verifier-results.json',passages,[clean(base)],'Selected actual tool-result records showing missing then present Volume component references','[로그 확인]')

c=sqlite3.connect((ROOT/'tools/studio-dashboard/.runtime/hub.sqlite3').resolve().as_uri()+'?mode=ro',uri=True);c.execute('BEGIN')
cycles=[]
for x in json.loads(c.execute("select body from settings where key='cycles'").fetchone()[0]):
 cycles.append({'id':x['id'],'status':x['status'],'goal_scope':'PitCrew tooling (see 11-dashboard-records.json)','created':x['created'],'ended':x.get('ended'),
  'elapsed_span_seconds':x.get('ended',0)-x['created'] if x.get('ended') else None,
  'stages':[{k:s.get(k) for k in ['kind','round','agent','status','ts','startedAt','finishedAt','retried','retriedAt','delegated','helpers','verdict']} for s in x['stages']]})
turns=collections.Counter();costrecords=collections.Counter();tokenrecords=collections.Counter()
for agent,body in c.execute('select agent,body from messages'):
 x=json.loads(body);u=x.get('turnUsage')
 if not isinstance(u,dict):continue
 turns[agent]+=1
 if u.get('cost') is not None:costrecords[agent]+=1
 if u.get('input') is not None:tokenrecords[agent]+=1
c.rollback();c.close()
add('20-cycle-timestamps.json',{'cycles':cycles,'usage_field_coverage':{'turnUsage_record_counts':dict(turns),'with_cost':dict(costrecords),'with_input_tokens':dict(tokenrecords)},
 'limits':'Elapsed timestamps include waits. Do not interpret as human-free execution or useful game production. Cost not summed across resumed sessions or attributed to game cases.'},
 ['tools/studio-dashboard/.runtime/hub.sqlite3'],'Read transaction, actual stage timestamp field names; usage coverage not billing estimate','[로그 확인]')
mp.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'status_counts':d['status_counts'],'live':requests,'files':len(m['files'])},ensure_ascii=False))
