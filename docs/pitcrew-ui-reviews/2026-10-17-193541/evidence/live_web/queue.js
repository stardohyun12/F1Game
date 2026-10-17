/* Queue and hand-off bar above the composer.
   Queue: messages typed while the agent works, sent one by one when each run finishes.
   Hand-off: "when this finishes, send its last report to <teammate>". */
(function(){
  'use strict';
  let lastHtml='';
  function render(a){
    const bar=document.getElementById('queueBar');if(!bar)return;
    if(!a||a.source){bar.classList.add('hidden');bar.innerHTML='';lastHtml='';return;}
    const q=a.queue||[],hs=a.handoffs||[],held=(q.length||hs.length)&&['error','interrupted'].includes(a.status);
    const allows=(a.autoAllow||[]).map(k=>`<span class="allow-chip">${esc(k.split(':').pop())}<button type="button" data-allow-revoke="${esc(k)}" aria-label="${esc(k.split(':').pop())} 자동 허용 취소">×</button></span>`).join('');
    const hint=document.getElementById('composeHint');if(hint)hint.textContent=busy(a)?'Enter 대기열에 추가 · Shift+Enter 줄바꿈':'Enter 전송 · Shift+Enter 줄바꿈';
    const rules=hs.map(h=>`<li class="queue-item handoff"><span class="queue-index">→</span><span class="queue-text">끝나면 <b>${esc(agent(h.to)?.name||'삭제된 대화')}</b>에게 ${h.attach?'마지막 보고를 붙여 ':''}전달${h.text?` · “${esc(h.text.slice(0,80))}”`:''}</span><button type="button" class="quiet" data-handoff-remove="${esc(h.id)}" aria-label="인계 규칙 삭제">×</button></li>`).join('');
    const head=`<div class="queue-head"><span>${q.length?`대기 ${q.length} <small>· 대화 끝에 표시</small>`:''}${q.length&&hs.length?' · ':''}${hs.length?`인계 ${hs.length}`:''}${held?' · <em>작업이 완료되지 않아 보류 중</em>':''}</span><span>${held&&q.length?'<button type="button" class="quiet" data-queue-resume>이어서 보내기</button>':''}${q.length>1?'<button type="button" class="quiet" data-queue-clear>대기열 비우기</button>':''}<button type="button" class="quiet" data-handoff-open>끝나면 넘기기 ＋</button></span></div>`;
    const allowRow=allows?`<div class="allow-row"><span>계속 허용</span>${allows}</div>`:'';
    const html=(q.length||hs.length)?`${head}${rules?`<ul>${rules}</ul>`:''}${allowRow}`:`${allowRow}<button type="button" class="hidden" data-handoff-open tabindex="-1" aria-hidden="true"></button>`;
    bar.classList.remove('hidden');if(html!==lastHtml){bar.innerHTML=html;lastHtml=html;}
    const sig=a.id+':'+q.map(x=>x.id).join(',');
    if(sig!==lastQueue){lastQueue=sig;if(state.detail?.agent?.id===a.id&&typeof renderMessages==='function'){renderMessages(state.detail);if(q.length)document.getElementById('messages').scrollTop=1e9;}}
  }
  let lastQueue='';
  function handoffDialog(){
    const a=agent(state.selected);if(!a)return;
    const others=state.agents.filter(x=>x.id!==a.id&&!x.source&&!x.archived);
    if(!others.length)return toast('넘겨받을 다른 대화가 없습니다. 먼저 팀원을 추가하세요.');
    modal('끝나면 넘기기',`<form id="handoffForm"><p class="form-note"><b>${esc(a.name)}</b>의 작업이 대기열까지 모두 <b>완료</b>되면 아래 대화로 자동 전송합니다. 실패·중단이면 보내지 않고 보류합니다. 받는 쪽이 작업 중이면 그 대기열에 들어갑니다.</p>
      <div class="field"><label for="handoffTo">받는 대화</label><select id="handoffTo">${others.map(x=>`<option value="${esc(x.id)}">${esc(x.name)} · ${esc(providerName(x.provider))}${busy(x)?' (작업 중)':''}</option>`).join('')}</select></div>
      <div class="field"><label for="handoffText">함께 보낼 지시 <small>비우면 “결과를 확인하고 이어서 진행하세요”</small></label><textarea id="handoffText" rows="4" maxlength="20000" placeholder="예: 이 구현을 검토하고 결함만 보고해줘"></textarea></div>
      <label class="checkbox-row"><input type="checkbox" id="handoffAttach" checked> ${esc(a.name)}의 마지막 보고를 붙이기 (최대 12,000자)</label>
      <div id="formError" class="form-error"></div><div class="buttons"><button type="button" id="handoffCancel">취소</button><button class="primary" type="submit">규칙 추가</button></div></form>`,'HAND-OFF');
    document.getElementById('handoffCancel').onclick=closeModal;
    document.getElementById('handoffForm').onsubmit=async e=>{e.preventDefault();
      try{await api('handoff',{from:a.id,to:$('#handoffTo').value,text:$('#handoffText').value,attach:$('#handoffAttach').checked});closeModal();await poll();toast('인계 규칙을 추가했습니다. 대기열까지 모두 끝나면 전달합니다.');}catch(err){formError(err);}};
  }
  document.addEventListener('click',async e=>{
    const t=e.target.closest('[data-queue-remove],[data-queue-edit],[data-allow-revoke],[data-handoff-remove],[data-queue-resume],[data-queue-clear],[data-handoff-open]');if(!t)return;
    const id=state.selected;
    try{
      if(t.dataset.handoffOpen!==undefined)return handoffDialog();
      if(t.dataset.queueEdit){const item=(agent(id)?.queue||[]).find(x=>x.id===t.dataset.queueEdit);if(!item)return;
        const project=state.projectId;await api('queue',{agent:id,action:'remove',id:item.id},project);
        if(state.selected===id&&state.projectId===project){const box=$('#prompt');box.value=item.text+(box.value?'\n\n'+box.value:'');state.drafts[id]=box.value;box.focus();}
        else state.drafts[id]=item.text+(state.drafts[id]?'\n\n'+state.drafts[id]:'');
        saveDrafts();toast(`대기열에서 꺼내 ${agent(id)?.name||'원래 대화'}의 입력창에 넣었습니다. 고친 뒤 다시 보내세요.`);}
      else if(t.dataset.allowRevoke){await api('revoke-allow',{agent:id,key:t.dataset.allowRevoke});toast('자동 허용을 취소했습니다. 다음부터 다시 묻습니다.');}
      else if(t.dataset.queueRemove)await api('queue',{agent:id,action:'remove',id:t.dataset.queueRemove});
      else if(t.dataset.queueClear!==undefined)await api('queue',{agent:id,action:'clear'});
      else if(t.dataset.queueResume!==undefined)await api('queue',{agent:id,action:'resume'});
      else if(t.dataset.handoffRemove)await api('handoff',{from:id,action:'remove',id:t.dataset.handoffRemove});
      await poll();
    }catch(err){toast(err.message);}
  });
  window.PitCrewQueue={render:a=>{render(a);globalThis.PitCrewTeamPanel?.render(a);}};
})();

