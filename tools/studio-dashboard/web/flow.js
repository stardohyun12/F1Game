// 흐름(/flow)·팀(/team) 페이지. 서버 API(bootstrap·flow·detail·studio·shared)만 읽어서 그린다. 새 저장 형식 없음.
// 흐름은 좌표 캔버스: 배경 드래그 = 이동, 휠 = 확대, 카드 드래그 = 위치 바꾸기(브라우저에 기억), 더블클릭 = 그 대화.
'use strict';
const $=s=>document.querySelector(s), esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const PAGE=document.body.dataset.page;
const KIND={handoff:'결과를 넘김',delegate:'일을 맡김',cycle:'다음 단계로 넘김',research:'리서치를 맡김',forward:'메시지를 전달',mention:'참조',message:'메시지',queue:'대기열에 넣음',peer:'대화 연결',rule:'끝나면 넘기기'};
const STATUS={idle:'대기',running:'작업 중',waiting:'확인 필요',stopping:'중단 중',done:'끝남',error:'실패',queued:'예약'};
const MODE={read:'읽기',work:'작업',full:'전체 권한'};
let project=new URLSearchParams(location.search).get('project')||localStorage.getItem('pitcrew.project')||'default';
let boot=null,flow=null,details={},selected=localStorage.getItem('pitcrew.flow.selected')||'',filter='',studio=null,dragging=false;
const join=(...xs)=>xs.filter(x=>x!=null&&String(x).trim()).join(' · ');
const clip=(t,n=90)=>{const s=String(t||'').replace(/\s+/g,' ').trim();return s.length>n?s.slice(0,n)+'…':s;};
const pad=n=>String(n).padStart(2,'0');
const when=ts=>{if(ts==null||ts==='')return '';const n=typeof ts==='number'?ts*1000:Date.parse(ts);if(!Number.isFinite(n))return '';const d=new Date(n),now=new Date();const t=pad(d.getHours())+':'+pad(d.getMinutes());return d.toDateString()===now.toDateString()?t:(d.getMonth()+1)+'.'+d.getDate()+' '+t;};
const dur=s=>s<60?Math.round(s)+'초':s<3600?Math.floor(s/60)+'분 '+pad(Math.round(s%60))+'초':Math.floor(s/3600)+'시간 '+Math.floor(s%3600/60)+'분';
const kTok=n=>n==null?'?':n>=1000?(n/1000).toFixed(n>=10000?0:1)+'k':String(n);
const provider=p=>({claude:'Claude Code',codex:'Codex'})[p]||p||'';
const model=a=>String(a.actualModel||a.model||'').replace(/^claude-/,'');
const agentOf=id=>boot?.agents.find(a=>a.id===id);
const chatLink=(aid,mid)=>`/?project=${encodeURIComponent(project)}&agent=${encodeURIComponent(aid)}${mid?'&message='+encodeURIComponent(mid):''}`;
async function api(route,body){const r=await fetch('/api/hub/'+route+(route.includes('?')?'&':'?')+'project='+encodeURIComponent(project),{method:body?'POST':'GET',headers:body?{'Content-Type':'application/json','X-PitCrew-Token':boot?.token||''}:{},body:body?JSON.stringify(body):undefined});const j=await r.json();if(!r.ok)throw Error(j.error||'서버 연결 실패');return j;}
function setLive(ok,text){const e=$('#live');e.innerHTML=`<i class="${ok?'run':''}"></i>${esc(text)}`;}
function projects(){const s=$('#project');s.innerHTML=(boot.projects||[]).map(p=>`<option value="${esc(p.id)}" ${p.id===project?'selected':''}>${esc(p.name)}</option>`).join('');s.onchange=()=>{project=s.value;localStorage.setItem('pitcrew.project',project);details={};selected='';load(true);};}
function turns(d){const out=[];let cur=null;for(const m of d.messages||[]){if(m.role==='user'){cur={req:m,tools:0,ans:null,incoming:!!(m.origin&&m.origin.agent)};out.push(cur);}else if(cur){if(m.role==='tool')cur.tools++;else if(m.role==='assistant'&&(m.text||'').trim())cur.ans=m;}}return out;}
async function loadDetails(){const list=visibleAgents().slice(0,14);await Promise.all(list.map(async a=>{try{const d=await api('detail?agent='+encodeURIComponent(a.id)+'&etag='+encodeURIComponent(details[a.id]?.etag||''));if(d.messages)details[a.id]={etag:d.etag,turns:turns(d)};}catch(e){}}));}
function visibleAgents(){return (boot?.agents||[]).filter(a=>!a.archived&&!a.source).sort((a,b)=>(b.updated||0)-(a.updated||0));}
async function load(first){try{boot=await api('bootstrap');flow=await api('flow');if(PAGE==='flow'){try{studio=await api('studio');}catch(e){studio=null;}}setLive(true,join(boot.runtime?.independent?'runtime ready':'연결됨',boot.agents.filter(a=>a.status==='running').length+' 작업 중'));projects();if(first)render();await loadDetails();render();}catch(e){setLive(false,'연결 실패');document.querySelector('main').insertAdjacentHTML('afterbegin',`<div class="err">${esc(e.message)}</div>`);}}
function render(){if(!boot||dragging)return;PAGE==='flow'?renderFlow():renderTeam();}

