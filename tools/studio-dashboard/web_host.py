"""WMI-launched web entry point, independent of the invoking terminal's job."""
import argparse
from datetime import datetime, timezone
import faulthandler
import json
import os
from pathlib import Path
import runpy
import sys
import traceback

HERE = Path(__file__).resolve().parent

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--port', type=int, default=7878)
    args = parser.parse_args()
    directory = Path(os.environ.get('PITCREW_RUNTIME_DIR', HERE / '.runtime'))
    directory.mkdir(parents=True, exist_ok=True)
    os.environ['PITCREW_PORT'] = str(args.port)
    (directory / f'web-{args.port}.json').write_text(json.dumps({'pid': os.getpid()}), encoding='utf-8')
    with (directory / 'server.stdout.log').open('a', encoding='utf-8', buffering=1) as out, \
         (directory / 'server.stderr.log').open('a', encoding='utf-8', buffering=1) as err:
        sys.stdout, sys.stderr = out, err
        faulthandler.enable(file=err)
        print(f'{datetime.now(timezone.utc).isoformat()} web started pid={os.getpid()} parent={os.getppid()} port={args.port}', flush=True)
        try:
            runpy.run_path(str(HERE / 'server.py'), run_name='__main__')
        except BaseException:
            traceback.print_exc()
            raise
        finally:
            print(f'{datetime.now(timezone.utc).isoformat()} web exited pid={os.getpid()}', flush=True)
