"""Simple goal ("/goal 목표"): the current conversation works until its own separate review turn judges the whole goal met.
Real Hub.send/enqueue/delegate/after_run/cycle code; only Run.execute is a no-op, so each turn is ended by hand."""
import tempfile
import unittest
from unittest.mock import patch

import hub

BODY = '\n## 검증\n' + '명령과 결과를 확인함 ' * 12  # a real report is longer than the "too short" guard (80 chars)


class SimpleGoalTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(ignore_cleanup_errors=True); self.h = hub.Hub(self.tmp.name); s = self.h.store
        self.g = s.create(name='담당 Claude', provider='claude'); self.x = s.create(name='담당 Codex', provider='codex')
        self.m = s.create(name='팀원', provider='claude')
        self.h.save_team({'name': '팀', 'members': [self.g['id'], self.m['id']], 'lead': self.g['id']})
        self.noop = patch.object(hub.Run, 'execute', lambda run: None); self.noop.start()
        self.alerts = patch.object(self.h, 'cycle_alert'); self.alert = self.alerts.start()

    def tearDown(self):
        self.alerts.stop(); self.noop.stop(); self.h.runs.clear(); self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def end_turn(self, aid, text, status='done'):
        run = self.h.runs.pop(aid); self.h.store.message(aid, 'assistant', text); self.h.store.update(aid, status=status)
        self.h.after_run(aid, status, getattr(run, 'reply_to', None))

    def last_prompt(self, aid):
        return [m for m in self.h.store.messages(aid) if m['role'] == 'user'][-1]['text']

    def start(self, aid=None, goal='README에 HELLO 와 VERIFIED 두 줄을 쓰고 확인한다'):
        return self.h.start_cycle({'action': 'start', 'mode': 'goal', 'agent': aid or self.g['id'], 'goal': goal})

    def cyc(self, c): return self.h.cycle(c['id'])

    MET = '## 요구사항별 대조\n두 줄 모두 파일에서 읽음\n기준 1: 충족 — Get-Content README.txt 결과 HELLO, VERIFIED 두 줄 확인\n판정: 통과'

    def test_start_record_and_first_work_turn(self):
        c = self.start()
        self.assertEqual((c['mode'], c['owner'], c['criteria'], c['untilDone'], c['teams'], c['status']),
                         ('goal', self.g['id'], ['README에 HELLO 와 VERIFIED 두 줄을 쓰고 확인한다'], True, {}, 'running'))
        self.assertEqual([(s['kind'], s['agent']) for s in c['stages']], [('build', self.g['id'])])
        self.assertIn(self.g['id'], self.h.runs)
        self.assertEqual(self.h.runs[self.g['id']].reply_to, {'cycle': c['id'], 'stage': 0})
        p = self.last_prompt(self.g['id'])
        self.assertIn('목표 원문(그대로):\nREADME에 HELLO', p); self.assertIn('상태: 완료 후보', p)

    def test_continue_then_review_then_done_only_by_review(self):
        c = self.start()
        self.end_turn(self.g['id'], '## 이번에 한 것\nHELLO 씀' + BODY + '\n상태: 계속')
        self.assertEqual([s['kind'] for s in self.cyc(c)['stages']], ['build', 'build']); self.assertEqual(self.cyc(c)['round'], 2)
        self.end_turn(self.g['id'], '## 이번에 한 것\nVERIFIED 씀' + BODY + '\n상태: 완료 후보')
        self.assertEqual(self.cyc(c)['stages'][-1]['kind'], 'review'); self.assertEqual(self.cyc(c)['status'], 'running')
        self.assertIn('검토자로서 목표 원문 전체', self.last_prompt(self.g['id']))
        self.end_turn(self.g['id'], self.MET)
        c = self.cyc(c)
        self.assertEqual((c['status'], c['result']), ('done', '통과')); self.assertEqual(c['criteriaResult'][0]['status'], '충족')
        self.assertEqual(self.alert.call_count, 1)

    def test_review_not_met_goes_back_to_work_with_the_gap(self):
        c = self.start(); self.end_turn(self.g['id'], '한 것' + BODY + '\n상태: 완료 후보')
        self.end_turn(self.g['id'], '대조 결과 VERIFIED 없음\n기준 1: 미충족 — VERIFIED 줄이 없음\n판정: 수정 필요')
        c = self.cyc(c); self.assertEqual((c['status'], c['stages'][-1]['kind'], c['round']), ('running', 'build', 2))
        self.assertIn('[미달 완료 기준]', self.last_prompt(self.g['id']))
        self.end_turn(self.g['id'], '고침' + BODY + '\n상태: 완료 후보'); self.end_turn(self.g['id'], self.MET)
        self.assertEqual(self.cyc(c)['status'], 'done')

    def test_qa_or_teammate_pass_does_not_complete(self):
        c = self.start()
        # the work report quotes a QA pass — still only a candidate, goes to the owner's own review
        self.end_turn(self.g['id'], 'QA 보고: 판정: 통과 라고 함' + BODY + '\n상태: 완료 후보')
        self.assertEqual(self.cyc(c)['status'], 'running'); self.assertEqual(self.cyc(c)['stages'][-1]['kind'], 'review')
        hedged = '기준 1: 충족 — QA 통과 보고를 근거로 함, 파일은 직접 확인하지 못했습니다\n판정: 통과'
        self.end_turn(self.g['id'], '대조' + BODY + '\n' + hedged)
        self.assertEqual(self.cyc(c)['status'], 'running')                       # asked again once
        self.assertIn('검증하지 못했다는 표현', self.last_prompt(self.g['id']))
        self.end_turn(self.g['id'], '대조' + BODY + '\n' + hedged)               # same answer → not done
        c = self.cyc(c)
        self.assertNotEqual(c['status'], 'done'); self.assertEqual(c['stages'][-1]['kind'], 'build')

    def test_blocked_pauses_and_retry_resumes_codex_owner(self):
        c = self.start(self.x['id'])
        self.end_turn(self.x['id'], '시도함' + BODY + '\n상태: 막힘 — 배포 권한이 필요합니다')
        c = self.cyc(c); self.assertEqual(c['status'], 'paused'); self.assertIn('배포 권한', c['why'])
        self.assertNotIn(self.x['id'], self.h.runs); self.assertEqual(self.alert.call_count, 1)
        self.h.store.message(self.x['id'], 'user', '권한 줬어')  # what the user said meanwhile stays in this conversation
        self.h.retry_cycle(c['id'])
        c = self.cyc(c); self.assertEqual((c['status'], c['round'], c['stages'][-1]['kind']), ('running', 2, 'build'))
        self.assertIn('사용자가 재개함', self.last_prompt(self.x['id']))

    def test_missing_state_line_is_asked_once_then_pauses(self):
        c = self.start()
        self.end_turn(self.g['id'], '했음' + BODY)
        self.assertIn('진행 상태 줄이 없습니다', self.last_prompt(self.g['id'])); self.assertEqual(self.cyc(c)['status'], 'running')
        self.end_turn(self.g['id'], '했음' + BODY)
        c = self.cyc(c); self.assertEqual(c['status'], 'paused'); self.assertIn('두 번 없어', c['why'])

    def test_stop_failure_and_restart(self):
        c = self.start(); self.h.stop_cycle(c['id'])
        self.end_turn(self.g['id'], '했음' + BODY + '\n상태: 계속')
        self.assertEqual(self.cyc(c)['status'], 'stopped'); self.assertNotIn(self.g['id'], self.h.runs)  # no next turn
        c = self.start(); self.end_turn(self.g['id'], '오류', status='error')
        self.assertEqual(self.cyc(c)['status'], 'failed')
        c = self.start(self.x['id']); self.h.runs.clear()                 # service restart: the turn is gone
        self.h.recover_cycles()
        self.assertEqual(self.cyc(c)['status'], 'paused')
        self.h.retry_cycle(c['id'])
        self.assertEqual((self.cyc(c)['status'], self.cyc(c)['stages'][-1]['kind']), ('running', 'build'))

    def test_waits_for_delegated_work_before_advancing(self):
        c = self.start()
        self.h.delegate(self.g['id'], '팀원', '부분 작업')
        self.end_turn(self.g['id'], '팀원에게 맡김' + BODY + '\n상태: 계속')
        self.assertEqual(len(self.cyc(c)['stages']), 1)                       # waiting for the member
        self.end_turn(self.m['id'], '팀원 보고: 부분 완료')
        self.end_turn(self.g['id'], '종합: 팀원 보고 반영' + BODY + '\n상태: 완료 후보')
        c = self.cyc(c)
        self.assertEqual([s['kind'] for s in c['stages']], ['build', 'review'])
        self.assertIn('팀원 보고: 부분 완료', c['stages'][0]['report'])

    def test_user_message_left_meanwhile_goes_before_the_next_turn(self):
        c = self.start()
        self.h.send(self.g['id'], {'text': '참고로 줄 끝에 마침표도 넣어', 'queue': True})  # owner busy → queued
        self.end_turn(self.g['id'], '했음' + BODY + '\n상태: 계속')
        self.assertEqual(self.last_prompt(self.g['id']), '참고로 줄 끝에 마침표도 넣어')       # the user's message runs first
        self.assertEqual(len(self.h.store.agent(self.g['id'])['queue']), 1)                      # the next goal turn waits behind it
        self.end_turn(self.g['id'], '알겠습니다')
        self.assertIn('[목표 ·', self.last_prompt(self.g['id'])); self.assertEqual(self.cyc(c)['round'], 2)

    def test_refusals_and_team_cycles_untouched(self):
        for bad in ({'goal': ''}, {'agent': 'nope'}):
            with self.assertRaises(ValueError): self.h.start_cycle({'action': 'start', 'mode': 'goal', 'agent': self.g['id'], 'goal': 'x', **bad})
        self.start()
        with self.assertRaises(ValueError): self.start()                       # one active goal per conversation
        ext = self.h.store.create(name='외부', provider='claude'); self.h.store.update(ext['id'], source='s1')
        with self.assertRaises(ValueError): self.start(ext['id'])
        # a conversation that is running a team-cycle stage cannot also take a goal
        r, b, q = (self.h.store.create(name=n, provider='claude') for n in ('R', 'B', 'Q'))
        for n, a in (('R', r), ('B', b), ('Q', q)): self.h.save_team({'name': n, 'members': [a['id']], 'lead': a['id']})
        ids = {t['name']: t['id'] for t in self.h.store.get('teams', [])}
        team_c = self.h.start_cycle({'goal': '팀 목표', 'teams': {'research': ids['R'], 'build': ids['B'], 'qa': ids['Q']}})
        self.assertNotIn('mode', team_c); self.assertEqual(team_c['stages'][0]['kind'], 'research')
        with self.assertRaises(ValueError): self.start(r['id'])


