"""Opt-in real-provider workflow. Prepare native sessions, then import/connect via UI.

prepare prints the project and source names. verify uses only that manifest's
disposable conversations; it never sends to pre-existing user sessions.
"""
import json
import secrets
import subprocess
import sys
import time
import urllib.request
from pathlib import Path
from hub import Hub, HERE, Sources
from live_check import wait

MANIFEST=HERE/'.runtime/team-flow-current.json'
BASE='http://127.0.0.1:7878/api/hub/'

def api(path,body=None,project='default'):
    url=BASE+path+('&' if '?' in path else '?')+'project='+project
    headers={'Content-Type':'application/json'}
    if body is not None:
        headers['X-PitCrew-Token']=api('bootstrap',project=project)['token']
    with urllib.request.urlopen(urllib.request.Request(url,data=None if body is None else json.dumps(body).encode(),headers=headers),timeout=30) as response:
        return json.load(response)

def prepare():
    tag=secrets.token_hex(4).upper()
    directory=HERE/'.runtime'/('team-flow-'+tag)
    project=directory/'project';project.mkdir(parents=True)
    data={'tag':tag,'directory':str(directory),'projectPath':str(project),'requirementCode':'REQ_'+tag,'implementationCode':'BUILD_'+tag,'teamCode':'TEAM_'+tag,'sessions':{}}
    hub=Hub(directory/'origin',root=project)
    try:
        seeds={
            'claude':f"Do not use tools. This is a disposable collaboration test. You are the planner. Remember requirement code {data['requirementCode']}. Specify a JavaScript function usageLabel(value): finite numeric values are clamped to 0..100, rounded to nearest integer and suffixed with %. Non-numbers including null and strings, NaN and infinities return an em dash (—). Reply in Korean with a short requirement and these exact examples: 22.6 -> 23%, -10 -> 0%, 101 -> 100%, null -> —.",
            'codex':f"Do not use tools. This is a disposable collaboration test. You are the implementer. Remember implementation code {data['implementationCode']}. Later you will receive a planner's requirements through a connected peer conversation. Reply with your code and READY only."
        }
        for provider,prompt in seeds.items():
            agent=hub.save_agent({'name':'협업검증 '+tag+' '+provider,'provider':provider,'mode':'read'})
            hub.send(agent['id'],{'text':prompt})
            wait(hub,agent['id'],timeout=180)
            data['sessions'][provider]=hub.store.agent(agent['id'])['session']
    finally:
        hub.close();hub.store.db.close()
    # Save paid seed results before discovery/registration so failures can be
    # recovered without starting another pair of provider conversations.
    MANIFEST.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    p=api('projects',{'path':str(project),'name':'실제 협업 검증 '+tag})
    data['projectId']=p['id']
    sources=Sources(project)
    data['sources']={provider:next(s['id'] for s in sources.discover() if s['provider']==provider and not s.get('parent') and sources.native_session(s['id'])==sid) for provider,sid in data['sessions'].items()}
    MANIFEST.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
    print(json.dumps(data,ensure_ascii=False),flush=True)

