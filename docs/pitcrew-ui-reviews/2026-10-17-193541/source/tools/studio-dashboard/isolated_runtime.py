"""Lifecycle helpers for disposable tests; never signal PIDs from stale files.

The authenticated shutdown endpoint refuses to stop a runtime with active work.
Production and unknown runtime directories are deliberately not adopted.
"""
from contextlib import contextmanager
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import urllib.error
import urllib.request

HERE = Path(__file__).resolve().parent


def stop_isolated_runtime(directory, timeout=10):
    directory = Path(directory).resolve()
    if directory == (HERE / '.runtime').resolve():
        raise ValueError('The production runtime cannot be stopped by test cleanup')
    path = directory / 'runtime.json'
    if not path.exists():
        return
    descriptor = json.loads(path.read_text(encoding='utf-8'))
    production = HERE / '.runtime' / 'runtime.json'
    if production.exists():
        owner = json.loads(production.read_text(encoding='utf-8'))
        if descriptor.get('pid') == owner.get('pid') or descriptor.get('secret') == owner.get('secret'):
            raise ValueError('The descriptor refers to the production runtime')
    def request(route, body=None):
        req = urllib.request.Request(f"http://127.0.0.1:{descriptor['port']}" + route,
            data=body, headers={'X-PitCrew-Runtime': descriptor['secret']})
        with urllib.request.urlopen(req, timeout=2) as response:
            return json.load(response)
    try:
        health = request('/api/hub/runtime-health')
    except urllib.error.HTTPError:
        raise RuntimeError('Runtime identity cannot be verified; cleanup refused')
    except OSError:
        return  # No PID kill fallback: PID may now belong to an unrelated process.
    if health.get('service') != 'pitcrew-runtime' or health.get('pid') != descriptor['pid']:
        raise RuntimeError('Runtime identity changed; cleanup refused')
    deadline = time.monotonic() + timeout
    while True:
        try:
            request('/internal/shutdown', b'{}')
            break
        except urllib.error.HTTPError as error:
            # A released mock streaming request may still be finalizing its turn.
            if error.code != 409 or time.monotonic() >= deadline:
                raise
            error.close()
            time.sleep(.1)
    while time.monotonic() < deadline:
        try:
            request('/api/hub/runtime-health')
        except OSError:
            return
        time.sleep(.1)
    raise RuntimeError('Isolated runtime did not shut down')


@contextmanager
def isolated_runtime(directory, env=None):
    """Own a fresh runtime until the block exits, including assertion failures.

    Stop any test web/watchdog process before leaving this block. Existing
    directories with a descriptor are rejected so another audit is never adopted.
    """
    directory = Path(directory).resolve()
    if directory == (HERE / '.runtime').resolve() or (directory / 'runtime.json').exists():
        raise ValueError('A fresh isolated runtime directory is required')
    directory.mkdir(parents=True, exist_ok=True)
    child_env = {**os.environ, **(env or {}), 'PITCREW_RUNTIME_DIR': str(directory)}
    with (directory / 'runtime.log').open('ab') as log:
        child = subprocess.Popen([sys.executable, '-X', 'utf8', str(HERE / 'runtime.py'), '--serve'],
            env=child_env, stdin=subprocess.DEVNULL, stdout=log, stderr=log,
            creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
        try:
            deadline = time.monotonic() + 15
            while not (directory / 'runtime.json').exists():
                if child.poll() is not None or time.monotonic() >= deadline:
                    raise RuntimeError('Isolated runtime failed to start')
                time.sleep(.1)
            yield child_env
        finally:
            stop_isolated_runtime(directory)
            if child.poll() is None:
                if not (directory / 'runtime.json').exists():
                    # Only the exact Popen we created; never a descriptor PID.
                    child.terminate()
                child.wait(timeout=10)


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description='Stop an idle isolated test runtime by authenticated identity')
    parser.add_argument('directory')
    stop_isolated_runtime(parser.parse_args().directory)
