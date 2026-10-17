"""Work domains backed by existing teams/conversations, with a durable serial job queue."""
import time
import uuid

DOMAINS = {'production': ('전체 제작', '프로젝트 방향과 제작 조율'),
           'art': ('아트', '색감·시각 디자인·에셋'),
           'feel': ('조작감', '입력 반응·카메라·플레이 감각')}
TERMINAL = {'done', 'error', 'interrupted', 'cancelled'}


def state(hub):
    return hub.store.get('studio', dict(rooms={}, jobs=[], decisions=[], goal='', revision=0))


def save(hub, value):
    value['revision'] += 1
    hub.store.put('studio', value)


def setup(hub):
    with hub.lock:
        s = state(hub)
        teams = hub.store.get('teams', [])
        for key, (name, role) in DOMAINS.items():
            if key in s['rooms']: continue
            existing = next((t for t in teams if t['name'] == name and t.get('lead')), None)
            if existing:
                aid, tid = existing['lead'], existing['id']
            else:
                a = hub.store.create(name=name + ' 담당', provider='codex', role=role, mode='work',
                    instructions='사용자 원문과 채택된 프로젝트 방향을 따르세요. 제안·실험·검증 결과를 구분하세요. 파일 전달은 register_artifact 도구로 등록하세요.')
                aid, tid = a['id'], uuid.uuid4().hex
                teams.append(dict(id=tid, name=name, members=[aid], lead=aid, context='', rev=1))
            s['rooms'][key] = dict(id=key, name=name, team=tid, agent=aid)
        hub.store.put('teams', teams)
        save(hub, s)
    return snapshot(hub)


def snapshot(hub):
    s = state(hub)
    # A restarted process must never silently rerun an unfinished mutation.
    with hub.lock:
        changed = False
        for job in s['jobs']:
            if job['status'] == 'running' and job['agent'] not in hub.runs and not getattr(hub,'post_runs',0):
                agent_status=hub.store.agent(job['agent'])['status']
                if agent_status=='done':continue  # execute() is handing off to finished()
                job.update(status='interrupted', error='실행 서비스 재시작으로 중단됨. 결과를 확인하고 새 요청을 보내세요.')
                changed = True
        if changed: save(hub, s)
    rooms = []
    for room in s['rooms'].values():
        a = hub.store.agent(room['agent'])
        rooms.append(dict(room, status=a['status'], model=a.get('actualModel') or a.get('model'),
                          effort=a.get('effort'), mode=a['mode'], activity=a.get('currentActivity')))
    import deliverables
    import studio_assets
    import studio_terminal
    return dict(s, rooms=rooms, deliveries=deliverables.listing(hub), integration='not_verified',
                assets=studio_assets.rows(hub),
                terminal=studio_terminal.state(hub),
                testMode=getattr(hub,'studio_test_mode',False),
                approvals=[{k:v for k,v in a.items() if k not in ('event','answer')} for a in list(hub.approvals.values())],
                integrationLabel='통합·플레이 미검증', active=bool(hub.runs),
                limits={'concurrent': 1, 'queue': 10, 'linked': 4, 'seconds': 600})


def submit(hub, data):
    from hub import text
    prompt = text(data.get('text', ''), 30000).strip()
    if not prompt: raise ValueError('요청을 입력하세요.')
    with hub.lock:
        s = state(hub)
        room = s['rooms'].get(data.get('room'))
        import studio_terminal
        if studio_terminal.active(hub):raise ValueError('직접 입력 CLI가 프로젝트를 사용 중입니다. CLI 종료 후 구조화 요청을 보내세요.')
        if not room: raise ValueError('작업실을 먼저 준비하세요.')
        pending = [j for j in s['jobs'] if j['status'] in ('queued', 'running')]
        if len(pending) >= 10: raise ValueError('대기 작업은 최대 10개입니다.')
        if (hub.runs or pending) and data.get('action') != 'queue':
            raise ValueError('진행 중 작업이 있습니다. 다음 작업으로 예약하거나 중단 후 다시 요청하세요.')
        parent = data.get('parent')
        if parent:
            origin = next((j for j in s['jobs'] if j['id'] == parent), None)
            if not origin: raise ValueError('연결할 원본 작업이 없습니다.')
            root = origin.get('root') or parent
            if len([j for j in s['jobs'] if j.get('root') == root]) >= 4:
                raise ValueError('연결 작업은 원본 요청당 최대 4개입니다.')
        else: root = None
        impacts = data.get('impacts', [])
        if not isinstance(impacts, list) or any(k not in s['rooms'] for k in impacts):
            raise ValueError('영향받는 팀이 올바르지 않습니다.')
        job = dict(id=uuid.uuid4().hex, room=room['id'], agent=room['agent'], text=prompt,
                   status='queued', created=time.time(), parent=parent, root=root,
                   impacts=list(dict.fromkeys(impacts)), verification='미검증', integration='not_verified')
        import studio_assets
        _,refs=studio_assets.inputs(hub,data.get('images',[]))
        job['imageInputs']=refs
        s['jobs'].append(job)
        save(hub, s)
        pump(hub)
        return job


