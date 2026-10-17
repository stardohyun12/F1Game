'use strict';
const layoutCache={},layoutSaved={};
function workspaceLayout(){
  const key=state.projectId||'default';
  if(!layoutCache[key]){try{layoutCache[key]=JSON.parse(localStorage.getItem('pitcrew.layout.'+key)||'{}');}catch{layoutCache[key]={};}}
  if(!layoutCache[key]||typeof layoutCache[key]!=='object'||Array.isArray(layoutCache[key]))layoutCache[key]={};
  const value=layoutCache[key];
  if(!Array.isArray(value.order))value.order=[];
  if(!Array.isArray(value.tabs))value.tabs=[];
  return value;
}
function saveWorkspaceLayout(){const key='pitcrew.layout.'+(state.projectId||'default'),value=JSON.stringify(workspaceLayout());if(layoutSaved[key]===value)return;try{localStorage.setItem(key,value);layoutSaved[key]=value;}catch{toast('화면 배치를 저장할 공간이 부족합니다.');}}
function orderedAgents(){const order=workspaceLayout().order;return [...state.agents].sort((a,b)=>{const x=order.indexOf(a.id),y=order.indexOf(b.id);return (x<0?Infinity:x)-(y<0?Infinity:y);});}
function renderWorkspaceTabs(){
  if(state.loadedProject!==state.projectId){$('#workspaceTabs').innerHTML='<span class="form-note">대화 목록을 불러오는 중…</span>';return;}
  const layout=workspaceLayout();layout.selected=state.selected;layout.tabs=layout.tabs.filter(id=>agent(id));
  if(state.selected&&agent(state.selected)&&!layout.tabs.includes(state.selected))layout.tabs.push(state.selected);
  // Tabs are grouped by team (team label first; clicking it switches the sidebar to that team and opens its lead).
  const teamOf=id=>(state.teams||[]).find(t=>t.members.includes(id)),groups=[];
  for(const id of layout.tabs){const t=teamOf(id),key=t?.id||'';let g=groups.find(g=>g.key===key);if(!g){g={key,team:t,ids:[]};groups.push(g);}g.ids.push(id);}
  layout.tabs=groups.flatMap(g=>g.ids);  // what is shown is what is stored, so moves never snap back
  const labelled=groups.length>1||groups.some(g=>g.team);
  const tab=id=>{const a=agent(id);return `<div class="workspace-tab ${state.selected===id?'selected':''}" draggable="true" data-drag-agent="${esc(id)}" data-tab-id="${esc(id)}" title="끌어서 순서 변경 · 오른쪽에 놓아 비교 · Esc 취소"><button data-agent="${esc(id)}" aria-current="${state.selected===id?'page':'false'}">${esc(globalThis.PitCrewSidebar?.label(a)||a.name)}${globalThis.PitCrewNotify?.unread(id)?'<i class="unread-dot" aria-label="미확인 응답"></i>':''}${busy(a)?' · 작업 중':''}</button><button data-close-tab="${esc(id)}" aria-label="${esc(a.name)} 탭 닫기">×</button></div>`;};
  const html=groups.map(g=>`<div class="tab-group${g.ids.includes(state.selected)?' current':''}" role="group" aria-label="${esc(g.team?.name||'팀 밖')}">${labelled?`<button type="button" class="tab-team" data-tab-team="${esc(g.key)}" title="${g.team?`${esc(g.team.name)} 팀으로 이동${g.team.lead&&agent(g.team.lead)?' · 팀장 대화 열기':''}`:'팀에 속하지 않은 대화'}">${esc(g.team?.name||'팀 밖')}</button>`:''}${g.ids.map(tab).join('')}</div>`).join('')+'<button class="tab-board" data-board-open>팀 구성 ◫</button>';
  if($('#workspaceTabs').innerHTML!==html)$('#workspaceTabs').innerHTML=html;
  saveWorkspaceLayout();
}
function closeWorkspaceTab(id){
  const layout=workspaceLayout(),index=layout.tabs.indexOf(id);layout.tabs=layout.tabs.filter(x=>x!==id);
  if(state.selected===id){state.workspaceClosed=!layout.tabs.length;layout.closed=state.workspaceClosed;if(state.workspaceClosed){state.compare=null;$('#comparePanel').classList.add('hidden');}select(layout.tabs[Math.max(0,index-1)]);}
  saveWorkspaceLayout();renderWorkspaceTabs();
}
state.workspaceClosed=!!workspaceLayout().closed;
if(state.workspaceClosed)state.selected=null;
function reorderConversation(id,target,tab=false,after=false){
  if(!tab){const why=state.sidebarPinned?.get(id)||state.sidebarPinned?.get(target);if(why){toast(why);return;}}
  else{const team=x=>(state.teams||[]).find(t=>t.members.includes(x))?.id||'';if(team(id)!==team(target)){toast('탭은 같은 팀 묶음 안에서만 옮길 수 있습니다.');return;}}
  const layout=workspaceLayout(),list=tab?layout.tabs:orderedAgents().map(a=>a.id);
  if(id===target||!list.includes(id)||!list.includes(target))return;
  const next=list.filter(x=>x!==id);next.splice(next.indexOf(target)+(after?1:0),0,id);
  if(tab)layout.tabs=next;else layout.order=next;
  saveWorkspaceLayout();renderAgents();
}
async function archiveConversation(id){
  const a=agent(id),project=state.projectId;if(!a)return;
  await api('archive',{agent:id},project);
  if(state.projectId!==project)return;
  if(state.compare===id){state.compare=null;$('#comparePanel').classList.add('hidden');}
  closeModal();await poll();
  toast('대화를 보관했습니다. 기록과 원래 세션은 유지됩니다.');
  const undo=document.createElement('button');undo.textContent='되돌리기';
  undo.onclick=async()=>{undo.disabled=true;try{await api('restore',{agent:id},project);if(state.projectId===project){await poll();select(id);}toast('대화를 복원했습니다.');}catch(error){toast(error.message);}};
  $('#toast').append(undo);measureToastSpace();clearTimeout(toastTimer);toastTimer=setTimeout(hideToastWhenIdle,15000);
}
function renameConversation(id){
  const a=agent(id),project=state.projectId;if(!a)return;
  modal('이름 · 담당 역할 변경',`<form id="renameChatForm"><label for="chatDisplayName">이름</label><input id="chatDisplayName" maxlength="200" required value="${esc(a.name)}" placeholder="예: Claude · 기획 담당"><label for="chatDisplayRole">담당 역할</label><textarea id="chatDisplayRole" ${busy(a)?'disabled':''} maxlength="200" rows="3" placeholder="예: 요구사항 정리와 작업 계획 작성">${esc(a.role||'')}</textarea><p class="form-note">이름과 역할은 대화 목록과 팀 구성에 표시됩니다. 역할은 다음 작업의 역할 설명에도 반영됩니다. 원본 앱의 대화 이름은 바뀌지 않습니다.${busy(a)?' 작업 중에는 이름만 변경할 수 있습니다.':''}</p><p id="formError" role="alert"></p><div class="form-actions"><button type="button" id="cancelChatRename">취소</button><button type="submit" class="primary">저장</button></div></form>`);
  const version=state.modalVersion,form=$('#renameChatForm');
  $('#cancelChatRename').onclick=closeModal;$('#chatDisplayName').focus();
  form.onsubmit=async e=>{
    e.preventDefault();const name=$('#chatDisplayName').value.trim(),role=$('#chatDisplayRole').value.trim();
    if(!name){formError('이름을 입력하세요.');$('#chatDisplayName').focus();return;}
    const submit=form.querySelector('[type="submit"]');submit.disabled=true;
    try{
      const saved=await api('agents',busy(a)?{id,name}:{id,name,role},project);
      if(state.projectId!==project)return;
      state.agents=state.agents.map(item=>item.id===id?{...item,...saved}:item);renderAgents();
      if(state.modalVersion===version){closeModal();toast('이름과 담당 역할을 저장했습니다.');}
      await poll();
    }catch(error){if(state.projectId===project&&state.modalVersion===version)formError(error);}
    finally{submit.disabled=false;}
  };
}
// "⋯" on a conversation: a small menu next to the button (not a full-screen dialog). Team roles are set here.
function chatOrganizationMenu(id,anchor,at){
  const a=agent(id);if(!a)return;closeChatMenu();
  const tabs=workspaceLayout().tabs,ti=tabs.indexOf(id),teams=state.teams||[],mine=teams.filter(t=>t.members.includes(id)),other=teams.filter(t=>!t.members.includes(id));
  const role=t=>t.lead===id?'<b class="lead-tag">팀장</b>':t.lead?'<span class="pop-role">팀원</span>':'<span class="pop-role">팀장 없음</span>';
  const pop=document.createElement('div');pop.className='chat-pop';pop.setAttribute('role','menu');pop.setAttribute('aria-label',a.name+' 메뉴');
  pop.innerHTML=`<div class="pop-title">${esc(globalThis.PitCrewSidebar?.label(a)||a.name)}</div>
    <button role="menuitem" data-rename-chat="${esc(id)}">이름 · 역할 바꾸기</button>
    <button role="menuitem" data-show-side="${esc(id)}">오른쪽에 나란히 보기</button>
    ${ti>=0?`<button role="menuitem" data-close-chat="${esc(id)}">탭 닫기</button>`:''}
    <div class="pop-sep"></div><div class="pop-head">팀</div>
    ${mine.map(t=>`<div class="pop-team"><span class="pop-team-name">${esc(t.name)} ${role(t)}</span><span class="pop-team-act">${t.lead===id?`<button role="menuitem" data-set-lead="" data-who="${esc(id)}" data-target-team="${esc(t.id)}" aria-label="${esc(t.name)} 팀장 해제" title="팀장 지정을 풉니다">팀장 해제</button>`:`<button role="menuitem" data-set-lead="${esc(id)}" data-target-team="${esc(t.id)}" aria-label="${esc(t.name)} 팀장으로" title="이 팀의 알림을 이 대화만 받고, 팀원에게 일을 맡길 수 있습니다">팀장으로</button>`}<button role="menuitem" data-remove-member="${esc(id)}" data-target-team="${esc(t.id)}" aria-label="${esc(t.name)}에서 빼기" title="팀에서 뺍니다(대화는 유지)">빼기</button></span></div>`).join('')||'<p class="pop-note">아직 팀이 없습니다.</p>'}
    ${other.map(t=>`<button role="menuitem" data-add-member="${esc(id)}" data-target-team="${esc(t.id)}">＋ ${esc(t.name)}에 넣기</button>`).join('')}
    <button role="menuitem" data-new-team-member="${esc(id)}">＋ 이 대화로 새 팀 만들기</button>
    <div class="pop-sep"></div>
    <div class="pop-order" aria-label="순서"><span title="${esc(state.sidebarPinned?.get(id)||'')}">순서${state.sidebarPinned?.get(id)?' · 고정됨':''}</span><button role="menuitem" data-order-up="${esc(id)}" title="목록에서 위로" aria-label="목록에서 위로">↑</button><button role="menuitem" data-order-down="${esc(id)}" title="목록에서 아래로" aria-label="목록에서 아래로">↓</button>${ti>=0?`<button role="menuitem" data-tab-left="${esc(id)}" ${ti<=0?'disabled':''} title="탭을 왼쪽으로" aria-label="탭을 왼쪽으로">←</button><button role="menuitem" data-tab-right="${esc(id)}" ${ti===tabs.length-1?'disabled':''} title="탭을 오른쪽으로" aria-label="탭을 오른쪽으로">→</button>`:''}</div>
    <div class="pop-sep"></div>
    <button role="menuitem" class="pop-danger" data-archive-chat="${esc(id)}" ${busy(a)?'disabled title="작업 중인 대화는 완료 후 보관할 수 있습니다"':'title="목록에서 뺍니다. 원본 Codex·Claude 대화는 지우지 않으며 \'제거한 대화 복원\'에서 되돌립니다."'}>목록에서 보관</button>`;
  document.body.append(pop);
  const r=anchor?.getBoundingClientRect?.(),w=pop.offsetWidth,h=pop.offsetHeight;
  let x=at?at.x:r?r.right-w:20,y=at?at.y:r?r.bottom+4:20;
  x=Math.max(8,Math.min(x,innerWidth-w-8));if(y+h>innerHeight-8)y=Math.max(8,(r?r.top:y)-h-4);
  pop.style.left=x+'px';pop.style.top=y+'px';pop.dataset.for=id;state.chatMenuReturn=anchor||null;
  anchor?.setAttribute?.('aria-expanded','true');pop.addEventListener('focusout',e=>{if(e.relatedTarget&&!pop.contains(e.relatedTarget)&&!e.relatedTarget.closest('[data-chat-menu]'))closeChatMenu();});
  pop.querySelector('button:not([disabled])')?.focus();
}
function closeChatMenu(refocus){const p=document.querySelector('.chat-pop');if(!p)return;p.remove();state.chatMenuReturn?.setAttribute?.('aria-expanded','false');if(refocus)state.chatMenuReturn?.focus?.();}
document.addEventListener('keydown',e=>{const p=document.querySelector('.chat-pop');if(!p||!p.contains(document.activeElement))return;
  if(e.key==='Escape'){e.preventDefault();closeChatMenu(true);}
  else if(e.key==='Tab')setTimeout(()=>{if(!p.contains(document.activeElement))closeChatMenu();},0);
  else if(e.key==='ArrowDown'||e.key==='ArrowUp'){e.preventDefault();const items=[...p.querySelectorAll('button:not([disabled])')],i=items.indexOf(document.activeElement);items[(i+(e.key==='ArrowDown'?1:-1)+items.length)%items.length]?.focus();}});
