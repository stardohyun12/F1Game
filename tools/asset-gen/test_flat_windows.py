"""Run with Python; save durable, synthetic variation-check evidence."""
from pathlib import Path
import json
from PIL import Image
from check_asset import flat_windows

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'art/_review/2026-10-23-cargo-box-v03/validation'
OUT.mkdir(parents=True,exist_ok=True)
results=[]

def test(name,image,regions,expected):
 image.save(OUT/(name+'.png'))
 result=flat_windows(image,regions)
 assert result['pass']==expected,(name,result)
 results.append({'fixture':name,'expected_pass':expected,'observed':result})

face=[{'face':'front','rect_xywh':[2,2,10,10]}]
solid=Image.new('RGB',(16,16),(112,81,46))
test('solid_face_fails',solid,face,False)
striped=solid.copy()
for x in range(2,12,2):
 for y in range(2,12):striped.putpixel((x,y),(150,117,72))
test('two_color_variation_passes',striped,face,True)
# Only the surrounding atlas is varied; it must not rescue a flat face.
isolated=Image.new('RGB',(16,16),(150,117,72))
isolated.paste(solid.crop((2,2,12,12)),(2,2))
test('outside_region_cannot_rescue',isolated,face,False)
# A second face must never contaminate an adjacent 8x8 sampling window.
adjacent=Image.new('RGB',(16,8),(112,81,46))
adjacent.paste(Image.new('RGB',(8,8),(150,117,72)),(8,0))
test('adjacent_faces_are_isolated',adjacent,[{'face':'a','rect_xywh':[0,0,8,8]},{'face':'b','rect_xywh':[8,0,8,8]}],False)
# One contrasting corner makes some windows varied; the other windows fail.
corner=solid.copy();corner.putpixel((2,2),(150,117,72))
test('all_windows_not_only_whole_face',corner,face,False)
assert results[-1]['observed']['flat_window_count']==8
for name,im,regions in [
 ('tiny_region_rejected',solid,[{'face':'tiny','rect_xywh':[0,0,7,8]}]),
 ('out_of_bounds_rejected',solid,[{'face':'bad','rect_xywh':[10,10,8,8]}]),
 ('transparent_main_face_rejected',Image.new('RGBA',(16,16),(112,81,46,0)),face),
 ('no_regions_rejected',solid,[])]:
 try:flat_windows(im,regions)
 except ValueError as error:results.append({'fixture':name,'expected_error':True,'error':str(error)})
 else:raise AssertionError(name)
evidence={'pass':True,'fixture_count':len(results),'results':results}
(OUT/'flat-window-fixtures.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
print(json.dumps({'pass':True,'fixtures':len(results),'evidence':str(OUT/'flat-window-fixtures.json')}))
