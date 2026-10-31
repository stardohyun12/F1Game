# 에디터 없이 컴파일 확인: Unity 가 마지막에 쓴 rsp 를 복사해 출력만 여기로 돌리고 Unity 내장 Roslyn(csc.dll)으로 돌린다.
import os, re, subprocess, sys
PROJ = r"D:/Github/F1Game_shot/F1"
DAG = PROJ + "/Library/Bee/artifacts/1900b0aE.dag"
OUT = os.path.dirname(os.path.abspath(__file__)).replace("\\", "/") + "/cc_out"
UNITY = r"C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data"
DOTNET = UNITY + "/NetCoreRuntime/dotnet.exe"
CSC = UNITY + "/DotNetSdkRoslyn/csc.dll"
os.makedirs(OUT, exist_ok=True)

def absify(line):
    # "Library/..." / "Assets/..." / "Packages/..." 상대 경로를 프로젝트 절대 경로로
    def fix(m):
        p = m.group(1)
        if re.match(r"^(Library|Assets|Packages|Temp)/", p): p = PROJ + "/" + p
        return '"' + p + '"'
    return re.sub(r'"([^"]+)"', fix, line)

def build(name, extra_files, ref_swap):
    src = open(DAG + "/" + name + ".rsp", encoding="utf-8").read().splitlines()
    out = []
    for l in src:
        if l.startswith("-out:"): l = '-out:"%s/%s.dll"' % (OUT, name)
        elif l.startswith("-refout:"): l = '-refout:"%s/%s.ref.dll"' % (OUT, name)
        elif l.startswith("-generatedfilesout") or l.startswith("-errorlog"): continue
        else:
            for a, b in ref_swap.items():
                if a in l: l = '-r:"%s"' % b
            l = absify(l)
        out.append(l)
    for f in extra_files: out.append('"%s/%s"' % (PROJ, f))
    rsp = OUT + "/" + name + ".rsp"
    open(rsp, "w", encoding="utf-8").write("\n".join(out) + "\n")
    rsp2 = DAG + "/" + name + ".rsp2"
    args = [DOTNET, "exec", CSC, "/nostdlib", "/noconfig", "/shared", "@" + rsp]
    if os.path.exists(rsp2): args.append("@" + rsp2)
    r = subprocess.run(args, capture_output=True, text=True, encoding="utf-8", errors="replace", cwd=PROJ)
    lines = [x for x in (r.stdout + r.stderr).splitlines() if "error" in x or "warning CS" in x and ("OnFoot" in x or "FirstOrder" in x)]
    errs = [x for x in lines if " error " in x or ": error" in x]
    print("== %s exit=%d errors=%d" % (name, r.returncode, len(errs)))
    for x in lines[:60]: print(x)
    return r.returncode

a = build("PitCrew", [], {})
b = build("PitCrew.Editor", ["Assets/Editor/FirstOrderSetup.cs"], {"PitCrew.ref.dll": OUT + "/PitCrew.ref.dll"})
sys.exit(0 if a == 0 and b == 0 else 1)
