from pathlib import Path
import os,sys,tempfile,json,zipfile,hashlib
OUT=Path(__file__).resolve().parents[1]
APP=OUT.parents[2]/'tools/studio-dashboard'
with tempfile.TemporaryDirectory(prefix='pitcrew-artifact-audit-') as temp:
    root=Path(temp);os.environ['PITCREW_RUNTIME_DIR']=str(root/'runtime');os.environ['PITCREW_PROJECT']=str(root)
    os.environ['PITCREW_WINDOWS_NOTIFY']='0'
    sys.path.insert(0,str(APP));import hub
    h=hub.Hub(root/'runtime',root=root)
    try:
        file=root/'검토 자료.zip'
        with zipfile.ZipFile(file,'w') as z:z.writestr('note.txt','diagnostic only')
        a=h.store.create(name='진단 작업자',provider='codex')
        m=h.store.message(a['id'],'assistant',f'[다운로드](</{file.as_posix()}>)')
        artifacts=h.artifacts(a['id'],browse=True)
        result={'scope':'Actual current Hub + Store, temporary project and DB, no model run', 'file_exists':file.is_file(),'file_size':file.stat().st_size,'zip_crc_error':None,'sha256':hashlib.sha256(file.read_bytes()).hexdigest(),'message_contains_zip_link':True,'artifacts':artifacts,'generated_download_url':None,'model_calls':0}
        (OUT/'evidence/artifact-registration-probe.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    finally:h.close();h.store.db.close()
print('Temporary ZIP exists; artifact counts:',artifacts.get('counts'))