document.addEventListener('pointerdown',e=>{const p=document.querySelector('.chat-pop');if(p&&!p.contains(e.target)&&!e.target.closest('[data-chat-menu]'))closeChatMenu();},true);
// Any action inside the menu closes it once its own handler has run (order buttons keep it open to click again).
document.addEventListener('click',e=>{const b=e.target.closest('.chat-pop button');if(b&&!b.closest('.pop-order'))setTimeout(()=>closeChatMenu(),0);});
async function setTeamLead(teamId,lead,who){
  const t=(state.teams||[]).find(t=>t.id===teamId);if(!t)return;
  const saved=await api('team-member',{agent:lead||who,team:teamId,lead:!!lead});
  state.teams=state.teams.map(x=>x.id===teamId?saved:x);renderAgents();
  toast(lead?`${agent(lead)?.name||''}이(가) ${t.name}의 팀장입니다 — 이 팀 알림은 팀장에게서만 옵니다.`:`${t.name}의 팀장 지정을 풀었습니다.`);
}
let organizationBusy=false;
async function changeTeamMember(id,teamId,remove=false){
  if(organizationBusy)return;organizationBusy=true;const project=state.projectId,modalVersion=state.modalVersion,query=$('#boardSearch')?.value||'',scroll=$('.board-teams')?.scrollLeft||0;
  try{
    const saved=await api('team-member',{agent:id,team:teamId,remove},project);
    if(state.projectId!==project)return;
    state.teams=state.teams.map(t=>t.id===teamId?saved:t);renderAgents();
    if(modalVersion===state.modalVersion){if($('#teamBoard')){teamBoard();$('#boardSearch').value=query;$('#boardSearch').oninput({target:$('#boardSearch')});$('.board-teams').scrollLeft=scroll;}else closeModal();}
    toast(remove?'팀에서 연결을 해제했습니다. 대화는 유지됩니다.':`${saved.name}에 추가했습니다.`);
  }catch(error){toast(error.message);}finally{organizationBusy=false;}
}
function newTeamWithMember(id){teamEditor();const input=$(`[name="teamMember"][value="${id}"]`);if(input)input.checked=true;$('#teamName').focus();}
function boardCard(a,team){return `<article class="board-card" draggable="true" data-drag-agent="${esc(a.id)}"><span class="avatar ${avatarClass(a)}">${initials(a)}</span><div><strong>${esc(a.name)}</strong>${a.role?`<small title="${esc(a.role)}">${esc(a.role)}</small>`:``}<small>${esc(providerName(a.provider))} · ${esc(statusNames[a.status]||a.status)}</small></div><button data-chat-menu="${esc(a.id)}" aria-label="${esc(a.name)} 대화 메뉴">⋯</button>${team?`<button data-remove-member="${esc(a.id)}" data-target-team="${esc(team)}" aria-label="${esc(a.name)} 팀에서 빼기">×</button>`:''}</article>`;}
function unassignedConversations(){
  const assigned=new Set((state.teams||[]).flatMap(t=>t.members));
  return orderedAgents().filter(a=>!assigned.has(a.id));
}
function renderBoardLibrary(query=''){
  const available=unassignedConversations(),search=query.trim().toLowerCase();
  const matches=available.filter(a=>`${a.name} ${a.role||''}`.toLowerCase().includes(search));
  $('#boardLibrary').innerHTML=matches.map(a=>boardCard(a)).join('')||`<p class="form-note">${available.length?'검색 결과가 없습니다.':'모든 대화가 팀에 배치되었습니다. 팀에서 대화를 빼면 여기에 다시 표시됩니다.'}</p>`;
}
function teamBoard(){
  modal('팀 구성',`<div id="teamBoard"><div class="board-toolbar"><p>왼쪽 대화 카드를 팀으로 끌어 넣으세요.<br><small>팀에 넣은 대화는 왼쪽 목록에서 빠집니다. ⋯ 메뉴에서 다른 팀에도 연결할 수 있습니다.</small></p><button id="boardNewTeam" class="primary">＋ 새 팀</button></div><div class="team-board"><section class="board-library"><h3>팀 미지정 대화 <span>${unassignedConversations().length}</span></h3><input id="boardSearch" aria-label="팀 구성 대화 검색" placeholder="대화 검색"><div id="boardLibrary"></div><button id="boardImport">기존 대화 가져오기 ↗</button></section><div class="board-teams">${(state.teams||[]).map(t=>`<section class="board-lane" data-drop-team="${esc(t.id)}"><header><h3>${esc(t.name)} <span>${t.members.filter(id=>agent(id)).length}</span></h3><button data-edit-team="${esc(t.id)}" aria-label="${esc(t.name)} 팀 설정">설정</button></header><p>${esc(t.context||'팀의 목표를 설정해 주세요.')}</p>${t.members.map(id=>agent(id)).filter(Boolean).map(a=>boardCard(a,t.id)).join('')}<div class="board-drop-hint">여기에 대화를 놓아 팀에 추가</div><button data-connect-team="${esc(t.id)}">대화 공유 연결</button></section>`).join('')}<button class="new-team-drop" data-drop-new-team>＋ 새 팀 만들기<br><small>대화를 여기에 놓아 시작하세요</small></button></div></div></div>`,'WORKSPACE / TEAMS');
  $('#modal').classList.add('team-dialog');$('#boardNewTeam').onclick=()=>teamEditor();$('#boardImport').onclick=importConversations;
  renderBoardLibrary();
  $('#boardSearch').oninput=e=>renderBoardLibrary(e.target.value);
}
$('#manageTeams').onclick=teamBoard;
let dragged=null;
document.addEventListener('dragstart',e=>{const card=e.target.closest('[data-drag-agent]');if(!card)return;if(card.closest('.agent-row,.workspace-tab')){e.preventDefault();return;}dragged={id:card.dataset.dragAgent,project:state.projectId,tab:!!card.dataset.tabId};state.draggingLayout=true;e.dataTransfer.effectAllowed='copyMove';e.dataTransfer.setData('application/x-pitcrew-agent',dragged.id);card.classList.add('dragging');document.body.classList.add('organizing');});
function finishOrganizationDrag(){dragged=null;state.draggingLayout=false;document.body.classList.remove('organizing');document.querySelectorAll('.drop-target,.dragging').forEach(el=>el.classList.remove('drop-target','dragging'));}
document.addEventListener('dragend',finishOrganizationDrag);
function organizationTarget(e){return e.target.closest('[data-drop-team],[data-drop-new-team],[data-tab-id],.agent-row,#comparePanel,.conversation-head,[data-drop-compare]');}
document.addEventListener('dragover',e=>{if(!dragged||dragged.project!==state.projectId)return;const target=organizationTarget(e);if(target){e.preventDefault();document.querySelectorAll('.drop-target').forEach(el=>el.classList.remove('drop-target'));target.classList.add('drop-target');}});
document.addEventListener('drop',async e=>{
  if(!dragged||dragged.project!==state.projectId)return;const target=organizationTarget(e),id=dragged.id;if(!target)return;e.preventDefault();finishOrganizationDrag();
  if(target.dataset.dropTeam)await changeTeamMember(id,target.dataset.dropTeam);
  else if(target.hasAttribute('data-drop-new-team'))newTeamWithMember(id);
  else if(target.dataset.tabId)reorderConversation(id,target.dataset.tabId,true,e.clientX>target.getBoundingClientRect().x+target.getBoundingClientRect().width/2);
  else if(target.classList.contains('agent-row'))reorderConversation(id,target.dataset.dragAgent,false,e.clientY>target.getBoundingClientRect().y+target.getBoundingClientRect().height/2);
  else if(id!==state.selected)compare(id);
});
document.addEventListener('click',async e=>{
  const b=e.target.closest('button');if(!b)return;
  try{
    if(b.hasAttribute('data-board-open'))teamBoard();
    else if(b.hasAttribute('data-drop-new-team'))teamEditor();
    else if(b.dataset.closeTab)closeWorkspaceTab(b.dataset.closeTab);
    else if(b.dataset.tabTeam!==undefined){const t=(state.teams||[]).find(t=>t.id===b.dataset.tabTeam);
      state.selectedTeam=t?t.id:'__unassigned';localStorage.setItem('pitcrew.team.v2',state.selectedTeam);renderAgents();
      const go=t&&t.lead&&agent(t.lead)?t.lead:workspaceLayout().tabs.find(id=>(t?t.members.includes(id):!(state.teams||[]).some(x=>x.members.includes(id))));if(go&&go!==state.selected)select(go);}
    else if(b.dataset.chatMenu){if(document.querySelector(`.chat-pop[data-for="${b.dataset.chatMenu}"]`))closeChatMenu();else chatOrganizationMenu(b.dataset.chatMenu,b);}
    else if(b.dataset.setLead!==undefined&&b.dataset.targetTeam)await setTeamLead(b.dataset.targetTeam,b.dataset.setLead,b.dataset.who);
    else if(b.dataset.renameChat)renameConversation(b.dataset.renameChat);
    else if(b.dataset.openChat){closeModal();select(b.dataset.openChat);}
    else if(b.dataset.showSide)compare(b.dataset.showSide);
    else if(b.dataset.closeChat){closeModal();closeWorkspaceTab(b.dataset.closeChat);}
    else if(b.dataset.archiveChat)await archiveConversation(b.dataset.archiveChat);
    else if(b.dataset.addMember)await changeTeamMember(b.dataset.addMember,b.dataset.targetTeam);
    else if(b.dataset.removeMember)await changeTeamMember(b.dataset.removeMember,b.dataset.targetTeam,true);
    else if(b.dataset.newTeamMember)newTeamWithMember(b.dataset.newTeamMember);
    else if(b.dataset.tabLeft||b.dataset.tabRight){const id=b.dataset.tabLeft||b.dataset.tabRight,list=workspaceLayout().tabs,i=list.indexOf(id),j=i+(b.dataset.tabLeft?-1:1);const team=x=>(state.teams||[]).find(t=>t.members.includes(x))?.id||'';if(i>=0&&j>=0&&j<list.length&&team(list[i])!==team(list[j]))toast('탭은 같은 팀 묶음 안에서만 옮길 수 있습니다.');else if(i>=0&&j>=0&&j<list.length){[list[i],list[j]]=[list[j],list[i]];saveWorkspaceLayout();renderWorkspaceTabs();toast('탭 순서를 변경했습니다.');}}
    else if((b.dataset.orderUp||b.dataset.orderDown)&&state.sidebarPinned?.get(b.dataset.orderUp||b.dataset.orderDown))toast(state.sidebarPinned.get(b.dataset.orderUp||b.dataset.orderDown));
    else if(b.dataset.orderUp||b.dataset.orderDown){const id=b.dataset.orderUp||b.dataset.orderDown,rows=orderedAgents(),i=rows.findIndex(a=>a.id===id),j=i+(b.dataset.orderUp?-1:1);if(rows[j]){const list=rows.map(a=>a.id);[list[i],list[j]]=[list[j],list[i]];workspaceLayout().order=list;saveWorkspaceLayout();renderAgents();toast('대화 순서를 변경했습니다.');}}
  }catch(error){toast(error.message);}
});
document.addEventListener('contextmenu',e=>{const row=e.target.closest('[data-drag-agent]');if(row){e.preventDefault();chatOrganizationMenu(row.dataset.dragAgent,row,{x:e.clientX,y:e.clientY});}});

