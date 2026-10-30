"""Supplementary Blender-only frontage, eaves and foundation views. No export/save."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];V=R/'art/_review/2026-10-30-env-first-v2'
m=json.loads((R/'art/export/env-first-v2/manifest.json').read_text(encoding='utf8'))
bpy.ops.wm.open_mainfile(filepath=str(R/'art/models/env_first_v2/env_first.blend'))
sc=bpy.context.scene;cam=sc.camera;sc.render.resolution_x=960;sc.render.resolution_y=720;cam.data.type='ORTHO'
def root_of(o):
    while o.parent:o=o.parent
    return o.name
for b in m['buildings']:
    for o in sc.objects:
        if o.type=='MESH':o.hide_render=root_of(o)!=b['name']
    px,pz=b['pivot_xz'];base=Vector((px-185,-(pz+61),b['y']-25.27))
    h=b['houses'][0];u=Vector((h['u'][0],-h['u'][1],0));n=Vector((h['out'][0],-h['out'][1],0))
    width=max(math.dist(a,c) for a in b['poly'] for c in b['poly']);cam.data.ortho_scale=max(12,width+3)
    target=base+Vector((0,0,3.0));cam.location=target+n*24+u*12+Vector((0,0,11))
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=str(V/f"detail_{b['number']:02d}.png");bpy.ops.render.render(write_still=True)
    if b['number']==6:
        # Contact close-up includes upper door/landing, roof underside and stone foot.
        hh=b['houses'][0];center=Vector((hh['center'][0]-185,-(hh['center'][1]+61),b['y']-25.27))
        target=center+u*(hh['width']/2)+Vector((0,0,2.8));cam.location=target+u*13+n*9+Vector((0,0,5))
        cam.data.ortho_scale=9;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=str(V/'seams_stairs.png');bpy.ops.render.render(write_still=True)
        target=center+Vector((0,0,hh['foundation']+5.8));cam.location=target+n*8-u*7+Vector((0,0,2))
        cam.data.ortho_scale=8;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=str(V/'seams_roof.png');bpy.ops.render.render(write_still=True)
