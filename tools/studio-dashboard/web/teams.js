'use strict';
state.selectedTeam=localStorage.getItem('pitcrew.team.v2')??'__unassigned';
function teamVisibleIds(){return state.selectedTeam==='__unassigned'?state.agents.filter(a=>!(state.teams||[]).some(t=>t.members.includes(a.id))).map(a=>a.id):state.selectedTeam?(state.teams||[]).find(t=>t.id===state.selectedTeam)?.members:null;}
function renderTeamFilter(){
  const teams=state.teams||[];if(state.selectedTeam&&state.selectedTeam!=='__unassigned'&&!teams.some(t=>t.id===state.selectedTeam))state.selectedTeam='__unassigned';
  const unassigned=state.agents.filter(a=>!teams.some(t=>t.members.includes(a.id)));
  if(state.selectedTeam==='__unassigned'&&state.agents.length&&!unassigned.length){state.selectedTeam='';state.teamFallback=true;}
  const fallback=state.teamFallback&&state.selectedTeam===''&&state.agents.length&&!unassigned.length;
  const html='<option value="__unassigned">팀 미지정</option><option value="">전체 대화</option>'+teams.map(t=>`<option value="${esc(t.id)}">${esc(t.name)} · ${t.members.length}</option>`).join('');
  if($('#teamFilter').innerHTML!==html)$('#teamFilter').innerHTML=html;$('#teamFilter').value=state.selectedTeam;
  const note=$('#teamFilterNote'),t=teams.find(t=>t.id===state.selectedTeam);if(note){note.classList.toggle('hidden',!t&&state.selectedTeam!=='__unassigned'&&!fallback);const count=state.agents.filter(a=>!t?.members.includes(a.id)).length;note.innerHTML=t?`이 팀의 대화만 표시 중 · 다른 대화 ${count}개<br><button id="showAllChats">전체 대화 보기 →</button>`:fallback?'모든 대화가 팀에 배치되어 전체 대화를 보여드립니다. 팀 선택에서 원하는 팀을 열 수 있습니다.':state.selectedTeam==='__unassigned'?'팀에 배치한 대화는 각 팀 또는 전체 대화에서 볼 수 있습니다.':'';if(t)$('#showAllChats').onclick=()=>{$('#teamFilter').value='';$('#teamFilter').dispatchEvent(new Event('change'));};}
}
function teamEditor(id){
  const t=(state.teams||[]).find(t=>t.id===id);
  modal(t?'팀 설정':'새 팀 만들기',`<form id="teamForm"><div class="field"><label for="teamName">팀 이름</label><input id="teamName" required maxlength="100" placeholder="예: 아트 팀, 개발 팀" value="${esc(t?.name||'')}"></div><div class="field"><label for="teamGoal">팀 공통 목표 · 자료</label><textarea id="teamGoal" maxlength="10000" placeholder="이 팀의 에이전트가 함께 참고할 목표와 제약사항">${esc(t?.context||'')}</textarea><small>팀원에게 다음 메시지를 보낼 때 함께 전달됩니다.</small></div><div class="field"><label for="teamLead">팀장 <small>알림은 팀장에게서만 — 팀원은 승인 요청·실패 때만 알립니다</small></label><select id="teamLead"><option value="">없음 — 모든 대화가 각자 알림</option>${state.agents.filter(a=>!a.source).map(a=>`<option value="${esc(a.id)}" ${t?.lead===a.id?'selected':''}>${esc(a.name)} · ${esc(providerName(a.provider))}</option>`).join('')}</select><small>Claude 팀장은 팀원에게 직접 일을 맡기고, 팀원의 보고를 자동으로 돌려받습니다.</small></div><h3>기존 대화와 에이전트 연결</h3><p class="form-note">한 대화를 여러 팀에 넣을 수 있습니다. 팀에 넣는 것만으로 다른 팀원의 대화가 자동 전달되지는 않습니다.</p><div class="member-list">${state.agents.map(a=>`<label class="member-option"><input type="checkbox" name="teamMember" value="${esc(a.id)}" ${t?.members.includes(a.id)?'checked':''}><span><b>${esc(a.name)}</b><small>${esc(providerName(a.provider))} · ${a.source?'가져온 기록':esc(a.role)}</small></span></label>`).join('')}</div><div id="formError" class="form-error"></div><div class="buttons"><button type="button" id="cancelTeam">취소</button><button class="primary" type="submit">${t?'팀 저장':'팀 만들기'}</button></div></form>`,'TEAMS');
  $('#cancelTeam').onclick=closeModal;$('#teamForm').onsubmit=async e=>{e.preventDefault();const button=e.submitter;button.disabled=true;try{const lead=$('#teamLead').value,members=[...document.querySelectorAll('[name=teamMember]:checked')].map(x=>x.value);if(lead&&!members.includes(lead))members.push(lead);const team=await api('teams',{id:t?.id,rev:t?.rev??0,name:$('#teamName').value,context:$('#teamGoal').value,lead,members});state.selectedTeam=team.id;localStorage.setItem('pitcrew.team.v2',team.id);closeModal();await poll();renderAgents();toast('팀을 저장했습니다. 대화 가져오기로 기존 기록도 추가할 수 있습니다.');}catch(error){formError(error);button.disabled=false;}};
}
function teamManager(){
  modal('내 팀 관리',`<div class="team-manager-actions"><button id="createTeam" class="primary">＋ 새 팀 만들기</button><button id="teamImport">기존 대화 가져오기</button></div><div class="team-manager-list">${(state.teams||[]).map(t=>`<section class="team-node"><h3>${esc(t.name)} <span class="tiny-tag">${t.members.length}명 연결</span></h3><p>${esc(t.context||'팀 공통 목표를 추가해 주세요.')}</p><div class="buttons"><button data-edit-team="${esc(t.id)}">팀원 · 목표 편집</button><button data-connect-team="${esc(t.id)}">대화 공유 연결</button></div></section>`).join('')||'<div class="welcome"><h2>함께 일할 팀을 만드세요</h2><p>아트·개발처럼 팀을 나누고 Codex와 Claude 대화를 같은 팀에 모으세요.</p></div>'}</div>`,'TEAMS');
  $('#createTeam').onclick=()=>teamEditor();$('#teamImport').onclick=()=>importConversations();
}
function connectTeam(id){
  const t=state.teams.find(t=>t.id===id);if(!t)return;
  const project=state.projectId,members=state.agents.filter(a=>t.members.includes(a.id)),receivers=members.filter(a=>!a.source);
  const drafts=Object.fromEntries(receivers.map(a=>[a.id,(a.peers||[]).filter(p=>t.members.includes(p))]));
  const initial=JSON.stringify(drafts);
  modal(`${t.name} · 대화 공유 연결`,`<form id="teamConnections"><p class="form-note">참고할 대화에서 받는 사람으로 맥락이 전달됩니다. 지시가 자동 전송되지는 않으며, 다음 메시지부터 적용됩니다.</p>${receivers.length?`<div class="link-toolbar"><button type="button" id="linkEveryone">팀원끼리 모두 연결</button><span id="linkChanges" role="status">현재 연결</span></div><div class="field"><label for="teamReceiver">맥락을 받을 사람</label><select id="teamReceiver">${receivers.map(a=>`<option value="${esc(a.id)}">${esc(a.name)} · ${esc(providerName(a.provider))}</option>`).join('')}</select></div><div id="linkDirection" class="link-direction"></div><div id="teamPeerChoices" class="member-list"></div><details class="link-summary"><summary>팀 전체 연결 보기</summary><div id="linkSummary"></div></details><div id="formError" class="form-error" role="alert"></div><div class="buttons"><button type="button" id="cancelLinks">닫기</button><button class="primary" id="saveLinks" disabled>변경 사항 저장</button></div>`:`<div class="empty-links"><h3>${members.length?'먼저 원래 대화를 이어서 연결하세요':'아직 연결할 팀원이 없습니다'}</h3><p>${members.length?'대화를 연 뒤 ‘원래 세션 이어서 사용’을 누르면 맥락을 받을 수 있습니다.':'팀 설정에서 대화를 추가하거나 기존 대화를 가져오세요.'}</p>${members.map(a=>`<button type="button" data-open-chat="${esc(a.id)}">${esc(a.name)} 열기 ↗</button>`).join('')}<button type="button" data-edit-team="${esc(t.id)}">팀원 편집</button></div>`}</form>`,'CONTEXT LINKS');
  if(!receivers.length)return;
  const form=$('#teamConnections'),version=state.modalVersion;
  const update=()=>{
    const changed=JSON.stringify(drafts)!==initial;
    $('#saveLinks').disabled=!changed;$('#linkChanges').textContent=changed?'저장하지 않은 변경 사항':'현재 연결';
    $('#linkSummary').innerHTML=receivers.map(a=>`<p><b>${esc(a.name)}</b> ← ${drafts[a.id].map(id=>esc(agent(id)?.name||id)).join(', ')||'연결 없음'}</p>`).join('');
  };
  const draw=()=>{
    const a=agent($('#teamReceiver').value),outside=(a.peers||[]).filter(id=>!t.members.includes(id)).length;
    $('#linkDirection').innerHTML=`선택한 대화 <span aria-hidden="true">→</span> <strong>${esc(a.name)}</strong>`;
    $('#teamPeerChoices').innerHTML=members.filter(m=>m.id!==a.id).map(m=>`<label class="member-option"><input name="teamPeer" type="checkbox" value="${esc(m.id)}" ${drafts[a.id].includes(m.id)?'checked':''}><span>${esc(m.name)}<small>${esc(providerName(m.provider))} · ${m.source?'가져온 기록':'팀원 대화'}</small></span></label>`).join('')+`<p class="form-note">최대 8개 · 팀 밖의 기존 연결 ${outside}개는 유지됩니다. 받는 사람을 바꿔도 선택은 유지됩니다.</p>`;
    update();
  };
  $('#teamReceiver').onchange=draw;
  $('#teamPeerChoices').onchange=()=>{drafts[$('#teamReceiver').value]=[...form.querySelectorAll('[name=teamPeer]:checked')].map(x=>x.value);update();};
  $('#linkEveryone').onclick=()=>{
    if(receivers.some(a=>(a.peers||[]).filter(p=>!t.members.includes(p)).length+members.length-1>8)){formError(new Error('팀 밖 연결을 포함해 최대 8개입니다. 받는 사람별로 필요한 대화를 선택하세요.'));return;}
    for(const a of receivers)drafts[a.id]=members.filter(m=>m.id!==a.id).map(m=>m.id);draw();
  };
  $('#cancelLinks').onclick=closeModal;draw();
  form.onsubmit=async e=>{
    e.preventDefault();const button=$('#saveLinks');button.disabled=true;
    const original=JSON.parse(initial),links=Object.fromEntries(Object.entries(drafts).filter(([aid,peers])=>JSON.stringify(peers)!==JSON.stringify(original[aid])));
    form.querySelectorAll('input,select,button').forEach(el=>el.disabled=true);
    try{
      await api('team-links',{team:t.id,members:t.members,links},project);
      if(project!==state.projectId)return;
      await poll();if(state.modalVersion===version)closeModal();toast('연결을 저장했습니다. 각 팀원의 다음 메시지부터 반영됩니다.');
    }catch(error){if($('#teamConnections')===form){form.querySelectorAll('input,select,button').forEach(el=>el.disabled=false);formError(error);update();}}
  };
}
async function importConversations(){
  const teams=state.teams||[];
  modal('기존 대화 가져오기',`<p class="form-note"><b>${esc(document.getElementById('projectName')?.textContent||'이 프로젝트')}</b> 프로젝트 폴더의 로컬 Codex·Claude 기록만 보입니다 (다른 프로젝트 기록은 왼쪽 위에서 프로젝트를 바꾼 뒤). 먼저 기록을 가져온 뒤, 대화의 ‘원래 세션 이어서 사용’으로 입력을 활성화할 수 있습니다.</p><div class="form-grid"><div class="field"><label for="importTeam">가져올 팀</label><select id="importTeam"><option value="">팀 없이 연결</option>${teams.map(t=>`<option value="${esc(t.id)}" ${t.id===state.selectedTeam?'selected':''}>${esc(t.name)}</option>`).join('')}</select></div><div class="field"><label for="importProvider">AI 종류</label><select id="importProvider"><option value="">전체</option><option value="codex">Codex</option><option value="claude">Claude</option></select></div></div><input id="importSearch" class="import-search" aria-label="가져올 대화 검색" placeholder="아트, 작업 내용, 세션 이름으로 검색"><label class="checkbox-row"><input id="includeChildren" type="checkbox">하위 에이전트 기록도 표시</label><div id="importList" class="import-list">기록을 찾고 있습니다…</div><div id="formError" class="form-error"></div><div class="import-footer"><span id="importCount">0개 선택</span><button id="cancelImport">취소</button><button id="confirmImport" class="primary" disabled>선택한 대화 가져오기</button></div>`,'IMPORT CONVERSATIONS');
  const list=$('#importList');const selected=new Set();$('#cancelImport').onclick=closeModal;
  try{const rows=await api('sources');if($('#importList')!==list)return;
    const draw=()=>{const q=$('#importSearch').value.toLowerCase();const provider=$('#importProvider').value;list.innerHTML=rows.filter(s=>(!provider||s.provider===provider)&&($('#includeChildren').checked||!s.parent)&&`${s.preview||''} ${s.name} ${s.label||''}`.toLowerCase().includes(q)).map(s=>`<label class="import-row"><input type="checkbox" ${s.connectedAgent?'disabled':''} data-import-source="${esc(s.id)}" ${selected.has(s.id)?'checked':''}><span><b>${esc(s.label||s.preview||s.name)}</b><small>${esc(providerName(s.provider))} · ${dateStamp(s.updated)}${s.parent?' · 하위 에이전트':''}${s.connectedAgent?' · 이미 연결됨':''}</small><code>${esc(s.name)}</code>${s.connectedAgent?`<button type="button" data-open-import="${esc(s.connectedAgent.id)}" data-import-archived="${s.connectedAgent.archived?'yes':''}">${s.connectedAgent.archived?'복원하고 대화 열기':'대화 열기'} ↗</button>`:''}</span></label>`).join('')||'<p class="form-note">일치하는 대화가 없습니다. 현재 선택한 프로젝트의 로컬 기록만 표시합니다.</p>';};
    $('#importSearch').oninput=draw;$('#importProvider').onchange=draw;$('#includeChildren').onchange=draw;draw();
    list.onclick=async e=>{const b=e.target.closest('[data-open-import]');if(!b)return;e.preventDefault();try{if(b.dataset.importArchived)await api('restore',{agent:b.dataset.openImport});await poll();select(b.dataset.openImport);closeModal();}catch(error){formError(error);}};
    list.onchange=e=>{const id=e.target.dataset.importSource;if(!id)return;e.target.checked?selected.add(id):selected.delete(id);$('#importCount').textContent=selected.size+'개 선택';$('#confirmImport').disabled=!selected.size;};
    $('#confirmImport').onclick=async()=>{const button=$('#confirmImport');button.disabled=true;try{const tid=$('#importTeam').value;const result=await api('import',{sources:[...selected],team:tid,names:Object.fromEntries(rows.filter(s=>selected.has(s.id)).map(s=>[s.id,(providerName(s.provider)+' · '+(s.label||s.preview||s.name)).slice(0,100)]))});state.selectedTeam=tid;localStorage.setItem('pitcrew.team.v2',tid);await poll();for(const a of result.agents){state.agents=state.agents.filter(old=>old.id!==a.id).concat(a);}if(result.team)state.teams=state.teams.filter(t=>t.id!==result.team.id).concat(result.team);if(result.agents[0])select(result.agents[0].id);closeModal();toast(`${result.agents.length}개 대화를 연결했습니다. 팀 관리의 ‘대화 공유 연결’로 에이전트에게 전달할 수 있습니다.`);}catch(error){formError(error);button.disabled=false;}};
  }catch(error){if($('#importList')===list)list.textContent='기록을 불러오지 못했습니다. '+error.message;}
}
$('#manageTeams').onclick=teamManager;$('#importChats').onclick=importConversations;$('#sourcesOpen').onclick=importConversations;
$('#teamFilter').onchange=e=>{state.teamFallback=false;state.selectedTeam=e.target.value;localStorage.setItem('pitcrew.team.v2',state.selectedTeam);renderTeamFilter();const ids=teamVisibleIds();select(state.agents.find(a=>!ids||ids.includes(a.id))?.id);};
document.addEventListener('click',e=>{const b=e.target.closest('button');if(b?.dataset.editTeam)teamEditor(b.dataset.editTeam);if(b?.dataset.connectTeam)connectTeam(b.dataset.connectTeam);});

// One line under the team picker: who leads this team (alerts come only from the lead) or how to set one.
{const base=renderTeamFilter;renderTeamFilter=function(...args){base.apply(this,args);
  let line=document.getElementById('teamLeadLine');const t=(state.teams||[]).find(t=>t.id===state.selectedTeam);
  if(!line){const anchor=document.getElementById('teamFilterNote');if(!anchor)return;line=document.createElement('p');line.id='teamLeadLine';line.className='team-lead-line';anchor.after(line);}
  const html=!t?'':t.lead&&agent(t.lead)?`<span class="lead-tag">팀장</span> ${esc(agent(t.lead).name)} · 알림은 팀장에게서만`:'팀장 없음 · 대화의 ⋯ 에서 <b>팀장으로</b> 지정';
  line.hidden=!html;if(line.innerHTML!==html)line.innerHTML=html;};}
