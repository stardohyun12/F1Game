"""Team lead: only the lead alerts for routine finishes, the lead can hand work to members, reports come back."""
import os
import tempfile
import unittest
from unittest.mock import patch

import hub

class TeamLeadTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.h = hub.Hub(self.tmp.name)
        s = self.h.store
        self.lead = s.create(name='팀장', provider='claude'); self.dev = s.create(name='개발 팀원', provider='codex')
        self.qa = s.create(name='검토 팀원', provider='claude'); self.out = s.create(name='바깥', provider='claude')
        for a in (self.lead, self.dev, self.qa, self.out): s.update(a['id'], runStartedAt=1.0, lastTask='x')
        self.team = self.h.save_team({'name': '툴 팀', 'members': [self.lead['id'], self.dev['id'], self.qa['id']], 'lead': self.lead['id']})
        self.env = patch.dict(os.environ, {'PITCREW_WINDOWS_NOTIFY': '1'}); self.env.start()
        self.pop = patch.object(hub.subprocess, 'Popen'); self.popen = self.pop.start()

    def tearDown(self):
        self.pop.stop(); self.env.stop(); self.h.close(); self.h.store.db.close(); self.tmp.cleanup()

    def test_lead_must_be_a_member(self):
        with self.assertRaises(ValueError):self.h.save_team({'name': 't', 'members': [self.dev['id']], 'lead': self.out['id']})

    def test_removing_the_lead_clears_it(self):
        t = self.h.change_team_member({'agent': self.lead['id'], 'team': self.team['id'], 'remove': True})
        self.assertEqual(t['lead'], '')

    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_member_finishes_quietly_but_asks_out_loud(self):
        self.h.alert(self.dev['id'], 'done'); self.h.alert(self.dev['id'], 'interrupted')
        self.assertEqual(self.popen.call_count, 0)
        self.h.alert(self.dev['id'], 'waiting'); self.h.alert(self.qa['id'], 'error')
        self.assertEqual(self.popen.call_count, 2)
        self.assertIn('툴 팀 · 개발 팀원', ' '.join(self.popen.call_args_list[0][0][0]))

    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_lead_and_outsiders_alert(self):
        self.h.alert(self.lead['id'], 'done'); self.assertEqual(self.popen.call_count, 1)
        self.h.last_done_alert = 0  # outside the 3-minute burst window
        self.h.alert(self.out['id'], 'done'); self.assertEqual(self.popen.call_count, 2)

    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_lead_waits_for_open_delegations(self):
        with patch.object(self.h, 'send') as send:
            self.h.delegate(self.lead['id'], '개발', '버그 고쳐')
        item = dict(id='q', text='버그 고쳐', ts=1, replyTo=send.call_args[0][1]['replyTo'])
        self.h.store.update(self.dev['id'], queue=[item])
        self.h.alert(self.lead['id'], 'done'); self.assertEqual(self.popen.call_count, 0)
        self.h.store.update(self.dev['id'], queue=[]); self.h.store.update(self.lead['id'], runStartedAt=2.0)
        self.h.alert(self.lead['id'], 'done'); self.assertEqual(self.popen.call_count, 1)

    def test_delegate_routes_report_back_to_lead(self):
        with patch.object(self.h, 'send') as send:
            m = self.h.delegate(self.lead['id'], '검토 팀원', '이 변경 검토')
        self.assertEqual(m['id'], self.qa['id'])
        aid, data = send.call_args[0]
        self.assertEqual(aid, self.qa['id']); self.assertTrue(data['queue'])
        self.assertEqual((data['replyTo']['agent'], data['origin']['kind']), (self.lead['id'], 'delegate'))

    def test_only_the_lead_delegates_and_only_to_members(self):
        with patch.object(self.h, 'send'):
            with self.assertRaises(ValueError):self.h.delegate(self.dev['id'], '검토 팀원', 'x')
            with self.assertRaises(ValueError):self.h.delegate(self.lead['id'], '바깥', 'x')
            with self.assertRaises(ValueError):self.h.delegate(self.lead['id'], '팀원', 'x')  # ambiguous
            with self.assertRaises(ValueError):self.h.delegate(self.lead['id'], '개발 팀원', '  ')

    def test_context_tells_each_side_its_role(self):
        lead = self.h.context(self.h.store.agent(self.lead['id']), [])['team']
        self.assertEqual(lead['you'], '팀장'); self.assertEqual(sorted(m['name'] for m in lead['members']), ['개발 팀원', '검토 팀원'])
        dev = self.h.context(self.h.store.agent(self.dev['id']), [])['team']
        self.assertEqual((dev['you'], dev['lead']), ('팀원', '팀장'))
        self.assertNotIn('team', self.h.context(self.h.store.agent(self.out['id']), []))

    def test_report_reaches_the_lead_after_member_done(self):
        sent = []
        with patch.object(self.h, 'send', side_effect=lambda aid, d: sent.append((aid, d))), patch.object(self.h, 'alert'):
            self.h.store.message(self.qa['id'], 'user', '검토'); self.h.store.message(self.qa['id'], 'assistant', '결함 2개')
            self.h.after_run(self.qa['id'], 'done', {'agent': self.lead['id'], 'text': '팀원 보고'})
        self.assertEqual(sent[0][0], self.lead['id']); self.assertIn('결함 2개', sent[0][1]['text'])

