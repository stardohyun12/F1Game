import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import workspace_changes as W


class WorkspaceChangesTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.git('init', '-q')
        self.git('config', 'user.email', '[REDACTED_EMAIL]')
        self.git('config', 'user.name', 'Test')
        for name in ['Game/scene.unity', 'Game/car.prefab', 'Other/file.txt']:
            p = self.root / name
            p.parent.mkdir(exist_ok=True)
            p.write_text('original\n', encoding='utf-8')
        self.git('add', '.')
        self.git('-c', 'core.hooksPath=NUL', 'commit', '-qm', 'fixture')

    def git(self, *args):
        return subprocess.run(['git', '-C', str(self.root), *args], check=True, capture_output=True)

    def test_clean_is_a_verified_empty_result(self):
        r = W.snapshot(self.root)
        self.assertEqual((r['status'], r['total'], r['entries']), ('ready', 0, []))

    def test_unity_external_edits_staging_and_unicode_are_visible_without_index_writes(self):
        (self.root/'Game/scene.unity').write_text('modified scene\n')
        (self.root/'Game/car.prefab').unlink()
        (self.root/'Game/한글 새 파일.mat').write_text('material\n', encoding='utf-8')
        self.git('add', 'Game/scene.unity')
        (self.root/'Game/scene.unity').write_text('modified again\n')
        idx = self.root/'.git/index'
        original = (idx.read_bytes(), idx.stat().st_mtime_ns)
        r = W.snapshot(self.root)
        entries = {e['path']: e for e in r['entries']}
        self.assertEqual(entries['Game/scene.unity']['index'], 'M')
        self.assertEqual(entries['Game/scene.unity']['worktree'], 'M')
        self.assertEqual(entries['Game/car.prefab']['worktree'], 'D')
        self.assertTrue(entries['Game/한글 새 파일.mat']['untracked'])
        self.assertEqual(original, (idx.read_bytes(), idx.stat().st_mtime_ns))
        self.assertFalse((self.root/'.git/index.lock').exists())

    def test_workspace_scope_does_not_include_siblings(self):
        (self.root/'Other/file.txt').write_text('outside\n')
        (self.root/'Game/scene.unity').write_text('inside\n')
        r = W.snapshot(self.root/'Game')
        self.assertEqual([e['path'] for e in r['entries']], ['Game/scene.unity'])

    def test_untracked_directories_group_and_ignored_files_stay_out(self):
        (self.root/'.git/info/exclude').write_text('ignored.tmp\n')
        (self.root/'ignored.tmp').write_text('ignored')
        (self.root/'NewFolder').mkdir()
        (self.root/'NewFolder/item.mat').write_text('new')
        r = W.snapshot(self.root)
        self.assertEqual(len(r['entries']), 1)
        self.assertEqual(r['entries'][0]['path'], 'NewFolder/')
        self.assertTrue(r['entries'][0]['directory'])

    def test_rename_is_explicit_delete_and_add_without_heuristics(self):
        self.git('mv', 'Game/car.prefab', 'Game/new car.prefab')
        entries = {e['path']:e for e in W.snapshot(self.root)['entries']}
        self.assertEqual(entries['Game/car.prefab']['index'], 'D')
        self.assertEqual(entries['Game/new car.prefab']['index'], 'A')

    def test_merge_conflict_is_not_a_normal_modification(self):
        branch = self.git('branch', '--show-current').stdout.decode().strip()
        self.git('checkout', '-qb', 'other')
        (self.root/'Game/scene.unity').write_text('other\n')
        self.git('add', '.')
        self.git('-c', 'core.hooksPath=NUL', 'commit', '-qm', 'other')
        self.git('checkout', '-q', branch)
        (self.root/'Game/scene.unity').write_text('ours\n')
        self.git('add', '.')
        self.git('-c', 'core.hooksPath=NUL', 'commit', '-qm', 'ours')
        merge = subprocess.run(['git', '-C', str(self.root), '-c', 'core.hooksPath=NUL', 'merge', '--no-edit', 'other'], capture_output=True)
        self.assertNotEqual(merge.returncode, 0)
        entry = W.snapshot(self.root)['entries'][0]
        self.assertEqual(entry['path'], 'Game/scene.unity')
        self.assertTrue(entry['conflicted'])

    def test_inherited_git_target_cannot_redirect_workspace(self):
        with patch.dict(os.environ, {'GIT_DIR':str(self.root/'bogus'), 'GIT_WORK_TREE':str(self.root/'bogus')}):
            self.assertEqual(W.snapshot(self.root)['status'], 'ready')

    def test_truncated_list_does_not_claim_complete(self):
        for i in range(5):
            (self.root/f'new{i}.txt').write_text('new')
        with patch.object(W, 'MAX_ENTRIES', 2):
            r = W.snapshot(self.root)
            self.assertEqual((len(r['entries']), r['limited'], r['total']), (2, True, 5))
        with patch.object(W, 'MAX_BYTES', 17):
            r = W.snapshot(self.root)
            self.assertTrue(r['limited'])
            self.assertIsNone(r['total'])
            self.assertTrue(all(e['path'].endswith('.txt') for e in r['entries']))

    def test_missing_repo_and_timeout_are_not_clean(self):
        with tempfile.TemporaryDirectory() as nonrepo:
            result = W.snapshot(nonrepo)
            self.assertEqual(result['status'], 'unavailable')
            self.assertEqual(result['reason'], '이 작업 공간은 Git 저장소가 아니어서 변경 목록을 표시할 수 없습니다.')
        with patch.object(W.subprocess, 'run', side_effect=subprocess.TimeoutExpired('git', 5)):
            self.assertEqual(W.snapshot(self.root)['status'], 'timeout')
        # Failure releases the slot and the next read still works.
        self.assertEqual(W.snapshot(self.root)['status'], 'ready')


if __name__ == '__main__':
    unittest.main()
