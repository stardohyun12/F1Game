"""Independent local execution service. The web process only forwards requests.

One owner per runtime directory; secret-authenticated loopback HTTP, no command replay.
"""
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys
import time
import logging
import threading
import urllib.error
import urllib.request

HERE = Path(__file__).resolve().parent
DIRECTORY = Path(os.environ.get('PITCREW_RUNTIME_DIR', HERE / '.runtime'))
DESCRIPTOR = DIRECTORY / 'runtime.json'

def descriptor():
    data = json.loads(DESCRIPTOR.read_text(encoding='utf-8'))
    if data.get('protocol') != 1 or not isinstance(data.get('port'), int):
        raise RuntimeError('지원하지 않는 실행 서비스입니다.')
    return data

def forward(path, body=None, token=None, timeout=125):
    d = descriptor()
    headers = {'X-PitCrew-Runtime': d['secret']}
    if body is not None:
        headers.update({'Content-Type': 'application/json', 'X-PitCrew-Token': token or ''})
    req = urllib.request.Request(f"http://127.0.0.1:{d['port']}" + path, data=body, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=timeout) as response:
            return response.status, response.read()
    except urllib.error.HTTPError as error:
        try:
            return error.code, error.read()
        except OSError:
            return error.code, json.dumps({'error': f'실행 서비스가 요청을 거절했습니다 (HTTP {error.code}).'}).encode()
        finally:
            error.close()

def health():
    try:
        status, body = forward('/api/hub/runtime-health', timeout=2)
        data = json.loads(body)
        return data if status == 200 and data.get('service') == 'pitcrew-runtime' else None
    except (OSError, ValueError, KeyError, RuntimeError):
        return None

def ensure_running():
    existing = health()
    if existing:
        return existing
    DIRECTORY.mkdir(parents=True, exist_ok=True)
    with (DIRECTORY / 'runtime.log').open('ab') as log:
        subprocess.Popen([sys.executable, '-X', 'utf8', str(HERE / 'runtime.py'), '--serve'],
            cwd=HERE.parent.parent, stdin=subprocess.DEVNULL, stdout=log, stderr=log,
            creationflags=(getattr(subprocess, 'CREATE_NO_WINDOW', 0) | getattr(subprocess, 'CREATE_NEW_PROCESS_GROUP', 0)),
            start_new_session=os.name != 'nt')
    deadline = time.monotonic() + 15
    while time.monotonic() < deadline:
        existing = health()
        if existing:
            return existing
        time.sleep(.2)
    raise RuntimeError('실행 서비스 시작을 확인하지 못했습니다. .runtime/runtime.log를 확인하세요.')

def stop_running():
    if not health():
        return
    status, body = forward('/internal/shutdown', b'{}', timeout=3)
    if status != 200:
        raise RuntimeError(json.loads(body).get('error', '실행 서비스를 중단하지 못했습니다.'))
    deadline = time.monotonic() + 10
    while time.monotonic() < deadline:
        if not health():
            return
        time.sleep(.1)
    raise RuntimeError('실행 서비스가 아직 종료 중입니다.')

