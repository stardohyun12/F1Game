"""Read-only account quota adapters. Credentials never leave their provider or reach the UI.

Independent implementation; reference contracts and Orca failure cases in PRODUCT_PLAN.md.
"""
import datetime
import email.utils
import json
import math
import os
from pathlib import Path
import threading
import time
import urllib.error
import urllib.request

def percent(value):
    return min(100,max(0,value)) if isinstance(value,(int,float)) and not isinstance(value,bool) and math.isfinite(value) else None

def timestamp(value):
    if isinstance(value,(int,float)):return value
    try:return datetime.datetime.fromisoformat(value.replace('Z','+00:00')).timestamp()
    except (ValueError,TypeError,AttributeError):return None

def codex_windows(data):
    buckets=data.get('rateLimitsByLimitId') or {'codex':data.get('rateLimits') or {}}
    rows=[]
    for key,bucket in buckets.items():
        for lane in ('primary','secondary'):
            window=bucket.get(lane)
            if not window:continue
            rows.append(dict(id=key+':'+lane,label=bucket.get('limitName') or key,
                usedPercent=percent(window.get('usedPercent')),minutes=window.get('windowDurationMins'),
                resetsAt=timestamp(window.get('resetsAt'))))
    return rows

def claude_windows(data):
    rows=[]
    for key,label,minutes in [('five_hour','현재 세션',300),('seven_day','전체 모델',10080),
            ('seven_day_opus','Opus',10080),('seven_day_sonnet','Sonnet',10080),
            ('fable_weekly','Fable',10080),('fable_seven_day','Fable',10080),('seven_day_fable','Fable',10080)]:
        item=data.get(key)
        if not isinstance(item,dict):continue
        value=percent(item.get('utilization',item.get('used_percentage')))
        if value is not None:rows.append(dict(id=key,label=label,usedPercent=value,minutes=minutes,resetsAt=timestamp(item.get('resets_at'))))
    for i,item in enumerate(data.get('limits') or []):
        value=percent(item.get('percent'))
        if value is None or item.get('is_active') is False:continue
        label=((item.get('scope') or {}).get('model') or {}).get('display_name') or item.get('kind','한도')
        if any(r['label']==label for r in rows):continue
        rows.append(dict(id='limit:'+str(i),label=label,usedPercent=value,minutes=10080 if 'weekly' in item.get('kind','') else None,resetsAt=timestamp(item.get('resets_at'))))
    return rows

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args,**kwargs):return None

class AccountUsage:
    def __init__(self,store):
        self.store=store;self.lock=threading.Lock();self.pending=set()
        self.cache=store.get('account-usage',{})

    def snapshot(self):
        with self.lock:
            for provider in ('codex','claude'):
                old=self.cache.get(provider,{})
                if provider not in self.pending and time.time()>=old.get('retryAt',0):
                    self.pending.add(provider)
                    threading.Thread(target=self.refresh,args=(provider,),daemon=True).start()
            return [dict(self.cache.get(p,dict(provider=p,status='loading',windows=[])),refreshing=p in self.pending) for p in ('codex','claude')]

    def refresh(self,provider):
        now=time.time();retry=300;result=dict(provider=provider,status='unavailable',windows=[],checkedAt=now)
        try:
            if provider=='codex':
                from hub import Rpc
                rpc=Rpc()
                try:data=rpc.call('account/rateLimits/read',{})
                finally:rpc.close()
                result.update(windows=codex_windows(data),source='Codex CLI · account/rateLimits/read')
            else:
                path=Path(os.environ.get('CLAUDE_CONFIG_DIR',Path.home()/'.claude'))/'.credentials.json'
                credentials=json.loads(path.read_text(encoding='utf-8-sig')).get('claudeAiOauth',{})
                token=credentials.get('accessToken')
                if not token:raise ValueError('no subscription credentials')
                request=urllib.request.Request('https://api.anthropic.com/api/oauth/usage',headers={
                    'Authorization':'Bearer '+token,'anthropic-beta':'oauth-2025-04-20','User-Agent':'PitCrew/1.0'})
                with urllib.request.build_opener(NoRedirect).open(request,timeout=10) as response:data=json.load(response)
                result.update(windows=claude_windows(data),source='Claude 구독 사용량 · OAuth')
            result['status']='ok' if result['windows'] else 'unavailable'
            result['message']='' if result['windows'] else '계정에서 사용량 정보를 제공하지 않았습니다.'
        except urllib.error.HTTPError as error:
            if error.code==429:
                retry=900
                header=error.headers.get('Retry-After','')
                try:retry=max(retry,float(header))
                except ValueError:
                    try:retry=max(retry,email.utils.parsedate_to_datetime(header).timestamp()-now)
                    except (ValueError,TypeError):pass
                result.update(status='cooldown',message='사용량 조회가 제한되었습니다. 안내된 대기 후 자동 갱신합니다.')
            elif error.code in (401,403):result['message']='Claude 로그인 또는 구독 사용량 조회 권한을 확인하세요.'
            else:result['message']='사용량 서비스가 응답하지 않았습니다.'
            error.close()
        except Exception:
            result['message']='로그인 또는 CLI 연결을 확인하세요. 확인하지 못한 사용량은 0으로 표시하지 않습니다.'
        result['retryAt']=now+retry
        with self.lock:
            old=self.cache.get(provider,{})
            if result['status']!='ok' and old.get('windows'):
                result.update(windows=old['windows'],stale=True,lastSuccessAt=old.get('lastSuccessAt',old.get('checkedAt')))
            elif result['status']=='ok':result['lastSuccessAt']=now
            self.cache[provider]=result;self.pending.discard(provider)
            self.store.put('account-usage',self.cache)
