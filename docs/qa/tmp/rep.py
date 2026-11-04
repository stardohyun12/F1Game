import sys


def edit(path, pairs):
    s = open(path, encoding='utf-8', newline='').read()
    for old, new in pairs:
        old = old.replace('\r\n', '\n').replace('\n', '\r\n')
        new = new.replace('\r\n', '\n').replace('\n', '\r\n')
        if s.count(old) != 1:
            print("FAIL count", s.count(old), "for:", old[:80])
            sys.exit(1)
        s = s.replace(old, new)
    open(path, 'w', encoding='utf-8', newline='').write(s)
    print("ok", path)
