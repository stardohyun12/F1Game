"""Results browser: artifacts(aid, query, image_page, input_page, browse=True) finds what the short panel view cuts off.
Real Hub + Store on a temp directory; the server route is checked through the real server.H handler."""
import json
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
from http.server import ThreadingHTTPServer
from unittest.mock import patch

import hub


class Base(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(ignore_cleanup_errors=True)
        self.h = hub.Hub(self.tmp.name); self.a = self.h.store.create(name='작업', provider='claude')

    def tearDown(self):
        self.h.close(); self.h.store.db.close(); self.tmp.cleanup()  # Hub.close() leaves the store's sqlite open

    def images(self, n, prefix='img'):
        for i in range(n):  # oldest first; media names are the stored file names
            self.h.store.message(self.a['id'], 'tool', f'shot {i}', title=f'{prefix}-{i:03d}', images=[f'{prefix}{i:03d}.png'])

    def inputs(self, n):
        for i in range(n):
            self.h.store.message(self.a['id'], 'user', f'첨부 {i}', files=[{'name': f'note-{i:03d}.txt', 'path': f'D:/inbox/note-{i:03d}.txt'}])

    def browse(self, **kw):
        return self.h.artifacts(self.a['id'], browse=True, **kw)


class CompatibilityTests(Base):
    def test_default_call_keeps_shape_and_60_40_caps(self):
        self.images(70); self.inputs(50)
        r = self.h.artifacts(self.a['id'])
        self.assertEqual(set(r), {'images', 'files', 'inputs'})
        self.assertEqual(len(r['images']), 60); self.assertEqual(len(r['inputs']), 40)
        self.assertEqual(r['images'][0]['src'], '/media/img069.png')   # newest first, as before
        self.assertEqual(r['inputs'][0]['name'], 'note-049.txt')

    def test_duplicate_image_keeps_the_latest_mention(self):
        self.h.store.message(self.a['id'], 'assistant', '![a](D:/cap/x.png)')
        later = self.h.store.message(self.a['id'], 'assistant', '![a](D:/cap/x.png)')
        for r in (self.h.artifacts(self.a['id']), self.browse()):
            self.assertEqual([i['message'] for i in r['images']], [later['id']])


class BrowseTests(Base):
    def test_image_older_than_the_61st_is_found(self):
        self.images(70)
        r = self.browse(query='IMG-000')            # label, case-insensitive
        self.assertEqual([i['src'] for i in r['images']], ['/media/img000.png'])
        self.assertEqual(r['counts']['images'], 70)
        self.assertEqual(r['pagination']['images'], dict(page=1, pages=1, total=1, pageSize=12))
        self.assertEqual(r['query'], 'IMG-000')

    def test_input_older_than_the_41st_is_found_by_name_or_path(self):
        self.inputs(50)
        self.assertEqual([f['name'] for f in self.browse(query='note-000')['inputs']], ['note-000.txt'])
        self.assertEqual([f['name'] for f in self.browse(query='d:/INBOX/note-001')['inputs']], ['note-001.txt'])
        self.assertEqual(self.browse(query='note-0')['counts']['inputs'], 50)

    def test_url_encoded_korean_and_space_paths_are_searchable(self):
        self.h.store.message(self.a['id'], 'assistant', '![a](<D:/게임 캡처/내 그림.png>) ![b](D:/other/plain.png)')
        r = self.browse(query='게임 캡처')                      # only in the decoded src path
        self.assertEqual(len(r['images']), 1)
        self.assertIn('%EA%B2%8C%EC%9E%84%20', r['images'][0]['src'])   # src itself stays encoded
        self.assertEqual(len(self.browse(query='내 그림')['images']), 1)   # label
        self.assertEqual(len(self.browse(query='plain')['images']), 1)

    def test_files_are_filtered_whole_and_counted_before_filtering(self):
        for p in ('Assets/Scripts/Car.cs', 'Assets/Scenes/Main.unity', 'tools/x.py'):
            self.h.store.message(self.a['id'], 'tool', 'edit', diff={'files': [{'path': p, 'add': 2, 'del': 1}]})
        r = self.browse(query='assets/')
        self.assertEqual(sorted(f['path'] for f in r['files']), ['Assets/Scenes/Main.unity', 'Assets/Scripts/Car.cs'])
        self.assertEqual(r['counts']['files'], 3)

    def test_page_boundaries_and_clamping(self):
        self.images(25); self.inputs(41)
        first = self.browse()
        self.assertEqual(first['pagination']['images'], dict(page=1, pages=3, total=25, pageSize=12))
        self.assertEqual(first['pagination']['inputs'], dict(page=1, pages=3, total=41, pageSize=20))
        self.assertEqual(first['images'][0]['src'], '/media/img024.png')
        last = self.browse(image_page=3, input_page=3)
        self.assertEqual([i['src'] for i in last['images']], ['/media/img000.png'])
        self.assertEqual([f['name'] for f in last['inputs']], ['note-000.txt'])
        self.assertEqual(self.browse(image_page=2)['images'][0]['src'], '/media/img012.png')
        over = self.browse(image_page=999, input_page='99')
        self.assertEqual((over['pagination']['images']['page'], over['pagination']['inputs']['page']), (3, 3))
        self.assertEqual([i['src'] for i in over['images']], ['/media/img000.png'])

    def test_invalid_page_values_fall_back_to_page_1(self):
        self.images(25)
        for bad in (0, -2, '-5', 'abc', '', None, '1e3', ' 2 ', 10**30, '9' * 400):
            r = self.browse(image_page=bad)
            want = 2 if bad == ' 2 ' else 3 if bad in (10**30, '9' * 400) else 1
            self.assertEqual(r['pagination']['images']['page'], want, repr(bad)[:20])
            self.assertTrue(r['images'], repr(bad)[:20])

    def test_empty_results(self):
        self.images(3)
        r = self.browse(query='없는 이름', image_page=4, input_page=-1)
        self.assertEqual((r['images'], r['inputs'], r['files']), ([], [], []))
        self.assertEqual(r['pagination']['images'], dict(page=1, pages=0, total=0, pageSize=12))
        self.assertEqual(r['pagination']['inputs'], dict(page=1, pages=0, total=0, pageSize=20))
        self.assertEqual(r['counts'], dict(images=3, files=0, inputs=0))
        empty = Base.browse(self)  # nothing filtered, but this conversation has no inputs
        self.assertEqual(empty['pagination']['inputs'], dict(page=1, pages=0, total=0, pageSize=20))

    def test_blank_query_means_everything(self):
        self.images(13)
        self.assertEqual(self.browse(query='   ')['pagination']['images']['total'], 13)


class RouteTests(Base):
    def get(self, query):
        import server
        srv = ThreadingHTTPServer(('127.0.0.1', 0), server.H); port = srv.server_address[1]
        threading.Thread(target=srv.serve_forever, daemon=True).start()
        try:
            with patch.object(hub, 'get_hub', lambda project=None: self.h), patch.object(server, 'PORT', port):
                try:
                    return 200, json.loads(urllib.request.urlopen(f'http://127.0.0.1:{port}/api/hub/artifacts?{query}', timeout=10).read())
                except urllib.error.HTTPError as e:
                    return e.code, json.loads(e.read() or b'{}')
        finally:
            srv.shutdown(); srv.server_close()

    def test_route_without_browse_keeps_the_old_answer(self):
        self.images(65)
        code, r = self.get(f"agent={self.a['id']}&q=img-000&image_page=2")   # extra params ignored without browse=1
        self.assertEqual(code, 200); self.assertEqual(set(r), {'images', 'files', 'inputs'}); self.assertEqual(len(r['images']), 60)

    def test_route_with_browse_passes_query_and_pages(self):
        self.images(30)
        code, r = self.get(f"agent={self.a['id']}&browse=1&q=%EC%97%86%EC%9D%8C")   # '없음' -> nothing
        self.assertEqual((code, r['pagination']['images']['total'], r['query']), (200, 0, '없음'))
        code, r = self.get(f"agent={self.a['id']}&browse=1&image_page=3")
        self.assertEqual(r['pagination']['images'], dict(page=3, pages=3, total=30, pageSize=12))
        self.assertEqual(len(r['images']), 6)
        code, r = self.get(f"agent={self.a['id']}&browse=1&image_page=abc&input_page=-4")
        self.assertEqual((code, r['pagination']['images']['page'], r['pagination']['inputs']['page']), (200, 1, 1))


if __name__ == '__main__':
    unittest.main()
