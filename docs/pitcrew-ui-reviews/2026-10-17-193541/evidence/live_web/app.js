'use strict';
const $ = (s, root=document) => root.querySelector(s);
const esc = s => String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const state={projectId:localStorage.getItem('pitcrew.project')||'default',agents:[],providers:[],selected:localStorage.getItem('pitcrew.selected'),detail:null,token:'',refs:{},drafts:{},compare:null,models:{},shared:'',approvals:[],polling:false,detailHash:'',compareHash:'',contextHash:'',approvalHash:''};
try{state.drafts=JSON.parse(localStorage.getItem('pitcrew.drafts')||'{}');state.refs=JSON.parse(localStorage.getItem('pitcrew.refs')||'{}');}catch{state.drafts={};state.refs={};}
function saveDrafts(){try{localStorage.setItem('pitcrew.drafts',JSON.stringify(state.drafts));localStorage.setItem('pitcrew.refs',JSON.stringify(state.refs));}catch{}}
const statusNames={idle:'대기',running:'작업 중',waiting:'입력 대기',done:'완료',error:'실행 실패',interrupted:'중단됨',observing:'외부 대화',stopping:'중단 중'};
const busy=a=>['running','waiting','stopping'].includes(a?.status);
const agent=id=>state.agents.find(a=>a.id===id);
const providerName=id=>state.providers.find(p=>p.id===id)?.name||id;
const initials=a=>a.provider==='claude'?'C':a.provider==='codex'?'⌘':a.provider.slice(0,1).toUpperCase();
const avatarClass=a=>a.provider==='codex'?'codex':a.provider==='claude'?'claude':'other';
const stamp=ts=>{if(!ts)return '';const d=new Date(typeof ts==='number'?ts*1000:ts);return Number.isNaN(d.getTime())?'':d.toLocaleTimeString('ko-KR',{hour:'2-digit',minute:'2-digit'});};
const elapsed=t=>{const s=Math.max(0,Math.round(Date.now()/1000-t));return s<60?`${s}초`:s<3600?`${Math.floor(s/60)}분`:`${Math.floor(s/3600)}시간 ${Math.floor(s%3600/60)}분`;};
const dateStamp=ts=>new Date(ts*1000).toLocaleString('ko-KR',{month:'short',day:'numeric',hour:'2-digit',minute:'2-digit'});
let toastTimer;
function hideToastWhenIdle(){
  const banner=$('#toast');
  if(banner.contains(document.activeElement)){toastTimer=setTimeout(hideToastWhenIdle,1000);return;}
  banner.classList.add('hidden');measureToastSpace();
}
function toast(message){$('#toast').textContent=message;$('#toast').classList.remove('hidden');measureToastSpace();clearTimeout(toastTimer);toastTimer=setTimeout(hideToastWhenIdle,5500);}
// Reserve the banner's measured height on narrow screens, including undo notices
// written directly by other modules. This never changes keyboard focus.
function measureToastSpace(){
  const banner=$('#toast');
  document.documentElement.style.setProperty('--toast-space',banner.classList.contains('hidden')?'0px':Math.ceil(banner.getBoundingClientRect().height)+'px');
}
// Call synchronously on show, before callers scroll their inline feedback into
// view. The observer still handles appended actions and later content changes.
new ResizeObserver(measureToastSpace).observe($('#toast'));
window.addEventListener('resize',measureToastSpace);
async function api(path, data, project=state.projectId){path+=(path.includes('?')?'&':'?')+'project='+encodeURIComponent(project||'default');const options=data===undefined?{}:{method:'POST',headers:{'Content-Type':'application/json','X-PitCrew-Token':state.token},body:JSON.stringify(data)};return PitCrewConnection.request('/api/hub/'+path,options,path.startsWith('models?')?120000:data===undefined?12000:60000);}
function markdown(input){return PitCrewReader.render(input);}
function modal(title,html,eyebrow='PITCREW'){state.modalVersion=(state.modalVersion||0)+1; $('#modal').classList.remove('model-dialog','team-dialog','flow-dialog','collab-dialog','goal-dialog');$('#modalTitle').textContent=title;$('#modalEyebrow').textContent=eyebrow;$('#modalContent').innerHTML=html;if(!$('#modal').open)$('#modal').showModal();$('#modalClose').focus();}
function closeModal(){state.modalVersion=(state.modalVersion||0)+1;$('#modal').close();}
function formError(e){const el=$('#formError');if(el){el.setAttribute('role','alert');el.textContent=e.message||e;}else toast(e.message||e);}
function refs(){return state.refs[state.selected]||[];}
function renderRefs(){$('#references').innerHTML=refs().map((r,i)=>`<span class="ref-chip">↗ ${esc(r.name)}의 메시지 <button type="button" data-remove-ref="${i}" aria-label="참조 제거">×</button></span>`).join('');saveDrafts();}
function select(id){
  globalThis.PitCrewWorkOverview?.show(false);
  if(id){state.workspaceClosed=false;if(typeof workspaceLayout==='function')workspaceLayout().closed=false;}
  if(state.selected)state.drafts[state.selected]=$('#prompt').value;
  const selectedTeam=(state.teams||[]).find(t=>t.id===state.selectedTeam);
  if(id&&((selectedTeam&&!selectedTeam.members.includes(id))||(state.selectedTeam==='__unassigned'&&(state.teams||[]).some(t=>t.members.includes(id))))){state.selectedTeam='';localStorage.setItem('pitcrew.team.v2','');}
  state.selected=id;localStorage.setItem('pitcrew.selected',id);state.detail=null;state.detailHash='';state.contextHash='';state.approvalHash='';
  $('#prompt').value=state.drafts[id]||'';$('#messages').innerHTML='';renderRefs();globalThis.PitCrewAttach?.render();globalThis.PitCrewComposer?.render();globalThis.PitCrewComposer?.close();renderAgents();renderHead();globalThis.PitCrewCycle?.render();globalThis.PitCrewArtifacts?.sync();refreshDetail();
}
function renderAgents(){
  if(state.draggingLayout)return;
  if(typeof renderTeamFilter==='function')renderTeamFilter();
  const visible=typeof teamVisibleIds==='function'?teamVisibleIds():null;
  const list=(typeof orderedAgents==='function'?orderedAgents():state.agents).filter(a=>(globalThis.PitCrewSidebar?.accepts(a)??true)&&(!visible||visible.includes(a.id))&&(!state.chatQuery||`${a.name} ${a.role} ${providerName(a.provider)}`.toLowerCase().includes(state.chatQuery)));
  // "확인 필요" first (approval / question waiting, failed, unread answer — research r4: Cursor "Needs Attention"),
  // then the rest with each team's members tucked under their lead.
  const asking=new Set((state.approvals||[]).map(x=>x.agent));
  const ledMember=a=>(state.teams||[]).some(t=>t.lead&&t.lead!==a.id&&t.members.includes(a.id));  // its results go to the lead
  const needs=a=>asking.has(a.id)||a.status==='error'||(a.id!==state.selected&&!ledMember(a)&&!!globalThis.PitCrewNotify?.unread(a.id));
  const attention=list.filter(needs),others=list.filter(a=>!needs(a)),member=new Set(),placed=new Set(),ordered=[];
  for(const a of others){if(placed.has(a.id))continue;
    const leadOf=(state.teams||[]).filter(t=>t.lead===a.id);
    if(!leadOf.length&&(state.teams||[]).some(t=>t.lead&&t.lead!==a.id&&t.members.includes(a.id)&&others.some(x=>x.id===t.lead)))continue;  // placed under its lead
    ordered.push(a);placed.add(a.id);
    for(const m of others)if(!placed.has(m.id)&&leadOf.some(t=>t.members.includes(m.id))){ordered.push(m);placed.add(m.id);member.add(m.id);}}
  for(const a of others)if(!placed.has(a.id)){ordered.push(a);placed.add(a.id);}
  // A member's line says what its lead handed it (latest hand-off, while live or within 15 min), not just the provider.
  const handed={};for(const d of state.delegations||[])handed[d.member]=d;
  const task=a=>{const d=handed[a.id];return d&&(['queued','running'].includes(d.status)||Date.now()/1000-(d.finishedAt||d.ts)<900)?d.task:'';};
  state.sidebarPinned=new Map([...attention.map(a=>[a.id,'확인 필요 묶음은 상태로 정렬됩니다. 확인하면 원래 자리로 돌아갑니다.']),...[...member].map(id=>[id,'팀원은 팀장 아래에 붙어 있습니다. 순서를 바꾸려면 팀장을 옮기세요.'])]);
  const row=a=>`<div class="agent-row${member.has(a.id)?' member-row':''}" draggable="true" data-drag-agent="${esc(a.id)}"><button draggable="true" data-drag-agent="${esc(a.id)}" class="agent-card ${a.id===state.selected?'active':''}" data-agent="${esc(a.id)}" title="${esc(a.name)} · ${esc(a.role||'역할 미지정')} · ${esc(statusNames[a.status])}"><span class="avatar ${avatarClass(a)}">${initials(a)}</span><span class="agent-info"><span class="agent-name"><span class="agent-name-text">${esc(globalThis.PitCrewSidebar?.label(a)||a.name)}</span>${(state.teams||[]).some(t=>t.lead===a.id)?'<span class="lead-tag" title="팀장 — 이 팀의 알림을 받는 대화">팀장</span>':''}${globalThis.PitCrewNotify?.unread(a.id)?'<i class="unread-dot" aria-label="미확인 응답"></i>':''}</span><span class="agent-sub"><i class="status-dot ${esc(a.status)}"></i>${esc(statusNames[a.status]||a.status)}${task(a)?`<span class="sub-task" title="${esc(task(a))}">· ${esc(task(a))}</span>`:globalThis.PitCrewSidebar?.badge(a)||`<span>· ${esc(providerName(a.provider))}</span>`}</span></span></button><button class="row-menu" data-chat-menu="${esc(a.id)}" aria-haspopup="menu" aria-expanded="false" aria-label="${esc(a.name)} 대화 메뉴">⋯</button></div>`;
  const agentHtml=(attention.length?`<p class="list-group attention">확인 필요 <span>${attention.length}</span></p>${attention.map(row).join('')}${ordered.length?'<p class="list-group">대화</p>':''}`:'')+ordered.map(row).join('');
  const empty=state.chatQuery?'<p class="list-empty">이름·역할 검색 결과가 없습니다. 본문은 대화 내 찾기를 이용하세요.</p>':'<p class="list-empty">이 보기에는 대화가 없습니다.</p>';
  if($('#agents').innerHTML!==(agentHtml||empty))$('#agents').innerHTML=agentHtml||empty;
  if(typeof renderWorkspaceTabs==='function')renderWorkspaceTabs();
  $('#teamSummary').textContent=`${state.agents.filter(a=>!a.source).length}명의 에이전트 · ${state.agents.filter(busy).length}명 작업 중`;
}
function renderHead(){
  globalThis.PitCrewRefresh?.render(agent(state.selected));
  const a=agent(state.selected);renderAgentNotice(a);if(!a){$('#selectedName').textContent='열린 대화가 없습니다';$('#selectedMeta').textContent='왼쪽 목록에서 대화를 열거나 새 팀을 구성하세요.';$('#modelBar').innerHTML='';$('#messages').innerHTML=window.PitCrewNavigation?.empty()||'<div class="welcome"><h2>대화를 선택하세요</h2></div>';$('#contextContent').innerHTML='';$('#composer').classList.add('hidden');$('#observerNotice').classList.add('hidden');return;}
  $('#selectedName').textContent=globalThis.PitCrewSidebar?.label(a)||a.name;$('#selectedAvatar').className='avatar '+avatarClass(a);$('#selectedAvatar').textContent=initials(a);
  $('#selectedMeta').textContent=[providerName(a.provider),a.model||'기본 모델',a.source?'외부 기록':({read:'읽기',work:'작업',full:'전체 권한'}[a.mode]||a.mode)].join(' · ');$('#selectedMeta').title=(a.role||'')+' · '+(statusNames[a.status]||a.status);$('#selectedName').title=a.name+' · '+(statusNames[a.status]||a.status);
  const model=state.models[a.provider]?.models.find(m=>m.id===a.model);
  $('#modelBar').innerHTML=`<button class="model-pill" data-settings> ${esc(providerName(a.provider))} / ${esc(model?.name||a.model||'기본 모델')} ▾</button>${a.effort?`<button class="model-pill" data-settings>추론 ${esc(a.effort)} ▾</button>`:''}${a.serviceTier?`<span class="model-pill">${esc(a.serviceTier)}</span>`:''}<span class="mode">${a.source?'◉ 읽기 전용 연결':a.mode==='full'?'◈ 전체 권한 · 자동 실행':a.mode==='work'?'◈ 파일 작업 · 승인 요청':'◇ 읽기 · 분석 모드'}</span>`;
  if(a.originSource)$('#modelBar').insertAdjacentHTML('beforeend',`<span class="tiny-tag" title="${esc(a.session)}">원래 세션 연결 · 외부 기록 동기화</span>`);
  if(a.source)$('#modelBar').innerHTML=`<span class="model-pill">${esc(providerName(a.provider))} · 외부 대화 기록</span><span class="mode">◉ 읽기 전용 연결</span>`;
  // While working, sending queues the message (it starts when the current run finishes).
  $('#send').disabled=!!state.sending?.has(a.id)||a.status==='stopping';$('#send').innerHTML=busy(a)?'대기열에 추가 <span>⏎</span>':'보내기 <span>↑</span>';$('#stop').classList.toggle('hidden',!busy(a));$('#stop').disabled=a.status==='stopping';
  globalThis.PitCrewQueue?.render(a);globalThis.PitCrewComposer?.renderTools();
  $('#prompt').disabled=!!a.source;$('#prompt').placeholder=`${globalThis.PitCrewSidebar?.label(a)||a.name}에게 메시지`;
  $('#composeTarget').textContent=a.name+'에게 메시지';$('#composer').classList.toggle('hidden',!!a.source);$('#observerNotice').classList.toggle('hidden',!a.source);
}
function sessionConflict(error){return /(?:thread[^\n]*already has an active writer|session[^\n]*(?:already (?:in use|being used)|is (?:in use|locked)))/i.test(error||'');}
async function continueConversation(){
  const id=state.selected,project=state.projectId;if(!id||state.continuing)return;state.continuing=true;
  try{const saved=await api('continue-conversation',{agent:id},project);if(project!==state.projectId)return;const draft=state.selected===id?$('#prompt').value:state.drafts[id]||'';state.drafts[saved.id]=draft;state.refs[saved.id]=(state.refs[id]||[]).map(r=>({...r}));saveDrafts();await poll();select(saved.id);toast('새 대화를 만들었습니다. 이전 대화의 최근 내용이 연결됩니다. 입력을 확인한 뒤 보내세요.');}
  catch(error){toast(error.message);}finally{state.continuing=false;}
}
function renderAgentNotice(a){
  const el=$('#agentNotice');if(!el)return;
  let title='',description='',action='',detail='';
  if(a){
    const sendError=state.sendErrors?.[a.id],waiting=state.approvals.filter(x=>x.agent===a.id).length;
    if(sessionConflict(sendError||a.error)){title='다른 곳에서 사용 중인 대화입니다';description=`이 대화가 ${providerName(a.provider)} 앱 등 다른 곳에서 열려 있거나 세션 ID가 사용 중입니다. 그 창의 작업을 마치고 닫은 뒤 다시 보내거나, 새 대화로 이어가세요. 새 대화에는 이전 기록을 연결하며 입력 초안은 유지됩니다.${a.provider==='claude'?' Claude에서는 기존 세션 ID와 충돌한 경우에도 이 오류가 날 수 있습니다.':''}`;detail=sendError||a.error;action='continue';}
    else if(sendError){title='전송 결과를 확인해 주세요';description='입력은 남겨 두었습니다. 대화에 이미 반영되었는지 확인한 뒤 다시 보내세요.';detail=sendError;action='draft';}
    else if(waiting){title=`응답할 요청 ${waiting}건`;description='승인 또는 질문에 답하면 작업이 계속됩니다.';action='approval';}
    // Interrupted with a held queue: the queue bar already says so and offers "이어서 보내기" — one card, not two.
    else if(a.status==='interrupted'&&(a.queue||[]).length){}
    else if(a.status==='error'||a.status==='interrupted'){title=a.status==='error'?'작업을 완료하지 못했습니다':'작업이 중단되었습니다';description='저장된 대화는 유지됩니다. 마지막 결과를 확인하고 다음 지시를 보내세요.';detail=a.error||'';action='draft';}
    else if(!a.source&&state.providers.find(p=>p.id===a.provider)?.ready===false){title='연결 도구 설정이 필요합니다';description='이 제공자의 CLI 설치 또는 연결 설정을 확인하세요.';action='setup';}
  }
  el.classList.toggle('hidden',!title);if(!title){el.innerHTML='';return;}
  const html=`<div><strong>${esc(title)}</strong><details><summary>이유와 해결 방법</summary><p>${esc(description)}</p>${detail?`<pre>${esc(detail)}</pre>`:''}</details></div><button type="button" data-notice-action="${action}">${action==='continue'?'새 대화로 이어가기':action==='approval'?'요청 확인':action==='setup'?'연결 안내':'입력 확인'}</button>`;
  if(el.innerHTML!==html)el.innerHTML=html;
}
// Attachment blocks in a user prompt are for the agent; people see a gallery instead.
const FILE_BLOCK=/\n+\[(첨부 파일|참조 파일)[^\]]*\]\n(?:- [^\n]*(?:\n|$))+/g;
function visibleText(m){return m.files?.length?String(m.text||'').replace(FILE_BLOCK,'').trimEnd():m.text;}
const isImage=p=>/\.(png|jpe?g|gif|webp|bmp)$/i.test(p||'');
function fileGallery(m){
  const files=m.files||[];if(!files.length)return '';
  const imgs=files.filter(f=>isImage(f.path)),docs=files.filter(f=>!isImage(f.path));
  return `<div class="msg-files">${imgs.map(f=>`<button type="button" class="msg-image-btn" data-lightbox="/local-image?path=${encodeURIComponent(f.path)}" title="${esc(f.name)}"><img class="msg-image" src="/local-image?path=${encodeURIComponent(f.path)}" alt="${esc(f.name)}" loading="lazy"></button>`).join('')}${docs.map(f=>`<span class="msg-file" title="${esc(f.path)}">📄 ${esc(f.name)}</span>`).join('')}</div>`;
}
// What this conversation sent to teammates, shown where it happened (receivers show the origin badge).
const OUT_KIND={handoff:'결과를 넘김',delegate:'일을 맡김',cycle:'다음 단계로 넘김',research:'리서치를 맡김',forward:'메시지를 전달',mention:'참조로 쓰임',message:'메시지를 보냄',queue:'대기열에 넣음'};
function eventHtml(m){
  // Keep a durable, linked hand-off in the transcript after the live team panel changes.
  return `<div class="msg-event" data-event="${esc(m.id)}"><span class="msg-event-mark" aria-hidden="true">↗</span><span>${esc(OUT_KIND[m.kind]||'보냄')}</span><button type="button" class="msg-event-to" data-open-message="${esc(m.messageId)}" data-open-agent="${esc(m.to)}">${esc(m.toName||'팀원')}</button><span class="msg-event-text">${esc(m.text||'')}</span><time>${stamp(m.ts)}</time></div>`;
}
const kTok=n=>n==null?'?':n>=1000?(n/1000).toFixed(n>=10000?0:1)+'k':String(n);
function usageHtml(u){
  if(!u)return '';const parts=[u.model,u.cost!=null?'$'+Number(u.cost).toFixed(Number(u.cost)<0.1?3:2):'',u.input!=null?`입력 ${kTok(u.input)}${u.cached?` (캐시 ${kTok(u.cached)})`:''} → 출력 ${kTok(u.output)}${u.requests>1?` · 요청 ${u.requests}회`:''}`:''].filter(Boolean);
  return parts.length?`<span class="turn-usage" title="이 턴의 모델·비용·토큰 (제공자 보고값)">${esc(parts.join(' · '))}</span>`:'';
}
const IN_KIND={handoff:'결과를 넘겨받음',delegate:'팀장이 맡긴 일',cycle:'사이클 이전 단계에서 받음',research:'리서치 요청을 받음',message:'메시지를 받음',queue:'대기열로 받음'};
function incomingHtml(m,a){
  const o=m.origin,text=visibleText(m),split=text.indexOf('\n\n['),lead=(split>0?text.slice(0,split):text).trim(),rest=split>0?text.slice(split).trim():'';
  return `<article class="message incoming" data-message="${esc(m.id)}"><div class="incoming-head"><span class="msg-event-mark" aria-hidden="true">←</span><button type="button" class="msg-event-to" data-open-agent="${esc(o.agent)}" title="보낸 대화 열기">${esc(o.name||'팀원')}</button><span>${esc(IN_KIND[o.kind]||'받음')}</span><time>${stamp(m.ts)}</time></div><div class="message-body incoming-lead">${markdown(lead)}</div>${rest?`<details class="incoming-more"><summary>함께 온 보고 펼치기 <small>· ${rest.length.toLocaleString('ko-KR')}자</small></summary><div class="message-body">${markdown(rest)}</div></details>`:''}${fileGallery(m)}<div class="message-actions"><button data-copy="${esc(m.id)}" data-owner="${esc(a.id)}">복사</button><button data-forward="${esc(m.id)}" data-owner="${esc(a.id)}">↗ 전달</button></div></article>`;
}
function messageHtml(m,a,comparison=false,turn={}){
  if(m.role==='event')return eventHtml(m);
  if(m.role==='user'&&m.origin?.agent)return incomingHtml(m,a);
  if(m.role==='context')return `<article class="message tool"><details class="tool-box" data-tool="${esc(m.id)}"><summary>◈ ${esc(m.title||'앱 컨텍스트')} · ${stamp(m.ts)}</summary><pre>${esc(m.text)}</pre><div class="message-actions"><button data-forward="${esc(m.id)}" data-owner="${esc(a.id)}">↗ 전달</button></div></details></article>`;
  if(m.role==='tool')return toolBlockHtml([m],a);
  if(m.role==='system'&&(m.error||sessionConflict(m.text))){
    const current=!comparison&&(a.error===m.text||sessionConflict(a.error)&&sessionConflict(m.text));
    if(current&&!state.sendErrors?.[a.id])return '';
    return `<article class="message system error-record" data-message="${esc(m.id)}"><details><summary>${current?'오류 기록':'이전 실행 오류'} · ${stamp(m.ts)}</summary><pre>${esc(m.text)}</pre></details></article>`;
  }
  const speaker=[providerName(a.provider),(m.turnUsage?.model||a.actualModel||a.model||'').replace(/^claude-/,'')].filter(Boolean).join(' · ');
  const who=m.role==='user'?'나':m.role==='system'?'시스템':speaker||a.name;
  const cont=m.role==='assistant'&&turn.cont,quiet=m.role==='assistant'&&turn.notLast;
  return `<article class="message ${esc(m.role)}${cont?' cont':''}${quiet?' mid':''}" data-message="${esc(m.id)}">${cont?'':`<div class="message-header">${m.role==='assistant'?`<i class="speaker-avatar" aria-hidden="true">${globalThis.PitCrewIcons?.html(a.provider==='codex'?'terminal':'sparkles')||''}</i>`:''}<b>${esc(who)}</b>${m.replyTo?`<span class="reply-to"><span aria-hidden="true">←</span><button type="button" class="msg-event-to" data-open-agent="${esc(m.replyTo.agent)}">${esc(m.replyTo.name||'팀원')}</button><span>${esc({handoff:'의 인계에 대한 답',delegate:'이 맡긴 일에 대한 답',research:'의 리서치 요청에 대한 답',message:'의 메시지에 대한 답'}[m.replyTo.kind]||'의 요청에 대한 답')}</span></span>`:''}<span>${stamp(m.ts)}</span>${m.streaming?'<span>응답 중</span>':''}</div>`}${globalThis.PitCrewFlow?.origin(m,a)||''}<div class="message-body">${markdown(m.role==='user'?visibleText(m):m.text)}</div>${m.role==='user'?fileGallery(m):''}${quiet?'':`<div class="message-actions"><button data-copy="${esc(m.id)}" data-owner="${esc(a.id)}">복사</button>${m.role==='assistant'?usageHtml(m.turnUsage):''}${m.role==='user'&&!a.source&&!comparison?`<button data-reuse="${esc(m.id)}" title="입력창에 다시 넣기 (자동으로 보내지 않음)">다시 사용</button>`:''}<button data-forward="${esc(m.id)}" data-owner="${esc(a.id)}">↗ 전달</button>${m.context?`<button data-context-message="${esc(m.id)}" data-owner="${esc(a.id)}">전달된 컨텍스트 ${m.context.snapshots?.length?`· 참조 ${m.context.snapshots.length}`:''}</button>`:''}</div>`}</article>`;
}
// Tool activity: a call and its result are one row; consecutive calls fold into one group.
// Older records: subagent events were titled "팀원" and labelled with the raw event name.
const toolTitle=m=>m.title==='팀원'?'하위 작업':(m.title||'도구'),toolLabel=m=>/^task_(started|progress|notification)$/.test(m.label||'')?'':(m.label||'');
const toolState=m=>{const s=String(m.status||'');return /error|fail/.test(s)?'error':/running|progress|started|pending/i.test(s)?'running':/interrupt|unconfirmed|declin|cancel/.test(s)?'stopped':'done';};
const TOOL_MARK={error:'!',running:'',stopped:'■',done:'·'},TOOL_STATE={error:'실패',running:'실행 중',stopped:'중단',done:'실행 종료'};
function toolOutputPreview(m,recent=false){
  if(!m?.output||!String(m.text||'').trim())return '';
  const text=String(m.text),chars=Array.from(text.slice(0,2000).trim().replace(/\s+/g,' '));
  const preview=chars.slice(0,180).join('')+(chars.length>180||text.length>2000?'…':'');
  return `<span class="tool-output-preview"><span>${recent?'최근 출력 · '+esc(toolTitle(m)):'출력'}</span><span>${esc(preview)}</span></span>`;
}
function toolImages(items){return items.flatMap(m=>m.images||[]).map(n=>`<a class="tool-image" href="/media/${esc(n)}" target="_blank" rel="noopener" data-lightbox="/media/${esc(n)}"><img src="/media/${esc(n)}" alt="도구가 만든 이미지" loading="lazy"></a>`).join('');}
function diffHtml(d){
  return (d?.files||[]).map(f=>`<div class="tool-diff"><div class="diff-file"><span>${esc(String(f.path).replace(/\\/g,'/').split('/').slice(-3).join('/'))}</span><span class="diff-stat">${f.note?`<small>${esc(f.note)}</small> `:''}<b class="add">+${f.add}</b> <b class="del">−${f.del}</b></span></div><pre>${f.lines.map(l=>`<span class="${l.startsWith('+')?'add':l.startsWith('-')?'del':l.startsWith('@@')?'hunk':''}">${esc(l)}</span>`).join('\n')}</pre></div>`).join('');
}
const diffStat=d=>{const f=d?.files||[];if(!f.length)return '';return `<span class="diff-stat"><b class="add">+${f.reduce((s,x)=>s+x.add,0)}</b> <b class="del">−${f.reduce((s,x)=>s+x.del,0)}</b></span>`;};
function toolItemHtml(m,a){
  const full=state.fullTool?.[m.id],src=full||m,st=toolState(m),text=src.text,truncated=m.truncated&&!full;
  // The "full" button is independent of output: a long input with an empty result must still open.
  const input=m.diff?`<div class="tool-part"><span>변경</span>${diffHtml(m.diff)}</div>`:m.output&&src.input?`<div class="tool-part"><span>입력</span><pre data-part="input">${esc(src.input)}</pre></div>`:'';
  const body=text?`<div class="tool-part">${input?'<span>결과</span>':''}<pre data-part="output">${esc(text)}</pre></div>`:'<p class="tool-empty">출력 없음</p>';
  const more=truncated?`<div class="tool-part"><button type="button" class="quiet" data-tool-full="${esc(m.id)}" data-owner="${esc(a.id)}">전체 보기</button></div>`:'';
  return `<details class="tool-box tool-${st}" data-tool="${esc(m.id)}"><summary><span class="tool-summary-main"><span class="tool-mark" aria-hidden="true">${TOOL_MARK[st]}</span><b>${esc(toolTitle(m))}</b><span class="tool-label">${esc(toolLabel(m))}</span>${diffStat(m.diff)}<span class="tool-count" title="도구 실행 상태이며 게임 검증 판정은 아닙니다">${TOOL_STATE[st]}</span></span>${toolOutputPreview(m)}</summary>${input}${body}${more}</details>`;
}
function planHtml(m){
  const icon={completed:'✓',inProgress:'◐',pending:'○'},items=m.plan||[],done=items.filter(s=>s.status==='completed').length;
  return `<article class="message plan" data-message="${esc(m.id)}"><div class="plan-head"><b>계획</b><span>${done}/${items.length} 완료</span><span class="plan-bar" aria-hidden="true"><i style="width:${items.length?Math.round(done/items.length*100):0}%"></i></span></div>${m.text?`<p class="plan-note">${esc(m.text)}</p>`:''}<ol class="plan-list">${items.map(s=>`<li class="plan-${esc(s.status)}"><span class="plan-icon" aria-label="${s.status==='completed'?'완료':s.status==='inProgress'?'진행 중':'대기'}">${icon[s.status]||'○'}</span><span>${esc(s.status==='inProgress'&&s.active?s.active:s.step)}</span></li>`).join('')}</ol></article>`;
}
function toolBlockHtml(items,a){
  if(items.length===1)return `<article class="message tool">${toolItemHtml(items[0],a)}${toolImages(items)}</article>`;
  const states=items.map(toolState),errors=states.filter(s=>s==='error').length,running=states.includes('running'),last=items[items.length-1];
  const kinds=[...new Set(items.map(toolTitle))],names=kinds.slice(0,4).join(' · ')+(kinds.length>4?` 외 ${kinds.length-4}`:'');
  const active=[...items].reverse().find(m=>toolState(m)==='running')||last;
  const head=running?`<span class="tool-mark" aria-hidden="true"></span><b>${esc(toolTitle(active))}</b><span class="tool-label">${esc(toolLabel(active))}</span><span class="tool-count">실행 중 · ${items.length}개${active.startedAt?` · ${Math.max(0,Math.round(Date.now()/1000-active.startedAt))}초째`:''}</span>`:`<span class="tool-mark" aria-hidden="true">${errors?'!':states.includes('stopped')?'■':'·'}</span><b>도구 ${items.length}개</b><span class="tool-label">${esc(names)}</span>${(()=>{const label=toolLabel([...items].reverse().find(toolLabel)||{});return label?`<span class="tool-last" title="${esc(label)}">${esc(label)}</span>`:'';})()}<span class="tool-count">${states.includes('stopped')?'중단 포함':'실행 종료'}</span>`;
  const preview=toolOutputPreview([...items].reverse().find(m=>m.output&&String(m.text||'').trim()),true);
  return `<article class="message tool tool-group${running?' tool-running':''}${errors?' has-error':''}${!errors&&!running&&states.includes('stopped')?' tool-stopped':''}"><details class="tool-group-box" data-tool="group-${esc(items[0].id)}"><summary><span class="tool-summary-main">${head}${errors?`<span class="tool-count tool-error">실패 ${errors}</span>`:''}</span>${preview}</summary><p class="tool-output-note">도구 실행 상태입니다. 결과의 검증 여부는 출력에서 확인하세요.</p><div class="tool-list">${items.map(m=>toolItemHtml(m,a)).join('')}</div></details>${toolImages(items)}</article>`;
}
function messageBlocks(detail,comparison){
  const a=detail.agent,blocks=[];let run=[],ask=null,answered=false;
  // Last answer text of each turn (a turn ends at the next user message) carries the actions.
  const lastOfTurn=new Set();let lastText=null;
  for(const m of detail.messages){if(m.role==='user'){if(lastText)lastOfTurn.add(lastText);lastText=null;}else if(m.role==='assistant'&&(m.text||'').trim())lastText=m.id;}
  if(lastText)lastOfTurn.add(lastText);
  const flush=()=>{if(run.length){blocks.push({key:'tools-'+run[0].id,html:toolBlockHtml(run,a)});run=[];}};
  for(const m of detail.messages){if(m.role==='tool'&&m.plan?.length){flush();blocks.push({key:'plan-'+m.id,html:planHtml(m)});continue;}
    if(m.role==='tool'){run.push(m);continue;}flush();
    if(m.role==='user'){ask=m;answered=false;}
    const shown=m.role==='assistant'&&ask?.origin?.agent?{...m,replyTo:ask.origin}:m;
    const html=messageHtml(shown,a,comparison,m.role==='assistant'?{cont:answered,notLast:!lastOfTurn.has(m.id)&&!m.streaming}:{});
    if(m.role==='assistant'&&(m.text||'').trim())answered=true;
    if(html)blocks.push({key:'m-'+m.id,html});}
  flush();return blocks;
}
// Keyed update: only changed blocks are replaced, so selection, scroll and open rows survive polling.
function patchBlocks(el,blocks,target,expanded){
  state.renderCache=state.renderCache||{};const cache=state.renderCache[target]||(state.renderCache[target]=new Map());
  const keep=new Set(blocks.map(b=>b.key));
  for(const child of [...el.children])if(!keep.has(child.dataset.key)||cache.get(child.dataset.key)?.node!==child)child.remove();
  let prev=null;
  for(const b of blocks){
    let entry=cache.get(b.key);
    if(!entry||entry.html!==b.html||entry.node.parentNode!==el&&entry.node.parentNode){
      const t=document.createElement('template');t.innerHTML=b.html.trim();
      const node=t.content.childElementCount===1?t.content.firstElementChild:Object.assign(document.createElement('div'),{innerHTML:b.html});
      node.dataset.key=b.key;for(const d of node.querySelectorAll('details'))if(expanded.has(d.dataset.tool)||state.verbose&&d.matches('.tool-group-box,.tool-box'))d.open=true;
      if(entry?.node?.parentNode===el)entry.node.replaceWith(node);
      entry={node,html:b.html};cache.set(b.key,entry);
    }
    const want=prev?prev.nextSibling:el.firstChild;if(want!==entry.node)el.insertBefore(entry.node,want);prev=entry.node;
  }
  for(const k of [...cache.keys()])if(!keep.has(k))cache.delete(k);
}
function renderMessages(detail,target='#messages',comparison=false){
  const a=detail.agent,el=$(target),atBottom=el.scrollHeight-el.scrollTop-el.clientHeight<100;
  const expanded=new Set([...el.querySelectorAll('details[open]')].map(x=>x.dataset.tool));
  const blocks=[];
  if(detail.messages.length>=300||a.source||a.originSource)blocks.push({key:'note',html:`<p class="form-note">${esc(detail.historyNote||'최근 기록')} · ${detail.messages.length}개 기록</p>`});
  blocks.push(...messageBlocks(detail,comparison));
  let html='';
  if(!blocks.length)html=`<div class="welcome"><div class="welcome-mark">✳</div><span class="eyebrow">YOUR TEAM, IN CONTEXT</span><h2>${esc(a.name)} · 새로운 대화<br>다음 작업을 시작하세요.</h2><p>담당 역할: ${esc(a.role)}<br>역할과 자료를 설정하고, 필요한 대화를 연결해 주세요.</p><div class="suggestions"><button data-suggest="프로젝트를 읽고 현재 구조와 다음에 할 일을 정리해줘. 파일은 변경하지 마.">프로젝트를 파악하고 작업 계획 세우기 <span>↗</span></button><button data-suggest="내가 제공할 요구사항을 검토하고, 구현 전에 결정해야 할 내용을 정리해줘.">요구사항을 함께 구체화하기 <span>↗</span></button></div><div class="welcome-actions"><button data-setup-action="start" class="primary">작업 공간 준비하기</button><button data-setup-action="import">기존 대화 가져오기</button></div><div class="welcome-foot">기존 대화를 이어가거나 역할별로 새 작업을 시작하세요.</div></div>`;
  if(html)blocks.push({key:'welcome',html});
  const live=[...detail.messages].reverse().find(m=>m.role==='tool'&&toolState(m)==='running');
  // queued messages sit at the end of the conversation as "대기 중" bubbles (fresh queue from the last poll)
  if(!comparison)for(const q of (agent(a.id)||a).queue||[]){const from=q.origin?.name||(q.from&&agent(q.from)?.name)||(q.replyTo?.cycle?'사이클':'');
    blocks.push({key:'q-'+q.id,html:`<article class="message user queued" data-queued="${esc(q.id)}"><div class="message-header"><b>${from?esc(from)+'에게서':'나'}</b><span class="queued-tag">대기 중 · 지금 작업이 끝나면 자동으로 보냅니다</span></div><div class="message-body">${markdown(q.text)}</div><div class="message-actions"><button type="button" data-queue-edit="${esc(q.id)}" title="입력창으로 되돌려 고치기">편집</button><button type="button" data-queue-remove="${esc(q.id)}">보내지 않기</button></div></article>`});}
  if(busy(a))blocks.push({key:'pending',html:`<div class="pending"><i></i>${a.status==='running'&&live?`<b>${esc(live.title||'도구')}</b> ${esc(live.label||'')} · `:''}${a.status==='waiting'?'아래 요청에 응답해 주세요.':a.status==='stopping'?'작업을 중단하고 있습니다.':'작업 중입니다'+((a.turnStartedAt||a.runStartedAt)?` · ${elapsed(a.turnStartedAt&&a.turnStartedAt>=(a.runStartedAt||0)?a.turnStartedAt:a.runStartedAt)} 경과`:'')+'. 도구 실행과 응답이 여기에 표시됩니다.'}</div>`});
  const qi=blocks.findIndex(b=>b.key==='pending');if(qi>0){const [p]=blocks.splice(qi,1);const first=blocks.findIndex(b=>b.key.startsWith('q-'));blocks.splice(first<0?blocks.length:first,0,p);}
  patchBlocks(el,blocks,target,expanded);
  if(atBottom){
    el.scrollTop=el.scrollHeight;
    const position=el.scrollTop;
    // The composer/team panel can resize later in the same poll. Follow the
    // final layout only if the reader hasn't moved away in the meantime.
    requestAnimationFrame(()=>{if(el.isConnected&&el.scrollTop===position)el.scrollTop=el.scrollHeight;});
  }
}
function renderContext(d){
  const a=d.agent;$('#resumeSource').classList.toggle('hidden',!d.canResume);const hash=JSON.stringify([a,state.shared,state.teams,d.children]);if(hash===state.contextHash)return;state.contextHash=hash;
  if(window.PitCrewNavigation){const opened=new Set([...$('#contextContent').querySelectorAll('details[open]')].map(e=>e.querySelector('summary')?.textContent));$('#contextContent').innerHTML=PitCrewNavigation.context(d);for(const e of $('#contextContent').querySelectorAll('details'))if(opened.has(e.querySelector('summary')?.textContent))e.open=true;}
}
// Approval card: what / on what / why first; raw request folded.
function approvalBody(d){
  const s=d.summary;if(!s)return `<pre>${esc(d.command||d.reason||d.tool||d.method)}\n${esc(JSON.stringify(d.input||d,null,2))}</pre>`;
  const raw=JSON.stringify(d.input||Object.fromEntries(Object.entries(d).filter(([k])=>!['summary','key','proposal'].includes(k))),null,2);
  return `<p class="approval-action"><b>${esc(s.action)}</b>${s.cwd?` <span class="muted">· ${esc(s.cwd)}</span>`:''}</p>${s.target?`<pre class="approval-target">${esc(s.target)}</pre>`:''}${s.reason?`<p class="approval-reason">${esc(s.reason)}</p>`:''}<details class="approval-raw"><summary>요청 원문</summary><pre>${esc(raw)}</pre></details>`;
}
function renderApprovals(){
  const list=state.approvals.filter(x=>x.agent===state.selected);const hash=JSON.stringify(list);if(hash===state.approvalHash)return;state.approvalHash=hash;
  $('#approvals').innerHTML=list.map(item=>{
    const d=item.detail;const qs=d.questions||d.input?.questions||[];
    return `<section class="approval" data-request="${esc(item.id)}"><h3>${item.kind==='question'?'답변이 필요합니다':'실행 승인 요청'}</h3>${qs.length?qs.map((q,i)=>`<label class="field"><span>${esc(q.question||q.header)}</span><input data-question="${esc(q.id||q.question||String(i))}" placeholder="답변 입력" list="options-${i}"><datalist id="options-${i}">${(q.options||[]).map(o=>`<option value="${esc(o.label)}">${esc(o.description)}</option>`).join('')}</datalist></label>`).join(''):approvalBody(d)}<div class="buttons"><button data-decision="deny">거절</button>${item.kind!=='question'&&d.key?`<button data-decision="always" title="이 대화에서 ${esc(d.summary?.tool||d.key.split(':').pop())}를 다시 묻지 않습니다 · 입력창 위 칩에서 취소">항상 허용</button>`:''}<button class="primary" data-decision="allow">${item.kind==='question'?'답변 보내기':'이번만 허용'}</button></div></section>`;
  }).join('');
}
function focusApproval(){
  const target=document.querySelector('#approvals input')||document.querySelector('#approvals .approval h3');
  if(!target)return false;
  if(target.tagName!=='INPUT')target.setAttribute('tabindex','-1');
  target.focus();target.scrollIntoView({block:'nearest'});return true;
}
async function refreshDetail(strict=false){
  const id=state.selected,project=state.projectId;if(!id)return;
  const etag=t=>/^[0-9a-f]{40}$/.test(t||'')?'&etag='+t:'';
  try{let d=await api('detail?agent='+encodeURIComponent(id)+etag(state.detailHash));if(id!==state.selected||project!==state.projectId)return;
    if(d.unchanged&&!state.detail){state.detailHash='';d=await api('detail?agent='+encodeURIComponent(id));if(id!==state.selected||project!==state.projectId)return;}
    if(d.unchanged)d=state.detail;else{state.detail=d;const hash=d.etag||JSON.stringify(d.messages)+d.agent.status;if(hash!==state.detailHash){renderMessages(d);state.detailHash=hash;}}
    renderContext(d);renderApprovals();globalThis.PitCrewArtifacts?.sync();
    if(state.compare){const cid=state.compare;const c=await api('detail?agent='+encodeURIComponent(cid)+(state.compareDetail?.agent?.id===cid?etag(state.compareHash):''));if(cid!==state.compare||project!==state.projectId)return;if(!c.unchanged){state.compareDetail=c;const ch=c.etag||JSON.stringify(c.messages)+c.agent.status;if(ch!==state.compareHash){renderMessages(c,'#compareMessages',true);state.compareHash=ch;}}}
  }catch(e){if(strict)throw e;toast(e.message);}
}
async function syncWorkspace(){
    const pid=state.projectId;const b=await api('bootstrap?client='+(document.hidden?0:1)+','+(globalThis.PitCrewNotify?.canNotify()?1:0));if(pid!==state.projectId)return;state.loadedProject=pid;state.projects=b.projects||[];if(typeof renderProjectNavigation==='function')renderProjectNavigation();state.lastServerContact=Date.now();Object.assign(state,{teams:b.teams||[],delegations:b.delegations||[],cycles:b.cycles||[],alertLog:b.alertLog||[],agents:b.agents,providers:b.providers,token:b.token,shared:b.shared,approvals:b.approvals,activity:b.activity,project:b.project});$('#projectName').textContent=state.projects.find(p=>p.id===pid)?.name||b.project.split(/[\\/]/).pop();
    state.capabilities=b.capabilities||{};
    if(typeof renderTeamFilter==='function')renderTeamFilter();
    globalThis.PitCrewTeamPanel?.render(agent(state.selected));
      const visible=typeof teamVisibleIds==='function'?teamVisibleIds():null;const candidates=state.agents.filter(a=>!visible||visible.includes(a.id));
      if(!state.workspaceClosed&&!candidates.some(a=>a.id===state.selected)){if(state.selected)state.drafts[state.selected]=$('#prompt').value;const remembered=typeof workspaceLayout==='function'?workspaceLayout().selected:null;state.selected=candidates.find(a=>a.id===remembered)?.id||candidates[0]?.id;state.contextHash='';$('#messages').innerHTML='';state.detailHash='';$('#prompt').value=state.drafts[state.selected]||'';renderRefs();globalThis.PitCrewAttach?.render();globalThis.PitCrewComposer?.render();globalThis.PitCrewComposer?.close();}
    state.runtime=b.runtime;$('#runtimeSummary').textContent=b.runtime?.independent?`● 독립 실행 서비스 · ${state.agents.filter(busy).length}명 작업 중${state.approvals.length?' · 응답 대기 '+state.approvals.length+'건':''}`:'기존 실행 방식 · 서버 재시작 시 작업 중단';
    renderAgents();renderHead();globalThis.PitCrewWorkOverview?.render();globalThis.PitCrewNotify?.observe();await refreshDetail(true);if($('#modal').open&&$('#teamOverview')&&Date.now()-(state.teamAt||0)>5000)await refreshTeam();
}
async function poll(){if(syncLoop.pending)await syncLoop.pending;return syncLoop.run();}
function teamOverview(){modal('팀 상태와 하위 에이전트','<div id="teamOverview">팀 기록을 불러오는 중…</div>','TEAM / RUNTIME');$('#modal').classList.add('team-dialog');refreshTeam();}
async function refreshTeam(){
  const target=$('#teamOverview');if(!target)return;
  try{const team=await api('team');if($('#teamOverview')!==target)return;state.teamAt=Date.now();
    const opened=new Set([...target.querySelectorAll('details[open]')].map(d=>d.dataset.team));
    const runtime=team.runtime;const waiting=team.approvals||[];
    target.innerHTML=`<div class="runtime-card"><b>${runtime?.independent?'● 독립 실행 서비스가 연결되어 있습니다':'실행 서비스 확인 필요'}</b><p>${runtime?.independent?`서비스 PID ${Number(runtime.pid)} · ${dateStamp(runtime.startedAt)} 시작<br>화면 서버를 다시 시작하거나 브라우저를 닫아도 실행 서비스는 유지됩니다.`:'이 화면에서 실행 서비스 상태를 확인할 수 없습니다.'}</p></div>${waiting.length?`<section class="team-attention"><h3>응답이 필요한 요청 · ${waiting.length}</h3>${waiting.map(w=>`<button data-team-agent="${esc(w.agent)}">${esc(agent(w.agent)?.name||'에이전트')} · ${w.kind==='question'?'질문에 답하기':'승인 확인'} ↗</button>`).join('')}</section>`:''}<p class="form-note">외부 대화는 마지막 기록을 보여줍니다. 기록 시각만으로 작업 중이라고 판단하지 않습니다.</p><div class="team-tree">${team.groups.map(g=>{
      const a=g.agent;return `<section class="team-node"><div class="team-node-head"><span class="avatar ${avatarClass(a)}">${initials(a)}</span><div><b>${esc(a.name)}</b><small>${esc(providerName(a.provider))} · ${esc(a.role)}</small></div><span class="tiny-tag">${esc(statusNames[a.status]||a.status)}</span></div><p>${esc(a.lastTask||a.instructions||'아직 작업 지시가 없습니다.')}</p>${a.runStartedAt?`<small>최근 실행 ${dateStamp(a.runStartedAt)}${a.lastEventAt?' · 마지막 출력 '+dateStamp(a.lastEventAt):''}</small>`:''}<div class="team-node-actions"><button data-team-agent="${esc(a.id)}">대화 열기</button>${a.id!==state.selected?`<button data-compare="${esc(a.id)}">현재 대화와 비교</button>`:''}</div>${g.children.length?`<details data-team="${esc(a.id)}" ${opened.has(a.id)?'open':''}><summary>하위 에이전트 기록 · ${g.children.length}명</summary><div class="team-children">${g.children.map(c=>`<button ${c.agent?`data-team-agent="${esc(c.agent)}"`:`data-watch="${esc(c.source)}"`}><span>↳ ${esc(c.name)}</span><small>${esc(c.role||'하위 에이전트')} · ${dateStamp(c.updated)} 기록</small></button>`).join('')}</div></details>`:''}</section>`;
    }).join('')}</div>`;
  }catch(e){if($('#teamOverview')===target)target.textContent='팀 정보를 불러오지 못했습니다. '+e.message;}
}
async function send(event){
  event.preventDefault();const id=state.selected,project=state.projectId,a=agent(id);
  const raw=$('#prompt').value,value=raw.trim();state.sending=state.sending||new Set();
  const goalCommand=/^\/goal(?:\s|$)/.test(value),objective=goalCommand?value.slice(5).trim():'';
  if(goalCommand&&!objective&&globalThis.PitCrewComposer?.openGoal){globalThis.PitCrewComposer.close();globalThis.PitCrewComposer.openGoal();return;}
  if(goalCommand&&!state.capabilities?.simpleGoal)return toast('목표 기능 업데이트 적용을 기다리고 있습니다. 입력한 목표는 그대로 유지됩니다.');
  if(!value||!a||a.source||a.status==='stopping'||state.sending.has(id))return;const queued=busy(a);
  const files=globalThis.PitCrewAttach?.ready()||{pending:false,files:[]};if(files.pending)return toast('첨부 파일을 올리는 중입니다. 잠시 후 보내세요.');
  if(files.failed){globalThis.PitCrewAttach?.focusFailure();return toast('업로드에 실패한 첨부를 다시 올리거나 제거한 뒤 보내세요.');}
  const sentRefs=refs().map(r=>({agent:r.agent,message:r.message}));
  state.sending.add(id);state.sendErrors=state.sendErrors||{};delete state.sendErrors[id];$('#send').disabled=true;
  try{
    const payload={agent:id,text:value,refs:sentRefs,queue:queued,attachments:files.files,...(globalThis.PitCrewComposer?.payload(id)||{})};
    await api(goalCommand?'cycle':'send',goalCommand?{...payload,action:'start',mode:'goal',goal:objective}:payload,project);globalThis.PitCrewAttach?.clear(id);globalThis.PitCrewComposer?.clear(id);globalThis.PitCrewComposer?.noteSent(id,value);if(goalCommand)toast('목표를 시작했습니다. 이 대화에서 달성할 때까지 계속합니다.');else if(queued)toast('대기열에 넣었습니다. 지금 작업이 끝나면 자동으로 보냅니다.');
    const stillHere=state.selected===id&&state.projectId===project;
    if(stillHere){if($('#prompt').value===raw)$('#prompt').value='';state.drafts[id]=$('#prompt').value;}
    else if(state.drafts[id]===raw)state.drafts[id]='';
    state.refs[id]=(state.refs[id]||[]).filter(r=>!sentRefs.some(sent=>sent.agent===r.agent&&sent.message===r.message));
    saveDrafts();if(stillHere)renderRefs();
    if(state.projectId===project)await poll();
  }catch(e){state.sendErrors[id]=e.message;toast(e.message);}
  finally{state.sending.delete(id);renderHead();}
}
async function loadModels(provider,refresh=false){
  if(state.models[provider]&&!refresh)return state.models[provider];
  const result=await api('models?provider='+encodeURIComponent(provider)+(refresh?'&refresh=1':''));state.models[provider]=result;return result;
}
async function modelPicker(){
  const a=agent(state.selected);if(!a||a.source)return;
  modal(`${providerName(a.provider)} 모델 선택`,`<form id="modelPicker"><div class="picker-toolbar"><input id="modelSearch" aria-label="모델 검색" placeholder="모델 이름 또는 ID 검색"><button type="button" id="reloadCatalog">새로고침 ↻</button></div><div class="picker-layout"><div id="modelChoices" class="model-choices" aria-label="모델 목록">실제 모델 목록을 불러오는 중…</div><section id="modelOptions" class="model-options"></section></div><p id="pickerSource" class="form-note"></p><div id="formError" class="form-error" role="alert"></div><div class="picker-footer"><span id="selectionSummary">다음 메시지부터 적용됩니다.</span><button type="button" id="cancelModel">취소</button><button type="submit" id="applyModel" class="primary" disabled>적용</button></div></form>`,'MODEL / REASONING');
  $('#modal').classList.add('model-dialog');
  const form=$('#modelPicker');let rows=[],selected=a.model,effort=a.effort||'',tier=a.serviceTier||'',thinking=a.thinking||'adaptive',sequence=0;
  const current=()=>rows.find(m=>m.id===selected);
  function summary(){const m=current();$('#selectionSummary').textContent=m?`${m.name} · ${effort||m.defaultEffort||'기본 추론'}${tier?' · '+tier:''}`:'모델을 선택하세요.';}
  function drawList(){const q=$('#modelSearch').value.toLowerCase().trim();$('#modelChoices').innerHTML=rows.filter(m=>`${m.name} ${m.id} ${m.description}`.toLowerCase().includes(q)).map(m=>`<button type="button" class="model-choice ${m.id===selected?'selected':''}" data-pick-model="${esc(m.id)}" aria-pressed="${m.id===selected}"><span class="model-choice-title">${esc(m.name)}<span>${m.id===a.model?'현재 사용':m.default?'기본':''}</span></span><small>${esc(m.description)}</small><code>${esc(m.id)}</code></button>`).join('')||'<p class="form-note">검색 결과가 없습니다. 다른 이름으로 검색하세요.</p>';}
  function drawOptions(){const m=current();if(!m){$('#modelOptions').innerHTML='<p class="form-note">왼쪽에서 모델을 선택하세요. 현재 모델이 목록에 없다면 기존 설정은 유지됩니다.</p>';$('#applyModel').disabled=true;summary();return;}
    if(!(m.efforts||[]).some(e=>e.id===effort))effort='';if(!(m.tiers||[]).some(t=>t.id===tier))tier='';
    $('#modelOptions').innerHTML=`<span class="eyebrow">선택한 모델</span><h3>${esc(m.name)}</h3><p>${esc(m.description)}</p><code class="resolved-model">${esc(m.resolved||m.id)}</code><div class="model-details">${(m.modalities||[]).map(v=>`<span class="tiny-tag">${esc(v)}</span>`).join('')}</div><fieldset><legend>추론 강도</legend><div class="effort-choices">${[{id:'',label:'기본'+(m.defaultEffort?' · '+m.defaultEffort:'')},...(m.efforts||[])].map(e=>`<label class="option-chip"><input type="radio" name="pickerEffort" value="${esc(e.id)}" ${e.id===effort?'checked':''}><span>${esc(e.label||e.id)}</span></label>`).join('')}</div><p id="pickerEffortHelp" class="form-note"></p></fieldset><div class="field"><label for="pickerTier">응답 속도</label><select id="pickerTier" ${!m.tiers?.length?'disabled':''}><option value="">기본 속도</option>${(m.tiers||[]).map(t=>`<option value="${esc(t.id)}">${esc(t.name)} · ${esc(t.description)}</option>`).join('')}</select></div>${a.provider==='claude'&&m.adaptive?'<div class="field"><label for="pickerThinking">사고 방식</label><select id="pickerThinking"><option value="adaptive">Adaptive · 자동 조절</option><option value="disabled">사고 비활성화</option></select></div>':''}<p class="form-note">지원되는 옵션만 표시합니다. 변경한 설정은 이 에이전트의 다음 메시지부터 적용됩니다.</p>`;
    $('#pickerTier').value=tier;$('#pickerTier').onchange=e=>{tier=e.target.value;summary();};if($('#pickerThinking')){$('#pickerThinking').value=thinking;$('#pickerThinking').onchange=e=>thinking=e.target.value;}
    function effortHelp(){$('#pickerEffortHelp').textContent=m.efforts?.find(e=>e.id===(effort||m.defaultEffort))?.description||(effort?`${effort} 단계로 실행합니다. 연결에서 이 단계의 추가 설명을 제공하지 않습니다.`:'제공자의 기본 설정을 사용합니다.');summary();}
    form.querySelectorAll('[name=pickerEffort]').forEach(el=>el.onchange=()=>{effort=el.value;effortHelp();});effortHelp();$('#applyModel').disabled=busy(agent(a.id));
  }
  async function fill(refresh=false){const seq=++sequence;$('#applyModel').disabled=true;$('#reloadCatalog').disabled=true;$('#pickerSource').textContent='설치된 연결에서 모델 목록을 조회하고 있습니다…';try{const catalog=await loadModels(a.provider,refresh);if($('#modelPicker')!==form||sequence!==seq)return;rows=catalog.models;if(!selected)selected=rows.find(m=>m.default)?.id||rows[0]?.id||'';drawList();drawOptions();$('#pickerSource').textContent=`${catalog.source} · ${rows.length}개 모델 · ${dateStamp(catalog.fetchedAt)} 조회. ${catalog.accessNote||''}`;}catch(e){if($('#modelPicker')===form){$('#pickerSource').textContent='조회 실패 — 새로고침으로 다시 시도하세요.';formError(e);}}finally{if($('#modelPicker')===form)$('#reloadCatalog').disabled=false;}}
  $('#modelChoices').onclick=e=>{const b=e.target.closest('[data-pick-model]');if(!b)return;selected=b.dataset.pickModel;drawList();drawOptions();};$('#modelSearch').oninput=drawList;$('#reloadCatalog').onclick=()=>fill(true);$('#cancelModel').onclick=closeModal;
  form.onsubmit=async e=>{e.preventDefault();if(!current())return;$('#applyModel').disabled=true;try{await api('agents',{id:a.id,model:selected,effort,serviceTier:tier,thinking});closeModal();await poll();toast('모델 설정을 저장했습니다. 다음 메시지부터 적용됩니다.');}catch(err){formError(err);$('#applyModel').disabled=busy(agent(a.id));}};
  await fill();
}
async function settings(a=null){
  a=a||agent(state.selected);if(a?.source){observerSettings(a);return;}
  const fresh=!a?.id;let chosen=a?.provider||'codex';
  modal(fresh?'에이전트 추가':'에이전트 설정',`<form id="agentForm"><div class="form-grid"><div class="field"><label for="agentName">이름</label><input id="agentName" required maxlength="200" value="${esc(a?.name||'')}" placeholder="예: 구현 담당"></div><div class="field"><label for="agentRole">역할</label><input id="agentRole" maxlength="200" value="${esc(a?.role||'')}" placeholder="예: 구현과 검증"></div><div class="field full"><label for="provider">AI 연결</label><select id="provider" ${a?.session?'disabled':''}>${state.providers.map(p=>`<option value="${esc(p.id)}" ${p.id===chosen?'selected':''} ${!p.ready?'disabled':''}>${esc(p.name)}${p.kind==='chat'?' · 대화 전용':''}${!p.ready?' · 설치/설정 필요':''}${p.protocol==='acp'?' · ACP':''}</option>`).join('')}</select><button type="button" class="quiet add-cli-link" data-add-cli>＋ 다른 에이전트 CLI 연결 (ACP)</button></div><div class="field full"><label for="modelSelect">모델 <button type="button" id="refreshModels" class="quiet">목록 새로고침 ↻</button></label><select id="modelSelect" disabled><option>실제 모델 목록을 불러오는 중…</option></select><small id="catalogSource"></small></div></div><div id="modelDescription" class="model-description">CLI / SDK에 연결하고 있습니다.</div><div class="form-grid"><div class="field"><label for="effort">추론 강도</label><select id="effort"><option value="">모델 기본값</option></select><small id="effortDescription"></small></div><div class="field"><label for="speed">속도</label><select id="speed"><option value="">기본</option></select></div><div class="field" id="thinkingField"><label for="thinking">사고 방식 · Claude</label><select id="thinking"><option value="adaptive">Adaptive · 자동 조절</option><option value="disabled">사고 비활성화</option></select></div><label class="checkbox-row field full lean-row" ${chosen==='claude'?'':'hidden'}><input type="checkbox" id="leanRun" ${a?.lean?'checked':''}> 가볍게 실행 — 프로젝트 규칙·스킬 없이 (리서치·단순 질문용, 입력 토큰 약 60% 절감)</label><div class="field"><label for="mode">실행 권한</label><select id="mode"><option value="read">읽기 / 분석</option><option value="work">작업 허용 · 필요한 실행은 승인 요청</option><option value="full">전체 권한 · 도구 실행 자동 승인</option></select><small>전체 권한은 파일 수정·명령 실행·네트워크 접근을 매번 묻지 않고 허용합니다. 다음 작업부터 적용되며 사용자 질문·제공자 필수 승인은 남습니다. 대화 전용 제공자는 파일 도구가 없습니다.</small></div></div><details><summary class="form-note">고급: 모델 ID 직접 입력</summary><div class="field"><label for="customModel">목록 외 모델 ID · 입력하면 목록 선택보다 우선</label><input id="customModel" placeholder="계정에서 지원하는 정확한 ID"><small>사용 권한은 실제 실행 시 확인됩니다. 추론·속도는 기본값을 사용합니다.</small></div></details><div class="field"><label for="instructions">역할 지시</label><textarea id="instructions">${esc(a?.instructions||'')}</textarea></div><div class="field"><label for="privateContext">목표 · 자료 · 제약사항</label><textarea id="privateContext">${esc(a?.context||'')}</textarea></div><p class="form-note">모델과 설정 변경은 다음 턴부터 적용됩니다. 기존 대화는 이어집니다.</p><div id="formError" class="form-error"></div><div class="buttons">${fresh?'':`<button type="button" id="archiveAgent">보관</button>`}<button type="button" id="cancelSettings">취소</button><button class="primary" id="saveAgent" type="submit" ${busy(a)?'disabled':''}>${fresh?'추가':'저장'}</button></div></form>`,'AGENT / MODEL');
  $('#mode').value=a?.mode||'read';$('#thinking').value=a?.thinking||'adaptive';
  let rows=[];let loadingId=0;const activeForm=$('#agentForm');
  function describe(){
    const m=rows.find(m=>m.id===$('#modelSelect').value)||rows.find(m=>m.default);if(!m)return;
    $('#modelDescription').innerHTML=`<b>${esc(m.name)}</b><div>${esc(m.description)}</div><div class="id">${esc(m.resolved||m.id)}</div><div class="model-details">${m.default?'<span class="tiny-tag">제공자 기본 모델</span>':''}${(m.modalities||[]).map(v=>`<span class="tiny-tag">${esc(v)}</span>`).join('')}</div>`;
    $('#effort').innerHTML='<option value="">모델 기본값'+(m.defaultEffort?' · '+esc(m.defaultEffort):'')+'</option>'+(m.efforts||[]).map(e=>`<option value="${esc(e.id)}">${esc(e.id)}</option>`).join('');
    $('#effort').value=(m.efforts||[]).some(e=>e.id===a?.effort)?a.effort:'';
    $('#effort').disabled=!m.efforts?.length;
    $('#speed').innerHTML='<option value="">기본</option>'+(m.tiers||[]).map(t=>`<option value="${esc(t.id)}">${esc(t.name)} · ${esc(t.description)}</option>`).join('');
    $('#speed').value=(m.tiers||[]).some(t=>t.id===a?.serviceTier)?a.serviceTier:'';$('#speed').disabled=!m.tiers?.length;
    $('#thinkingField').classList.toggle('hidden',chosen!=='claude'||!m.adaptive);
    $('#effortDescription').textContent=m.efforts?.find(e=>e.id===$('#effort').value)?.description||'';
  }
  async function fill(refresh=false){
    const n=++loadingId;chosen=$('#provider').value;$('#modelSelect').disabled=true;$('#modelSelect').innerHTML='<option value="">목록 조회 중…</option>';$('#saveAgent').disabled=true;$('#catalogSource').textContent='연결 중…';
    try{const catalog=await loadModels(chosen,refresh);if($('#agentForm')!==activeForm||n!==loadingId)return;rows=catalog.models;
      $('#modelSelect').innerHTML='<option value="">제공자 기본 모델</option>'+rows.map(m=>`<option value="${esc(m.id)}">${esc(m.name)}${m.default?' · 기본':''}</option>`).join('');
      $('#modelSelect').value=rows.some(m=>m.id===a?.model)?a.model:(rows.find(m=>m.default)?.id||rows[0]?.id||'');
      if(a?.model&&!rows.some(m=>m.id===a.model))$('#customModel').value=a.model;
      $('#modelSelect').disabled=false;$('#catalogSource').textContent=catalog.source+' · '+dateStamp(catalog.fetchedAt)+' 조회 · '+catalog.accessNote;describe();
    }catch(e){if($('#agentForm')!==activeForm||n!==loadingId)return;$('#catalogSource').textContent='목록 조회 실패: '+e.message;$('#modelSelect').innerHTML='<option value="">제공자 기본 모델</option>';$('#modelSelect').disabled=false;$('#modelDescription').textContent='목록을 새로고침하거나 모델 ID를 직접 입력할 수 있습니다.';}
    finally{if($('#agentForm')===activeForm&&n===loadingId)$('#saveAgent').disabled=busy(a);}
  }
  $('#provider').onchange=()=>{a={...a,model:'',effort:'',serviceTier:''};$('#customModel').value='';fill();};
  $('#modelSelect').onchange=()=>{a={...a,effort:'',serviceTier:''};describe();};$('#refreshModels').onclick=()=>fill(true);
  $('#effort').onchange=()=>{$('#effortDescription').textContent=rows.find(m=>m.id===$('#modelSelect').value)?.efforts?.find(e=>e.id===$('#effort').value)?.description||'';};
  $('#cancelSettings').onclick=closeModal;
  if(!fresh)$('#archiveAgent').onclick=async()=>{try{await api('archive',{agent:a.id});closeModal();await poll();}catch(e){formError(e);}};
  $('#agentForm').onsubmit=async ev=>{
    ev.preventDefault();const custom=$('#customModel').value.trim();const data={id:a?.id,team:fresh&&state.selectedTeam!=='__unassigned'?state.selectedTeam:undefined,name:$('#agentName').value.trim(),role:$('#agentRole').value.trim()||'담당자',provider:$('#provider').value,model:custom||$('#modelSelect').value,effort:custom?'':$('#effort').value,serviceTier:custom?'':$('#speed').value,thinking:$('#thinking').value,mode:$('#mode').value,instructions:$('#instructions').value,context:$('#privateContext').value,...($('#leanRun')?{lean:$('#provider').value==='claude'&&$('#leanRun').checked}:{})};
    $('#saveAgent').disabled=true;try{const saved=await api('agents',data);await poll();select(saved.id);closeModal();toast('설정을 저장했습니다. 다음 메시지부터 적용됩니다.');}catch(e){formError(e);$('#saveAgent').disabled=false;}
  };fill();
}
function shared(){modal('팀 공유 컨텍스트',`<form id="sharedForm"><p class="form-note">모든 에이전트에게 다음 메시지를 보낼 때 전달됩니다. 목표, 확정된 결정, 완료 기준을 적어두세요.</p><div class="field"><label for="sharedText">공통 목표와 결정 사항</label><textarea id="sharedText" rows="12" maxlength="30000">${esc(state.shared)}</textarea></div><div id="formError" class="form-error"></div><div class="buttons"><button class="primary" type="submit">저장</button></div></form>`,'SHARED CONTEXT');$('#sharedForm').onsubmit=async e=>{e.preventDefault();try{await api('shared',{text:$('#sharedText').value});closeModal();await poll();toast('팀 공유 컨텍스트를 저장했습니다.');}catch(e){formError(e);}};}
function peers(){const a=agent(state.selected);modal('연결할 대화 선택',`<p class="form-note">선택한 대화의 최근 메시지를 ${esc(a.name)}의 다음 입력에 첨부합니다. 상대에게 메시지를 보내지는 않습니다.</p><form id="peersForm">${state.agents.filter(p=>p.id!==a.id).map(p=>`<label class="checkbox-row"><input type="checkbox" name="peer" value="${esc(p.id)}" ${(a.peers||[]).includes(p.id)?'checked':''}>${esc(p.name)} · ${esc(providerName(p.provider))}</label>`).join('')}<div id="formError" class="form-error"></div><div class="buttons"><button class="primary" ${busy(a)?'disabled':''}>연결 저장</button></div></form>`);$('#peersForm').onsubmit=async e=>{e.preventDefault();try{await api('agents',{id:a.id,peers:[...document.querySelectorAll('[name=peer]:checked')].map(x=>x.value)});closeModal();await poll();}catch(e){formError(e);}};}
async function sources(){modal('외부 대화와 Claude 팀원',`<p class="form-note">이 프로젝트에서 만들어진 Claude Code / Codex 대화 기록을 로컬에서 읽습니다. 진행 상태를 추측하지 않고 마지막 기록 시점을 표시합니다.</p><input id="sourceSearch" style="width:100%;margin-bottom:14px" placeholder="이름이나 내용 검색"><div id="sourceList" class="source-list">대화 기록을 찾는 중…</div>`,'CONNECTED SESSIONS');
  try{const rows=await api('sources');if(!$('#sourceList'))return;function draw(){const q=$('#sourceSearch').value.toLowerCase();$('#sourceList').innerHTML=rows.filter(s=>JSON.stringify(s).toLowerCase().includes(q)).map(s=>`<div class="source-row"><span class="avatar ${s.provider==='codex'?'codex':''}">${s.parent?'↳':s.provider==='codex'?'⌘':'C'}</span><div class="info"><b>${esc(s.parent?'Claude 팀원':providerName(s.provider))}</b><p>${esc(s.preview||s.name)}</p><small>${dateStamp(s.updated)} · ${esc(s.name.slice(-12))}</small></div><button data-watch="${esc(s.id)}">열람</button></div>`).join('')||'<p class="form-note">일치하는 프로젝트 대화가 없습니다.</p>'; }$('#sourceSearch').oninput=draw;draw();}catch(e){if($('#sourceList'))$('#sourceList').textContent=e.message;}
}
async function watch(sid){try{const a=await api('watch',{source:sid});closeModal();await poll();select(a.id);}catch(e){toast(e.message);}}
function compare(id){globalThis.PitCrewArtifacts?.close();if(!id){modal('나란히 볼 대화',`<div class="source-list">${state.agents.filter(a=>a.id!==state.selected).map(a=>`<button class="source-row" data-compare="${esc(a.id)}"><span class="avatar ${avatarClass(a)}">${initials(a)}</span><span>${esc(a.name)}</span></button>`).join('')||'다른 에이전트를 추가하세요.'}</div>`);return;}state.compare=id;state.compareHash='';$('#compareName').textContent=agent(id)?.name||'대화';$('#comparePanel').classList.remove('hidden');closeModal();refreshDetail();}
function findMessage(owner,mid){const d=state.detail?.agent.id===owner?state.detail:state.compareDetail?.agent.id===owner?state.compareDetail:null;return d?.messages.find(m=>m.id===mid);}
function forward(owner,mid){const m=findMessage(owner,mid);if(!m)return;modal('메시지를 다른 역할에 전달',`<p class="form-note">원문과 출처를 입력창에 첨부합니다. 내용을 확인한 뒤 보내세요.</p><div class="model-description">${esc(m.text.slice(0,400))}${m.text.length>400?'…':''}</div><div class="source-list">${state.agents.filter(a=>!a.source&&a.id!==owner).map(a=>`<button class="source-row" data-forward-target="${esc(a.id)}" data-source-owner="${esc(owner)}" data-source-mid="${esc(mid)}"><span class="avatar ${avatarClass(a)}">${initials(a)}</span><span>${esc(a.name)}에게 전달</span></button>`).join('')||'전달할 에이전트를 먼저 추가하세요.'}</div>`);}
function showContext(mid,owner){const m=findMessage(owner||state.selected,mid);if(!m?.context)return;const c=m.context;modal('이 메시지와 함께 전달된 맥락',`<div class="prose"><p>전송 시점에 저장한 스냅샷입니다. ${stamp(m.ts)}</p><h3>실행 설정</h3><pre>${esc(JSON.stringify(m.settings,null,2))}</pre><h3>역할</h3><p>${esc(c.role)}<br>${esc(c.instructions)}</p><h3>개별 자료</h3><p>${markdown(c.private||'없음')}</p><h3>팀 공유 컨텍스트</h3><p>${markdown(c.shared||'없음')}</p>${(c.projectRules||[]).map(r=>`<details><summary>프로젝트 규칙 · ${esc(r.path)}</summary><pre>${esc(r.text)}</pre></details>`).join('')}${c.snapshots.map(s=>`<h3>↗ ${esc(s.name)} · ${s.kind==='peer'?'연결한 대화':'선택한 메시지'}</h3><div class="message-body">${markdown(s.text)}</div>`).join('')}</div>`,'CONTEXT SNAPSHOT');}
function activity(){modal('팀 활동 기록',`<div>${(state.activity||[]).map(e=>`<div class="log-row"><time>${stamp(e.ts)}</time><div><b>${esc(agent(e.agent)?.name||'워크스페이스')}</b><br>${esc(e.text)}</div></div>`).join('')||'<p class="form-note">아직 활동이 없습니다.</p>'}</div>`,'ACTIVITY');}
function observerSettings(a){modal('외부 대화 표시 설정',`<form id="observerForm"><div class="field"><label for="observerName">워크스페이스에 표시할 이름</label><input id="observerName" value="${esc(a.name)}" required></div><p class="form-note">원본 대화와 파일은 변경하지 않습니다.</p><div id="formError" class="form-error"></div><div class="buttons"><button id="archiveObserver" type="button">보관</button><button class="primary">저장</button></div></form>`);$('#observerForm').onsubmit=async e=>{e.preventDefault();try{await api('agents',{id:a.id,name:$('#observerName').value});closeModal();await poll();}catch(e){formError(e);}};$('#archiveObserver').onclick=async()=>{try{await api('archive',{agent:a.id});closeModal();await poll();}catch(e){formError(e);}};}
async function archived(){modal('보관된 대화','<div id="archivedList">불러오는 중…</div>');try{const rows=await api('archived');if(!$('#archivedList'))return;$('#archivedList').innerHTML=rows.map(a=>`<div class="source-row"><div class="info"><b>${esc(a.name)}</b><p>${esc(providerName(a.provider))}</p></div><button data-restore="${esc(a.id)}">복원</button></div>`).join('')||'<p class="form-note">보관된 대화가 없습니다.</p>';}catch(e){toast(e.message);}}
function help(){modal('시작하기',`<div class="prose"><h3>프로젝트를 선택하세요</h3><p>왼쪽 ‘프로젝트 관리’에서 로컬 폴더를 추가하세요. 프로젝트마다 대화와 팀, 자료가 분리되며 전환해도 작업은 계속됩니다.</p><h3>역할을 나누고 대화를 이어가세요</h3><p>＋로 에이전트를 만들고 제공자·모델·역할을 선택하세요. ‘대화 가져오기’로 기존 기록을 연결하고 ‘원래 세션 이어서 사용’으로 그 대화에서 계속할 수 있습니다. 다른 앱의 해당 작업을 마친 뒤 재개하세요.</p><h3>팀이 참고할 맥락을 고르세요</h3><p>팀 관리에서 공통 목표와 팀원을 정합니다. ‘대화 공유 연결’은 선택한 팀원의 최근 기록을 다음 입력에 첨부합니다. ‘비교’로 대화를 나란히 볼 수 있습니다.</p><h3>사용량과 실행 권한</h3><p>상단 사용량 버튼에서 계정 전체의 구독 한도와 초기화 시각을 확인하세요. 기본은 읽기 모드이며, 파일 수정이 필요하면 설정에서 작업을 허용하세요. 필요한 승인 요청은 대화 아래에 표시됩니다.</p><h3>화면과 기록</h3><p>상단 ◐ 버튼으로 밝은 화면과 어두운 화면을 바꿀 수 있습니다. 작은 화면에서는 ☰로 프로젝트와 대화 목록을 엽니다. 보관된 대화는 언제든 복원할 수 있습니다.</p></div>`);}

