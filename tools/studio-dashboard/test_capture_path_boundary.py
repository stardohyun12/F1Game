"""Independent root review: reject outside paths before any candidate filesystem resolution."""
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from hub import Hub


class CaptureBoundaryTests(unittest.TestCase):
    def test_outside_paths_never_resolve_candidate(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder).resolve()
            owner = object.__new__(Hub); owner.root = root
            original = Path.resolve
            seen = []
            def resolve(path, *args, **kwargs):
                seen.append(str(path))
                if path != root: raise AssertionError('outside candidate touched')
                return original(path, *args, **kwargs)
            candidates = [str(root.parent/'elsewhere'/'image.png'), '../elsewhere/image.png']
            if os.name == 'nt': candidates.append(r'\\unreachable.invalid\share\capture.png')
            with patch.object(Path, 'resolve', resolve):
                for candidate in candidates:
                    self.assertIsNone(owner.project_image(candidate))
            self.assertEqual(len(seen), len(candidates))

    def test_exhausted_budget_does_not_scan_history(self):
        owner = object.__new__(Hub)
        class Unreadable:
            def __getitem__(self, key): raise AssertionError('old text scanned')
        self.assertEqual(owner.plain_image_paths(Unreadable(), [0]), [])


if __name__ == '__main__': unittest.main()
