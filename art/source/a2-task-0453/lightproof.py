from pathlib import Path
import bpy,json
R=Path('D:/Github/F1Game');W=R/'art/source/a2-task-0453';O=R/'art/models/garage_scene_a2_kit'
bpy.ops.wm.open_mainfile(filepath=str(O/'background_building_05_unlit.blend'));sc=bpy.context.scene
sc.render.filepath=str(W/'unlit-before.png');bpy.ops.render.render(write_still=True)
bpy.ops.object.light_add(type='SUN');bpy.context.object.data.energy=100000;bpy.context.object.data.color=(1,0,0)
sc.render.filepath=str(W/'unlit-redlight.png');bpy.ops.render.render(write_still=True)
# Raw render pixels comparison in Blender, no external package dependence.
a=bpy.data.images.load(str(W/'unlit-before.png'));b=bpy.data.images.load(str(W/'unlit-redlight.png'));aa=list(a.pixels);bb=list(b.pixels);delta=max(abs(x-y) for x,y in zip(aa,bb));assert delta==0,delta
(W/'unlit-proof.json').write_text(json.dumps(dict(test='add red sun energy100000',asset='background_building_05',max_pixel_difference=delta,passed=True),indent=2));print('UNLIT PROOF PASS',flush=True)
