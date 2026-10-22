// 대화 화면 v2 스킨 켜기/끄기. 스킨은 chat-v2.css 가 html[data-skin=v2] 로 입힌다. 기본은 켜짐; 상단 '예전 화면' 으로 끈다 (브라우저에 저장).
(function(){
  'use strict';
  const KEY='pitcrew.skin';
  function current(){try{return localStorage.getItem(KEY)||'v2'}catch{return 'v2'}}
  function apply(skin){
    document.documentElement.dataset.skin=skin;
    if(skin==='v2')document.documentElement.dataset.theme='dark';   // 스킨은 어두운 화면 기준
    const b=document.getElementById('skinToggle');if(b)b.textContent=skin==='v2'?'예전 화면':'새 화면';
  }
  apply(current());
  // 브라우저 기본 <select> 목록은 꾸밀 수 없어 투박하다 → 같은 select 를 뒤에 두고 앞에 칩+메뉴를 그린다 (값·change 이벤트는 그대로 select 로)
  function enhance(sel){
    if(sel.dataset.v2||sel.closest('#modal'))return;sel.dataset.v2='1';
    const chip=document.createElement('button');chip.type='button';chip.className='v2-select';chip.setAttribute('aria-haspopup','listbox');
    const label=()=>{const o=sel.selectedOptions[0];chip.innerHTML='<span>'+(o?o.textContent:'')+'</span><i>⌄</i>';};
    sel.after(chip);sel.classList.add('v2-hidden');label();
    sel.addEventListener('change',label);new MutationObserver(label).observe(sel,{childList:true,subtree:true,characterData:true,attributes:true});
    chip.onclick=e=>{e.stopPropagation();closeMenus();if(sel.disabled)return;const m=document.createElement('div');m.className='v2-menu';m.setAttribute('role','listbox');
      m.innerHTML=[...sel.options].map((o,i)=>`<button type="button" role="option" data-i="${i}" ${o.disabled?'disabled':''} aria-selected="${o.selected}">${o.textContent.replace(/</g,'&lt;')}</button>`).join('');
      m.onclick=ev=>{const b=ev.target.closest('[data-i]');if(!b)return;sel.selectedIndex=+b.dataset.i;sel.dispatchEvent(new Event('change',{bubbles:true}));closeMenus();chip.focus();};
      const r=chip.getBoundingClientRect();m.style.left=r.left+'px';m.style.top=(r.bottom+4)+'px';m.style.minWidth=Math.max(160,r.width)+'px';document.body.append(m);
      if(r.bottom+4+m.offsetHeight>innerHeight)m.style.top=Math.max(8,r.top-4-m.offsetHeight)+'px';};
  }
  function closeMenus(){for(const m of document.querySelectorAll('.v2-menu'))m.remove();}
  document.addEventListener('click',closeMenus);document.addEventListener('keydown',e=>{if(e.key==='Escape')closeMenus();});
  function enhanceAll(){if(current()!=='v2')return;for(const s of document.querySelectorAll('.sidebar select,#modeQuick'))enhance(s);}
  // 협업 보기·팀 흐름 창 → 새 페이지로 (같은 일을 더 넓게 보여준다)
  document.addEventListener('click',e=>{if(current()!=='v2')return;const b=e.target.closest('#collabOpen,#flowOpen');if(!b)return;e.preventDefault();e.stopImmediatePropagation();location.href=b.id==='collabOpen'?'/team':'/flow';},true);
  // 답이 끝나면 서버가 원래 세션의 옛 기록을 앞에 합쳐 주는데(실행 중엔 안 합침), 그때 목록이 위로 길어져도 보고 있던 자리가 그대로이게 한다.
  // 바닥에 있었으면 원래 로직(바닥 유지)대로, 아니면 "첫 보이는 메시지"를 기준으로 스크롤을 되돌린다.
  function keepReadingPosition(){
    if(typeof renderMessages!=='function'||renderMessages.__v2)return;
    const orig=renderMessages;
    const wrapped=function(detail,target='#messages',comparison=false){
      const el=document.querySelector(target);let anchor=null;
      if(el){const atBottom=el.scrollHeight-el.scrollTop-el.clientHeight<100;if(!atBottom){const top=el.scrollTop;for(const n of el.children){if(n.offsetTop+n.offsetHeight>top){anchor={key:n.dataset.key,delta:n.offsetTop-top};break;}}}}
      const r=orig.call(this,detail,target,comparison);
      if(anchor&&anchor.key&&el){const n=[...el.children].find(x=>x.dataset.key===anchor.key);if(n){const want=n.offsetTop-anchor.delta;if(Math.abs(el.scrollTop-want)>2)el.scrollTop=want;}}
      return r;};
    wrapped.__v2=true;renderMessages=wrapped;
  }
  document.addEventListener('DOMContentLoaded',()=>{
    try{keepReadingPosition();}catch{}
    enhanceAll();new MutationObserver(enhanceAll).observe(document.body,{childList:true,subtree:true});
    const bar=document.querySelector('.appbar'),conn=document.getElementById('connection');if(!bar)return;
    // 흐름 · 팀 · 대화 · 작업실 — 세 화면을 오가는 메뉴 (대화 화면이 현재)
    const brand=bar.querySelector('.brand');if(brand){brand.innerHTML='<span class="brand-dot"></span><strong>PitCrew</strong>';const nav=document.createElement('span');nav.id='pageNav';nav.innerHTML='<a href="/flow">흐름</a><a href="/team">팀</a><a href="/" class="on">대화</a><a href="/studio">작업실</a>';brand.after(nav);}
    // 딥링크: /?agent=ID&message=MID → 그 대화를 열고 메시지로 스크롤 (흐름·팀 페이지에서 옴)
    const q=new URLSearchParams(location.search),aid=q.get('agent'),mid=q.get('message');
    if(aid){let tries=0;const t=setInterval(()=>{tries++;if(typeof state!=='undefined'&&state.agents?.some(a=>a.id===aid)){clearInterval(t);try{select(aid);}catch{}if(mid){let n=0;const s=setInterval(()=>{n++;const el=document.querySelector(`[data-message="${CSS.escape(mid)}"]`);if(el){clearInterval(s);el.scrollIntoView({block:'center'});el.style.outline='1.5px solid var(--accent)';setTimeout(()=>el.style.outline='',2500);}else if(n>40)clearInterval(s);},250);}history.replaceState(null,'',location.pathname);}else if(tries>80)clearInterval(t);},250);}
    const b=document.createElement('button');b.id='skinToggle';b.type='button';b.className='quiet';
    b.onclick=()=>{const next=current()==='v2'?'classic':'v2';try{localStorage.setItem(KEY,next)}catch{}apply(next);if(next!=='v2')location.reload();};
    bar.insertBefore(b,conn||null);apply(current());
  });
})();
