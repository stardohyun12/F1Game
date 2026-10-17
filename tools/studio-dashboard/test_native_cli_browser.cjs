const {chromium}=require('C:/Users/rtx40/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert=require('node:assert/strict'),fs=require('node:fs');
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});const results=[];try{
 const page=await browser.newPage({viewport:{width:1600,height:1000}});
 for(const provider of ['codex','claude']){
  await page.goto('http://127.0.0.1:7891/studio');await page.locator('#rooms [data-room=art]').click();await page.locator('#native-cli summary').click();
  await page.locator('#cli-provider').selectOption(provider);await page.locator('#cli-start').click();await page.waitForTimeout(4500);
  const before=await page.evaluate(async()=>{const d=await (await fetch('/api/hub/studio-terminal')).json();return d.sessions.find(s=>s.alive).id;});
  const input=page.locator('#terminal .xterm-helper-textarea');await input.focus();
  if(provider==='codex'){await page.keyboard.insertText('한글 붙여넣기 검증 — 아직 전송하지 않음');await page.waitForTimeout(1000);assert.ok((await page.locator('#terminal').innerText()).includes('한글 붙여넣기'));}
  else{const text=await page.locator('#terminal').innerText();assert.ok(text.includes('external')||text.includes('Claude'));await page.keyboard.press('Enter');await page.waitForTimeout(1200);}
  await page.setViewportSize({width:1350,height:900});await page.reload();await page.locator('#rooms [data-room=art]').click();await page.locator('#native-cli summary').click();await page.waitForTimeout(1000);
  const after=await page.evaluate(async()=>{const d=await (await fetch('/api/hub/studio-terminal')).json();return d.sessions.find(s=>s.alive).id;});assert.equal(before,after);
  assert.ok(await page.locator('#send').isDisabled());await page.screenshot({path:`.ai_logs/pitcrew-studio/native-${provider}-reconnected.png`,fullPage:true});
  await page.locator('#cli-interrupt').click();await page.locator('#cli-close').click();await page.waitForTimeout(500);
  const state=await page.evaluate(async()=>await (await fetch('/api/hub/studio-terminal')).json());assert.equal(state.active,false);
  results.push({provider,sameSession:true,close:true,structuredBlocked:true,koreanPaste:provider==='codex',approvalKeyboard:provider==='claude'});
 }
 fs.writeFileSync('.ai_logs/pitcrew-studio/native-cli-browser.json',JSON.stringify(results,null,2));console.log(results);
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1});
