# Play 중: mode:tag 쌍마다 cam-run.cs 를 돌리고 요약 출력. usage: cam_series.py ":before" "noshake:before_noshake"
import subprocess, sys, time
def ex(code):
    return subprocess.run(['bash', 'scripts/ucli-http.sh', 'exec', code], capture_output=True, text=True, encoding='utf-8').stdout.strip()
for arg in sys.argv[1:]:
    mode, tag, start, cap, flags = (arg.split(':') + ['8', '20', ''])[:5]
    ex('UnityEditor.SessionState.SetInt("cam.kbd",%d); UnityEditor.SessionState.SetInt("cam.legacy",%d); return "";' % ('k' in flags, 'L' in flags))
    ex('UnityEditor.SessionState.SetString("cam.tag","%s"); UnityEditor.SessionState.SetString("cam.mode","%s"); UnityEditor.SessionState.SetInt("cam.start",%s); UnityEditor.SessionState.SetFloat("cam.cap",%sf); return "ok";' % (tag, mode, start, cap))
    ex('CamJitterProbe.Done = false; return "";')
    # python 이 부르는 bash 에선 @파일의 줄바꿈이 사라진다 → // 주석 줄을 빼고 코드로 넘긴다
    code = '\n'.join(l for l in open('docs/qa/tmp/cam-run.cs', encoding='utf-8').read().splitlines() if not l.strip().startswith('//'))
    print(ex(code))
    time.sleep(15)  # 기록 창(5~14초) 동안 에디터에 묻지 않는다 — 묻는 순간 에디터가 0.33초 멈칫해 측정이 튄다
    t0 = time.time(); s = ''
    while time.time() - t0 < 60:
        time.sleep(2)
        s = ex('return CamJitterProbe.Done ? CamJitterProbe.Summary : "";')
        if s: break
    print(s or 'TIMEOUT', flush=True)
    time.sleep(3)
