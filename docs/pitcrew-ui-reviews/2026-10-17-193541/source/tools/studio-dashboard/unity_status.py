"""Read-only Unity editor status for one PitCrew project root.

snapshot(root) finds Unity projects (root itself and its direct children that hold
ProjectSettings/ProjectVersion.txt), matches each to the unity-cli connector heartbeat in
~/.unity-cli/instances/*.json by exact project path, and, only when that heartbeat is fresh,
asks the editor on 127.0.0.1 one fixed read-only question through POST /command (exec):
Application.dataPath, EditorApplication.isPlaying / isCompiling, active scene path.

Never runs user-supplied code, never captures, never enters Play, never changes project assets; this
module itself writes no file. The connector (com.youngwoocho02.unity-cli-connector 0.3.18) has no /health
route and no read-only state command, so POST /command exec is the only way in. That exec writes a temporary
.cs/.rsp/.dll under the OS temp folder (unity-cli-exec), compiles it with csc, deletes the files, and
Assembly.Load()s the result into the editor, where it stays until the next domain reload. Every command is
also serialized by the connector (CommandRouter SemaphoreSlim(1,1)). So results are cached, concurrent
callers share one probe, and the whole snapshot has a time budget.
"""
import http.client, json, math, os, socket, threading, time
from pathlib import Path

INSTANCES = Path.home() / '.unity-cli' / 'instances'
MAX_CHILDREN = 400          # directory entries scanned under root
MAX_PROJECTS = 12           # Unity projects reported per root
MAX_DESCRIPTORS = 64        # heartbeat files read
MAX_DESCRIPTOR_BYTES = 16 * 1024
MAX_VERSION_BYTES = 4 * 1024
MAX_RESPONSE_BYTES = 64 * 1024
TIMEOUT = 6.0               # seconds per editor socket step, never more than the budget left
BUDGET = 9.0                # whole snapshot, all projects; the web client gives a GET 12 s
MIN_PROBE = 0.5             # less time than this left: do not start a request, report 'unverified'
STALE_AFTER = 30.0          # the connector rewrites its heartbeat every 0.5 s while the editor updates
FUTURE_SKEW = 5.0           # a heartbeat this far ahead of our clock is not trusted
CACHE_TTL = 10.0
# The only code ever sent. A constant: no part of it comes from a request.
PROBE_CODE = ('return new Dictionary<string, object> { { "dataPath", Application.dataPath }, '
              '{ "playing", EditorApplication.isPlaying }, { "compiling", EditorApplication.isCompiling }, '
              '{ "scene", SceneManager.GetActiveScene().path } };')
PROBE_BODY = json.dumps({'command': 'exec', 'params': {'code': PROBE_CODE}}).encode()

_cache = {}                 # root key -> (expires, result)
_locks = {}
_master = threading.Lock()
_probe_slots = threading.BoundedSemaphore(2)   # at most two editor requests at once, process-wide


def _key(path):
    return os.path.normcase(os.path.normpath(str(path))).replace('\\', '/').rstrip('/')


def _inside(path, parent):
    try:
        Path(path).resolve(strict=True).relative_to(Path(parent).resolve(strict=True))
        return True
    except (OSError, ValueError, RuntimeError):
        return False


def _is_link(p):
    try:
        return p.is_symlink() or (hasattr(p, 'is_junction') and p.is_junction())
    except OSError:
        return True


def _version_of(project):
    """m_EditorVersion from ProjectVersion.txt, or None when the file is missing or leads outside the project."""
    settings = project / 'ProjectSettings'
    f = settings / 'ProjectVersion.txt'
    if _is_link(settings) or _is_link(f) or not f.is_file() or not _inside(f, project):
        return None
    try:
        with open(f, 'rb') as fh:
            text = fh.read(MAX_VERSION_BYTES).decode('utf-8', 'replace')
    except OSError:
        return None
    for line in text.splitlines():
        if line.startswith('m_EditorVersion:'):
            return line.split(':', 1)[1].strip()[:40] or ''
    return ''


