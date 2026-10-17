/* Local polling resilience. Independently implemented; source comparisons in RESILIENCE.md. */
(function(root){
  'use strict';
  async function request(url,options={},timeout=12000,fetcher=fetch){
    const controller=new AbortController();let timer;
    const deadline=new Promise((_,reject)=>{timer=setTimeout(()=>{
      const error=new Error(options.method==='POST'?'요청 결과를 아직 확인하지 못했습니다. 대화 상태를 확인한 뒤 다시 시도하세요.':'서버 응답이 지연되어 다시 연결합니다.');
      error.code='TIMEOUT';reject(error);controller.abort();
    },timeout);});
    try{return await Promise.race([deadline,(async()=>{
      const response=await fetcher(url,{...options,cache:'no-store',signal:controller.signal});
      let body;try{body=await response.json();}catch{throw new Error('서버 응답을 읽지 못했습니다.');}
      if(!response.ok){const error=new Error(body.error||'요청에 실패했습니다.');error.status=response.status;throw error;}
      return body;
    })()]);}finally{clearTimeout(timer);}
  }
  const retryDelay=failures=>Math.min(30000,1000*2**Math.min(Math.max(0,failures-1),5));
  class Poller{
    constructor(task,{onSuccess=()=>{},onError=()=>{},interval=()=>1400,setTimer=(fn,ms)=>setTimeout(fn,ms),clearTimer=id=>clearTimeout(id)}={}){
      Object.assign(this,{task,onSuccess,onError,interval,setTimer,clearTimer});this.failures=0;this.timer=null;this.pending=null;this.stopped=false;
    }
    run(){
      if(this.stopped)return Promise.resolve();if(this.pending)return this.pending;
      this.clearTimer(this.timer);this.timer=null;
      this.pending=Promise.resolve().then(this.task).then(()=>{this.failures=0;this.onSuccess();},e=>{this.failures++;this.onError(e,retryDelay(this.failures));}).finally(()=>{
        this.pending=null;if(!this.stopped)this.timer=this.setTimer(()=>this.run(),this.failures?retryDelay(this.failures):this.interval());
      });return this.pending;
    }
    stop(){this.stopped=true;this.clearTimer(this.timer);}
  }
  const exported={request,Poller,retryDelay};
  if(typeof module!=='undefined'&&module.exports)module.exports=exported;else root.PitCrewConnection=exported;
})(typeof globalThis!=='undefined'?globalThis:this);
