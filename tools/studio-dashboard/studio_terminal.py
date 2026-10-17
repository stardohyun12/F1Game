"""Opt-in Windows ConPTY sessions, distinct from structured agent threads.

No arbitrary executable/cwd API. The owning runtime retains the PTY across browser reconnects.
Output stays in a bounded memory buffer; no private terminal transcript is written to disk.
"""
import os
import shutil
import subprocess
import threading
import time
import uuid

def active(hub):
    return any(s.proc.isalive() for s in getattr(hub,'studio_terminals',{}).values())

class Session:
    def __init__(self,provider,room,command,cwd):
        from winpty import PtyProcess
        self.id=uuid.uuid4().hex;self.provider=provider;self.room=room;self.created=time.time()
        self.buffer='';self.start=0;self.lock=threading.Lock();self.error='';self.writes={};self.closing=False
        self.proc=PtyProcess.spawn(command,cwd=str(cwd),dimensions=(28,100),env={**os.environ,'TERM':'xterm-256color'})
        threading.Thread(target=self.read,daemon=True).start()
    def read(self):
        try:
            while True:
                chunk=self.proc.read(8192)
                if not chunk:break
                with self.lock:
                    self.buffer+=chunk
                    if len(self.buffer)>1_000_000:
                        remove=len(self.buffer)-1_000_000;self.buffer=self.buffer[remove:];self.start+=remove
        except EOFError:pass
        except Exception as e:
            if not self.closing:self.error=str(e)
        finally:
            # pywinpty 3.1 marks closed in isalive() before close() can free its sockets.
            self.proc.fileobj.close();self.proc._server.close()
    def close(self):
        self.closing=True
        self.proc.close(force=True)
        self.proc.fileobj.close();self.proc._server.close()
    def snapshot(self,offset=0):
        with self.lock:
            offset=max(0,int(offset));reset=offset<self.start or offset>self.start+len(self.buffer)
            start=self.start if reset else offset
            return dict(id=self.id,provider=self.provider,room=self.room,created=self.created,alive=self.proc.isalive(),
                        output=self.buffer[max(0,start-self.start):],offset=self.start+len(self.buffer),reset=reset,error=self.error,
                        mode='native-cli',sharedWithStructured=False)

def state(hub,identity='',offset=0):
    sessions=getattr(hub,'studio_terminals',{})
    if identity:
        if identity not in sessions:raise ValueError('이 프로젝트의 CLI 세션을 찾을 수 없습니다. 서버 재시작 후에는 새 CLI를 여세요.')
        return sessions[identity].snapshot(offset)
    return {'sessions':[{k:v for k,v in s.snapshot().items() if k!='output'} for s in sessions.values()],
            'supported':os.name=='nt','active':active(hub)}

def action(hub,data):
    from hub import text
    with hub.lock:
        sessions=hub.__dict__.setdefault('studio_terminals',{})
        kind=data.get('action')
        if kind=='start':
            import studio
            if os.name!='nt':raise ValueError('현재 CLI 시제품은 Windows ConPTY 전용입니다.')
            if getattr(hub.owner,'shutting_down',False):raise ValueError('실행 서비스가 종료 중입니다.')
            room=data.get('room')
            if room not in studio.state(hub)['rooms']:raise ValueError('작업실이 없습니다.')
            if hub.runs or any(j['status'] in ('running','queued') for j in studio.state(hub)['jobs']) or active(hub):
                raise ValueError('프로젝트 작업 또는 CLI가 실행 중입니다. 먼저 종료하세요. 중복 실행하지 않습니다.')
            provider=data.get('provider')
            if provider=='codex':
                from probe import codex_command
                command=codex_command()+['--no-alt-screen','--sandbox','workspace-write','--ask-for-approval','on-request']
            elif provider=='claude':
                executable=shutil.which('claude')
                if not executable:raise ValueError('Claude Code가 설치되지 않았습니다.')
                command=[executable,'--settings','{"disableAllHooks":true}']
            else:raise ValueError('Codex 또는 Claude를 선택하세요.')
            # Retain a few ended sessions, never resurrect them automatically.
            if len(sessions)>=6:
                old=next((k for k,s in sessions.items() if not s.proc.isalive()),None)
                if old:sessions.pop(old)
            session=Session(provider,room,command,hub.root);sessions[session.id]=session
            hub.store.activity(None,'독립 CLI 세션 시작 · '+provider)
            return session.snapshot()
        session=sessions.get(data.get('id'))
        if not session:raise ValueError('CLI 세션이 없습니다.')
        if kind=='close':
            session.close()
            return {'ok':True}
        if not session.proc.isalive():raise ValueError('CLI가 종료되었습니다. 새 CLI를 여세요.')
        if kind=='input':
            identity=text(data.get('inputId',''),100)
            if not identity:raise ValueError('입력 ID가 필요합니다.')
            if identity not in session.writes:
                session.proc.write(text(data.get('text',''),20000));session.writes[identity]=True
                if len(session.writes)>1000:session.writes.pop(next(iter(session.writes)))
        elif kind=='resize':
            cols=max(30,min(240,int(data.get('cols',100))));rows=max(8,min(80,int(data.get('rows',28))))
            session.proc.setwinsize(rows,cols)
        elif kind=='interrupt':session.proc.write('\x03')
        else:raise ValueError('지원하지 않는 CLI 동작입니다.')
        return {'ok':True}
