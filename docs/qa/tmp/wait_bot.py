"""봇이 끝날 때까지(최대 N초) 2초마다 Status·표식 단계를 읽어 전이를 한 줄씩 낸다. 사용: python -I wait_bot.py <최대초>"""
import subprocess, sys, time

limit = float(sys.argv[1]) if len(sys.argv) > 1 else 240
code = ('var ui=UnityEngine.Object.FindFirstObjectByType<DotScreenUI>(); '
        'return OnFootRouteBot.Status+"|"+ui.DestMode+"|"+ui.DestDistM.ToString("F1")+"|"+ui.FlagTier+"|"+ui.FlagSize.x+"x"+ui.FlagSize.y+"|"+ui.DestAreaPct.ToString("F2");')
t0 = time.time(); last = None
while time.time() - t0 < limit:
    r = subprocess.run(['bash', 'D:/Github/F1Game/scripts/ucli-http.sh', 'exec', code], capture_output=True, text=True, encoding='utf-8')
    line = (r.stdout.strip().splitlines() or [''])[-1]
    st, mode, dist, tier, wh, area = (line.split('|') + [''] * 6)[:6]
    key = (mode, tier)
    if key != last:
        print(f"{time.time()-t0:5.0f}s status={st} mode={mode} dist={dist} tier={tier} size={wh} area={area}", flush=True)
        last = key
    if st.startswith('done') or st.startswith('FAIL'):
        print('END', st, flush=True); break
    time.sleep(2)