class TeamLeadReviewTests(TeamLeadTests):
    """Codex review 7: RC13–RC16."""
    def test_rc13_unrelated_member_run_does_not_mark_lead_delegating(self):
        self.h.store.update(self.dev['id'], status='running')
        lead = next(a for a in self.h.bootstrap()['agents'] if a['id'] == self.lead['id'])
        self.assertFalse(lead.get('delegating'))
        self.h.store.update(self.dev['id'], queue=[dict(id='q', text='x', ts=1, replyTo={'agent': self.lead['id']})])
        lead = next(a for a in self.h.bootstrap()['agents'] if a['id'] == self.lead['id'])
        self.assertTrue(lead['delegating'])

    def test_rc14_failed_or_stopped_delegation_is_reported_to_the_lead(self):
        sent = []
        reply = {'agent': self.lead['id'], 'kind': 'delegate', 'chain': {'id': 'c', 'path': []}, 'text': '보고'}
        with patch.object(self.h, 'send', side_effect=lambda aid, d: sent.append((aid, d))), patch.object(self.h, 'alert'):
            self.h.store.update(self.qa['id'], error='토큰 한도')
            self.h.after_run(self.qa['id'], 'error', reply)
            self.h.after_run(self.dev['id'], 'interrupted', reply)
        self.assertEqual([a for a, _ in sent], [self.lead['id'], self.lead['id']])
        self.assertIn('토큰 한도', sent[0][1]['text']); self.assertIn('사용자가 중단', sent[1][1]['text'])
        self.assertEqual(sent[0][1]['origin']['chain']['id'], 'c')

    def test_rc15_lead_of_two_teams_reaches_both(self):
        self.h.save_team({'name': '둘째 팀', 'members': [self.lead['id'], self.out['id']], 'lead': self.lead['id']})
        with patch.object(self.h, 'send'):
            self.assertEqual(self.h.delegate(self.lead['id'], '바깥', 'x')['id'], self.out['id'])
            self.assertEqual(self.h.delegate(self.lead['id'], '개발 팀원', 'x')['id'], self.dev['id'])
        names = sorted(m['name'] for m in self.h.context(self.h.store.agent(self.lead['id']), [])['team']['members'])
        self.assertEqual(names, sorted(['개발 팀원', '검토 팀원', '바깥']))

    def test_rc16_cycles_depth_and_budget_are_refused(self):
        self.h.save_team({'name': '역방향', 'members': [self.dev['id'], self.lead['id']], 'lead': self.dev['id']})
        run = type('R', (), {})()
        # dev is working on something the lead handed over: handing it back to the lead is a cycle
        run.chain = {'id': 'c1', 'path': [self.lead['id']]}; self.h.runs[self.dev['id']] = run
        with patch.object(self.h, 'send'):
            with self.assertRaises(ValueError):self.h.delegate(self.dev['id'], '팀장', 'x')
        self.h.runs.clear()
        run.chain = {'id': 'c2', 'path': ['a', 'b', 'c']}; self.h.runs[self.lead['id']] = run
        with patch.object(self.h, 'send'):
            with self.assertRaises(ValueError):self.h.delegate(self.lead['id'], '개발 팀원', 'x')  # depth
        run.chain = {'id': 'c3', 'path': []}
        with patch.object(self.h, 'send') as send:
            for _ in range(self.h.DELEGATION_BUDGET):self.h.delegate(self.lead['id'], '개발 팀원', 'x')
            with self.assertRaises(ValueError):self.h.delegate(self.lead['id'], '개발 팀원', 'x')  # budget
        self.assertEqual(send.call_args[0][1]['origin']['chain'], {'id': 'c3', 'path': [self.lead['id']]})
        self.h.runs.clear()

