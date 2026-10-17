const {chromium}=require(process.env.PLAYWRIGHT_PATH||'C:/Users/rtx40/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert=require('node:assert/strict'),path=require('node:path'),fs=require('node:fs');
(async()=>{const browser=await chromium.launch({channel:'msedge',headless:true});try{
 const page=await browser.newPage({viewport:{width:1600,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:7891/studio');await page.locator('#rooms [data-room=art]').click();
 const before=await page.locator('#asset-list [data-asset]').count();await page.locator('#image-upload').setInputFiles(path.resolve(__dirname,'../../art/source/coastal-formula/01_front.png'));
 await page.waitForFunction(n=>document.querySelectorAll('#asset-list [data-asset]').length>n,before);await page.locator('#image-controls').waitFor({state:'visible'});await page.locator('#image-purpose').fill('지붕 색상만 검토');
 for(const [k,v] of Object.entries({x:.1,y:.2,w:.3,h:.4}))await page.locator(`[data-region=${k}]`).fill(String(v));
 await page.locator('[data-region=h]').blur();await page.locator('#prompt').fill('이미지를 읽고 지붕의 색감을 제안하세요.');
 await page.locator('#rooms [data-room=feel]').click();await page.locator('#rooms [data-room=art]').click();
 assert.equal(await page.locator('#image-purpose').inputValue(),'지붕 색상만 검토');assert.equal(await page.locator('#prompt').inputValue(),'이미지를 읽고 지붕의 색감을 제안하세요.');
 await page.locator('#send').click();await page.waitForFunction(()=>document.querySelector('#jobs').textContent.includes('실행 종료'));
 await page.locator('#production-map [data-flow-job]').first().waitFor({state:'visible'});
 await page.locator('#deliveries [data-download]').first().waitFor({state:'visible'});
 await page.screenshot({path:path.resolve(__dirname,'../../.ai_logs/pitcrew-studio/workbench-desktop.png'),fullPage:true});
 await page.setViewportSize({width:390,height:844});assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));await page.screenshot({path:path.resolve(__dirname,'../../.ai_logs/pitcrew-studio/workbench-mobile.png'),fullPage:true});
 await page.setViewportSize({width:1600,height:1000});await page.locator('#prompt').fill('FAIL_TEST');await page.locator('#send').click();await page.waitForFunction(()=>document.querySelector('#rooms [data-room=art]').textContent.includes('실행 실패'));await page.locator('#prompt').fill('cancel current trial');await page.locator('#send').click();await page.locator('#run-controls [data-cancel]').first().click();await page.waitForFunction(()=>document.querySelector('#rooms [data-room=art]').textContent.includes('중단됨'));await page.locator('#prompt').fill('offline draft');await page.context().setOffline(true);await page.waitForFunction(()=>document.querySelector('#connection').textContent.includes('연결 끊김'));await page.context().setOffline(false);await page.waitForFunction(()=>document.querySelector('#connection').textContent.includes('연결됨'));assert.equal(await page.locator('#prompt').inputValue(),'offline draft');assert.deepEqual(errors,[]);console.log('Workbench upload, region, room persistence, actual request attachment, views, mobile: PASS (mock provider)');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1});
