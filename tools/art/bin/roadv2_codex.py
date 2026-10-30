"""Road v2 task transport: the existing unity-cli HTTP protocol, main F1 only."""
import argparse, json, pathlib, sys, time, urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[3]

def request(command, params):
    instances = pathlib.Path.home() / '.unity-cli/instances'
    deadline=time.monotonic()+120
    while True:
        d=None
        for p in instances.glob('*.json'):
            try:
                candidate = json.loads(p.read_text(encoding='utf-8'))
            except (OSError, ValueError):
                continue
            if candidate.get('projectPath', '').replace('\\', '/').lower() == str(ROOT / 'F1').replace('\\', '/').lower() and candidate.get('state') != 'stopped':
                d=candidate; break
        if d and d.get('state') not in ('reloading','compiling','refreshing'):
            break
        if time.monotonic()>deadline:
            raise RuntimeError(f'Main F1 connector unavailable: {d}')
        time.sleep(1)
    body = json.dumps({'command': command, 'params': params}).encode()
    req = urllib.request.Request(f"http://127.0.0.1:{d['port']}/command", body, {'Content-Type': 'application/json'})
    result = json.load(urllib.request.urlopen(req, timeout=280))
    if not result.get('success'):
        raise RuntimeError(result)
    data = result.get('data')
    return data if isinstance(data, str) else json.dumps(data, ensure_ascii=False, indent=2)

if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--file'); p.add_argument('--code'); p.add_argument('--out')
    p.add_argument('--command', default='exec'); p.add_argument('--params', default='{}')
    args = p.parse_args()
    params = json.loads(args.params)
    if args.file or args.code:
        params = {'code': pathlib.Path(args.file).read_text(encoding='utf-8') if args.file else args.code}
    result = request(args.command, params)
    if args.out:
        pathlib.Path(args.out).parent.mkdir(parents=True, exist_ok=True)
        pathlib.Path(args.out).write_text(result, encoding='utf-8')
        print(args.out)
    else:
        print(result)