/* Team panel above the composer (research r4: Claude Code agent-team panel, Cursor "needs attention").
   Lead: one row per member — what it was handed, status, time — and approvals waiting on a member.
   Member: one line saying who handed this work and that the result goes back automatically. */
(function(){
  'use strict';
  let open=localStorage.getItem('pitcrew.teamPanel')===null?innerWidth>720:localStorage.getItem('pitcrew.teamPanel')!=='0',lastHtml='';
  const ICON={queued:'○',running:'◐',done:'✓',failed:'!',stopped:'■'},WORD={queued:'대기',running:'작업 중',done:'완료',failed:'실패',stopped:'중단'};
  const ago=t=>{const s=Math.max(0,Math.round(Date.now()/1000-t));return s<60?`${s}초`:s<3600?`${Math.round(s/60)}분`:`${Math.round(s/3600)}시간`;};
  function el(){let p=document.getElementById('teamPanel');if(!p){const q=document.getElementById('queueBar');if(!q)return null;p=document.createElement('section');p.id='teamPanel';p.className='team-panel hidden';p.setAttribute('aria-label','팀 현황');q.before(p);}return p;}
  function render(a){
    const p=el();if(!p)return;let html='';
    const teams=a&&!a.source?(state.teams||[]).filter(t=>t.lead&&t.members.includes(a.id)):[];
    const led=teams.filter(t=>t.lead===a.id),dl=state.delegations||[];
    if(led.length){
      const ids=[...new Set(led.flatMap(t=>t.members))].filter(id=>id!==a.id&&agent(id)&&!agent(id).source);
      const rows=ids.map(id=>{const m=agent(id),mine=dl.filter(x=>x.lead===a.id&&x.member===id),waiting=mine.filter(x=>x.status==='queued').length,
        d=[...mine].reverse().find(x=>x.status==='running')||[...mine].reverse().find(x=>x.status==='queued')||mine[mine.length-1],ask=(state.approvals||[]).some(x=>x.agent===id);
        const live=d&&(d.status==='queued'||d.status==='running'),recent=d&&!live&&Date.now()/1000-(d.finishedAt||d.ts)<900;return {m,d,ask,live,recent,more:waiting-(d?.status==='queued'?1:0)};});
      const busy=rows.filter(r=>r.live||r.ask||r.recent),idle=rows.filter(r=>!(r.live||r.ask||r.recent));
      const count=k=>rows.filter(r=>r.d?.status===k&&(k!=='done'||r.recent)).length;  // members, not tasks
      const sum=[count('running')&&`작업 중 ${count('running')}`,count('queued')&&`대기 ${count('queued')}`,rows.filter(r=>r.ask).length&&`승인 필요 ${rows.filter(r=>r.ask).length}`,count('failed')&&`실패 ${count('failed')}`,count('done')&&`최근 완료 ${count('done')}`].filter(Boolean).join(' · ')||'지금 맡긴 일 없음';
      const row=r=>{const st=r.d?.status||'queued';return `<li class="tp-row tp-${r.ask?'ask':st}"><button type="button" class="tp-open" data-open-agent="${esc(r.m.id)}" title="${esc(r.m.name)} 대화 열기"><span class="tp-mark" aria-hidden="true">${r.ask?'?':ICON[st]}</span><b>${esc(r.m.name)}</b><span class="tp-task" title="${esc(r.d?.summary||r.d?.task||'')}">${esc(r.ask?'승인을 기다립니다':r.m.status==='running'&&r.m.currentActivity?'지금 · '+r.m.currentActivity.label:(st==='done'||st==='failed'||st==='stopped')&&r.d.summary?r.d.summary:r.d?.task||'')}</span><span class="tp-state">${r.ask?'승인 필요':WORD[st]}${r.more>0?` · 다음 ${r.more}건 대기`:''}${r.d?` · <span class="tp-age" data-since="${r.d.status==='running'?(r.d.startedAt||r.d.ts):r.d.status==='queued'?r.d.ts:(r.d.finishedAt||r.d.ts)}"></span>`:''}</span></button></li>`;};
      html=`<div class="tp-head"><button type="button" class="tp-toggle" aria-expanded="${open}" data-tp-toggle>팀원 ${rows.length}명 <span>${esc(sum)}</span><i aria-hidden="true">${open?'▾':'▸'}</i></button>${['claude','codex'].includes(a.provider)?'<span class="tp-hint">팀원에게 맡기라고 말하면 직접 나눠 줍니다</span>':'<span class="tp-hint">팀원 대화에서 직접 작업을 요청할 수 있습니다</span>'}</div>${open?`<ul class="tp-list">${busy.map(row).join('')}</ul>${idle.length?`<p class="tp-idle">쉬는 팀원 ${idle.length}명 · ${idle.map(r=>`<button type="button" data-open-agent="${esc(r.m.id)}">${esc(r.m.name)}</button>`).join(', ')}</p>`:''}`:''}`;
    }else if(teams.length){
      const d=[...dl].reverse().find(x=>x.member===a.id&&(x.status==='queued'||x.status==='running')),lead=agent(teams[0].lead);
      if(d&&agent(d.lead))html=`<p class="tp-member">← <button type="button" data-open-agent="${esc(d.lead)}">${esc(agent(d.lead).name)}</button> 팀장이 맡긴 일 · 끝나면 결과가 팀장에게 자동으로 갑니다</p>`;
      else if(lead)html=`<p class="tp-member">팀장 · <button type="button" data-open-agent="${esc(lead.id)}">${esc(lead.name)}</button> · 이 대화의 완료 알림은 팀장이 정리해서 보냅니다</p>`;
    }
    const pending=a?.pendingReplies|| (a?.pendingReply?[a.pendingReply]:[]);
    if(pending.length)html=`<p class="tp-pending" role="status">보고 대기 · ${pending.length}건 · 팀원 보고를 종합한 뒤 다음 단계로 넘깁니다</p>`+html;
    p.classList.toggle('hidden',!html);if(html!==lastHtml){p.innerHTML=html;lastHtml=html;}
    ages(p);
  }
  function ages(p){for(const s of p.querySelectorAll('.tp-age[data-since]')){const t=ago(+s.dataset.since);if(s.textContent!==t)s.textContent=t;}}
  document.addEventListener('click',e=>{if(e.target.closest('[data-tp-toggle]')){open=!open;localStorage.setItem('pitcrew.teamPanel',open?'1':'0');lastHtml='';render(agent(state.selected));}});
  setInterval(()=>{const p=document.getElementById('teamPanel');if(!document.hidden&&p&&!p.classList.contains('hidden'))ages(p);},15000);
  globalThis.PitCrewTeamPanel={render};
})();
