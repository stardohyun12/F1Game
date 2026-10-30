"""One-time integration of task 1340 geometry helpers; fails if the expected stage changed."""
from pathlib import Path
root=Path(__file__).resolve().parents[3]
p=root/'F1/Assets/Editor/RoadNetV2.cs'
s=p.read_text(encoding='utf-8')
a=s.index('            var juncs = new List<(int n,')
b=s.index('            // 1차 실행: 높이를 먼저',a)
s=s[:a]+'''            var originalPaths = chains.ToDictionary(c => c, c => c.p.ToList());
            var juncs = V2PrepareJunctions(chains, nP, nodeY, lg);
'''+s[b:]
a=s.index('            foreach (var (n, inc) in juncs)')
b=s.index('            Tk("띠·갈림목 메시");',a)
s=s[:a]+'''            foreach (var junction in juncs)
            {
                int n = junction.n; var inc = junction.inc;
                if (inc.Count == 0) throw new System.InvalidOperationException("Junction has no external road: " + n);
                float ny = nodeY[n] + RoadColLift;
                var surface = V2JunctionSurface(junction, originalPaths, bands, nP);
                V2MakeUnionJunction(v2T, "ROAD_V2j_" + n, surface, ny, inc.OrderByDescending(x => x.c.w).First().c, meshes, topTris, objN++);
                foreach (var contour in surface) {
                    var poly = contour.Select(q => new Vector3((float)q.x, ny, (float)q.y)).ToList();
                    if (Clipper2Lib.Clipper.Area(contour) > 0) { jPolys.Add((poly, ny)); jCen.Add(new Vector3(nP[n].x, ny, nP[n].z)); }
                }
                foreach (var arm in inc) {
                    var band = bands[arm.c]; int m = band.Count;
                    var A = arm.st ? band[0] : band[m - 1]; var B = arm.st ? band[1] : band[m - 2];
                    foreach (var q in new[] { A, B, (A + B) * .5f }) {
                        float distance = surface.Min(contour => Mathf.Abs(V2PolyE(contour.Select(p => new Vector3((float)p.x, ny, (float)p.y)).ToList(), q, out _)));
                        if (distance > .01f) { gapN++; lg.Add($"입구 틈: {n} {arm.c.id} {distance:F3}m"); }
                    }
                }
            }
'''+s[b:]
s=s.replace(' && !(orig - yO > 3f && eO <= sh)', '')
s=s.replace('            int pathN = 0, pathCut = 0;', '            int pathN = 0, pathCut = 0;\n            var draper = new V2Draper(w, m2.triangles);')
a=s.index('                        // 1d: 정점 높이만 재면')
b=s.index('                    }\n                    i = j;',a)
s=s[:a]+'''                        draper.Make(v2T, "ROAD_V2p_" + (string)r["id"] + "_" + piece++, v, mat, meshes); pathN++;
'''+s[b:]
p.write_text(s,encoding='utf-8')
helper='using System.Collections.Generic;\nusing System.Linq;\nusing UnityEngine;\n\npublic static partial class BuildBayLoop\n{\n'
for name in ['roadv2_junctions.cs.txt','roadv2_drape.cs.txt']:
    helper+=(root/'tools/art/bin'/name).read_text(encoding='utf-8')+'\n'
helper+='}\n'
(root/'F1/Assets/Editor/RoadNetV2Geometry.cs').write_text(helper,encoding='utf-8')
