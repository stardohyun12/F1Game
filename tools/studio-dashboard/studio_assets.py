"""Versioned image references and selected regions; IDs never accept arbitrary paths."""
import base64
import hashlib
from pathlib import Path
import time
import uuid

KINDS={'reference','candidate','applied','adopted','build','file'}
IMAGE_TYPES={'image/png','image/jpeg','image/webp','image/gif'}

def rows(hub):
    import deliverables
    local=hub.store.get('studio-assets',[])
    meta=hub.store.get('studio-asset-meta',{})
    outputs=[]
    for d in deliverables.listing(hub):
        outputs.append(dict(d,kind='candidate' if d['type'] in IMAGE_TYPES else 'file',source='delivery',version=d['id']))
    return [dict(a,**meta.get(a['id'],{}),src='/studio-image/'+a['id']+'?project='+hub.project_id) for a in local+outputs]

def upload(hub,data):
    if data.get('type') not in IMAGE_TYPES:raise ValueError('PNG·JPEG·WebP·GIF 이미지를 선택하세요.')
    from hub import text
    saved=hub.upload(data)
    content=Path(saved['path']).read_bytes()
    # Decode, not just extension or MIME, before sending to an image model.
    from PIL import Image
    with Image.open(saved['path']) as image:
        actual={'PNG':'image/png','JPEG':'image/jpeg','WEBP':'image/webp','GIF':'image/gif'}.get(image.format)
        if actual!=data['type']:raise ValueError('이미지 형식과 MIME 유형이 다릅니다.')
        image.verify()
    suffix={'image/png':'.png','image/jpeg':'.jpg','image/webp':'.webp','image/gif':'.gif'}[actual]
    target=hub.directory/'image-inputs'/(uuid.uuid4().hex+suffix)
    target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(content)
    saved['path']=str(target)
    aid=uuid.uuid4().hex
    item=dict(saved,id=aid,version=aid,kind='reference',source='upload',room=text(data.get('room','art'),100),
              purpose=text(data.get('purpose',''),2000),created=time.time(),sha256=hashlib.sha256(content).hexdigest())
    with hub.lock:hub.store.put('studio-assets',hub.store.get('studio-assets',[])+[item])
    return next(a for a in rows(hub) if a['id']==aid)

def resolve(hub,identity):
    a=next((a for a in rows(hub) if a['id']==identity),None)
    if not a or a['type'] not in IMAGE_TYPES:raise ValueError('이 프로젝트의 이미지 버전을 찾을 수 없습니다.')
    if a['source']=='delivery':
        import deliverables
        deliverables.download(hub,identity) # hash/status check before model input
        path=hub.directory/'deliverables'/identity
        # Providers detect images by suffix. A derived immutable input copy uses the verified bytes.
        target=hub.directory/'image-inputs'/(identity+{ 'image/png':'.png','image/jpeg':'.jpg','image/webp':'.webp','image/gif':'.gif'}[a['type']])
        target.parent.mkdir(parents=True,exist_ok=True)
        content=path.read_bytes()
        if not target.exists() or hashlib.sha256(target.read_bytes()).hexdigest()!=a['sha256']:target.write_bytes(content)
        path=target
    else:path=Path(a['path'])
    if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=a['sha256']:
        raise ValueError('이미지 원본이 삭제되거나 변경되었습니다. 다시 첨부하세요.')
    return a,path

def image(hub,identity):
    a,path=resolve(hub,identity)
    return dict(type=a['type'],content=base64.b64encode(path.read_bytes()).decode('ascii'))

def inputs(hub,selections):
    from hub import text
    if not isinstance(selections,list) or len(selections)>8:raise ValueError('참고 이미지는 최대 8장입니다.')
    files=[];refs=[]
    for selection in selections:
        a,path=resolve(hub,selection.get('id'))
        region=selection.get('region')
        if region is not None:
            if not isinstance(region,dict) or set(region)!={'x','y','w','h'} or any(type(v) not in (int,float) or not 0<=v<=1 for v in region.values()):raise ValueError('영역 좌표가 올바르지 않습니다.')
            if region['w']<=0 or region['h']<=0 or region['x']+region['w']>1.001 or region['y']+region['h']>1.001:raise ValueError('영역이 이미지 범위를 벗어났습니다.')
        refs.append(dict(id=a['id'],version=a['version'],name=a['name'],sha256=a['sha256'],kind=a['kind'],
                         purpose=text(selection.get('purpose',a.get('purpose','')),2000),region=region))
        files.append(dict(name=a['name'],path=str(path)))
    return files,refs

def update(hub,data):
    from hub import text
    identity=data.get('id');item=next((a for a in rows(hub) if a['id']==identity),None)
    if not item:raise ValueError('산출물을 찾을 수 없습니다.')
    kind=data.get('kind',item['kind'])
    if kind not in KINDS:raise ValueError('지원하지 않는 자료 종류입니다.')
    with hub.lock:
        meta=hub.store.get('studio-asset-meta',{})
        entry=dict(meta.get(identity,{}),kind=kind,explanation=text(data.get('explanation',item.get('explanation','')),5000))
        if kind=='adopted':entry.update(adoptedAt=time.time(),adoptedBy='user')
        if data.get('parent'):
            if data['parent']==identity or not any(a['id']==data['parent'] for a in rows(hub)):raise ValueError('비교 원본을 찾을 수 없습니다.')
            entry['parent']=data['parent']
        meta[identity]=entry;hub.store.put('studio-asset-meta',meta)
    return {'ok':True}