def maintenance_status(hub):
    """Read-only snapshot under the owner's lock, including unopened project stores.

    This is a point-in-time observation, not a reservation to shut down later.
    Unopened stores are read without Hub/Store construction (which performs recovery writes).
    """
    import sqlite3
    owner = hub.owner
    with owner.lock:
        rows = owner.projects()
        known = {r['id'] for r in rows}
        rows += [dict(id=pid, name=h.root.name) for pid,h in owner.project_hubs.items() if pid not in known]
        projects = []
        for row in rows:
            pid = row['id']
            h = owner if pid == 'default' else owner.project_hubs.get(pid)
            item = dict(id=pid, name=row['name'], loaded=h is not None, runs=len(h.runs) if h else 0,
                        postRuns=getattr(h,'post_runs',0) if h else 0, runningGoals=[], heldGoals=[],
                        queuedMessages=0, pendingReplies=0, studioJobs=0, blockers=[])
            cycles, agents, studio_state = [], [], {}
            try:
                if h:
                    cycles = h.store.get('cycles', [])
                    agents = h.store.agents(include_archived=True)
                    studio_state = h.store.get('studio', {})
                else:
                    path = owner.directory / 'projects' / pid / 'hub.sqlite3'
                    if path.exists():
                        db = sqlite3.connect(path.resolve().as_uri()+'?mode=ro',uri=True,timeout=.2)
                        try:
                            db.execute('BEGIN')
                            saved = db.execute("SELECT body FROM settings WHERE key='cycles'").fetchone()
                            cycles = json.loads(saved[0]) if saved else []
                            saved_studio = db.execute("SELECT body FROM settings WHERE key='studio'").fetchone()
                            studio_state = json.loads(saved_studio[0]) if saved_studio else {}
                            agents = [json.loads(r[0]) for r in db.execute('SELECT body FROM agents')]
                        finally: db.close()
                item['runningGoals'] = [c['id'] for c in cycles if c.get('status') == 'running']
                item['studioJobs'] = sum(j.get('status') in ('running','queued') for j in studio_state.get('jobs',[]))
                item['heldGoals'] = [c['id'] for c in cycles if c.get('status') == 'paused' and c.get('heldStage')]
                item['queuedMessages'] = sum(len(a.get('queue') or []) for a in agents)
                item['pendingReplies'] = sum(len(a.get('pendingReplies') or ([a['pendingReply']] if a.get('pendingReply') else [])) for a in agents)
            except (OSError,sqlite3.Error,ValueError,TypeError,KeyError,AttributeError):
                item['blockers'].append('state-unreadable')
            import studio_terminal
            if h and studio_terminal.active(h): item['blockers'].append('nativeCli')
            for field in ('runs','postRuns','runningGoals','queuedMessages','pendingReplies','studioJobs'):
                if item[field]: item['blockers'].append(field)
            projects.append(item)
        pending = getattr(owner,'pending_requests',0)
        closing = getattr(owner,'shutting_down',False)
        blockers = (['pendingRequests'] if pending else []) + (['shuttingDown'] if closing else [])
        return dict(ready=not blockers and not any(p['blockers'] for p in projects),
                    observedAt=time.time(), pendingRequests=pending, shuttingDown=closing,
                    blockers=blockers, projects=projects)


def begin_shutdown(hub):
    """Atomically stop accepting work only after every project's work has settled."""
    hub = hub.owner
    with hub.lock:
        hubs = [hub, *hub.project_hubs.values()]
        # Run.execute transfers runs -> post_runs while holding this same shared lock.
        # after_run may still deliver reports, drain queues, or start the next cycle stage.
        if not maintenance_status(hub)['ready']:
            return False
        for h in hubs:
            h.shutting_down = True
        return True


