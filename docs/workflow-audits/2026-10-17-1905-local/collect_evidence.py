"""Read-only source inspection; writes only this new handoff directory.
Never imports product modules, contacts an editor, or opens credential files.
"""
from pathlib import Path
import collections, datetime as dt, hashlib, json, os, re, sqlite3, subprocess, tomllib

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
EV = OUT / 'evidence'
HOME = Path.home()
NOW = dt.datetime.now(dt.timezone.utc)
entries = []
def clean(s):
    s = str(s).replace(str(HOME), 'C:\\Users\\<USER>').replace(str(HOME).replace('\\','/'), 'C:/Users/<USER>')
    s = re.sub(r'(?i)C:[/\\]+Users[/\\]+(?:rtx40|stard)(?=[/\\])', 'C:/Users/<USER>', s)
    return s.replace('도현', '<USER>')
def run(*args):
    p = subprocess.run(args, cwd=ROOT, capture_output=True, encoding='utf-8', errors='replace')
    return {'exit_code':p.returncode,'stdout':p.stdout.rstrip(),'stderr':p.stderr.strip()}
def git(*args): return run('git','-c','core.quotepath=false',*args)['stdout']
SHA = git('rev-parse','HEAD')
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def source_info(n, ranges=None):
    p = ROOT/n
    return {'path':clean(n),'source_sha256':sha(p),'git_status':git('status','--porcelain','--',n) or 'clean tracked (or ignored; see tracked)',
            'tracked':bool(git('ls-files','--',n)),'baseline_HEAD':SHA,'line_ranges':ranges}
def write(name, obj, sources, method, label):
    text = obj if isinstance(obj,str) else json.dumps(obj,ensure_ascii=False,indent=2)
    p=EV/name;p.write_text(clean(text)+'\n',encoding='utf-8')
    entries.append({'file':'evidence/'+name,'sources':sources,'method':method,'claim_type':label,
                    'redacted':True,'redaction':'home username and personal name masked; selected fields only; no credentials copied',
                    'sha256':sha(p),'bytes':p.stat().st_size,'captured_at_utc':NOW.isoformat()})
def excerpt(name, selections):
    blocks=[];sources=[]
    for n,ranges in selections:
        p=ROOT/n
        if not p.exists(): blocks.append(f'MISSING: {n}');continue
        ls=p.read_text(encoding='utf-8-sig',errors='replace').splitlines()
        info=source_info(n,ranges);sources.append(info)
        blocks.append(json.dumps(info,ensure_ascii=False)+'\n'+'\n'.join(f'{i+1}: {s}' for i,s in enumerate(ls) if any(a<=i+1<=b for a,b in ranges)))
    write(name,'\n\n'.join(blocks),sources,'Numbered source excerpts, no product execution','[코드 확인]')

status=git('status','--porcelain','--untracked-files=all').splitlines()
workflow=[s for s in status if re.search(r'(\.claude/|scripts/|tools/|project_rules/|docs/gates/|art/_briefs/|art/source/.*\.(json|txt|md)$|F1/(Packages|ProjectSettings)|F1/Assets/(Editor|Scripts)/)',s) and 'docs/workflow-audits/2026-10-17-1905-local' not in s]
baseline={'captured_at':NOW.isoformat(),'root':str(ROOT),'remote':git('remote','-v'),'branch':git('branch','--show-current'),
 'HEAD':SHA,'last_commit':git('log','-1','--format=%H%n%cI%n%s'),'provided_remote_snapshot':'964deb7df570ac8485fee0f94bd7bf669b9d2e7b',
 'local_remote_refs':git('for-each-ref','--format=%(refname) %(objectname)','refs/remotes/origin'),
 'cached_upstream_delta':git('rev-list','--left-right','--count','origin/main...HEAD'),
 'commits_above_cached_upstream':git('log','origin/main..HEAD','--format=%H %cI %s'),
 'worktrees':git('worktree','list','--porcelain'),'staged_stat':git('diff','--cached','--stat'),
 'status_counts':dict(collections.Counter(s[:2] for s in status if 'docs/workflow-audits/2026-10-17-1905-local' not in s)),
 'workflow_changes':workflow,'limits':'No fetch; refs/remotes are cached. Audit output excluded from counts. No author names/emails exported.'}
