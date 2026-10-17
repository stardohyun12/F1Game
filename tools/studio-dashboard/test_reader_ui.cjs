const assert=require('node:assert/strict'),fs=require('node:fs');
const {chromium}=require(process.env.PLAYWRIGHT_PATH||'playwright');
(async()=>{
 const b=await chromium.launch({channel:'msedge',headless:true});
 const context=await b.newContext({viewport:{width:1440,height:1000},permissions:['clipboard-read','clipboard-write']});
 const p=await context.newPage(),errors=[];p.on('pageerror',e=>errors.push(e.message));
 try{
  const d=JSON.parse(fs.readFileSync('.runtime/team-flow-current.json','utf8'));
  await p.goto('http://localhost:7878');await p.locator(`#projectSelect option[value="${d.projectId}"]`).waitFor({state:'attached'});
  await p.locator('#projectSelect').selectOption(d.projectId);await p.locator('#projectName').filter({hasText:d.tag}).waitFor();
  await p.locator('#agents .agent-card').filter({hasText:'Claude · 기획과 검토'}).click();
  await p.locator('#messages table').last().waitFor();assert.ok(await p.locator('#messages table tbody tr').count()>=7);
  const code=p.locator('#messages .code-block').last();const original=await code.locator('code').textContent();
  await code.locator('[data-copy-code]').click();assert.equal((await p.evaluate(()=>navigator.clipboard.readText())).replace(/\r\n/g,'\n'),original);
  assert.equal(await code.locator('button').innerText(),'복사됨');
  await p.locator('#messages table').last().scrollIntoViewIfNeeded();await p.screenshot({path:'.runtime/reader-desktop-verified.png'});
  await p.setViewportSize({width:390,height:844});await p.locator('#messages table').last().scrollIntoViewIfNeeded();
  assert.ok(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  await p.screenshot({path:'.runtime/reader-mobile-verified.png'});
  await p.locator('#themeToggle').click();await p.screenshot({path:'.runtime/reader-dark-verified.png'});
  assert.deepEqual(errors,[]);console.log('PASS real history: semantic tables, exact code clipboard, 390px no page overflow, light/dark, no browser errors');
 }finally{await b.close();}
})();