def serve():
    # Script execution names this module __main__. Later imports by HTTP handlers
    # must reuse it, not load newer disk code into a second runtime module.
    running_module = sys.modules[__name__]
    if sys.modules.setdefault('runtime', running_module) is not running_module:
        raise RuntimeError('실행 서비스 모듈이 이미 다른 사본으로 적재되어 있습니다.')
    DIRECTORY.mkdir(parents=True, exist_ok=True)
    logging.basicConfig(filename=DIRECTORY / 'lifecycle.log', level=logging.INFO,
        format='%(asctime)s %(levelname)s %(message)s', encoding='utf-8')
    logging.info('runtime starting pid=%s parent=%s', os.getpid(), os.getppid())
    # Keep the lock for the process lifetime; a crash releases it at OS level.
    lock = (DIRECTORY / 'runtime.lock').open('a+b')
    if lock.tell() == 0:
        lock.write(b'0'); lock.flush()
    lock.seek(0)
    try:
        if os.name == 'nt':
            import msvcrt
            msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
        else:
            import fcntl
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    except OSError:
        lock.close()
        return
    import server
    from hub import get_hub
    from http.server import ThreadingHTTPServer
    secret = secrets.token_urlsafe(32)
    class RuntimeHandler(server.H):
        def _local(self):
            return super()._local() and secrets.compare_digest(self.headers.get('X-PitCrew-Runtime', ''), secret)
        def do_POST(self):
            if self.path == '/internal/shutdown':
                try:
                    length=int(self.headers.get('Content-Length','0'))
                    if length != 2:return self._json({'error':'잘못된 요청'},400)
                    self.connection.settimeout(3);self.rfile.read(length)
                except (ValueError,OSError):return self._json({'error':'잘못된 요청'},400)
                if not self._local():
                    return self._json({'error': '인증 필요'}, 403)
                # Serialize with send(): no turn may start after shutdown is accepted.
                if not begin_shutdown(hub):
                    return self._json({'error': '작업·후속 처리 또는 저장 요청을 처리 중이어서 실행 서비스를 유지합니다.'}, 409)
                self._json({'ok': True})
                import threading
                threading.Thread(target=service.shutdown, daemon=True).start()
                return
            # Count the entire mutation request, including body reads and response writes.
            # ThreadingHTTPServer uses daemon threads: shutdown otherwise drops uploads.
            with hub.lock:
                closing = getattr(hub, 'shutting_down', False)
                if not closing:
                    hub.pending_requests = getattr(hub, 'pending_requests', 0) + 1
            if closing:
                self.close_connection = True
                return self._json({'error': '실행 서비스가 종료 중입니다. 다시 연결한 뒤 시도하세요.'}, 503)
            try:
                return super().do_POST()
            finally:
                with hub.lock:
                    hub.pending_requests -= 1
    service = ThreadingHTTPServer(('127.0.0.1', 0), RuntimeHandler)
    server.PORT = service.server_port
    hub = get_hub()
    threading.Thread(target=hub.apply_reviewed_repairs, daemon=True).start()  # only a reviewed list, once
    threading.Thread(target=hub.recover_cycles, daemon=True).start()  # stages cut by a restart → paused, resumable
    hub.runtime = dict(service='pitcrew-runtime', protocol=1, independent=True, pid=os.getpid(), startedAt=time.time())
    import runtime_identity
    import hub as hub_module
    import studio, deliverables, delegate_bridge, studio_assets, studio_terminal
    hub.runtime_identity = runtime_identity.capture({'hub.py':hub_module,'server.py':server,
        'runtime.py':sys.modules[__name__],'runtime_identity.py':runtime_identity,
        'studio.py':studio,'deliverables.py':deliverables,'delegate_bridge.py':delegate_bridge,
        'studio_assets.py':studio_assets,'studio_terminal.py':studio_terminal})
    info = {**hub.runtime, 'port': service.server_port, 'secret': secret}
    temp = DESCRIPTOR.with_suffix('.tmp')
    temp.write_text(json.dumps(info), encoding='utf-8'); os.replace(temp, DESCRIPTOR)
    logging.info('runtime ready pid=%s port=%s', os.getpid(), service.server_port)
    stopped = threading.Event()
    def heartbeat():
        while not stopped.wait(60):
            logging.info('runtime alive pid=%s', os.getpid())
    threading.Thread(target=heartbeat, daemon=True).start()
    try:
        service.serve_forever()
    except BaseException:
        logging.exception('runtime failed pid=%s', os.getpid())
        raise
    finally:
        stopped.set()
        logging.info('runtime exiting pid=%s', os.getpid())
        hub.close(); service.server_close(); lock.close()

if __name__ == '__main__':
    if '--serve' in sys.argv:
        os.environ['PITCREW_RUNTIME_SERVICE'] = '1'
        serve()
    else:
        if '--stop' in sys.argv or '--restart' in sys.argv:
            stop_running()
        if '--stop' not in sys.argv:
            result = ensure_running()
            print(f"PitCrew 실행 서비스 준비됨 (PID {result['pid']})")
