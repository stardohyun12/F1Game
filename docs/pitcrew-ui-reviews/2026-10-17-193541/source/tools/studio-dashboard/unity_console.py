"""Explicit, bounded console observation through the installed unity-cli connector.

No clear, play, asset write, arbitrary code, or user-selected network destination.
The fixed probe temporarily removes console display filters and restores them in finally.
Scene state and console are sequential observations, not an atomic game/test result.
"""
import json
from pathlib import Path
import threading
import time
import unity_status as U

LIMIT = 50
MAX_ENTRY = 4000
MAX_CONSOLE_BYTES = 1024 * 1024
CONSOLE_CODE = Path(__file__).with_name('unity_console_probe.cs').read_text(encoding='utf-8')
CONSOLE_BODY = json.dumps({'command': 'exec', 'params': {'code': CONSOLE_CODE}}).encode()
_cache = {}
_lock = threading.Lock()


def snapshot(root, project):
    projects = U.find_projects(root)
    match = next(((p, v) for p, v in projects if U._key(p) == U._key(project)), None)
    if match is None:
        raise ValueError('이 작업 공간에서 확인한 Unity 프로젝트를 선택하세요.')
    path, version = match
    key = (U._key(Path(root).resolve()), U._key(path))
    # Do not queue requests indefinitely or send concurrent reads to Unity.
    if not _lock.acquire(timeout=.2):
        raise ValueError('다른 콘솔 조회를 처리 중입니다. 잠시 뒤 다시 확인하세요.')
    try:
        cached = _cache.get(key)
        if cached and time.monotonic() < cached[0]:
            return cached[1]
        deadline = time.monotonic() + U.BUDGET
        row, port = U._precheck(path, version, U._descriptors(time.time()).get(U._key(path)), time.time())
        if port is None:
            raise ValueError(row['reason'] or '에디터 연결을 확인하지 못했습니다.')
        left = deadline - time.monotonic()
        if not U._probe_slots.acquire(timeout=max(0, left - U.MIN_PROBE)):
            raise ValueError('에디터 조회가 밀려 있습니다. 잠시 뒤 다시 확인하세요.')
        try:
            def request(body, kind):
                left = deadline - time.monotonic()
                if left < U.MIN_PROBE:
                    raise ValueError('조회 시간 안에 콘솔을 확인하지 못했습니다.')
                limit = MAX_CONSOLE_BYTES if body is CONSOLE_BODY else U.MAX_RESPONSE_BYTES
                data, error = U._request_editor(port, min(U.TIMEOUT, left), deadline, body, kind,
                                                max_response_bytes=limit,
                                                max_error_chars=2048 if body is CONSOLE_BODY else 160)
                if error:
                    message = error[1]
                    if body is CONSOLE_BODY:
                        message = message.replace('상태 질의', '콘솔 조회').replace(
                            'Console settings may not be restored.',
                            '콘솔 표시 설정을 되돌리지 못했을 수 있습니다. Unity 콘솔의 필터와 검색어를 확인하세요.')
                    raise ValueError(message)
                return data
            data = request(U.PROBE_BODY, dict)
            U._apply(row, path, port, data, None)
            if row['status'] != 'connected':
                raise ValueError(row['reason'])
            console = request(CONSOLE_BODY, dict)
            entries = console.get('entries')
            if (U._key(console.get('dataPath', '')) != U._key(path / 'Assets') or
                console.get('order') != 'newest-first' or not isinstance(entries, list) or
                len(entries) > LIMIT or any(not isinstance(e, str) for e in entries) or
                any(type(console.get(k)) is not bool for k in ('hasEarlier', 'scanLimited', 'textTruncated')) or
                any(type(console.get(k)) is not int or console[k] < 0 for k in ('totalRows', 'scannedRows')) or
                not len(entries) <= console['scannedRows'] <= min(5000, console['totalRows']) or
                console['hasEarlier'] != (console['scannedRows'] < console['totalRows']) or
                console['scanLimited'] != (console['hasEarlier'] and len(entries) < LIMIT)):
                raise ValueError('콘솔 응답 형식이 예상과 다릅니다.')
            result = dict(project=row, observedAt=round(time.time(), 3), limit=LIMIT,
                          order='newest-first', atLimit=console['hasEarlier'], scanLimited=console['scanLimited'],
                          totalRows=console['totalRows'], scannedRows=console['scannedRows'],
                          textTruncated=console['textTruncated'] or any(len(e) > MAX_ENTRY for e in entries),
                          entries=[e[:MAX_ENTRY] for e in entries])
            if len(_cache) >= 32:
                _cache.clear()
            _cache[key] = (time.monotonic() + 5, result)
            return result
        finally:
            U._probe_slots.release()
    finally:
        _lock.release()
