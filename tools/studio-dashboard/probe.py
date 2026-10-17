"""Small integration probes. Never edits the project or launches agent tasks."""
import asyncio
import json
import os
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]

def codex_command():
    import shutil
    path = shutil.which('codex')
    if path and pathlib.Path(path).suffix.lower() not in ('.cmd', '.ps1', '.bat'):
        return [path]
    npm = pathlib.Path(os.environ.get('APPDATA', '')) / 'npm/node_modules/@openai/codex'
    native = list(npm.glob('node_modules/@openai/codex-*/vendor/*/bin/codex.exe'))
    if native:
        return [str(native[0])]
    if (npm / 'bin/codex.js').exists():
        return [shutil.which('node') or 'node', str(npm / 'bin/codex.js')]
    raise RuntimeError('Codex CLI를 찾을 수 없습니다.')

async def probe_claude():
    from claude_agent_sdk import ClaudeSDKClient, ClaudeAgentOptions, ResultMessage
    options = ClaudeAgentOptions(cwd=str(ROOT), setting_sources=[], tools=[],
                                system_prompt='You are testing a local chat connection. Answer briefly.')
    async with ClaudeSDKClient(options=options) as client:
        for prompt in ['Remember the word PITCREW. Reply READY only.', 'What word did I ask you to remember? Reply with that word only.']:
            await client.query(prompt)
            async for msg in client.receive_response():
                if isinstance(msg, ResultMessage):
                    print(json.dumps({'provider':'claude','error':msg.is_error,'result':msg.result,'session':msg.session_id}), flush=True)

def probe_codex():
    p = subprocess.Popen(codex_command()+['app-server','--listen','stdio://'], stdin=subprocess.PIPE,
                         stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, encoding='utf-8',
                         creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
    def send(n, method, params):
        p.stdin.write(json.dumps({'id':n,'method':method,'params':params})+'\n'); p.stdin.flush()
    def response(n):
        while True:
            line=p.stdout.readline()
            if not line: raise RuntimeError('app-server exited')
            obj=json.loads(line)
            if obj.get('id')==n:
                if 'error' in obj: raise RuntimeError(str(obj['error']))
                return obj['result']
    try:
        send(1,'initialize',{'clientInfo':{'name':'pitcrew_probe','version':'1.0.0'}})
        response(1)
        p.stdin.write('{"method":"initialized"}\n'); p.stdin.flush()
        send(2,'thread/start',{'cwd':str(ROOT),'approvalPolicy':'untrusted','sandbox':'read-only'})
        tid=response(2)['thread']['id']
        for i,prompt in enumerate(['Do not use tools. Remember PITCREW and reply READY only.', 'Do not use tools. What word did I ask you to remember? Reply only that word.']):
            send(3+i,'turn/start',{'threadId':tid,'input':[{'type':'text','text':prompt}]})
            while True:
                line=p.stdout.readline()
                if not line: raise RuntimeError('app-server exited')
                obj=json.loads(line)
                if obj.get('error'): raise RuntimeError(str(obj['error']))
                if obj.get('method')=='item/completed' and obj['params']['item']['type']=='agentMessage':
                    print(json.dumps({'provider':'codex','text':obj['params']['item']['text']},ensure_ascii=False),flush=True)
                if obj.get('method')=='turn/completed':
                    print(json.dumps({'provider':'codex','turn':obj['params']['turn']['status'],'error':obj['params']['turn'].get('error')}),flush=True)
                    break
    finally:
        p.terminate(); p.wait(timeout=10)

if __name__ == '__main__':
    if sys.argv[1]=='claude': asyncio.run(probe_claude())
    else: probe_codex()