def find_projects(root):
    """Root and its direct children only. Links (symlink/junction) are skipped, so the scan never leaves root."""
    root = Path(root).resolve()
    found = []
    if root.is_dir():
        v = _version_of(root)
        if v is not None:
            found.append((root, v))
        try:
            with os.scandir(root) as it:
                entries = []
                for i, e in enumerate(it):
                    if i >= MAX_CHILDREN:
                        break
                    entries.append(e)
        except OSError:
            entries = []
        for e in sorted(entries, key=lambda e: e.name.lower()):
            if len(found) >= MAX_PROJECTS:
                break
            p = Path(e.path)
            try:
                if e.is_symlink() or _is_link(p) or not e.is_dir(follow_symlinks=False):
                    continue
            except OSError:
                continue
            if p.resolve().parent != root:
                continue
            v = _version_of(p)
            if v is not None:
                found.append((p, v))
    return found


def _valid_ts(ts, now):
    """Heartbeat time in seconds, or None when it cannot be trusted: not a number (bool too), NaN/Infinity,
    before 2000, or more than FUTURE_SKEW ahead of this clock. A bad time must never read as fresh."""
    if isinstance(ts, bool) or not isinstance(ts, (int, float)) or not math.isfinite(ts):
        return None
    seen = ts / 1000.0
    if seen < 946684800 or seen - now > FUTURE_SKEW:
        return None
    return seen


def _descriptors(now):
    """{normalized projectPath: newest heartbeat dict}. Bounded count and size; unreadable files are ignored.
    A file with a bad timestamp is kept (so the row can say why) but loses to any file with a valid one."""
    out = {}
    try:
        files = sorted(INSTANCES.glob('*.json'))[:MAX_DESCRIPTORS]
    except OSError:
        return out
    rank = lambda d: (d['_seen'] is not None, d['_seen'] or 0.0)
    for f in files:
        try:
            if f.stat().st_size > MAX_DESCRIPTOR_BYTES:
                continue
            d = json.loads(f.read_text(encoding='utf-8'))  # Python's json accepts NaN/Infinity; _valid_ts rejects them
        except (OSError, ValueError, RecursionError):
            continue
        if not isinstance(d, dict) or not isinstance(d.get('projectPath'), str):
            continue
        d['_seen'] = _valid_ts(d.get('timestamp'), now)
        k = _key(d['projectPath'])
        if k not in out or rank(d) > rank(out[k]):
            out[k] = d
    return out


def _ask_editor(port, timeout, deadline):
    return _request_editor(port, timeout, deadline, PROBE_BODY, dict)


