"""Company-style cycle: research → build → QA, QA "수정 필요" sends it back to build; one alert at the end."""
import os
import tempfile
import unittest
from unittest.mock import patch

import hub

class CycleTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.h = hub.Hub(self.tmp.name); s = self.h.store
        mk = lambda n: s.create(name=n, provider='claude')
        self.r, self.b, self.q, self.helper = mk('리서치 팀장'), mk('개발 팀장'), mk('QA 팀장'), mk('QA 팀원')
        t = lambda n, ms, lead: self.h.save_team({'name': n, 'members': [x['id'] for x in ms], 'lead': lead['id']})
        self.tr, self.tb, self.tq = t('리서치', [self.r], self.r), t('개발', [self.b], self.b), t('QA', [self.q, self.helper], self.q)
        self.sent = []
        self.send = patch.object(self.h, 'send', side_effect=lambda aid, d: self.sent.append((aid, d))); self.send.start()
        self.alerts = patch.object(self.h, 'cycle_alert'); self.cycle_alert = self.alerts.start()

    def tearDown(self):
        self.alerts.stop(); self.send.stop(); self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    BODY = '근거와 검증: ' + '확인한내용' * 20  # a real report has substance (the guard asks again below ~80 chars)

    def finish(self, aid, text, status='done', body=True):
        """The stage agent answers its cycle message, then its run ends."""
        _, d = self.sent[-1]
        self.h.store.message(aid, 'user', d['text']); self.h.store.message(aid, 'assistant', (self.BODY + '\n' if body else '') + text)
        self.h.after_run(aid, status, d.get('replyTo'))

    def start(self, rounds=2):
        return self.h.start_cycle({'goal': '협업 효율 개선', 'rounds': rounds, 'teams': {'research': self.tr['id'], 'build': self.tb['id'], 'qa': self.tq['id']}})

    def test_full_cycle_with_one_fix_round(self):
        c = self.start()
        self.assertEqual(self.sent[-1][0], self.r['id'])
        self.finish(self.r['id'], '사실 5개')
        self.assertEqual(self.sent[-1][0], self.b['id']); self.assertIn('사실 5개', self.sent[-1][1]['text'])
        self.assertEqual(self.sent[-1][1]['origin']['agent'], self.r['id'])  # team → team hand-off is visible
        self.finish(self.b['id'], '구현함')
        self.assertEqual(self.sent[-1][0], self.q['id'])
        self.finish(self.q['id'], '결함 1개\n판정: 수정 필요')
        self.assertEqual(self.sent[-1][0], self.b['id']); self.assertIn('QA 지적', self.sent[-1][1]['text'])
        self.assertEqual(self.h.cycle(c['id'])['round'], 2)
        self.finish(self.b['id'], '고침')
        self.finish(self.q['id'], '이상 없음\n판정: 통과')
        c = self.h.cycle(c['id'])
        self.assertEqual((c['status'], c['result']), ('done', '통과'))
        self.assertEqual([s['kind'] for s in c['stages']], ['research', 'build', 'qa', 'build', 'qa'])
        self.assertEqual([s['verdict'] for s in c['stages'] if s['kind'] == 'qa'], ['수정 필요', '통과'])
        self.assertEqual(self.cycle_alert.call_count, 1)  # one alert, at the end

    def test_rounds_cap_ends_the_cycle(self):
        c = self.start(rounds=1)
        self.finish(self.r['id'], 'r'); self.finish(self.b['id'], 'b'); self.finish(self.q['id'], '판정: 수정 필요')
        c = self.h.cycle(c['id']); self.assertEqual((c['status'], c['result']), ('paused', '수정 필요'))
        self.assertTrue(c['roundLimitReached'])
        self.h.retry_cycle(c['id'])
        self.assertEqual(self.sent[-1][0],self.b['id'])
        self.assertIn('판정: 수정 필요',self.sent[-1][1]['text'])
        self.assertEqual(self.h.cycle(c['id'])['round'],2)
        self.finish(self.b['id'],'수정 완료');self.finish(self.q['id'],'판정: 통과')
        self.assertEqual(self.h.cycle(c['id'])['status'],'done')

    def test_developer_cannot_approve_own_work(self):
        with self.assertRaises(ValueError):
            self.h.start_cycle({'goal':'test','teams':{'research':self.tr['id'],'build':self.tb['id'],'qa':self.tb['id']}})
        self.assertEqual(self.sent,[])

    def test_failed_stage_stops_and_alerts(self):
        c = self.start()
        self.finish(self.r['id'], '오류', status='error')
        self.assertEqual(self.h.cycle(c['id'])['status'], 'failed'); self.assertEqual(self.cycle_alert.call_count, 1)
        self.assertEqual(len(self.sent), 1)  # nothing further was sent

    def test_stop_prevents_next_stage(self):
        c = self.start(); self.h.stop_cycle(c['id'])
        self.finish(self.r['id'], 'r')
        self.assertEqual(len(self.sent), 1)

    def test_lead_that_delegates_reports_after_its_members(self):
        # QA lead hands part of the review to its member: the stage result waits for the member's report
        c = self.start()
        self.finish(self.r['id'], 'r'); self.finish(self.b['id'], 'b')
        qa_msg = self.sent[-1][1]
        stage = qa_msg['replyTo']['stage']
        self.h.store.put('delegations', [dict(id='x', lead=self.q['id'], member=self.helper['id'], task='코드 봐줘', status='running', ts=hub.time.time(), cycle=c['id'], stage=stage)])
        self.h.store.message(self.q['id'], 'user', qa_msg['text']); self.h.store.message(self.q['id'], 'assistant', '팀원에게 맡겼습니다')
        self.h.after_run(self.q['id'], 'done', qa_msg['replyTo'])
        self.assertEqual(self.h.cycle(c['id'])['stages'][-1]['status'], 'queued')  # not yet
        self.assertTrue(self.h.store.agent(self.q['id'])['pendingReplies'])
        self.h.store.put('delegations', [dict(id='x', lead=self.q['id'], member=self.helper['id'], task='코드 봐줘', status='done', ts=hub.time.time(), cycle=c['id'], stage=stage)])
        self.h.store.message(self.q['id'], 'user', '[인계] 팀원 보고', origin={'agent': self.helper['id'], 'kind': 'handoff'})
        self.h.store.message(self.q['id'], 'assistant', self.BODY + '\n종합: 문제 없음\n판정: 통과')
        self.h.after_run(self.q['id'], 'done', None)
        c = self.h.cycle(c['id'])
        self.assertEqual((c['status'], c['stages'][-1]['verdict']), ('done', '통과'))
        self.assertNotIn('팀원에게 맡겼습니다', c['stages'][-1]['report'])  # live cycle 3: only the synthesis written after the members' reports
        self.assertIn('[팀원 보고 원문 — 근거]', c['stages'][-1]['report'])  # the member's own words travel as evidence

    def test_empty_report_is_asked_again_once_then_fails(self):
        # QA round 1 of the real cycle got an empty build report; the next stage must not start on nothing
        c = self.start()
        self.finish(self.r['id'], '', body=False)
        self.assertEqual(self.sent[-1][0], self.r['id']); self.assertIn('비어 있거나 너무 짧습니다', self.sent[-1][1]['text'])
        self.assertEqual(self.h.cycle(c['id'])['stages'][0]['status'], 'running')
        self.finish(self.r['id'], '이번엔 제대로')
        self.assertEqual(self.sent[-1][0], self.b['id'])  # moved on with the second, real report
        self.finish(self.b['id'], '', body=False); self.finish(self.b['id'], '', body=False)
        c = self.h.cycle(c['id']); self.assertEqual((c['status'], c['stages'][-1]['status']), ('failed', 'failed'))

    def test_qa_without_verdict_line_is_asked_again(self):
        c = self.start()
        self.finish(self.r['id'], 'r'); self.finish(self.b['id'], 'b')
        self.finish(self.q['id'], '대체로 괜찮음')
        self.assertEqual(self.sent[-1][0], self.q['id']); self.assertIn('판정 줄이 없습니다', self.sent[-1][1]['text'])
        self.finish(self.q['id'], '판정: 통과')
        self.assertEqual(self.h.cycle(c['id'])['result'], '통과')

    def test_quoted_verdict_does_not_count_only_the_last_verdict_line(self):
        # Real cycle b93a5e6d: QA quoted "`판정: 통과`" from the build report and concluded "판정: 수정 필요".
        c = self.start(rounds=1)
        self.finish(self.r['id'], 'r'); self.finish(self.b['id'], 'b')
        self.finish(self.q['id'], '개발 보고의 `판정: 통과` 처리 설명을 읽었습니다.\n결함 2개\n판정: 수정 필요')
        c = self.h.cycle(c['id']); self.assertEqual((c['result'], c['stages'][-1]['verdict']), ('수정 필요', '수정 필요'))

    def test_short_qa_answer_with_a_verdict_line_is_enough(self):
        c = self.start()
        self.finish(self.r['id'], 'r'); self.finish(self.b['id'], 'b')
        self.finish(self.q['id'], '판정: 통과', body=False)
        self.assertEqual(self.h.cycle(c['id'])['result'], '통과')

    def test_stage_message_carries_roster_split_rule_and_report_form(self):
        # Real cycle b93a5e6d: 0 hand-offs — "팀원이 있으면 나눠 맡겨도 됩니다" read as optional.
        self.h.store.update(self.helper['id'], role='코드 리뷰')
        self.start(); self.finish(self.r['id'], 'r'); self.finish(self.b['id'], 'b')
        qa_text = self.sent[-1][1]['text']
        self.assertIn('QA 팀원', qa_text); self.assertIn('코드 리뷰', qa_text)
        self.assertIn('delegate 도구로 맡긴다', qa_text); self.assertIn('## 분담', qa_text)
        self.assertIn('혼자다', self.sent[0][1]['text'])  # research team has only its lead

    def test_stage_records_how_many_hand_offs_the_lead_made(self):
        c = self.start(); self.finish(self.r['id'], 'r'); self.finish(self.b['id'], 'b')
        self.h.store.put('delegations', [dict(id='d1', lead=self.q['id'], member=self.helper['id'], task='리뷰', status='done', ts=hub.time.time())])
        self.finish(self.q['id'], '판정: 통과')
        s = self.h.cycle(c['id'])['stages'][-1]
        self.assertEqual((s['delegated'], s['helpers'], s['members']), (1, ['QA 팀원'], 1))
        self.assertEqual(self.h.cycle(c['id'])['stages'][0]['members'], 0)

    # --- Codex R9 audit ---
    def test_r9_1_removing_a_queued_stage_pauses_and_retry_resumes(self):
        c = self.start()
        msg = self.sent[-1][1]
        self.h.store.update(self.r['id'], queue=[dict(id='q1', text=msg['text'], ts=1, replyTo=msg['replyTo'])])
        self.h.queue_action({'agent': self.r['id'], 'action': 'remove', 'id': 'q1'})
        c = self.h.cycle(c['id']); self.assertEqual((c['status'], c['stages'][0]['status']), ('paused', 'interrupted'))
        self.h.retry_cycle(c['id'])
        c = self.h.cycle(c['id']); self.assertEqual((c['status'], [s['kind'] for s in c['stages']]), ('running', ['research', 'research']))
        self.finish(self.r['id'], '다시 조사함')
        self.assertEqual(self.sent[-1][0], self.b['id'])

    def test_r9_2_two_waiting_requests_do_not_overwrite_each_other(self):
        c = self.start(); cyc = self.sent[-1][1]['replyTo']
        self.h.store.put('delegations', [dict(id='d', lead=self.r['id'], member=self.helper['id'], status='running', ts=hub.time.time(), cycle=c['id'], stage=0),
                                          dict(id='e', lead=self.r['id'], member=self.helper['id'], status='running', ts=hub.time.time())])
        self.h.store.message(self.r['id'], 'user', 'x'); self.h.store.message(self.r['id'], 'assistant', '맡겼다')
        self.h.after_run(self.r['id'], 'done', cyc)
        self.h.store.update(self.helper['id'], queue=[dict(id='w', text='다른 일', ts=1, replyTo={'agent': self.r['id']})])  # member busy for r
        self.h.after_run(self.r['id'], 'done', {'agent': self.q['id']})
        self.assertEqual(len(self.h.store.agent(self.r['id'])['pendingReplies']), 2)

    def test_r9_3_duplicate_completion_creates_the_next_stage_once(self):
        c = self.start(); cyc = self.sent[-1][1]['replyTo']
        self.h.store.message(self.r['id'], 'user', 'x'); self.h.store.message(self.r['id'], 'assistant', self.BODY)
        self.h.after_run(self.r['id'], 'done', cyc); self.h.after_run(self.r['id'], 'done', cyc)
        self.assertEqual([s['kind'] for s in self.h.cycle(c['id'])['stages']], ['research', 'build'])

    def test_r9_4_verdict_only_from_the_last_line(self):
        self.assertEqual(self.h.verdict_of('예시:\n> 판정: 통과\n실제 검증은 못 했습니다.'), '')
        self.assertEqual(self.h.verdict_of('본문\n**판정: 수정 필요**'), '수정 필요')
        self.assertEqual(self.h.verdict_of('본문\n판정: 통과\n'), '통과')

    def test_r9_6_restart_pauses_a_cut_stage(self):
        c = self.start()  # research stage sent (send is mocked, nothing queued, nothing running)
        self.h.recover_cycles()
        self.assertEqual(self.h.cycle(c['id'])['status'], 'paused')

    def test_r9_15_stop_removes_queued_stage_work(self):
        c = self.start(); msg = self.sent[-1][1]
        self.h.store.update(self.r['id'], queue=[dict(id='q1', text='x', ts=1, replyTo=msg['replyTo']), dict(id='q2', text='다른 일', ts=1)])
        self.h.stop_cycle(c['id'])
        self.assertEqual([x['id'] for x in self.h.store.agent(self.r['id'])['queue']], ['q2'])

    def test_long_lead_report_preserves_head_in_storage_and_next_stage(self):
        c = self.start(); reply = self.sent[-1][1]['replyTo']; aid = self.r['id']
        self.h.store.message(aid, 'user', 'stage task')
        self.h.store.message(aid, 'assistant', 'LEAD_SUMMARY_START\n' + '가' * 11000)
        self.h.after_run(aid, 'done', reply)
        report = self.h.cycle(c['id'])['stages'][0]['report']
        self.assertTrue(report.startswith('LEAD_SUMMARY_START'))
        self.assertIn('뒷부분 생략', report)
        self.assertIn('수집된 팀원 보고 없음', report)
        self.assertIn(report, self.sent[-1][1]['text'])

    def test_combined_member_proofs_mark_truncation_and_keep_lead(self):
        c = self.start(); reply = self.sent[-1][1]['replyTo']; aid = self.r['id']
        self.h.store.message(aid, 'user', 'stage task')
        for i in range(4):
            self.h.store.message(aid, 'user', '근거' * 1400,
                origin={'kind': 'handoff', 'name': str(i), 'agent': self.helper['id']})
        self.h.store.message(aid, 'assistant', self.BODY)
        self.h.after_run(aid, 'done', reply)
        report = self.h.cycle(c['id'])['stages'][0]['report']
        self.assertTrue(report.startswith(self.BODY))
        self.assertGreater(len(report), 6000)
        self.assertIn('뒷부분 생략', report.split(self.h.PROOF_HEAD)[1])
        self.assertIn(report, self.sent[-1][1]['text'])

    def test_long_qa_report_keeps_final_verdict_separate_from_proofs(self):
        c = self.start(); self.finish(self.r['id'], 'r'); self.finish(self.b['id'], 'b')
        self.finish(self.q['id'], '가' * 11000 + '\n판정: 통과')
        stage = self.h.cycle(c['id'])['stages'][-1]
        self.assertEqual(stage['verdict'], '통과')
        self.assertEqual(stage['summary'], '판정: 통과')

    def test_team_without_members_is_refused_before_anything_starts(self):
        empty = self.h.save_team({'name': '빈 팀', 'members': []})
        with self.assertRaises(ValueError):
            self.h.start_cycle({'goal': 'x', 'teams': {'research': empty['id'], 'build': self.tb['id'], 'qa': self.tq['id']}})
        self.assertEqual(self.sent, [])

if __name__ == '__main__':
    unittest.main()
