const {test}=require('node:test');
const assert=require('node:assert/strict');
const http=require('node:http');
const {request,Poller,retryDelay}=require('./web/connection.js');

test('a stalled request releases the polling guard and reconnects',async()=>{
  let calls=0,scheduled;const errors=[];
  const p=new Poller(()=>request('http://unused',{},20,()=>++calls===1?new Promise(()=>{}):Promise.resolve({ok:true,json:async()=>({ok:true})})),{
    onError:e=>errors.push(e.code),setTimer:(fn,ms)=>{scheduled={fn,ms};return 1;},clearTimer:()=>{}
  });
  const first=p.run();assert.equal(first,p.run());await first;
  assert.deepEqual(errors,['TIMEOUT']);assert.equal(scheduled.ms,1000);
  await scheduled.fn();assert.equal(calls,2);assert.equal(p.failures,0);assert.equal(scheduled.ms,1400);p.stop();
});
test('retries continue after an hour of simulated outage, without overlap',async()=>{
  let scheduled,total=0,online=false,active=0,max=0;
  const p=new Poller(async()=>{active++;max=Math.max(max,active);try{if(!online)throw Error('offline');}finally{active--;}},{setTimer:(fn,ms)=>{scheduled={fn,ms};return 1;},clearTimer:()=>{}});
  await p.run();for(let n=0;n<130;n++){total+=scheduled.ms;await scheduled.fn();}
  assert.ok(total>3600000);assert.equal(scheduled.ms,30000);online=true;await p.run();assert.equal(p.failures,0);assert.equal(max,1);p.stop();
});
test('HTTP error, hung response body and dropped POST do not replay commands',async t=>{
  let posts=0;const server=http.createServer((req,res)=>{
    if(req.method==='POST'){posts++;req.socket.destroy();return;}
    if(req.url==='/hang'){res.writeHead(200,{'Content-Type':'application/json'});res.write('{');return;}
    res.writeHead(503,{'Content-Type':'application/json'});res.end('{"error":"unavailable"}');
  });
  await new Promise(r=>server.listen(0,'127.0.0.1',r));t.after(()=>{server.closeAllConnections();server.close();});const base='http://127.0.0.1:'+server.address().port;
  await assert.rejects(request(base),e=>e.status===503);
  await assert.rejects(request(base+'/hang',{},40),e=>e.code==='TIMEOUT');
  await assert.rejects(request(base,{method:'POST',body:'one'},500));assert.equal(posts,1);
});
test('backoff is bounded and stopping cancels future polls',async()=>{
  assert.deepEqual([1,2,3,7,100].map(retryDelay),[1000,2000,4000,30000,30000]);
  let scheduled=false;const p=new Poller(async()=>{},{setTimer:()=>{scheduled=true;},clearTimer:()=>{}});p.stop();await p.run();assert.equal(scheduled,false);
});