document.addEventListener('click',async e=>{
  const t=e.target.closest('button');if(!t)return;
  try{
    if(t.dataset.noticeAction){if(t.dataset.noticeAction==='continue'){await continueConversation();}else if(t.dataset.noticeAction==='approval'){focusApproval();}else if(t.dataset.noticeAction==='setup')onboarding();else $('#prompt').focus();}
    else if(t.dataset.agent)select(t.dataset.agent);
    else if(t.dataset.teamAgent){closeModal();select(t.dataset.teamAgent);}
    else if(t.hasAttribute('data-settings')){if(t.closest('#modelBar'))modelPicker();else settings();}
    else if(t.hasAttribute('data-shared'))shared();
    else if(t.hasAttribute('data-peers'))peers();
    else if(t.dataset.watch)watch(t.dataset.watch);
    else if(t.dataset.restore){await api('restore',{agent:t.dataset.restore});closeModal();await poll();select(t.dataset.restore);}
    else if(t.dataset.compare)compare(t.dataset.compare);
    else if(t.dataset.suggest){$('#prompt').value=t.dataset.suggest;$('#prompt').focus();}
    else if(t.dataset.removeRef!==undefined){state.refs[state.selected].splice(Number(t.dataset.removeRef),1);renderRefs();}
    else if(t.hasAttribute('data-copy-code')){const code=t.closest('.code-block')?.querySelector('code');if(code){await navigator.clipboard.writeText(code.textContent);t.textContent='복사됨';setTimeout(()=>{if(t.isConnected)t.textContent='복사';},1800);}}
    else if(t.dataset.copy){const m=findMessage(t.dataset.owner,t.dataset.copy);if(m){await navigator.clipboard.writeText(m.text);toast('메시지를 복사했습니다.');}}
    else if(t.dataset.forward)forward(t.dataset.owner,t.dataset.forward);
    else if(t.dataset.forwardTarget){const target=t.dataset.forwardTarget;state.refs[target]=[...(state.refs[target]||[]),{agent:t.dataset.sourceOwner,message:t.dataset.sourceMid,name:agent(t.dataset.sourceOwner)?.name||'외부 대화'}];closeModal();select(target);$('#prompt').focus();}
    else if(t.dataset.contextMessage)showContext(t.dataset.contextMessage,t.dataset.owner);
    else if(t.dataset.decision){const box=t.closest('[data-request]');const answers={};box.querySelectorAll('[data-question]').forEach(x=>answers[x.dataset.question]=x.value);await api('decide',{id:box.dataset.request,allow:t.dataset.decision!=='deny',answers,remember:t.dataset.decision==='always'});await poll();}
    else if(t.hasAttribute('data-export')){const d=await api('export?agent='+encodeURIComponent(state.selected));const blob=new Blob([JSON.stringify(d,null,2)],{type:'application/json'});const url=URL.createObjectURL(blob);const a=document.createElement('a');a.href=url;a.download='pitcrew-'+d.agent.id+'.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
  }catch(err){toast(err.message);}
});
$('#modalClose').onclick=closeModal;$('#newAgent').onclick=()=>settings({});$('#settingsOpen').onclick=()=>settings();$('#sourcesOpen').onclick=sources;$('#sharedOpen').onclick=shared;$('#activityOpen').onclick=activity;$('#helpOpen').onclick=help;$('#compareOpen').onclick=()=>compare();$('#compareClose').onclick=()=>{state.compare=null;$('#comparePanel').classList.add('hidden');$('#compareOpen').focus();};
$('#contextToggle').onclick=()=>{if(innerWidth<=1100)$('#contextPanel').classList.toggle('visible');else{$('#contextPanel').classList.toggle('hidden');$('.workspace').style.gridTemplateColumns=$('#contextPanel').classList.contains('hidden')?'234px minmax(340px,1fr)':'';}};
$('#composer').onsubmit=send;$('#prompt').onkeydown=e=>{if(e.key==='Enter'&&!e.shiftKey&&!e.isComposing){e.preventDefault();if(!$('#send').disabled)$('#composer').requestSubmit();}};
$('#archivedOpen').onclick=archived;
$('#prompt').value=state.drafts[state.selected]||'';renderRefs();
$('#prompt').oninput=()=>{state.drafts[state.selected]=$('#prompt').value;saveDrafts();};
$('#stop').onclick=async()=>{try{await api('stop',{agent:state.selected});await poll();}catch(e){toast(e.message);}};
$('#messages').onscroll=()=>{$('#jumpLatest').classList.toggle('hidden',$('#messages').scrollHeight-$('#messages').scrollTop-$('#messages').clientHeight<200);};$('#jumpLatest').onclick=()=>{$('#messages').scrollTop=$('#messages').scrollHeight;};
const syncLoop=new PitCrewConnection.Poller(syncWorkspace,{
  interval:()=>document.hidden?(state.agents.some(busy)?4000:30000):state.agents.some(busy)?1400:5000,
  onSuccess:()=>{globalThis.PitCrewWorkOverview?.connection(true);state.lastSync=Date.now();$('#connection').textContent='● 로컬 연결';$('#connection').style.color='';$('#connection').title='마지막 동기화 '+new Date(state.lastSync).toLocaleTimeString('ko-KR');$('#syncNotice').classList.add('hidden');},
  onError:(error,delay)=>{globalThis.PitCrewWorkOverview?.connection(false);const recent=state.lastServerContact&&Date.now()-state.lastServerContact<30000;$('#connection').textContent=recent?'갱신 재시도 중':'재연결 중';$('#connection').style.color='var(--red)';$('#syncNotice').classList.remove('hidden');$('#syncMessage').textContent=`${recent?'화면 갱신이 지연되고 있습니다.':'서버 응답을 기다리고 있습니다.'} ${delay/1000}초 후 자동 재시도 · 입력 중인 내용은 유지됩니다.${state.lastSync?' 마지막 동기화 '+new Date(state.lastSync).toLocaleTimeString('ko-KR'):''}`;$('#syncNotice').title=error.message;}
});
$('#retryConnection').onclick=()=>poll();
$('#teamOpen').onclick=teamOverview;
window.addEventListener('online',()=>poll());
window.addEventListener('pageshow',()=>poll());
window.addEventListener('focus',()=>poll());
document.addEventListener('visibilitychange',()=>{if(!document.hidden)poll();});
poll();

$('#resumeSource').onclick=()=>{
  const a=agent(state.selected);if(!a?.source)return;
  modal('원래 세션 이어서 사용',`<p><b>${esc(a.name)}</b>의 원래 세션 ID로 다음 메시지를 보냅니다. 새 대화로 복사하지 않으며, 이후 응답은 원본 세션에도 기록됩니다.</p><p class="form-note">다른 앱에서 이 대화가 실행 중이라면 먼저 작업을 마치세요. 앱 사이 동시 실행은 자동으로 제어하지 못합니다. 대화 기록은 이어지지만 원래 앱 전용 도구와 실행 중인 하위 에이전트는 인계되지 않습니다.</p><p class="form-note">우선 읽기·분석 모드로 연결합니다. 파일 작업은 설정에서 변경할 수 있습니다. 연결만으로 메시지를 보내지는 않습니다.</p><div id="formError" class="form-error"></div><div class="buttons"><button id="cancelResume">취소</button><button id="confirmResume" class="primary">이 세션에서 계속하기</button></div>`,'RESUME ORIGINAL SESSION');
  $('#cancelResume').onclick=closeModal;$('#confirmResume').onclick=async()=>{const b=$('#confirmResume');b.disabled=true;try{const saved=await api('resume-source',{agent:a.id});await poll();state.agents=state.agents.filter(old=>old.id!==saved.id).concat(saved);select(saved.id);closeModal();toast('원래 세션을 연결했습니다. 입력창에서 다음 메시지를 보내세요.');}catch(e){formError(e);b.disabled=false;}};
};

// Keep keyboard navigation inside the active dialog, including browser chrome edges.
$('#modal').addEventListener('cancel',()=>{state.modalVersion=(state.modalVersion||0)+1;});
$('#modal').addEventListener('keydown',e=>{
  if(e.key!=='Tab')return;
  const list=[...$('#modal').querySelectorAll('button,input,select,textarea,a[href],[tabindex]')].filter(el=>!el.disabled&&el.tabIndex>=0&&el.getClientRects().length);
  if(!list.length){e.preventDefault();return;}
  const first=list[0],last=list[list.length-1],active=document.activeElement;
  if(e.shiftKey&&(active===first||!$('#modal').contains(active))){e.preventDefault();last.focus();}
  else if(!e.shiftKey&&(active===last||!$('#modal').contains(active))){e.preventDefault();first.focus();}
});
document.addEventListener('click',async e=>{
  const b=e.target.closest('[data-tool-full]');if(!b)return;e.preventDefault();b.disabled=true;b.textContent='불러오는 중…';
  try{const m=await api(`message?agent=${encodeURIComponent(b.dataset.owner)}&id=${encodeURIComponent(b.dataset.toolFull)}`);state.fullTool=state.fullTool||{};state.fullTool[m.id]={text:m.text||'',input:m.input||''};
    // Show it in place right away (main or comparison panel); later renders use state.fullTool.
    const box=b.closest('.tool-box'),out=box?.querySelector('pre[data-part="output"]'),inp=box?.querySelector('pre[data-part="input"]');if(out)out.textContent=m.text||'';if(inp&&m.input)inp.textContent=m.input;b.remove();
    state.detailHash='';state.compareHash='';await refreshDetail();}
  catch(err){b.disabled=false;b.textContent='전체 보기';toast(err.message);}
});

// Image viewer: any [data-lightbox] opens full size; Esc, click or × closes; focus returns.
document.addEventListener('click',e=>{
  const t=e.target.closest('[data-lightbox]');if(!t||e.ctrlKey||e.metaKey)return;e.preventDefault();
  let box=document.getElementById('lightbox');
  if(!box){box=document.createElement('dialog');box.id='lightbox';box.className='lightbox';box.innerHTML='<button type="button" class="lightbox-close" aria-label="닫기">×</button><img alt=""><a class="lightbox-open" target="_blank" rel="noopener">원본 열기 ↗</a>';
    document.body.appendChild(box);box.addEventListener('click',ev=>{if(ev.target===box||ev.target.closest('.lightbox-close'))box.close();});box.addEventListener('close',()=>box._from?.focus());}
  box._from=t;box.querySelector('img').src=t.dataset.lightbox;box.querySelector('img').alt=t.title||t.querySelector('img')?.alt||'';box.querySelector('.lightbox-open').href=t.dataset.lightbox;box.showModal();
});

document.addEventListener('click',e=>{
  const b=e.target.closest('[data-reuse]');if(!b)return;
  const m=state.detail?.messages.find(x=>x.id===b.dataset.reuse);if(!m)return;
  const box=$('#prompt');box.value=visibleText(m)+(box.value.trim()?'\n\n'+box.value:'');state.drafts[state.selected]=box.value;saveDrafts();
  box.focus();box.setSelectionRange(box.value.length,box.value.length);toast('입력창에 넣었습니다. 고친 뒤 보내세요.');
});

// View mode (Claude Code's Normal/Verbose, Ctrl+O): verbose opens every tool group and call.
function setVerbose(on){state.verbose=on;try{localStorage.setItem('pitcrew.verbose',on?'1':'0');}catch{}document.body.classList.toggle('verbose-tools',on);
  for(const d of document.querySelectorAll('#messages details.tool-group-box,#messages details.tool-box'))d.open=on;toast(on?'도구 활동을 자세히 봅니다 (Ctrl+O)':'도구 활동을 요약해서 봅니다 (Ctrl+O)');}
document.addEventListener('keydown',e=>{if((e.ctrlKey||e.metaKey)&&!e.shiftKey&&!e.altKey&&e.key.toLowerCase()==='o'){e.preventDefault();setVerbose(!state.verbose);}});
try{state.verbose=localStorage.getItem('pitcrew.verbose')==='1';document.body.classList.toggle('verbose-tools',state.verbose);}catch{}

// Connect another agent CLI over ACP: live handshake first (no model call), then save.
function addCliDialog(){
  modal('다른 에이전트 CLI 연결',`<form id="cliForm"><p class="form-note"><a href="https://agentclientprotocol.com" target="_blank" rel="noopener">ACP(Agent Client Protocol)</a>를 지원하는 CLI 를 연결합니다. 예: <code>gemini --acp</code>. 먼저 연결 확인으로 초기화만 해 봅니다 — 모델은 호출하지 않습니다.</p>
    <div class="field"><label for="cliCommand">실행 명령</label><input id="cliCommand" required maxlength="500" placeholder="gemini --acp" autocomplete="off"></div>
    <div class="form-grid"><div class="field"><label for="cliName">표시 이름 <small>비우면 CLI 가 알려준 이름</small></label><input id="cliName" maxlength="80"></div><div class="field"><label for="cliModelFlag">모델 지정 플래그 <small>선택 · 예: -m</small></label><input id="cliModelFlag" maxlength="20"></div></div>
    <div id="cliResult" class="cli-result" aria-live="polite"></div><div id="formError" class="form-error"></div>
    <div class="buttons"><button type="button" id="cliCheck">연결 확인</button><button type="submit" class="primary">연결</button></div></form>`,'AGENT CLI');
  const body=()=>({command:$('#cliCommand').value,name:$('#cliName').value,modelFlag:$('#cliModelFlag').value});
  const show=r=>{$('#cliResult').innerHTML=`<b>${esc(r.name||'이름 없음')}</b> ${esc(r.version||'')} · ACP v${esc(r.protocol)}<br>이미지 ${r.image?'지원':'미지원'} · 이어서 대화 ${r.loadSession?'지원':'미지원'}${r.auth?.length?` · 로그인 방식: ${esc(r.auth.join(', '))}`:''}`;};
  $('#cliCheck').onclick=async()=>{$('#formError').textContent='';$('#cliResult').textContent='확인 중…';try{show(await api('add-cli',{...body(),check:true}));}catch(e){$('#cliResult').textContent='';formError(e);}};
  $('#cliForm').onsubmit=async e=>{e.preventDefault();$('#cliResult').textContent='연결 중…';try{const r=await api('add-cli',body());closeModal();await poll();toast(`${r.name||r.id} 를 연결했습니다. 에이전트 설정의 AI 연결에서 고르세요.`);}catch(err){$('#cliResult').textContent='';formError(err);}};
}
document.addEventListener('click',e=>{if(e.target.closest('[data-add-cli]')){e.preventDefault();addCliDialog();}});

// Open the receiving conversation at the message that came from here.
let messageJumpSequence=0;
document.addEventListener('click',async e=>{
  const b=e.target.closest('[data-open-agent]');if(!b||!agent(b.dataset.openAgent))return;e.preventDefault();
  const mid=b.dataset.openMessage,aid=b.dataset.openAgent,project=state.projectId,request=++messageJumpSequence;
  if(state.selected!==aid||state.detail?.agent.id!==aid)select(aid);
  if(!mid)return;
  for(let i=0;i<40;i++){
    await new Promise(r=>setTimeout(r,100));
    if(request!==messageJumpSequence||state.projectId!==project||state.selected!==aid)return;
    if(state.detail?.agent.id!==aid)continue;
    const escaped=CSS.escape(mid),el=document.querySelector(`#messages [data-message="${escaped}"],#messages [data-tool="${escaped}"]`);
    if(!el)continue;
    // A tool result lives inside its own disclosure and usually an outer tool group.
    for(let d=el.closest('details');d;d=d.parentElement?.closest('details'))d.open=true;
    const target=el.matches('details')?el.querySelector(':scope > summary'):el;
    if(!target.hasAttribute('tabindex')&&!target.matches('summary'))target.tabIndex=-1;
    target.focus({preventScroll:true});target.scrollIntoView({block:'center'});
    el.classList.add('flash');setTimeout(()=>el.classList.remove('flash'),1600);return;
  }
  toast('메시지를 찾지 못했습니다. 대화를 새로고침한 뒤 다시 시도하세요.');
});

document.addEventListener('change',e=>{if(e.target.id==='provider'){const row=document.querySelector('.lean-row');if(row)row.hidden=e.target.value!=='claude';}});
