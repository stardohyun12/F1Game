/* Attachments: 📎 button, paste (screenshots) and drag & drop onto the composer.
   Files are stored under .runtime/uploads and listed in the prompt; the agent opens them with its file tools. */
(function(){
  'use strict';
  const MAX=20*1024*1024;
  const artifactPending=new WeakMap();
  const uploadSources=new WeakMap();
  state.attachments=state.attachments||{};
  const list=()=>state.attachments[state.selected]||(state.attachments[state.selected]=[]);
  const size=n=>n<1024?n+'B':n<1048576?Math.round(n/1024)+'KB':(n/1048576).toFixed(1)+'MB';
  function render(){
    const el=document.getElementById('attachments');if(!el)return;
    const failed=list().filter(f=>f.error);
    el.innerHTML=list().map((f,i)=>`<span class="attach-chip ${f.error?'failed':f.path?'':'uploading'}" title="${esc(f.error||f.path||'올리는 중')}">${f.preview?`<img src="${f.preview}" alt="">`:'<span class="attach-icon">📄</span>'}<span class="attach-name">${esc(f.name)}</span><small>${f.error?'실패':f.path?size(f.size):'올리는 중…'}</small>${f.error&&uploadSources.has(f)?`<button type="button" data-attach-retry="${i}" aria-label="${esc(f.name)} 업로드 다시 시도">재시도</button>`:''}<button type="button" data-attach-remove="${i}" aria-label="${esc(f.name)} 첨부 취소">×</button></span>`).join('')+(failed.length?`<div class="attachment-errors" role="status"><strong>첨부 ${failed.length}개를 올리지 못해 보낼 수 없습니다.</strong>${failed.map(f=>`<p>${esc(f.name)}: ${esc(f.error)}</p>`).join('')}<p>실패한 파일을 다시 올리거나 ×로 제거해 주세요. 재시도 버튼이 없으면 원래 결과물에서 다시 첨부하세요.</p></div>`:'');
    document.dispatchEvent(new Event('pitcrew-attachments-changed'));
  }
  async function upload(item,file,{id,project,token}){
    const present=()=>state.attachments[id]?.includes(item);
    if(!present())return false;
    uploadSources.set(item,file);
    item.size=file.size;
    if(item.preview)URL.revokeObjectURL(item.preview);
    if(file.type.startsWith('image/'))item.preview=URL.createObjectURL(file);
    if(state.selected===id&&state.projectId===project)render();
    const data=await new Promise((ok,fail)=>{const r=new FileReader();r.onload=()=>ok(String(r.result).split(',')[1]||'');r.onerror=()=>fail(new Error('파일을 읽지 못했습니다.'));r.readAsDataURL(file);});
    if(!present())return false;
    const saved=await PitCrewConnection.request('/api/hub/upload?project='+encodeURIComponent(project),{method:'POST',headers:{'Content-Type':'application/json','X-PitCrew-Token':token},body:JSON.stringify({name:item.name,type:file.type,data})},120000);
    if(!present())return false;
    Object.assign(item,{path:saved.path,size:saved.size});uploadSources.delete(item);return true;
  }
  async function retry(item){
    const id=state.selected,project=state.projectId,token=state.token,file=uploadSources.get(item);
    if(!file||!item.error||!state.attachments[id]?.includes(item))return;
    delete item.error;render();
    let finish,saved=false;artifactPending.set(item,new Promise(resolve=>{finish=resolve;}));
    const live=()=>state.selected===id&&state.projectId===project&&state.attachments[id]?.includes(item);
    try{saved=await upload(item,file,{id,project,token});}
    catch(e){if(state.attachments[id]?.includes(item))item.error=e.message||'업로드 실패';}
    finally{finish(saved);artifactPending.delete(item);if(live()){
      const restoreFocus=document.activeElement===document.body;
      render();const index=state.attachments[id].indexOf(item);
      if(restoreFocus)document.querySelector(`[data-attach-${item.error?'retry':'remove'}="${index}"]`)?.focus({preventScroll:true});
    }}
  }
  async function add(files){
    const id=state.selected,a=agent(id),project=state.projectId,token=state.token;if(!a||a.source)return toast('이 대화에는 파일을 첨부할 수 없습니다.');
    for(const file of files){
      if((state.attachments[id]||[]).length>=10){toast('첨부는 최대 10개입니다.');break;}
      const item={name:file.name||'붙여넣은 이미지.png',size:file.size};
      if(file.size>MAX){toast(`${item.name}: 20MB 이하만 첨부할 수 있습니다.`);continue;}
      state.attachments[id]=[...(state.attachments[id]||[]),item];render();
      try{
        await upload(item,file,{id,project,token});
      }catch(e){item.error=e.message||'업로드 실패';toast(`${item.name}: ${item.error}`);}
      if(state.selected===id)render();
    }
  }
  async function attachImage(src,label,{onError,snapshot,identity,quiet=false}={}){
    const id=state.selected,a=agent(id),project=state.projectId,token=state.token;
    const fail=message=>{if(state.selected===id&&state.projectId===project){if(!quiet)toast(message);onError?.(message);}return false;};
    if(!a||a.source)return fail('이 대화에는 파일을 첨부할 수 없습니다.');
    const url=snapshot?null:new URL(src,location.href);
    if(!snapshot&&(url.origin!==location.origin||!(url.pathname.startsWith('/media/')||url.pathname==='/local-image')))return fail('이 이미지의 첨부 경로를 확인할 수 없습니다.');
    const key=snapshot?{href:'comparison:'+identity}:new URL(url.href);if(!snapshot)key.searchParams.delete('pc_read');
    const items=state.attachments[id]||(state.attachments[id]=[]);
    const existing=items.find(f=>f.artifact===key.href&&!f.error);
    if(existing&&artifactPending.has(existing)){
      const saved=await artifactPending.get(existing);
      if(!state.attachments[id]?.includes(existing))return false;
      if(!saved&&existing.error&&state.selected===id&&state.projectId===project)onError?.(existing.error);
      return saved;
    }
    if(existing){if(!quiet)toast(existing.path?(url?.pathname==='/local-image'?'이미 첨부한 이미지입니다. 현재 파일로 바꾸려면 기존 첨부를 지운 뒤 다시 첨부하세요.':'이미 첨부한 이미지입니다.'):'이미지를 첨부하는 중입니다.');return !!existing.path;}
    const failed=items.findIndex(f=>f.artifact===key.href&&f.error);
    if(failed<0&&items.length>=10)return fail('첨부는 최대 10개입니다.');
    const item={name:String(label||'결과물 이미지').slice(0,180),size:0,artifact:key.href};
    let finish,completed=false;artifactPending.set(item,new Promise(resolve=>{finish=resolve;}));
    if(failed>=0){if(items[failed].preview)URL.revokeObjectURL(items[failed].preview);items.splice(failed,1,item);}else items.push(item);
    render();
    const controller=new AbortController(),timer=setTimeout(()=>controller.abort(),12000);
    try{
      let file=snapshot;
      if(!file){
        const response=await fetch(url.href,{signal:controller.signal,cache:'no-store'});
        if(!response.ok)throw new Error('이미지를 읽지 못했습니다. 결과물을 새로고침해 주세요.');
        if(Number(response.headers.get('content-length'))>MAX)throw new Error('20MB 이하만 첨부할 수 있습니다.');
        file=await response.blob();
      }
      if(file.size>MAX)throw new Error('20MB 이하만 첨부할 수 있습니다.');
      if(!/^image\/(png|jpeg|gif|webp|bmp)(;|$)/i.test(file.type))throw new Error('지원되는 이미지 파일이 아닙니다.');
      const ext=file.type.split('/')[1].split(';')[0].replace('jpeg','jpg');
      if(!/\.(png|jpe?g|gif|webp|bmp)$/i.test(item.name))item.name+='.'+ext;
      clearTimeout(timer);
      const saved=await upload(item,file,{id,project,token});
      completed=saved;
      if(saved&&!quiet&&state.selected===id&&state.projectId===project)toast('입력창에 이미지를 첨부했습니다. 요청을 적고 보내세요.');
      return saved;
    }catch(e){if(state.attachments[id]?.includes(item)){item.error=e.name==='AbortError'?'이미지 응답이 늦습니다.':e.message||'이미지를 첨부하지 못했습니다.';fail(item.error);}return false;}
    finally{clearTimeout(timer);finish(completed);artifactPending.delete(item);if(state.selected===id&&state.projectId===project)render();}
  }
  const fromArtifact=(src,label,options)=>attachImage(src,label,options);
  async function fromComparison(snapshots,{onError}={}){
    const fail=message=>{onError?.(message);return false;};
    if(!Array.isArray(snapshots)||snapshots.length!==2||snapshots.some(s=>!s||!(s.blob instanceof Blob)||!s.blob.size||s.blob.size>MAX||!/^[a-f0-9]{64}$/.test(s.hash)||!/^image\/(png|jpeg|gif|webp|bmp)$/i.test(s.blob.type)))return fail('비교 이미지 두 장을 모두 읽은 뒤 첨부하세요.');
    const items=list(),keys=[...new Set(snapshots.map(s=>'comparison:'+s.hash))];
    if(items.length+keys.filter(key=>!items.some(item=>item.artifact===key)).length>10)return fail('비교 이미지를 넣을 공간이 부족합니다. 기존 첨부를 지워 주세요. 첨부는 최대 10개입니다.');
    // Each call reserves its chip synchronously before its first await.
    const results=await Promise.all(snapshots.map((s,i)=>attachImage(null,(i?'B — ':'A — ')+s.label,{snapshot:s.blob,identity:s.hash,onError,quiet:true})));
    return results.every(Boolean);
  }
  function ready(){const l=list();return {pending:l.some(f=>!f.path&&!f.error),failed:l.filter(f=>f.error).length,files:l.filter(f=>f.path&&!f.error).map(f=>({path:f.path,name:f.name}))};}
  function focusFailure(){document.querySelector('.attach-chip.failed [data-attach-retry],.attach-chip.failed [data-attach-remove]')?.focus();}
  function clear(id){for(const f of state.attachments[id]||[]){if(f.preview)URL.revokeObjectURL(f.preview);uploadSources.delete(f);}state.attachments[id]=[];if(state.selected===id)render();}
  function consume(id,items){
    const sent=new Set(items);
    state.attachments[id]=(state.attachments[id]||[]).filter(f=>{if(!sent.has(f))return true;if(f.preview)URL.revokeObjectURL(f.preview);uploadSources.delete(f);return false;});
    if(state.selected===id)render();
  }
  function setup(){
    const form=document.getElementById('composer'),input=document.getElementById('attachInput');if(!form||!input)return;
    // A retry redraw can move another control under the second click of a double-click.
    let retryClickSequence=false;
    document.addEventListener('click',e=>{
      if(e.detail>1&&retryClickSequence){e.preventDefault();e.stopImmediatePropagation();return;}
      retryClickSequence=e.detail===1&&!!e.target.closest('[data-attach-retry]');
    },true);
    document.getElementById('attach').onclick=()=>input.click();
    input.onchange=()=>{add([...input.files]);input.value='';};
    // Screenshots and copied images: some sources fill only clipboardData.items, so read both.
    const pasted=e=>{const d=e.clipboardData;if(!d)return [];const files=[...(d.files||[])];
      return files.length?files:[...(d.items||[])].filter(i=>i.kind==='file').map(i=>i.getAsFile()).filter(Boolean);};
    document.getElementById('prompt').addEventListener('paste',e=>{const files=pasted(e);if(files.length){e.preventDefault();add(files);}});
    // Pasting while the conversation (not another field) has focus also attaches, then puts you back in the composer.
    document.addEventListener('paste',e=>{const t=e.target;if(e.defaultPrevented||t.isContentEditable||t.closest?.('#prompt')||t.closest?.('input,textarea,select,dialog'))return;
      if(!agent(state.selected)||agent(state.selected).source)return;const files=pasted(e);if(!files.length)return;e.preventDefault();add(files);document.getElementById('prompt')?.focus();});
    form.addEventListener('dragover',e=>{if([...e.dataTransfer.types].includes('Files')){e.preventDefault();form.classList.add('drop-target');}});
    form.addEventListener('dragleave',e=>{if(!form.contains(e.relatedTarget))form.classList.remove('drop-target');});
    // Files dropped anywhere on the conversation are attached too (not opened by the browser).
    const pane=document.querySelector('.conversation')||document.body;let depth=0;
    const endDrag=()=>{depth=0;pane.classList.remove('file-drag');form.classList.remove('drop-target');};
    form.addEventListener('drop',e=>{if(e.dataTransfer.files.length){e.preventDefault();e.stopPropagation();endDrag();add([...e.dataTransfer.files]);}});
    window.addEventListener('dragend',endDrag);window.addEventListener('blur',endDrag);
    pane.addEventListener('dragenter',e=>{if([...e.dataTransfer.types].includes('Files')){depth++;pane.classList.add('file-drag');}});
    pane.addEventListener('dragleave',()=>{if(--depth<=0){depth=0;pane.classList.remove('file-drag');}});
    pane.addEventListener('dragover',e=>{if([...e.dataTransfer.types].includes('Files'))e.preventDefault();});
    pane.addEventListener('drop',e=>{endDrag();if(e.dataTransfer.files.length){e.preventDefault();add([...e.dataTransfer.files]);document.getElementById('prompt')?.focus();}});
    document.getElementById('attachments').addEventListener('click',e=>{const r=e.target.closest('[data-attach-retry]');if(r){retry(list()[+r.dataset.attachRetry]);return;}const b=e.target.closest('[data-attach-remove]');if(!b)return;const l=list(),index=+b.dataset.attachRemove,f=l[index];if(f?.preview)URL.revokeObjectURL(f.preview);if(f)uploadSources.delete(f);l.splice(index,1);render();
      (document.querySelector(`[data-attach-remove="${Math.min(index,l.length-1)}"]`)||document.getElementById('prompt'))?.focus();
    });
  }
  setup();
  window.PitCrewAttach={render,ready,clear,consume,focusFailure,fromArtifact,fromComparison};
})();
