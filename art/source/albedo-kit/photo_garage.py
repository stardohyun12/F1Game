from pathlib import Path
R=Path('D:/Github/F1Game');W=R/'art/source/albedo-kit'
s=(R/'art/source/photo-kit/garage_bake.py').read_text()
s=s.replace("W=R/'art/source/photo-kit'","W=R/'art/source/albedo-kit'").replace("W/'source'/(variant+'.png')","R/'art/source/photo-kit/source'/(variant+'.png')").replace("n.new('ShaderNodeBsdfDiffuse')","n.new('ShaderNodeEmission')").replace("sc.cycles.samples=128","sc.cycles.samples=1").replace("type='DIFFUSE'","type='EMIT'")
s=s.replace("bpy.ops.object.bake(type='AO');target.filepath_raw=str(W/('garage_'+variant+'_ao.png'));target.save();",'')
exec(compile(s,str(W/'photo_garage.py'),'exec'))