class OneAlertPerChainTests(TeamLeadTests):
    """"Too many alerts": a chain me → reviewer → me must alert once, at its real end."""
    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_turn_that_reports_elsewhere_is_silent_and_requester_waits_for_the_reply(self):
        a, b = self.out, self.qa  # a asked b for a review (reply comes back to a); neither is in a led role for this
        self.h.store.update(b['id'], queue=[dict(id='q', text='리뷰', ts=1, replyTo={'agent': a['id']})])
        self.h.alert(a['id'], 'done')                       # a's turn ends while the review is pending → silent
        self.h.alert(b['id'], 'error', reported=True)       # b's failure goes back to a → silent
        self.assertEqual(self.popen.call_count, 0)
        self.h.alert(b['id'], 'waiting', reported=True)     # but b needing you always alerts
        self.assertEqual(self.popen.call_count, 1)
        self.h.store.update(b['id'], queue=[]); self.h.store.update(a['id'], runStartedAt=5.0)
        self.h.alert(a['id'], 'done')                       # chain really finished → one alert
        self.assertEqual(self.popen.call_count, 2)

    def test_after_run_marks_reported_turns_for_the_browser(self):
        with patch.object(self.h, 'send'), patch.object(self.h, 'alert') as alert:
            self.h.after_run(self.qa['id'], 'done', {'agent': self.out['id']})
            self.assertEqual(self.h.store.agent(self.qa['id'])['reportedTo'], self.out['id'])
            self.assertTrue(alert.call_args.kwargs['reported'])
            self.h.after_run(self.qa['id'], 'done', None)
            self.assertEqual(self.h.store.agent(self.qa['id'])['reportedTo'], '')

class LanguageAndAlertBurstTests(TeamLeadTests):
    """도현: 중간 설명이 영어로 나옴 / 알림이 너무 많음."""
    def test_every_prompt_asks_for_the_workspace_language(self):
        r = hub.Run(self.h, self.h.store.agent(self.lead['id']), {}, '일 해줘')
        self.assertIn('응답 언어: 한국어', r.full_prompt()); self.assertTrue(r.full_prompt().endswith('[사용자 메시지]\n일 해줘'))
        self.h.store.put('language', 'off'); self.assertNotIn('응답 언어', r.full_prompt())

    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_finishes_within_three_minutes_are_one_alert_and_logged(self):
        self.h.alert(self.out['id'], 'done'); self.h.alert(self.lead['id'], 'done')
        self.h.alert(self.lead['id'], 'waiting')  # needing you is never merged
        self.assertEqual(self.popen.call_count, 2)
        log = self.h.store.get('alertLog')
        self.assertEqual([(x['name'], x['kind'], x['sent']) for x in log], [('바깥', 'done', True), ('팀장', 'done', False), ('팀장', 'waiting', True)])

