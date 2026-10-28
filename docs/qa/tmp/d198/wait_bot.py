# 봇이 끝날 때까지 15초마다 상태 확인, 끝나면 결과 파일 출력(최대 9분)
import subprocess, time
for _ in range(36):
    s = subprocess.run(["bash", "scripts/ucli-http.sh", "exec", "return TownPathBot.Status;"], capture_output=True, text=True, encoding="utf8", errors="replace").stdout.strip().splitlines()
    s = s[-1] if s else ""
    if "done" in s: break
    time.sleep(15)
print(s)
print(open("docs/qa/tmp/townv6-bot-loopBox.txt", encoding="utf-8-sig").read())
