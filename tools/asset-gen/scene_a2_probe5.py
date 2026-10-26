import bpy
bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/models/garage_scene_a2_kit/background_building_04.blend');ob=bpy.data.objects['background_building_04'];im=bpy.data.images.load('D:/Github/F1Game/art/models/garage_scene_a2_kit/background_building_04_raw.png');uv=ob.data.uv_layers['BakedUV'];W,H=im.size;px=im.pixels[:]
for f in sorted(ob.data.polygons,key=lambda p:p.area,reverse=True)[:8]:
 q=sum((uv.data[i].uv for i in f.loop_indices),uv.data[f.loop_start].uv*0)/len(f.loop_indices);i=(int(q.y*H)*W+int(q.x*W))*4;print('SAMPLE',f.area,list(q),px[i:i+4])
print('max',max(px[::4]))
