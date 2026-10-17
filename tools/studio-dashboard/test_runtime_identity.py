import copy
import hashlib
from pathlib import Path
import tempfile
import types
import unittest
import os
import subprocess
import sys

import runtime_identity as identity


class RuntimeIdentityTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory(); self.root=Path(self.tmp.name)
        self.path=self.root/'fixture.py'
        self.path.write_text('def value():\n    return 1\nclass Example:\n    @staticmethod\n    def answer(): return 42\n',encoding='utf-8')

    def tearDown(self): self.tmp.cleanup()

    def load(self):
        module=types.ModuleType('identity_fixture'); module.__file__=str(self.path)
        exec(compile(self.path.read_bytes(),str(self.path),'exec'),vars(module))
        return module

    def test_file_edit_does_not_claim_running_code_changed(self):
        module=self.load(); captured=identity.capture({'fixture.py':module}); saved=copy.deepcopy(captured)
        self.assertEqual(captured['modules']['fixture.py']['functionCount'],2)
        self.path.write_text('def value():\n    return 2\n',encoding='utf-8')
        result=identity.snapshot(captured,self.root)['modules']['fixture.py']
        self.assertTrue(result['sourceChangedSinceCapture'])
        self.assertEqual(module.value(),1)
        self.assertEqual(result['loadedCodeSha256'],identity.code_fingerprint(module)[0])
        self.assertEqual(captured,saved)
        restarted=self.load()
        self.assertEqual(restarted.value(),2)
        self.assertNotEqual(identity.code_fingerprint(restarted)[0],result['loadedCodeSha256'])

    def test_same_code_reload_has_same_fingerprint(self):
        a=self.load(); b=self.load()
        self.assertEqual(identity.code_fingerprint(a),identity.code_fingerprint(b))
        snapshot=identity.snapshot(identity.capture({'fixture.py':a}),self.root)
        self.assertFalse(snapshot['modules']['fixture.py']['sourceChangedSinceCapture'])
        self.assertEqual(snapshot['modules']['fixture.py']['sourceSha256Now'],hashlib.sha256(self.path.read_bytes()).hexdigest())

    def test_path_and_line_offsets_do_not_change_code_fingerprint(self):
        original=self.load()
        source='\n\n\n'+self.path.read_text(encoding='utf-8')
        self.path=self.root/'different-location.py'
        self.path.write_text(source,encoding='utf-8')
        moved=self.load()
        self.assertNotEqual(original.value.__code__.co_filename,moved.value.__code__.co_filename)
        self.assertNotEqual(original.value.__code__.co_firstlineno,moved.value.__code__.co_firstlineno)
        self.assertEqual(identity.code_fingerprint(original),identity.code_fingerprint(moved))

    def test_missing_file_is_unknown_instead_of_unchanged(self):
        captured=identity.capture({'fixture.py':self.load()}); self.path.unlink()
        row=identity.snapshot(captured,self.root)['modules']['fixture.py']
        self.assertIsNone(row['sourceSha256Now'])
        self.assertIsNone(row['sourceChangedSinceCapture'])
        self.assertTrue(row['loadedCodeSha256'])

    def test_legacy_without_capture_is_unavailable(self):
        self.assertEqual(identity.snapshot(None,self.root),{'available':False})

    def test_fingerprint_is_stable_across_hash_seeds(self):
        script="import types,runtime_identity as r; m=types.ModuleType('fixture'); exec(compile(\"def f(x): return x in {'alpha','bravo','charlie','delta'}\",'fixture.py','exec'),vars(m)); print(r.code_fingerprint(m)[0])"
        values=[subprocess.check_output([sys.executable,'-c',script],cwd=Path(identity.__file__).parent,
            env={**os.environ,'PYTHONHASHSEED':str(seed)},text=True).strip() for seed in (1,2,3)]
        self.assertEqual(len(set(values)),1)


if __name__=='__main__': unittest.main()
