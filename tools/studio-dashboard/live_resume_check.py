"""Opt-in tiny live check: persisted original CLI session -> fresh Hub import -> same-ID resume.
Uses provider quota; never sends to an existing user conversation or approves tools.
"""
import json
import sys
import tempfile
import time
import secrets
from hub import Hub, HERE
from live_check import wait

def main(provider):
    with tempfile.TemporaryDirectory(prefix='pitcrew-resume-origin-') as original:
        first=Hub(original)
        try:
            a=first.save_agent(dict(provider=provider,name='Disposable resume verification',mode='read'))
            word='RESUME_'+secrets.token_hex(5).upper()
            first.send(a['id'],dict(text='Do not use tools. Remember this test word: '+word+'. Reply READY only.'))
            wait(first,a['id'],timeout=150)
            sid=first.store.agent(a['id'])['session']
        finally:first.close();first.store.db.close()
    # No original Hub DB/history available: memory must come from provider's original session.
    with tempfile.TemporaryDirectory(prefix='pitcrew-resume-import-') as target:
        second=Hub(target)
        try:
            sources=second.sources.discover()
            source=next(s for s in sources if s['provider']==provider and not s.get('parent') and second.sources.native_session(s['id'])==sid)
            a=second.watch(source['id']);second.resume_source(a['id'])
            before=len(second.detail(a['id'])['messages'])
            second.send(a['id'],dict(text='Do not use tools. What exact test word did I ask you to remember? Reply with that word only.'))
            messages=wait(second,a['id'],timeout=150)
            result=second.store.agent(a['id'])
            answer=next(m['text'] for m in reversed(messages) if m['role']=='assistant')
            assert word in answer,(word,answer)
            assert result['session']==sid,(sid,result['session'])
            second.sources.transcripts.clear()
            original_text='\n'.join(m['text'] for m in second.sources.messages(source['id']))
            assert 'What exact test word' in original_text
            assert original_text.count(word)>=2
            report=dict(provider=provider,session=sid,status='PASS',sameSession=True,remembered=True,originalTranscriptAppended=True,originalDisplayedMessages=before)
            print(json.dumps(report),flush=True)
            (HERE/'.runtime'/('resume-'+provider+'-verified.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
        finally:second.close();second.store.db.close()

if __name__=='__main__':main(sys.argv[1])
