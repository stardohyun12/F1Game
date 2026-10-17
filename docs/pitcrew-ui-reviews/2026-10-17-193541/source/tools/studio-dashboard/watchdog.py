"""Single local supervisor: recover dead services without replaying AI commands."""
import argparse
import json
import logging
import os
import socket
import subprocess
import sys
import time
from runtime import DIRECTORY, HERE, ensure_running, health


def supervise(port):
    DIRECTORY.mkdir(parents=True, exist_ok=True)
    lock = (DIRECTORY / f'watchdog-{port}.lock').open('a+b')
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
    logging.basicConfig(filename=DIRECTORY / 'watchdog.log', level=logging.INFO,
                        format='%(asctime)s %(levelname)s %(message)s', encoding='utf-8')
    stop = DIRECTORY / f'watchdog-{port}.stop'
    status = DIRECTORY / f'watchdog-{port}.json'
    child = None
    failures = 0
    logging.info('watchdog started pid=%s port=%s', os.getpid(), port)
    try:
        while not stop.exists():
            try:
                previous = health()
                runtime = previous or ensure_running()
                if not previous:
                    logging.warning('runtime recovered pid=%s', runtime['pid'])
                # An occupied port is never killed: slow requests must not abort AI work.
                with socket.socket() as probe:
                    probe.settimeout(1)
                    listening = probe.connect_ex(('127.0.0.1', port)) == 0
                if not listening and (child is None or child.poll() is not None):
                    with (DIRECTORY / 'web-launch.log').open('ab') as log:
                        child = subprocess.Popen([sys.executable, '-X', 'utf8', '-u',
                            str(HERE / 'web_host.py'), '--port', str(port)], cwd=HERE.parent.parent,
                            stdin=subprocess.DEVNULL, stdout=log, stderr=log,
                            creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
                    logging.warning('web launched pid=%s port=%s', child.pid, port)
                    failures = min(failures + 1, 5)
                elif listening:
                    failures = 0
                state = dict(pid=os.getpid(), port=port, checkedAt=time.time(),
                             runtimePid=runtime['pid'], webListening=listening)
                temp = status.with_suffix('.tmp')
                temp.write_text(json.dumps(state), encoding='utf-8'); os.replace(temp, status)
            except Exception:
                failures = min(failures + 1, 5)
                logging.exception('recovery failed; retrying')
            # Bounded restart backoff; no permanent retry limit.
            for _ in range(10 * min(30, max(2, 2 ** failures))):
                if stop.exists():
                    break
                time.sleep(.1)
    finally:
        logging.info('watchdog stopped pid=%s; services left running', os.getpid())
        lock.close()


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=7878)
    supervise(parser.parse_args().port)
