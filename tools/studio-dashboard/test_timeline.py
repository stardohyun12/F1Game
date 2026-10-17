"""Tool activity records: a call keeps its name/label when its result arrives; images become files."""
import base64
import tempfile
import unittest
from unittest.mock import patch
from pathlib import Path

import hub

PNG = base64.b64encode(bytes.fromhex('89504e470d0a1a0a0000000d49484452000000010000000108060000001f15c4890000000d4944415478da63f8ffff3f0005fe02fe0dc4c7c40000000049454e44ae426082')).decode()

class TimelineTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.h = hub.Hub(self.tmp.name)
        self.a = self.h.store.agents()[0]; self.run = hub.Run(self.h, self.a, {}, 'x')
        self.media = patch.object(hub, 'MEDIA', Path(self.tmp.name) / 'media'); self.media.start()

    def tearDown(self):
        self.media.stop(); self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def only_tool(self):
        tools = [m for m in self.h.store.messages(self.a['id']) if m['role'] == 'tool']
        self.assertEqual(len(tools), 1); return tools[0]

    def test_current_activity_tracks_tool_and_clears_only_matching_result(self):
        self.run.tool('one', 'Read', '{}', label='원문 확인')
        self.run.tool('two', 'Search', '{}', label='반례 조사')
        self.run.tool_result('one', 'ok', 'done')
        self.assertEqual(self.h.store.agent(self.a['id'])['currentActivity']['label'], '반례 조사')
        self.run.tool_result('two', 'ok', 'done')
        self.assertIsNone(self.h.store.agent(self.a['id'])['currentActivity'])

    def test_result_keeps_call_identity(self):
        inputs = {'command': 'ls -la', 'description': 'List files'}
        self.run.tool('k', 'Bash', hub.dumps(inputs), label=hub.tool_label('Bash', inputs), input=hub.dumps(inputs))
        self.run.tool_result('k', 'a\nb', 'done')
        m = self.only_tool()
        self.assertEqual((m['title'], m['label'], m['status'], m['text']), ('Bash', 'List files', 'done', 'a\nb'))
        self.assertIn('ls -la', m['input']); self.assertTrue(m['output'])

    def test_image_result_is_a_file_not_base64(self):
        self.run.tool('k', 'Read', '{}', label='shot.png')
        text, images = hub.tool_output([{'type': 'image', 'source': {'type': 'base64', 'media_type': 'image/png', 'data': PNG}}, {'type': 'text', 'text': 'caption'}])
        self.run.tool_result('k', text, 'done', images)
        m = self.only_tool()
        self.assertEqual(m['text'], 'caption'); self.assertNotIn(PNG[:20], hub.dumps(m))
        self.assertTrue((hub.MEDIA / m['images'][0]).read_bytes().startswith(b'\x89PNG'))

    def test_polling_payload_is_trimmed_and_full_text_on_demand(self):
        self.run.tool('k', 'Read', '{}'); self.run.tool_result('k', 'x' * 9000, 'done')
        listed = self.only_tool(); detail = self.h.detail(self.a['id'])
        shown = next(m for m in detail['messages'] if m['role'] == 'tool')
        self.assertTrue(shown['truncated']); self.assertEqual(len(shown['text']), hub.TOOL_PREVIEW)
        self.assertEqual(len(self.h.message(self.a['id'], listed['id'])['text']), 9000)

    def test_legacy_repr_image_is_hidden(self):
        self.h.store.message(self.a['id'], 'tool', "[{'type': 'image', 'source': {'type': 'base64', 'data': '" + PNG + "', 'media_type': 'image/png'}}]", title='도구 결과', status='done')
        shown = next(m for m in self.h.detail(self.a['id'])['messages'] if m['role'] == 'tool')
        self.assertNotIn(PNG[:20], shown['text']); self.assertIn('이미지 생략', shown['text'])

    def test_legacy_image_any_field_order(self):
        # Codex review R2: {type, media_type, data} order used to leak base64.
        self.h.store.message(self.a['id'], 'tool', str([{'type': 'image', 'source': {'type': 'base64', 'media_type': 'image/png', 'data': 'A' * 3000}}]), title='도구 결과', status='done')
        shown = next(m for m in self.h.detail(self.a['id'])['messages'] if m['role'] == 'tool')
        self.assertNotIn('A' * 64, shown['text']); self.assertLess(len(shown['text']), 200)

    def test_input_preview_is_capped_even_with_short_output(self):
        # Codex review R4.
        self.run.tool('k', 'Write', '{}', input='x' * 4000); self.run.tool_result('k', 'ok', 'done')
        shown = next(m for m in self.h.detail(self.a['id'])['messages'] if m['role'] == 'tool')
        self.assertEqual(len(shown['input']), hub.TOOL_PREVIEW); self.assertTrue(shown['truncated'])

    def test_replace_all_diff_is_marked_as_snippet(self):
        d = hub.edit_diff('Edit', {'file_path': 'a.py', 'old_string': 'x', 'new_string': 'y', 'replace_all': True})
        self.assertIn('조각', d['files'][0]['note'])
        self.assertNotIn('note', hub.edit_diff('Edit', {'file_path': 'a.py', 'old_string': 'x', 'new_string': 'y'})['files'][0])

    def test_short_streamed_line_is_whole_before_the_next_tool(self):
        # Seen live: "같이 확인할게요" streamed in pieces then a tool → the record kept only "같" while the run went on.
        for piece in ('같', '이 ', '확인', '할게요'): self.run.output('m1', piece, True)
        self.run.tool('t1', 'Bash', 'ls')
        texts = [m['text'] for m in self.h.store.messages(self.a['id']) if m['role'] == 'assistant']
        self.assertEqual(texts, ['같이 확인할게요'])

    def write_session(self, turns, stamp=None):
        import json, os
        from datetime import datetime, timezone
        stamp = stamp if stamp is not None else datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z')
        d = self.h.sources.claude_directory(); d.mkdir(parents=True, exist_ok=True)
        rows = []
        for prompt, texts in turns:
            rows.append({'type': 'user', 'message': {'content': '[PitCrew 작업 컨텍스트 — 이번 턴의 최신 설정]\n{}\n\n[사용자 메시지]\n' + prompt}})
            rows += [{'type': 'assistant', 'timestamp': stamp, 'message': {'content': [{'type': 'text', 'text': t}]}} for t in texts]
        (d / 's1.jsonl').write_text('\n'.join(json.dumps(r, ensure_ascii=False) for r in rows), encoding='utf-8')

    def test_repair_restores_cut_answers_within_their_own_turn(self):
        import os
        with patch.dict(os.environ, {'CLAUDE_CONFIG_DIR': str(Path(self.tmp.name) / 'claude')}):
            a = self.h.store.update(self.a['id'], provider='claude', session='s1')
            self.write_session([('일 해줘', ['완전한 답', '같이 확인할게요', 'The screen looks fine'])])
            self.h.store.message(a['id'], 'user', '일 해줘')
            for t in ('완전한 답', '같', 'The scre'): self.h.store.message(a['id'], 'assistant', t)
            found = self.h.repair_cut_texts()
            self.assertEqual([f['new'] for f in found], ['같이 확인할게요', 'The screen looks fine'])
            self.assertEqual([m['text'] for m in self.h.store.messages(a['id']) if m['role'] == 'assistant'], ['완전한 답', '같', 'The scre'])  # dry run
            self.h.repair_cut_texts(apply=True)
            ms = [m for m in self.h.store.messages(a['id']) if m['role'] == 'assistant']
            self.assertEqual([m['text'] for m in ms], ['완전한 답', '같이 확인할게요', 'The screen looks fine'])
            self.assertEqual(ms[1]['repairedFrom'], '같')
            self.assertEqual(self.h.repair_cut_texts(), [])

    def test_reviewed_repair_list_is_applied_once_and_only_where_unchanged(self):
        import json
        a = self.h.store.message(self.a['id'], 'assistant', '같'); b = self.h.store.message(self.a['id'], 'assistant', '나중에 바뀐 글')
        approved = [dict(message=a['id'], old='같', new='같이 확인할게요'), dict(message=b['id'], old='나', new='나중')]
        (self.h.directory / 'repair-approved.json').write_text(json.dumps(approved, ensure_ascii=False), encoding='utf-8')
        self.assertEqual(self.h.apply_reviewed_repairs(), 1)
        self.assertEqual([m['text'] for m in self.h.store.messages(self.a['id']) if m['role'] == 'assistant'][-2:], ['같이 확인할게요', '나중에 바뀐 글'])
        self.assertFalse((self.h.directory / 'repair-approved.json').exists())
        self.assertEqual(self.h.apply_reviewed_repairs(), 0)

    def test_repair_never_borrows_from_another_turn(self):
        # Codex RC18: an older turn's "같은 파일을 삭제했습니다." must not replace this turn's "같".
        import os
        with patch.dict(os.environ, {'CLAUDE_CONFIG_DIR': str(Path(self.tmp.name) / 'claude')}):
            a = self.h.store.update(self.a['id'], provider='claude', session='s1')
            self.write_session([('OLD TASK', ['같은 파일을 삭제했습니다.']), ('CURRENT TASK', ['같이 확인할게요'])])
            self.h.store.message(a['id'], 'user', 'CURRENT TASK'); self.h.store.message(a['id'], 'assistant', '같')
            self.assertEqual([f['new'] for f in self.h.repair_cut_texts()], ['같이 확인할게요'])
            # a turn whose session record is missing is left alone
            self.write_session([('OLD TASK', ['같은 파일을 삭제했습니다.'])])
            self.assertEqual(self.h.repair_cut_texts(), [])
            # RC19: no time on the session text, no repair
            self.write_session([('CURRENT TASK', ['같이 확인할게요'])], stamp='')
            self.assertEqual(self.h.repair_cut_texts(), [])
            # and a session text written long before the cut copy is not it
            self.write_session([('CURRENT TASK', ['같이 확인할게요'])], stamp='2020-01-01T00:00:00Z')
            self.assertEqual(self.h.repair_cut_texts(), [])

    def test_artifacts_find_angle_bracket_image_paths_with_spaces(self):
        self.h.store.message(self.a['id'], 'assistant', '![a](<D:/work/my image.png>) ![b](/D:/work/plain.png)')
        self.assertEqual(sorted(i['label'] for i in self.h.artifacts(self.a['id'])['images']), ['my image.png', 'plain.png'])

    def test_member_report_keeps_its_beginning(self):
        # live cycle 3: Luna's report reached QA starting mid-way ("packages\claude_agent_sdk\types.py ```")
        text = '요약: 세 가지 확인\n' + '근거 줄\n' * 600
        out = hub.clip_head(text, 300)
        self.assertTrue(out.startswith('요약: 세 가지 확인')); self.assertIn('뒷부분 생략', out); self.assertLess(len(out), 400)
        self.assertEqual(hub.clip_head('짧음', 300), '짧음')

    def test_clip_never_ends_inside_brackets(self):
        t = '위 기획을 바탕으로 tools/studio-dashboard/web/composer.js 에 어떻게 구현할지 코드 수준으로 제안해줘(파일 수정 금지, 20줄 이내 코드 스케치 + 바꿀 함수 이름).'
        c = hub.clip(t, 90)
        self.assertTrue(c.endswith('…')); self.assertNotIn('(', c); self.assertLessEqual(len(c), 91)
        self.assertEqual(hub.clip('짧은 문장', 90), '짧은 문장')

    def test_shell_wrapper_is_not_the_label(self):
        self.assertEqual(hub.tool_label('x', {'command': '"C:\\x\\pwsh.exe" -Command "rg -n foo"'}), 'rg -n foo')
        self.assertEqual(hub.tool_label('x', {'command': 'bash -lc "ls"'}), 'ls')

if __name__ == '__main__':
    unittest.main()
