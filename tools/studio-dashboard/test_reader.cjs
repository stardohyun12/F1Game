const {test}=require('node:test'),assert=require('node:assert/strict');
const {render}=require('./web/reader.js');
const vm=require('node:vm'),fs=require('node:fs');
const readerSource=fs.readFileSync(require.resolve('./web/reader.js'),'utf8');

test('missing or invalid browser library preserves escaped original text and allows later recovery',()=>{
 for(const markdownit of [undefined,{}]){
  const context=vm.createContext({markdownit});vm.runInContext(readerSource,context);
  const text='# 원문\n<img src=x onerror="evil()"> & \'value\'\n![remote](https://example.com/a.png)';
  const html=context.PitCrewReader.render(text);
  assert.ok(html.includes('reader-fallback'));assert.ok(html.includes('# 원문\n'));
  assert.ok(html.includes('&lt;img src=x onerror=&quot;evil()&quot;&gt; &amp; &#39;value&#39;'));
  assert.ok(!/<img|<script|<a\b/.test(html));
  context.markdownit=require('./web/markdown-it.min.js');
  const restored=context.PitCrewReader.render('# 복구');
  assert.ok(restored.includes('<h1>복구</h1>'));assert.ok(!restored.includes('reader-fallback'));
 }
});
test('tables, nested lists, headings and links render semantically',()=>{
 const html=render('# 결과\n\n| 입력 | 결과 |\n|---|---|\n| 1 | 통과 |\n\n- 항목\n  - 하위\n\n[문서](https://example.com)');
 for(const fragment of ['<h1>결과</h1>','<table>','<th>입력</th>','<td>통과</td>','<ul>','rel="noopener noreferrer"'])assert.ok(html.includes(fragment));
 assert.equal((html.match(/<ul>/g)||[]).length,2);
});

test('unconstructable, throwing and malformed libraries fall back once per library and recover when replaced',()=>{
 for(const markdownit of [()=>{},function(){throw Error('fixture');},function(){},function(){this.utils={escapeHtml:x=>x};this.renderer={rules:{}};}]){
  const warnings=[],context=vm.createContext({markdownit,console:{warn:(...args)=>warnings.push(args)}});
  vm.runInContext(readerSource,context);
  for(const empty of ['',null,undefined,' \r\n\t'])assert.equal(context.PitCrewReader.render(empty),'');
  assert.equal(warnings.length,0);
  for(let i=0;i<3;i++){
   const html=context.PitCrewReader.render('<script>test</script>\r\n& text');
   assert.ok(html.includes('reader-fallback'));assert.ok(html.includes('&lt;script&gt;test&lt;/script&gt;\r\n&amp; text'));
   assert.ok(!html.includes('<script>'));
  }
  assert.equal(warnings.length,1);
  context.markdownit=require('./web/markdown-it.min.js');
  assert.ok(context.PitCrewReader.render('# recovered').includes('<h1>recovered</h1>'));
 }
});
test('untrusted HTML and dangerous link protocols cannot execute',()=>{
 const html=render('<script>alert(1)</script>\n\n[bad](javascript:alert(1))\n\n![bad](data:text/html;base64,abcd)\n\n<iframe src=x></iframe>');
 assert.ok(!/<script|<iframe|href="javascript:|src="data:/.test(html));
 assert.ok(html.includes('&lt;script&gt;'));
});
test('fenced code escapes source and info, preserving exact text',()=>{
 const html=render('```js\n<div onclick="evil()">&</div>\n```');
 assert.ok(html.includes('data-copy-code'));assert.ok(html.includes('&lt;div onclick=&quot;evil()&quot;&gt;&amp;&lt;/div&gt;'));
 assert.ok(!html.includes('<div onclick='));
 assert.ok(!render('```<img/onerror=evil()>\nx\n```').includes('<img'));
});
test('images never trigger network requests and incomplete streaming fences render',()=>{
 const html=render('![diagram](https://example.com/image.png)');assert.ok(!html.includes('<img'));assert.ok(html.includes('이미지 · diagram'));
 assert.ok(render('```js\nconst x = 1;').includes('const x = 1;'));
});
