"""Plain capture paths in answers become results-panel images only for existing image files inside the project.
Real Hub/SQLite in a temp folder; no provider, no server process."""
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
import urllib.parse

from hub import Hub

PNG = b'\x89PNG\r\n\x1a\n' + b'0' * 32


class CapturePathTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(ignore_cleanup_errors=True); base = Path(self.tmp.name)
        self.project = base / 'project'; self.outside = base / 'outside'
        for d in (self.project / 'docs/screenshots/auto', self.project / 'captures/art', self.outside): d.mkdir(parents=True)
        for rel in ('docs/screenshots/auto/auto_0001.png', 'captures/a.png', 'captures/art/b.jpg', 'c.png', 'captures/공용 폴더.png'):
            (self.project / rel).write_bytes(PNG)
        (self.project / 'captures/notes.txt').write_text('x'); (self.project / 'captures/dir.png').mkdir()
        (self.outside / 'secret.png').write_bytes(PNG)
        self.h = Hub(base / 'runtime', root=self.project); self.aid = self.h.store.agents()[0]['id']

    def tearDown(self):
        self.h.close()
        for h in self.h.project_hubs.values(): h.store.db.close()
        self.h.store.db.close(); self.tmp.cleanup()

    def say(self, text, role='assistant'): return self.h.store.message(self.aid, role, text)

    def paths(self, **kw):
        out = []
        for i in self.h.artifacts(self.aid, **kw)['images']:
            raw = urllib.parse.unquote(i['src'].split('path=', 1)[1]) if i['src'].startswith('/local-image?') else i['src']
            p = Path(raw)
            out.append(p.resolve().relative_to(self.project.resolve()).as_posix() if p.is_absolute() else raw)
        return out

    def test_explicit_forms_are_collected(self):
        self.say('Screenshot: docs/screenshots/auto/auto_0001.png\n수정 후: captures/a.png, 비교는 `captures/art/b.jpg` 와 `c.png`.')
        self.assertCountEqual(self.paths(), ['docs/screenshots/auto/auto_0001.png', 'captures/a.png', 'captures/art/b.jpg', 'c.png'])
        image = next(i for i in self.h.artifacts(self.aid)['images'] if i['label'] == 'auto_0001.png')
        self.assertTrue(Path(urllib.parse.unquote(image['src'].split('path=', 1)[1])).is_absolute())

    def test_windows_and_dot_prefixed_paths(self):
        absolute = str((self.project / 'captures' / 'a.png').resolve())
        self.say(f'캡처 {absolute} 와 .\\captures\\art\\b.jpg')
        self.assertCountEqual(self.paths(), ['captures/a.png', 'captures/art/b.jpg'])

    def test_markdown_images_unchanged_and_not_duplicated(self):
        self.say('![전](captures/a.png) ![없음](captures/missing.png)\n같은 파일: captures/a.png')
        got = self.paths()
        self.assertEqual(sorted(got), ['captures/a.png', 'captures/missing.png'])  # markdown keeps even a missing file, as before
        self.assertEqual(got.count('captures/a.png'), 1)

    def test_rejected_candidates(self):
        outside_abs = str((self.outside / 'secret.png').resolve())
        self.say('\n'.join([
            'captures/missing.png', '../outside/secret.png', outside_abs, 'captures/notes.txt', 'captures/dir.png',
            'https://example.com/captures/a.png', 'file:///captures/a.png', 'captures/a.png.bak', 'xcaptures/a.pngx',
            '이 문장은 captures 폴더의 c.png 를 말한다', 'c.png 단독 평문', 'captures/공용 폴더.png']))
        self.assertEqual(self.paths(), [])

    def test_link_escape_is_refused(self):
        made = []
        try:
            os.symlink(self.outside / 'secret.png', self.project / 'captures/link.png'); made.append('captures/link.png')
        except (OSError, NotImplementedError): pass
        if os.name == 'nt':
            r = subprocess.run(['cmd', '/c', 'mklink', '/J', str(self.project / 'junction'), str(self.outside)], capture_output=True)
            if r.returncode == 0: made.append('junction/secret.png')
        if not made: self.skipTest('no symlink or junction could be created here')
        self.say(' '.join(made) + ' captures/a.png')
        self.assertEqual(self.paths(), ['captures/a.png'])

    def test_only_answers_are_scanned(self):
        self.say('captures/a.png', role='user'); self.say('captures/a.png', role='tool')
        self.assertEqual(self.paths(), [])

    def test_duplicates_and_trailing_punctuation(self):
        self.say('captures/a.png, captures/a.png. `captures/a.png`'); self.say('다시: captures/a.png')
        self.assertEqual(self.paths(), ['captures/a.png'])

    def test_limits(self):
        many = ' '.join(f'captures/x{i}.png' for i in range(30)) + ' captures/a.png'
        self.say(many)  # 21st candidate onward is not checked in one answer
        self.assertEqual(self.paths(), [])

    def test_budget_is_spent_from_the_newest_answer(self):
        self.say('captures/a.png')                                  # oldest
        for _ in range(3): self.say('captures/x.png')               # missing files still use the budget
        self.say('Screenshot: docs/screenshots/auto/auto_0001.png')  # newest
        self.assertEqual(self.paths(), ['docs/screenshots/auto/auto_0001.png', 'captures/a.png'])  # full budget: newest first
        self.h.PLAIN_IMAGE_TOTAL = 3
        self.assertEqual(self.paths(), ['docs/screenshots/auto/auto_0001.png'])  # budget ends in the old answers
        del self.h.PLAIN_IMAGE_TOTAL

    def test_order_and_duplicates_with_budget(self):
        self.say('captures/a.png'); self.say('`captures/art/b.jpg`'); self.say('다시 captures/a.png')
        self.assertEqual(self.paths(), ['captures/a.png', 'captures/art/b.jpg'])  # newest mention of a.png wins, once
        self.h.PLAIN_IMAGE_TOTAL = 1
        self.assertEqual(self.paths(), ['captures/a.png'])
        del self.h.PLAIN_IMAGE_TOTAL

    def test_long_answer_scan_limit(self):
        self.say('가' * (Hub.PLAIN_IMAGE_TEXT + 10) + ' captures/a.png')
        self.assertEqual(self.paths(), [])

    def test_browse_search_and_project_scope(self):
        self.say('Screenshot: docs/screenshots/auto/auto_0001.png')
        found = self.h.artifacts(self.aid, query='auto_0001', browse=True)['images']
        self.assertEqual([i['label'] for i in found], ['auto_0001.png'])
        other = Path(self.tmp.name) / 'other'; (other / 'captures').mkdir(parents=True); (other / 'captures/o.png').write_bytes(PNG)
        child = self.h.project(self.h.add_project({'path': str(other)})['id']); aid = child.store.agents()[0]['id']
        child.store.message(aid, 'assistant', 'captures/o.png captures/a.png')
        labels = [i['label'] for i in child.artifacts(aid)['images']]
        self.assertEqual(labels, ['o.png'])  # resolved against that project's own root only


if __name__ == '__main__':
    unittest.main()
