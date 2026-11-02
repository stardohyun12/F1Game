/**
 * PitCrew 진행 상태 — 입력창 아래 상태 줄 칩 + 대화 아래 작은 패널(D-254).
 * 데이터: D:/Github/F1Game/scripts/status_server.py (127.0.0.1:8799, 20초마다 다시 읽음).
 */
import { cn, Tip, useQuery } from '@hermes/plugin-sdk'
import { jsx, jsxs } from 'react/jsx-runtime'

const ID = 'pitcrew-status'

function useStatus() {
  return useQuery({
    queryKey: ['pitcrew-status'],
    queryFn: async () => {
      const r = await fetch('http://127.0.0.1:8799/status')
      if (!r.ok) throw new Error('HTTP ' + r.status)
      return r.json()
    },
    refetchInterval: 20000,
    retry: 0
  })
}

function shortLine(data) {
  if (!data) return 'PitCrew: 상태 서버 꺼짐'
  const run = (data.lines || []).filter(l => l.startsWith('▶'))
  if (!run.length) return '⏸ PitCrew: 도는 작업 없음'
  return run.map(l => l.replace(/^▶\s*/, '').replace(/\s*\(시작.*$/, '')).join(' · ')
}

function Chip() {
  const q = useStatus()
  const data = q.data
  const prog = (data?.lines || []).find(l => l.includes('진행'))
  const label = (q.isError ? 'PitCrew: 상태 서버 꺼짐' : '▶ ' + shortLine(data)).slice(0, 70) + (prog ? '  ' + prog.trim().replace(/^진행\s*/, '').split(' ')[0] : '')
  return jsx(Tip, {
    label: data?.text || '상태 서버(127.0.0.1:8799)에 연결 안 됨',
    children: jsx('span', {
      className: cn('inline-flex h-full items-center px-1.5 text-[0.6875rem]', 'text-(--ui-text-secondary)'),
      children: label
    })
  })
}

function Pane() {
  const q = useStatus()
  const lines = q.isError ? ['상태 서버(127.0.0.1:8799)에 연결 안 됨 — PD 에게 말해 주세요'] : (q.data?.lines || ['불러오는 중…'])
  return jsx('div', {
    className: 'flex h-full flex-col gap-0.5 overflow-auto p-2 font-mono text-[0.75rem] text-(--ui-text-secondary)',
    children: lines.map((l, i) => jsx('div', { className: l.startsWith('▶') ? 'text-(--ui-text-primary) font-medium' : '', children: l }, i))
  })
}

export default {
  id: ID,
  name: 'PitCrew 진행 상태',
  register(ctx) {
    ctx.register({ id: 'chip', area: 'statusBar.left', order: 5, render: () => jsx(Chip, {}) })
    ctx.register({
      id: 'pane',
      area: 'panes',
      title: 'PitCrew 진행',
      data: { placement: 'bottom', dock: { pane: 'workspace', pos: 'bottom' }, height: '150px' },
      render: () => jsx(Pane, {})
    })
  }
}
