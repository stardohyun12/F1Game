# Conversation and team organization — verified 2026-10-14

## Precedents and application

- [VS Code custom layout](https://code.visualstudio.com/docs/configure/custom-layout):
  adopted separate open tabs, closing a view without destroying its underlying
  content, reorderable tabs, and dragging a conversation to a side-by-side area.
- [W3C dragging movements guidance](https://www.w3.org/WAI/WCAG22/Understanding/dragging-movements):
  each organization operation has click/menu alternatives. Mobile users can use
  those controls without dragging. This is not a claim of full WCAG certification.

No external code or new package was copied. Layout preferences live per project
in localStorage; teams and recoverably archived conversations use the existing
authenticated backend. Closing a tab never interrupts an AI run or deletes a
native session. Busy conversations cannot be archived.

## Implementation

- Open conversation tabs with close buttons, drag reordering and saved order.
  Closing all tabs leaves an empty workspace, including after reload.
- Conversation list drag reordering, menu-based up/down movement, and saved order.
  The list uses pointer dragging because native dragging from nested buttons was
  unreliable in the tested browser. Tabs and board cards use native drag events.
- Conversation menu: open, show alongside, reorder list/tabs, close tab, archive,
  add to an existing team, or create a team with the conversation preselected.
- Team board with searchable library, drop-to-add lanes, removal controls, team
  settings, context links and new-team creation. Membership is additive: a
  conversation may belong to multiple teams; removal only detaches that team.
- Recoverable archive and immediate undo; persistent archive restoration remains
  available in the sidebar. Supporting tools collapse to leave more room for chats.
- Side-by-side drop area; light/dark themes; mobile team columns stack vertically.
- Draft preservation, project-scoped layout, stable DOM when polling has no
  changes, focus outlines and disabled unavailable tab-movement controls.

## Completion evidence

Live browser checks used an isolated port-7880 database with three disposable
agents; production conversations were not modified for these tests.

| Requirement | Observed evidence |
|---|---|
| Click-close tabs | Closed selected tabs and all tabs; reload showed no open conversation. |
| Remove unnecessary conversations | Archived fixture review agent; restored it through archive UI with its identity retained. |
| Preserve draft | Switched conversations, closed a tab, and reloaded; unsent draft remained. |
| Reorder tabs | Dragged Development before Planning; reload preserved the order. |
| Reorder conversation list | Pointer drag changed Planning/Development order; subsequent board library used saved order. |
| Arrange adjacent conversations | Dragged Planning onto the right-side drop area; comparison panel opened with Planning. |
| Build team by dragging | Dragged Development from library to Review team; lane changed from one to two members and survived reload. |
| Create a team from a conversation | Review checkbox preselected; saved Release Verification team with one member; board showed it. |
| Compact screen | 390px viewport had 390px document width; team lanes stacked at 297px, without document overflow. |
| Visual themes | Inspected light and dark boards at desktop size. |
| Production delivery | Reloaded port 7878; open tabs and new organization entry rendered, no browser errors. |

JavaScript syntax checks passed for app.js, organize.js and workspace.js;
server.py compiled. Native session adapters and AI execution were unchanged.
Screenshot: `.runtime/organization-board-verified.png` (fixture team data).

Scope: organization of PitCrew's conversations and teams, not browser-tab management
or moving windows between operating-system monitors. Records are archived through
the recoverable product flow rather than permanently erasing provider transcripts.
