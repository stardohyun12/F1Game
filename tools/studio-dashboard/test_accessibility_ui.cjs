const {chromium}=require(process.env.PLAYWRIGHT_PATH||'playwright');
const fs=require('fs');
(async()=>{const b=await chromium.launch({channel:'msedge',headless:true});const p=await b.newPage({viewport:{width:1440,height:1000},reducedMotion:'reduce'}),results=[];
async function audit(name){await p.addScriptTag({path:process.env.AXE_PATH||require.resolve('axe-core/axe.min.js')});const r=await p.evaluate(()=>axe.run(document,{runOnly:{type:'tag',values:['wcag2a','wcag2aa','wcag21aa']}}));results.push({name,violations:r.violations});console.log(name,JSON.stringify(r.violations.map(v=>({id:v.id,nodes:v.nodes.map(n=>({target:n.target,summary:n.failureSummary}))}))));}
try{await p.goto('http://localhost:7878');await p.locator('#agents .agent-card').first().waitFor();await audit('workspace');
for(const [name,selector]of [['setup','#setupOpen'],['teams','#manageTeams'],['import','#importChats'],['settings','#settingsOpen']]){await p.locator(selector).click();await p.locator('#modal').waitFor();if(name==='settings')await p.locator('#saveAgent:enabled').waitFor({timeout:130000});await audit(name);await p.keyboard.press('Escape');}
await p.locator('#themeToggle').click();await audit('dark');await p.setViewportSize({width:390,height:844});await audit('mobile');
fs.writeFileSync('.runtime/accessibility-audit.json',JSON.stringify(results,null,2));if(results.some(r=>r.violations.length))throw Error('Accessibility violations; inspect .runtime/accessibility-audit.json');}finally{await b.close();}})();