const splitDrop=document.createElement('div');splitDrop.className='split-drop';splitDrop.dataset.dropCompare='';splitDrop.textContent='여기에 놓아 나란히 보기';$('.chat-columns').append(splitDrop);

// Pointer previews remain outside scrolling containers; committing happens only on release.
let rowPointer=null,suppressRowClickUntil=0;
function clearPointerTarget(){
  document.querySelectorAll('.drop-target').forEach(el=>el.classList.remove('drop-target'));
  if(rowPointer?.line)rowPointer.line.hidden=true;
}
function pointerDestination(p){
  const hit=document.elementFromPoint(p.cx,p.cy),target=hit&&organizationTarget({target:hit});
  if(!target)return null;
  const isTab=target.hasAttribute('data-tab-id'),isRow=target.classList.contains('agent-row');
  if(isTab||isRow){
    if(isTab!==p.tab||target===p.source)return null;
    const rect=target.getBoundingClientRect();
    return {target,after:isTab?p.cx>rect.left+rect.width/2:p.cy>rect.top+rect.height/2};
  }
  if(target.hasAttribute('data-drop-compare')||target.id==='comparePanel'||target.classList.contains('conversation-head'))return {target};
  return null;
}
function drawPointerDrag(){
  const p=rowPointer;if(!p?.moved)return;
  if(p.project!==state.projectId||!p.source.isConnected){endPointerDrag(false);return;}
  p.preview.style.transform=`translate3d(${p.cx-p.dx}px,${p.cy-p.dy}px,0)`;
  const list=$(p.tab?'#workspaceTabs':'#agents'),r=list.getBoundingClientRect();
  if(p.cx>=r.left&&p.cx<=r.right&&p.cy>=r.top&&p.cy<=r.bottom){
    if(p.tab)list.scrollLeft+=p.cx>r.right-32?10:p.cx<r.left+32?-10:0;
    else list.scrollTop+=p.cy>r.bottom-32?10:p.cy<r.top+32?-10:0;
  }
  clearPointerTarget();const dest=pointerDestination(p);
  if(dest){
    const t=dest.target,r=t.getBoundingClientRect();
    if(t.hasAttribute('data-tab-id')||t.classList.contains('agent-row')){
      p.line.hidden=false;
      Object.assign(p.line.style,p.tab?{left:(dest.after?r.right:r.left)-2+'px',top:r.top+'px',width:'3px',height:r.height+'px'}:{left:r.left+'px',top:(dest.after?r.bottom:r.top)-2+'px',width:r.width+'px',height:'3px'});
    }else t.classList.add('drop-target');
  }
  p.frame=requestAnimationFrame(drawPointerDrag);
}
function endPointerDrag(commit){
  const p=rowPointer;if(!p)return;
  const dest=commit&&p.project===state.projectId?pointerDestination(p):null;
  if(p.frame)cancelAnimationFrame(p.frame);
  if(p.moved)suppressRowClickUntil=Date.now()+350;
  clearPointerTarget();p.preview?.remove();p.line?.remove();p.source.classList.remove('pointer-origin');
  rowPointer=null;finishOrganizationDrag();
  if(p.source.hasPointerCapture?.(p.pointer))p.source.releasePointerCapture(p.pointer);
  if(dest){const t=dest.target;
    if(t.hasAttribute('data-tab-id'))reorderConversation(p.id,t.dataset.tabId,true,dest.after);
    else if(t.classList.contains('agent-row'))reorderConversation(p.id,t.dataset.dragAgent,false,dest.after);
    else if(p.id!==state.selected)compare(p.id);
  }
  if(p.moved)renderAgents();
}
document.addEventListener('pointerdown',e=>{
  suppressRowClickUntil=0;
  const source=e.target.closest('.agent-row,.workspace-tab');
  if(!source||e.button!==0||e.pointerType==='touch'||e.target.closest('.row-menu,[data-close-tab]'))return;
  const r=source.getBoundingClientRect();
  // Polling must not replace the pressed node before pointer capture starts.
  state.draggingLayout=true;
  rowPointer={id:source.dataset.dragAgent,project:state.projectId,source,tab:source.hasAttribute('data-tab-id'),x:e.clientX,y:e.clientY,cx:e.clientX,cy:e.clientY,dx:e.clientX-r.left,dy:e.clientY-r.top,pointer:e.pointerId,moved:false};
});
document.addEventListener('pointermove',e=>{
  const p=rowPointer;if(!p||p.pointer!==e.pointerId)return;
  if(!p.source.isConnected||p.project!==state.projectId){endPointerDrag(false);return;}
  p.cx=e.clientX;p.cy=e.clientY;
  if(!p.moved&&Math.hypot(p.cx-p.x,p.cy-p.y)<7)return;
  e.preventDefault();
  if(!p.moved){
    p.moved=true;state.draggingLayout=true;document.body.classList.add('organizing');
    const r=p.source.getBoundingClientRect(),preview=p.source.cloneNode(true);
    for(const el of [preview,...preview.querySelectorAll('*')])for(const a of [...el.attributes])if(a.name==='id'||a.name==='draggable'||a.name.startsWith('data-'))el.removeAttribute(a.name);
    preview.classList.add('drag-preview');preview.setAttribute('aria-hidden','true');preview.inert=true;
    Object.assign(preview.style,{width:r.width+'px',height:r.height+'px'});document.body.append(preview);p.preview=preview;
    p.line=document.createElement('div');p.line.className='drag-insertion';p.line.setAttribute('aria-hidden','true');document.body.append(p.line);
    p.source.classList.add('pointer-origin');p.source.setPointerCapture(e.pointerId);drawPointerDrag();
  }
});
document.addEventListener('pointerup',e=>{if(rowPointer?.pointer===e.pointerId){rowPointer.cx=e.clientX;rowPointer.cy=e.clientY;endPointerDrag(rowPointer.moved);}});
document.addEventListener('pointercancel',()=>endPointerDrag(false));
document.addEventListener('lostpointercapture',()=>{if(rowPointer?.moved)endPointerDrag(false);});
document.addEventListener('visibilitychange',()=>{if(document.hidden)endPointerDrag(false);});
globalThis.addEventListener?.('blur',()=>endPointerDrag(false));
document.addEventListener('keydown',e=>{if(e.key==='Escape'&&rowPointer){e.preventDefault();endPointerDrag(false);}});
document.addEventListener('click',e=>{if(Date.now()<suppressRowClickUntil){e.preventDefault();e.stopImmediatePropagation();}},true);

function filterConversations(){state.chatQuery=$('#chatSearch').value.trim().toLowerCase();$('#clearChatSearch').classList.toggle('hidden',!state.chatQuery);renderAgents();}
$('#chatSearch').oninput=filterConversations;
$('#clearChatSearch').onclick=()=>{$('#chatSearch').value='';filterConversations();$('#chatSearch').focus();};
$('#chatSearch').onkeydown=e=>{if(e.key==='Escape'){$('#chatSearch').value='';filterConversations();}};
document.addEventListener('keydown',e=>{if((e.ctrlKey||e.metaKey)&&e.key.toLowerCase()==='k'&&!$('#modal').open){e.preventDefault();document.body.classList.add('navigation-open');$('#chatSearch').focus();$('#chatSearch').select();}});

