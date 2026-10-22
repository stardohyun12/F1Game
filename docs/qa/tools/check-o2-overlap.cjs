// Independent recount of exported per-building observations; does not measure Unity meshes.
const fs = require('node:fs');
const crypto = require('node:crypto');
const input = process.argv[2] || 'tools/town-gen/out/scene_structure_s15a3/o2_scene_overlap.json';
const raw = fs.readFileSync(input);
const data = JSON.parse(raw);
const groups = {
  car: ['Asphalt', 'Alleys', 'TownSheet_Lane', 'PlazaPockets'],
  walk: ['Sidewalk_Curb', 'TownSheet_Walk', 'TownSteps', 'PublicStairs', 'Driveways'],
  shoulder: ['Shoulder']
};
const rows = data.overlaps;
if (!Array.isArray(rows) || rows.length >= 200) throw Error('Missing or potentially truncated observations');
const counts = {}, mismatches = [];
for (const [group, kinds] of Object.entries(groups)) {
  const hits = rows.filter(r => kinds.some(k => (r.overlap_m2[k] || 0) > 0));
  const area = hits.reduce((sum, r) => sum + kinds.reduce((s, k) => s + (r.overlap_m2[k] || 0), 0), 0);
  counts[group] = {buildings: hits.length, area_m2: area};
  for (const k of kinds) {
    const per = rows.map(r => r.overlap_m2[k] || 0);
    const actual = {buildings: per.filter(a => a > 0).length, buildingsGe1m2: per.filter(a => a >= 1).length, area_m2: per.reduce((a,b) => a+b, 0)};
    for (const key of Object.keys(actual)) if (actual[key] !== data[group][k][key]) mismatches.push(`${k}.${key}`);
  }
  for (const key of ['buildings', 'area_m2']) if (counts[group][key] !== data[group][key]) mismatches.push(`${group}.${key}`);
}
const isFail = r => [...groups.car, ...groups.walk].some(k => (r.overlap_m2[k] || 0) > 0);
const failures = rows.filter(isFail);
if (rows.some(r => r.fail !== isFail(r))) mismatches.push('row.fail');
if (data.failBuildings !== failures.length || data.pass !== (failures.length === 0)) mismatches.push('overall');
const regionHits = rows.filter(r => r.bbox[0] <= 2742 && r.bbox[2] >= 2703 && r.bbox[1] <= 2299 && r.bbox[3] >= 2283);
console.log(JSON.stringify({
  input, sha256: crypto.createHash('sha256').update(raw).digest('hex'),
  observationRows: rows.length, counts, failBuildings: failures.length,
  verdict: failures.length ? 'FAIL' : 'PASS', summaryMismatches: mismatches,
  regionOverlapRows: regionHits, failures,
  limitation: 'Recounts exported observations, not independent mesh measurement; 0.5m samples cannot prove exact geometric zero.'
}, null, 2));
process.exitCode = mismatches.length ? 2 : failures.length ? 1 : 0;
