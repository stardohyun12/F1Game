@echo off
rem 대기열 3줄을 Hermes 와 무관한 독립 프로세스로 띄움(게이트웨이 재시작에도 안 죽게, D-247)
cd /d D:\Github\F1Game
start "q-editor" /min "C:\Users\rtx40\AppData\Local\hermes\tools\git-2.53.0+3-win32-x64\bin\bash.exe" -lc "cd /d/Github/F1Game && bash scripts/team_queue.sh editor >> .ai_logs/team/queue-editor.out 2>&1"
start "q-studio" /min "C:\Users\rtx40\AppData\Local\hermes\tools\git-2.53.0+3-win32-x64\bin\bash.exe" -lc "cd /d/Github/F1Game && bash scripts/team_queue.sh studio >> .ai_logs/team/queue-studio.out 2>&1"
start "q-feature" /min "C:\Users\rtx40\AppData\Local\hermes\tools\git-2.53.0+3-win32-x64\bin\bash.exe" -lc "cd /d/Github/F1Game && bash scripts/team_queue.sh feature >> .ai_logs/team/queue-feature.out 2>&1"
