"""Team goal = cycle with completion criteria; team-managed Codex runs never also run Codex's own goal loop."""
import queue
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import hub


class CycleBase(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(ignore_cleanup_errors=True); self.h = hub.Hub(self.tmp.name); s = self.h.store
        mk = lambda n: s.create(name=n, provider='claude')
        self.r, self.b, self.q = mk('리서치 팀장'), mk('개발 팀장'), mk('QA 팀장')
        t = lambda n, a: self.h.save_team({'name': n, 'members': [a['id']], 'lead': a['id']})
        self.tr, self.tb, self.tq = t('리서치', self.r), t('개발', self.b), t('QA', self.q)
        self.sent = []
        self.send = patch.object(self.h, 'send', side_effect=lambda aid, d: self.sent.append((aid, d))); self.send.start()
        self.alerts = patch.object(self.h, 'cycle_alert'); self.cycle_alert = self.alerts.start()

    def tearDown(self):
        self.alerts.stop(); self.send.stop(); self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    BODY = '근거와 검증: ' + '확인한내용' * 20

    def finish(self, aid, text):
        _, d = self.sent[-1]
        self.h.store.message(aid, 'user', d['text']); self.h.store.message(aid, 'assistant', self.BODY + '\n' + text)
        self.h.after_run(aid, 'done', d.get('replyTo'))

    def start(self, criteria='- 테스트 전부 통과\n2) 목표 칩이 사이클 상태를 보여 준다', rounds=2):
        return self.h.start_cycle({'goal': '팀 목표 완료', 'rounds': rounds, 'criteria': criteria,
                                   'teams': {'research': self.tr['id'], 'build': self.tb['id'], 'qa': self.tq['id']}})

    def to_qa(self):
        c = self.start(); self.finish(self.r['id'], '조사'); self.finish(self.b['id'], '구현')
        self.assertEqual(self.sent[-1][0], self.q['id']); return c


class CriteriaTests(CycleBase):
    def test_criteria_are_stored_and_shown_to_every_stage(self):
        c = self.start()
        self.assertEqual(c['criteria'], ['테스트 전부 통과', '목표 칩이 사이클 상태를 보여 준다'])
        self.assertIn('1. 테스트 전부 통과', self.sent[-1][1]['text'])
        self.finish(self.r['id'], '조사'); self.finish(self.b['id'], '구현')
        qa = self.sent[-1][1]['text']
        self.assertIn('2. 목표 칩이 사이클 상태를 보여 준다', qa); self.assertIn('기준 N: 충족 — 근거', qa)

    def test_all_criteria_met_with_evidence_completes(self):
        c = self.to_qa()
        self.finish(self.q['id'], '기준 1: 충족 — python -m unittest 204 OK\n기준 2: 충족 — 7881 화면에서 칩 문구 확인\n판정: 통과')
        c = self.h.cycle(c['id'])
        self.assertEqual((c['status'], c['result']), ('done', '통과'))
        self.assertEqual([r['status'] for r in c['criteriaResult']], ['충족', '충족'])

    def test_pass_with_an_unmet_criterion_is_not_done(self):
        c = self.to_qa()
        self.finish(self.q['id'], '기준 1: 충족 — python -m unittest 204 OK\n기준 2: 미충족 — 칩이 안 바뀜\n판정: 통과')
        c = self.h.cycle(c['id']); qa = c['stages'][2]
        self.assertEqual((c['status'], qa['verdict']), ('running', '수정 필요'))
        self.assertIn('기준 2 미충족', qa['verdictNote'])
        self.assertEqual(self.sent[-1][0], self.b['id'])  # back to build, round 2
        self.assertIn('[미달 완료 기준]', self.sent[-1][1]['text'])
        self.assertEqual(self.cycle_alert.call_count, 0)

    def test_pass_without_criterion_lines_is_asked_once_then_counted_as_fix(self):
        c = self.to_qa()
        self.finish(self.q['id'], '이상 없음\n판정: 통과')
        self.assertEqual(self.sent[-1][0], self.q['id'])  # asked again, same stage
        self.assertIn('기준 1, 기준 2', self.sent[-1][1]['text'])
        self.assertEqual(self.h.cycle(c['id'])['status'], 'running')
        self.finish(self.q['id'], '기준 1: 충족\n기준 2: 충족\n판정: 통과')  # "met" with no evidence
        c = self.h.cycle(c['id'])
        self.assertEqual(c['stages'][2]['verdict'], '수정 필요')
        self.assertEqual([r['status'] for r in c['stages'][2]['criteria']], ['근거 없음', '근거 없음'])
        self.assertEqual(self.sent[-1][0], self.b['id'])

    def test_round_limit_pauses_with_criteria_result(self):
        c = self.start(rounds=1); self.finish(self.r['id'], '조사'); self.finish(self.b['id'], '구현')
        self.finish(self.q['id'], '기준 1: 미충족 — 2건 실패\n기준 2: 충족 — 칩 문구 화면 확인함\n판정: 수정 필요')
        c = self.h.cycle(c['id'])
        self.assertEqual((c['status'], c['result']), ('paused', '수정 필요'))
        self.assertEqual(c['criteriaResult'][0]['status'], '미충족'); self.assertEqual(self.cycle_alert.call_count, 1)

    def test_explicit_unmet_skips_the_evidence_reask(self):
        c = self.to_qa()
        self.finish(self.q['id'], '기준 1: 미충족 — 2건 실패\n기준 2: 충족\n판정: 통과')  # 기준 2 has no evidence, but 기준 1 already fails
        c = self.h.cycle(c['id']); qa = c['stages'][2]
        self.assertEqual(self.sent[-1][0], self.b['id'])  # straight back to build, QA was not asked again
        self.assertNotIn('retried', qa)
        self.assertEqual((qa['verdict'], c['round']), ('수정 필요', 2))
        self.assertEqual([r['status'] for r in qa['criteria']], ['미충족', '근거 없음'])
        self.assertIn('기준 1 (미충족)', self.sent[-1][1]['text']); self.assertIn('기준 2 (근거 없음)', self.sent[-1][1]['text'])

    def test_resume_after_round_cap_keeps_unmet_criteria_for_build(self):
        c = self.start(rounds=1); self.finish(self.r['id'], '조사'); self.finish(self.b['id'], '구현')
        self.finish(self.q['id'], '기준 1: 미충족 — 2건 실패\n기준 2: 충족 — 칩 문구 화면 확인함\n판정: 수정 필요')
        self.assertEqual(self.h.cycle(c['id'])['status'], 'paused')
        self.h.retry_cycle(c['id'])
        aid, d = self.sent[-1]
        self.assertEqual(aid, self.b['id'])
        given = d['text'].split('[QA 지적]\n', 1)[1]
        self.assertTrue(given.startswith('[미달 완료 기준]\n기준 1 (미충족): 테스트 전부 통과\n'))
        self.assertNotIn('기준 2 (', given.split('\n\n', 1)[0])  # a met criterion is not listed as unmet
        self.assertIn('판정: 수정 필요', given)  # QA's own report still follows

    def test_without_criteria_nothing_changes(self):
        c = self.start(criteria=None)
        self.assertNotIn('criteria', c); self.assertNotIn('완료 기준 (전부', self.sent[-1][1]['text'])
        self.finish(self.r['id'], '조사'); self.finish(self.b['id'], '구현'); self.finish(self.q['id'], '판정: 통과')
        self.assertEqual(self.h.cycle(c['id'])['status'], 'done')

    def test_bad_criteria_are_refused_before_starting(self):
        for bad in (['a'], '\n'.join(f'기준{i}' for i in range(13))):
            with self.assertRaises(ValueError): self.start(criteria=bad)
        self.assertEqual(self.sent, [])


class UntilDoneTests(CycleBase):
    """untilDone: no fixed round cap — QA "수정 필요" always sends it back to build; only all-criteria-met + QA pass ends it."""
    MET = '기준 1: 충족 — python -m unittest 215 OK\n기준 2: 충족 — 7881 화면에서 칩 문구 확인\n판정: 통과'
    FIX = '기준 1: 미충족 — 2건 실패\n기준 2: 충족 — 7881 화면에서 칩 문구 확인\n판정: 수정 필요'

    def start_until(self, **extra):
        return self.h.start_cycle({'goal': '팀 목표 완료', 'rounds': 1, 'untilDone': True, 'criteria': '- 테스트 전부 통과\n- 목표 칩이 사이클 상태를 보여 준다',
                                   'teams': {'research': self.tr['id'], 'build': self.tb['id'], 'qa': self.tq['id']}, **extra})

    def test_keeps_going_past_round_four_until_pass(self):
        c = self.start_until(); self.assertTrue(c['untilDone'])
        self.assertIn('통과할 때까지 반복', self.sent[-1][1]['text'])
        self.finish(self.r['id'], '조사')
        for rnd in range(1, 5):  # rounds=1 would have paused after the first QA
            self.finish(self.b['id'], f'{rnd}회차 구현'); self.finish(self.q['id'], self.FIX)
            c = self.h.cycle(c['id'])
            self.assertEqual((c['status'], c['round']), ('running', rnd + 1)); self.assertNotIn('roundLimitReached', c)
            self.assertEqual(self.sent[-1][0], self.b['id']); self.assertIn('[미달 완료 기준]', self.sent[-1][1]['text'])
        self.finish(self.b['id'], '5회차 구현'); self.finish(self.q['id'], self.MET)
        c = self.h.cycle(c['id'])
        self.assertEqual((c['status'], c['result'], c['round']), ('done', '통과', 5))
        self.assertEqual([s['kind'] for s in c['stages']].count('qa'), 5)
        self.assertEqual(self.cycle_alert.call_count, 1)  # still one alert, at the end

    def test_pass_with_unmet_criterion_keeps_going_not_done(self):
        c = self.start_until(); self.finish(self.r['id'], '조사'); self.finish(self.b['id'], '구현')
        self.finish(self.q['id'], '기준 1: 미충족 — 2건 실패\n기준 2: 충족 — 7881 화면에서 칩 문구 확인\n판정: 통과')
        c = self.h.cycle(c['id']); self.assertEqual((c['status'], c['round']), ('running', 2))
        self.assertEqual(self.sent[-1][0], self.b['id'])

    def test_user_stop_is_not_continued(self):
        c = self.start_until(); self.finish(self.r['id'], '조사')
        for _ in range(3): self.finish(self.b['id'], '구현'); self.finish(self.q['id'], self.FIX)
        self.h.stop_cycle(c['id'])  # stopped while round 4's build is out
        n = len(self.sent); self.finish(self.b['id'], '4회차 구현')  # the build run still ends after the stop
        c = self.h.cycle(c['id'])
        self.assertEqual(c['status'], 'stopped'); self.assertEqual(len(self.sent), n)  # no QA, no next round
        self.assertEqual(c['round'], 4)

    def test_failure_and_restart_still_halt_it(self):
        c = self.start_until(); self.finish(self.r['id'], '조사')
        _, d = self.sent[-1]; self.h.after_run(self.b['id'], 'error', d.get('replyTo'))
        self.assertEqual(self.h.cycle(c['id'])['status'], 'failed')
        c = self.start_until(); self.h.recover_cycles()  # its research message is gone from every queue (send is mocked)
        self.assertEqual(self.h.cycle(c['id'])['status'], 'paused')
        n = len(self.sent); self.h.retry_cycle(c['id'])
        self.assertEqual((len(self.sent), self.sent[-1][0], self.h.cycle(c['id'])['status']), (n + 1, self.r['id'], 'running'))
        self.assertTrue(self.h.cycle(c['id'])['untilDone'])

    def test_until_done_needs_criteria_and_a_real_bool(self):
        for extra in ({'criteria': ''}, {'untilDone': 'yes'}):
            with self.assertRaises(ValueError): self.start_until(**extra)
        self.assertEqual(self.sent, [])
        c = self.start(rounds=1)  # default stays the old capped behaviour
        self.assertNotIn('untilDone', c)


class VerificationGapTests(CycleBase):
    """Cycle cc0edf66 completed although QA's own '충족' lines said parts were not verified. The wording check is an
    auxiliary device (not a truth guarantee): it sends such a pass back once, then counts it as 수정 필요."""
    CRITERIA = '\n'.join([
        '팀원 위임과 보고의 출처가 양쪽 대화 및 협업 뷰에서 식별되고, 실제 인계 증거가 남는다.',
        '동일 fixture의 1440/1280/390 밝은·어두운 6조건 전후 캡처를 비교해 개선하고 잘림·가로 넘침·페이지 오류가 없다.',
        '/ 명령·팀 목표 적용·진행·대기·완료 상태가 실제 서버 상태와 일치하며 재작업 후 완료 기준별 QA 근거를 볼 수 있다.',
        'G1 이미지/G2 명령/G3 멘션/G6 ACP에 대한 Claude 독립 회귀 검토와 변경 관련 테스트가 통과한다.',
        '개발자와 다른 QA가 실제 결과를 검사하고 self-upgrade-result.md 및 codex-impl.md R3에 근거·변경 파일·남은 제한을 기록한다.'])
    # verbatim from the production QA report of cycle cc0edf66 (stages[2].report, lines 29-37)
    REAL_QA = '''## 완료 기준별 결과
- 기준 1: 충족 — 운영 실데이터 증거 JSON에 outgoing·delegations·incoming(`kind:handoff`와 보낸 팀 이름)·flow 양방향이 있고, 쓰기 0·오류 0입니다. Claude 검토도 팀장 쪽 "↗ 일을 맡김/← 결과를 넘겨받음"과 팀원 쪽 "팀장이 맡긴 일" 표시를 확인했습니다.
- 기준 2: 충족 — 전후 6조건 24장과 측정 JSON을 대조했습니다. 후 6조건은 가로 넘침 false, 오류 `[]`, 개선 항목(이벤트·대기 표시·잘못된 안내 제거)이 모두 true입니다. 캡처 2장은 직접 봤고 잘림이 없습니다. 나머지 22장은 측정값에만 근거합니다.
- 기준 3: 충족(간접 근거 포함) — 서빙 파일 diff 0, 보드와 막대에서 "보고 대기"·진행·완료·"수정 필요"가 보이고 기준별 충족/미충족 근거를 볼 수 있습니다. 위 결함 2 때문에 실제 대기 상태 대조는 하지 못했습니다.
- 기준 4: 충족 — Claude 독립 회귀(G1·G2·G3·G4·G6 통과)와 변경 관련 테스트(Python 223, Node 13)를 직접 재실행해 통과했습니다. 마지막 수정분은 결함 1대로 독립 코드 리뷰 밖입니다.
- 기준 5: 충족 — 개발자와 다른 QA인 내가 실제 결과를 검사했고, `self-upgrade-result.md`와 `codex-impl.md` R3에 근거·변경 파일·남은 제한이 기록돼 있습니다.

이번 QA 1회차는 기준 5개가 모두 충족이고 막는 결함이 없습니다. 위 낮음 3건은 다음 사이클로 넘기면 됩니다.

판정: 통과'''

    def to_qa5(self):
        c = self.start(criteria=self.CRITERIA); self.finish(self.r['id'], '조사'); self.finish(self.b['id'], '구현')
        self.assertEqual(self.sent[-1][0], self.q['id']); return c

    def test_real_cc0edf66_qa_does_not_pass(self):
        c = self.to_qa5()
        self.assertIn('직접 검증하지 못한 요구사항이 남은 기준은 충족이 아니라 미충족', self.sent[-1][1]['text'])  # the QA prompt says so
        self.finish(self.q['id'], self.REAL_QA)
        c = self.h.cycle(c['id'])
        self.assertEqual(c['status'], 'running'); self.assertEqual(self.sent[-1][0], self.q['id'])  # asked again once, not done
        ask = self.sent[-1][1]['text']
        for n in (2, 3, 4): self.assertIn(f'기준 {n}(', ask)
        self.assertNotIn('기준 1(', ask); self.assertNotIn('기준 5(', ask)
        self.finish(self.q['id'], self.REAL_QA)  # same answer again → not met
        c = self.h.cycle(c['id']); qa = c['stages'][2]
        self.assertEqual(qa['verdict'], '수정 필요'); self.assertNotEqual(c['status'], 'done')
        self.assertEqual([r['status'] for r in qa['criteria']], ['충족', '근거 의심', '근거 의심', '근거 의심', '충족'])
        self.assertEqual(self.sent[-1][0], self.b['id']); self.assertIn('기준 3 (근거 의심)', self.sent[-1][1]['text'])

    def test_the_two_reported_lines_each_flag(self):
        lines = self.REAL_QA.splitlines()
        for n in (3, 4):
            ev = next(l for l in lines if l.startswith(f'- 기준 {n}:'))
            self.assertTrue(self.h.verification_gap(ev.split('충족', 1)[1]), n)

    def test_plain_negative_results_are_not_flagged(self):
        ok = ['쓰기 요청은 하지 않았습니다. python -m unittest 223 OK',
              '오류 없음 — 6조건 페이지 오류 0, 가로 넘침 false',
              '운영 재시작은 하지 않았고 격리 7885 에서 화면 4곳을 직접 확인했습니다',
              '결함을 10회 재현 시도했으나 재현되지 않음(고쳐짐), 테스트 223 통과',
              '실패 0건, 누락된 파일 없음']
        for ev in ok: self.assertEqual(self.h.verification_gap(ev), '', ev)
        bad = ['화면은 직접 보지 못했습니다', '확인하지 못함', '간접적으로만 봤습니다', '코드 검토 범위 밖', '미검증', '다음 사이클로 넘깁니다 확인']
        for ev in bad: self.assertTrue(self.h.verification_gap(ev), ev)

    def test_fixed_answer_after_reask_completes(self):
        c = self.to_qa5(); self.finish(self.q['id'], self.REAL_QA)
        fixed = '\n'.join(f'기준 {n}: 충족 — 7885 격리 실측과 운영 기록을 직접 대조, 명령과 결과 첨부' for n in range(1, 6)) + '\n판정: 통과'
        self.finish(self.q['id'], fixed)
        c = self.h.cycle(c['id'])
        self.assertEqual((c['status'], c['result']), ('done', '통과'))


class StageReportWaitTests(unittest.TestCase):
    """Live cycle a115d53e: the member finished before the lead's stage turn ended → its report sat in the lead's queue,
    the stage advanced to QA without it, and the lead's synthesis turn ran orphaned after the cycle moved on.
    Real Hub.send/enqueue/after_run; only Run.execute is a no-op so each turn is ended by hand."""
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(ignore_cleanup_errors=True); self.h = hub.Hub(self.tmp.name); s = self.h.store
        mk = lambda n: s.create(name=n, provider='claude')
        self.r, self.b, self.m, self.q = mk('리서치 팀장'), mk('개발 팀장'), mk('확인자'), mk('QA 팀장')
        team = lambda n, ms, lead: self.h.save_team({'name': n, 'members': [x['id'] for x in ms], 'lead': lead['id']})
        self.tr, self.tb, self.tq = team('리서치', [self.r], self.r), team('개발', [self.b, self.m], self.b), team('QA', [self.q], self.q)
        self.noop = patch.object(hub.Run, 'execute', lambda run: None); self.noop.start()
        self.alerts = patch.object(self.h, 'cycle_alert'); self.alerts.start()

    def tearDown(self):
        self.alerts.stop(); self.noop.stop(); self.h.runs.clear(); self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def end_turn(self, aid, text):
        """The running turn of `aid` writes its answer and ends (what Run.execute's finally does)."""
        run = self.h.runs.pop(aid); self.h.store.message(aid, 'assistant', text); self.h.store.update(aid, status='done')
        self.h.after_run(aid, 'done', getattr(run, 'reply_to', None))

    BODY = '\n근거와 검증: ' + '확인한내용' * 20

    def test_stage_waits_for_a_report_already_in_the_leads_queue(self):
        c = self.h.start_cycle({'goal': 'g', 'rounds': 1, 'teams': {'research': self.tr['id'], 'build': self.tb['id'], 'qa': self.tq['id']}})
        self.end_turn(self.r['id'], '조사' + self.BODY)
        self.assertIn(self.b['id'], self.h.runs)                      # build stage turn is running
        self.h.delegate(self.b['id'], '확인자', '요구사항 검토')       # lead hands a part to the member
        self.end_turn(self.m['id'], '확인자 보고: 기준 충족')          # member finishes FIRST → report queued at the lead
        self.assertEqual(len(self.h.store.agent(self.b['id'])['queue']), 1)
        self.end_turn(self.b['id'], '1차 보고: 확인자 보고 대기 중' + self.BODY)  # lead's stage turn ends
        c = self.h.cycle(c['id'])
        self.assertEqual([s['kind'] for s in c['stages']], ['research', 'build'])  # QA has NOT started
        self.assertNotIn(self.q['id'], self.h.runs)
        self.assertIn(self.b['id'], self.h.runs)                      # the lead is now reading the member's report
        self.end_turn(self.b['id'], '종합: 확인자 보고까지 반영 완료' + self.BODY)
        c = self.h.cycle(c['id'])
        self.assertEqual([s['kind'] for s in c['stages']], ['research', 'build', 'qa'])
        self.assertIn('종합: 확인자 보고까지 반영 완료', c['stages'][1]['report'])
        self.assertIn('확인자 보고: 기준 충족', c['stages'][1]['report'])   # the member's report travels as evidence
        self.assertNotIn(self.b['id'], self.h.runs)                   # no orphan lead turn left behind
        self.end_turn(self.q['id'], '판정: 통과')
        c = self.h.cycle(c['id'])
        self.assertEqual(c['status'], 'done')
        self.assertFalse(any(self.h.store.agent(a).get('queue') for a in (self.b['id'], self.m['id'])) or self.h.runs)  # done = team idle


def goal_rpc(goal_status, fail=None):
    """Fake app-server: records calls; the goal is `goal_status`; `fail` = method that errors."""
    calls = []
    class FakeRpc:
        def __init__(self):
            self.events = queue.Queue(); self.status = goal_status
            self.events.put({'method': 'item/completed', 'params': {'threadId': 'saved', 'turnId': 'turn', 'item': {'id': 'm', 'type': 'agentMessage', 'text': 'done'}}})
            self.events.put({'method': 'turn/completed', 'params': {'threadId': 'saved', 'turn': {'id': 'turn', 'status': 'completed'}}})
        def call(self, method, params):
            calls.append((method, params))
            if method == fail: raise RuntimeError('boom')
            if method == 'thread/goal/get': return {'goal': {'objective': 'o', 'status': self.status} if self.status else None}
            if method == 'thread/goal/set': self.status = params.get('status', self.status); return {'goal': {'status': self.status}}
            if method in ('thread/resume', 'thread/start'): return {'thread': {'id': 'saved'}}
            return {'turn': {'id': 'turn'}}
        def write(self, m): pass
        def close(self): pass
    return FakeRpc, calls


class NativeGoalTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(ignore_cleanup_errors=True); self.h = hub.Hub(self.tmp.name); self.h.root = Path(self.tmp.name)
        self.a = self.h.store.create(name='Codex 팀원', provider='codex'); self.h.store.update(self.a['id'], session='saved')

    def tearDown(self):
        self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def run_with(self, goal_status, reply_to, fail=None):
        rpc, calls = goal_rpc(goal_status, fail)
        r = hub.Run(self.h, self.h.store.agent(self.a['id']), {}, 'x'); r.reply_to = reply_to
        with patch.object(hub, 'Rpc', rpc), patch.object(self.h, 'after_run'), patch.object(self.h, 'codex_skills', return_value=[]): r.execute()
        return [c[0] for c in calls], calls

    def test_team_stage_pauses_active_goal_before_resume(self):
        for reply in ({'cycle': 'c', 'stage': 0}, {'agent': 'lead', 'kind': 'delegate'}):
            with self.subTest(reply=reply):
                methods, calls = self.run_with('active', reply)
                self.assertEqual(methods[:4], ['thread/goal/get', 'thread/goal/set', 'thread/goal/get', 'thread/resume'])
                self.assertEqual(calls[1][1], {'threadId': 'saved', 'status': 'paused'})
                self.assertEqual(self.h.store.agent(self.a['id'])['goal']['status'], 'paused')
        self.assertTrue(any('자체 목표를 일시 정지' in m['text'] for m in self.h.store.messages(self.a['id']) if m['role'] == 'system'))

    def test_failure_to_pause_stops_the_stage(self):
        methods, _ = self.run_with('active', {'cycle': 'c', 'stage': 0}, fail='thread/goal/set')
        self.assertNotIn('thread/resume', methods); self.assertNotIn('turn/start', methods)
        a = self.h.store.agent(self.a['id'])
        self.assertEqual(a['status'], 'error'); self.assertIn('일시 정지하지 못해', a['error'])

    def test_inactive_goal_or_direct_chat_is_left_alone(self):
        methods, _ = self.run_with('paused', {'cycle': 'c', 'stage': 0})
        self.assertEqual(methods[:2], ['thread/goal/get', 'thread/resume'])
        methods, _ = self.run_with('active', None)  # the user's own chat keeps Codex's own goal behaviour
        self.assertNotIn('thread/goal/get', methods)

    def test_delegate_tool_is_preapproved_in_every_mode(self):
        lead = self.h.store.create(name='lead', provider='codex', mode='read'); m = self.h.store.create(name='m', provider='claude')
        self.h.save_team({'name': 't', 'members': [lead['id'], m['id']], 'lead': lead['id']})
        rpc, calls = goal_rpc(None)
        r = hub.Run(self.h, self.h.store.agent(lead['id']), {}, 'x')
        with patch.object(hub, 'Rpc', rpc), patch.object(self.h, 'after_run'), patch.object(self.h, 'codex_skills', return_value=[]): r.execute()
        cfg = next(p for meth, p in calls if meth == 'thread/start')['config']['mcp_servers.pitcrew']
        self.assertEqual(cfg['default_tools_approval_mode'], 'approve')


if __name__ == '__main__':
    unittest.main()
