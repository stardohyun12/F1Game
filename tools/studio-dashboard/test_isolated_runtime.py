import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import urllib.request
from isolated_runtime import HERE, isolated_runtime, stop_isolated_runtime


class IsolatedRuntimeTests(unittest.TestCase):
    def test_failure_cleans_up_real_runtime(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaisesRegex(AssertionError, 'fixture failure'):
                with isolated_runtime(folder):
                    d = json.loads((Path(folder) / 'runtime.json').read_text())
                    request = urllib.request.Request(f"http://127.0.0.1:{d['port']}/api/hub/runtime-health",
                        headers={'X-PitCrew-Runtime': d['secret']})
                    with urllib.request.urlopen(request, timeout=2) as response:
                        self.assertEqual(json.load(response)['pid'], d['pid'])
                    raise AssertionError('fixture failure')
            with self.assertRaises(OSError):
                urllib.request.urlopen(request, timeout=2)
            stop_isolated_runtime(folder)  # Repeated cleanup is safe.

    def test_production_directory_refused(self):
        with self.assertRaises(ValueError):
            stop_isolated_runtime(HERE / '.runtime')
        with self.assertRaises(ValueError):
            with isolated_runtime(HERE / '.runtime'):
                self.fail('production must never be adopted')

    def test_existing_runtime_not_adopted(self):
        with tempfile.TemporaryDirectory() as folder:
            (Path(folder) / 'runtime.json').write_text('{}')
            with self.assertRaises(ValueError):
                with isolated_runtime(folder):
                    self.fail('existing runtime must never be adopted')

    def test_copied_production_descriptor_refused(self):
        with tempfile.TemporaryDirectory() as folder:
            base = Path(folder)
            (base / '.runtime').mkdir()
            (base / 'fixture').mkdir()
            data = json.dumps({'pid': 42, 'secret': 'fixture-secret', 'port': 1})
            (base / '.runtime' / 'runtime.json').write_text(data)
            (base / 'fixture' / 'runtime.json').write_text(data)
            with patch('isolated_runtime.HERE', base):
                with self.assertRaisesRegex(ValueError, 'production'):
                    stop_isolated_runtime(base / 'fixture')


if __name__ == '__main__':
    unittest.main()
