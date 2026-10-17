"""Protocol-shaped local events, no provider calls. Verify bytes reach the artifact index."""
import base64
import tempfile
import threading
import concurrent.futures
import unittest
from pathlib import Path
from unittest.mock import patch
import hub
from test_codex_events import fake_rpc
from test_timeline import PNG


class McpImageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.h = hub.Hub(self.temp.name)
        self.h.root = Path(self.temp.name)
        self.media = patch.object(hub, 'MEDIA', Path(self.temp.name) / 'media')
        self.media.start()

    def tearDown(self):
        self.media.stop()
        self.h.close()
        self.h.store.db.close()
        self.temp.cleanup()

    def test_mcp_and_anthropic_image_bytes_and_text(self):
        for block in [{'type': 'image', 'data': PNG, 'mimeType': 'image/png'},
                      {'type': 'image', 'source': {'type': 'base64', 'data': PNG, 'media_type': 'image/png'}}]:
            with self.subTest(shape=list(block)):
                text, images = hub.tool_output([block, {'type': 'text', 'text': 'caption'}])
                self.assertEqual(text, 'caption')
                self.assertEqual(len(images), 1)
                self.assertEqual((hub.MEDIA / images[0]).read_bytes(), base64.b64decode(PNG))

    def test_invalid_image_is_reported_without_base64_or_dropping_caption(self):
        for data, mime in [('not base64!', 'image/png'), (PNG, 'image/svg+xml'), (PNG, 'image/bmp'), (123, 'image/png')]:
            with self.subTest(mime=mime, data_type=type(data).__name__):
                text, images = hub.tool_output([{'type': 'image', 'data': data, 'mimeType': mime}, {'type': 'text', 'text': 'caption'}])
                self.assertEqual(images, [])
                self.assertIn('이미지 저장 실패', text)
                self.assertIn('caption', text)
                self.assertNotIn(PNG, text)

    def test_oversize_image_is_rejected_before_decode_or_write(self):
        with patch.object(base64, 'b64decode', side_effect=AssertionError('must not decode')):
            text, images = hub.tool_output([{'type': 'image', 'data': 'A' * (40 * 1024 * 1024 + 1), 'mimeType': 'image/png'}])
        self.assertEqual(images, [])
        self.assertIn('이미지 저장 실패', text)
        self.assertFalse(hub.MEDIA.exists())

    def test_same_image_is_reused_and_failure_reasons_are_distinct(self):
        first = hub.save_image(PNG)
        second = hub.save_image(PNG)
        self.assertEqual(first, second)
        self.assertEqual(len(list(hub.MEDIA.iterdir())), 1)
        messages = []
        for data, mime in [('!', 'image/png'), (PNG, 'image/svg+xml'), ('A' * (40 * 1024 * 1024 + 1), 'image/png')]:
            text, images = hub.tool_output([{'type': 'image', 'data': data, 'mimeType': mime}])
            messages.append(text)
            self.assertEqual(images, [])
        self.assertEqual(len(set(messages)), 3)
        with patch.object(hub, 'save_image', side_effect=OSError('private path')):
            text, images = hub.tool_output([{'type': 'image', 'data': PNG, 'mimeType': 'image/png'}])
        self.assertIn('파일', text)
        self.assertNotIn('private path', text)
        self.assertEqual(images, [])

    def test_concurrent_identical_images_share_one_complete_file(self):
        data = base64.b64decode(PNG) * 12000
        encoded = base64.b64encode(data).decode()
        gate = threading.Barrier(8)
        def write(_):
            gate.wait(timeout=10)
            return hub.save_image(encoded)
        with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
            names = list(pool.map(write, range(8)))
        self.assertEqual(len(set(names)), 1)
        self.assertEqual([p.name for p in hub.MEDIA.iterdir()], [names[0]])
        self.assertEqual((hub.MEDIA / names[0]).read_bytes(), data)

    def test_codex_images_only_and_structured_result_reach_artifacts(self):
        for structured in (None, {'scene': 'Main'}):
            a = self.h.store.create(name='Codex image', provider='codex')
            item = {'id': 'capture', 'type': 'mcpToolCall', 'server': 'unity', 'tool': 'capture', 'status': 'completed',
                    'arguments': {}, 'result': {'content': [{'type': 'image', 'data': PNG, 'mimeType': 'image/png'}], 'structuredContent': structured}}
            run = hub.Run(self.h, a, {}, 'test')
            with patch.object(hub, 'Rpc', fake_rpc([{'method': 'item/completed', 'params': {'item': item}}])), patch.object(self.h, 'after_run'):
                run.execute()
            m = next(m for m in self.h.store.messages(a['id']) if m['role'] == 'tool')
            self.assertEqual(len(m.get('images', [])), 1)
            self.assertTrue(m['output'])
            self.assertNotIn(PNG, hub.dumps(m))
            self.assertEqual(m['text'], hub.dumps(structured) if structured else '')
            self.assertEqual(self.h.artifacts(a['id'])['images'][0]['src'], '/media/' + m['images'][0])
            self.assertEqual((hub.MEDIA / m['images'][0]).read_bytes(), base64.b64decode(PNG))


if __name__ == '__main__':
    unittest.main()
