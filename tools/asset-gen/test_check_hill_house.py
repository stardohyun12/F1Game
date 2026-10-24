"""Small independent known-geometry and injected-defect checks for the audit."""
from pathlib import Path
from types import SimpleNamespace
import csv
import io
import json
import struct
import tempfile
import unittest

import numpy as np
from PIL import Image

from check_hill_house import ROOT, inspect, sha256
from photo_to_pixel import load_palette


class AuditChecks(unittest.TestCase):
    def setUp(self):
        review = ROOT / 'art/_review/2026-10-23-hillhouse'
        review.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix='audit-fixture-', dir=review)
        self.addCleanup(self.temp.cleanup)
        location = Path(self.temp.name)
        self.args = SimpleNamespace(
            model=location / 'synthetic.glb', texture=location / 'synthetic.png',
            palette=ROOT / 'art/source/palette/pitcrew_d123_32.json',
            provenance=location / 'synthetic-provenance.csv', tiles=list(range(51, 59)),
            density=32, tolerance=.1, max_tris=2500, pivot_tolerance=.0001,
            dimensions=None, require_centered_bounds=False,
        )

    def fixture(self, scale=(1, 1, 1), uv_scale=(32, 32), uv_start=(194, 66), sampler=9728, outside=False):
        color = tuple(load_palette(self.args.palette)[0]) + (255,)
        image = Image.new('RGBA', (512, 512), color)
        if outside:
            image.putpixel((0, 0), (1, 2, 3, 255))
        image.save(self.args.texture)
        photo = io.BytesIO()
        image.save(photo, format='PNG')
        x, y = uv_start
        sx, sy = uv_scale
        arrays = [
            np.array([[-.5, 0, -.5], [.5, 0, -.5], [-.5, 0, .5]], dtype='<f4').tobytes(),
            (np.array([[x, y], [x + sx, y], [x, y + sy]], dtype='<f4') / 512).tobytes(),
            np.array([0, 1, 2], dtype='<u2').tobytes(), photo.getvalue(),
        ]
        data = bytearray()
        views = []
        for chunk in arrays:
            data.extend(b'\0' * (-len(data) % 4))
            views.append({'buffer': 0, 'byteOffset': len(data), 'byteLength': len(chunk)})
            data.extend(chunk)
        data.extend(b'\0' * (-len(data) % 4))
        gltf = {
            'asset': {'version': '2.0'}, 'scene': 0, 'scenes': [{'nodes': [0]}],
            'nodes': [{'children': [1], 'scale': list(scale)}, {'mesh': 0}],
            'meshes': [{'primitives': [{'attributes': {'POSITION': 0, 'TEXCOORD_0': 1}, 'indices': 2, 'material': 0}]}],
            'materials': [{'pbrMetallicRoughness': {'baseColorTexture': {'index': 0}, 'metallicFactor': 0}}],
            'textures': [{'sampler': 0, 'source': 0}],
            'samplers': [{'magFilter': sampler, 'minFilter': sampler}],
            'images': [{'bufferView': 3, 'mimeType': 'image/png'}],
            'buffers': [{'byteLength': len(data)}], 'bufferViews': views,
            'accessors': [
                {'bufferView': 0, 'componentType': 5126, 'count': 3, 'type': 'VEC3'},
                {'bufferView': 1, 'componentType': 5126, 'count': 3, 'type': 'VEC2'},
                {'bufferView': 2, 'componentType': 5123, 'count': 3, 'type': 'SCALAR'},
            ],
        }
        encoded = json.dumps(gltf).encode()
        encoded += b' ' * (-len(encoded) % 4)
        glb = struct.pack('<4sII', b'glTF', 2, 28 + len(encoded) + len(data))
        glb += struct.pack('<II', len(encoded), 0x4e4f534a) + encoded
        glb += struct.pack('<II', len(data), 0x004e4942) + data
        self.args.model.write_bytes(glb)
        with self.args.provenance.open('w', newline='', encoding='utf-8') as handle:
            fields = ['asset_id', 'path', 'sha256', 'source_url', 'tool', 'license', 'ai_generated']
            writer = csv.DictWriter(handle, fieldnames=fields)
            writer.writeheader()
            writer.writerow(dict(asset_id='synthetic-test-only', path=self.args.texture.relative_to(ROOT).as_posix(), sha256=sha256(self.args.texture), source_url='synthetic-fixture-no-external-source', tool='unit-test', license='test-only', ai_generated='no'))
        return inspect(self.args)

    def test_one_metre_known_triangle_passes(self):
        result = self.fixture()
        self.assertTrue(result['pass'], result['checks'])
        self.assertEqual(result['tris'], 1)
        self.assertEqual(result['density_min'], 32)
        self.assertEqual(result['density_max'], 32)
        self.assertEqual(result['used_tile_triangle_counts'], {51: 1})

    def test_parent_transform_affects_density(self):
        result = self.fixture(scale=(2, 2, 2))
        self.assertFalse(result['checks']['density_both_principal_axes'])
        self.assertEqual(result['density_min'], 16)
        self.assertEqual(result['density_max'], 16)

    def test_anisotropic_uv_fails_despite_good_average(self):
        result = self.fixture(uv_scale=(16, 48))
        self.assertFalse(result['checks']['density_both_principal_axes'])
        self.assertEqual(result['density_min'], 16)
        self.assertEqual(result['density_max'], 48)

    def test_degenerate_uv_fails(self):
        result = self.fixture(uv_scale=(0, 32))
        self.assertFalse(result['checks']['all_triangles_have_valid_world_and_uv_area'])

    def test_legacy_forbidden_tile_fails(self):
        result = self.fixture(uv_start=(130, 66))
        self.assertFalse(result['checks']['all_triangles_inside_one_permitted_atlas_cell'])

    def test_triangle_spanning_two_allowed_cells_fails(self):
        result = self.fixture(uv_start=(240, 66))
        self.assertFalse(result['checks']['all_triangles_inside_one_permitted_atlas_cell'])

    def test_linear_sampler_fails(self):
        result = self.fixture(sampler=9729)
        self.assertFalse(result['checks']['point_sampling'])

    def test_outside_palette_pixel_fails(self):
        result = self.fixture(outside=True)
        self.assertFalse(result['checks']['palette_outside_zero'])
        self.assertEqual(result['outside_color_count'], 1)


if __name__ == '__main__':
    result = unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(AuditChecks))
    report = {
        'pass': result.wasSuccessful(), 'tests_run': result.testsRun,
        'failures': len(result.failures), 'errors': len(result.errors),
        'scope': 'Synthetic known 1-metre triangle, parent-scale transform, anisotropic/degenerate UV, forbidden/spanning tile, Point-vs-linear sampler, outside-palette injection. These are checker tests, not evidence that the house model passes.',
    }
    (ROOT / 'art/_review/2026-10-23-hillhouse/checker-self-test.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    raise SystemExit(0 if result.wasSuccessful() else 1)
