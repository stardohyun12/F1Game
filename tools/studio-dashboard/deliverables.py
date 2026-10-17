"""Explicit, project-scoped immutable deliveries. Never serve user supplied paths."""
import base64
import hashlib
import mimetypes
import os
from pathlib import Path
import time
import uuid

MAX_BYTES = 64 * 1024 * 1024


def opened_path(file, expected):
    """Validate the opened handle too: a junction may change between resolve and open."""
    if os.name == 'nt':
        import ctypes
        import msvcrt
        from ctypes import wintypes
        query=ctypes.windll.kernel32.GetFinalPathNameByHandleW
        query.argtypes=[wintypes.HANDLE,wintypes.LPWSTR,wintypes.DWORD,wintypes.DWORD]
        query.restype=wintypes.DWORD
        buf=ctypes.create_unicode_buffer(32768)
        count=query(msvcrt.get_osfhandle(file.fileno()),buf,len(buf),0)
        if not count or count>=len(buf):raise ValueError('파일의 실제 경로를 확인할 수 없습니다.')
        raw=buf.value
        if raw.startswith('\\\\?\\UNC\\'):raise ValueError('네트워크 파일은 제공하지 않습니다.')
        return Path(raw.removeprefix('\\\\?\\'))
    proc=Path('/proc/self/fd')/str(file.fileno())
    return proc.resolve() if proc.exists() else expected.resolve(strict=True)


def register(hub, data):
    aid = data.get('agent', '')
    hub.store.agent(aid)
    raw = str(data.get('path', ''))
    if not raw or raw.startswith(('\\\\', '//')):
        raise ValueError('프로젝트 안의 일반 파일을 지정하세요.')
    p = Path(os.path.abspath(hub.root / raw))
    if not p.is_relative_to(hub.root):
        raise ValueError('프로젝트 밖 파일은 등록할 수 없습니다.')
    # Reject junctions/symlinks at every level, including links that stay inside.
    for part in [p, *p.parents]:
        if part == hub.root: break
        if part.is_symlink() or (hasattr(part, 'is_junction') and part.is_junction()):
            raise ValueError('링크 파일·폴더는 등록할 수 없습니다.')
    p = p.resolve(strict=True)
    if not p.is_relative_to(hub.root) or not p.is_file():
        raise ValueError('프로젝트 안의 일반 파일만 등록할 수 있습니다.')
    relative = p.relative_to(hub.root)
    if any(x.startswith('.') for x in relative.parts) or ':' in str(relative):
        raise ValueError('숨김 파일·설정·대체 데이터 스트림은 제공하지 않습니다.')
    if p.stat().st_size > MAX_BYTES: raise ValueError('산출물은 64 MiB 이하여야 합니다.')
    with p.open('rb') as src:
        if opened_path(src,p)!=p:
            raise ValueError('등록 중 파일 경로가 변경되었습니다. 다시 등록하세요.')
        before = os.fstat(src.fileno())
        content = src.read(MAX_BYTES + 1)
        after = os.fstat(src.fileno())
    if len(content) > MAX_BYTES or (before.st_size, before.st_mtime_ns) != (after.st_size, after.st_mtime_ns):
        raise ValueError('등록 중 파일이 변경되었습니다. 생성 완료 후 다시 등록하세요.')
    mid = data.get('message')
    if mid:
        message = hub.store.message_by_id(mid)
        if not message or message['agent'] != aid: raise ValueError('다른 대화의 메시지입니다.')
    run = hub.runs.get(aid)
    run_id = getattr(run, 'delivery_run_id', None) or data.get('run')
    if not run_id: raise ValueError('산출물을 생성한 실행 ID가 필요합니다.')
    if not any(m.get('run') == run_id and m['role'] == 'user' for m in hub.store.messages(aid,-1)):
        raise ValueError('이 대화에 기록된 실행 ID만 사용할 수 있습니다.')
    item = dict(id=uuid.uuid4().hex, project=hub.project_id, agent=aid, run=run_id,
                message=mid, name=p.name, size=len(content), type='application/zip' if p.suffix.lower()=='.zip' else mimetypes.guess_type(p.name)[0] or 'application/octet-stream',
                created=time.time(), sha256=hashlib.sha256(content).hexdigest(), status='available',
                verification='미검증', policy='immutable-copy')
    item['url'] = '/deliverables/' + item['id'] + '?project=' + hub.project_id
    directory = hub.directory / 'deliverables'
    directory.mkdir(parents=True, exist_ok=True)
    target = directory / item['id']
    with target.open('xb') as out: out.write(content)
    try:
        with hub.lock:
            hub.store.put('deliverables', hub.store.get('deliverables', []) + [item])
    except Exception:
        target.unlink(missing_ok=True)
        raise
    return item


def listing(hub, aid=''):
    rows = []
    for item in hub.store.get('deliverables', []):
        if aid and item['agent'] != aid: continue
        row = dict(item)
        if row['status'] == 'available' and not (hub.directory / 'deliverables' / row['id']).is_file():
            row['status'] = 'missing'
        rows.append(row)
    return rows


def download(hub, identity):
    item = next((x for x in listing(hub) if x['id'] == identity), None)
    if not item: raise ValueError('이 프로젝트에 등록되지 않은 산출물입니다.')
    if item['status'] != 'available': raise ValueError('삭제되거나 만료된 산출물입니다. 담당 팀에 재등록을 요청하세요.')
    content = (hub.directory / 'deliverables' / item['id']).read_bytes()
    if hashlib.sha256(content).hexdigest() != item['sha256']:
        raise ValueError('저장 사본이 변경되었습니다. 담당 팀에 재등록을 요청하세요.')
    return dict(item, content=base64.b64encode(content).decode('ascii'))


def revoke(hub, identity):
    with hub.lock:
        rows = hub.store.get('deliverables', [])
        item = next((x for x in rows if x['id'] == identity), None)
        if not item: raise ValueError('산출물이 없습니다.')
        item['status'] = 'expired'
        hub.store.put('deliverables', rows)
    return {'ok': True}
