"""Read working-tree status, scoped to the registered workspace. Never attributes changes to an agent.

Uses Git's stable porcelain v1 NUL format and --no-optional-locks (no index refresh write).
Repository-configured clean filters may run and maintain their own caches (for example Git LFS).
This is not a guarantee of zero filesystem writes by Git or its configured filters.
Untracked directories are grouped; ignored files and submodule contents are not inspected.
Temporary output files keep process output out of Python memory; execution is limited to 5 seconds.
"""
import os
from pathlib import Path
import subprocess
import tempfile
import threading
import time

TIMEOUT = 5
MAX_BYTES = 256 * 1024
MAX_ENTRIES = 500
_slots = threading.BoundedSemaphore(2)


def snapshot(root):
    result = dict(supported=True, status='unavailable', checkedAt=time.time(), entries=[],
                  limited=False, total=None, pathBase='repository', reason='')
    if not _slots.acquire(timeout=.2):
        result['reason'] = '다른 변경 목록을 확인 중입니다. 잠시 뒤 다시 확인해 주세요.'
        return result
    try:
        root = Path(root).resolve(strict=True)
        # Inherited GIT_DIR/INDEX_FILE/WORK_TREE must not redirect another project's query.
        env = {k: v for k, v in os.environ.items() if not k.upper().startswith('GIT_')}
        env.update(GIT_OPTIONAL_LOCKS='0', GIT_TERMINAL_PROMPT='0', LC_ALL='C')
        command = ['git', '--no-optional-locks', '-c', 'core.fsmonitor=false', '-c', 'core.untrackedCache=false',
                   '-C', str(root), 'status', '--porcelain=v1', '-z', '--no-renames',
                   '--untracked-files=normal', '--ignore-submodules=all', '--', '.']
        with tempfile.TemporaryFile() as output, tempfile.TemporaryFile() as error:
            run = subprocess.run(command, stdout=output, stderr=error, env=env, timeout=TIMEOUT,
                                 creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
            if run.returncode:
                error.seek(0)
                detail = error.read(400).decode('utf-8', errors='replace').strip()
                result['reason'] = ('이 작업 공간은 Git 저장소가 아니어서 변경 목록을 표시할 수 없습니다.'
                                    if 'not a git repository' in detail
                                    else detail or 'Git 변경을 읽지 못했습니다.')
                return result
            output.seek(0)
            raw = output.read(MAX_BYTES + 1)
        clipped = len(raw) > MAX_BYTES
        parts = raw[:MAX_BYTES].split(b'\0')
        # Drop an incomplete last pathname when the byte budget is reached.
        parts.pop()
        records = []
        for part in parts:
            if len(part) < 4 or part[2:3] != b' ':
                raise ValueError('예상하지 못한 Git 상태 형식입니다.')
            code = part[:2].decode('ascii')
            if any(c not in ' MADTURC?!' for c in code):
                raise ValueError('예상하지 못한 Git 변경 상태입니다.')
            path = part[3:].decode('utf-8', errors='replace')
            records.append(dict(path=path, index=code[0], worktree=code[1], untracked=code == '??',
                                conflicted=code in {'DD','AU','UD','UA','DU','AA','UU'}, directory=path.endswith('/')))
        result.update(status='ready', entries=records[:MAX_ENTRIES], limited=clipped or len(records) > MAX_ENTRIES,
                      total=None if clipped else len(records))
    except subprocess.TimeoutExpired:
        result.update(status='timeout', reason='변경 목록 조회가 5초 안에 끝나지 않았습니다. 잠시 뒤 다시 확인해 주세요.')
    except (OSError, ValueError) as exc:
        result['reason'] = 'Git 변경을 읽지 못했습니다: ' + str(exc)[:240]
    finally:
        result['checkedAt'] = time.time()
        _slots.release()
    return result
