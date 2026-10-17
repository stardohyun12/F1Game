const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const vm=require('node:vm');

function harness(){
  const storage=new Map(),elements=new Map(),writes=[],created=[];
  function element(){return {innerHTML:'',value:'',classList:{add(){},remove(){},contains(){return false;}},append(){},dataset:{}};}
  const state={projectId:'a',loadedProject:'a',agents:[{id:'one',name:'One',status:'idle'}],selected:'one',teams:[]};
  const context=vm.createContext({state,localStorage:{getItem:k=>storage.get(k)||null,setItem:(k,v)=>{storage.set(k,v);writes.push(k);}},
    document:{addEventListener(){},querySelectorAll:()=>[],createElement(){const e=element();created.push(e);return e;},body:{classList:{remove(){}}}},
    $:key=>{if(!elements.has(key))elements.set(key,element());return elements.get(key);},
    agent:id=>state.agents.find(a=>a.id===id),esc:String,busy:()=>false,toast(){},toastTimer:0,
    setTimeout:()=>1,clearTimeout(){},renderAgents(){},closeModal(){},poll:async()=>{},select(){},api:async()=>({}),Date,console});
  vm.runInContext(fs.readFileSync(__dirname+'/web/organize.js','utf8'),context);
  return {state,storage,writes,created,context,run:code=>vm.runInContext(code,context)};
}
test('project loading does not erase its saved tabs',()=>{
  const h=harness();h.storage.set('pitcrew.layout.b',JSON.stringify({tabs:['saved'],order:['saved']}));
  h.state.projectId='b';h.state.selected=null;h.state.agents=[];
  h.run('renderWorkspaceTabs()');
  assert.deepEqual(JSON.parse(h.storage.get('pitcrew.layout.b')).tabs,['saved']);
});
test('unchanged polling does not repeatedly write layout storage',()=>{
  const h=harness();h.run('renderWorkspaceTabs()');h.writes.length=0;
  for(let i=0;i<30;i++)h.run('renderWorkspaceTabs()');
  assert.equal(h.writes.length,0);
});
test('late team mutation response cannot replace the new project state',async()=>{
  const h=harness(),calls=[];let resolve;
  h.context.api=(path,body,project)=>{calls.push({path,body,project});return new Promise(r=>resolve=r);};
  const pending=h.run("changeTeamMember('one','team')");h.state.projectId='b';
  h.state.teams=[{id:'new-team',name:'New project',members:[]}];
  resolve({id:'team',name:'Old team',members:['one']});
  await Promise.race([pending,new Promise((_,reject)=>setTimeout(()=>reject(Error('Unexpected second request')),100))]);
  assert.equal(calls.length,1);assert.equal(calls[0].project,'a');assert.equal(h.state.teams[0].id,'new-team');
});
test('archive undo targets original project after navigation',async()=>{
  const h=harness(),calls=[];
  h.context.api=async(path,body,project)=>{calls.push({path,body,project});return {};};
  await h.run("archiveConversation('one')");h.state.projectId='b';
  await h.created.find(e=>e.textContent==='되돌리기').onclick();
  assert.equal(calls[1].path,'restore');assert.equal(calls[1].project,'a');
});
