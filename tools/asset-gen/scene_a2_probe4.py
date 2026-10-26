import bpy
for name in ['Large Shed A','Shack A']:
 bpy.ops.wm.open_mainfile(filepath='D:/Github/F1Game/art/thirdparty/chillydurango-shacks-sheds/'+name+'.blend');print(name,[(o.name,o.hide_render,o.hide_viewport,len(o.data.polygons)) for o in bpy.data.objects if o.type=='MESH'])
