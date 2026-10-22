import unittest,sys
from spline_geometry import reconstruct,mesh_components
from convex_clip import intersection,area

class GeometryTests(unittest.TestCase):
 def test_overlap_contact_and_disjoint(self):
  a=[(0,0),(1,0),(1,1),(0,1)]
  self.assertAlmostEqual(area(intersection(a,[(.5,0),(1.5,0),(1.5,1),(.5,1)])),.5)
  self.assertEqual(area(intersection(a,[(1,0),(2,0),(2,1),(1,1)])),0)
  self.assertEqual(area(intersection(a,[(2,0),(3,0),(3,1),(2,1)])),0)
  self.assertAlmostEqual(area(intersection(a,list(reversed(a)))),1)
 def edge(self,lo=0,hi=0):return dict(pts=[dict(x=x,z=0) for x in (0,2,4,6)],y=[0]*4,alley=False,clipA=lo,clipB=hi)
 def quad(self,a,b):return [[a,0,-3],[a,0,3],[b,0,3],[b,0,-3]]
 def test_clipped_and_old_strips(self):
  c,idx,err=reconstruct([self.quad(2,4)],[self.edge(2,2)])
  self.assertEqual(c,[[[2,0,0],[4,0,0]]]);self.assertEqual(idx,[[1,2]])
  self.assertEqual(max(err),0)
  self.assertEqual(len(reconstruct([self.quad(a,a+2) for a in (0,2,4)],[self.edge()])[0][0]),4)
 def test_missing_extra_and_hidden_crosssection(self):
  for q in ([],[self.quad(2,4),self.quad(4,6)]):
   with self.assertRaises(AssertionError):reconstruct(q,[self.edge(2,2)])
  q=[self.quad(0,2),self.quad(2,4),self.quad(4,6)];q[0][2][1]=.1
  with self.assertRaises(AssertionError):reconstruct(q,[self.edge()])
 def test_nearby_disconnected_is_not_joined(self):
  m=dict(vertices=[[0,0,0],[1,0,0],[0,0,1],[1.1,0,0],[2.1,0,0],[1.1,0,1]],triangles=[0,1,2,3,4,5])
  self.assertEqual(mesh_components(m)['components'],2)
  m['vertices'][3]=[1,0,0];self.assertEqual(mesh_components(m)['components'],1)
 def test_overpass_is_not_joined(self):
  m=dict(vertices=[[0,0,0],[1,0,0],[0,0,1],[0,1,0],[1,1,0],[0,1,1]],triangles=[0,1,2,3,4,5])
  self.assertEqual(mesh_components(m)['components'],2)

if __name__=='__main__':unittest.main(testRunner=unittest.TextTestRunner(stream=sys.stdout,verbosity=2))