def _request_editor(port, timeout, deadline, request_body, data_type, max_response_bytes=MAX_RESPONSE_BYTES,
                    max_error_chars=160):
    """One bounded POST /command on loopback. Returns (data, None) or (None, (status, reason)).
    http.client never follows redirects and never consults proxy settings.
    Socket timeouts restart on every byte, so they cannot bound a server that trickles headers or body.
    A timer therefore shuts the real socket down at `deadline`; any blocked step (connect wait, getresponse,
    read1) then returns at once and the thread gives its probe slot back."""
    conn = http.client.HTTPConnection('127.0.0.1', port, timeout=timeout)
    cut = timeout < TIMEOUT  # the budget, not the editor, set this limit
    aborted = threading.Event()

    held = []  # the real socket, kept apart from conn.sock: for HTTP/1.0 or "Connection: close" answers,
               # getresponse() sets conn.sock = None while the response keeps reading from that same socket

    def abort():
        aborted.set()
        sock = held[0] if held else conn.sock
        if sock is not None:
            # shutdown wakes a blocked recv on Linux; on Windows a recv waiting for bytes that never come only
            # returns when the handle itself is closed. The response keeps its own reference (makefile), so
            # sock.close() would not close it: detach the handle and close that. Later reads fail at once.
            try:
                sock.shutdown(socket.SHUT_RDWR)
            except OSError:
                pass
            try:
                fd = sock.detach()
                if fd >= 0:
                    socket.close(fd)
            except OSError:
                pass

    timer = threading.Timer(max(0.0, deadline - time.monotonic()), abort)
    timer.daemon = True
    timer.start()
    late = (None, ('unverified', f'이번 조회 시간({BUDGET:g}초) 안에 응답이 끝나지 않아 확인하지 못했습니다(포트 {port}).'))
    try:
        conn.connect()
        held.append(conn.sock)
        if aborted.is_set():  # the timer fired while connecting, before there was a socket to shut
            return late
        conn.request('POST', '/command', body=request_body, headers={'Content-Type': 'application/json'})
        r = conn.getresponse()
        if 300 <= r.status < 400:
            return None, ('unverified', f'포트 {port} 가 다른 주소로 넘기려 했습니다(HTTP {r.status}). 따라가지 않았습니다.')
        raw = b''
        while len(raw) <= max_response_bytes:
            if time.monotonic() > deadline:
                raise TimeoutError('budget')
            # read1: whatever has arrived (one recv), not "wait until n bytes" — read(n) would block a drip forever
            chunk = r.read1(min(8192, max_response_bytes + 1 - len(raw)))
            if not chunk:
                break
            raw += chunk
        if aborted.is_set():
            return late
        if len(raw) > max_response_bytes:
            return None, ('unverified', f'포트 {port} 의 응답이 너무 큽니다({max_response_bytes // 1024}KB 제한 초과). 응답을 읽지 못했습니다.')
        if r.status != 200:
            return None, ('unverified', f'포트 {port} 가 HTTP {r.status} 로 답했습니다.')
        try:
            body = json.loads(raw.decode('utf-8'))
        except (ValueError, RecursionError):
            return None, ('unverified', f'포트 {port} 의 응답이 JSON 이 아닙니다.')
        if not isinstance(body, dict) or body.get('success') is not True:
            msg = str(body.get('message', '') if isinstance(body, dict) else '')
            if len(msg) > max_error_chars:
                msg = msg[:max_error_chars] + ' … [잘림]'
            return None, ('unverified', f'에디터가 상태 질의를 거절했습니다: {msg}' if msg else '에디터가 상태 질의를 거절했습니다.')
        data = body.get('data')
        if not isinstance(data, data_type):
            return None, ('unverified', '에디터 응답에 상태 값이 없습니다.')
        if aborted.is_set():  # a cut-off body could still parse; the deadline wins
            return late
        return data, None
    except (TimeoutError, OSError, ValueError, http.client.HTTPException) as e:
        # After the deadline shutdown, http.client may report EOF as RemoteDisconnected/BadStatusLine etc.
        timed_out = aborted.is_set() or isinstance(e, TimeoutError) or 'timed out' in str(e)
        if aborted.is_set() or (timed_out and (cut or str(e) == 'budget')):
            return late
        kind = '시간 안에 응답하지 않았습니다' if timed_out else '연결되지 않았습니다'
        return None, ('offline', f'연결 기록은 있지만 에디터가 {kind}(포트 {port}).')
    finally:
        timer.cancel()
        conn.close()
        for sock in held:
            try:
                sock.close()
            except OSError:
                pass


def _precheck(project, version, desc, now):
    """Everything decidable without the network. Returns (row, port to ask or None)."""
    row = dict(path=str(project).replace('\\', '/'), name=project.name, version=version, status='offline',
               reason='', observedAt=None, scene=None, playing=None, compiling=None)
    if desc is None:
        row['reason'] = '이 프로젝트를 연 Unity 에디터의 연결 기록이 없습니다(unity-cli connector).'
        return row, None
    seen = desc['_seen']
    if seen is None:
        row.update(status='unverified', reason='연결 기록의 시각이 올바르지 않습니다(숫자가 아니거나 미래 시각). 연결로 보지 않습니다.')
        return row, None
    row['observedAt'] = round(seen, 3)
    state = str(desc.get('state', ''))[:40]
    if state == 'stopped':
        row['reason'] = '에디터가 닫혔거나 connector 가 멈췄습니다(마지막 기록: stopped).'
        return row, None
    age = now - seen
    if age > STALE_AFTER:
        row['reason'] = f'마지막 연결 기록이 {_ago(age)} 전입니다({state or "상태 없음"}). 에디터가 닫혔거나 멈춘 것으로 봅니다.'
        return row, None
    port = desc.get('port')
    if not isinstance(port, int) or isinstance(port, bool) or not 0 < port < 65536:
        row.update(status='unverified', reason='연결 기록의 포트 값이 올바르지 않습니다.', observedAt=None)
        return row, None
    return row, port


