# 2117 조사: 정비소 관련 스크립트 머리말·수정 시각·입출력 경로
import sys, re, os, time
from pathlib import Path
R = Path('D:/Github/F1Game')
fs = [R / p for p in sys.argv[1:]]
for f in sorted(fs, key=lambda f: f.stat().st_mtime):
    t = f.read_text(encoding='utf-8', errors='replace').splitlines()
    head = [l for l in t[:6] if l.strip()][:4]
    io = sorted(set(re.findall(r"['\"]([\w./-]*(?:\.fbx|\.png|\.blend|\.gpl|\.json))['\"]", '\n'.join(t))))[:10]
    print('==', time.strftime('%m-%d %H:%M', time.localtime(f.stat().st_mtime)), f.relative_to(R), len(t), 'lines')
    for l in head: print('   ', l[:220])
    print('    io:', io)
