const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),crypto=require('node:crypto');
const {chromium}=require(process.env.PLAYWRIGHT_PATH||'C:/Users/USER/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const out=path.resolve(__dirname,'..'),ev=path.join(out,'evidence');
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const save=(f,o)=>fs.writeFileSync(path.join(ev,f),JSON.stringify(o,null,2));
const origin='http://127.0.0.1:7878';
(async()=>{
const browser=await chromium.launch({channel:'msedge',headless:true});const result=(process.env.DIAG_ONLY||process.env.LIVE_ONLY)?JSON.parse(fs.readFileSync(path.join(ev,'browser-results.json'),'utf8')):{capturedAt:new Date().toISOString(),browser:browser.version(),screenshots:[],blockedWrites:0,liveUi:{},diagnostics:[]};delete result.error;
let server;
try{
 if(!process.env.DIAG_ONLY){
 const ctx=await browser.newContext({viewport:{width:1440,height:960},acceptDownloads:true});
 await ctx.route('**/api/**',async r=>{
   if(r.request().method()!=='GET'){result.blockedWrites++;return r.abort();}
   if(/\/unity[^/?]*[?/]?/.test(r.request().url()))return r.fulfill({status:503,contentType:'application/json',body:JSON.stringify({error:'조사 중 Unity 조회 차단 (fixture)'})});
   return r.continue();
 });
 const p=await ctx.newPage();await p.goto(origin);await p.locator('#agents .agent-card').first().waitFor();
 const aid=fs.readFileSync(path.join(__dirname,'target-agent.local'),'utf8').trim();
 // Select in this disposable browser only; API writes are blocked.
 const row=p.locator(`[data-agent="${aid}"]`).first();if(await row.count())await row.click();
 await p.waitForTimeout(1500);
 result.liveUi=await p.evaluate(()=>({agentCards:document.querySelectorAll('#agents .agent-card').length,projects:document.querySelector('#projectSelect')?.options.length,statusLabels:[...document.querySelectorAll('.agent-sub')].map(e=>e.textContent.split('·')[0].trim()),messageCount:document.querySelectorAll('#messages article').length,promptVisible:!!document.querySelector('#prompt')?.getBoundingClientRect().height,buttons:[...document.querySelectorAll('button[id]')].map(e=>({id:e.id,label:e.getAttribute('aria-label')}))}));
 const masks=['#projectName','#projectSelect','#teamFilter','#selectedName','.agent-name-text','.sub-task','.avatar','#contextContent','#workspaceTabs','#teamSummary','#teamFilterNote','#messages','#queueBar','#approvals','#references','#attachments','#prompt','#workOverview','#inboxPanel','#cycleBar','#usageOpen'];
 async function shot(name,extra=[]){await p.screenshot({path:path.join(ev,name),mask:[...masks,...extra].map(s=>p.locator(s)),maskColor:'#d4d8df',animations:'disabled'});result.screenshots.push({file:name,kind:'current live UI, privacy masks; data not fabricated'});}
 await shot('01-live-workspace.png',['#messages','#agentNotice']);
 // Narrow original assistant message contains the broken link; hide all other message bodies.
 const link=p.locator('#messages a').filter({hasText:'인계 ZIP'}).first();
 const anyZip=p.locator('#messages a[href*="WORKFLOW_HANDOFF_BUNDLE.zip"]').first();
 const chosen=await anyZip.count()?anyZip:link;
 if(await chosen.count()){
   await chosen.scrollIntoViewIfNeeded();
   result.liveUi.originalLink={href:await chosen.getAttribute('href'),text:await chosen.textContent()};
   // Real link click, no artificial route. Screenshot only error page, never raw private transcript.
   const popped=ctx.waitForEvent('page',{timeout:8000});await chosen.click();
   const error=await popped;await error.waitForLoadState('domcontentloaded');
   result.liveUi.originalLink.clickUrl=error.url();result.liveUi.originalLink.visibleResult=await error.locator('body').innerText();
   await error.screenshot({path:path.join(ev,'02-original-link-error.png')});await error.close();
 }else result.liveUi.originalLink={found:false,reason:'Not in the currently loaded history slice; diagnostic replay below'};
 await p.locator('#artifactsOpen').click();await p.waitForTimeout(1600);
 result.liveUi.results=await p.evaluate(()=>({summary:document.querySelector('.artifact-summary')?.textContent,downloadLinks:document.querySelectorAll('#artifactsPanel a[download]').length,buttons:[...document.querySelectorAll('#artifactsPanel button')].map(e=>e.textContent).filter(t=>/다운로드|마지막 변경|보낸 메시지/.test(t)).slice(0,10)}));
 await shot('03-live-results.png',['#artifactOwner','#artifactsBody','#unityStatus','.workspace-changes','#agentNotice']);
 await p.setViewportSize({width:390,height:844});await shot('04-live-mobile.png',['#artifactOwner','#artifactsBody','#unityStatus','.workspace-changes','#agentNotice']);
 result.liveUi.mobile=await p.evaluate(()=>({width:innerWidth,documentWidth:document.documentElement.scrollWidth,promptRect:(()=>{const r=document.querySelector('#prompt').getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height}})()}));
 await ctx.close();
 }
 if(process.env.LIVE_ONLY)return;
 // A separate, explicitly labelled fixture displays only synthetic data. The real deployed renderer is reused.
 const filename='진단 파일 공백.txt', data=Buffer.from('PitCrew 다운로드 진단\n한글 파일명과 공백 확인\n','utf8');fs.writeFileSync(path.join(ev,filename),data);
 const realPath=path.join(ev,filename).replace(/\\/g,'/');
 const cases=[['windows-root',`[Windows 루트 링크](</${realPath}>)`],['windows-drive',`[Windows 드라이브](<${realPath}>)`],['file-scheme',`[file URL](<file:///${realPath}>)`],['sandbox-scheme','[sandbox URL](sandbox:/mnt/data/probe.txt)'],['control','[정상 진단 다운로드](/download)']];
 server=http.createServer((req,res)=>{
   if(req.url==='/download'){res.writeHead(200,{'Content-Type':'text/plain; charset=utf-8','Content-Disposition':`attachment; filename="diagnostic.txt"; filename*=UTF-8''${encodeURIComponent(filename)}`,'Content-Length':data.length});return res.end(data);}
   if(['/reader.js','/markdown-it.min.js'].includes(req.url)){res.setHeader('Content-Type','text/javascript; charset=utf-8');return res.end(fs.readFileSync(path.join(ev,'live_web',req.url.slice(1))));}
   if(req.url!=='/'){res.writeHead(404,{'Content-Type':'text/plain'});return res.end('not found');}
   res.setHeader('Content-Type','text/html; charset=utf-8');res.end('<!doctype html><meta charset="utf-8"><title>격리 다운로드 진단</title><style>body{font:18px sans-serif;margin:40px;line-height:1.8}section{padding:12px;border-bottom:1px solid #ddd}small{color:#555}</style><h1>격리 진단 — 운영 대화가 아님</h1><p>운영에서 받은 reader.js / markdown-it 사용. 일반 파일 다운로드는 진단용 대조군만 제공.</p><script src="/markdown-it.min.js"></script><script src="/reader.js"></script>'+cases.map(([id,md])=>`<section id="${id}"><strong>${id}</strong><div></div></section>`).join('')+'<script>const cases='+JSON.stringify(cases)+'; for(const [id,md] of cases){document.querySelector("#"+id+" div").innerHTML=PitCrewReader.render(md)};document.querySelector("#windows-root a")?.setAttribute("href",'+JSON.stringify(origin+'/'+realPath)+');</script>');
 });
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const port=server.address().port;
 result.video={recorded:false,reason:'Installed Playwright ffmpeg executable missing; attempt recorded separately. Screenshots provided.'};
 const c=await browser.newContext({viewport:{width:1100,height:750},acceptDownloads:true});
 const d=await c.newPage();await d.goto(`http://127.0.0.1:${port}/`);
 result.diagnostics=await d.evaluate(()=>[...document.querySelectorAll('section')].map(s=>({id:s.id,rendered:s.querySelector('div').innerHTML,href:s.querySelector('a')?.getAttribute('href')||null})));
 await d.screenshot({path:path.join(ev,'05-isolated-link-diagnostic.png')});
 let downloads=0;c.on('page',pg=>pg.on('download',()=>downloads++));d.on('download',()=>downloads++);
 const badPromise=c.waitForEvent('page');await d.locator('#windows-root a').click();const bad=await badPromise;await bad.waitForLoadState('domcontentloaded');
 await bad.screenshot({path:path.join(ev,'06-diagnostic-click-404.png')});
 result.badClick={url:bad.url(),body:await bad.locator('body').innerText(),downloadEvents:downloads};await bad.close();
 const downloadP=d.waitForEvent('download');await d.locator('#control a').click();const download=await downloadP;
 const saved=path.join(ev,'downloaded-diagnostic.txt');await download.saveAs(saved);
 result.controlDownload={scope:'isolated diagnostic only; not a product fix',suggestedFilename:download.suggestedFilename(),originalBytes:data.length,savedBytes:fs.statSync(saved).size,sha256:hash(data),savedSha256:hash(fs.readFileSync(saved)),match:hash(data)===hash(fs.readFileSync(saved)),failure:await download.failure()};
 await d.waitForTimeout(800);await c.close();
}catch(e){result.error={name:e.name,message:e.message};process.exitCode=1;}
finally{save('browser-results.json',result);if(server)await new Promise(r=>server.close(r));await browser.close();}
console.log(JSON.stringify({error:result.error,originalLink:result.liveUi.originalLink,controlDownload:result.controlDownload,screenshots:result.screenshots.length}));
})();
