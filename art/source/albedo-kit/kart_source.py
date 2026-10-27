import bpy,json,hashlib,numpy as np
from pathlib import Path
R=Path('D:/Github/F1Game');W=R/'art/source/albedo-kit'
s=(W/'bake_color.py').read_text();exec(s[s.index('def signature'):s.index('for d in jobs:')])
bpy.ops.wm.open_mainfile(filepath=str(R/'art/source/kart-v6/kart-v6-baked.blend'));sc=bpy.context.scene;fix_paths();obs=[o for o in sc.objects if o.type=='MESH'];target=bpy.data.images.new('material_color',4096,1024,alpha=False);emit(obs,target);sc.render.engine='CYCLES';sc.cycles.samples=1;sc.cycles.use_denoising=False;sc.render.bake.margin=3;sc.render.bake.use_clear=True
bpy.ops.object.select_all(action='DESELECT')
for o in obs:o.select_set(True)
bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join();bpy.ops.object.bake(type='EMIT',uv_layer='BakedUV');target.filepath_raw=str(W/'kart_source_color.png');target.file_format='PNG';target.save()