write('01-repository.json',baseline,['git local metadata'],'read-only git commands','[이번에 재현]')
excerpt('02-project-rules.txt',[
 ('CLAUDE.md',[(1,150)]),('F1/ProjectSettings/ProjectVersion.txt',[(1,5)]),
 ('.claude/activeContext.md',[(8,44)]),('project_rules/roles.md',[(1,160)]),
 ('project_rules/session.md',[(1,110)]),('project_rules/no-compromise.md',[(1,150)])])
excerpt('03-regression-and-connection.txt',[(n,[(1,300)]) for n in ['scripts/regression-check.sh','scripts/ucli.sh','scripts/ucli-http.sh']])
excerpt('04-hooks-gates.txt',[(n,[(1,250)]) for n in ['.claude/hooks/stop.sh','.claude/settings.json','.claude/agents/verifier.md','.claude/agents/design-gate.md','.claude/agents/graphic-gate.md']])
excerpt('05-hub-orchestration.txt',[
 ('tools/studio-dashboard/hub.py',[(450,480),(855,881),(930,945),(959,992),(1198,1245),(1291,1404),(1406,1499),(2570,2604),(2640,2667),(2733,2783),(2796,2804),(2828,2840)])])
excerpt('06-runtime-and-docs.txt',[
 ('tools/studio-dashboard/runtime.py',[(21,81),(83,147),(148,176),(225,260)]),
 ('tools/studio-dashboard/README.md',[(1,105)]),('tools/studio-dashboard/start.ps1',[(1,100)])])
excerpt('07-art-pipeline.txt',[
 ('tools/asset-gen/README.md',[(1,120)]),('project_rules/asset.md',[(1,145)]),
 ('art/_briefs/2026-10-13-building-faces.md',[(1,140)]),('art/_briefs/2026-10-13-building-variety.md',[(1,140)]),
 ('art/source/coastal-town/README.md',[(25,54),(127,174)])])
excerpt('08-game-gate-record.txt',[('docs/gates/2026-10-13.md',[(1,60)])])
excerpt('09-codex-task-records.txt',[
 ('.ai_logs/codex-town-density-2026-10-13.log',[(1,30)]),
 ('.ai_logs/codex-building-faces-2026-10-13.log',[(1,15)]),
 ('.ai_logs/codex-building-variety-2026-10-13.log',[(1,15)])])
configs=[]
for p in [HOME/'.codex/config.toml',HOME/'.claude/settings.json',ROOT/'.mcp.json',ROOT/'.claude/settings.local.json']:
    rec={'path':clean(p),'exists':p.exists()}
    if p.exists():
        d=tomllib.loads(p.read_text(encoding='utf-8')) if p.suffix=='.toml' else json.loads(p.read_text(encoding='utf-8-sig'))
        rec['settings']={k:d[k] for k in ['model','model_reasoning_effort','approval_policy','sandbox_mode','effortLevel','disableAllHooks','enabledPlugins'] if k in d}
        rec['permission_default']=d.get('permissions',{}).get('defaultMode')
        rec['env_presence']={k:bool(v) for k,v in d.get('env',{}).items()}
        rec['mcp']=[{'name':k,'command':v.get('command'),'url_configured':bool(v.get('url')),'env_presence':{a:bool(b) for a,b in v.get('env',{}).items()}} for key in ['mcp_servers','mcpServers'] for k,v in d.get(key,{}).items()]
    configs.append(rec)
envnames=['OPENAI_API_KEY','ANTHROPIC_API_KEY','PITCREW_RUNTIME_DIR','CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS','UNITY_CLI_PATH','BLENDER_PATH']
packages=json.loads((ROOT/'F1/Packages/manifest.json').read_text())
write('10-environment-config.json',{'configs':configs,'current_process_env_presence':{k:bool(os.getenv(k)) for k in envnames},'Unity_packages':packages.get('dependencies',{}),
 'harness_roots_checked':{str(p):p.exists() for p in [ROOT/'harness-core',ROOT.parent/'harness-core',HOME/'harness-core']},
 'note':'Current process env is not the full environment of already running agents. OAuth/auth files deliberately not read.'},[clean(p) for p in [HOME/'.codex/config.toml',HOME/'.claude/settings.json']]+['F1/Packages/manifest.json'],'Allowlisted config fields, env names/set status only','[코드 확인]')

