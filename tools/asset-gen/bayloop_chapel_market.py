"""0155 landmark repair: bpy-only, photographed atlas, metre UVs. No Unity edits."""
from pathlib import Path
import sys,math,json,hashlib,argparse
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bayloop_buildings as b
import bayloop_buildgen as g
from bayloop_buildgen_textures import select_style
ROOT=b.ROOT; OUT=ROOT/'art/models/bayloop_chapel_market_v01'; REV=ROOT/'art/_review/2026-10-26-landmarks/chapel-market'
box=b.box;face=b.face;beam=b.beam

def move(fn,x=0,y=0,z=0,angle=0):
 n=len(b.V);fn();c=math.cos(angle);s=math.sin(angle)
 for k in range(n,len(b.V)):
  a,d,e=b.V[k];b.V[k]=(x+a*c-d*s,y+a*s+d*c,z+e)

def lathe(x,y,z,rings,tile,segments=12):
 for (zz,r),(tt,rr) in zip(rings,rings[1:]):
  for i in range(segments):
   a=i*math.tau/segments;c=(i+1)*math.tau/segments
   face([(x+r*math.cos(a),y+r*math.sin(a),z+zz),(x+r*math.cos(c),y+r*math.sin(c),z+zz),(x+rr*math.cos(c),y+rr*math.sin(c),z+tt),(x+rr*math.cos(a),y+rr*math.sin(a),z+tt)],tile)

def arch(x,y,z,w,h,tile='trim',depth=.2):
 r=w/2;spring=z+h-r;t=.16
 for xx in [-r-t/2,r+t/2]:box(x+xx,y,z,t,depth,h-r,tile)
 for i in range(8):
  a=i*math.pi/8;c=(i+1)*math.pi/8
  for yy in [y-depth/2,y+depth/2]:
   face([(x+r*math.cos(a),yy,spring+r*math.sin(a)),(x+(r+t)*math.cos(a),yy,spring+(r+t)*math.sin(a)),(x+(r+t)*math.cos(c),yy,spring+(r+t)*math.sin(c)),(x+r*math.cos(c),yy,spring+r*math.sin(c))],tile)
  face([(x+r*math.cos(a),y-depth/2,spring+r*math.sin(a)),(x+r*math.cos(c),y-depth/2,spring+r*math.sin(c)),(x+r*math.cos(c),y+depth/2,spring+r*math.sin(c)),(x+r*math.cos(a),y+depth/2,spring+r*math.sin(a))],'stone')

