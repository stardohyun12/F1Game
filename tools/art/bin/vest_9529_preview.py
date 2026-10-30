"""9529 격리 Blender 전후 미리보기. Unity 캡처/판정 대체 아님."""
import bpy, json, math, os, sys
from pathlib import Path
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[3]
RV = ROOT / 'art/_review/2026-10-30-house-vest'
mode = sys.argv[sys.argv.index('--') + 1]
bpy.ops.wm.open_mainfile(filepath=str(RV / mode / 'room.blend'))
sc = bpy.context.scene
for o in sc.objects:
    o.hide_render = o.name not in ('VEST_01', 'CHAIR_01')
sc.render.engine = 'BLENDER_WORKBENCH'
sc.display.shading.light = 'STUDIO'
sc.display.shading.color_type = 'TEXTURE'
sc.display.shading.show_shadows = True
sc.display.shading.show_cavity = False
sc.display.shading.show_specular_highlight = False
sc.display.shading.background_type = 'WORLD'
if sc.world is None: sc.world = bpy.data.worlds.new('ReviewWorld')
sc.world.color = (.16, .18, .20)
sc.view_settings.view_transform = 'Standard'
sc.render.resolution_x = sc.render.resolution_y = 640
sc.render.resolution_percentage = 100
cam = bpy.data.objects.new('ReviewCamera', bpy.data.cameras.new('ReviewCamera'))
sc.collection.objects.link(cam);sc.camera = cam
cam.data.type = 'ORTHO';cam.data.ortho_scale = 1.12
for name, pos, target in [('front',(1.65,.6,1.25),(.75,-.75,.49)),
                          ('back',(-.2,-2.1,1.25),(.75,-.75,.49)),
                          ('top',(.75,-.75,2.8),(.75,-.75,.49))]:
    cam.location = Vector(pos)
    cam.rotation_euler = (Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath = str(RV / mode / (name+'.png'))
    bpy.ops.render.render(write_still=True)
print('PREVIEW_DONE',mode,flush=True)
sys.stdout.flush();os._exit(0)