class UndeliveredReportTests(TeamLeadTests):
    """Codex review 12 RC25: a hand-back that failed is not "reported" — the turn alerts like any other end."""
    def test_failed_hand_back_does_not_silence(self):
        with patch.object(self.h, 'send', side_effect=ValueError('대기열은 최대 10개입니다.')), patch.object(self.h, 'alert') as alert:
            self.h.after_run(self.qa['id'], 'error', {'agent': self.out['id']})
        self.assertFalse(alert.call_args.kwargs['reported'])
        self.assertEqual(self.h.store.agent(self.qa['id'])['reportedTo'], '')

class TeamWriteRaceTests(TeamLeadTests):
    """Codex review 10 RC20: changing the lead must not drop a member someone else just added."""
    def test_lead_change_keeps_a_member_added_meanwhile(self):
        tid = self.team['id']
        self.h.change_team_member({'agent': self.out['id'], 'team': tid})  # another client adds a member
        t = self.h.change_team_member({'agent': self.dev['id'], 'team': tid, 'lead': True})
        self.assertEqual(t['lead'], self.dev['id']); self.assertIn(self.out['id'], t['members'])
        t = self.h.change_team_member({'agent': self.dev['id'], 'team': tid, 'lead': False})
        self.assertEqual(t['lead'], '')
        with self.assertRaises(ValueError):self.h.change_team_member({'agent': 'nope', 'team': tid, 'lead': True})

    def test_full_form_save_is_refused_when_the_team_changed_since_it_opened(self):
        tid, rev = self.team['id'], self.team['rev']
        self.h.change_team_member({'agent': self.out['id'], 'team': tid})
        with self.assertRaises(ValueError):
            self.h.save_team({'id': tid, 'rev': rev, 'name': '툴 팀', 'members': [self.lead['id'], self.dev['id']], 'lead': self.lead['id']})
        fresh = next(t for t in self.h.store.get('teams') if t['id'] == tid)
        self.assertIn(self.out['id'], fresh['members'])

class DelegationRecordTests(TeamLeadTests):
    def records(self): return {d['member']: d for d in self.h.store.get('delegations', [])}

    def test_record_follows_queued_running_done(self):
        with patch.object(hub.threading, 'Thread'):  # no real run
            self.h.delegate(self.lead['id'], '개발 팀원', '버그 고쳐')
        d = self.records()[self.dev['id']]
        self.assertEqual(d['status'], 'running')  # member was idle: the run started right away
        with patch.object(self.h, 'send'), patch.object(self.h, 'alert'):
            self.h.store.message(self.dev['id'], 'assistant', '고쳤습니다. 테스트 통과')
            self.h.after_run(self.dev['id'], 'done', {'agent': self.lead['id'], 'delegation': d['id']})
        d = self.records()[self.dev['id']]
        self.assertEqual((d['status'], d['summary']), ('done', '고쳤습니다. 테스트 통과'))

    def test_cancelling_a_queued_hand_off_closes_the_record_and_tells_the_lead(self):
        self.h.runs[self.qa['id']] = type('R', (), {'chain': None, 'reply_to': None})()  # member busy → task queues
        self.h.delegate(self.lead['id'], '검토 팀원', '검토해')
        item = self.h.store.agent(self.qa['id'])['queue'][0]
        self.assertEqual(self.records()[self.qa['id']]['status'], 'queued')
        with patch.object(self.h, 'send') as send:
            self.h.queue_action({'agent': self.qa['id'], 'action': 'remove', 'id': item['id']})
        self.assertEqual(self.records()[self.qa['id']]['status'], 'stopped')
        self.assertEqual(send.call_args[0][0], self.lead['id'])
        self.h.runs.clear()

if __name__ == '__main__':
    unittest.main()
