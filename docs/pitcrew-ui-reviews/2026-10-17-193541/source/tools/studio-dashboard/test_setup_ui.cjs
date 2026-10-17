const assert=require('node:assert/strict');
const {chromium}=require(process.env.PLAYWRIGHT_PATH||'playwright');
(async()=>{const b=await chromium.launch({channel:'msedge',headless:true});const p=await b.newPage({viewport:{width:1440,height:1000}}),errors=[];p.on('pageerror',e=>errors.push(e.message));try{
 await p.goto('http://localhost:7878');await p.locator('#agents .agent-card').first().waitFor();
 await p.locator('#setupOpen').click();await p.getByRole('dialog',{name:'내 작업 공간 준비하기'}).waitFor();
 for(let i=0;i<18;i++){await p.keyboard.press('Tab');assert.ok(await p.locator('#modal').evaluate(e=>e.contains(document.activeElement)));}
 for(let i=0;i<18;i++){await p.keyboard.press('Shift+Tab');assert.ok(await p.locator('#modal').evaluate(e=>e.contains(document.activeElement)));}
 assert.equal(await p.locator('.setup-steps li').count(),3);assert.ok(await p.locator('.setup-provider').count()>=2);
 await p.screenshot({path:'.runtime/setup-desktop-verified.png'});
 await p.locator('[data-setup-action=project]').click();await p.locator('#projectForm').waitFor();await p.keyboard.press('Escape');
 await p.locator('#setupOpen').click();await p.locator('[data-setup-action=import]').click();await p.locator('#importSearch').waitFor();await p.locator('#cancelImport').click();
 await p.locator('#setupOpen').click();await p.locator('[data-setup-action=team]').click();await p.locator('#teamBoard').waitFor();await p.keyboard.press('Escape');
 await p.setViewportSize({width:390,height:844});await p.locator('#navToggle').click();await p.locator('.sidebar-tools summary').click();await p.locator('#helpOpen').click();
 await p.getByRole('dialog',{name:'내 작업 공간 준비하기'}).waitFor();
 assert.ok(await p.locator('#modal').evaluate(e=>e.scrollWidth<=e.clientWidth+1));
 await p.screenshot({path:'.runtime/setup-mobile-verified.png'});
 await p.keyboard.press('Escape');await p.locator('#modal').waitFor({state:'hidden'});
 assert.deepEqual(errors,[]);console.log('PASS setup: actual readiness, 3 actionable steps, project/import/team routing, accessible dialog, mobile fit and Escape');
}finally{await b.close();}})();
