// artlive 1653: 뷰어(127.0.0.1:8770)에서 정비소 근접 4지점 캡처. node docs/qa/tmp/viewer_spots_1653.js <tag>  → docs/qa/evidence/studio-garage/v_<tag>_<spot>.png
// 지점은 블렌더 좌표(가로 x, 깊이 y 앞0→뒤12, 높이 z). glTF 메시 로컬 = (bx, bz, -by) → mesh.localToWorld.
const {spawn}=require('child_process'); const fs=require('fs');
const tag=process.argv[2]||'x'; const OUT='D:/Github/F1Game/docs/qa/evidence/studio-garage/';
const SPOTS={ceil:[[1.0,2.6,1.5],[1.3,7.0,3.4]],peg:[[2.4,4.8,1.75],[4.98,4.8,1.85]],cyl:[[-1.8,3.2,1.3],[-4.2,4.2,0.5]],board:[[-2.4,4.65,1.9],[-4.83,4.65,2.1]]};
const E='C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe';
const p=spawn(E,['--headless=new','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--remote-debugging-port=9334','--user-data-dir='+process.env.LOCALAPPDATA+'/Temp/edgeprof_cdp1653','--window-size=1470,720','about:blank']);
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
(async()=>{
 let tabs; for(let i=0;i<40;i++){ try{ tabs=await (await fetch('http://127.0.0.1:9334/json')).json(); if(tabs.length) break;}catch(e){} await sleep(250);}
 const t=tabs.find(x=>x.type==='page'); const ws=new WebSocket(t.webSocketDebuggerUrl); let id=0; const pend={};
 ws.onmessage=e=>{const m=JSON.parse(e.data); if(m.id&&pend[m.id]){pend[m.id](m);delete pend[m.id];}};
 await new Promise(r=>ws.onopen=r);
 const send=(method,params={})=>new Promise(r=>{const i=++id;pend[i]=r;ws.send(JSON.stringify({id:i,method,params}));});
 const ev=async(x)=>(await send('Runtime.evaluate',{expression:x,awaitPromise:true,returnByValue:true})).result.result.value;
 await send('Page.navigate',{url:'http://127.0.0.1:8770/?b=garage&shot=1&t='+Date.now()});
 for(let i=0;i<160;i++){ if(await ev(`!!(window.__viewer&&__viewer.ready&&__viewer.current==='garage')`)) break; await sleep(250);}
 await ev(`document.getElementById('mWalk').click()`); await sleep(300);
 console.log('lamps', await ev(`(()=>{const a=[];__viewer.root.traverse(o=>{if(/^LAMP_/.test(o.name)||o.isPointLight)a.push(o.name+':'+o.type)});return a.join(',')})()`));
 for(const [k,[c,l]] of Object.entries(SPOTS)){
  const r=await ev(`(()=>{const V=__viewer, m=V.root.getObjectByName('garage_v5_combo')||(()=>{let q;V.root.traverse(o=>{if(!q&&o.isMesh)q=o});return q})();
   const W=(b)=>m.localToWorld(new m.position.constructor(b[0],b[2],-b[1]));
   const cam=V.camera; cam.fov=60; cam.updateProjectionMatrix(); cam.position.copy(W(${JSON.stringify(c)})); cam.lookAt(W(${JSON.stringify(l)})); return cam.position.toArray().map(v=>v.toFixed(2)).join(',')})()`);
  await sleep(600);
  const url=await ev(`document.getElementById('c').toDataURL('image/png')`);
  fs.writeFileSync(OUT+`v_${tag}_${k}.png`,Buffer.from(url.split(',')[1],'base64')); console.log(k,r);
 }
 ws.close(); p.kill(); process.exit(0);
})();