'use strict';
// Navigation owns layout/search only. It does not send messages or change permissions.
globalThis.PitCrewNavigation=(()=>{
  // Layout helpers also run without a browser; mount navigation only on its page.
  if(!globalThis.document?.getElementById?.('contextPanel'))return;
  const panel=$('#contextPanel'),toggle=$('#contextToggle');let returnFocus=null;
  function contextOpen(open){
    if(open)returnFocus=document.activeElement;
    panel.classList.toggle('hidden',!open);panel.classList.toggle('visible',open);
    document.body.classList.toggle('context-open',open);
    $('.workspace').style.gridTemplateColumns='';
    toggle.setAttribute('aria-expanded',String(open));
    if(open)$('#contextClose').focus();else if(returnFocus?.isConnected)returnFocus.focus();
  }
  toggle.onclick=()=>contextOpen(panel.classList.contains('hidden')||getComputedStyle(panel).display==='none');
  $('#contextClose').onclick=()=>contextOpen(false);
  toggle.setAttribute('aria-controls','contextPanel');contextOpen(false);
  const shade=document.createElement('button');shade.id='contextShade';shade.setAttribute('aria-label','컨텍스트 닫기');shade.tabIndex=-1;shade.onclick=()=>contextOpen(false);document.body.append(shade);
  // Keep management available without taking space above the conversation list.
  const management=document.createElement('details');management.className='navigation-management';management.innerHTML='<summary>프로젝트·팀 관리</summary>';
  $('.sidebar-tools').before(management);for(const id of ['projectsOpen','manageTeams','importChats'])management.append($('#'+id));
  const help=document.createElement('button');help.id='shortcutsOpen';help.textContent='키보드 도움말';management.append(help);
  help.onclick=()=>modal('키보드로 탐색하기','<dl class="shortcuts"><dt>Ctrl / ⌘ + K</dt><dd>대화 이름·역할 검색</dd><dt>Ctrl / ⌘ + Shift + F</dt><dd>현재 대화 본문 찾기</dd><dt>Ctrl / ⌘ + Shift + K</dt><dd>모든 대화에서 찾기</dd><dt>Ctrl + O</dt><dd>도구 활동 자세히 보기</dd><dt>Alt + I</dt><dd>현재 대화 입력창</dd><dt>Escape</dt><dd>패널·검색 닫기</dd><dt>검색창 Enter / Shift + Enter</dt><dd>다음 / 이전 결과</dd></dl><p class="form-note">브라우저의 Ctrl+F 찾기는 그대로 사용할 수 있습니다.</p>','KEYBOARD');
  const textBlock=(label,value,action)=>`<section class="context-section"><div class="context-label">${esc(label)}<button class="quiet" ${action}>편집</button></div><p>${esc(value)}</p></section>`;
  function context(d){
    const a=d.agent,teams=(state.teams||[]).filter(t=>t.members.includes(a.id)),peers=(a.peers||[]).map(agent).filter(Boolean);
    const items=[['이 대화의 지시',a.instructions,'data-settings'],['이 대화의 자료',a.context,'data-settings'],['프로젝트 공통',state.shared,'data-shared'],...teams.map(t=>[t.name+' · 팀 자료',t.context,`data-edit-team="${esc(t.id)}"`])];
    const filled=items.filter(x=>x[1]?.trim()),empty=items.filter(x=>!x[1]?.trim());
    return `<section class="context-section"><h3>${esc(a.role||'역할 미지정')}</h3><p>직접 설정한 지시·자료 ${filled.length}개 · 연결 대화 ${peers.length}개</p><p class="hint">프로젝트 / 팀 / 이 대화의 범위입니다. 다른 팀원의 대화는 연결해야 전달됩니다.</p></section>`+
      filled.map(x=>textBlock(...x)).join('')+
      (empty.length?`<details class="context-section"><summary>자료 추가 · ${empty.length}개 빈 항목</summary>${empty.map(([label,,action])=>`<button class="context-add" ${action}>＋ ${esc(label)}</button>`).join('')}</details>`:'')+
      `<section class="context-section"><div class="context-label">연결한 대화 · ${peers.length}<button class="quiet" data-peers>관리</button></div>${peers.map(p=>`<button class="context-source" data-compare="${esc(p.id)}">${esc(p.name)}<span>${esc(providerName(p.provider))} · 나란히 보기</span></button>`).join('')}${peers.length?'<p class="hint">각 대화 최근 내용 최대 12,000자</p>':''}</section>`+
      ((d.children||[]).length?`<section class="context-section"><div class="context-label">하위 에이전트 기록</div>${d.children.map(c=>`<button class="context-source" data-watch="${esc(c.id)}">${esc(c.label||c.name)}</button>`).join('')}</section>`:'')+
      `<details class="context-section"><summary>실행 정보</summary><p>요청 모델: ${esc(a.model||'기본값')}<br>보고된 모델: ${esc(a.actualModel||'실행 후 확인')}</p>${a.usage?`<p>최근 입력 토큰: ${esc(a.usage?.last?.inputTokens??a.usage?.input_tokens??'미제공')}</p>`:''}${a.cost!==undefined?`<p>SDK 비용 추정: $${Number(a.cost).toFixed(4)}</p>`:''}<p class="hint">기록과 전달 자료이며 모델 내부의 전체 컨텍스트가 아닙니다.</p><button class="context-link" data-export>대화 JSON 내보내기 ↗</button></details>`;
  }
  function empty(){
    const team=(state.teams||[]).find(t=>t.id===state.selectedTeam);
    const title=team?`${team.name} 팀에 열린 대화가 없습니다`:!state.agents.length?'첫 대화를 시작하세요':'열린 탭이 없습니다';
    const description=team?'기존 대화를 이 팀에 추가하거나 새 팀원을 만들어 주세요.':!state.agents.length?'프로젝트에 맞는 역할을 만들거나 기존 대화를 가져오세요.':'왼쪽 목록에서 대화를 다시 열 수 있습니다. 기록은 그대로 남아 있습니다.';
    return `<div class="welcome"><h2>${esc(title)}</h2><p>${esc(description)}</p><div class="welcome-actions">${team?`<button data-edit-team="${esc(team.id)}">기존 대화 추가</button><button data-nav-new>새 팀원 만들기</button>`:'<button data-nav-new>새 대화 만들기</button><button data-setup-action="import">대화 가져오기</button>'}</div></div>`;
  }
  document.addEventListener('click',e=>{if(e.target.closest('[data-nav-new]'))$('#newAgent').click();});
  let generation=0,hits=[],hit=-1,searchTimer,searchOwner='',searchQuery='';
  function clearHits(){for(const el of document.querySelectorAll('.search-hit'))el.classList.remove('search-hit');}
  function closeFind(){generation++;clearTimeout(searchTimer);$('#chatFindBar').classList.add('hidden');clearHits();$('#findInChat').focus();}
  function openFind(){if(!state.selected){toast('먼저 대화를 선택하세요.');return;}$('#chatFindBar').classList.remove('hidden');$('#chatFindInput').focus();$('#chatFindInput').select();}
  $('#findInChat').onclick=openFind;$('#chatFindClose').onclick=closeFind;
  async function search(){
    const seq=++generation,owner=state.selected,project=state.projectId,q=$('#chatFindInput').value.trim().toLocaleLowerCase();searchOwner=owner;searchQuery=q;hits=[];hit=-1;clearHits();
    if(!q){$('#chatFindCount').textContent='본문·도구 기록';return;}
    if(state.detail?.agent.id!==owner){$('#chatFindCount').textContent='대화를 불러오는 중';return;}
    $('#chatFindCount').textContent='전체 기록 검색 중…';
    const records=state.detail.messages.map(m=>({...m}));let cursor=0;
    try{
      // Explicit search fetches trimmed originals, bounded to four concurrent reads.
      await Promise.all(Array.from({length:4},async()=>{while(cursor<records.length){const i=cursor++,m=records[i];if(seq!==generation)return;if(m.truncated)records[i]=await api(`message?agent=${encodeURIComponent(owner)}&id=${encodeURIComponent(m.id)}`,undefined,project);}}));
      if(seq!==generation||owner!==state.selected||project!==state.projectId)return;
      hits=records.filter(m=>[m.text,m.input,m.title,m.label].filter(Boolean).join('\n').toLocaleLowerCase().includes(q));
      move(1);
    }catch(e){if(seq===generation)$('#chatFindCount').textContent='검색 실패 · 다시 입력해 재시도';}
  }
  function move(dir){
    clearHits();if(searchOwner!==state.selected){search();return;}
    if(!hits.length){$('#chatFindCount').textContent='결과 없음';return;}
    hit=(hit+dir+hits.length)%hits.length;const m=hits[hit];
    let el=[...$('#messages').querySelectorAll('[data-message],details[data-tool]')].find(e=>e.dataset.message===m.id||e.dataset.tool===m.id);
    if(el){
      // Preserve timeline ownership; reveal original text only through its existing full-view action.
      const full=el.querySelector('[data-tool-full]');if(full)full.click();
      for(let p=el;p&&p!==$('#messages');p=p.parentElement)if(p.matches('details[data-tool]'))p.open=true;
      el.classList.add('search-hit');el.scrollIntoView({block:'center',behavior:'instant'});
    }
    $('#chatFindCount').textContent=`${hit+1} / ${hits.length}개 기록`;
  }
  $('#chatFindInput').oninput=()=>{generation++;clearTimeout(searchTimer);searchTimer=setTimeout(search,250);};
  $('#chatFindPrev').onclick=()=>move(-1);$('#chatFindNext').onclick=()=>move(1);
  $('#chatFindInput').onkeydown=e=>{if(e.key==='Enter'){e.preventDefault();move(e.shiftKey?-1:1);}};
  // Close search when changing conversations instead of presenting stale hits.
  new MutationObserver(()=>{if(searchOwner&&searchOwner!==state.selected){generation++;hits=[];clearHits();searchOwner='';$('#chatFindInput').value='';$('#chatFindCount').textContent='본문·도구 기록';}}).observe($('#selectedName'),{childList:true});
  document.addEventListener('keydown',e=>{
    if($('#modal').open)return;
    if((e.ctrlKey||e.metaKey)&&e.shiftKey&&e.key.toLowerCase()==='f'){e.preventDefault();openFind();}
    if(e.altKey&&(e.key==='ArrowDown'||e.key==='ArrowUp')&&!e.target.closest('input,textarea,select,[contenteditable]')){
      const ids=[...document.querySelectorAll('#agents .agent-row')].map(r=>r.dataset.dragAgent),i=ids.indexOf(state.selected);
      const next=ids[(i+(e.key==='ArrowDown'?1:-1)+ids.length)%ids.length];if(next&&next!==state.selected){e.preventDefault();select(next);}
    }
    if(e.altKey&&e.key.toLowerCase()==='i'){e.preventDefault();document.body.classList.remove('navigation-open');if(!$('#prompt').disabled&&!$('#composer').classList.contains('hidden'))$('#prompt').focus();else toast('이 대화는 읽기 전용입니다.');}
    if(e.key==='Escape'){
      if(!panel.classList.contains('hidden')){e.preventDefault();contextOpen(false);}
      else if(!$('#chatFindBar').classList.contains('hidden')){e.preventDefault();closeFind();}
      else if(document.body.classList.contains('navigation-open')){document.body.classList.remove('navigation-open');$('#navToggle').focus();}
    }
    if(e.key==='Tab'&&innerWidth<=1100&&!panel.classList.contains('hidden')){
      const f=[...panel.querySelectorAll('button,summary,a,input')].filter(x=>x.getClientRects().length),first=f[0],last=f.at(-1);
      if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}
    }
  });
  // The module is loaded after initial script setup; invalidate an early context render.
  queueMicrotask(()=>{state.contextHash='';if(state.detail)renderContext(state.detail);});
  return {context,empty,contextOpen};
})();

