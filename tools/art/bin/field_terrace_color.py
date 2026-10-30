"""Measure rendered colours (read-only). Usage: python field_terrace_color.py"""
from PIL import Image
import numpy as np, colorsys, json
d='D:/Github/F1Game/art/_review/2026-10-30-field_terrace/'
def load(f): return np.asarray(Image.open(f).convert('RGB')).astype(float)
def med(a,box): x0,y0,x1,y1=box; c=a[y0:y1,x0:x1].reshape(-1,3); return np.median(c,0).round(0).tolist()
def hsv(rgb): h,s,v=colorsys.rgb_to_hsv(*(np.array(rgb)/255)); return [round(h*360),round(s,2),round(v,2)]
out={}
base=load('D:/Github/F1Game/art/_review/2026-10-29-field_patch/map_baseline.png')
out['terrain_brown_patch(barn side) baseline']=med(base,(1085,235,1135,275))
out['terrain_grass_lit baseline']=med(base,(700,420,800,470))
nb=load(d+'new_B_high.png'); out['terrain_brown_patch new_B_high']=med(nb,(1085,235,1135,275))
t=load(d+'dbg_B_top.png')
# soil mask in the top view (brownish: r>g+12, r>b+30, not green crop)
r,g,b=t[...,0],t[...,1],t[...,2]
soil=(r>g+12)&(r>b+30)&(r<230)
out['soil px']=int(soil.sum())
sv=t[soil]; out['soil median top']=np.median(sv,0).round(0).tolist(); out['soil p10']=np.percentile(sv,10,0).round(0).tolist(); out['soil p90']=np.percentile(sv,90,0).round(0).tolist()
lum=sv@np.array([.299,.587,.114]); out['soil lum cv %']=round(float(lum.std()/lum.mean()*100),1)
crop=(g>r+4)&(g>b+8)&(g<110)&(r<90)
cv=t[crop]; out['crop px']=int(crop.sum()); out['crop median']=np.median(cv,0).round(0).tolist(); out['crop hsv']=hsv(np.median(cv,0)); 
gr=load(d+'dbg_B_top.png')[600:700,900:1200].reshape(-1,3); out['grass near field (top) median']=np.median(gr,0).round(0).tolist(); out['grass hsv']=hsv(np.median(gr,0))
out['soil hsv']=hsv(np.median(sv,0))
print(json.dumps(out,ensure_ascii=False,indent=1))
