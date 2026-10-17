// Isolated API fixture: actual UI assets, no live AI sends or production writes.
const assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_PATH||'playwright');
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 const page=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 const agents=[{id:'a',name:'Claude 기획',provider:'claude'},{id:'b',name:'Codex 구현',provider:'codex'}].map(a=>({...a,status:'idle',role:'담당자',peers:[],mode:'read'}));
 let approvals=[],messages=[],sends=0,saves=0,failLinks=true,ready=true;
 const team={id:'team',name:'출시 준비 팀',context:'테스트 목표',members:['a','b']};
 let teams=[team];
 async function audit(){if(!process.env.AXE_PATH)return;await page.addScriptTag({path:process.env.AXE_PATH});const r=await page.evaluate(()=>axe.run(document,{runOnly:{type:'tag',values:['wcag2a','wcag2aa','wcag21aa']}}));assert.deepEqual(r.violations.map(v=>({id:v.id,nodes:v.nodes.map(n=>n.target)})),[]);}
 await page.route('**/api/hub/**',async route=>{
  const u=new URL(route.request().url()),endpoint=u.pathname.split('/').pop();let result={};const data=route.request().postDataJSON();
  if(endpoint==='bootstrap')result={token:'fixture',project:'D:/Fixture',projectId:'default',projects:[{id:'default',name:'Fixture',path:'D:/Fixture',available:true}],agents,providers:[{id:'claude',name:'Claude',ready},{id:'codex',name:'Codex',ready}],teams,runtime:{independent:true},shared:'',approvals,activity:[]};
  else if(endpoint==='detail')result={agent:agents.find(a=>a.id===u.searchParams.get('agent')),messages,children:[]};
  else if(endpoint==='usage')result={accounts:[]};
  else if(endpoint==='send'){sends++;return route.fulfill({status:503,json:{error:'테스트: 연결을 확인할 수 없습니다.'}});}
  else if(endpoint==='team-links'){saves++;if(failLinks)return route.fulfill({status:400,json:{error:'테스트: 저장하지 못했습니다.'}});for(const [id,peers]of Object.entries(data.links))agents.find(a=>a.id===id).peers=peers;result={agents};}
  else if(endpoint==='sources')result=[];
  await route.fulfill({json:result});
 });
 try{
  await page.goto('http://localhost:7878');await page.locator('#agents .agent-card').first().waitFor();
  await page.locator('#manageTeams').click();await page.locator('[data-connect-team]').click();
  await page.locator('[name=teamPeer]').check();await page.locator('#teamReceiver').selectOption('b');await page.locator('[name=teamPeer]').check();
  await page.locator('#teamReceiver').selectOption('a');assert.ok(await page.locator('[name=teamPeer]').isChecked());
  await page.locator('.link-summary summary').click();await page.screenshot({path:'.runtime/team-links-desktop-verified.png'});await audit();
  await page.locator('#saveLinks').click();await page.locator('#formError').filter({hasText:'저장하지 못했습니다'}).waitFor();assert.ok(await page.locator('[name=teamPeer]').isChecked());assert.equal(saves,1);
  failLinks=false;await page.locator('#saveLinks').click();await page.locator('#modal').waitFor({state:'hidden'});assert.deepEqual(agents.map(a=>a.peers),[['b'],['a']]);
  // Draft retained and failed request not replayed by polling/focus.
  await page.locator('#prompt').fill('남아 있어야 하는 입력');await page.locator('#send').click();
  await page.locator('#agentNotice').filter({hasText:'전송 결과를 확인'}).waitFor();assert.equal(await page.locator('#prompt').inputValue(),'남아 있어야 하는 입력');
  await page.locator('[data-notice-action=draft]').click();assert.equal(await page.locator('#prompt').evaluate(e=>e===document.activeElement),true);assert.equal(sends,1);
  await page.screenshot({path:'.runtime/recovery-notice-verified.png'});
  // Reload clears client send error; server interrupted state stays visible.
  agents[0].status='interrupted';agents[0].error='실행 서비스 재시작';await page.reload();await page.locator('#agentNotice').filter({hasText:'작업이 중단'}).waitFor();
  approvals=[{id:'approval',agent:'a',kind:'question',detail:{questions:[{id:'q',question:'어떤 색상으로 만들까요?',options:[]}]}}];agents[0].status='waiting';await page.reload();
  await page.locator('#agentNotice').filter({hasText:'응답할 요청 1건'}).waitFor();await page.locator('[data-notice-action=approval]').click();assert.equal(await page.locator('#approvals input').evaluate(e=>e===document.activeElement),true);
  await page.setViewportSize({width:390,height:844});assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));await page.screenshot({path:'.runtime/approval-mobile-verified.png'});await audit();
  approvals=[];agents[0].status='idle';agents.forEach(a=>a.source='record');await page.reload();await page.locator('#navToggle').click();await page.locator('#manageTeams').click();await page.locator('[data-connect-team]').click();
  await page.locator('.empty-links').filter({hasText:'원래 대화를 이어서'}).waitFor();assert.equal(await page.locator('[data-open-chat]').count(),2);
  await page.keyboard.press('Escape');agents.forEach(a=>{delete a.source;a.status='idle';});ready=false;await page.reload();
  await page.locator('#agentNotice').filter({hasText:'연결 도구 설정이 필요'}).waitFor();await page.locator('[data-notice-action=setup]').click();await page.locator('.setup-provider').filter({hasText:'설치·설정 필요'}).first().waitFor();await page.keyboard.press('Escape');
  ready=true;agents[0].status='running';messages=[{id:'stream',role:'assistant',text:Array.from({length:100},(_,i)=>'문단 '+i+' — 긴 답변을 읽는 중입니다.').join('\n\n'),streaming:true}];await page.reload();
  await page.locator('#messages .message').waitFor();await page.locator('#messages').evaluate(e=>e.scrollTop=100);const scroll=await page.locator('#messages').evaluate(e=>e.scrollTop);
  messages[0].text+='\n\n새 출력 A';await page.evaluate(()=>window.dispatchEvent(new Event('focus')));await page.locator('#messages').filter({hasText:'새 출력 A'}).waitFor();
  assert.ok(Math.abs(await page.locator('#messages').evaluate(e=>e.scrollTop)-scroll)<3,'Reading position should remain stable');
  await page.locator('#messages').evaluate(e=>e.scrollTop=e.scrollHeight);messages[0].text+='\n\n새 출력 B';await page.evaluate(()=>window.dispatchEvent(new Event('focus')));await page.locator('#messages').filter({hasText:'새 출력 B'}).waitFor();
  assert.ok(await page.locator('#messages').evaluate(e=>e.scrollHeight-e.scrollTop-e.clientHeight<10),'Follow output at bottom');
  await page.locator('#compareOpen').click();await page.locator('#modal [data-compare]').first().click();await page.locator('#comparePanel').waitFor();assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));await page.screenshot({path:'.runtime/compare-mobile-verified.png'});await page.locator('#compareClose').click();assert.ok(await page.locator('#compareOpen').evaluate(e=>e===document.activeElement));
  teams=Array.from({length:20},(_,i)=>({...team,id:'team'+i,name:'작업 팀 '+i}));await page.reload();await page.locator('#navToggle').click();await page.locator('#manageTeams').click();assert.equal(await page.locator('.board-lane').count(),20);assert.ok(await page.locator('#modal').evaluate(e=>e.scrollWidth<=e.clientWidth+1));await page.keyboard.press('Escape');teams=[team];
  agents.splice(0);messages=[];team.members=[];await page.reload();await page.locator('#selectedName').filter({hasText:'열린 대화가 없습니다'}).waitFor();
  await page.locator('#messages [data-setup-action=import]').click();await page.locator('#importList').filter({hasText:'일치하는 대화가 없습니다'}).waitFor();await page.keyboard.press('Escape');
  assert.equal(sends,1);assert.deepEqual(errors,[]);console.log('PASS shared-context drafts, atomic save UI/failure recovery, persistent send/interruption/waiting notices, focus, mobile, read-only team guidance; no live mutations');
 }finally{await browser.close();}
})();
