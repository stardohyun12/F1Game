// 조각 15a: measure.json carGraph 요약 출력 (QA·기획이 에디터 없이 확인용). 사용: node tools/town-gen/show_cargraph.js [prefix=s15]
const path = require('path');
const prefix = process.argv[2] || 's15';
const m = require(path.join(__dirname, 'out', 'scene_structure_' + prefix, 'measure.json'));
const g = m.carGraph;
console.log('graph', g.nodeCount, 'nodes', g.edgeCount, 'edges', g.total_m, 'm · components', g.components, JSON.stringify(g.componentLengths_m.slice(0, 6)));
console.log('byKind_m', JSON.stringify(g.byKind_m));
console.log('R6', JSON.stringify(g.R6_notOnMap));
console.log('7b', JSON.stringify(g.entryJoin7b));
for (const r of g.routes) {
  console.log(`${r.pair} snap ${r.snap_m} same=${r.sameComponent} distinct=${r.distinctRoutes} ${JSON.stringify(r.distinctCharacters)}`);
  for (const x of r.routes) console.log(`   ${x.length_m}m ${x.vsRoute1_pct}% overlap ${x.overlapWithRoute1} distinct=${x.distinct} ${x.character} ${JSON.stringify(x.byKind_m)}`);
}
console.log('R5', JSON.stringify(g.R5_coreLoops), 'upperVillage', JSON.stringify(g.upperVillage));
const gr = { ...g.grade }; delete gr.violations; console.log('grade', JSON.stringify(gr), JSON.stringify(g.grade.violations.slice(0, 10)));
const cf = { ...g.upperCutFill }; delete cf.overAt; console.log('upperCutFill', JSON.stringify(cf));
console.log('checks', m.report.checks.map(c => `${c.id}:${c.pass ? 'PASS' : 'FAIL'}`).join(' '));
for (const c of m.report.checks.filter(c => !c.pass)) console.log('  ', c.id, c.detail);
