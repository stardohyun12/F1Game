// 세션별 토큰 사용량 합산 — Claude(팀장·기획·프로그래밍)와 Codex(아트·QA) 둘 다.
// 사용: node tools/team_usage.js [--since HH:MM | --since YYYY-MM-DDTHH:MM]   (시각은 로컬 시간)
// Claude: %USERPROFILE%/.claude/projects/D--Github-ddakji/*.jsonl  (message.usage)
// Codex : %USERPROFILE%/.codex/sessions/**/*.jsonl  (event_msg token_count.last_token_usage, cwd 가 이 저장소인 것만)
const fs = require('fs'), path = require('path'), os = require('os');
const PROJECT = 'D:' + String.fromCharCode(92) + 'Github' + String.fromCharCode(92) + 'F1Game';
const CLAUDE_DIR = path.join(os.homedir(), '.claude', 'projects', 'D--Github-ddakji');
const CODEX_DIR = path.join(os.homedir(), '.codex', 'sessions');
const arg = process.argv.indexOf('--since');
let since = 0;
if (arg > 0) {
  const v = process.argv[arg + 1];
  since = /^\d\d:\d\d$/.test(v) ? new Date(new Date().toDateString() + ' ' + v).getTime() : new Date(v).getTime();
}
const rows = []; let total = 0;
const push = (backend, name, calls, maxCtx, sum, out, last) => {
  if (!calls) return; total += sum;
  rows.push({ backend, session: name, calls, maxCtxK: Math.round(maxCtx / 1000), totalM: +(sum / 1e6).toFixed(2), outK: Math.round(out / 1000), last: new Date(last).toLocaleTimeString('ko-KR', { hour12: false }) });
};
// Claude
for (const f of fs.existsSync(CLAUDE_DIR) ? fs.readdirSync(CLAUDE_DIR).filter(f => f.endsWith('.jsonl')) : []) {
  let sum = 0, out = 0, calls = 0, last = 0, maxCtx = 0;
  for (const line of fs.readFileSync(path.join(CLAUDE_DIR, f), 'utf8').split('\n')) {
    if (!line) continue; let j; try { j = JSON.parse(line); } catch { continue; }
    const u = j.message && j.message.usage; if (!u) continue;
    const t = j.timestamp ? Date.parse(j.timestamp) : 0; if (t < since) continue;
    const ctx = (u.input_tokens || 0) + (u.cache_read_input_tokens || 0) + (u.cache_creation_input_tokens || 0);
    sum += ctx + (u.output_tokens || 0); out += u.output_tokens || 0; calls++; maxCtx = Math.max(maxCtx, ctx); if (t > last) last = t;
  }
  push('claude', f.slice(0, 8), calls, maxCtx, sum, out, last);
}
// Codex
const walk = d => fs.existsSync(d) ? fs.readdirSync(d, { withFileTypes: true }).flatMap(e => e.isDirectory() ? walk(path.join(d, e.name)) : (e.name.endsWith('.jsonl') ? [path.join(d, e.name)] : [])) : [];
for (const p of walk(CODEX_DIR)) {
  const lines = fs.readFileSync(p, 'utf8').split('\n');
  let meta; try { meta = JSON.parse(lines[0]); } catch { continue; }
  if (!meta || meta.type !== 'session_meta' || !meta.payload || meta.payload.cwd !== PROJECT) continue;
  let sum = 0, out = 0, calls = 0, last = 0, maxCtx = 0;
  for (const line of lines) {
    if (!line.includes('"token_count"')) continue; let j; try { j = JSON.parse(line); } catch { continue; }
    const u = j.payload && j.payload.info && j.payload.info.last_token_usage; if (!u) continue;
    const t = Date.parse(j.timestamp); if (t < since) continue;
    sum += u.total_tokens || 0; out += u.output_tokens || 0; calls++; maxCtx = Math.max(maxCtx, u.input_tokens || 0); if (t > last) last = t;
  }
  push('codex', path.basename(p).slice(28, 36), calls, maxCtx, sum, out, last);
}
rows.sort((a, b) => b.totalM - a.totalM);
console.table(rows);
console.log('합계(M):', (total / 1e6).toFixed(2), since ? ' since ' + new Date(since).toLocaleString('ko-KR') : '');
console.log('읽는 법: totalM = 입력(캐시 포함)+출력 합. maxCtxK = 한 호출 최대 컨텍스트. calls 많고 maxCtxK 크면 그 세션이 새는 곳. Codex 는 캐시 읽기도 input 에 포함.');
