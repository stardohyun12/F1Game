"""Prop-artist trial: softly hanging garments, no stiff cuffs or repeated socks."""
import math
def build(p,b):
 def rope(x):return 1.38+.14*(abs(x)/1.6)**2
 b.PART='rope_mounts'
 for i in range(16):
  x=-1.6+i*.2;b.beam((x,0,rope(x)),(x+.2,0,rope(x+.2)),.009,'metal')
 for x in [-1.6,1.6]:b.box(x,.035,1.43,.065,.08,.12,'metal')
 def cloth(name,x,w,bottom,top,tile,nx=6,nz=5,phase=0):
  # Broad physically curved folds; both sides split for correct smooth normals.
  def pt(i,j,side):
   u=i/nx;t=j/nz;xx=x+(u-.5)*w
   z=bottom(u)*(1-t)+top(u)*t+.018*math.sin(u*math.tau)*math.sin(t*math.pi)
   y=min(.055,w*.11)*math.sin(u*math.tau+phase)*(1-.25*t)+.045*math.sin(t*math.pi)+side*.001
   return xx,y,z
  for side in [-1,1]:
   b.PART=name+('_front' if side<0 else '_back')
   for j in range(nz):
    for i in range(nx):
     quad=[pt(i,j,side),pt(i+1,j,side),pt(i+1,j+1,side),pt(i,j+1,side)]
     for ids in [(0,1,2),(0,2,3)]:
      tri=[quad[k] for k in ids];tri=tri if side<0 else tri[::-1]
      fstart=len(b.F);b.face(tri,tile,(30+int((x+1.6)*32),30))
      # Stronger folds use true 3D metric island axes; intentional shared-photo
      # islands retain density instead of flattening a curved surface into XZ.
 # Shirt body, short sleeves, open V at neck; thin actual fabric surface.
 x=-1.0;z=rope(x)
 cloth('shirt',x,.42,lambda u:.70+.025*math.sin(u*math.pi),lambda u:z-.03-.10*max(0,1-abs(u-.5)*5),'trim',6,4)
 for side in [-1,1]:
  cloth('sleeve',x+side*.29,.16,lambda u:.99+.035*u,lambda u:1.23+.04*u,'trim',2,2,side*.4)
 x=-.27;z=rope(x)
 cloth('towel',x,.53,lambda u:.012*(1-math.cos(u*math.tau)),lambda u:z-.045*math.sin(u*math.pi),'cloth',8,6,.6)
 x=.48;z=rope(x)
 cloth('trouser_waist',x,.42,lambda u:1.11,lambda u:z-.025*math.sin(u*math.pi),'teal',6,2,.1)
 for side in [-1,1]:
  cloth('trouser_leg',x+side*.119,.182,lambda u:.42+.018*u,lambda u:1.12,'teal',3,5,side*.35)
 x=1.14;z=rope(x)
 cloth('small_cloth',x,.32,lambda u:.85+.03*math.sin(u*math.pi),lambda u:z-.04*math.sin(u*math.pi),'rose_wall',5,4,.5)
 b.PART='wooden_pegs'
 for x,w in [(-1,.36),(-.27,.49),(.48,.38),(1.14,.28)]:
  for xx in [x-w/2,x+w/2]:b.box(xx,-.012,rope(x)-.035,.018,.028,.07,'wood_dark')
 return dict(pivot='bottom_center',mount_height_m=2.7,placement_offset_m=0,triangle_budget=1500,design_notes=['Prop-artist: garment-scale primary silhouette before seams. One shirt/towel/trousers/small cloth; no repeated socks.','Broad folds, smooth garment normals, physical32px/m triangle UV islands, intentional shared-photo overlap; no simulation or animation.'],skills=['prop-artist','uv-workflow','lowpoly-style'])
