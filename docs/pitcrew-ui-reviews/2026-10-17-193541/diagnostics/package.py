from pathlib import Path
import json,re,hashlib,subprocess,datetime,zipfile
OUT=Path(__file__).resolve().parents[1];ROOT=OUT.parents[2]
def digest(b):return hashlib.sha256(b).hexdigest()
def dump(p,obj):p.write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8')
def scrub(s):
    s=re.sub(r'(?i)([A-Z]:[\\/]+Users[\\/]+)[^\\/\s"\']+',r'\1USER',s)
    return s.replace('USER','USER').replace('[사용자]','[사용자]')
provenance=json.loads((OUT/'evidence/source-provenance.json').read_text(encoding='utf-8'))
changes=[]
for row in provenance:
    if digest((ROOT/row['source']).read_bytes())!=row['original_sha256']:changes.append(row['source'])
texts={'.md','.txt','.json','.py','.cjs','.js','.css','.html','.ps1','.cmd'}
redactions=[]
for p in OUT.rglob('*'):
    if not p.is_file() or p.suffix not in texts or '__pycache__' in p.parts:continue
    old=p.read_text(encoding='utf-8-sig');new=scrub(old)
    if new!=old:p.write_text(new,encoding='utf-8');redactions.append(p.relative_to(OUT).as_posix())
for row in provenance:
    row['sha256']=digest((OUT/row['path']).read_bytes())
    row['redacted']=row['sha256']!=row['original_sha256']
dump(OUT/'evidence/source-provenance.json',provenance)
findings=[]
patterns=[r'sk-(?:proj-|ant-)?[A-Za-z0-9_-]{25,}',r'gh[pousr]_[A-Za-z0-9]{25,}',r'github_pat_[A-Za-z0-9_]{25,}',r'-----BEGIN (?:RSA |OPENSSH |EC )?PRIVATE KEY-----',r'(?i)[A-Z]:[\\/]+Users[\\/]+(?!USER\b)[^\\/\s"\']+']
for p in OUT.rglob('*'):
    if not p.is_file() or p.suffix not in texts:continue
    text=p.read_text(encoding='utf-8-sig')
    if any(re.search(pattern,text) for pattern in patterns):findings.append(p.relative_to(OUT).as_posix())
dump(OUT/'evidence/final-checks.json',dict(completed_at=datetime.datetime.now(datetime.timezone.utc).isoformat(),head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),original_product_sources_compared=len(provenance),original_product_sources_changed_since_capture=changes,export_redactions=redactions,secret_pattern_findings=findings,live_web_pid_after=39668,live_runtime_pid_after=19020,notes=['No production code modified or services restarted','Original live UI screenshot private text masked','UI fixture and diagnostic control server are not production fixes','Video unavailable: ffmpeg missing; failed attempts retained']))
if findings:raise SystemExit('Review redaction scan findings before packaging: '+str(findings))
if changes:raise SystemExit('Source changed during capture; review before packaging: '+str(changes))
by_path={r['path']:r for r in provenance}
files=[p for p in OUT.rglob('*') if p.is_file() and '__pycache__' not in p.parts and p.name not in ('manifest.json','delivery-receipt.json','target-agent.local') and p.suffix not in ('.zip','.pyc')]
items=[]
for p in sorted(files):
    rel=p.relative_to(OUT).as_posix();r=by_path.get(rel,{})
    source=r.get('source')
    if rel.startswith('workflow_context/'):source='docs/workflow-audits/2026-10-17-1905-local/'+rel.split('/',1)[1]
    if rel.startswith('evidence/live_web/'):source='HTTP GET http://127.0.0.1:7878/'+('' if p.name=='index.html' else p.name)
    live_mask=rel in ('evidence/01-live-workspace.png','evidence/03-live-results.png','evidence/04-live-mobile.png')
    if live_mask:source='Current production browser UI http://127.0.0.1:7878/; privacy masks applied before screenshot'
    if not source:source='generated in this audit: '+rel
    items.append(dict(path=rel,source=source,sha256=digest(p.read_bytes()),bytes=p.stat().st_size,original_sha256=r.get('original_sha256'),git_status=r.get('git_status','new audit artifact / earlier local audit copy'),redacted=r.get('redacted',live_mask or rel in redactions or rel.startswith(('workflow_context/','evidence/download-conversation'))),verification='original-byte SHA + exported SHA' if r else 'SHA-256 of included file; provenance/scope in README'))
manifest=dict(schema_version=1,baseline_sha=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),files=items,manifest_self_hash='not embedded; the whole ZIP SHA is recorded externally in delivery-receipt.json',exclusions=[dict(pattern=x,reason=y) for x,y in [('.runtime/, *.sqlite3, full session JSONL','raw conversations and runtime credentials'),('.env, providers.local.json, auth/cookies/global config','credentials and private settings'),('game Assets/Library/Temp, node_modules, .venv, .git','large unrelated/generated/dependency data'),('diagnostics/target-agent.local','local-only conversation selector'),('__pycache__, archive itself, delivery receipt','generated cache / circular archive hash')]])
dump(OUT/'manifest.json',manifest)
archive=OUT/('pitcrew_ui_review_'+datetime.datetime.now().strftime('%Y-%m-%d_%H%M%S')+'.zip')
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as z:
    for p in sorted(files+[OUT/'manifest.json']):z.write(p,p.relative_to(OUT).as_posix())
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    for item in items:assert digest(z.read(item['path']))==item['sha256'],item['path']
    count=len(z.infolist())
receipt=dict(filename=archive.name,bytes=archive.stat().st_size,sha256=digest(archive.read_bytes()),zip_entries=count,crc_test='PASS',manifest_entries_verified=len(items),delivery='Local ZIP ready; browser delivery verification pending',public_upload=False)
dump(OUT/'delivery-receipt.json',receipt)
print(json.dumps(receipt))
