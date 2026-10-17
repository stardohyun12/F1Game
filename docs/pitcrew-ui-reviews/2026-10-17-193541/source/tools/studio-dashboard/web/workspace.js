'use strict';
document.documentElement.dataset.theme=localStorage.getItem('pitcrew.theme')||'light';
function renderProjectNavigation(){
  const el=$('#projectSelect');const html=(state.projects||[]).map(p=>`<option value="${esc(p.id)}">${esc(p.name)}${p.running?' · '+p.running+'명 실행 중':''}${p.available?'':' · 폴더 없음'}</option>`).join('');
  if(el.innerHTML!==html)el.innerHTML=html;el.value=state.projectId;
  const current=state.projects?.find(p=>p.id===state.projectId);el.title=current?.path||'';
  if(current){$('#projectName').textContent=current.name;$('#projectName').title=current.path;}
}
async function switchProject(id){
  if(id===state.projectId||state.switchingProject)return;
  state.switchingProject=true;$('#projectSelect').disabled=true;globalThis.PitCrewWorkOverview?.reset();
  try{
  if(syncLoop.pending)await syncLoop.pending;
  globalThis.PitCrewWorkOverview?.reset();
  if(state.selected){state.drafts[state.selected]=$('#prompt').value;saveDrafts();}
  state.projectId=id;state.workspaceClosed=typeof workspaceLayout==='function'?!!workspaceLayout().closed:false;localStorage.setItem('pitcrew.project',id);state.selected=null;state.chatQuery='';$('#chatSearch').value='';$('#clearChatSearch').classList.add('hidden');state.selectedTeam='__unassigned';state.teamFallback=false;state.compare=null;state.detail=null;state.agents=[];state.teams=[];state.contextHash='';state.detailHash='';
  $('#prompt').value='';globalThis.PitCrewAttach?.render();globalThis.PitCrewComposer?.render();globalThis.PitCrewComposer?.close();$('#comparePanel').classList.add('hidden');$('#approvals').innerHTML='';globalThis.PitCrewArtifacts?.close();globalThis.PitCrewArtifacts?.reset();globalThis.PitCrewUnity?.reset();globalThis.PitCrewWorkspaceChanges?.reset();closeModal();renderAgents();renderHead();await poll();
  }finally{state.switchingProject=false;$('#projectSelect').disabled=false;globalThis.PitCrewWorkOverview?.render();}
}
$('#projectSelect').onchange=e=>switchProject(e.target.value).catch(toast);
async function projectManager(){
  modal('프로젝트',`<p class="form-note">프로젝트마다 대화·팀·공유 자료를 따로 보관합니다. 전환해도 진행 중인 작업은 계속됩니다.</p><div id="projectList" class="project-list">목록을 불러오는 중…</div><form id="projectForm"><h3>프로젝트 추가</h3><div class="field"><label for="projectPath">로컬 폴더 경로</label><input id="projectPath" required placeholder="D:\\Projects\\MyApp" autocomplete="off"></div><div class="field"><label for="projectLabel">표시 이름 · 선택</label><input id="projectLabel" maxlength="100" placeholder="폴더 이름을 자동으로 사용합니다"></div><div id="formError" class="form-error"></div><div class="buttons"><button class="primary" type="submit">추가하고 열기</button></div></form>`,'YOUR WORKSPACES');
  $('#projectForm').onsubmit=async e=>{e.preventDefault();e.submitter.disabled=true;try{const p=await api('projects',{path:$('#projectPath').value,name:$('#projectLabel').value},'default');await switchProject(p.id);}catch(error){formError(error);e.submitter.disabled=false;}};
  try{const rows=await api('projects',undefined,'default');if(!$('#projectList'))return;$('#projectList').innerHTML=rows.map(p=>`<button class="project-row" data-open-project="${esc(p.id)}"><span class="project-icon">▣</span><span><b>${esc(p.name)}</b><small>${esc(p.path)}</small></span><span class="tiny-tag">${p.id===state.projectId?'선택됨':p.running?p.running+'명 실행 중':p.available?'열기':'폴더 없음'}</span></button>`).join('');}catch(error){formError(error);}
}
$('#projectsOpen').onclick=projectManager;

