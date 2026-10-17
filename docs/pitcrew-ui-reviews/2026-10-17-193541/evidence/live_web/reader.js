/* markdown-it 15.0.2: vendored MIT distribution, see markdown-it.LICENSE. */
(function(root){
  'use strict';
  const commonJS=typeof module==='object'&&module.exports;
  const getLibrary=()=>commonJS?require('./markdown-it.min.js'):root.markdownit;
  const escapePlain=value=>value.replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  function createRenderer(MarkdownIt){
  const md=new MarkdownIt({html:false,linkify:true,breaks:true});
  const escape=md.utils.escapeHtml;
  if(typeof md.render!=='function'||typeof escape!=='function')throw new TypeError('Invalid Markdown renderer');
  md.renderer.rules.table_open=()=>'<div class="message-table" tabindex="0" role="region" aria-label="표 · 가로로 스크롤"><table>';
  md.renderer.rules.table_close=()=>'</table></div>';
  const link=md.renderer.rules.link_open||((tokens,i,options,env,self)=>self.renderToken(tokens,i,options));
  md.renderer.rules.link_open=(tokens,i,options,env,self)=>{
    tokens[i].attrSet('target','_blank');tokens[i].attrSet('rel','noopener noreferrer');
    return link(tokens,i,options,env,self);
  };
  // Remote images are links: rendering an AI response must not fetch tracking URLs.
  md.renderer.rules.image=(tokens,i)=>{
    const token=tokens[i],encoded=token.attrGet('src')||'',alt=escape(token.content||'이미지');
    let url=encoded;try{url=decodeURI(encoded);}catch{}  // markdown-it percent-encodes Windows backslashes
    const local=/^(?:[a-zA-Z]:[\\/]|\/(?!\/)|file:|\.{0,2}[\\/]?[\w.-]+[\\/])/.test(url)&&!/^https?:/i.test(url);
    if(local&&/\.(png|jpe?g|gif|webp|bmp)$/i.test(url)){
      const src='/local-image?path='+encodeURIComponent(url.replace(/^file:\/\/\/?/,''));
      return `<button type="button" class="msg-image-btn inline" data-lightbox="${src}" title="${alt}"><img class="msg-image" src="${src}" alt="${alt}" loading="lazy"></button>`;
    }
    return `<a href="${escape(encoded)}" target="_blank" rel="noopener noreferrer">이미지 · ${alt}</a>`;
  };
  md.renderer.rules.fence=(tokens,i)=>{
    const t=tokens[i],language=t.info.trim().split(/\s+/)[0]||'코드';
    return `<section class="code-block"><header><span>${escape(language)}</span><button type="button" data-copy-code aria-label="코드 복사">복사</button></header><pre tabindex="0"><code>${escape(t.content)}</code></pre></section>`;
  };
  return md;
  }
  let renderer,failedLibrary;
  const reader={render(value){
    const text=String(value??'');
    if(!text.trim())return '';
    if(!renderer){
      const Library=getLibrary();
      if(typeof Library==='function'&&Library!==failedLibrary){
        try{renderer=createRenderer(Library);}
        catch(error){failedLibrary=Library;console.warn('[PitCrewReader] Markdown initialization failed; displaying original text.',error);}
      }
    }
    if(renderer)return renderer.render(text);
    return `<div class="reader-fallback"><small>서식을 불러오지 못해 원문으로 표시합니다. 새로고침하면 다시 시도합니다.</small><div class="reader-plain">${escapePlain(text)}</div></div>`;
  }};
  if(typeof module==='object'&&module.exports)module.exports=reader;else root.PitCrewReader=reader;
})(globalThis);