def _apply(row, project, port, data, err):
    if err:
        row['status'], row['reason'] = err
        if err[0] == 'unverified':
            row['observedAt'] = None
        return
    data_path = data.get('dataPath')
    playing, compiling, scene = data.get('playing'), data.get('compiling'), data.get('scene')
    if not isinstance(data_path, str) or not isinstance(playing, bool) or not isinstance(compiling, bool) or not isinstance(scene, (str, type(None))):
        row.update(status='unverified', reason='에디터 응답의 형식이 예상과 다릅니다.', observedAt=None)
        return
    answered = data_path[:-len('/Assets')] if data_path.endswith('/Assets') else data_path
    if _key(answered) != _key(project):
        row.update(status='unverified', observedAt=None,
                   reason=f'포트 {port} 의 에디터는 다른 프로젝트를 열고 있습니다: {answered[:200]}')
        return
    row.update(status='connected', reason='', observedAt=round(time.time(), 3), playing=playing,
               compiling=compiling, scene=scene or None)


def _probe_job(job, deadline):
    """Runs in a daemon thread: wait for a slot only until the deadline, then ask with the time that is left."""
    late = (None, ('unverified', f'이번 조회 시간({BUDGET:g}초) 안에 차례가 오지 않아 확인하지 못했습니다(포트 {job["port"]}).'))
    left = deadline - time.monotonic()
    if left < MIN_PROBE or not _probe_slots.acquire(timeout=max(0.0, left - MIN_PROBE)):
        job['result'] = late
        return
    try:
        left = deadline - time.monotonic()
        if left < MIN_PROBE:
            job['result'] = late
            return
        job['asking'] = True
        job['result'] = _ask_editor(job['port'], min(TIMEOUT, left), deadline)
    finally:
        _probe_slots.release()


def _ago(seconds):
    s = int(seconds)
    if s < 120:
        return f'{s}초'
    if s < 7200:
        return f'{s // 60}분'
    if s < 172800:
        return f'{s // 3600}시간'
    return f'{s // 86400}일'


def _compute(root):
    """Whole answer within BUDGET seconds: projects that could not be asked in time come back 'unverified'."""
    deadline = time.monotonic() + BUDGET
    projects = find_projects(root)
    if not projects:
        return {'checkedAt': round(time.time(), 3), 'projects': [], 'supported': True}
    now = time.time()
    descs = _descriptors(now)
    rows, jobs = [], []
    for p, v in projects:
        row, port = _precheck(p, v, descs.get(_key(p)), now)
        rows.append(row)
        if port is not None:
            job = dict(row=row, project=p, port=port, result=None, asking=False)
            job['thread'] = threading.Thread(target=_probe_job, args=(job, deadline), daemon=True, name='unity-status-probe')
            jobs.append(job)
    for job in jobs:
        job['thread'].start()
    for job in jobs:
        job['thread'].join(max(0.0, deadline - time.monotonic()))
        result = job['result']
        if result is None:  # still waiting or still reading when the budget ran out; the thread ends on its own timeout
            why = '응답이 끝나지 않아' if job['asking'] else '차례가 오지 않아'
            result = (None, ('unverified', f'이번 조회 시간({BUDGET:g}초) 안에 {why} 확인하지 못했습니다(포트 {job["port"]}).'))
        _apply(job['row'], job['project'], job['port'], *result)
    return {'checkedAt': round(time.time(), 3), 'projects': rows, 'supported': True}


def snapshot(root):
    """Cached per root. Concurrent callers for the same root wait for one computation instead of each probing."""
    k = _key(Path(root).resolve())
    with _master:
        lock = _locks.setdefault(k, threading.Lock())
    with lock:
        hit = _cache.get(k)
        if hit and hit[0] > time.monotonic():
            return hit[1]
        result = _compute(root)
        _cache[k] = (time.monotonic() + CACHE_TTL, result)
        return result


def clear_cache():
    with _master:
        _cache.clear()