def pump(hub):
    with hub.lock:
        import studio_terminal
        if studio_terminal.active(hub): return
        s = state(hub)
        if hub.runs or getattr(hub.owner, 'shutting_down', False): return
        if any(j['status'] == 'running' for j in s['jobs']): return
        job = next((j for j in s['jobs'] if j['status'] == 'queued'), None)
        if not job: return
        job.update(status='running', started=time.time(), decisionRevision=s['revision'])
        save(hub, s)
        try:
            import studio_assets
            from hub import dumps
            files,refs=studio_assets.inputs(hub,job.get('imageInputs',[]))
            prompt=job['text']+ ('\n\n[선택한 실제 이미지 입력 — 버전·용도·정규화 영역]\n'+dumps(refs) if refs else '')
            hub.send(job['agent'], {'text': prompt, 'studioJob': job['id'],'files':files})
        except Exception as exc:
            job.update(status='error', error=str(exc), finished=time.time())
            save(hub, s)  # keep following jobs queued; user explicitly resumes


def finished(hub, job_id, status):
    with hub.lock:
        s = state(hub)
        job = next((j for j in s['jobs'] if j['id'] == job_id), None)
        if job:
            job.update(status=status, finished=time.time())
            messages = hub.store.messages(job['agent'])
            result = next((m for m in reversed(messages) if m.get('run') == job_id and m['role'] == 'assistant'), None)
            if result: job['message'] = result['id']
            root = next((j for j in s['jobs'] if j['id'] == job.get('root')), None)
            if root and root['room'] == 'production' and not job.get('reportedToLead'):
                name = s['rooms'][job['room']]['name']
                excerpt = (result.get('text', '')[:4000] if result else job.get('error', '결과 답변 없음'))
                report = hub.store.message(root['agent'], 'system',
                    f"{name} 팀 결과 보고 · {status}\n\n{excerpt}\n\n팀 실행 상태 보고이며 통합·사용자 플레이 검증은 별개입니다.",
                    sourceJob=job['id'], sourceRoom=job['room'])
                job['reportedToLead'] = report['id']
            save(hub, s)
    if status == 'done': pump(hub)


def report_impact(hub, job_id, data):
    """A run can enqueue one bounded, traceable hand-off to another work domain."""
    from hub import text
    with hub.lock:
        s=state(hub);origin=next((j for j in s['jobs'] if j['id']==job_id and j['status']=='running'),None)
        if not origin:raise ValueError('진행 중인 팀 작업에서만 영향을 보고할 수 있습니다.')
        target=data.get('room');reason=text(data.get('reason',''),2000).strip();task=text(data.get('task',''),10000).strip()
        if not reason or not task:raise ValueError('영향 이유와 연결 작업 내용이 필요합니다.')
        root=origin.get('root') or origin['id']
        visited={j['room'] for j in s['jobs'] if j['id']==root or j.get('root')==root}
        if target in visited:raise ValueError('같은 전달 체인에서 이미 방문한 팀입니다. 순환·중복 호출을 막았습니다.')
        child=submit(hub,dict(action='queue',room=target,parent=origin['id'],text=task+'\n\n영향 근거: '+reason+'\n원문 작업: '+origin['id']))
        s=state(hub);origin=next(j for j in s['jobs'] if j['id']==job_id)
        origin['impacts']=list(dict.fromkeys(origin.get('impacts',[])+[target]))
        origin.setdefault('impactEvents',[]).append(dict(room=target,reason=reason,job=child['id'],created=time.time()))
        save(hub,s)
        return dict(id=child['id'],status='queued',room=target,parent=job_id,reason=reason)


