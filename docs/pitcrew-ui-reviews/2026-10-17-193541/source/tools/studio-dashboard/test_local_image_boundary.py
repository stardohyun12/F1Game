"""Exercise the image-serving boundary without contacting any remote filesystem."""
import os
from pathlib import Path
import tempfile
import threading
import unittest
from http.server import ThreadingHTTPServer
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import urlopen
from unittest.mock import Mock, patch
import hub
import server
from server import H


class LocalImageBoundaryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        base = Path(self.temp.name).resolve()
        self.root = base / 'project'; self.root.mkdir()
        self.media = base / 'media'; self.media.mkdir()
        self.other = base / 'other'; self.other.mkdir()
        self.handler = object.__new__(H)
        self.handler._image_roots = Mock(return_value=[self.root, self.media, self.other])
        self.handler._json = lambda body, code=200: (code, body)
        self.handler._send = lambda body, mime: (200, body, mime)
        self.root_patch = patch.object(hub, 'ROOT', self.root)
        self.root_patch.start(); self.addCleanup(self.root_patch.stop)

    def test_untrusted_paths_rejected_before_filesystem_access(self):
        candidates = [str(self.root.parent / 'outside.png'), '../outside.png',
                      str(self.root) + '-sibling/x.png',
                      r'\\unreachable.invalid\share\x.png', '//unreachable.invalid/share/x.png',
                      'file://unreachable.invalid/share/x.png',
                      'file:////unreachable.invalid/share/x.png',
                      r'\\?\UNC\unreachable.invalid\share\x.png']
        with patch.object(Path, 'resolve', side_effect=AssertionError('resolved untrusted path')), \
             patch.object(Path, 'is_file', side_effect=AssertionError('probed untrusted path')), \
             patch.object(Path, 'read_bytes', side_effect=AssertionError('read untrusted path')):
            for candidate in candidates:
                with self.subTest(candidate=candidate):
                    self.assertEqual(self.handler._local_image(candidate)[0], 403)

    def test_allowed_paths_preserve_bytes_and_mime(self):
        for root in (self.root, self.media, self.other):
            image = root / 'with space.png'; image.write_bytes(b'png-fixture')
            candidates = [str(image), 'file:///' + image.as_posix().lstrip('/')]
            if root == self.root: candidates.append('with space.png')
            if os.name == 'nt': candidates.append('/' + image.as_posix())
            for candidate in candidates:
                with self.subTest(candidate=candidate):
                    self.assertEqual(self.handler._local_image(candidate),
                                     (200, b'png-fixture', 'image/png'))

    def test_resolved_escape_rejected_before_file_read(self):
        outside = self.root.parent / 'outside.png'; outside.write_bytes(b'private')
        with patch.object(Path, 'resolve', return_value=outside), \
             patch.object(Path, 'is_file', side_effect=AssertionError('probed escaped file')), \
             patch.object(Path, 'read_bytes', side_effect=AssertionError('read escaped file')):
            self.assertEqual(self.handler._local_image(str(self.root / 'linked.png'))[0], 403)

    def test_non_image_rejected_before_resolution(self):
        with patch.object(Path, 'resolve', side_effect=AssertionError('resolved non-image')):
            self.assertEqual(self.handler._local_image('config.txt')[0], 404)

    def test_missing_image(self):
        self.assertEqual(self.handler._local_image('missing.png')[0], 404)

    def test_large_image_still_rejected(self):
        image = self.root / 'large.png'
        with image.open('wb') as stream: stream.truncate(30 * 1024 * 1024 + 1)
        with patch.object(Path, 'read_bytes', side_effect=AssertionError('read oversized file')):
            self.assertEqual(self.handler._local_image(str(image))[0], 413)

    def test_http_query_decoding_keeps_boundary_and_space_paths(self):
        roots = [self.root, self.media, self.other]
        class Handler(H):
            def _image_roots(self): return roots
        service = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
        thread = threading.Thread(target=service.serve_forever, daemon=True)
        thread.start()
        port = service.server_port
        image = self.root / 'space name.png'; image.write_bytes(b'http-image')
        try:
            with patch.object(server, 'PORT', port):
                base = f'http://127.0.0.1:{port}/local-image?'
                with urlopen(base + urlencode({'path': str(image)}), timeout=3) as response:
                    self.assertEqual(response.read(), b'http-image')
                    self.assertEqual(response.headers['Content-Type'], 'image/png')
                with patch.object(Path, 'resolve', side_effect=AssertionError('HTTP candidate resolved')), \
                     patch.object(Path, 'is_file', side_effect=AssertionError('HTTP candidate probed')):
                    for path in (r'\\unreachable.invalid\share\x.png',
                                 'file://unreachable.invalid/share/x.png', '../outside.png'):
                        with self.subTest(path=path), self.assertRaises(HTTPError) as error:
                            urlopen(base + urlencode({'path': path}), timeout=3)
                        self.assertEqual(error.exception.code, 403)
                        error.exception.close()
        finally:
            service.shutdown(); service.server_close(); thread.join(timeout=3)


if __name__ == '__main__': unittest.main()
