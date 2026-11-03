"""Hermes 데스크톱 입력창 위 '실행 중' 표시용: 1분마다 지금 상태를 한 줄로 찍음(D-254).
작업 자체는 Hermes 밖(start_queues.ps1)에서 돌고, 이건 보이기만 함 — 꺼져도 작업엔 영향 없음."""
import time, pathlib, datetime as dt
src = pathlib.Path("D:/Github/F1Game/scripts/tg_status.py").read_text(encoding="utf-8").split('prev = ""')[0]
g = {}
exec(src.replace("pidf.write_text(str(os.getpid()))", "pass"), g)
while True:
    try:
        lines = [l.strip() for l in g["status"]().splitlines()[1:] if l.strip() and not l.startswith("최근")]
        print(dt.datetime.now().strftime("%H:%M"), " | ".join(lines), flush=True)
    except Exception as e:
        print("status error", e, flush=True)
    time.sleep(60)