/* Lucide SVG subset, pinned in r3-icons.json; ISC / Feather MIT: lucide.LICENSE. */
globalThis.PitCrewIcons=(()=>{const paths={"search": "<path d=\"m21 21-4.34-4.34\" />\n  <circle cx=\"11\" cy=\"11\" r=\"8\" />", "panel-right": "<rect width=\"18\" height=\"18\" x=\"3\" y=\"3\" rx=\"2\" />\n  <path d=\"M15 3v18\" />", "panel-left": "<rect width=\"18\" height=\"18\" x=\"3\" y=\"3\" rx=\"2\" />\n  <path d=\"M9 3v18\" />", "x": "<path d=\"M18 6 6 18\" />\n  <path d=\"m6 6 12 12\" />", "plus": "<path d=\"M5 12h14\" />\n  <path d=\"M12 5v14\" />", "paperclip": "<path d=\"m16 6-8.414 8.586a2 2 0 0 0 2.829 2.829l8.414-8.586a4 4 0 1 0-5.657-5.657l-8.379 8.551a6 6 0 1 0 8.485 8.485l8.379-8.551\" />", "arrow-up": "<path d=\"m5 12 7-7 7 7\" />\n  <path d=\"M12 19V5\" />", "arrow-right": "<path d=\"M5 12h14\" />\n  <path d=\"m12 5 7 7-7 7\" />", "arrow-down": "<path d=\"M12 5v14\" />\n  <path d=\"m19 12-7 7-7-7\" />", "git-branch": "<path d=\"M15 6a9 9 0 0 0-9 9V3\" />\n  <circle cx=\"18\" cy=\"6\" r=\"3\" />\n  <circle cx=\"6\" cy=\"18\" r=\"3\" />", "users": "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\" />\n  <path d=\"M16 3.128a4 4 0 0 1 0 7.744\" />\n  <path d=\"M22 21v-2a4 4 0 0 0-3-3.87\" />\n  <circle cx=\"9\" cy=\"7\" r=\"4\" />", "settings-2": "<path d=\"M14 17H5\" />\n  <path d=\"M19 7h-9\" />\n  <circle cx=\"17\" cy=\"17\" r=\"3\" />\n  <circle cx=\"7\" cy=\"7\" r=\"3\" />", "ellipsis": "<circle cx=\"12\" cy=\"12\" r=\"1\" />\n  <circle cx=\"19\" cy=\"12\" r=\"1\" />\n  <circle cx=\"5\" cy=\"12\" r=\"1\" />", "moon": "<path d=\"M20.985 12.486a9 9 0 1 1-9.473-9.472c.405-.022.617.46.402.803a6 6 0 0 0 8.268 8.268c.344-.215.825-.004.803.401\" />", "sun": "<circle cx=\"12\" cy=\"12\" r=\"4\" />\n  <path d=\"M12 2v2\" />\n  <path d=\"M12 20v2\" />\n  <path d=\"m4.93 4.93 1.41 1.41\" />\n  <path d=\"m17.66 17.66 1.41 1.41\" />\n  <path d=\"M2 12h2\" />\n  <path d=\"M20 12h2\" />\n  <path d=\"m6.34 17.66-1.41 1.41\" />\n  <path d=\"m19.07 4.93-1.41 1.41\" />", "terminal": "<path d=\"M12 19h8\" />\n  <path d=\"m4 17 6-6-6-6\" />", "sparkles": "<path d=\"M11.017 2.814a1 1 0 0 1 1.966 0l1.051 5.558a2 2 0 0 0 1.594 1.594l5.558 1.051a1 1 0 0 1 0 1.966l-5.558 1.051a2 2 0 0 0-1.594 1.594l-1.051 5.558a1 1 0 0 1-1.966 0l-1.051-5.558a2 2 0 0 0-1.594-1.594l-5.558-1.051a1 1 0 0 1 0-1.966l5.558-1.051a2 2 0 0 0 1.594-1.594z\" />\n  <path d=\"M20 2v4\" />\n  <path d=\"M22 4h-4\" />\n  <circle cx=\"4\" cy=\"20\" r=\"2\" />", "check": "<path d=\"M20 6 9 17l-5-5\" />", "circle-question-mark": "<circle cx=\"12\" cy=\"12\" r=\"10\" />\n  <path d=\"M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3\" />\n  <path d=\"M12 17h.01\" />", "link": "<path d=\"M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71\" />\n  <path d=\"M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71\" />", "clock-3": "<circle cx=\"12\" cy=\"12\" r=\"10\" />\n  <path d=\"M12 6v6h4\" />", "corner-down-right": "<path d=\"m15 10 5 5-5 5\" />\n  <path d=\"M4 4v7a4 4 0 0 0 4 4h12\" />", "copy": "<rect width=\"14\" height=\"14\" x=\"8\" y=\"8\" rx=\"2\" ry=\"2\" />\n  <path d=\"M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2\" />", "columns-2": "<rect width=\"18\" height=\"18\" x=\"3\" y=\"3\" rx=\"2\" />\n  <path d=\"M12 3v18\" />", "slash": "<path d=\"M22 2 2 22\" />", "at-sign": "<circle cx=\"12\" cy=\"12\" r=\"4\" />\n  <path d=\"M16 8v5a3 3 0 0 0 6 0v-1a10 10 0 1 0-4 8\" />"};return {html:name=>`<svg class="pc-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">${paths[name]||paths['circle-question-mark']}</svg>`};})();

