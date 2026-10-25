"""Synthetic binary fixtures test rejection, not visual approval or FBX parsing."""
import copy, io, json, struct, tempfile, unittest
from pathlib import Path
import numpy as np
from PIL import Image
from check_buildings import inspect_asset, sha, triangle_colors, prop_pivot_checks, triangle_limit

def make_glb(path, png, uv_scale=1, root_x=0, metallic=0, smooth=False, geometry_scale=1, repeats=1):
    blob=bytearray(); views=[]; accessors=[]
    def view(data):
        while len(blob)%4:blob.append(0)
        off=len(blob);blob.extend(data);views.append({'buffer':0,'byteOffset':off,'byteLength':len(data)})
        return len(views)-1
    def acc(array,ctype,kind):
        v=view(array.tobytes());accessors.append({'bufferView':v,'componentType':ctype,'count':len(array),'type':kind})
        return len(accessors)-1
    pos=acc(np.array([[-.5,0,0],[.5,0,0],[-.5,1,0],[.5,1,0]],dtype='<f4')*geometry_scale,5126,'VEC3')
    uv=acc(np.array([[0,0],[1,0],[0,1],[1,1]],dtype='<f4')*uv_scale,5126,'VEC2')
    idx=acc(np.array([0,1,2,2,1,3]*repeats,dtype='<u2'),5123,'SCALAR')
    im=view(png)
    g={'asset':{'version':'2.0'},'buffers':[{'byteLength':len(blob)}],'bufferViews':views,'accessors':accessors,
       'images':[{'bufferView':im,'mimeType':'image/png'}],
       'samplers':[{'magFilter':9729 if smooth else 9728,'minFilter':9728}],
       'textures':[{'source':0,'sampler':0}],
       'materials':[{'pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicFactor':metallic}}],
       'meshes':[{'primitives':[{'attributes':{'POSITION':pos,'TEXCOORD_0':uv},'indices':idx,'material':0}]}],
       'nodes':[{'mesh':0,'translation':[root_x,0,0]}],'scenes':[{'nodes':[0]}],'scene':0}
    j=json.dumps(g).encode();j+=b' '*((-len(j))%4);blob+=b'\0'*((-len(blob))%4)
    path.write_bytes(struct.pack('<4sII',b'glTF',2,28+len(j)+len(blob))+struct.pack('<II',len(j),0x4e4f534a)+j+struct.pack('<II',len(blob),0x004e4942)+blob)

class BuildingAuditTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.root=Path(self.temp.name)
        (self.root/'sources').mkdir();(self.root/'palette.json').write_text(json.dumps([[10,20,30],[100,110,120]]))
        a=np.empty((32,32,3),dtype=np.uint8)
        a[:]=[10,20,30];a[::2,::2]=[100,110,120]
        Image.fromarray(a).save(self.root/'albedo.png');Image.fromarray(a).save(self.root/'sources/photo.png')
        (self.root/'proof.fbx').write_bytes(b'SYNTHETIC EXISTENCE FIXTURE ONLY')
        self.asset={'id':'fixture','type':'shop_house','texture':'albedo.png','model':'proof.glb','fbx':'proof.fbx',
                    'dimensions_m':[1,1,0],'bounds_min':[-.5,0,0],'bounds_max':[.5,1,0],'front_wall_z':0,
                    'source_urls':['https://polyhaven.com/a/fixture']}
        self.manifest={'palette':'palette.json','density_px_m':32,'max_tris':4000,'source_manifests':['source.json']}
        (self.root/'source.json').write_text(json.dumps({'path':'sources/photo.png','url':self.asset['source_urls'][0],
            'download_url':'https://dl.polyhaven.org/file/ph-assets/Textures/photo.png','license':'CC0-1.0',
            'sha256':sha(self.root/'sources/photo.png')}))
        self.export()
    def tearDown(self):self.temp.cleanup()
    def export(self,**kwargs):make_glb(self.root/'proof.glb',(self.root/'albedo.png').read_bytes(),**kwargs)
    def rows(self):
        return [{'path':p,'sha256':sha(self.root/p),'ai_generated':'no','license':'CC0-1.0','tool':'fixture',
                 'source_url':self.asset['source_urls'][0]} for p in ['albedo.png','proof.glb','proof.fbx','sources/photo.png']]
    def inspect(self,rows=None):return inspect_asset(self.asset,self.manifest,self.root,self.rows() if rows is None else rows)
    def test_valid(self):self.assertTrue(self.inspect()['pass'])
    def prop(self,kind='floor_center'):
        self.asset.update(asset_kind='prop',max_tris=800,pivot_kind=kind)
        self.asset.pop('front_wall_z')
    def test_prop_valid_floor_center(self):
        self.prop();self.assertTrue(self.inspect()['pass'])
    def test_prop_valid_floor_back(self):
        self.prop('floor_back_center');self.assertTrue(self.inspect()['pass'])
    def test_prop_rejects_undefined_pivot(self):
        self.prop('arbitrary');self.assertFalse(self.inspect()['checks']['prop_pivot_kind'])
    def test_prop_rejects_missing_budget(self):
        self.prop();self.asset.pop('max_tris')
        self.assertFalse(self.inspect()['checks']['prop_budget_declared'])
    def test_prop_budget_cannot_increase_above_800(self):
        self.prop();self.asset['max_tris']=4000
        self.assertEqual(triangle_limit(self.asset,self.manifest),800)
        self.assertFalse(self.inspect()['checks']['prop_budget_declared'])
    def test_building_per_asset_budget_can_tighten(self):
        self.asset['max_tris']=1;self.assertFalse(self.inspect()['checks']['triangle_budget'])
    def test_prop_back_cannot_extend_behind_wall(self):
        self.prop('floor_back_center')
        self.assertFalse(prop_pivot_checks(self.asset,[-.5,0,-.1],[.5,1,.9])['prop_depth_pivot'])
    def test_prop_floor_center_measures_depth_not_front_wall(self):
        self.prop()
        self.assertTrue(prop_pivot_checks(self.asset,[-.5,0,-.5],[.5,1,.5])['prop_depth_pivot'])
        self.assertFalse(prop_pivot_checks(self.asset,[-.5,0,0],[.5,1,1])['prop_depth_pivot'])
    def test_prop_center_measures_width(self):
        self.prop()
        self.assertFalse(prop_pivot_checks(self.asset,[-.4,0,-.5],[.6,1,.5])['prop_x_center_zero'])
    def test_prop_provenance_stays_required(self):
        self.prop();self.assertFalse(self.inspect([])['checks']['provenance_files'])
    def test_prop_actual_over_800_tris_fails(self):
        self.prop();self.export(repeats=401)
        self.assertFalse(self.inspect()['checks']['triangle_budget'])
    def tiny_prop(self):
        self.prop();self.export(geometry_scale=.1,uv_scale=.1)
        self.asset.update(dimensions_m=[.1,.1,0],bounds_min=[-.05,0,0],bounds_max=[.05,.1,0])
    def test_tiny_prop_checks_density_and_sampled_colors(self):
        self.tiny_prop();report=self.inspect()
        self.assertEqual(report['major_triangles'],0);self.assertTrue(report['pass'])
    def test_tiny_uniform_prop_rejected(self):
        Image.new('RGB',(32,32),(10,20,30)).save(self.root/'albedo.png')
        self.tiny_prop();self.assertFalse(self.inspect()['checks']['prop_sampled_surface_variation'])
    def test_wrong_density(self):
        self.export(uv_scale=.5);self.assertFalse(self.inspect()['checks']['density_both_principal_axes'])
    def test_wrong_root(self):
        self.export(root_x=.1);self.assertFalse(self.inspect()['checks']['root_origin'])
    def test_flat_major_surface(self):
        Image.new('RGB',(32,32),(10,20,30)).save(self.root/'albedo.png');self.export()
        self.assertFalse(self.inspect()['checks']['main_triangle_variation'])
    def test_outside_palette(self):
        im=Image.open(self.root/'albedo.png');im.putpixel((0,0),(255,0,0));im.save(self.root/'albedo.png');self.export()
        self.assertFalse(self.inspect()['checks']['palette_outside_zero'])
    def test_linear_sampler(self):
        self.export(smooth=True);self.assertFalse(self.inspect()['checks']['material_checks'])
    def test_metal(self):
        self.export(metallic=1);self.assertFalse(self.inspect()['checks']['material_checks'])
    def test_embedded_mismatch(self):
        im=Image.open(self.root/'albedo.png');im.putpixel((0,0),(10,20,30));im.save(self.root/'albedo.png')
        self.assertFalse(self.inspect()['checks']['material_checks'])
    def test_stale_provenance(self):
        rows=self.rows();rows[0]['sha256']='0'*64
        self.assertFalse(self.inspect(rows)['checks']['provenance_files'])
    def test_source_missing(self):
        self.assertFalse(self.inspect(self.rows()[:-1])['checks']['provenance_photos'])
    def test_processed_tile_cannot_count_as_photo(self):
        # A processed image can have a correct ledger hash, but cannot replace
        # the exact raw-photo path identified by download metadata.
        rows=self.rows();rows[-1]['path']='albedo.png';rows[-1]['tool']='photo_to_pixel.py'
        self.assertFalse(self.inspect(rows)['checks']['provenance_photos'])
    def test_pipeline_name_does_not_invalidate_raw_bytes(self):
        rows=self.rows();rows[-1]['tool']='photo_to_pixel.py; download ledger generator'
        self.assertTrue(self.inspect(rows)['checks']['provenance_photos'])
    def test_source_metadata_missing(self):
        self.manifest['source_manifests']=[]
        self.assertFalse(self.inspect()['checks']['provenance_photos'])
    def test_download_metadata_hash_mismatch(self):
        record=json.loads((self.root/'source.json').read_text());record['sha256']='0'*64
        (self.root/'source.json').write_text(json.dumps(record))
        self.assertFalse(self.inspect()['checks']['provenance_photos'])
    def test_duplicate_provenance(self):
        rows=self.rows();rows.append(rows[0].copy());self.assertFalse(self.inspect(rows)['checks']['provenance_files'])
    def test_bounding_box_cannot_supply_colors(self):
        px=np.zeros((16,16,4),dtype=np.uint8);px[12:,12:,:3]=255
        colors,_,_=triangle_colors(px,np.array([[0.,0.],[16.,0.],[0.,16.]]))
        self.assertEqual(colors,1)

if __name__=='__main__':unittest.main()
