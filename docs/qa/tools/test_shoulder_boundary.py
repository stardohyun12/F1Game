"""Offline geometry regression tests; no Unity editor or scene writes."""
import json
import math
import random
import sys
import unittest
from pathlib import Path
from shoulder_boundary import outer_edges


def mesh(quads):
    v=[];tri=[]
    for q in quads:
        n=len(v);v.extend(q);tri.extend([n,n+1,n+2,n,n+2,n+3])
    return {'vertices':v,'triangles':tri}


def fixture(subdivisions, curved=False):
    def p(x,z):
        # Shared contact geometry is linear per road segment, as in exported road meshes.
        if curved:
            return [30+x*math.cos(.4)-z*math.sin(.4),2+.1*z,40+x*math.sin(.4)+z*math.cos(.4)]
        return [x,.1*z,z]
    roads=mesh([[p(-4,0),p(4,0),p(4,8),p(-4,8)]])
    shoulder=[]
    for side in [-1,1]:
        for i in range(subdivisions):
            a=8*i/subdivisions;b=8*(i+1)/subdivisions
            shoulder.append([p(side*4,a),p(side*6,a),p(side*6,b),p(side*4,b)])
    # A nearby unrelated road defeats naive "nearest centerline" selection, but does not touch.
    neighbor=mesh([[p(6.2,0),p(7,0),p(7,8),p(6.2,8)]])
    return mesh(shoulder),[roads,neighbor],p


def shuffled(m):
    r=random.Random(71);order=list(range(len(m['vertices'])));r.shuffle(order)
    inv={old:new for new,old in enumerate(order)}
    tris=[m['triangles'][i:i+3] for i in range(0,len(m['triangles']),3)];r.shuffle(tris)
    for t in tris:
        if r.random()<.5:t.reverse()
    return {'vertices':[m['vertices'][i] for i in order], 'triangles':[inv[i] for t in tris for i in t]}


class BoundaryTests(unittest.TestCase):
    def test_subdivisions_and_nearby_road(self):
        for n in [1,4,8,16]:
            for rotated in [False,True]:
                with self.subTest(n=n,rotated=rotated):
                    sh,roads,p=fixture(n,rotated);edges,stats,amb=outer_edges(sh,roads)
                    self.assertEqual(len(edges),2*n);self.assertFalse(amb)
                    self.assertAlmostEqual(sum(math.dist(a,b) for a,b in edges),2*math.sqrt(8**2+.8**2),places=5)
                    allowed=[p(side*6,8*i/n) for side in [-1,1] for i in range(n+1)]
                    self.assertTrue(all(any(math.dist(v,q)<1e-6 for q in allowed) for e in edges for v in e))

    def test_vertex_triangle_order_and_winding(self):
        sh,roads,p=fixture(8,True)
        expected,stats,amb=outer_edges(sh,roads)
        actual,new_stats,new_amb=outer_edges(shuffled(sh),[shuffled(r) for r in roads])
        self.assertEqual(expected,actual);self.assertEqual(stats,new_stats);self.assertEqual(amb,new_amb)

    def test_duplicate_backfaces(self):
        sh,roads,p=fixture(4)
        expected,_,_=outer_edges(sh,roads)
        sh['triangles']+=list(reversed(sh['triangles']))
        actual,_,_=outer_edges(sh,roads)
        self.assertEqual(expected,actual)

    def test_saved_scene_array_permutation(self):
        path=Path(__file__).resolve().parents[1]/'evidence/spline-scene-geometry.json'
        data=json.loads(path.read_text(encoding='utf-8'));m={x['name']:x for x in data['meshes']}
        roads=[m['Sidewalk_Curb'],m['Alleys']]
        expected,stats,amb=outer_edges(m['Shoulder'],roads)
        actual,new_stats,new_amb=outer_edges(shuffled(m['Shoulder']),roads)
        self.assertEqual(expected,actual);self.assertEqual(stats,new_stats);self.assertEqual(amb,new_amb)
        self.assertFalse(amb);self.assertGreater(stats['outer_edges'],0)


if __name__=='__main__':
    unittest.main(testRunner=unittest.TextTestRunner(stream=sys.stdout,verbosity=2))