/* ---------- 흐름: 데이터 → 카드 ---------- */
const LANES=[['req','요청','도현'],['work','작업 중','팀'],['check','결과 확인','도현'],['adopt','채택','공통 방향']];
function flowCards(){
  const now=Date.now()/1000,cards=[];
  for(const a of visibleAgents()){
    const t=(details[a.id]?.turns||[]).filter(x=>!x.incoming);const last=t[t.length-1];
    const run=['running','stopping'].includes(a.status);
    if(last)cards.push({col:'req',id:'req:'+a.id,agent:a,turn:last,ts:last.req.ts,text:last.req.text,mid:last.req.id});
    if(run)cards.push({col:'work',id:'work:'+a.id,agent:a,turn:last,ts:a.runStartedAt||a.updated,state:'running',text:last?.req.text||a.lastTask||'작업 중',mid:last?.req.id,activity:a.currentActivity});
    else if(a.status==='waiting')cards.push({col:'check',id:'work:'+a.id,agent:a,turn:last,ts:a.updated,state:'waiting',text:last?.req.text||'확인 필요',mid:last?.req.id});
    else if(a.status==='error')cards.push({col:'check',id:'work:'+a.id,agent:a,turn:last,ts:a.updated,state:'error',text:a.error||last?.req.text||'실패',mid:last?.req.id});
    else if(last?.ans&&last.ans.ts>=now-86400)cards.push({col:'check',id:'work:'+a.id,agent:a,turn:last,ts:last.ans.ts,state:'done',text:last.ans.text,mid:last.ans.id});
  }
  for(const ap of boot.approvals||[])cards.push({col:'check',id:'ask:'+ap.id,agent:agentOf(ap.agent)||{name:ap.name||'팀원',id:ap.agent},ts:ap.ts||now,state:'ask',text:ap.title||ap.prompt||ap.text||'승인 요청',approval:ap});
  for(const d of (studio?.decisions||[]).filter(x=>x.kind==='adopt'||x.type==='adopt'||x.adopted))cards.push({col:'adopt',id:'adopt:'+(d.id||d.ts),agent:{name:'도현',id:''},ts:d.ts||d.created||now,state:'adopt',text:d.text||d.title||''});
  return cards.sort((a,b)=>(b.ts||0)-(a.ts||0));
}
function card(c){
  const a=c.agent,pill=c.state==='running'?'<span class="pill run">작업 중</span>':c.state==='waiting'||c.state==='ask'?'<span class="pill warn">확인 필요</span>':c.state==='error'?'<span class="pill err">실패</span>':c.state==='done'?'<span class="pill ok">끝남</span>':c.state==='adopt'?'<span class="pill acc">채택</span>':'';
  const meta=c.col==='req'?`<span class="dot"></span><span class="nm">${esc(a.name)}에게</span><span class="sp"></span><span class="tm">${when(c.ts)}</span>`:c.col==='adopt'?`<span class="dot done"></span><span class="nm">도현 결정</span><span class="sp"></span><span class="tm">${when(c.ts)}</span>`:`<span class="dot ${esc(a.status||'')}"></span><span class="nm">${esc(join(a.name,model(a)))}</span><span class="sp"></span><span class="tm">${c.state==='running'?dur(Date.now()/1000-(c.ts||Date.now()/1000)):when(c.ts)}${c.turn?' · '+c.turn.tools+' tools':''}</span>`;
  const act=c.state==='running'&&c.activity?`<div class="act">▸ ${esc(join(c.activity.title,clip(c.activity.label||'',60)))}</div>`:'';
  return `<div class="card node ${selected===c.id?'sel':''}" data-id="${esc(c.id)}" data-agent="${esc(a.id||'')}">${pill}<p class="t">${esc(clip(c.text,120))}</p><div class="r">${meta}</div>${act}</div>`;
}