function onboarding(){
  const project=state.projects?.find(p=>p.id===state.projectId);
  const imported=state.agents.filter(a=>a.source||a.originSource).length;
  modal('내 작업 공간 준비하기',`<p class="setup-intro">프로젝트를 고르고, 대화를 연결한 뒤 팀으로 함께 작업하세요. 현재 상태에 맞는 단계부터 시작하면 됩니다.</p><div class="setup-providers">${state.providers.map(p=>`<span class="setup-provider ${p.ready?'ready':''}"><i></i>${esc(p.name)} · ${p.ready?'연결 도구 준비됨':'설치·설정 필요'}</span>`).join('')}</div><p class="form-note">도구 준비 상태입니다. 계정 로그인과 모델 사용 권한은 모델 조회 또는 실제 실행 시 확인됩니다.</p><ol class="setup-steps"><li><span class="setup-number">1</span><div><h3>작업할 프로젝트</h3><p>${esc(project?.name||'프로젝트 선택')}<small>${esc(project?.path||'로컬 폴더를 선택하세요.')}</small></p></div><button data-setup-action="project">프로젝트 선택</button></li><li><span class="setup-number">2</span><div><h3>기존 대화 또는 새 역할</h3><p>${imported?`기존 대화 ${imported}개 연결됨`:'Codex·Claude에서 하던 대화를 가져오거나 새 담당자를 만드세요.'}<small>가져온 기록에서 ‘원래 세션 이어서 사용’을 선택하면 계속 입력할 수 있습니다.</small></p></div><div class="setup-actions"><button data-setup-action="import">대화 가져오기</button><button data-setup-action="agent">새 역할 만들기</button></div></li><li><span class="setup-number">3</span><div><h3>팀과 공유할 맥락</h3><p>${state.teams.length?`${state.teams.length}개 팀이 준비되어 있습니다.`:'팀원을 모으고 공통 목표를 정하세요.'}<small>‘대화 공유 연결’에서 누가 누구의 대화를 참고할지 정합니다. 다음 메시지부터 적용됩니다.</small></p></div><button data-setup-action="team">팀 구성하기</button></li></ol><div class="setup-footer"><span>대화 기록은 이 컴퓨터에 저장됩니다.</span><button data-setup-action="help" class="quiet">자세한 사용법 ↗</button></div>`,'GET STARTED');
}
document.addEventListener('click',e=>{
  const action=e.target.closest('[data-setup-action]')?.dataset.setupAction;
  if(action==='start')onboarding();else if(action==='project')projectManager();else if(action==='import')importConversations();else if(action==='agent')settings({});else if(action==='team')teamBoard();else if(action==='help')help();
});
$('#helpOpen').onclick=onboarding;
document.addEventListener('click',e=>{const b=e.target.closest('[data-open-project]');if(b){if(b.dataset.openProject===state.projectId)closeModal();else switchProject(b.dataset.openProject).catch(toast);}if(e.target.closest('[data-agent]'))document.body.classList.remove('navigation-open');});
$('#navShade').onclick=()=>document.body.classList.remove('navigation-open');
$('#navToggle').onclick=()=>document.body.classList.toggle('navigation-open');
document.addEventListener('keydown',e=>{if(e.key==='Escape')document.body.classList.remove('navigation-open');});
$('#themeToggle').onclick=()=>{const value=document.documentElement.dataset.theme==='light'?'dark':'light';document.documentElement.dataset.theme=value;localStorage.setItem('pitcrew.theme',value);};
let usageState=null,usagePending=false;
const durationLabel=minutes=>minutes==null?'한도':minutes%1440===0?minutes/1440+'일':minutes%60===0?minutes/60+'시간':minutes+'분';
function usageMarkup(){
  return `<p class="form-note">로그인한 계정 전체의 구독 한도입니다. 모든 프로젝트와 다른 앱의 사용량이 함께 반영됩니다. 과도한 조회를 막기 위해 5분 간격으로 갱신합니다.</p><div class="usage-grid">${(usageState?.accounts||[]).map(a=>`<section class="usage-card"><div class="usage-card-head"><span class="avatar ${a.provider}">${a.provider==='codex'?'⌘':'C'}</span><h3>${a.provider==='codex'?'Codex':'Claude'}</h3><span class="tiny-tag">${a.refreshing?'조회 중':a.stale?'이전 조회':a.status==='ok'?'연결됨':a.status==='cooldown'?'조회 대기':'확인 필요'}</span></div>${a.windows.map(w=>`<div class="quota-window"><div><span>${esc(w.label)} · ${durationLabel(w.minutes)}</span><b>${w.usedPercent==null?'확인 불가':w.usedPercent+'% 사용'}</b></div>${w.usedPercent==null?'':`<progress max="100" value="${w.usedPercent}" aria-label="${esc(w.label)} 사용률">${w.usedPercent}%</progress>`}<small>${w.resetsAt?'초기화 '+new Date(w.resetsAt*1000).toLocaleString('ko-KR'):'초기화 시각 미제공'}</small></div>`).join('')||'<p class="usage-empty">'+(a.refreshing?'사용량을 확인하고 있습니다.':'사용량 정보 없음')+'</p>'}${a.message?`<p class="form-note">${esc(a.message)}</p>`:''}<footer>${a.lastSuccessAt?'마지막 확인 '+dateStamp(a.lastSuccessAt):'아직 확인된 수치가 없습니다.'}${a.status==='cooldown'?'<br>다음 조회 '+dateStamp(a.retryAt):''}</footer></section>`).join('')}</div><p class="form-note">사용량을 조회할 때 모델을 실행하지 않습니다. 조회 실패는 한도 소진을 뜻하지 않습니다.</p>`;
}
async function refreshUsage(){
  if(usagePending||document.hidden)return;usagePending=true;
  try{usageState=await api('usage');const label=usageState.accounts.map(a=>{const w=a.windows[0];return(a.provider==='codex'?'Codex':'Claude')+' '+(w?.usedPercent==null?'—':w.usedPercent+'%');}).join(' · ');$('#usageOpen').textContent='한도 사용 '+label;$('#usageOpen').title='구독 한도 중 이번 기간에 쓴 비율 · 눌러서 자세히';if($('#usageContent'))$('#usageContent').innerHTML=usageMarkup();}
  catch{if($('#usageContent'))$('#usageContent').textContent='사용량을 불러오지 못했습니다. 연결을 확인하고 다시 열어주세요.';}
  finally{usagePending=false;}
}
$('#usageOpen').onclick=()=>{modal('사용량',`<div id="usageContent">${usageState?usageMarkup():'계정 사용량을 확인하는 중…'}</div>`,'ACCOUNT USAGE');refreshUsage();};
refreshUsage();setInterval(refreshUsage,5000);