def verify():
    data=json.loads(MANIFEST.read_text(encoding='utf-8'));pid=data['projectId']
    b=api('bootstrap',project=pid)
    agents={provider:next(a for a in b['agents'] if a.get('session')==sid) for provider,sid in data['sessions'].items()}
    team=next(t for t in b['teams'] if all(a['id'] in t['members'] for a in agents.values()))
    assert data['teamCode'] in team['context'],'Team context missing'
    for provider,a in agents.items():
        other=agents['claude' if provider=='codex' else 'codex']
        assert other['id'] in a['peers'],f'{provider} peer context missing'
        assert a.get('originSource')==data['sources'][provider]
    def turn(provider,text):
        aid=agents[provider]['id'];api('send',{'agent':aid,'text':text},pid)
        deadline=time.monotonic()+180
        while time.monotonic()<deadline:
            d=api('detail?agent='+aid,project=pid)
            if d['agent']['status']=='done':
                assert d['agent']['session']==data['sessions'][provider]
                return d
            if d['agent']['status'] in ('error','waiting','interrupted'):
                raise RuntimeError(provider+': '+str(d['agent'].get('error') or d['agent']['status']))
            time.sleep(1)
        raise RuntimeError(provider+' timed out; inspect the test project before retrying')
    codex=turn('codex','도구는 사용하지 마세요. 연결된 기획자 대화의 요구사항과 팀 공통 목표를 참고해 usageLabel(value)를 구현하세요. 새로 전달된 요구코드와 팀코드, 원래 대화에서 기억하는 구현코드를 먼저 쓰고, 순수 JavaScript 함수 하나만 코드 블록으로 작성하세요. 한국어로 짧게 답하세요.')
    answer=next(m['text'] for m in reversed(codex['messages']) if m['role']=='assistant')
    for key in ('requirementCode','implementationCode','teamCode'):assert data[key] in answer,key+' missing from Codex answer'
    import re
    blocks=re.findall(r'```(?:javascript|js)?\s*\n(.*?)```',answer,re.S)
    assert blocks,'Code block missing'
    code=next(c for c in blocks if 'usageLabel' in c)
    checker="""const vm=require('node:vm'),fs=require('node:fs');const code=fs.readFileSync(0,'utf8');const scope={};vm.createContext(scope);vm.runInContext(code,scope,{timeout:1000});const cases=[[22.6,'23%'],[-10,'0%'],[101,'100%'],[null,'—'],['40','—'],[NaN,'—'],[Infinity,'—'],[undefined,'—'],[0,'0%'],[100,'100%']];for(const [value,expected] of cases){scope.testInput=value;const actual=vm.runInContext('usageLabel(testInput)',scope,{timeout:1000});if(actual!==expected)throw Error('Expected '+expected+' got '+actual);}console.log('10 implementation cases PASS');"""
    result=subprocess.run(['node','-e',checker],input=code,text=True,encoding='utf-8',capture_output=True,timeout=15)
    assert result.returncode==0,result.stderr
    print(result.stdout.strip(),flush=True)
    claude=turn('claude','도구는 사용하지 마세요. 연결된 구현자 대화의 최신 usageLabel 코드를 원래 요구사항과 비교해 검토하세요. 구현자가 기억하는 구현코드와 팀 공통 목표의 팀코드를 적고, null, 문자열, NaN, Infinity, -10, 22.6, 101의 기대 결과를 확인하세요. 오류가 있으면 구체적으로 지적하고, 없으면 검토 통과라고 하세요. 한국어로 짧게 답하세요.')
    review=next(m['text'] for m in reversed(claude['messages']) if m['role']=='assistant')
    assert data['implementationCode'] in review and data['teamCode'] in review
    assert '검토 통과' in review,review
    for provider,detail in [('codex',codex),('claude',claude)]:
        sent=next(m for m in reversed(detail['messages']) if m['role']=='user')
        assert any(s['kind']=='peer' for s in sent['context']['snapshots'])
        assert data['teamCode'] in sent['context']['shared']
        # Test codes were never repeated in either verification prompt.
        assert not any(data[k] in sent['text'] for k in ('requirementCode','implementationCode','teamCode'))
    report={'verifiedAt':time.time(),'project':pid,'team':team['id'],'sameNativeSessions':True,'codexUsedClaudeContext':True,'claudeUsedCodexContext':True,'sharedTeamContext':True,'implementationCasesPassed':10,'codexAnswer':answer,'claudeReview':review}
    (Path(data['directory'])/'verified.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    (Path(data['projectPath'])/'usageLabel.js').write_text(code,encoding='utf-8')
    print(json.dumps(report,ensure_ascii=False),flush=True)

if __name__=='__main__':
    if sys.argv[1]=='prepare':prepare()
    elif sys.argv[1]=='verify':verify()
    else:raise SystemExit('Use prepare or verify')
