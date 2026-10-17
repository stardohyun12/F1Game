// Real browser regression; only local browser layout changes, no AI sends or DB edits.
const assert=require('node:assert/strict');
const fs=require('node:fs');
const {chromium}=require(process.env.PLAYWRIGHT_PATH||'playwright');
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 const page=await browser.newPage({viewport:{width:1500,height:1000}});const errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 const ids=()=>page.locator('#workspaceTabs [data-tab-id]').evaluateAll(es=>es.map(e=>e.dataset.tabId));
 const rows=()=>page.locator('#agents .agent-row').evaluateAll(es=>es.map(e=>e.dataset.dragAgent));
 async function dragStart(selector){const r=await page.locator(selector).evaluate(e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height};});const x=r.x+45,y=r.y+r.height/2;await page.mouse.move(x,y);await page.mouse.down();await page.mouse.move(x+20,y+12,{steps:4});await page.locator('.drag-preview').waitFor();return {x,y,r};}
 try{
  await page.goto('http://localhost:7878');
  await page.locator('#agents .agent-row').first().waitFor();
  const cards=await page.locator('#agents .agent-row').count();assert(cards>=3);
  for(let i=0;i<3;i++)await page.locator('#agents .agent-card').nth(i).click();
  await page.waitForFunction(()=>document.querySelectorAll('#workspaceTabs [data-tab-id]').length>=3);
  const before=await ids(),first=before[0],last=before.at(-1);
  const start=await dragStart(`[data-tab-id="${first}"]`);
  const x=start.x+95,y=start.y+30;await page.mouse.move(x,y,{steps:6});
  await page.waitForFunction(({left,top})=>{const r=document.querySelector('.drag-preview')?.getBoundingClientRect();return r&&Math.abs(r.x-left)<2&&Math.abs(r.y-top)<2;},{left:start.r.x+95,top:start.r.y+30});
  const target=await page.locator(`[data-tab-id="${last}"]`).boundingBox();
  await page.mouse.move(target.x+target.width-12,target.y+target.height/2,{steps:10});
  await page.locator('.drag-insertion:not([hidden])').waitFor();
  await page.screenshot({path:'.runtime/drag-tabs-verified.png'});
  await page.mouse.up();await page.locator('.drag-preview').waitFor({state:'detached'});
  assert.deepEqual(await ids(),[...before.slice(1),first]);
  await page.reload();await page.locator('#agents .agent-row').first().waitFor();assert.deepEqual(await ids(),[...before.slice(1),first]);
  // Escape and invalid release preserve order; the following click still works.
  const order=await ids();await dragStart(`[data-tab-id="${order[0]}"]`);await page.keyboard.press('Escape');await page.mouse.up();assert.deepEqual(await ids(),order);assert.equal(await page.locator('.drag-preview').count(),0);
  await dragStart(`[data-tab-id="${order[0]}"]`);await page.mouse.move(20,25);await page.mouse.up();assert.deepEqual(await ids(),order);
  // Vertical list uses the same preview, with independent persistent ordering.
  const original=await rows();const top=original[0],bottom=original[2];
  await dragStart(`#agents .agent-row[data-drag-agent="${top}"]`);
  const dest=await page.locator(`#agents .agent-row[data-drag-agent="${bottom}"]`).boundingBox();
  await page.mouse.move(dest.x+80,dest.y+dest.height-8,{steps:12});await page.locator('.drag-insertion:not([hidden])').waitFor();
  await page.screenshot({path:'.runtime/drag-sidebar-verified.png'});await page.mouse.up();
  assert.deepEqual((await rows()).slice(0,3),[original[1],original[2],top]);
  await page.reload();await page.locator('#agents .agent-row').first().waitFor();assert.deepEqual((await rows()).slice(0,3),[original[1],original[2],top]);
  const close=await ids();await page.locator(`[data-close-tab="${close[0]}"]`).click();assert.equal((await ids()).length,close.length-1);
  // Long tab bars auto-scroll even when the pointer stops near their edge.
  for(let i=0;i<cards;i++)await page.locator('#agents .agent-card').nth(i).click();
  await page.setViewportSize({width:760,height:850});
  await page.locator('#workspaceTabs').evaluate(e=>e.scrollLeft=0);
  const all=await ids();await dragStart(`[data-tab-id="${all[0]}"]`);
  const bar=await page.locator('#workspaceTabs').boundingBox();
  await page.mouse.move(bar.x+bar.width-5,bar.y+bar.height/2);
  await page.waitForFunction(()=>document.querySelector('#workspaceTabs').scrollLeft>20);
  await page.keyboard.press('Escape');await page.mouse.up();assert.deepEqual(await ids(),all);
  await page.setViewportSize({width:1500,height:1000});
  await page.locator('#themeToggle').click();await dragStart(`[data-tab-id="${all[0]}"]`);
  await page.screenshot({path:'.runtime/drag-dark-verified.png'});
  await page.dispatchEvent('body','pointercancel');await page.mouse.up();assert.equal(await page.locator('.drag-preview').count(),0);
  assert.deepEqual(errors,[]);console.log('PASS: pointer offset tracking, insertion line, tab and sidebar reorder, persistence, Escape, invalid drop, close button; no page errors');
 }catch(e){await page.screenshot({path:'.runtime/drag-error.png'});throw e;}finally{await browser.close();}
})();
