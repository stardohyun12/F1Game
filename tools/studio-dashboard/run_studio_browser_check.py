"""Own the exact disposable process; run browser assertions on fresh state each time."""
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import urllib.request

HERE=Path(__file__).resolve().parent
with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as folder:
    root=Path(folder)
    child=subprocess.Popen([sys.executable,'-X','utf8',str(HERE/'studio_check_server.py'),'--root',str(root/'project'),
        '--data',str(root/'data'),'--port','7892','--mock'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL,
        creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    try:
        for _ in range(60):
            if child.poll() is not None:raise RuntimeError('Test server failed')
            try:urllib.request.urlopen('http://127.0.0.1:7892/studio',timeout=1).close();break
            except OSError:time.sleep(.1)
        result=subprocess.run(['node',str(HERE/'test_studio_browser.cjs')],env={**os.environ,'STUDIO_URL':'http://127.0.0.1:7892'},timeout=100)
        if result.returncode:raise SystemExit(result.returncode)
    finally:child.terminate();child.wait(timeout=10)
