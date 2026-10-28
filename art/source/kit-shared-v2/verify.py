from pathlib import Path
import sys,json,subprocess,platform
import numpy as np
from PIL import Image
import PIL,cv2
R=Path(__file__).resolve().parents[3];W=Path(__file__).parent;E=R/'art/export/kit-shared-v2'
sys.path.insert(0,str(R/'tools/asset-gen'));import delight as d
palette=d.palette(E/'palette.gpl');allowed=set(map(tuple,palette.tolist()));layout=json.loads((E/'layout.json').read_text());jobs=json.loads((W/'jobs.json').read_text());result={}
for sheet in ['wall','trim','floor','roof','decals']:
 a=np.array(Image.open(E/(sheet+'.png')));assert a.shape[:2]==(512,512)
 rgb=a[:,:,:3];mask=a[:,:,3]>0 if a.shape[2]==4 else np.ones(a.shape[:2],bool)
 colors=np.unique(rgb[mask],axis=0);assert all(tuple(c) in allowed for c in colors)
 seams=[]
 if sheet!='decals':
  for part in layout[sheet]:
   x,y,w,h=part['rect_px'];p=rgb[y:y+h,x:x+w];dx=np.abs(p[:,0].astype(int)-p[:,-1].astype(int)).max();dy=np.abs(p[0].astype(int)-p[-1].astype(int)).max()
   assert dx==0,(sheet,part['name'],'U',dx)
   if sheet!='trim':assert dy==0,(sheet,part['name'],'V',dy)
   same=np.zeros(p.shape[:2],bool)
   for yy in [-1,0,1]:
    for xx in [-1,0,1]:
     if xx or yy:same|=np.all(p==np.roll(p,(yy,xx),(0,1)),axis=2)
   isolated=float((~same).mean());assert isolated<=.003,(sheet,part['name'],'isolated texels',isolated)
   seams.append(dict(part=part['name'],edge_U_max=int(dx),edge_V_max=int(dy),isolated_fraction=isolated))
 result[sheet]=dict(colors=len(colors),size=[512,512],sha256=d.sha(E/(sheet+'.png')),parts=seams)
# Import route versus actual public CLI, exact PNG byte comparison.
job=jobs['roof_F3'];before=d.sha(R/job['output']);subprocess.run([sys.executable,str(R/'tools/asset-gen/delight.py'),str(W/'example-job.json'),'--root',str(R)],check=True);after=d.sha(R/job['output']);assert before==after
result['determinism']={'import_vs_CLI_sha256':after,'byte_identical':True}
# Same photographed texture under each material-class target: correct *linear* ordering.
means={}
for material in ['asphalt','brick','concrete','plaster']:
 j=dict(job);j.pop('base_srgb');j['material']=material;j['physical_m']=[2,2]
 a,info=d.process(j,R);means[material]=info['mean_linear_luminance']
assert means['asphalt']<means['brick']<min(means['concrete'],means['plaster'])
result['material_order_linear']=means
bad=dict(job);bad['quad_px']=[[0,0],[10,10],[0,10],[10,0]]
try:d.process(bad,R);raise AssertionError('crossed quad accepted')
except ValueError:pass
result['crossed_quad_rejected']=True
roof=np.array(Image.open(E/'roof.png'))[256:,256:,:3].astype(float)
assert roof[:,:,2].mean()>roof[:,:,0].mean()+35,'F3 blue paint was lost'
wall=np.array(Image.open(E/'wall.png')).astype(float)
grains=[wall[:256,:256].mean(2).ravel(),wall[:256,256:].mean(2).ravel(),wall[256:,:256].mean(2).ravel()]
corr=np.corrcoef(grains);assert corr.min()>.85,'wall variants no longer share grain'
result['wall_grain_correlation']=corr.tolist();result['F3_mean_RGB']=roof.mean((0,1)).tolist()
dec=np.array(Image.open(E/'decals.png'));assert dec.shape[2]==4 and dec[:,:,3].min()==0 and dec[:,:,3].max()==255
result['decal_alpha_range']=[0,255]
result['dependencies']={'python':platform.python_version(),'numpy':np.__version__,'Pillow':PIL.__version__,'OpenCV':cv2.__version__}
(E/'validation.json').write_text(json.dumps(result,indent=2));print('VERIFY PASS: dimensions / palette / tiling / deterministic CLI / material order',flush=True)