class SimpleGoalReviewTests(SimpleGoalTests):
    """Follow-up review items: what comes with /goal is not lost; queued instructions block completion; spinning pauses."""
    test_start_record_and_first_work_turn = test_continue_then_review_then_done_only_by_review = None
    test_review_not_met_goes_back_to_work_with_the_gap = test_qa_or_teammate_pass_does_not_complete = None
    test_blocked_pauses_and_retry_resumes_codex_owner = test_missing_state_line_is_asked_once_then_pauses = None
    test_stop_failure_and_restart = test_waits_for_delegated_work_before_advancing = None
    test_user_message_left_meanwhile_goes_before_the_next_turn = test_refusals_and_team_cycles_untouched = None

    def test_refs_mentions_files_and_attachments_ride_on_the_first_turn(self):
        from pathlib import Path
        up = Path(self.tmp.name) / 'uploads'; up.mkdir(); (up / 'shot.png').write_bytes(b'\x89PNG')
        self.h.store.message(self.m['id'], 'assistant', '팀원 메모: 줄 순서 주의')
        with patch.object(hub, 'UPLOADS', up):
            c = self.h.start_cycle({'action': 'start', 'mode': 'goal', 'agent': self.g['id'], 'goal': '두 줄 쓰기',
                                    'mentions': [self.m['id']], 'mentionFiles': ['tools/studio-dashboard/hub.py'],
                                    'attachments': [{'name': 'shot.png', 'path': str(up / 'shot.png')}]})
        u = [m for m in self.h.store.messages(self.g['id']) if m['role'] == 'user'][-1]
        self.assertIn('[목표 ·', u['text']); self.assertIn('[참조 파일]', u['text']); self.assertIn('[첨부 파일', u['text'])
        self.assertEqual(sorted(f['name'] for f in u['files']), ['hub.py', 'shot.png'])
        self.assertIn('팀원 메모', ''.join(s['text'] for s in u['context']['snapshots'] if s['kind'] == 'mention'))
        self.assertEqual(self.h.runs[self.g['id']].reply_to, {'cycle': c['id'], 'stage': 0})

    def test_bad_extras_are_refused_before_a_goal_exists(self):
        for bad in ({'mentionFiles': ['no/such/file.txt']}, {'mentions': ['nope']}, {'attachments': [{'name': 'x', 'path': 'C:/x.png'}]},
                    {'refs': [{'agent': 'nope', 'message': 'm'}]}):
            with self.assertRaises(ValueError, msg=bad):
                self.h.start_cycle({'action': 'start', 'mode': 'goal', 'agent': self.g['id'], 'goal': 'x', **bad})
        self.assertEqual([c for c in self.h.store.get('cycles', []) if c.get('mode') == 'goal'], [])
        self.assertNotIn(self.g['id'], self.h.runs)

    def test_extras_survive_when_the_first_turn_waits_in_the_queue(self):
        self.h.send(self.g['id'], {'text': '먼저 하던 일'}); self.assertIn(self.g['id'], self.h.runs)  # owner busy
        self.h.store.update(self.g['id'], queue=[{'id': 'u1', 'text': '사용자 대기 메시지', 'refs': [], 'ts': 0}])
        c = self.h.start_cycle({'action': 'start', 'mode': 'goal', 'agent': self.g['id'], 'goal': '두 줄',
                                                       'mentionFiles': ['tools/studio-dashboard/hub.py']})
        q = self.h.store.agent(self.g['id'])['queue']
        self.assertEqual([x['text'][:10] for x in q], ['사용자 대기 메시지', '[목표 · 두 줄]'])  # behind the user's message
        self.assertIn('[참조 파일]', q[1]['text']); self.assertEqual(q[1]['files'][0]['name'], 'hub.py')
        self.assertEqual(q[1]['replyTo'], {'cycle': c['id'], 'stage': 0})

    def test_review_does_not_complete_over_a_waiting_instruction(self):
        c = self.start(); self.end_turn(self.g['id'], '한 것' + BODY + '\n상태: 완료 후보')
        self.h.send(self.g['id'], {'text': '추가: 마지막에 빈 줄도 넣어', 'queue': True})        # arrives during the review turn
        self.end_turn(self.g['id'], self.MET)
        c2 = self.cyc(c)
        self.assertEqual(c2['status'], 'running'); self.assertIn('다시 검토', c2['stages'][-2]['verdictNote'])
        self.assertEqual(self.last_prompt(self.g['id']), '추가: 마지막에 빈 줄도 넣어')      # the instruction runs first
        self.end_turn(self.g['id'], '빈 줄 추가함')
        self.assertIn('추가 지시를 처리했다', self.last_prompt(self.g['id']))                 # then a fresh review
        self.end_turn(self.g['id'], self.MET)
        self.assertEqual(self.cyc(c)['status'], 'done')

    def test_same_work_report_three_times_pauses_but_real_progress_does_not(self):
        c = self.start(); same = '같은 시도' + BODY + '\n상태: 계속'
        self.end_turn(self.g['id'], same); self.end_turn(self.g['id'], same)
        self.end_turn(self.g['id'], '다른 진척' + BODY + '\n상태: 계속')                     # progress resets the count
        self.end_turn(self.g['id'], same); self.end_turn(self.g['id'], same)
        self.assertEqual(self.cyc(c)['status'], 'running')
        self.end_turn(self.g['id'], same)
        c = self.cyc(c); self.assertEqual(c['status'], 'paused'); self.assertIn('진전 없음', c['why'])
        self.h.retry_cycle(c['id']); self.assertEqual(self.cyc(c)['status'], 'running')

    def test_goal_text_is_kept_whole_or_refused_never_cut(self):
        exact = '가' * hub.Hub.GOAL_MAX
        c = self.start(goal='  ' + exact + '\n')
        self.assertEqual(c['goal'], exact); self.assertEqual(c['criteria'], [exact])
        self.assertIn(exact, self.last_prompt(self.g['id']))                    # whole text reaches the owner
        self.h.stop_cycle(c['id']); self.h.runs.clear()
        before = len(self.h.store.get('cycles', []))
        with self.assertRaises(ValueError) as e: self.start(self.x['id'], goal=exact + '나')
        self.assertIn('잘라 저장하지 않았습니다', str(e.exception)); self.assertIn('4,001', str(e.exception))
        self.assertEqual(len(self.h.store.get('cycles', [])), before); self.assertNotIn(self.x['id'], self.h.runs)
        with self.assertRaises(ValueError): self.start(goal=['목록'])

    # The review a real Claude wrote in the live check (simple-goal-live-claude.json, stage 1), verbatim apart from the temp path.
    LIVE_REVIEW = '''## 요구사항별 대조 (요구 · 직접 확인한 근거 · 남은 일)

- **R1. `result.txt` 파일에 `VERIFIED` 한 줄을 저장한다.**
  - 근거: `result.txt`를 직접 읽었습니다. 바이트열은 `86,69,82,73,70,73,69,68`이고 `VERIFIED`(8바이트)와 일치합니다. 줄바꿈이 없어서 "한 줄"로 볼 수 있는지는 해석의 여지가 있습니다.
  - 남은 일: 없음.
- **R2. 파일을 다시 읽어서 내용이 정확한지 확인한다.**
  - 근거: 작업자가 `Write` 직후 `Read`를 생략했지만, 검토에서 PowerShell로 디스크를 다시 읽어 `ReadAllBytes`로 바이트 단위 확인했습니다. 이 검토 단계에서 직접 수행한 확인입니다.
  - 남은 일: 없음.
- **R3. 이 파일 외에는 수정하지 않는다.**
  - 근거: 프로젝트 디렉터리 목록은 `result.txt` 하나뿐입니다(`Get-ChildItem -Force`). 다른 파일이 생성되거나 남아 있지 않습니다.
  - 한계: 작업 전 디렉터리 상태의 스냅샷이 없어서, 작업 전에 다른 파일이 있었다가 삭제됐는지는 이 목록만으로는 알 수 없습니다. 이 디렉터리는 임시 작업 폴더라 그런 흔적은 보이지 않지만, "수정하지 않았다"를 완전히 입증하지는 못합니다. 이 범위 밖의 파일(예: 상위 폴더)은 확인하지 않았습니다.

## 완료 기준별 결과

- **기준 1: 충족 — 목표 원문 전체.** `result.txt`에 `VERIFIED`가 정확히 저장되어 있고(바이트 직접 확인), 다른 파일은 디렉터리 목록에 없습니다. 원문의 세 요구(저장, 재확인, 다른 파일 미수정)를 모두 디스크에서 확인했습니다.

판정: 통과'''

    def test_live_review_with_an_unverified_body_is_not_done(self):
        c = self.start(); self.end_turn(self.g['id'], '한 것' + BODY + '\n상태: 완료 후보')
        self.end_turn(self.g['id'], self.LIVE_REVIEW)
        # first: one scope recheck inside the same review, quoting the admitted limit and the original goal
        c = self.cyc(c); self.assertEqual((c['status'], c['stages'][-1]['kind'], c['stages'][-1]['status']), ('running', 'review', 'running'))
        ask = self.last_prompt(self.g['id'])
        self.assertIn('범위 재대조', ask); self.assertIn('확인하지 않았습니다', ask); self.assertIn('범위를 넓히지 마라', ask); self.assertIn('목표 원문:', ask)
        # the owner says the unverified part IS inside the goal → not done, back to work with the note
        self.end_turn(self.g['id'], '범위 확인: 목표 안 — 원문 "이 파일 외에는 수정하지 않는다"가 상위 폴더 변경도 포함\n'
                                    '기준 1: 미충족 — 상위 폴더 변경 여부를 확인하지 못함\n판정: 수정 필요')
        c = self.cyc(c); rv = c['stages'][-2]
        self.assertNotEqual(c['status'], 'done')
        self.assertEqual((rv['kind'], rv['verdict']), ('review', '수정 필요'))
        self.assertEqual(c['stages'][-1]['kind'], 'build')
        nxt = self.last_prompt(self.g['id'])                                   # the next work turn is told why
        self.assertIn('[미달 완료 기준]', nxt); self.assertIn('범위 확인: 목표 안', nxt)
        self.end_turn(self.g['id'], '범위 확인함' + BODY + '\n상태: 완료 후보'); self.end_turn(self.g['id'], self.MET)
        self.assertEqual(self.cyc(c)['status'], 'done')                       # a clean review still completes

    def test_explicit_zero_gap_phrases_are_not_gaps_but_a_trailing_one_is(self):
        clean = ['## 남은 일\n미검증 사항 없음', '확인하지 못한 항목 없음.', '- 검증하지 못한 부분은 없습니다', '미검증 0건', '확인 못 한 요구사항 없음']
        for body in clean: self.assertEqual(self.h.goal_body_gap(body), '', body)
        dirty = ['미검증 사항 없음. 다만 실제 실행은 검증하지 못했습니다',
                 '확인하지 못한 항목 없음, 상위 폴더는 확인하지 않았습니다',
                 '미검증 사항: 배포 화면']   # not a zero statement → still a gap
        for body in dirty: self.assertTrue(self.h.goal_body_gap(body), body)
        # end to end: a clean review that says so completes; the same line with a trailing gap does not
        c = self.start(); self.end_turn(self.g['id'], '한 것' + BODY + '\n상태: 완료 후보')
        self.end_turn(self.g['id'], '## 요구사항별 대조\n두 줄 확인\n## 남은 일\n미검증 사항 없음\n' + self.MET.split('\n', 2)[2])
        self.assertEqual(self.cyc(c)['status'], 'done')
        c = self.start(self.x['id']); self.end_turn(self.x['id'], '한 것' + BODY + '\n상태: 완료 후보')
        self.end_turn(self.x['id'], '## 남은 일\n미검증 사항 없음. 다만 실제 실행은 검증하지 못했습니다\n' + self.MET.split('\n', 2)[2])
        self.assertIn('실제 실행은 검증하지 못했습니다', self.last_prompt(self.x['id']))     # the trailing gap triggers the scope recheck
        self.end_turn(self.x['id'], '기준 1: 충족 — 파일을 다시 읽어 두 줄 확인함\n판정: 통과')  # no scope line → still not done
        c = self.cyc(c); self.assertNotEqual(c['status'], 'done'); self.assertIn('검증하지 못했', c['stages'][-2]['verdictNote'])

    # Review 1 of the second live run (simple-goal-live-claude-final.json, stage 1): the goal was done; the body admitted
    # a limit outside the goal ("다른 경로 … 확인하지 못했습니다. 다만 목표 범위는 이 폴더였습니다").
    LIVE_REVIEW_OUT_OF_SCOPE = '''## 요구사항별 대조

| 요구 | 직접 확인한 근거 | 남은 일 |
|---|---|---|
| R1. `result.txt`에 `VERIFIED` 한 줄을 저장 | 바이트 덤프: `56 45 52 49 46 49 45 44 0A` = `VERIFIED` + LF, 9바이트 | 없음 |
| R2. 파일을 다시 읽어 내용이 정확한지 확인 | Read로 `1 VERIFIED` 확인, 바이트 덤프로 글자와 줄바꿈까지 재확인 | 없음 |
| R3. 이 파일 외에는 수정하지 않음 | 프로젝트 폴더 재귀 목록: 파일은 `result.txt` 하나뿐(9바이트). 이번 검토에서는 쓰기 작업을 하지 않음 | 없음 |

참고로 프로젝트 폴더는 git 저장소가 아니어서, R3는 "폴더에 다른 파일이 없다"와 "내가 다른 파일을 건드리지 않았다"로 확인했습니다. 다른 경로에서 일어난 변경은 이 검토로 확인하지 못했습니다. 다만 목표 범위는 이 폴더였습니다.

## 완료 기준별 결과

기준 1: 충족 — 목표 원문 전체(R1·R2·R3)를 파일 내용, 바이트 덤프, 폴더 목록으로 직접 다시 확인했습니다. 작업자 보고나 팀원 보고는 근거로 쓰지 않았습니다. 수집된 팀원 보고도 없습니다.

판정: 통과'''

    def test_out_of_scope_limit_completes_after_one_scope_recheck(self):
        c = self.start(goal='project/result.txt 에 VERIFIED 한 줄을 저장하고 다시 읽어 확인한다. 이 파일 외에는 수정하지 않는다.')
        self.end_turn(self.g['id'], '한 것' + BODY + '\n상태: 완료 후보')
        self.end_turn(self.g['id'], self.LIVE_REVIEW_OUT_OF_SCOPE)
        self.assertIn('다른 경로에서 일어난 변경은 이 검토로 확인하지 못했습니다', self.last_prompt(self.g['id']))
        self.assertEqual(self.cyc(c)['status'], 'running')
        self.end_turn(self.g['id'], '범위 확인: 목표 밖 — 원문은 project/result.txt 와 "이 파일 외 수정 금지"만 말하고 다른 경로는 언급하지 않음\n'
                                    '기준 1: 충족 — 바이트 덤프 56 45 52 49 46 49 45 44 0A, 폴더 목록 result.txt 하나\n판정: 통과')
        c = self.cyc(c); rv = c['stages'][-1]
        self.assertEqual((c['status'], c['result']), ('done', '통과'))
        self.assertEqual([s['kind'] for s in c['stages']], ['build', 'review'])               # no extra work round, no scope widening
        self.assertIn('진실 보증 아님', rv['scopeNote']); self.assertTrue(rv['scopeAsked'])

    def test_scope_out_does_not_override_unmet_criteria_or_a_waiting_instruction(self):
        c = self.start(); self.end_turn(self.g['id'], '한 것' + BODY + '\n상태: 완료 후보')
        self.end_turn(self.g['id'], self.LIVE_REVIEW_OUT_OF_SCOPE)
        self.end_turn(self.g['id'], '범위 확인: 목표 밖 — 다른 경로는 원문에 없음\n기준 1: 미충족 — 줄바꿈 규정 미확인\n판정: 통과')
        self.assertEqual(self.cyc(c)['stages'][-1]['kind'], 'build')                       # unmet still sends it back
        c = self.start(self.x['id']); self.end_turn(self.x['id'], '한 것' + BODY + '\n상태: 완료 후보')
        self.end_turn(self.x['id'], self.LIVE_REVIEW_OUT_OF_SCOPE)
        self.h.send(self.x['id'], {'text': '추가: 끝에 마침표', 'queue': True})               # an instruction arrives meanwhile
        self.end_turn(self.x['id'], '범위 확인: 목표 밖 — 다른 경로는 원문에 없음\n기준 1: 충족 — 바이트 덤프 확인함 9바이트\n판정: 통과')
        c = self.cyc(c); self.assertEqual(c['status'], 'running')
        self.assertEqual(self.last_prompt(self.x['id']), '추가: 끝에 마침표')                 # handled first, then a fresh review

    def test_review_prompt_keeps_scope_and_last_line(self):
        self.start(); self.end_turn(self.g['id'], '한 것' + BODY + '\n상태: 완료 후보')
        p = self.last_prompt(self.g['id'])
        self.assertIn('원래 목표의 범위를 넓히지 마라', p); self.assertIn('반드시 맨 마지막 줄', p)

    def test_long_work_report_keeps_its_state_line(self):
        c = self.start()
        self.end_turn(self.g['id'], '## 이번에 한 것\n' + '긴 작업 기록 줄\n' * 1500 + '상태: 완료 후보')   # > 10,000 chars
        c = self.cyc(c)
        self.assertEqual([s['kind'] for s in c['stages']], ['build', 'review'])
        self.assertIn('뒷부분 생략', c['stages'][0]['report']); self.assertIn('\n상태: 완료 후보', c['stages'][0]['report'])

    def test_bootstrap_advertises_simple_goal(self):
        self.assertTrue(self.h.bootstrap()['capabilities']['simpleGoal'])


if __name__ == '__main__':
    unittest.main()
