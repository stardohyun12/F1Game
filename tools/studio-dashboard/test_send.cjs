const {test}=require('node:test');
const assert=require('node:assert/strict');
const vm=require('node:vm');
const fs=require('node:fs');
const source=fs.readFileSync(__dirname+'/web/app.js','utf8');
const sendSource=source.slice(source.indexOf('async function send(event)'),source.indexOf('async function loadModels'));
function fixture(){
  let complete,fail;const calls=[];
  const state={projectId:'a',selected:'one',drafts:{one:'first'},refs:{one:[]},capabilities:{simpleGoal:true}};
  const prompt={value:'first'},button={};
  const context=vm.createContext({state,$:s=>s==='#prompt'?prompt:button,agent:id=>id?{id}:null,busy:()=>false,
    refs:()=>state.refs[state.selected]||[],api:(...args)=>{calls.push(args);return new Promise((resolve,reject)=>{complete=resolve;fail=reject;});},
    saveDrafts(){},renderRefs(){},renderHead(){},poll:async()=>{},toast(){}});
  vm.runInContext(sendSource,context);
  return {state,prompt,calls,context,send:()=>vm.runInContext('send({preventDefault(){}})',context),complete:()=>complete(),fail:()=>fail(new Error('goal rejected'))};
}
test('successful send preserves newer typing while request was pending',async()=>{
  const f=fixture(),pending=f.send();f.prompt.value='next draft';f.state.drafts.one='next draft';f.complete();await pending;
  assert.equal(f.prompt.value,'next draft');assert.equal(f.state.drafts.one,'next draft');
});
test('double submit produces one request and preserves other project input',async()=>{
  const f=fixture(),pending=f.send();await f.send();assert.equal(f.calls.length,1);
  f.state.projectId='b';f.state.selected='two';f.prompt.value='other project';f.complete();await pending;
  assert.equal(f.prompt.value,'other project');assert.equal(f.calls[0][2],'a');
});
test('empty workspace cannot submit an orphaned prompt',async()=>{
  const f=fixture();f.state.selected=null;await f.send();assert.equal(f.calls.length,0);
});
test('/goal starts one durable objective without team configuration',async()=>{
  const f=fixture();f.prompt.value='/goal 모바일 읽기 개선';const pending=f.send();await f.send();
  assert.equal(f.calls.length,1);assert.equal(f.calls[0][0],'cycle');
  const data=f.calls[0][1];assert.equal(data.mode,'goal');assert.equal(data.action,'start');
  assert.equal(data.agent,'one');assert.equal(data.goal,'모바일 읽기 개선');assert.equal(data.teams,undefined);
  f.complete();await pending;assert.equal(f.prompt.value,'');
});
test('/goal completion preserves another project draft',async()=>{
  const f=fixture();f.prompt.value='/goal 목표';const pending=f.send();
  f.state.projectId='b';f.state.selected='two';f.prompt.value='다른 초안';f.complete();await pending;
  assert.equal(f.prompt.value,'다른 초안');assert.equal(f.calls[0][2],'a');
});
test('/goal never calls the legacy runtime and preserves the objective',async()=>{
  const f=fixture();f.state.capabilities={};f.prompt.value='/goal 목표';await f.send();
  assert.equal(f.calls.length,0);assert.equal(f.prompt.value,'/goal 목표');
});

test('/goal carries references and attachments and clears them only after success',async()=>{
  const f=fixture(),cleared=[];f.prompt.value='/goal 자료를 반영';f.state.refs.one=[{agent:'ref',message:'message'}];
  f.context.PitCrewAttach={ready:()=>({pending:false,files:[{path:'upload.txt'}]}),clear:id=>cleared.push('attach:'+id)};
  f.context.PitCrewComposer={payload:()=>({mentions:['peer'],mentionFiles:['notes.txt']}),clear:id=>cleared.push('composer:'+id),noteSent(){}};
  const pending=f.send(),data=f.calls[0][1];
  assert.equal(data.refs[0].message,'message');assert.equal(data.attachments[0].path,'upload.txt');
  assert.equal(data.mentions[0],'peer');assert.equal(data.mentionFiles[0],'notes.txt');assert.deepEqual(cleared,[]);
  f.complete();await pending;assert.deepEqual(cleared,['attach:one','composer:one']);assert.equal(f.state.refs.one.length,0);
});

test('/goal rejection preserves the objective and supporting material',async()=>{
  const f=fixture(),cleared=[];f.prompt.value='/goal 원문 유지';f.state.refs.one=[{agent:'ref',message:'message'}];
  f.context.PitCrewAttach={ready:()=>({pending:false,files:[{path:'upload.txt'}]}),clear:()=>cleared.push('attach')};
  f.context.PitCrewComposer={payload:()=>({mentions:['peer']}),clear:()=>cleared.push('composer')};
  const pending=f.send();f.fail();await pending;
  assert.equal(f.prompt.value,'/goal 원문 유지');assert.equal(f.state.refs.one.length,1);assert.deepEqual(cleared,[]);
  assert.equal(f.state.sending.size,0);
});
