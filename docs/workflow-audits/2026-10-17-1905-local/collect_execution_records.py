"""Allowlisted execution metadata and isolated, fake-connector regression reproduction."""
from pathlib import Path
import collections, datetime as dt, hashlib, json, os, re, shutil, subprocess, tempfile
ROOT=Path(__file__).resolve().parents[3]; OUT=Path(__file__).resolve().parent; HOME=Path.home()
manifest=json.loads((OUT/'evidence_manifest.json').read_text(encoding='utf-8'))
def clean(s):
    return str(s).replace(str(HOME),'C:\\Users\\<USER>').replace(str(HOME).replace('\\','/'),'C:/Users/<USER>').replace('도현','<USER>')
def add(name,data,sources,method,label='[로그 확인]'):
    p=OUT/'evidence'/name;p.write_text(clean(json.dumps(data,ensure_ascii=False,indent=2))+'\n',encoding='utf-8')
    manifest['files']=[x for x in manifest['files'] if x['file']!='evidence/'+name]
    manifest['files'].append({'file':'evidence/'+name,'sources':sources,'method':method,'claim_type':label,'redacted':True,
      'redaction':'Allowlisted fields only; usernames/names masked; images, hidden reasoning, raw prompts and credential contents omitted',
      'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size,'captured_at_utc':dt.datetime.now(dt.timezone.utc).isoformat()})

caseids=['01a1348e-5347-7141-b79a-36e26758d4a4','01a134cd-6c99-7e10-b5e1-e4cb4f1c7943','01a134e4-dfd8-7a70-950a-c1e0c61981b1']
cases=[]
for sid in caseids:
    ps=list((HOME/'.codex/sessions/2026/10/13').glob('*'+sid+'*'))
    if not ps: cases.append({'id':sid,'record':'not found'});continue
    p=ps[0];times=[];counts=collections.Counter();ctx=[];lastusage=None;outputs=[];calllines={};meta={}
    for i,line in enumerate(p.open(encoding='utf-8'),1):
        o=json.loads(line);q=o.get('payload',{});t=o.get('timestamp');typ=q.get('type','')
        if t:times.append(t)
        counts[(o['type'],typ)]+=1
        if o['type']=='session_meta':meta={k:q.get(k) for k in ['id','timestamp','cwd','cli_version','model_provider']}
        if o['type']=='turn_context':ctx.append({'line':i,**{k:q.get(k) for k in ['model','effort','approval_policy','sandbox_policy']}})
        if typ=='token_count' and q.get('info'):lastusage={'line':i,'value':q['info'].get('total_token_usage')}
        if typ in ('custom_tool_call','function_call'):calllines[q.get('call_id')]={'line':i,'name':q.get('name')}
        if typ!='custom_tool_call_output':continue
        blocks=q.get('output',[])
        if not isinstance(blocks,list):continue
        for b in blocks:
            if b.get('type')!='input_text':continue
            try:r=json.loads(b.get('text',''))
            except (ValueError,TypeError):continue
            if not isinstance(r,dict):continue
            raw=r.get('output','')
            # Export actual command-result passages only, never source listings or image/base64 blocks.
            if len(raw)>6000 or not any(s in raw for s in ['PASS 01_ingame','compiling=False playing=False','checks passed','checks Passed']):continue
            outputs.append({'line':i,'timestamp':t,'call_id':q.get('call_id'),'call':calllines.get(q.get('call_id')),'exit_code':r.get('exit_code'),'output':raw})
    cases.append({'id':sid,'source':clean(p),'record_range':[1,i],'metadata':meta,'first_timestamp':min(times),'last_timestamp':max(times),
                  'context':ctx,'event_counts':[{'record_type':a,'payload_type':b,'count':n} for (a,b),n in counts.items()],
                  'last_cumulative_token_record':lastusage,'result_excerpts':outputs,
                  'limits':'Token totals are provider recorded cumulative tokens including cache; not dollars, not unique-context size. Tool-call counts are outer orchestration calls, not every nested CLI invocation.'})
add('13-codex-session-evidence.json',cases,[x.get('source',x['id']) for x in cases],'Parse selected session IDs; metadata, final token counters, short actual tool-result blocks only')

base=HOME/'.claude/projects/D--Github-F1Game';summaries=[];dispatch=[];subdetails=[];game_main='a3e29587-ae78-41dd-a321-ea4500e45403'
for p in list(base.glob('*.jsonl'))+list((base/game_main/'subagents').glob('*.jsonl')):
    times=[];models=collections.Counter();tools=collections.Counter();ids=set();usercount=0
    for i,line in enumerate(p.open(encoding='utf-8'),1):
        try:o=json.loads(line)
        except ValueError:continue
        if o.get('timestamp'):times.append(o['timestamp'])
        m=o.get('message',{})
        if not isinstance(m,dict):continue
        if m.get('model'):models[m['model']]+=1
        if o.get('type')=='user':usercount+=1
        for b in m.get('content',[]) if isinstance(m.get('content'),list) else []:
            if not isinstance(b,dict) or b.get('type')!='tool_use':continue
            tid=b.get('id')
            if tid in ids:continue
            ids.add(tid);tools[b.get('name','?')]+=1
            z=b.get('input',{})
            if p.stem==game_main and b.get('name') in ('Agent','Task'):
                dispatch.append({'source_line':i,'timestamp':o.get('timestamp'),'tool_use_id':tid,'subagent_type':z.get('subagent_type'),
                  'requested_model':z.get('model'),'description':z.get('description'),'run_in_background':z.get('run_in_background')})
    if not times:continue
    item={'source':clean(p),'first_timestamp':min(times),'last_timestamp':max(times),'models':dict(models),'tool_calls':dict(tools),
          'user_record_count_including_tool_results':usercount,'unique_tool_use_ids':len(ids)}
    (summaries if p.parent==base else subdetails).append(item)
add('14-claude-session-evidence.json',{'main_sessions':summaries,'game_session_id':game_main,'game_dispatch':dispatch,'game_subagents':subdetails,
 'limits':'Session first/last timestamps are log span, not continuous autonomous work. User records include tool results and automated prompts. No cost inferred; partial/replayed messages can repeat usage.'},
 [clean(base)],'Parse JSONL timestamps, tool-use IDs and model names; no conversation bodies or hidden reasoning')

# Do not run original against live editor. Copy byte-for-byte to a fresh temp dir with only a fake sibling connector.
src=ROOT/'scripts/regression-check.sh';original=src.read_bytes();bash=Path('C:/Program Files/Git/bin/bash.exe')
fake='''#!/usr/bin/env bash
case "$2" in *HarnessChecks*)
 case "$AUDIT_SCENARIO" in
  empty_failure) exit 127 ;;
  text_failure) echo 'FAIL: isolated connector unavailable'; exit 1 ;;
  all_skip) echo 'SKIP: isolated fixture'; exit 0 ;;
  pass_nonzero) echo 'PASS: isolated fixture'; exit 9 ;;
  too_few) case "$2" in *CheckCoreServicesPresent*) echo 'PASS: isolated fixture';; esac; exit 0 ;;
 esac ;;
esac
exit 0
'''
results=[]
with tempfile.TemporaryDirectory(prefix='f1-workflow-audit-') as temp:
    d=Path(temp);(d/'regression-check.sh').write_bytes(original)
    (d/'ucli.sh').write_text(fake,encoding='utf-8',newline='\n');(d/'sleep').write_text('#!/bin/sh\nexit 0\n',encoding='utf-8',newline='\n')
    for scenario in ['empty_failure','text_failure','all_skip','pass_nonzero','too_few']:
        env=os.environ.copy();env['AUDIT_SCENARIO']=scenario;env['PATH']=str(d)+os.pathsep+env['PATH']
        r=subprocess.run([str(bash),str(d/'regression-check.sh')],cwd=d,env=env,capture_output=True,encoding='utf-8',errors='replace',timeout=30)
        results.append({'scenario':scenario,'exit_code':r.returncode,'stdout':r.stdout,'stderr':r.stderr})
add('15-regression-isolated-reproduction.json',{'source':'scripts/regression-check.sh','source_sha256':hashlib.sha256(original).hexdigest(),
 'source_unchanged':src.read_bytes()==original,'expected_say_calls':len(re.findall(r'^say ',original.decode(),re.M)),
 'method':'Exact script copy; sibling ucli.sh replaced by fake below, PATH sleep shim returns immediately. No HTTP, Unity, game or model calls. Temporary folder removed.',
 'fake_ucli':fake,'sleep_shim':'#!/bin/sh\nexit 0\n','results':results},['scripts/regression-check.sh'],'Isolated behavior reproduction with explicit substituted dependencies','[이번에 재현]')

env=[]
for command in [[str(HOME/'.local/bin/claude.exe'),'--version'],['powershell','-NoProfile','-Command','codex --version'],
                ['D:/Tools/Blender/blender.exe','--version'],[str(ROOT/'tools/studio-dashboard/.venv/Scripts/python.exe'),'-c','import importlib.metadata as m; print(m.version("claude-agent-sdk"))']]:
    r=subprocess.run(command,capture_output=True,encoding='utf-8',errors='replace',timeout=30)
    env.append({'command':command,'exit_code':r.returncode,'stdout':'\n'.join(r.stdout.splitlines()[:3])})
unity=[]
for p in (HOME/'.unity-cli/instances').glob('*.json'):
    try:d=json.loads(p.read_text(encoding='utf-8-sig'))
    except ValueError:continue
    if str(d.get('projectPath','')).replace('\\','/').lower()!=str(ROOT/'F1').replace('\\','/').lower():continue
    unity.append({'source':clean(p),'descriptor':{k:d.get(k) for k in ['projectPath','unityVersion','state','port','pid']},'note':'Saved descriptor alone is not liveness evidence; no editor request made'})
cp=HOME/'.claude.json';mcp=[]
if cp.exists():
    d=json.loads(cp.read_text(encoding='utf-8'))
    for scope,config in [('global',d),('F1Game',d.get('projects',{}).get('D:/Github/F1Game',{}))]:
        mcp += [{'scope':scope,'name':k,'command':v.get('command'),'url_configured':bool(v.get('url')),'env_presence':{a:bool(b) for a,b in v.get('env',{}).items()}} for k,v in config.get('mcpServers',{}).items()]
packages=[]
for p in (ROOT/'F1/Library/PackageCache').glob('*unity-cli*/package.json'):
    d=json.loads(p.read_text(encoding='utf-8'));packages.append({'path':str(p.relative_to(ROOT)),'name':d.get('name'),'version':d.get('version')})
add('16-installed-tools.json',{'versions':env,'Unity_descriptors':unity,'Unity_connector_packages':packages,'Claude_MCP_allowlist':mcp},
 [clean(cp),'F1/Library/PackageCache/*unity-cli*/package.json'],'Version-only commands; whitelisted saved descriptors/MCP configuration','[이번에 재현]')
(OUT/'evidence_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'files':len(manifest['files']),'cases':len(cases),'claude_sessions':len(summaries),'dispatches':len(dispatch),'reproduction':[(x['scenario'],x['exit_code']) for x in results]},ensure_ascii=False))
