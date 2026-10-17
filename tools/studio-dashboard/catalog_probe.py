import asyncio, json, subprocess
from probe import codex_command, ROOT

async def claude():
    from claude_agent_sdk import ClaudeSDKClient, ClaudeAgentOptions
    async with ClaudeSDKClient(options=ClaudeAgentOptions(cwd=str(ROOT),setting_sources=[],tools=[])) as c:
        info=await c.get_server_info()
        print(json.dumps({'keys':list(info),'models':info.get('models')},ensure_ascii=False),flush=True)

def codex():
    p=subprocess.Popen(codex_command()+['app-server'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,text=True,encoding='utf-8',creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
    try:
        for i,method,params in [(1,'initialize',{'clientInfo':{'name':'pitcrew_catalog','version':'1.0'}}),(2,'model/list',{'includeHidden':False})]:
            p.stdin.write(json.dumps({'id':i,'method':method,'params':params})+'\n');p.stdin.flush()
            while True:
                d=json.loads(p.stdout.readline())
                if d.get('id')==i:break
            if i==1:p.stdin.write('{"method":"initialized"}\n');p.stdin.flush()
            else:print(json.dumps(d,ensure_ascii=False),flush=True)
    finally:p.terminate();p.wait()

if __name__=='__main__':
    import sys
    if sys.argv[1]=='claude':asyncio.run(claude())
    else:codex()