def chapel():
 b.PART='tower_masonry'
 # Four wall planes, real rectangular window cutouts. Thickness appears in jambs.
 def wall(front=True):
  ops=([(-.47,.47,.1,2.4)] if front else [(-.24,.24,1.15,2.4)])+[(-.24,.24,4.3,5.45),(-.24,.24,7.0,8.15)]
  xs=sorted(set([-1.5,1.5]+[v for op in ops for v in op[:2]]));zs=sorted(set([0,9.0]+[v for op in ops for v in op[2:]]))
  for l,r in zip(xs,xs[1:]):
   for low,hi in zip(zs,zs[1:]):
    if any(l>=a-1e-6 and r<=c+1e-6 and low>=d-1e-6 and hi<=e+1e-6 for a,c,d,e in ops):continue
    face([(l,0,low),(r,0,low),(r,0,hi),(l,0,hi)],'wear_wall' if hi<=.11 else 'plaster')
  for lo,hi in [(4.3,5.45),(7,8.15)]:
   box(0,.14,lo,.48,.06,hi-lo,'glass');box(0,-.055,lo-.08,.65,.25,.09,'stone')
   for xx in [-.26,.26]:box(xx,.06,lo,.07,.22,hi-lo,'trim')
  if front:
   b.arched_window(0,.08,.1,.94,2.3)
   box(0,.04,.1,.83,.025,1.85,'wood_dark');box(.29,-.01,1.0,.045,.07,.17,'metal')
  else:
   box(0,.14,1.15,.48,.06,1.25,'glass');box(0,-.055,1.07,.65,.25,.09,'stone')
 for a,x,y in [(0,0,0),(math.pi/2,1.5,1.5),(math.pi,0,3),(-math.pi/2,-1.5,1.5)]:move(lambda:wall(a==0),x,y,angle=a)
 for zz in [.02,3.1,6.35,8.85]:box(0,1.5,zz,3.16,3.16,.14,'stone')
 # Wear is localized at the plinth and rain-bearing corners.
 for xx in [-1.48,1.48]:box(xx,1.5,.18,.05,2.9,.36,'wear_wall')
 box(0,-.015,.18,2.95,.035,.24,'wear_wall');box(0,3.015,.18,2.95,.035,.24,'wear_wall')
 box(0,-.22,0,1.35,.5,.12,'stone')
 b.PART='open_belfry'
 for xx in [-1.28,1.28]:
  for yy in [.22,2.78]:box(xx,yy,9,.44,.44,2.55,'plaster')
 for a,x,y in [(0,0,.22),(math.pi/2,1.28,1.5),(math.pi,0,2.78),(-math.pi/2,-1.28,1.5)]:move(lambda:arch(0,0,9.05,1.95,2.3),x,y,angle=a)
 # Heavy yoke carried on masonry, visible metal bearing straps and hanging shaft.
 beam((-1.3,1.5,10.95),(1.3,1.5,10.95),.23,'wood_dark')
 for xx in [-1.1,1.1]:box(xx,1.5,10.69,.16,.33,.48,'metal')
 box(0,1.5,10.46,.14,.14,.55,'metal')
 lathe(0,1.5,9.65,[(0,.52),(.12,.47),(.36,.31),(.7,.22),(.82,.09)],'rust')
 lathe(0,1.5,9.65,[(0,.46),(.12,.40),(.36,.24),(.67,.15)],'metal')
 beam((0,1.5,9.49),(0,1.5,10.3),.075,'metal');lathe(0,1.5,9.44,[(0,.09),(.12,.09)],'rust',8)
 box(0,1.5,11.53,3.6,3.6,.20,'stone')
 # Shallow terracotta hipped roof rather than oversized toy cone.
 for p,q in [((-1.8,-.3),(1.8,-.3)),((1.8,-.3),(1.8,3.3)),((1.8,3.3),(-1.8,3.3)),((-1.8,3.3),(-1.8,-.3))]:face([(p[0],p[1],11.73),(q[0],q[1],11.73),(0,1.5,12.35)],'roof_clay')
 beam((0,1.5,12.3),(0,1.5,12.8),.06,'metal');beam((-.15,1.5,12.63),(.15,1.5,12.63),.06,'metal')
 return dict(footprint_m=[3.6,3.6],storeys=1,front_wall_z=0,legacy_name='LANDMARK_종탑',legacy_height_m=12.8)