// Decorate controls only; never rewrite user text, the composer, or timeline output.
(()=>{
  if(!globalThis.document?.getElementById?.('contextPanel'))return;
  const icons=PitCrewIcons.html;
  const controls={attach:['paperclip',''],slashBtn:['slash',''],mentionBtn:['at-sign',''],themeToggle:['moon',''],navToggle:['panel-left',''],contextToggle:['panel-right',''],contextClose:['x',''],modalClose:['x',''],compareClose:['x',''],newAgent:['plus',''],findInChat:['search','찾기'],compareOpen:['columns-2','비교'],settingsOpen:['settings-2','설정'],chatFindPrev:['arrow-up',''],chatFindNext:['arrow-down',''],chatFindClose:['x','']};
  function decorate(){
    for(const [id,[icon,label]] of Object.entries(controls)){const el=document.getElementById(id);if(el){if(label&&!el.hasAttribute('aria-label'))el.setAttribute('aria-label',label);if(!el.querySelector('.pc-icon'))el.innerHTML=icons(icon)+(label?`<span>${label}</span>`:'');}}
    for(const el of document.querySelectorAll('.row-menu,[data-close-tab]'))if(!el.querySelector('.pc-icon'))el.innerHTML=icons(el.hasAttribute('data-close-tab')?'x':'ellipsis');
    for(const el of document.querySelectorAll('.avatar'))if(!el.querySelector('.pc-icon'))el.innerHTML=icons(el.classList.contains('codex')?'terminal':'sparkles');
  }
  decorate();let scheduled=false;
  new MutationObserver(()=>{if(scheduled)return;scheduled=true;queueMicrotask(()=>{scheduled=false;decorate();});}).observe(document.querySelector('.workspace'),{subtree:true,childList:true});
})();
/* Team flow: read-only relationships and provenance. Never sends or creates work. */
globalThis.PitCrewFlow=(()=>{
  const count=v=>Number.isSafeInteger(v)&&v>=0?v:0;
  const kinds={handoff:'인계',delegate:'팀장이 맡김',cycle:'사이클 단계',forward:'메시지 전달',peer:'대화 연결',queue:'대기열',rule:'인계 예정',mention:'멘션',research:'리서치 요청',message:'메시지'};
  function normalize(input){
    const nodes=Array.isArray(input?.nodes)?input.nodes.filter(n=>n&&typeof n.id==='string').map(n=>({id:n.id,name:String(n.name||'이름 없는 대화'),provider:String(n.provider||''),status:String(n.status||'idle'),queued:count(n.queued),rules:count(n.rules)})):[];
    const edges=Array.isArray(input?.edges)?input.edges.filter(e=>e&&Object.hasOwn(kinds,e.kind)&&typeof e.from==='string'&&typeof e.to==='string').map((e,i)=>({from:e.from,to:e.to,kind:e.kind,label:String(e.label||''),at:e.at??null,messageId:typeof e.messageId==='string'?e.messageId:'',index:i})):[];
    return {nodes,edges};
  }
  function routes(edges){
    const grouped=new Map();
    for(const e of edges){const key=JSON.stringify([e.from,e.to]);let row=grouped.get(key);if(!row){row={from:e.from,to:e.to,total:0,kinds:{}};grouped.set(key,row);}row.total++;row.kinds[e.kind]=(row.kinds[e.kind]||0)+1;}
    return [...grouped.values()].sort((a,b)=>b.total-a.total);
  }
  function origin(m,a){
    const o=m.origin;if(!o||typeof o.agent!=='string'||!o.agent)return '';
    const label=Object.hasOwn(kinds,o.kind)?kinds[o.kind]:'참조';
    return `<div class="message-origin"><span aria-hidden="true">←</span><button type="button" data-flow-agent="${esc(o.agent)}" title="출처 대화 열기">${esc(o.name||'이전 대화')}</button><span>에서 받음 · ${esc(label)}</span></div>`;
  }
  function grouped(edges){
    const groups=new Map();for(const e of edges){const key=JSON.stringify([e.from,e.to,e.kind,e.label]);if(!groups.has(key))groups.set(key,{...e,occurrences:[]});groups.get(key).occurrences.push(e);}return [...groups.values()];
  }
  function excerpt(value){const plain=String(value||'').replace(/`/g,'').replace(/\s+/g,' ').trim();if(plain.length<=90)return plain;const part=plain.slice(0,90),space=part.lastIndexOf(' ');return part.slice(0,space>0?space:90)+'…';}
  const apiObject={normalize,origin,routes,grouped,excerpt};
  if(!globalThis.document?.getElementById?.('contextPanel'))return apiObject;
  let request=0,model=null,filter='',selectedNode='',flowProject='';
  const icon=name=>PitCrewIcons.html(name);
  const openButton=document.createElement('button');openButton.id='flowOpen';openButton.type='button';openButton.className='flow-open';openButton.innerHTML=icon('git-branch')+'<span>팀 흐름</span>';openButton.title='인계·전달·연결·대기열 보기';$('.team-controls').after(openButton);
  function prettyTime(at){if(at===null||at==='')return '예정·연결';const n=typeof at==='number'?at*1000:Date.parse(at),d=new Date(n);return Number.isNaN(d.getTime())?'시각 미제공':d.toLocaleString('ko-KR',{month:'numeric',day:'numeric',hour:'2-digit',minute:'2-digit'});}
  function render(){
    const root=$('#flowContent');if(!root||!model)return;
    const map=new Map(model.nodes.map(n=>[n.id,n])),name=id=>{const n=map.get(id);return n?(globalThis.PitCrewSidebar?.label(agent(id)||n)||n.name):'삭제되었거나 다른 프로젝트의 대화';};
    const edges=grouped(model.edges.filter(e=>(!filter||e.kind===filter)&&(!selectedNode||e.from===selectedNode||e.to===selectedNode)).sort((a,b)=>(a.kind==='peer')-(b.kind==='peer')));
    root.innerHTML=`<div class="flow-toolbar"><select data-flow-select aria-label="대화별 흐름 필터"><option value="">모든 대화</option>${model.nodes.map(n=>`<option value="${esc(n.id)}" ${selectedNode===n.id?'selected':''}>${esc(n.name)}${n.queued?' · 대기 '+n.queued:''}${n.rules?' · 인계 예정 '+n.rules:''}</option>`).join('')}</select><button type="button" data-flow-refresh>새로고침</button></div><div class="flow-filters" aria-label="관계 종류">${[['','전체'],...Object.entries(kinds).filter(([k])=>model.edges.some(e=>e.kind===k))].map(([k,label])=>`<button type="button" data-flow-filter="${k}" aria-pressed="${filter===k}" class="${filter===k?'selected':''}">${label}</button>`).join('')}</div><ol class="flow-edges flow-timeline">${edges.map((e,i)=>`${(i===0||((e.kind==='peer')!==(edges[i-1].kind==='peer')))?`<li class="flow-section-label">${e.kind==='peer'?'참고 연결':'전달·인계·대기'}</li>`:''}<li class="flow-edge flow-${e.kind}"><div class="flow-route"><button type="button" data-flow-agent="${esc(e.from)}" ${map.has(e.from)?'':'disabled'}>${esc(name(e.from))}</button>${icon('arrow-right')}<button type="button" data-flow-agent="${esc(e.to)}" ${map.has(e.to)?'':'disabled'}>${esc(name(e.to))}</button></div><span class="flow-kind">${kinds[e.kind]}${e.occurrences.length>1?` ×${e.occurrences.length}`:''}</span>${e.label&&e.label!==kinds[e.kind]&&!(e.kind==='peer'&&e.label==='연결한 대화')?`<details class="flow-description"><summary aria-label="요약 펼치기">${esc(excerpt(e.label))}</summary><p>${esc(e.label)}</p>${e.occurrences.length>1?e.occurrences.map(o=>`<div class="flow-occurrence"><time>${esc(prettyTime(o.at))}</time>${o.messageId?`<button type="button" data-flow-message="${esc(o.messageId)}" data-flow-target="${esc(o.to)}">해당 메시지 보기</button>`:''}</div>`).join(''):''}</details>`:'<span></span>'}<time>${esc(e.at?prettyTime(e.at):e.kind==='peer'?'연결 중':'대기 중')}</time>${e.messageId?`<button type="button" class="flow-message-link" data-flow-message="${esc(e.messageId)}" data-flow-target="${esc(e.to)}" aria-label="해당 메시지 보기">${icon('arrow-right')}</button>`:''}</li>`).join('')}</ol>${edges.length?'':'<p class="flow-empty">아직 표시할 흐름이 없습니다.</p>'}`;

  }
  async function load(){
    const seq=++request,project=flowProject,root=$('#flowContent');if(!root)return;
    root.innerHTML='<p class="flow-loading" role="status">팀 흐름을 불러오는 중…</p>';
    try{const data=await api('flow',undefined,project);if(seq!==request||$('#flowContent')!==root)return;if(project!==state.projectId){root.innerHTML='<div class="flow-empty"><p>프로젝트가 바뀌었습니다.</p><button type="button" data-flow-refresh>현재 프로젝트 흐름 열기</button></div>';return;}model=normalize(data);render();}
    catch(error){if(seq!==request||$('#flowContent')!==root)return;root.innerHTML=`<div class="flow-empty"><h3>팀 흐름을 불러오지 못했습니다</h3><p>${esc(error.message||'연결 상태를 확인하세요.')}</p><button type="button" data-flow-refresh>다시 시도</button></div>`;}
  }
  function open(){filter='';selectedNode='';flowProject=state.projectId;model=null;modal('팀 흐름','<div id="flowContent"></div>','TEAM FLOW');$('#modal').classList.add('flow-dialog');load();}
  openButton.onclick=open;
  document.addEventListener('change',e=>{if(e.target.matches('[data-flow-select]')){selectedNode=e.target.value;render();$('[data-flow-select]')?.focus();}});
  async function jump(id,mid){
    if(flowProject&&flowProject!==state.projectId){toast('프로젝트가 변경되었습니다. 팀 흐름을 다시 열어주세요.');return;}
    if(!agent(id)){toast('이 프로젝트에서 열 수 없는 대화입니다.');return;}
    if($('#modal').open)closeModal();select(id);
    if(!mid)return;
    const project=state.projectId;await refreshDetail();if(state.selected!==id||state.projectId!==project)return;
    const el=[...$('#messages').querySelectorAll('[data-message],details[data-tool]')].find(x=>x.dataset.message===mid||x.dataset.tool===mid);
    if(!el){toast('현재 기록에서 해당 메시지를 찾지 못했습니다.');return;}
    for(let n=el;n&&n!==$('#messages');n=n.parentElement)if(n.matches('details[data-tool]'))n.open=true;
    el.scrollIntoView({block:'center',behavior:'instant'});el.classList.add('flow-destination');setTimeout(()=>el.classList.remove('flow-destination'),2500);
  }
  document.addEventListener('click',e=>{
    const b=e.target.closest('[data-flow-agent],[data-flow-message],[data-flow-filter],[data-flow-node],[data-flow-refresh]');if(!b)return;
    if(b.hasAttribute('data-flow-filter')){filter=b.dataset.flowFilter;render();$('#flowContent')?.querySelector(`[data-flow-filter="${filter}"]`)?.focus();}
    else if(b.hasAttribute('data-flow-node')){const id=b.dataset.flowNode;selectedNode=selectedNode===id?'':id;render();[...$('#flowContent').querySelectorAll('[data-flow-node]')].find(n=>n.dataset.flowNode===id)?.focus();}
    else if(b.hasAttribute('data-flow-refresh')){if(flowProject!==state.projectId)open();else load();}
    else if(b.dataset.flowMessage)jump(b.dataset.flowTarget,b.dataset.flowMessage);
    else {if(!b.closest('#flowContent'))flowProject=state.projectId;jump(b.dataset.flowAgent);}
  });
  return {...apiObject,open};
})();
/* Sidebar status/interaction view. Read-only; team membership and tab state stay separate. */
globalThis.PitCrewSidebar=(()=>{
  let mode='',hideIdle=false;
  function label(a){const peers=state.agents.filter(x=>x.name===a.name);if(peers.length<2)return a.name;const same=peers.filter(x=>x.provider===a.provider).sort((x,y)=>x.id.localeCompare(y.id));return a.name+' · '+providerName(a.provider)+(same.length>1?' '+(same.findIndex(x=>x.id===a.id)+1):'');}
  function accepts(a){const waiting=a.status==='waiting'||state.approvals.some(x=>x.agent===a.id);return (!hideIdle||a.status!=='idle'||waiting)&&(!mode||(mode==='running'?a.status==='running':mode==='waiting'?waiting||a.status==='error':a.status==='done'));}
  function badge(a){const labels=[];if(a.queue?.length)labels.push(`대기 ${a.queue.length}`);const targets=(a.handoffs||[]).map(h=>agent(h.to)?.name).filter(Boolean);if(targets.length)labels.push('→ '+targets[0]+(targets.length>1?` 외 ${targets.length-1}`:''));return labels.length?`<span class="agent-flow" title="${esc(labels.join(' · '))}">${esc(labels.join(' · '))}</span>`:'';}
  if(!globalThis.document?.getElementById?.('agents'))return {accepts,badge,label};
  const box=document.createElement('div');box.className='sidebar-status';box.innerHTML='<select id="agentStatusFilter" aria-label="대화 상태 필터"><option value="">모든 상태</option><option value="running">작업 중</option><option value="waiting">응답 필요</option><option value="done">완료</option></select><label><input id="hideIdleAgents" type="checkbox">유휴 접기</label>';
  $('#agents').before(box);$('#agentStatusFilter').onchange=e=>{mode=e.target.value;renderAgents()};$('#hideIdleAgents').onchange=e=>{hideIdle=e.target.checked;renderAgents()};
  return {accepts,badge,label};
})();

/* Put occasional actions behind native disclosure menus; preserve existing handlers and IDs. */
(()=>{
 if(!globalThis.document?.querySelector?.('.appbar'))return;
 const disclosure=(label,cls)=>{const d=document.createElement('details');d.className='compact-menu '+cls;d.innerHTML=`<summary aria-label="${label}">${label}</summary><div class="compact-menu-items"></div>`;return d;};
 const side=document.querySelector('.sidebar'),foot=document.querySelector('.sidebar-tools .side-bottom');
 const settings=disclosure('설정 · 도구','workspace-menu');
 document.querySelector('.sidebar-tools').before(settings);
 const dest=settings.lastElementChild;
 for(const control of document.querySelectorAll('.navigation-management button'))dest.append(control);document.querySelector('.navigation-management')?.remove();
 for(const id of ['projectsOpen','manageTeams','importChats','setupOpen','sourcesOpen','themeToggle','sharedOpen','activityOpen','archivedOpen','helpOpen','teamOpen']){const el=document.getElementById(id);if(el)dest.append(el);}
 for(const el of document.querySelectorAll('.appbar nav button:not(#usageOpen),.appbar nav a'))dest.append(el);
 const idle=document.querySelector('.sidebar-status label');if(idle)dest.append(idle);
 const usage=document.getElementById('usageOpen');if(usage)settings.before(usage);
 const head=disclosure('더 보기','conversation-menu');document.querySelector('.head-actions').append(head);
 for(const id of ['compareOpen','settingsOpen','contextToggle','flowOpen']){const el=document.getElementById(id);if(el)head.lastElementChild.append(el);}
 const runtime=document.querySelector('.runtime-bar');if(runtime)dest.append(runtime);
 const filters=document.createElement('div');filters.className='compact-filters';document.querySelector('.team-controls').before(filters);
 filters.append(document.getElementById('teamFilter'),document.getElementById('agentStatusFilter'));document.querySelector('.sidebar-status')?.remove();
 document.addEventListener('click',e=>{for(const d of document.querySelectorAll('.compact-menu[open]'))if(!d.contains(e.target)||e.target.closest('button,a'))d.open=false;});
 document.addEventListener('keydown',e=>{if(e.key==='Escape')for(const d of document.querySelectorAll('.compact-menu[open]')){d.open=false;d.querySelector('summary').focus();}});
})();

(()=>{if(!globalThis.document?.getElementById?.('chatSearch'))return;const field=document.getElementById('chatSearch'),wrap=document.createElement('div');wrap.className='sidebar-search-row';field.before(wrap);wrap.append(field);const btn=document.createElement('button');btn.id='globalSearchOpen';btn.type='button';btn.title='모든 대화에서 찾기';btn.setAttribute('aria-label',btn.title);btn.innerHTML=PitCrewIcons.html('search');btn.onclick=()=>globalThis.PitCrewSearch?.open(field.value);wrap.append(btn);})();
/* Keep the initial sidebar boundary between complete rows; scrolling remains native. */
(()=>{if(!globalThis.document?.getElementById?.('agents'))return;const list=document.getElementById('agents'),side=list.closest('.sidebar');let pending=false;
function fit(){pending=false;if(!side.getClientRects().length)return;const css=getComputedStyle(side);let room=side.clientHeight-parseFloat(css.paddingTop)-parseFloat(css.paddingBottom);for(const el of side.children){if(el===list||!el.getClientRects().length)continue;const s=getComputedStyle(el);if(s.position==='absolute'||s.position==='fixed')continue;room-=el.getBoundingClientRect().height+(parseFloat(s.marginTop)||0)+(parseFloat(s.marginBottom)||0);}const s=getComputedStyle(list);room-=(parseFloat(s.marginTop)||0)+(parseFloat(s.marginBottom)||0);const row=list.querySelector('.agent-row');if(!row)return;const next=row.nextElementSibling;const step=next?next.getBoundingClientRect().top-row.getBoundingClientRect().top:62;list.style.height=Math.max(62,Math.floor((room-1)/step)*step+1)+'px';}
function queueFit(){if(!pending){pending=true;requestAnimationFrame(fit);}}new ResizeObserver(queueFit).observe(side);new MutationObserver(queueFit).observe(list,{childList:true});window.addEventListener('resize',queueFit);queueFit();})();

(()=>{const menu=globalThis.document?.querySelector?.('.conversation-menu .compact-menu-items');if(!menu)return;const button=document.createElement('button');button.id='artifactsOpen';button.type='button';button.innerHTML=PitCrewIcons.html('columns-2')+'<span>결과물</span>';button.onclick=()=>globalThis.PitCrewArtifacts?.open();menu.prepend(button);})();

/* Conversation-first refresh. Reuse the existing actions, permissions and menus. */
globalThis.PitCrewRefresh=(()=>{
  const doc=globalThis.document;
  if(!doc?.querySelector?.('.conversation-head'))return {render(){}};
  const make=(id,label,action)=>{const b=doc.createElement('button');b.id=id;b.type='button';b.textContent=label;b.onclick=action;return b;};
  const side=doc.querySelector('.sidebar'),head=doc.querySelector('.conversation-head');
  const create=doc.getElementById('newAgent');
  create.classList.remove('icon');create.classList.add('new-conversation');create.innerHTML=PitCrewIcons.html('plus')+'<span>새 대화</span>';create.setAttribute('aria-label','새 대화 만들기');
  doc.querySelector('.project-switcher').after(create);
  doc.querySelector('.side-title > span').textContent='대화';
  const search=doc.getElementById('chatSearch');search.placeholder='대화 찾기 · Ctrl+K';
  const status=doc.createElement('span');status.id='conversationStatus';status.className='conversation-status';status.setAttribute('role','status');
  doc.querySelector('.conversation-title').append(status);
  let folded=false;try{folded=localStorage.getItem('pitcrew.sidebarFolded')==='1';}catch{}
  const fold=make('sidebarFold','목록 접기',()=>{folded=!folded;applyFold();try{localStorage.setItem('pitcrew.sidebarFolded',folded?'1':'0');}catch{}});
  fold.className='sidebar-fold';fold.setAttribute('aria-controls','pitcrewSidebar');side.id='pitcrewSidebar';head.prepend(fold);
  function applyFold(){doc.body.classList.toggle('sidebar-folded',folded);fold.textContent=folded?'목록 열기':'목록 접기';fold.setAttribute('aria-expanded',String(!folded));}
  applyFold();
  doc.addEventListener('keydown',e=>{if((e.ctrlKey||e.metaKey)&&e.key==='/'&&!e.isComposing){e.preventDefault();doc.getElementById('shortcutsOpen')?.click();}});
  // Promote results from the overflow menu: inspecting output is a primary task.
  const artifacts=doc.getElementById('artifactsOpen');if(artifacts){artifacts.classList.add('quiet');artifacts.title='대화의 결과물 보기';artifacts.setAttribute('aria-label','대화의 결과물 보기');doc.querySelector('.head-actions').prepend(artifacts);}
  const tools=doc.querySelector('.composer-tools');
  for(const [key,label] of [['mention','@ 참조'],['slash','/ 명령']]){
    const b=make('quick-'+key,label,()=>doc.querySelector(`#plusMenu [data-plus="${key}"]`)?.click());b.className='compose-shortcut';tools?.append(b);
  }
  const plus=doc.getElementById('plusBtn');if(plus){plus.title='파일 첨부 · 더 많은 도구';plus.setAttribute('aria-label',plus.title);}
  // Expose the theme control without requiring the settings menu.
  const theme=doc.getElementById('themeToggle');if(theme){theme.classList.add('refresh-theme');doc.querySelector('.appbar').insertBefore(theme,doc.getElementById('connection'));}
  function arrangeActions(){
    const menu=doc.querySelector('.conversation-menu .compact-menu-items'),actions=doc.querySelector('.head-actions'),find=doc.getElementById('findInChat');
    const flow=doc.getElementById('flowOpen');if(flow&&doc.getElementById('collabOpen'))flow.style.display='none';
    const context=doc.getElementById('contextToggle');if(context&&!context.querySelector('.context-label')){context.classList.remove('icon');const label=doc.createElement('span');label.className='context-label';label.textContent='컨텍스트';context.append(label);}
    for(const id of ['collabOpen','cycleOpen']){const b=doc.getElementById(id);if(!b)continue;const target=innerWidth<=720?menu:actions;if(b.parentElement!==target){const focused=doc.activeElement===b;if(target===menu)menu.append(b);else actions.insertBefore(b,find);if(focused)b.focus();}}
  }
  if(doc.readyState==='loading')doc.addEventListener('DOMContentLoaded',arrangeActions);else arrangeActions();
  addEventListener('resize',arrangeActions);
  function render(a){
    const asks=a&&(state.approvals||[]).some(x=>x.agent===a.id);
    const pending=a&&(a.pendingReplies?.length||a.pendingReply);
    const label=!a?'':asks?'응답 필요':a.status==='running'?'작업 중':pending?'보고 대기':a.source?'외부 기록':statusNames[a.status]||'대기';
    if(status.textContent!==label)status.textContent=label;
    status.dataset.status=asks?'waiting':a?.status||'idle';
    status.hidden=!a;
  }
  render(agent(state.selected));
  return {render};
})();

/* Project work overview. All labels derive from public runtime state; a report is not a verification. */
(()=>{
  'use strict';
  if(!globalThis.document?.getElementById?.('workspaceTabs'))return;
  let visible=true,lastHTML='',project='';
  const live=s=>['running','waiting','stopping'].includes(s);
  const words={running:'작업 중',waiting:'입력 대기',stopping:'중단 중',queued:'대기 중',paused:'일시 정지',failed:'실행 실패',error:'실행 실패',interrupted:'중단됨',stopped:'중단됨',done:'완료',idle:'대기',observing:'외부 기록'};
  const short=(s,n=220)=>{const t=String(s||'').replace(/\s+/g,' ').trim();return t.length>n?t.slice(0,n)+'…':t;};
  const when=t=>{const d=new Date(Number(t)*1000);return t&&Number.isFinite(d.getTime())?d.toLocaleString('ko-KR',{month:'numeric',day:'numeric',hour:'2-digit',minute:'2-digit'}):'시각 없음';};
  const name=id=>agent(id)?.name||'보관된 담당자';
  const open=(id,label)=>agent(id)?`<button type="button" data-work-agent="${esc(id)}">${esc(label)} <span aria-hidden="true">↗</span></button>`:'<span class="wo-muted">대화를 사용할 수 없습니다</span>';
  async function record(id,snapshot=null){
    const d=snapshot||(state.delegations||[]).find(d=>d.id===id);
    if(!d||state.switchingProject||state.loadedProject!==state.projectId)return;
    const owner=state.projectId;
    modal('맡긴 작업 기록','<div id="workRecord" aria-live="polite"><p>요청과 응답을 불러오는 중…</p></div>','WORK RECORD');
    const root=document.getElementById('workRecord'),version=state.modalVersion;
    $('#modal').addEventListener('close',()=>setTimeout(()=>{
      if(state.projectId!==owner||state.switchingProject||!visible||$('#modal').open||document.activeElement!==document.body)return;
      const opener=[...document.querySelectorAll('[data-work-record]')].find(b=>b.dataset.workRecord===id);
      (opener||document.getElementById('workOverviewTitle'))?.focus({preventScroll:true});
    },0),{once:true});
    const current=()=>root.isConnected&&$('#modal').open&&state.modalVersion===version&&state.projectId===owner&&!state.switchingProject;
    const jump=(m,label)=>`<button type="button" data-record-jump="${esc(m.id)}" data-record-agent="${esc(d.member)}">${esc(label)}</button>`;
    const heading=`<p class="wo-kicker">${esc(name(d.lead))} → ${esc(name(d.member))} · ${esc(when(d.finishedAt||d.ts))}</p><h3>${esc(d.task||'팀에 맡긴 작업')}</h3><section><h4>받은 요약</h4><p>${esc(d.summary||'저장된 요약이 없습니다.')}</p></section><p class="wo-record-note">${d.status==='done'?'보고 수신':esc(words[d.status]||d.status)} · 독립 검증 및 운영 적용 여부는 이 기록만으로 확인되지 않습니다.</p>`;
    try{
      const detail=await api('detail?agent='+encodeURIComponent(d.member),undefined,owner);
      if(!current())return;
      const messages=detail.messages||[],matches=messages.filter(m=>m.role==='user'&&m.origin?.delegation===id);
      if(matches.length!==1){
        root.innerHTML=heading+'<p role="status">이 작업과 정확히 연결되는 요청 기록을 찾지 못했습니다. 다른 작업의 응답을 대신 표시하지 않습니다.</p>';
      }else{
        const request=matches[0],start=messages.indexOf(request),next=messages.findIndex((m,i)=>i>start&&m.role==='user');
        const replies=messages.slice(start+1,next<0?undefined:next).filter(m=>m.role==='assistant'&&m.text);
        root.innerHTML=heading+`<details class="wo-record-request"><summary>맡긴 요청 원문</summary><div class="message-body">${markdown(request.text||'')}</div>${jump(request,'요청 위치로 이동')}</details><section><h4>이 요청 이후 응답</h4><p class="wo-muted">다음 요청이나 인계가 도착하기 전까지의 응답입니다. 보고 요약과 다를 수 있으며, 도구 실행 기록은 대화에서 확인할 수 있습니다.</p>${replies.length?replies.map(m=>`<article class="wo-record-reply"><div class="message-body">${markdown(m.text)}</div>${jump(m,'응답 위치로 이동')}</article>`).join(''):'<p role="status">이 구간에 저장된 응답이 없습니다. 후속 인계 이후의 결과는 담당 대화에서 확인하세요.</p>'}</section>`;
      }
      if(agent(d.member))root.insertAdjacentHTML('beforeend',`<button type="button" data-record-jump="" data-record-agent="${esc(d.member)}">담당 대화 전체 열기</button>`);
    }catch(error){if(current())root.innerHTML=heading+`<p role="alert">기록을 읽지 못했습니다. ${esc(error.message||'연결을 확인하세요.')}</p><button type="button" data-record-retry>다시 시도</button>`;}
    root.addEventListener('click',e=>{
      if(!current())return;
      if(e.target.closest('[data-record-retry]')){record(id,d);return;}
      const b=e.target.closest('[data-record-jump]');if(!b||!agent(b.dataset.recordAgent))return;
      closeModal();
      if(!b.dataset.recordJump){select(b.dataset.recordAgent);$('#selectedName').setAttribute('tabindex','-1');$('#selectedName').focus();return;}
      // Reuse the existing message jump, including its focus and disclosure handling.
      const link=document.createElement('button');link.dataset.openAgent=b.dataset.recordAgent;link.dataset.openMessage=b.dataset.recordJump;link.hidden=true;document.body.append(link);link.click();link.remove();
    });
  }
  function connection(ok){
    const el=document.getElementById('workOverviewConnection');if(!el)return;
    el.hidden=!!ok;el.textContent=ok?'':'연결을 다시 확인하고 있습니다. 아래는 마지막으로 받은 상태이며 현재 작업 상태와 다를 수 있습니다.';
  }
  function show(on,focus=false){
    visible=on;document.body.classList.toggle('work-overview-open',on);
    document.getElementById('workOverview').hidden=!on;
    document.getElementById('workOverviewOpen').setAttribute('aria-pressed',String(on));
    const skip=document.querySelector('.skip-chat');if(skip){skip.href=on?'#workOverviewTitle':'#prompt';skip.textContent=on?'작업 현황으로 이동':'입력창으로 이동';}
    if(on){render();document.body.classList.remove('navigation-open');if(focus)document.getElementById('workOverviewTitle').focus();}
  }
  function reset(){project='';lastHTML='';if(document.getElementById('workRecord')&&$('#modal').open)closeModal();document.getElementById('workOverviewBody').innerHTML='<p class="wo-empty">프로젝트의 작업을 불러오는 중…</p>';}
  function render(){
    const root=document.getElementById('workOverviewBody');if(!root)return;
    if(state.switchingProject||state.loadedProject!==state.projectId){reset();return;}
    const agents=(state.agents||[]).filter(a=>!a.source&&!a.archived),teams=state.teams||[],ds=state.delegations||[],approvals=state.approvals||[];
    const cycles=(state.cycles||[]).filter(c=>['running','paused','queued','failed'].includes(c.status));
    const active=agents.filter(a=>live(a.status)||a.delegating||ds.some(d=>d.member===a.id&&['queued','running'].includes(d.status)));
    const needs=agents.filter(a=>approvals.some(r=>r.agent===a.id)||['error','interrupted'].includes(a.status)||(!live(a.status)&&ds.some(d=>d.member===a.id&&d.status==='running')));
    const teamName=id=>teams.filter(t=>(t.members||[]).includes(id)).map(t=>t.name).join(' · ')||'팀 미지정';
    const card=a=>{
      const assigned=[...ds].reverse().find(d=>d.member===a.id&&['queued','running'].includes(d.status));
      const ask=approvals.find(r=>r.agent===a.id),task=assigned?.task||a.lastTask;
      const activity=a.status==='running'&&a.currentActivity?.label;
      return `<article class="wo-card"><div class="wo-card-top"><span class="wo-kicker">${esc(teamName(a.id))}</span><span class="wo-status">${ask?'응답 필요':live(a.status)?esc(words[a.status]):a.delegating?'팀원 보고 대기':assigned?.status==='queued'?'배정 대기':assigned?.status==='running'?'실행 상태 확인':esc(words[a.status]||a.status)}</span></div><h3>${esc(a.name)}</h3><p class="wo-meta">${esc(providerName(a.provider))}${a.role?' · '+esc(a.role):''}</p><p class="wo-task"><span>${assigned?'맡은 요청':'최근 요청'}</span>${esc(short(task)||'아직 작업 요청이 없습니다.')}</p>${assigned?`<p class="wo-meta">${esc(name(assigned.lead))} → ${esc(a.name)} · ${esc(assigned.status==='running'&&!live(a.status)?'배정 기록 확인 필요':words[assigned.status])}</p>`:''}${activity?`<p class="wo-activity">지금 · ${esc(short(activity,140))}</p>`:'<p class="wo-muted">세부 활동은 대화에서 확인할 수 있습니다.</p>'}<p class="wo-meta">마지막 출력 ${esc(when(a.lastEventAt))}</p>${open(a.id,ask?(ask.kind==='question'?'질문에 답하기':'승인 확인'):'작업 대화 열기')}</article>`;
    };
    const results=[...ds].filter(d=>['done','failed','stopped'].includes(d.status)).sort((a,b)=>(b.finishedAt||b.ts||0)-(a.finishedAt||a.ts||0)).slice(0,6);
    const html=`<p class="wo-scope">${esc(state.projects?.find(p=>p.id===state.projectId)?.name||'현재 프로젝트')} · 프로젝트 전체 작업</p>
      <div class="wo-metrics"><span><b>${active.filter(a=>live(a.status)||a.delegating||ds.some(d=>d.member===a.id&&d.status==='queued')).length}</b> 진행·보고 대기</span><span><b>${needs.length}</b> 응답·상태 확인</span><span><b>${cycles.length}</b> 진행·미완료 목표</span></div>
      ${needs.length?`<section class="wo-attention" aria-label="확인이 필요한 작업"><h2>먼저 확인해 주세요</h2><div class="wo-attention-list">${needs.map(a=>open(a.id,`${a.name} · ${approvals.some(r=>r.agent===a.id)?'응답 필요':(['error','interrupted'].includes(a.status)?words[a.status]:'실행 상태 확인')}`)).join('')}</div></section>`:''}
      <section aria-labelledby="woGoals"><div class="wo-section-head"><h2 id="woGoals">진행·미완료 목표</h2><div><button type="button" data-goal-history>목표 기록</button> <button type="button" data-work-start>목표 시작</button></div></div>${cycles.length?`<div class="wo-goals">${cycles.map(c=>{const stage=(c.stages||[]).find(s=>s.status==='running')||(c.stages||[]).find(s=>s.status==='queued');return `<article class="wo-goal"><span class="wo-kicker">${c.mode==='goal'?'개인 목표':'팀 목표'} · ${esc(c.status==='running'&&stage?.status!=='running'?'다음 단계 대기':words[c.status]||c.status)}</span><h3 title="${esc(c.goal)}">${esc(short(c.goal,140))}</h3>${stage?`<p>${stage.status==='queued'?'다음 · ':''}${esc(({research:'리서치',build:'구현',qa:'검토',review:'목표 대조'})[stage.kind]||stage.kind)} · ${esc(name(stage.agent))}</p>`:'<p class="wo-muted">현재 실행 중인 단계가 없습니다.</p>'}<button type="button" data-work-cycle="${esc(c.id)}">목표와 단계 확인 ↗</button></article>`;}).join('')}</div>`:'<p class="wo-empty">등록된 진행 목표가 없습니다. 목표를 시작하면 담당과 단계가 여기에 표시됩니다.</p>'}</section>
      <section aria-labelledby="woActive"><div class="wo-section-head"><h2 id="woActive">누가 무엇을 하고 있나요</h2><span class="wo-muted">실행 상태와 공개된 활동 기준</span></div>${active.length?`<div class="wo-grid">${active.map(card).join('')}</div>`:'<p class="wo-empty">현재 실행 중이거나 보고를 기다리는 작업이 없습니다.</p>'}</section>
      <section aria-labelledby="woResults"><div class="wo-section-head"><h2 id="woResults">최근 팀 보고</h2></div><p class="wo-muted">최근 수신 기록 최대 6건입니다. 보고 수신은 독립 검증이나 운영 적용 완료를 뜻하지 않습니다.</p>${results.length?`<div class="wo-results">${results.map(d=>`<article class="wo-result"><div><span class="wo-kicker">${esc(name(d.member))} → ${esc(name(d.lead))} · ${esc(when(d.finishedAt||d.ts))}</span><h3>${esc(short(d.task,140))||'팀에 맡긴 작업'}</h3><p>${esc(short(d.summary,280))||'요약이 없습니다. 담당 대화에서 결과를 확인하세요.'}</p><span class="wo-status">${d.status==='done'?'보고 수신':esc(words[d.status])}</span></div><button type="button" data-work-record="${esc(d.id)}">요청·응답 확인 ↗</button></article>`).join('')}</div>`:'<p class="wo-empty">아직 수신한 팀 보고가 없습니다.</p>'}</section>
      <details class="wo-teams"><summary>팀 구성 · ${teams.length}개 팀</summary><div class="wo-grid">${teams.map(t=>{const members=agents.filter(a=>(t.members||[]).includes(a.id));return `<article class="wo-card"><h3>${esc(t.name)}</h3><p class="wo-meta">${t.lead?'팀장 '+esc(name(t.lead)):'팀장 미지정'} · ${members.length}명</p>${members.map(a=>open(a.id,`${a.name} · ${words[a.status]||a.status}`)).join('')||'<p>등록된 담당자가 없습니다.</p>'}</article>`;}).join('')}</div></details>`;
    if(lastHTML===html&&project===state.projectId)return;
    const focus=root.contains(document.activeElement)?document.activeElement:null;
    const focusKey=focus&&['workAgent','workCycle','workStart','workRecord'].find(k=>k in focus.dataset);
    const focusValue=focusKey&&focus.dataset[focusKey],expanded=root.querySelector('.wo-teams')?.open;
    root.innerHTML=html;if(expanded)root.querySelector('.wo-teams').open=true;
    if(focusKey){const replacement=[...root.querySelectorAll('button')].find(b=>b.dataset[focusKey]===focusValue);(replacement||document.getElementById('workOverviewTitle')).focus({preventScroll:true});}
    lastHTML=html;project=state.projectId;
  }
  function init(){
    const main=document.createElement('section');main.id='workOverview';main.className='work-overview';main.setAttribute('aria-labelledby','workOverviewTitle');
    main.innerHTML='<header class="wo-header"><div><p class="wo-kicker">WORKSPACE</p><h1 id="workOverviewTitle" tabindex="-1">작업 현황</h1><p>현재 작업과 다음에 확인할 결과를 한곳에서.</p></div><button type="button" id="workReturn">대화로 돌아가기</button></header><div id="workOverviewBody"><p class="wo-empty">작업을 불러오는 중…</p></div>';
    document.querySelector('.conversation').before(main);
    const notice=document.createElement('p');notice.id='workOverviewConnection';notice.className='wo-attention';notice.setAttribute('role','status');notice.hidden=true;document.getElementById('workOverviewBody').before(notice);
    const nav=document.createElement('button');nav.id='workOverviewOpen';nav.type='button';nav.textContent='작업 현황';nav.setAttribute('aria-controls','workOverview');nav.onclick=()=>show(true,true);
    document.querySelector('.appbar .brand').after(nav);
    document.getElementById('workReturn').onclick=()=>{show(false);document.getElementById('selectedName').setAttribute('tabindex','-1');document.getElementById('selectedName').focus();};
    main.addEventListener('click',e=>{const a=e.target.closest('[data-work-agent]'),c=e.target.closest('[data-work-cycle]');if(e.target.closest('[data-work-record]')){record(e.target.closest('[data-work-record]').dataset.workRecord);return;}if(a){if(state.switchingProject||state.loadedProject!==state.projectId||!agent(a.dataset.workAgent))return;select(a.dataset.workAgent);document.getElementById('selectedName').setAttribute('tabindex','-1');document.getElementById('selectedName').focus();}else if(c)globalThis.PitCrewCycle?.board(c.dataset.workCycle);else if(e.target.closest('[data-work-start]'))document.getElementById('cycleOpen')?.click();});
    // Existing links may open the already-selected conversation without calling select().
    document.addEventListener('click',e=>{if(e.target.closest('[data-agent],[data-open-agent],[data-team-agent]'))show(false);},true);
    show(true);
  }
  globalThis.PitCrewWorkOverview={show,render,reset,connection};
  if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',init);else init();
})();
