/* Attention: tell the user when an agent finishes, fails or waits for an answer.
   Browser notification while the tab is in the background, unread count in the tab title always. */
(function(){
  'use strict';
  const supported='Notification' in window;
  const baseTitle=document.title;
  const seen=new Map();let unread=0,primed=false,project=null;
  const KEY='pitcrew.notify';
  const enabled=()=>{try{return localStorage.getItem(KEY)!=='off';}catch{return true;}};
  function setTitle(){document.title=unread?`(${unread}) ${baseTitle}`:baseTitle;}
  function clear(){if(!document.hidden&&document.hasFocus()){unread=0;setTitle();}}
  function button(){
    let b=document.getElementById('notifyToggle');
    if(!b){const nav=document.querySelector('.workspace-menu .compact-menu-items')||document.querySelector('.appbar nav');if(!nav)return;b=document.createElement('button');b.id='notifyToggle';b.type='button';b.className='quiet';nav.insertBefore(b,nav.firstChild);b.onclick=toggle;}
    const on=supported&&Notification.permission==='granted'&&enabled();
    b.textContent=on?'알림 켜짐':'알림 켜기';b.setAttribute('aria-pressed',String(on));
    b.title=!supported?'이 브라우저는 알림을 지원하지 않습니다.':Notification.permission==='denied'?'브라우저 설정에서 이 사이트의 알림이 차단되어 있습니다.':'작업 완료·실패·승인 대기를 화면이 가려져 있을 때 알립니다.';
  }
  async function toggle(){
    if(!supported)return toast('이 브라우저는 알림을 지원하지 않습니다.');
    if(Notification.permission==='denied')return toast('브라우저 주소창의 사이트 설정에서 알림을 허용해 주세요.');
    if(Notification.permission!=='granted'){const r=await Notification.requestPermission();try{localStorage.setItem(KEY,'on');}catch{}button();if(r==='granted')toast('작업이 끝나거나 응답이 필요하면 알려드립니다.');return;}
    try{localStorage.setItem(KEY,enabled()?'off':'on');}catch{}button();toast(enabled()?'알림을 켰습니다.':'알림을 껐습니다. 탭 제목의 숫자는 계속 표시됩니다.');
  }
  // Inbox (Linear-style "확인 필요"): events stay listed until the conversation is viewed.
  const INBOX='pitcrew.inbox';
  const loadInbox=()=>{try{return JSON.parse(localStorage.getItem(INBOX)||'{}');}catch{return {};}};
  let inbox=loadInbox();
  const saveInbox=()=>{try{localStorage.setItem(INBOX,JSON.stringify(inbox));}catch{}};
  const KIND={done:'완료',error:'실패',interrupted:'중단',waiting:'응답 필요'};
  const viewing=id=>id===state.selected&&!document.hidden&&!document.querySelector('.chat-columns.results-focused');
  function remember(a,kind){inbox[a.id]={kind,at:Date.now(),task:(a.lastTask||'').slice(0,90),project:state.projectId};saveInbox();renderInbox();}
  function markSeen(){
    const id=state.selected;if(!id||!viewing(id)||!inbox[id])return;
    if(inbox[id].kind==='waiting'&&(state.approvals||[]).some(x=>x.agent===id))return;  // still needs an answer
    delete inbox[id];saveInbox();renderInbox();
  }
  let marks='';
  function decorate(){
    const now=JSON.stringify(Object.keys(inbox).sort());if(now===marks)return;marks=now;
    if(typeof renderAgents==='function')renderAgents();
    if(typeof renderWorkspaceTabs==='function')renderWorkspaceTabs();
  }
  function renderInbox(){
    const items=Object.entries(inbox).filter(([id,x])=>x.project===state.projectId&&agent(id));
    let b=document.getElementById('inboxToggle');
    if(!b){const nav=document.querySelector('.appbar');if(!nav)return;b=document.createElement('button');b.id='inboxToggle';b.type='button';b.className='inbox-toggle';b.setAttribute('aria-haspopup','true');nav.insertBefore(b,nav.querySelector('.refresh-theme')||nav.querySelector('#connection'));b.onclick=toggleInbox;}
    const alerts=items.filter(([,x])=>x.kind!=='done').length;
    b.classList.toggle('hidden',!items.length);b.classList.toggle('has-alert',!!alerts);
    b.textContent=`확인 필요 ${items.length}`;b.title=alerts?`응답·실패 ${alerts}건 포함`:'완료된 작업';
    const pop=document.getElementById('inboxPop');if(pop&&!pop.classList.contains('hidden'))fillInbox(pop,items);
    decorate();
  }
  function fillInbox(pop,items){  // + the alert log at the bottom
    items.sort((x,y)=>(y[1].kind!=='done')-(x[1].kind!=='done')||y[1].at-x[1].at);
    const log=(state.alertLog||[]).slice(-8).reverse();
    const logHtml=log.length?`<details class="alert-log"><summary>최근 알림 기록 ${log.filter(x=>x.sent).length}건 보냄 · ${log.filter(x=>!x.sent).length}건 묶음</summary><ul>${log.map(x=>`<li class="${x.sent?'':'held'}"><time>${new Date(x.ts*1000).toLocaleTimeString('ko-KR',{hour:'2-digit',minute:'2-digit'})}</time> ${esc(x.name||'사이클')} · ${esc({done:'완료',error:'실패',interrupted:'중단',waiting:'응답 필요',cycle:'사이클'}[x.kind]||x.kind)} — ${esc(x.why)}</li>`).join('')}</ul></details>`:'';
    pop.innerHTML=(items.length?`<div class="inbox-head"><b>확인 필요</b><button type="button" class="quiet" data-inbox-clear>모두 확인</button></div>`+items.map(([id,x])=>`<button type="button" class="inbox-item kind-${x.kind}" data-inbox-open="${esc(id)}"><span class="inbox-kind">${KIND[x.kind]||x.kind}</span><span class="inbox-main"><b>${esc(agent(id)?.name||'')}</b><small>${esc(x.task||'')}</small></span><time>${new Date(x.at).toLocaleTimeString('ko-KR',{hour:'2-digit',minute:'2-digit'})}</time></button>`).join(''):'<p class="inbox-empty">확인할 항목이 없습니다.</p>')+logHtml;
  }
  function toggleInbox(){
    let pop=document.getElementById('inboxPop');
    if(!pop){pop=document.createElement('div');pop.id='inboxPop';pop.className='inbox-pop hidden';pop.setAttribute('role','dialog');pop.setAttribute('aria-label','확인 필요');document.body.appendChild(pop);
      pop.addEventListener('click',e=>{const o=e.target.closest('[data-inbox-open]');if(o){pop.classList.add('hidden');if(document.querySelector('.chat-columns.results-focused'))globalThis.PitCrewArtifacts?.close();if(state.selected!==o.dataset.inboxOpen)select(o.dataset.inboxOpen);else markSeen();document.getElementById('prompt')?.focus();}
        if(e.target.closest('[data-inbox-clear]')){const open=new Set((state.approvals||[]).map(x=>x.agent));for(const k of Object.keys(inbox))if(inbox[k].project===state.projectId&&!open.has(k))delete inbox[k];saveInbox();pop.classList.add('hidden');renderInbox();}});
      document.addEventListener('keydown',e=>{if(e.key==='Escape'&&!pop.classList.contains('hidden')){pop.classList.add('hidden');document.getElementById('inboxToggle')?.focus();}});
      document.addEventListener('click',e=>{if(!pop.contains(e.target)&&e.target.id!=='inboxToggle')pop.classList.add('hidden');});}
    const open=pop.classList.toggle('hidden')===false;
    if(open){fillInbox(pop,Object.entries(inbox).filter(([id,x])=>x.project===state.projectId&&agent(id)));pop.querySelector('button')?.focus();}
  }
  // Teams with a lead: members only interrupt you when they need you; the lead stays quiet while members work for it.
  // One notification per finished chain — the same rules as the server's Windows alert:
  // needing you always notifies; a turn whose result went to another agent never does (that agent will);
  // "done" waits while a reply this agent asked for is pending or more queued work follows; team members leave it to the lead.
  function quiet(a,kind){
    if(kind==='waiting')return false;
    if(a.reportedTo)return true;
    if(kind==='done'&&(a.delegating||(a.queue||[]).length))return true;
    const member=(state.teams||[]).some(t=>t.lead&&t.lead!==a.id&&t.members.includes(a.id));
    return member&&(kind==='done'||kind==='interrupted');
  }
  let lastDone=0;
  function notify(a,title,body,kind){
    const away=document.hidden||!document.hasFocus();
    if(!viewing(a.id))remember(a,kind);
    if(quiet(a,kind))return;  // kept in the inbox, no pop-up or title count
    if(!away&&viewing(a.id))return;
    // finishes close together are one pop-up (same rule as the Windows alert); needing you always pops up
    if(kind==='done'&&Date.now()-lastDone<180000)return;
    if(kind==='done')lastDone=Date.now();
    unread++;setTitle();
    if(!away||!supported||Notification.permission!=='granted'||!enabled())return;
    try{const n=new Notification(title,{body:String(body||'').slice(0,180),tag:'pitcrew-'+a.id,renotify:true});
      n.onclick=()=>{window.focus();if(a.id!==state.selected&&agent(a.id))select(a.id);n.close();};}catch{}
  }
  function observe(){
    if(project!==state.projectId){project=state.projectId;seen.clear();primed=false;}
    const waiting=new Set((state.approvals||[]).map(x=>x.agent));
    for(const id of waiting){const a=agent(id);if(a&&inbox[id]?.kind!=='waiting'&&!viewing(id))remember(a,'waiting');}
    for(const a of state.agents||[]){
      // runStartedAt identifies a run, so a run that started and ended between two polls still counts.
      const now=waiting.has(a.id)?'waiting':a.status,run=a.runStartedAt||0,prev=seen.get(a.id);seen.set(a.id,{status:now,run});
      if(!primed||prev===undefined)continue;
      const newRun=run&&run!==prev.run,before=prev.status;
      if(before===now&&!newRun)continue;
      const task=a.lastTask?`“${a.lastTask.slice(0,80)}”`:'';
      if(now==='waiting'){if(before!=='waiting')notify(a,`${a.name} · 응답이 필요합니다`,'승인 또는 질문에 답하면 작업이 계속됩니다. '+task,'waiting');}
      else if(['running','stopping','waiting'].includes(before)||newRun){
        if(now==='done')notify(a,`${a.name} · 작업 완료`,task,'done');
        else if(now==='error')notify(a,`${a.name} · 실행 실패`,a.error||task,'error');
        else if(now==='interrupted')notify(a,`${a.name} · 작업 중단`,task,'interrupted');
      }
    }
    primed=true;button();markSeen();renderInbox();
  }
  window.addEventListener('focus',()=>{clear();markSeen();});document.addEventListener('visibilitychange',()=>{clear();markSeen();});
  // Server uses this to decide whether a Windows notification is needed instead.
  const canNotify=()=>supported&&Notification.permission==='granted'&&enabled();
  // Opened from a Windows notification: ?agent=<id> selects that conversation.
  const wanted=new URLSearchParams(location.search).get('agent');
  if(wanted){let tries=0;const t=setInterval(()=>{if(agent(wanted)){clearInterval(t);if(state.selected!==wanted)select(wanted);history.replaceState(null,'',location.pathname);}else if(++tries>40)clearInterval(t);},250);}
  // Opening a conversation marks it read at once, whichever module opened it.
  if(typeof window.select==='function'){const open=window.select;window.select=function(id){const r=open.apply(this,arguments);markSeen();return r;};}
  globalThis.PitCrewNotify={observe,canNotify,markSeen,unread:id=>!!inbox[id],kind:id=>inbox[id]?.kind};
})();