def fish():
 b.PART='market_posts'
 box(0,.04,0,12,.08,.06,'stone')
 # Plan dimensions reflect the 12x8m legacy canopy, bottom always 0.
 for xx in [-5.55,0,5.55]:
  for yy in [.3,7.7]:
   box(xx,yy,0,.38,.38,.32,'stone');box(xx,yy,.32,.21,.21,2.88,'wood_dark')
   for dx in [-.7,.7]:beam((xx,yy,2.38),(xx+dx,yy,3.14),.1,'wood')
 for yy in [.3,7.7]:beam((-5.8,yy,3.16),(5.8,yy,3.16),.18,'wood_dark')
 for xx in [-5.55,0,5.55]:
  beam((xx,-.3,3.15),(xx,8.3,3.15),.17,'wood_dark')
  beam((xx,-.3,3.15),(xx,4,3.7),.11,'wood_dark');beam((xx,4,3.7),(xx,8.3,3.15),.11,'wood_dark')
  beam((xx,4,3.15),(xx,4,3.7),.1,'wood_dark')
 b.PART='weathered_metal_roof'
 face([(-6,-.3,3.2),(6,-.3,3.2),(6,4,3.75),(-6,4,3.75)],'darkroof')
 face([(-6,4,3.75),(6,4,3.75),(6,8.3,3.2),(-6,8.3,3.2)],'darkroof')
 beam((-6,4,3.76),(6,4,3.76),.09,'metal')
 # Real seams, gutter and repair strip limited to eave edge.
 for xx in [k*.375 for k in range(-15,16)]:
  beam((xx,-.3,3.21),(xx,4,3.76),.04,'metal');beam((xx,4,3.76),(xx,8.3,3.21),.04,'metal')
 for yy in [-.33,8.33]:beam((-6,yy,3.13),(6,yy,3.13),.10,'rust')
 beam((5.83,8.33,.18),(5.83,8.33,3.15),.12,'metal')
 beam((5.83,8.33,.18),(5.83,8.65,.06),.12,'metal')
 b.PART='fish_counter'
 for cx in [-3.7,0,3.7]:
  for xx in [cx-.9,cx+.9]:box(xx,2.3,0,.18,.9,.78,'stone')
  box(cx,2.3,.78,2.5,1.12,.11,'stone');box(cx,2.83,.85,2.5,.08,.12,'stone')
  # Repeated shallow market boxes and visible fish silhouettes.
  for dx in [-.64,.64]:
   x=cx+dx;box(x,2.25,.89,1.04,.73,.05,'cloth')
   for yy in [1.88,2.62]:box(x,yy,.89,1.04,.055,.17,'teal')
   for xx in [x-.52,x+.52]:box(xx,2.25,.89,.055,.73,.17,'teal')
   for k in range(3):
    yy=2.02+k*.20
    face([(x-.34,yy,.98),(x-.05,yy-.075,1.00),(x+.24,yy,1.02),(x-.05,yy+.075,1.00)],'plaster')
    face([(x+.20,yy,1.02),(x+.36,yy-.09,.98),(x+.36,yy+.09,.98)],'plaster')
 # Two large ice troughs show whole fish above the rim, not printed grey triangles.
 b.PART='whole_fish_on_ice'
 def wholefish(x,y,z,length=.95):
  rings=[(-length*.5,.025),(-length*.27,.13),(length*.05,.15),(length*.30,.08),(length*.40,.025)]
  for (xx,r),(xx2,r2) in zip(rings,rings[1:]):
   for k in range(8):
    a=k*math.tau/8;c=(k+1)*math.tau/8
    face([(x+xx,y+r*math.cos(a),z+r*.38*math.sin(a)),(x+xx2,y+r2*math.cos(a),z+r2*.38*math.sin(a)),(x+xx2,y+r2*math.cos(c),z+r2*.38*math.sin(c)),(x+xx,y+r*math.cos(c),z+r*.38*math.sin(c))], 'plaster' if k>=4 else 'rust')
  face([(x+length*.39,y,z),(x+length*.62,y-.12,z+.01),(x+length*.55,y,z-.01),(x+length*.62,y+.12,z+.01)],'rust')
  face([(x-.06,y,z+.055),(x+.20,y,z+.055),(x+.05,y,z+.105)],'rust')
  box(x-length*.32,y-.095,z+.025,.033,.025,.033,'wood_dark')
 for cx in [-3.7,3.7]:
  box(cx,.82,0,2.85,1.0,.64,'plaster')
  box(cx,.82,.64,2.67,.84,.07,'cloth')
  for yy in [.30,1.34]:box(cx,yy,.64,2.85,.08,.17,'plaster')
  for xx in [cx-1.43,cx+1.43]:box(xx,.82,.64,.08,1.12,.17,'plaster')
  for k in range(10):
   xx=cx-1.15+(k%5)*.52;yy=.59+(k//5)*.43
   face([(xx-.13,yy-.13,.73),(xx+.16,yy-.10,.73),(xx+.11,yy+.13,.76),(xx-.07,yy+.10,.88)],'plaster')
  for idx,(dx,dy) in enumerate([(-.77,-.15),(.13,.10),(.78,-.10)]):
   move(lambda:wholefish(0,0,.86,[.75,.65,.8][idx]),cx+dx,.80+dy,angle=[.2,-.35,2.9][idx])
 # Rear service side has an actual cutting counter and stacked insulated boxes.
 for xx in [-3.8,3.8]:
  box(xx,6.7,0,2.7,1.2,.75,'plaster');box(xx,6.7,.75,2.8,1.3,.07,'wood')
  box(xx,6.7,.82,2.55,1.1,.05,'cloth')
  for idx,dx in enumerate([-.8,0,.8]):move(lambda:wholefish(0,0,.94,.72),xx+dx,6.7,angle=[.1,3.3,-.3][idx])
  box(xx,5.6,0,1.1,.6,.4,'teal');box(xx+.55,5.6,.4,1.1,.6,.4,'teal')
 # Suspended silhouette sign: physical fish relief, attached by two straps.
 box(-2.6,-.39,2.14,2.4,.10,.58,'teal')
 for xx in [-3.35,-1.85]:beam((xx,-.39,2.72),(xx,-.3,3.13),.05,'metal')
 face([(-3.48,-.451,2.42),(-3.07,-.451,2.62),(-2.24,-.451,2.55),(-2.02,-.451,2.65),(-2.02,-.451,2.2),(-2.24,-.451,2.31),(-3.07,-.451,2.25)],'plaster')
 box(-3.17,-.47,2.45,.06,.025,.06,'wood_dark')
 # Working counter scale: pan, column, dial, separate from roof structures.
 box(4.4,3.25,.89,.35,.30,.13,'metal');box(4.4,3.25,1.02,.06,.06,.31,'metal')
 box(4.4,3.25,1.33,.37,.08,.32,'cloth');box(4.4,3.23,1.65,.50,.38,.035,'metal')
 for xx in [-4.7,-4.1]:box(xx,5.0,0,.52,.72,.42,'wood')
 return dict(footprint_m=[12,8.6],storeys=1,front_wall_z=0,legacy_name='FishMarketRoof',legacy_height_m=3.6,legacy_pier_note='Old columns extend -3m below local origin; new visible structure starts0. Programmer supplies slope foundations if necessary.')

def main():
 p=argparse.ArgumentParser();p.add_argument('--no-render',action='store_true');p.add_argument('--only');args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
 OUT.mkdir(parents=True,exist_ok=True);REV.mkdir(parents=True,exist_ok=True)
 base=json.loads((ROOT/'art/models/bayloop_variety_v01/manifest-chapel_bell.json').read_text());assets=[];rows=[]
 for name,fn in [('chapel_bell_01',chapel),('FishMarketRoof',fish)]:
  if args.only and args.only!=name:continue
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.scene.unit_settings.system='METRIC'
  for arr in [b.V,b.F,b.UV,b.PARTS]:arr.clear()
  style=select_style('cream','medium','green');b.TEX=OUT/('FishMarketPhotoAtlas.png' if name=='FishMarketRoof' else 'ChapelMarketPhotoAtlas.png');b.TILES.update(style['tiles']);b.TILES['darkroof']=15;mat=b.material();info=fn();tris=sum(len(f)-2 for f in b.F);assert tris<=4000,(name,tris)
  obj=b.mesh(name,mat);bpy.context.view_layer.objects.active=obj;obj.select_set(True)
  paths=[]
  for ext in ['glb','fbx']:
   path=OUT/(name+'.'+ext);paths.append(path)
   if ext=='glb':bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,export_yup=True)
   else:bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='COPY',embed_textures=True)
  vs=[(x,z,-y) for x,y,z in b.V];lo=[min(v[k] for v in vs) for k in range(3)];hi=[max(v[k] for v in vs) for k in range(3)]
  blend=OUT/(name+'.blend');bpy.ops.wm.save_as_mainfile(filepath=str(blend));paths.append(blend)
  (OUT/(name+'.geometry.json')).write_text(json.dumps(dict(vertices_y_up=vs,faces=b.F,uv_per_face=b.UV,part_per_face=b.PARTS)),encoding='utf8')
  urls=base['assets'][0]['source_urls'];assets.append(dict(id=name,type='chapel_bell' if name.startswith('chapel') else 'fish_market',model=b.rel(paths[0]),fbx=b.rel(paths[1]),texture=b.rel(b.TEX),tris=tris,dimensions_m=[hi[k]-lo[k] for k in range(3)],bounds_min=lo,bounds_max=hi,source_urls=urls,**info))
  for path in paths:rows.append(dict(asset_id='0155_'+name+'_'+path.suffix[1:],path=b.rel(path),source_url='; '.join(urls),license='CC0-1.0 photographs; original bpy geometry',accessed='2026-10-26',tool='bayloop_chapel_market.py; bayloop_buildings.py metric UV; existing photo_to_pixel.py CC0 atlas',ai_generated='no',sha256=b.sha(path),notes='Original landmark geometry; 32px/m; shared palette; local splash-band wear; static preview not Unity'))
  if not args.no_render:
   sc=b.stage();sc.cycles.samples=12;h=hi[1];depth=3 if name.startswith('chapel') else 8;target=(0,depth/2,h*.48);scale=26 if name.startswith('chapel') else 18
   views={'front':((14,-24,14),target,scale),'side':((26,depth/2,9),target,scale),'back':((-14,28,14),target,scale),'near':((9,-12,12),(0,1.5,10.35),8) if name.startswith('chapel') else ((13,-15,8),(0,3,1.8),16),'distance':((30,-55,18),target,65)}
   for view,(loc,t,ss) in views.items():b.render(sc,REV/(name+'_'+view+'.png'),loc,t,ss)
  print('LANDMARK_DONE',name,tris,flush=True)
 atlas=OUT/'ChapelMarketPhotoAtlas.png'
 rows.append(dict(asset_id='0155_ChapelMarketPhotoAtlas',path=b.rel(atlas),source_url='; '.join(base['assets'][0]['source_urls']),license='CC0-1.0',accessed='2026-10-26',tool='prepare_atlas.py; photo_to_pixel.py; bayloop_buildgen_textures.py',ai_generated='no',sha256=b.sha(atlas),notes='Existing CC0 photo atlas reused; plaster photo 2m resampled64px, photograph luminance16percent threshold, palette19/25; Point32px/m; no AI'))
 manifest={k:base[k] for k in ['palette','density_px_m','max_tris','source_manifests']};manifest['assets']=assets
 if args.only and (OUT/'manifest.json').exists():
  manifest['assets']=[a for a in json.loads((OUT/'manifest.json').read_text())['assets'] if a['id']!=args.only]+assets
  prior=json.loads((OUT/'provenance-rows.json').read_text());newpaths={r['path'] for r in rows};rows=[r for r in prior if r['path'] not in newpaths]+rows
 if args.only=='FishMarketRoof':
  aa=OUT/'FishMarketPhotoAtlas.png';rows=[r for r in rows if r['path']!=b.rel(aa)];rows.append(dict(asset_id='0155_FishMarketPhotoAtlas',path=b.rel(aa),source_url='; '.join(base['assets'][0]['source_urls']),license='CC0-1.0',accessed='2026-10-26',tool='revise_market.py; photo_to_pixel.py; bayloop_buildgen_textures.py',ai_generated='no',sha256=b.sha(aa),notes='Existing CC0 photograph patterns: roof cell15 palette11/7; insulated box cell0 palette14/19; fish silver cell14 palette18/19. No generated noise;32px/m Point.'))
 (OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8');(OUT/'provenance-rows.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf8')
if __name__=='__main__':main()
