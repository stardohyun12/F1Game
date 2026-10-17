from pathlib import Path
import subprocess, hashlib, json, re, urllib.request, urllib.error, datetime, zipfile
ROOT=Path(__file__).resolve().parents[4]
OUT=Path(__file__).resolve().parents[1]
APP=ROOT/'tools/studio-dashboard'
def sha(b): return hashlib.sha256(b).hexdigest()
def git(*args): return subprocess.check_output(['git',*args],cwd=ROOT).decode('utf-8','replace').strip()
def scrub(s):
    s=re.sub(r'(?i)([A-Z]:[\\/]+Users[\\/]+)[^\\/\s"\']+',r'\1USER',s)
    s=re.sub(r'[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}', '[REDACTED_EMAIL]',s)
    return s
def write(name,obj):
    p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True)
    p.write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8')
def status(p):
    rel=p.relative_to(ROOT).as_posix()
    row=git('status','--porcelain','--',rel)
    return 'untracked' if row.startswith('??') else 'modified/staged: '+row[:2] if row else 'tracked HEAD'
head=git('rev-parse','HEAD')
entries=[]
files=[p for p in APP.iterdir() if p.is_file() and (p.suffix in ('.py','.cjs','.ps1','.cmd','.md') or p.name in ('requirements.txt','requirements-lock.txt','providers.example.json'))]
files+=list((APP/'web').glob('*'))
for p in sorted(files):
    if not p.is_file():continue
    raw=p.read_bytes();clean=scrub(raw.decode('utf-8-sig'))
    dest=OUT/'source'/p.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True)
    # Exact bytes unless redaction is required; source remains current disk, not git export.
    data=raw if clean==raw.decode('utf-8-sig') else clean.encode('utf-8')
    dest.write_bytes(data)
    entries.append(dict(path=dest.relative_to(OUT).as_posix(),source=p.relative_to(ROOT).as_posix(),original_sha256=sha(raw),sha256=sha(data),git_status=status(p),redacted=data!=raw))
write('evidence/source-provenance.json',entries)
rows=git('status','--porcelain','--untracked-files=all','--','tools/studio-dashboard').splitlines()
private=[r for r in rows if '/.runtime/' in r or '/.venv/' in r or r.endswith(('activity.jsonl','state.json','running.json'))]
write('evidence/repository.json',dict(captured_at=datetime.datetime.now(datetime.timezone.utc).isoformat(),root=str(ROOT),branch=git('branch','--show-current'),head=head,last_commit=git('log','-1','--format=%cI'),dashboard_changes=[scrub(r) for r in rows if r not in private],excluded_runtime_state_change_count=len(private)))
responses=[]
urls=['/','/api/hub/runtime-health','/api/hub/runtime-identity','/D:/Github/F1Game/docs/workflow-audits/2026-10-17-1905-local/WORKFLOW_HANDOFF_BUNDLE.zip','/local-image?path=D%3A%2FGithub%2FF1Game%2Fdocs%2Fworkflow-audits%2F2026-10-17-1905-local%2FWORKFLOW_HANDOFF_BUNDLE.zip']
urls+=['/'+p.name for p in (APP/'web').glob('*') if p.suffix in ('.js','.css')]
for url in urls:
    try:
        try:r=urllib.request.urlopen('http://127.0.0.1:7878'+url,timeout=8)
        except urllib.error.HTTPError as e:r=e
        with r:
            b=r.read();row=dict(path=url,status=r.status,headers={k:v for k,v in r.headers.items() if k.lower() in ('content-type','content-length','content-disposition','server')},bytes=len(b),sha256=sha(b))
        disk=APP/'web'/('index.html' if url=='/' else url[1:])
        if disk.is_file():row['disk_sha256']=sha(disk.read_bytes());row['disk_match']=row['sha256']==row['disk_sha256']
        if '/api/' in url:
            try:
                obj=json.loads(b)
                row['body_allowlisted']={k:obj[k] for k in ('service','pid','startedAt','independent','error') if k in obj}
            except:pass
        elif row['status']>=400:row['body']=scrub(b.decode('utf-8','replace')[:500])
        responses.append(row)
    except Exception as e:responses.append(dict(path=url,error=type(e).__name__))
write('evidence/http-live.json',responses)
old=ROOT/'docs/workflow-audits/2026-10-17-1905-local'
# Keep the already sanitized handoff + all referenced evidence together; no original sessions or DB.
for rel in ['WORKFLOW_HANDOFF.md','evidence_manifest.json']+[str(p.relative_to(old)) for p in (old/'evidence').glob('*')]:
    p=old/rel;dest=OUT/'workflow_context'/rel;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(p.read_bytes())
bundle=old/'WORKFLOW_HANDOFF_BUNDLE.zip'
with zipfile.ZipFile(bundle) as z:check=dict(path=str(bundle.relative_to(ROOT)),bytes=bundle.stat().st_size,sha256=sha(bundle.read_bytes()),entries=len(z.infolist()),crc_error=z.testzip())
write('evidence/original-bundle.json',check)
print(json.dumps(dict(folder=str(OUT),source_files=len(entries),http_responses=len(responses)),ensure_ascii=False))
