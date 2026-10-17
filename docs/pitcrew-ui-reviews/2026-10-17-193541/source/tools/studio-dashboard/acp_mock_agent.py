"""Minimal ACP agent for tests: speaks the agentclientprotocol.com v1 JSON-RPC shapes PitCrew uses.
Prompt text "ASK" makes it request permission; "SLOW" streams slowly so cancel can be tested."""
import json, sys, threading, time, uuid

sessions = {}
pending = {}
lock = threading.Lock()

def send(m):
    with lock:
        sys.stdout.write(json.dumps({'jsonrpc': '2.0', **m}) + '\n'); sys.stdout.flush()

def update(sid, u):
    send({'method': 'session/update', 'params': {'sessionId': sid, 'update': u}})

def prompt(rid, p):
    sid = p['sessionId']; blocks = p.get('prompt', [])
    text = ' '.join(b.get('text', '') for b in blocks if b.get('type') == 'text')
    images = sum(1 for b in blocks if b.get('type') == 'image' and b.get('data'))
    sessions[sid]['turns'] += 1; sessions[sid]['cancel'] = False
    update(sid, {'sessionUpdate': 'plan', 'entries': [{'content': '읽기', 'priority': 'high', 'status': 'completed'}, {'content': '답하기', 'priority': 'high', 'status': 'pending'}]})
    update(sid, {'sessionUpdate': 'tool_call', 'toolCallId': 'call_1', 'title': 'Read README', 'kind': 'read', 'status': 'pending', 'locations': [{'path': 'README.md'}]})
    if 'ASK' in text:
        ev = threading.Event(); pending['perm'] = ev
        send({'id': 'perm-1', 'method': 'session/request_permission', 'params': {'sessionId': sid, 'toolCall': {'toolCallId': 'call_2', 'title': 'Write notes.txt', 'kind': 'edit'},
              'options': [{'optionId': 'ok', 'name': 'Allow', 'kind': 'allow_once'}, {'optionId': 'no', 'name': 'Reject', 'kind': 'reject_once'}]}})
        ev.wait(20); decision = pending.get('answer')
        update(sid, {'sessionUpdate': 'agent_message_chunk', 'content': {'type': 'text', 'text': f'PERMISSION:{decision} '}})
    update(sid, {'sessionUpdate': 'tool_call_update', 'toolCallId': 'call_1', 'status': 'completed', 'content': [{'type': 'content', 'content': {'type': 'text', 'text': 'README contents'}}]})
    if 'RETURN_IMAGE' in text:
        update(sid, {'sessionUpdate': 'tool_call_update', 'toolCallId': 'call_1', 'status': 'in_progress', 'content': [
            {'type': 'content', 'content': {'type': 'image', 'mimeType': 'image/png', 'data': 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/58BAwAI/AL+hc2rNAAAAABJRU5ErkJggg=='}},
            {'type': 'content', 'content': {'type': 'text', 'text': 'Game capture'}}]})
        update(sid, {'sessionUpdate': 'tool_call_update', 'toolCallId': 'call_1', 'status': 'completed'})
    for word in (f'TURN:{sessions[sid]["turns"]} ', f'IMAGES:{images} ', 'DONE'):
        if sessions[sid]['cancel']:
            send({'id': rid, 'result': {'stopReason': 'cancelled'}}); return
        update(sid, {'sessionUpdate': 'agent_message_chunk', 'content': {'type': 'text', 'text': word}})
        time.sleep(1.5 if 'SLOW' in text else 0.05)
    send({'id': rid, 'result': {'stopReason': 'end_turn'}})

for line in sys.stdin:
    m = json.loads(line); method = m.get('method'); p = m.get('params', {})
    if method is None and m.get('id') == 'perm-1':
        pending['answer'] = m.get('result', {}).get('outcome', {}).get('optionId', 'cancelled'); pending['perm'].set(); continue
    if method == 'initialize':
        send({'id': m['id'], 'result': {'protocolVersion': 1, 'agentInfo': {'name': 'mock-acp', 'title': 'Mock ACP', 'version': '1'},
              'agentCapabilities': {'loadSession': True, 'promptCapabilities': {'image': True}}, 'authMethods': []}})
    elif method == 'session/new':
        sid = 'sess_' + uuid.uuid4().hex[:8]; sessions[sid] = {'turns': 0, 'cancel': False}; send({'id': m['id'], 'result': {'sessionId': sid}})
    elif method == 'session/load':
        sid = p['sessionId']; sessions.setdefault(sid, {'turns': 0, 'cancel': False})
        # a real agent replays history here; the client must ignore it
        update(sid, {'sessionUpdate': 'agent_message_chunk', 'content': {'type': 'text', 'text': 'REPLAYED_OLD_TEXT'}})
        sessions[sid]['turns'] = int(p.get('_meta', {}).get('turns', 1)); send({'id': m['id'], 'result': {}})
    elif method == 'session/prompt':
        threading.Thread(target=prompt, args=(m['id'], p), daemon=True).start()
    elif method == 'session/cancel':
        sessions.get(p.get('sessionId'), {})['cancel'] = True
