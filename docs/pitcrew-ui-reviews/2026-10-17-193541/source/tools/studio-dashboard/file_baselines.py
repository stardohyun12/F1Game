"""Selected-file content observations. No Git writes, filters, file copies or restore operations."""
import hashlib
import os
from pathlib import Path, PurePosixPath
import stat
import time

MAX_FILES=20
MAX_FILE_BYTES=32*1024*1024
MAX_TOTAL_BYTES=128*1024*1024
WINDOWS_DEVICES={'CON','PRN','AUX','NUL','CONIN$','CONOUT$'}|{prefix+n for prefix in ('COM','LPT') for n in '123456789¹²³'}

def paths_checked(paths):
    if not isinstance(paths,list) or not 1<=len(paths)<=MAX_FILES:
        raise ValueError('비교할 파일 경로를 1~20개 입력하세요.')
    result=[]
    for value in paths:
        if not isinstance(value,str) or len(value)>500:raise ValueError('파일 경로는 500자 이하로 입력하세요.')
        value=value.strip().replace('\\','/')
        parts=value.split('/')
        if not value or value.startswith('/') or any(x in ('','..','.') or x.lower()=='.git' for x in parts) or ':' in value or any(ord(c)<32 for c in value):
            raise ValueError('프로젝트 폴더 기준의 파일 경로를 입력하세요. 상위 경로와 .git 내부는 제외합니다.')
        if os.name=='nt' and any(x.endswith((' ','.')) or x.split('.')[0].rstrip().upper() in WINDOWS_DEVICES or any(c in x for c in '<>"|?*') for x in parts):
            raise ValueError('Windows 예약 이름, 금지 문자(<>"|?*), 끝에 점·공백이 붙은 경로는 사용할 수 없습니다.')
        name=PurePosixPath(value).as_posix()
        if name.casefold() not in {p.casefold() for p in result}:result.append(name)
    return result

def _path(root,name):
    p=root
    for part in name.split('/'):
        p=p/part
        try:s=p.lstat()
        except FileNotFoundError:continue
        if stat.S_ISLNK(s.st_mode) or getattr(s,'st_file_attributes',0)&getattr(stat,'FILE_ATTRIBUTE_REPARSE_POINT',0x400):
            raise ValueError('링크 또는 연결된 폴더의 파일은 비교하지 않습니다.')
    if not p.resolve().is_relative_to(root):raise ValueError('프로젝트 밖의 파일은 비교하지 않습니다.')
    return p

def _signature(s):return (s.st_size,s.st_mtime_ns,s.st_ino,s.st_dev)

def observe(root,paths):
    paths=paths_checked(paths);root=Path(root).resolve(strict=True)
    started=time.time();deadline=time.monotonic()+5;used=0;entries=[]
    for name in paths:
        entry=dict(path=name,status='unavailable')
        try:
            if time.monotonic()>deadline:raise ValueError('조회 시간 한도에 도달했습니다. 파일 수를 줄여 다시 기록하세요.')
            p=_path(root,name)
            try:before=p.lstat()
            except FileNotFoundError:
                entry['status']='missing';entries.append(entry);continue
            if not stat.S_ISREG(before.st_mode):raise ValueError('일반 파일만 비교할 수 있습니다.')
            if before.st_size>MAX_FILE_BYTES:raise ValueError('파일이 32MB를 넘어 비교하지 못했습니다.')
            digest=hashlib.sha256();size=0
            with p.open('rb') as f:
                opened=os.fstat(f.fileno())
                # On Windows path stat and handle stat can expose different ctime semantics.
                if _signature(before)!=_signature(opened):raise ValueError('읽는 동안 파일이 바뀌었습니다.')
                while chunk:=f.read(1024*1024):
                    size+=len(chunk);used+=len(chunk)
                    if size>MAX_FILE_BYTES or used>MAX_TOTAL_BYTES:raise ValueError('읽기 용량 한도에 도달했습니다.')
                    if time.monotonic()>deadline:raise ValueError('조회 시간 한도에 도달했습니다.')
                    digest.update(chunk)
                ended=os.fstat(f.fileno())
                if _signature(opened)!=_signature(ended) or opened.st_ctime_ns!=ended.st_ctime_ns:raise ValueError('읽는 동안 파일이 바뀌었습니다.')
            if _signature(before)!=_signature(_path(root,name).lstat()):raise ValueError('읽는 동안 파일이 바뀌었습니다.')
            entry.update(status='file',sha256=digest.hexdigest(),size=size)
        except (OSError,ValueError) as e:entry['reason']=str(e)[:200]
        entries.append(entry)
    return dict(startedAt=started,finishedAt=time.time(),entries=entries)

def compare(before,after):
    current={x['path']:x for x in after['entries']};result=[]
    for old in before['entries']:
        new=current.get(old['path'],dict(path=old['path'],status='unavailable'))
        a,b=old['status'],new['status']
        state=('unavailable' if 'unavailable' in (a,b) else 'missing' if a==b=='missing' else 'created' if a=='missing' and b=='file' else
               'deleted' if a=='file' and b=='missing' else 'changed' if a==b=='file' and old['sha256']!=new['sha256'] else 'same')
        result.append(dict(path=old['path'],status=state,before=old,after=new))
    return result