# SQLite transaction: aggregate/allowlisted metadata only. Never export message body text, secrets or account-usage.
p=ROOT/'tools/studio-dashboard/.runtime/hub.sqlite3'
c=sqlite3.connect(p.resolve().as_uri()+'?mode=ro',uri=True);c.execute('BEGIN')
agents=[json.loads(r[0]) for r in c.execute('select body from agents')]
aget={a['id']:a for a in agents}
agentfields=['id','name','provider','model','actualModel','effort','mode','status','session','created','updated']
safeagents=[{k:a.get(k) for k in agentfields} for a in agents]
cycles=[]
r=c.execute("select body from settings where key='cycles'").fetchone()
for d in json.loads(r[0]) if r else []:
    fields=['id','goal','mode','status','created','ended','round','untilDone','owner','teams']
    item={k:d.get(k) for k in fields}
    item['stages']=[{k:s.get(k) for k in ['id','kind','agent','status','round','started','ended','verdict','message','error']} for s in d.get('stages',[])]
    cycles.append(item)
msgstats={}; recent=collections.Counter(); kindcounts=collections.Counter()
for agent,body in c.execute('select agent,body from messages'):
    d=json.loads(body);t=d.get('ts',d.get('created'));role=d.get('role');kind=d.get('kind')
    x=msgstats.setdefault(agent,{'count':0,'roles':collections.Counter(),'kinds':collections.Counter(),'min_ts':None,'max_ts':None,'usage_records':0})
    x['count']+=1;x['roles'][str(role)]+=1;x['kinds'][str(kind)]+=1
    if isinstance(t,(int,float)):
        x['min_ts']=min(x['min_ts'] or t,t);x['max_ts']=max(x['max_ts'] or t,t)
        if t>=NOW.timestamp()-48*3600:recent[(agent,str(role))]+=1
    if d.get('usage'):x['usage_records']+=1
c.rollback();c.close()
write('11-dashboard-records.json',{'agents':safeagents,'cycles':cycles,'message_stats':msgstats,
 'last_48h_start_utc':(NOW-dt.timedelta(hours=48)).isoformat(),'last_48h_role_counts':[{'agent':a,'role':r,'count':n} for (a,r),n in recent.items()],
 'limits':'Database status is persisted state, not proof a process is alive; timestamps are UTC epoch seconds; no message bodies exported. Activity span is not autonomous active duration.'},
 ['tools/studio-dashboard/.runtime/hub.sqlite3 (+ live WAL, read transaction)'],'SQLite mode=ro; allowlisted metadata, message aggregates','[로그 확인]')

# Small generated verification artifacts, no models/textures/screenshots copied.
names=['building-faces-geometry.json','building-faces-unity.json','building-variety-geometry.json','building-variety-final-state.json','building-variety-protected-files.json','building-catalog.json','play-verification.txt','validation.txt']
artifactdata=[]
for name in names:
    p=ROOT/'art/source/coastal-town'/name
    if p.exists():
        rec=source_info(str(p.relative_to(ROOT)).replace('\\','/'))
        rec['mtime_utc']=dt.datetime.fromtimestamp(p.stat().st_mtime,dt.timezone.utc).isoformat()
        rec['content']=p.read_text(encoding='utf-8-sig',errors='replace')
        artifactdata.append(rec)
write('12-art-validation-records.json',artifactdata,[x['path'] for x in artifactdata],'Read existing small output files; no rerun; mtime not treated as execution proof','[로그 확인]')

manifest={'investigation_started_utc':NOW.isoformat(),'baseline_HEAD':SHA,'scope':'F1Game current local workflow; no product modifications or model/editor calls',
 'redaction_policy':'No auth files read. No raw conversation dumps. Username/name masked. Config and DB metadata allowlisted. Session/run IDs retained as evidence anchors.',
 'files':entries}
(OUT/'evidence_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'output':str(OUT),'evidence_files':len(entries),'status_counts':baseline['status_counts'],'workflow_changes':len(workflow)},ensure_ascii=False))