/* ---------- 흐름: 캔버스 ---------- */
const LANE_W=300,LANE_GAP=48,LANE_X=i=>40+i*(LANE_W+LANE_GAP),TOP=64;
const posKey=()=>'pitcrew.flow.pos.'+project,viewKey=()=>'pitcrew.flow.view.'+project;
let pos={},view={x:0,y:0,z:1};
function loadPos(){try{pos=JSON.parse(localStorage.getItem(posKey())||'{}');}catch{pos={};}try{view=Object.assign({x:0,y:0,z:1},JSON.parse(localStorage.getItem(viewKey())||'{}'));}catch{}}
function savePos(){localStorage.setItem(posKey(),JSON.stringify(pos));}
function saveView(){localStorage.setItem(viewKey(),JSON.stringify(view));}
function applyView(){const c=$('#canvas');if(c)c.style.transform=`translate(${view.x}px,${view.y}px) scale(${view.z})`;const z=$('#zoom');if(z)z.textContent=Math.round(view.z*100)+'%';}
function renderFlow(){
  if(!Object.keys(pos).length&&!view._loaded){loadPos();view._loaded=true;}
  const cards=flowCards(),today=new Date().setHours(0,0,0,0)/1000;
  const running=boot.agents.filter(a=>a.status==='running'&&!a.archived).length,asking=cards.filter(c=>c.col==='check'&&(c.state==='ask'||c.state==='waiting')).length,doneToday=(boot.alertLog||[]).filter(e=>e.kind==='done'&&e.ts>=today).length;
  let cost=0;for(const id in details)for(const t of details[id].turns){const u=t.ans?.turnUsage;if(u&&t.ans.ts>=today)cost+=Number(u.cost)||0;}
  $('#sum').innerHTML=`<span class="r"><b>${running}</b>작업 중</span><span class="w"><b>${asking}</b>내 확인 기다림</span><span><b>${doneToday}</b>오늘 끝남</span><span><b>${cards.filter(c=>c.col==='adopt').length}</b>채택</span><span><b>$${cost.toFixed(2)}</b>오늘 비용</span><span class="sp"></span><button id="fit">맞춤</button><button id="zoom">100%</button><button id="tidy">자동 정렬</button><button id="refresh">새로고침</button>`;
  $('#refresh').onclick=()=>load();$('#fit').onclick=fit;$('#zoom').onclick=()=>{view.z=1;applyView();saveView();};$('#tidy').onclick=()=>{pos={};savePos();renderFlow();fit();};
  const canvas=$('#canvas');
  canvas.innerHTML=`<svg class="wires" id="wires"></svg>`+LANES.map(([k,t,w],i)=>`<div class="lane-head" style="left:${LANE_X(i)}px;top:${TOP-40}px;width:${LANE_W}px">${t} <span>· ${w}</span><span class="n">${cards.filter(c=>c.col===k).length}</span></div>`).join('')+cards.map(card).join('');
  // 자리: 기억한 좌표가 있으면 그대로, 없으면 열 안에 시간순으로 쌓는다
  const ys=LANES.map(()=>TOP);
  for(const c of cards){const el=canvas.querySelector(`.node[data-id="${CSS.escape(c.id)}"]`);el.style.width=LANE_W+'px';const li=LANES.findIndex(l=>l[0]===c.col);let p=pos[c.id];if(!p){p={x:LANE_X(li),y:ys[li]};ys[li]+=el.offsetHeight+14;}el.style.left=p.x+'px';el.style.top=p.y+'px';}
  const maxY=Math.max(...cards.map(c=>{const el=canvas.querySelector(`.node[data-id="${CSS.escape(c.id)}"]`);return el.offsetTop+el.offsetHeight;}),600);
  canvas.style.height=(maxY+200)+'px';canvas.style.width=(LANE_X(LANES.length)+200)+'px';
  for(const el of canvas.querySelectorAll('.node'))el.ondblclick=()=>{const c=cards.find(x=>x.id===el.dataset.id);if(c?.agent?.id)location.href=chatLink(c.agent.id,c.mid);};
  wires(cards);applyView();renderSide(cards.find(c=>c.id===selected)||null);
  if(!view._fitted&&Object.keys(details).length){view._fitted=true;if(!localStorage.getItem(viewKey()))fit();}
}
function wires(cards){
  const svg=$('#wires'),canvas=$('#canvas');if(!svg)return;const paths=[];
  const rect=id=>{const el=canvas.querySelector(`.node[data-id="${CSS.escape(id)}"]`);if(!el)return null;return {l:el.offsetLeft,r:el.offsetLeft+el.offsetWidth,y:el.offsetTop+Math.min(30,el.offsetHeight/2)};};
  const link=(from,to,hot)=>{const a=rect(from),b=rect(to);if(!a||!b)return;const fwd=b.l>=a.r;const x1=fwd?a.r:a.l,x2=fwd?b.l:b.r,dx=Math.max(40,Math.abs(x2-x1)/2)*(fwd?1:-1);paths.push(`<path class="${hot?'hot':''}" d="M ${x1} ${a.y} C ${x1+dx} ${a.y}, ${x2-dx} ${b.y}, ${x2} ${b.y}"/>`);};
  for(const c of cards)if(c.col==='req')link(c.id,'work:'+c.agent.id,selected===c.id||selected==='work:'+c.agent.id);
  const recent=(flow?.edges||[]).filter(e=>['delegate','handoff','forward','research','cycle'].includes(e.kind)&&e.at&&e.at>=Date.now()/1000-86400*3);
  const seen=new Set();for(const e of recent){const k=e.from+'>'+e.to;if(seen.has(k))continue;seen.add(k);link('work:'+e.from,'req:'+e.to,selected==='work:'+e.from||selected==='req:'+e.to);}
  svg.setAttribute('width',canvas.style.width);svg.setAttribute('height',canvas.style.height);svg.innerHTML=paths.join('');
}
function fit(){const b=$('#board'),canvas=$('#canvas');const nodes=[...canvas.querySelectorAll('.node')];const minX=Math.min(LANE_X(0),...nodes.map(n=>n.offsetLeft))-24,minY=TOP-60,maxX=Math.max(LANE_X(LANES.length-1)+LANE_W,...nodes.map(n=>n.offsetLeft+n.offsetWidth))+24,maxY=Math.max(TOP+360,...nodes.map(n=>n.offsetTop+n.offsetHeight))+40;const z=Math.min(1,(b.clientWidth-20)/(maxX-minX),(b.clientHeight-20)/(maxY-minY));view.z=Math.max(.3,z);view.x=10-minX*view.z;view.y=10-minY*view.z;applyView();saveView();}
function setupCanvas(){
  const b=$('#board'),canvas=$('#canvas');let pan=null,drag=null;
  b.addEventListener('wheel',e=>{e.preventDefault();const r=b.getBoundingClientRect(),mx=e.clientX-r.left,my=e.clientY-r.top;const nz=Math.min(2.2,Math.max(.3,view.z*(e.deltaY<0?1.1:1/1.1)));view.x=mx-(mx-view.x)*nz/view.z;view.y=my-(my-view.y)*nz/view.z;view.z=nz;applyView();saveView();},{passive:false});
  b.addEventListener('pointerdown',e=>{if(e.button!==0)return;const node=e.target.closest('.node');if(node){drag={id:node.dataset.id,el:node,sx:e.clientX,sy:e.clientY,ox:node.offsetLeft,oy:node.offsetTop,moved:false};}else{pan={sx:e.clientX,sy:e.clientY,ox:view.x,oy:view.y,moved:false};}b.setPointerCapture(e.pointerId);});
  b.addEventListener('pointermove',e=>{if(drag){const dx=(e.clientX-drag.sx)/view.z,dy=(e.clientY-drag.sy)/view.z;if(Math.abs(dx)+Math.abs(dy)>3){drag.moved=true;dragging=true;b.classList.add('dragging');}if(drag.moved){drag.el.style.left=Math.round(drag.ox+dx)+'px';drag.el.style.top=Math.round(drag.oy+dy)+'px';wires(flowCards());}}else if(pan){const dx=e.clientX-pan.sx,dy=e.clientY-pan.sy;if(Math.abs(dx)+Math.abs(dy)>3)pan.moved=true;if(pan.moved){view.x=pan.ox+dx;view.y=pan.oy+dy;applyView();}}});
  const up=e=>{if(drag){if(drag.moved){pos[drag.id]={x:drag.el.offsetLeft,y:drag.el.offsetTop};savePos();}else{selected=drag.id;localStorage.setItem('pitcrew.flow.selected',selected);for(const n of canvas.querySelectorAll('.node'))n.classList.toggle('sel',n.dataset.id===selected);wires(flowCards());renderSide(flowCards().find(c=>c.id===selected)||null);}drag=null;dragging=false;b.classList.remove('dragging');}if(pan){if(pan.moved)saveView();pan=null;}};
  b.addEventListener('pointerup',up);b.addEventListener('pointercancel',up);
}
function renderSide(c){
  const s=$('#side');if(!c){s.innerHTML='<div class="empty">카드를 고르면 여기에 증거가 나옵니다.</div>';return;}
  const a=c.agent,d=details[a.id],t=c.turn,u=t?.ans?.turnUsage;
  const edges=(flow?.edges||[]).filter(e=>e.from===a.id||e.to===a.id).sort((x,y)=>(y.at||0)-(x.at||0)).slice(0,8);
  const name=id=>agentOf(id)?.name||'다른 대화';
  s.innerHTML=`<h2>${esc(clip(c.text,80))}</h2><div class="sub">${c.state==='running'?'<span class="pill run">작업 중</span>':c.state==='done'?'<span class="pill ok">끝남</span>':c.state==='ask'||c.state==='waiting'?'<span class="pill warn">확인 필요</span>':''}<span>${esc(join(a.name,a.provider&&provider(a.provider),a.provider&&model(a)))}</span></div>
  <div class="kv">${t?`<div><div class="lbl">요청 → 답</div><div class="v num">${when(t.req.ts)} → ${t.ans?when(t.ans.ts):'아직'}</div></div><div><div class="lbl">도구 호출</div><div class="v num">${t.tools}</div></div>`:''}${u?`<div><div class="lbl">토큰 입력 → 출력</div><div class="v num">${kTok(u.input)}${u.cached?' (캐시 '+kTok(u.cached)+')':''} → ${kTok(u.output)}</div></div><div><div class="lbl">비용</div><div class="v num">$${Number(u.cost||0).toFixed(3)}</div></div>`:''}${a.mode?`<div><div class="lbl">권한</div><div class="v">${MODE[a.mode]||esc(a.mode)}</div></div>`:''}${a.status?`<div><div class="lbl">상태</div><div class="v">${STATUS[a.status]||esc(a.status)}</div></div>`:''}</div>
  ${t?.ans?`<hr><div class="lbl">답 (앞부분)</div><div class="body">${esc(clip(t.ans.text,400))}</div>`:''}
  ${edges.length?`<hr><div class="lbl">연결</div><ul>${edges.map(e=>`<li><span>${e.to===a.id?'←':'→'}</span>${esc(e.to===a.id?name(e.from):name(e.to))} · ${KIND[e.kind]||esc(e.kind)}${e.at?' · '+when(e.at):''}${e.messageId?` <a href="${chatLink(e.to,e.messageId)}">보기</a>`:''}</li>`).join('')}</ul>`:''}
  ${d?.turns?.length?`<hr><div class="lbl">이 대화의 흐름 · 최근 ${Math.min(10,d.turns.length)}턴</div><div class="turns">${d.turns.slice(-10).reverse().map(x=>`<div class="turn"><div class="tm">${when(x.req.ts)}</div><div><div class="q">${x.incoming?'↙ ':''}${esc(clip(x.req.text,70))}</div>${x.ans?`<div class="a">${esc(clip(x.ans.text,90))}</div>`:'<div class="a dim">답 없음</div>'}<div class="m num">${x.tools} tools${x.ans?.turnUsage?' · '+kTok(x.ans.turnUsage.input)+'→'+kTok(x.ans.turnUsage.output)+' · $'+Number(x.ans.turnUsage.cost||0).toFixed(3):''}</div></div></div>`).join('')}</div>`:''}
  <hr><div class="acts">${a.id?`<a class="pri" href="${chatLink(a.id,c.mid)}">이 대화 열기</a>`:''}<a href="/team">팀에서 보기</a></div>`;
}

