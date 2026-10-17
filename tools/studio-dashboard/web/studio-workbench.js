'use strict';
// Workbench state is per project and room; browsing never starts an agent.
(()=>{
const kinds={reference:'참고',candidate:'시안',applied:'게임 적용 캡처',adopted:'사용자 채택',build:'실행 빌드',file:'파일'};
let view='work',selected='',comparison='',assetHash='',terminal=null,session='',offset=0,terminalBusy=false,terminalProject='',writeChain=Promise.resolve();
const storageKey=()=>key()+'.workbench';
const load=()=>{try{return JSON.parse(localStorage.getItem(storageKey())||'{}')}catch{return {}}};
function save(v){localStorage.setItem(storageKey(),JSON.stringify({...load(),...v}));}
$('.desk-body').insertAdjacentHTML('afterbegin',`<section id="workbench" aria-label="작업 대상"><div id="production-map" hidden></div><div id="image-work"><div class="work-tools"><b>이미지 · 버전 비교</b><label class="file-button">자료 추가<input id="image-upload" type="file" accept="image/png,image/jpeg,image/webp,image/gif" multiple></label></div><p class="muted">이미지를 붙여넣거나 여기에 놓으세요. 원본·시안을 선택하고 용도를 적으면 실제 이미지 입력으로 전달됩니다.</p><div id="image-stage"><div class="image-frame"><img id="selected-image" alt="선택한 이미지 버전" hidden><div id="region-mark" hidden></div></div><img id="compare-image" alt="비교 원본" hidden></div><p id="image-empty" class="empty">아직 이미지가 없습니다. 기존 참고 자료를 첨부해 첫 시안을 요청하세요.</p><div id="image-controls" hidden><label>비교할 버전 <select id="compare-select"><option value="">비교 안 함</option></select></label><button id="region-clear">영역 지우기</button><p class="muted">이미지 위를 드래그해 영역을 지정하세요. 키보드로도 아래 좌표를 입력할 수 있습니다.</p><div class="region-fields">${['x','y','w','h'].map(k=>`<label>${k}<input data-region="${k}" type="number" min="0" max="1" step="0.01" aria-label="영역 ${k}"></label>`).join('')}</div><label for="image-purpose">이 버전·영역의 용도 또는 수정 의견</label><textarea id="image-purpose" rows="2" placeholder="예: 차체의 노랑은 유지하고 지붕의 채도를 낮춰 주세요."></textarea><label><input type="checkbox" id="image-include"> 다음 팀 요청에 이 이미지 포함</label><div class="asset-actions"><select id="asset-kind" aria-label="결과 분류">${Object.entries(kinds).map(([k,v])=>`<option value="${k}">${v}</option>`).join('')}</select><button id="asset-save">분류·의견 저장</button><button id="asset-adopt">이 버전과 의견 채택</button></div><p id="asset-evidence" class="muted"></p><div id="asset-source"></div><details><summary>무엇을 바꿨고 왜 달라졌나요?</summary><p id="asset-explanation"></p></details></div><div id="asset-list"></div></div><details id="native-cli"><summary>실제 CLI · 독립 세션</summary><p>프로젝트 폴더에서 실행하는 실제 CLI입니다. 팀 대화·이미지는 자동 전달되지 않습니다. 실행 중에는 구조화 요청을 잠급니다. 브라우저를 다시 열어도 같은 서버의 CLI에 재연결합니다.</p><select id="cli-provider" aria-label="CLI 종류"><option value="codex">Codex</option><option value="claude">Claude Code</option></select><button id="cli-start">독립 CLI 시작</button><button id="cli-interrupt">Ctrl+C</button><button id="cli-close">CLI 종료</button><span id="cli-state" role="status"></span><div id="terminal"></div></details></section>`);
// Conversation stays visible; secondary tools expand in the result rail.
$('.desk-body').prepend($('#conversation'));
const imageDetails=document.createElement('details');imageDetails.id='image-details';
imageDetails.innerHTML='<summary>이미지 비교·첨부·채택</summary>';
$('#image-work').before(imageDetails);imageDetails.append($('#image-work'));
const flowDetails=document.createElement('details');flowDetails.id='flow-details';flowDetails.open=true;
flowDetails.innerHTML='<summary>워크플로우 · 실제 배정 관계</summary>';
$('#production-map').before(flowDetails);flowDetails.append($('#production-map'));
$('#workbench').append($('#results'));
$('#composer').insertAdjacentHTML('afterbegin','<button type="button" id="attach-image">＋ 이미지 첨부·비교</button>');
$('#attach-image').onclick=()=>{imageDetails.open=true;imageDetails.scrollIntoView({block:'nearest'});$('#image-upload').focus();};
const history=document.createElement('details');history.innerHTML='<summary>작업 이력·예약·취소</summary>';history.append($('#results h2'),$('#jobs'),$('#resume'));$('#results').append(history);
$('#composer').insertAdjacentHTML('afterbegin','<div id=run-controls></div>');
function assets(){return (data?.assets||[]).filter(a=>a.type?.startsWith('image/'));}
function inputs(){return Object.entries(load().selections||{}).filter(([,v])=>v.include).map(([id,v])=>({id,purpose:v.purpose||'',region:v.region||null}));}
function selection(){return (load().selections||{})[selected]||{};}
function setSelection(value){const selections=load().selections||{};selections[selected]={...selection(),...value};save({selections});}
function selectImage(id){selected=id;save({selected:id});renderImage();}
function renderImage(){
 const a=assets().find(a=>a.id===selected),s=selection();
 $('#image-empty').hidden=!!a;$('#image-controls').hidden=!a;$('#selected-image').hidden=!a;
 if(!a)return;
 if($('#selected-image').getAttribute('src')!==a.src)$('#selected-image').src=a.src;
 $('#image-purpose').value=s.purpose??a.purpose??a.explanation??'';$('#image-include').checked=!!s.include;
 $('#asset-kind').value=a.kind;$('#asset-explanation').textContent=a.explanation||a.purpose||'아직 설명이 없습니다.';const sourceJob=data.jobs.find(j=>j.id===a.run);$('#asset-source').innerHTML=sourceJob?`<button data-job="${esc(sourceJob.id)}">원래 작업·대화 열기</button>`:'';$('#asset-evidence').textContent=`${kinds[a.kind]} · 버전 ${a.version.slice(0,8)} · ${a.name} · ${a.verification||'미검증'}`;
 $('#compare-select').innerHTML='<option value="">비교 안 함</option>'+assets().filter(b=>b.id!==a.id).map(b=>`<option value="${esc(b.id)}">${esc(kinds[b.kind]+' · '+b.name+' · '+b.version.slice(0,8))}</option>`).join('');
 comparison=load().comparison||'';$('#compare-select').value=comparison;const other=assets().find(b=>b.id===comparison);$('#compare-image').hidden=!other;if(other)$('#compare-image').src=other.src;
 document.querySelectorAll('[data-region]').forEach(i=>i.value=s.region?.[i.dataset.region]??'');drawRegion(s.region);
}
function drawRegion(r){const marker=$('#region-mark');marker.hidden=!r;if(r)Object.assign(marker.style,{left:r.x*100+'%',top:r.y*100+'%',width:r.w*100+'%',height:r.h*100+'%'});}
function render(){
 const saved=load();view=saved.view||'work';document.body.dataset.view=view;
 selected=saved.selected||assets()[0]?.id||'';
 const hash=JSON.stringify([project,room,data?.assets]);if(hash!==assetHash){assetHash=hash;$('#asset-list').innerHTML=assets().map(a=>`<button data-asset="${esc(a.id)}"><img src="${esc(a.src)}" alt=""><span>${esc(kinds[a.kind])} · ${esc(a.name)}<small>${esc(a.version.slice(0,8))}</small></span></button>`).join('');renderImage();}
 $('#production-map').hidden=false;$('#image-work').hidden=false;
 document.querySelectorAll('[data-view]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.view===view)));
 renderFlow();
 $('#run-controls').innerHTML=(data?.jobs||[]).filter(j=>j.room===room&&['running','queued'].includes(j.status)).map(j=>`<button type="button" data-cancel="${esc(j.id)}">${j.status==='running'?'현재 작업 중단':'예약 취소'}</button>`).join('');
 const own=(data?.terminal?.sessions||[]).filter(s=>s.room===room).at(-1);
 if(terminalProject!==project||session!==(own?.id||'')){session=own?.id||'';terminalProject=project;offset=0;terminal?.reset();}
 $('#cli-state').textContent=own?(own.alive?'CLI 실행 중 · 구조화 요청 잠김':'CLI 종료됨'):'시작 전';
 $('#cli-start').disabled=!!data?.terminal?.active||!!data?.active;$('#cli-close').disabled=!own?.alive;$('#cli-interrupt').disabled=!own?.alive;
 if(data?.terminal?.active){$('#send').disabled=true;$('#queueHint').textContent='실제 CLI 사용 중 — CLI 종료 후 팀 요청을 보내세요.';}
}
function renderFlow(){
 const jobs=data?.jobs||[], shown=jobs.slice(-30).reverse(), positions=new Map();
 const depth=j=>{let d=0,p=j.parent,seen=new Set([j.id]);while(p&&!seen.has(p)&&d<4){seen.add(p);d++;p=jobs.find(k=>k.id===p)?.parent;}return d;};
 shown.forEach((j,i)=>positions.set(j.id,{x:16+depth(j)*40,y:16+i*76}));
 const height=Math.max(85,shown.length*76+24);
 const edges=shown.filter(j=>positions.has(j.parent)).map(j=>{const a=positions.get(j.parent),b=positions.get(j.id);return `<path d="M ${a.x+8} ${a.y+54} V ${b.y+27} H ${b.x}" fill="none" stroke="#8faee1" stroke-width="2" marker-end="url(#arrow)"/>`;}).join('');
 const nodes=shown.map(j=>{const p=positions.get(j.id),name=data.rooms.find(r=>r.id===j.room)?.name||j.room;return `<g role="button" tabindex="0" data-flow-job="${esc(j.id)}" aria-label="${esc(name+' '+j.text)}"><rect x="${p.x}" y="${p.y}" width="220" height="54" rx="8" fill="#1c2533" stroke="${j.status==='running'?'#94b3ff':'#445166'}"/><text x="${p.x+12}" y="${p.y+20}" fill="#dae6fb">${esc(name)} · ${esc(labels[j.status]||j.status)}</text><text x="${p.x+12}" y="${p.y+40}" fill="#a9b7cb">${esc(j.text.slice(0,20))}</text></g>`;}).join('');
 $('#production-map').innerHTML=shown.length?`<p class="muted">선은 실제 작업 전달입니다. 노드를 누르면 해당 대화로 이동합니다.${jobs.length>30?' 최근 30건 표시. 전체 이력은 아래에서 확인하세요.':''}</p><div class="flow-canvas"><svg viewBox="0 0 410 ${height}" width="410" height="${height}" aria-label="작업 배정 흐름"><defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="5" markerHeight="5" orient="auto"><path d="M 0 0 L 10 5 L 0 10 z" fill="#8faee1"/></marker></defs>${edges}${nodes}</svg></div>`:'<p class="muted">팀장에게 요청하면 필요한 팀에 배정한 작업이 여기에 연결됩니다.</p>';
}
function openFlow(e){const n=e.target.closest('[data-flow-job]');if(!n)return;const j=data.jobs.find(j=>j.id===n.dataset.flowJob);if(j)switchRoom(j.room,j.message).catch(fail);}
$('#production-map').onclick=openFlow;
$('#production-map').onkeydown=e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();openFlow(e);}};
async function upload(files){for(const file of [...files].filter(f=>f.type.startsWith('image/'))){if(file.size>20*1024*1024)throw Error('이미지는 20 MB 이하로 첨부하세요.');const encoded=await new Promise((resolve,reject)=>{const r=new FileReader();r.onload=()=>resolve(r.result.split(',')[1]);r.onerror=reject;r.readAsDataURL(file);});const a=await api('studio-assets',{name:file.name,type:file.type,data:encoded,room});selected=a.id;imageDetails.open=true;save({selected});setSelection({include:true,purpose:''});}await refresh();}
$('#image-upload').onchange=e=>upload(e.target.files).catch(fail);
$('#workbench').ondragover=e=>{if(e.dataTransfer.types.includes('Files'))e.preventDefault();};
$('#workbench').ondrop=e=>{e.preventDefault();upload(e.dataTransfer.files).catch(fail);};
document.addEventListener('paste',e=>{if(e.target.closest('#terminal'))return;const files=[...(e.clipboardData?.files||[])];if(files.some(f=>f.type.startsWith('image/'))){e.preventDefault();upload(files).catch(fail);}});
$('#image-purpose').oninput=e=>setSelection({purpose:e.target.value});$('#image-include').onchange=e=>setSelection({include:e.target.checked,purpose:$('#image-purpose').value});
$('#compare-select').onchange=e=>{save({comparison:e.target.value});renderImage();};
$('#region-clear').onclick=()=>{setSelection({region:null});renderImage();};
document.querySelectorAll('[data-region]').forEach(i=>i.onchange=()=>{const r=Object.fromEntries([...document.querySelectorAll('[data-region]')].map(i=>[i.dataset.region,Number(i.value)]));setSelection({region:r});drawRegion(r);});
let origin=null;const frame=$('.image-frame');
function point(e){const b=frame.getBoundingClientRect();return {x:Math.max(0,Math.min(1,(e.clientX-b.left)/b.width)),y:Math.max(0,Math.min(1,(e.clientY-b.top)/b.height))};}
frame.onpointerdown=e=>{if(!selected)return;e.preventDefault();origin=point(e);frame.setPointerCapture(e.pointerId);};
frame.onpointermove=e=>{if(!origin)return;const p=point(e);drawRegion({x:Math.min(origin.x,p.x),y:Math.min(origin.y,p.y),w:Math.abs(origin.x-p.x),h:Math.abs(origin.y-p.y)});};
frame.onpointerup=e=>{if(!origin)return;const p=point(e),r={x:Math.min(origin.x,p.x),y:Math.min(origin.y,p.y),w:Math.abs(origin.x-p.x),h:Math.abs(origin.y-p.y)};origin=null;if(r.w>.005&&r.h>.005)setSelection({region:r,include:true});renderImage();};
async function classify(kind){await api('studio-assets',{action:'update',id:selected,kind,explanation:$('#image-purpose').value,parent:$('#compare-select').value||undefined});await refresh();}
$('#asset-save').onclick=()=>classify($('#asset-kind').value).catch(fail);$('#asset-adopt').onclick=()=>classify('adopted').catch(fail);
document.addEventListener('click',e=>{const b=e.target.closest('button');if(!b)return;if(b.dataset.asset){selectImage(b.dataset.asset);imageDetails.open=true;imageDetails.scrollIntoView({block:'nearest'});}if(b.dataset.view){save({view:b.dataset.view});if(b.dataset.view==='play'){const latest=assets().filter(a=>a.kind==='applied').at(-1)||assets().find(a=>a.kind==='adopted');if(latest)selectImage(latest.id);}render();}if(b.dataset.room||b.dataset.job){save({view:'work'});setTimeout(render,0);}});
function ensureTerminal(){if(terminal)return;terminal=new Terminal({cursorBlink:true,fontFamily:'Consolas, monospace',fontSize:13,cols:100,rows:28,scrollback:3000,allowProposedApi:false,theme:{background:'#090b10',foreground:'#d9dee8'}});terminal.open($('#terminal'));terminal.parser.registerOscHandler(10,()=>true);terminal.parser.registerOscHandler(11,()=>true);terminal.onData(text=>{if(!session)return;const id=session,p=project,inputId=crypto.randomUUID();writeChain=writeChain.then(()=>{if(p!==project||id!==session)return;return api('studio-terminal',{action:'input',id,text,inputId});}).catch(fail);});new ResizeObserver(()=>{if(!session||!$('#native-cli').open)return;const cols=Math.max(30,Math.min(240,Math.floor($('#terminal').clientWidth/8)));terminal.resize(cols,28);api('studio-terminal',{action:'resize',id:session,cols,rows:28}).catch(fail);}).observe($('#terminal'));}
$('#native-cli').ontoggle=()=>{if($('#native-cli').open)ensureTerminal();};
$('#cli-start').onclick=async()=>{try{const result=await api('studio-terminal',{action:'start',room,provider:$('#cli-provider').value});session=result.id;offset=0;ensureTerminal();terminal.reset();await refresh();}catch(e){fail(e);}};
for(const [id,action] of [['cli-close','close'],['cli-interrupt','interrupt']])$('#'+id).onclick=()=>api('studio-terminal',{action,id:session}).then(refresh).catch(fail);
setInterval(async()=>{if(!session||!$('#native-cli').open||terminalBusy)return;terminalBusy=true;const id=session,p=project;try{const r=await api('studio-terminal?id='+id+'&offset='+offset);if(id!==session||p!==project)return;ensureTerminal();if(r.reset)terminal.reset();terminal.write(r.output);offset=r.offset;}catch(e){fail(e);}finally{terminalBusy=false;}},350);
globalThis.workbench={render,inputs};
})();