def action(hub, data):
    from hub import text
    kind = data.get('action')
    if kind == 'setup': return setup(hub)
    if kind in ('send', 'queue'): return submit(hub, data)
    if kind == 'resume':
        pump(hub)
        return {'ok': True}
    with hub.lock:
        s = state(hub)
        if kind == 'goal': s['goal'] = text(data.get('text', ''), 10000)
        elif kind == 'verify':
            j=next((j for j in s['jobs'] if j['id']==data.get('id')),None)
            evidence=text(data.get('text',''),5000).strip()
            if not j or j['status'] not in TERMINAL or not evidence:raise ValueError('종료된 작업과 실제 검증 근거가 필요합니다.')
            j.update(verification='사용자 검증 기록 있음',verificationEvidence=evidence,verifiedBy='user',verifiedAt=time.time())
        elif kind in ('propose', 'experiment'):
            room = data.get('room')
            if room not in s['rooms']: raise ValueError('작업실이 없습니다.')
            mid = data.get('message')
            if mid:
                m = hub.store.message_by_id(mid)
                if not m or m['agent'] != s['rooms'][room]['agent']: raise ValueError('원문 대화가 일치하지 않습니다.')
            s['decisions'].append(dict(id=uuid.uuid4().hex, text=text(data.get('text', ''), 5000),
                room=room, message=mid, status='proposal' if kind == 'propose' else 'experiment', created=time.time()))
        elif kind in ('adopt', 'reject'):
            d = next((d for d in s['decisions'] if d['id'] == data.get('id')), None)
            if not d: raise ValueError('결정이 없습니다.')
            d.update(status='adopted' if kind == 'adopt' else 'rejected', decided=time.time())
        elif kind == 'cancel':
            j = next((j for j in s['jobs'] if j['id'] == data.get('id')), None)
            if not j or j['status'] not in ('running', 'queued'): raise ValueError('취소할 작업이 없습니다.')
            if j['status'] == 'running': return hub.stop(j['agent'])
            j.update(status='cancelled', finished=time.time())
        else: raise ValueError('지원하지 않는 작업입니다.')
        save(hub, s)
    return {'ok': True}


def context(hub, aid):
    s = state(hub)
    room = next((r for r in s['rooms'].values() if r['agent'] == aid), None)
    active = next((j for j in s['jobs'] if j['agent']==aid and j['status']=='running'),None)
    related = [j for j in s['jobs'] if room and (room['id']=='production' or room['id'] in j.get('impacts',[]) or (active and active.get('parent')==j['id']))][-8:]
    import deliverables
    deliveries=deliverables.listing(hub)
    references=[]
    for j in related:
        original=hub.store.message_by_id(j.get('message')) if j.get('message') else None
        references.append(dict(id=j['id'],room=j['room'],status=j['status'],agent=j['agent'],message=j.get('message'),
            resultExcerpt=(original.get('text','')[:4000] if original else ''),excerptLimit=4000,
            deliveries=[{k:d[k] for k in ('id','name','url','sha256','status')} for d in deliveries if d['run']==j['id']]))
    import studio_assets
    return dict(goal=s['goal'], adopted=[d for d in s['decisions'] if d['status'] == 'adopted'],
                adoptedAssets=[{k:a.get(k) for k in ('id','version','sha256','name','explanation','run')} for a in studio_assets.rows(hub) if a['kind']=='adopted'],
                local=[d for d in s['decisions'] if room and d['room'] == room['id'] and d['status'] in ('proposal', 'experiment')],
                references=references,
                coordination=('당신은 제작 팀장입니다. 사용자의 실행 요청을 필요한 팀으로 분해하고 report_team_impact 도구로 room=art 또는 feel에 구체적 작업과 reason을 배정하세요. 작은 질문은 직접 답하세요. 모든 팀을 불필요하게 실행하지 마세요. 도구 반환의 queued는 예약이며 완료가 아닙니다. 배정된 작업은 당신의 실행 종료 후 순서대로 실행됩니다. 결과를 기다리며 폴링하지 마세요. 사용자에게 배정 이유와 확인할 결과를 설명하세요.' if room and room['id']=='production' else '자기 팀 작업을 처리하고 다른 팀의 변경이 필요할 때만 report_team_impact로 전달하세요.'),
                rules='제안과 실험은 확정 방향이 아닙니다. 채택된 결정만 공통 방향입니다. 팀 실행 종료는 통합·플레이 검증 통과가 아닙니다.')