/* ---------- 팀 ---------- */
function renderTeam(){
  const teams=boot.teams||[],agents=visibleAgents(),inTeam=new Set(teams.flatMap(t=>t.members));
  const member=(a,lead)=>{const act=a.status==='running'&&a.currentActivity?`▸ ${join(a.currentActivity.title,clip(a.currentActivity.label||'',50))}`:a.lastTask?clip(a.lastTask,70):a.role||'';const st=a.status==='running'?'<span class="pill run">작업 중</span>':a.status==='waiting'?'<span class="pill warn">확인 필요</span>':a.status==='error'?'<span class="pill err">실패</span>':a.status==='done'?'<span class="pill ok">끝남</span>':'<span class="pill">대기</span>';
    return `<a class="member" href="${chatLink(a.id)}"><span class="dot ${esc(a.status||'')}"></span><span class="nm"><span class="nmt">${esc(a.name)}</span>${lead?'<span class="lead">팀장</span>':''}</span>${st}<span class="meta">${esc(join(provider(a.provider),model(a),MODE[a.mode]))}</span><span class="meta ${a.status==='running'?'act':''}">${esc(act)}</span></a>`;};
  const teamCard=t=>{const ms=t.members.map(agentOf).filter(a=>a&&!a.archived);if(!ms.length)return '';ms.sort((a,b)=>(b.id===t.lead)-(a.id===t.lead));return `<div class="card teamcard"><div class="h"><b>${esc(t.name)}</b><span class="ctx">${esc(clip(t.context||'',60))}</span><span class="dim num">${ms.length}</span></div>${ms.map(a=>member(a,a.id===t.lead)).join('')}</div>`;};
  const loose=agents.filter(a=>!inTeam.has(a.id));
  $('#teams').innerHTML=teams.map(teamCard).join('')+(loose.length?`<div class="card teamcard"><div class="h"><b>팀 없음</b><span class="dim num">${loose.length}</span></div>${loose.map(a=>member(a,false)).join('')}</div>`:'');
  renderFeed();
  const sh=$('#shared');if(document.activeElement!==sh&&!sh.dataset.dirty)sh.value=boot.shared||'';
}
function feedRows(){
  const rows=[];const name=id=>agentOf(id)?.name||'다른 대화';
  for(const e of flow?.edges||[])if(e.at)rows.push({ts:e.at,cat:'pass',from:name(e.from),to:name(e.to),kind:KIND[e.kind]||e.kind,text:e.label,link:e.messageId?chatLink(e.to,e.messageId):chatLink(e.to)});
  for(const d of boot.delegations||[])rows.push({ts:d.finishedAt||d.startedAt||d.ts,cat:d.status==='done'?'done':'pass',from:name(d.lead),to:name(d.member),kind:d.status==='done'?'맡긴 일 끝남':'일을 맡김',text:d.status==='done'?(d.summary||d.task):d.task,link:chatLink(d.member)});
  for(const e of boot.alertLog||[])if(e.kind!=='done'||!(boot.delegations||[]).some(d=>d.member===e.agent&&Math.abs((d.finishedAt||0)-e.ts)<120))rows.push({ts:e.ts,cat:e.kind==='waiting'||e.kind==='failed'||e.kind==='cycle'?'ask':'done',from:e.name||'팀',to:'도현',kind:({done:'끝남',waiting:'확인 요청',failed:'실패',cycle:'사이클'})[e.kind]||e.kind,text:e.why==='보냄'?'':e.why,link:e.agent?chatLink(e.agent):''});
  for(const ap of boot.approvals||[])rows.push({ts:ap.ts||Date.now()/1000,cat:'ask',from:agentOf(ap.agent)?.name||ap.name||'팀원',to:'도현',kind:'승인 기다림',text:ap.title||ap.prompt||ap.text,link:ap.agent?chatLink(ap.agent):''});
  // 오늘의 실행 기록(작업 시작·끝)도 같이 — 팀 사이 넘김만 있으면 조용한 날엔 "옛날 상태"처럼 보인다
  for(const e of boot.activity||[]){const txt=String(e.text||'');const m=txt.match(/^(작업 시작|작업 완료|작업 끝|완료|실행 종료|중단|실패)\s*[·•∙:：-]?\s*(.*)$/);rows.push({ts:e.ts,cat:'done',from:agentOf(e.agent)?.name||'팀',to:'',kind:m?m[1]:'활동',text:m?m[2]:txt,link:e.agent?chatLink(e.agent):''});}
  return rows.filter(r=>r.ts).sort((a,b)=>b.ts-a.ts).slice(0,100);
}
function renderFeed(){
  const rows=feedRows().filter(r=>!filter||r.cat===filter);
  $('#feed').innerHTML=rows.map(r=>`<div class="ev ${r.from==='도현'?'me':''}"><div class="time num">${when(r.ts)}</div><div><div class="line"><b>${esc(r.from)}</b>${r.to?`<span class="to">→</span><b>${esc(r.to)}</b>`:''} <span class="kd">${esc(r.kind)}</span></div>${r.text?`<div class="q">${esc(clip(r.text,220))}</div>`:''}${r.link?`<div class="k"><a href="${r.link}">그 대화</a></div>`:''}</div></div>`).join('')||'<div class="empty">아직 오간 것이 없습니다.</div>';
}
if(PAGE==='team'){document.addEventListener('click',e=>{const b=e.target.closest('.f[data-f]');if(!b)return;filter=b.dataset.f;for(const x of document.querySelectorAll('.f[data-f]'))x.classList.toggle('on',x===b);renderFeed();});
  $('#shared').oninput=e=>{e.target.dataset.dirty='1';$('#sharedHint').textContent='저장 안 됨';};
  $('#saveShared').onclick=async()=>{try{await api('shared',{text:$('#shared').value});delete $('#shared').dataset.dirty;$('#sharedHint').textContent='저장됨 · 모든 대화의 다음 요청에 붙음';}catch(e){$('#sharedHint').textContent=e.message;}};}
if(PAGE==='flow'){setupCanvas();window.addEventListener('resize',()=>{if(boot)wires(flowCards());});}
load(true);setInterval(()=>load(),8000);
