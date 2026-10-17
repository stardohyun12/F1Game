# Usability and regression checks — 2026-10-14

## Pointer-following tabs and conversations — 2026-10-15

Reviewed the public [UIBowl website gallery](https://uibowl.io/website), including
its sidebar/filter separation and grouped controls. Individual interaction
examples did not finish loading in the captured public view; no membership-only
content or gallery assets were copied. For concrete drag behavior, consulted
[Atlassian's design guidelines](https://atlassian.design/components/pragmatic-drag-and-drop/design-guidelines/)
and [dnd-kit's overlay documentation](https://dndkit.com/legacy/api-documentation/draggable/drag-overlay/).
Adopted the overlay, subdued source, explicit insertion marker and discoverable
drag affordance principles in the existing vanilla JS UI. This is an adaptation,
not a reproduction of those libraries. No new runtime package or copied assets.

Tabs and sidebar rows now have a fixed-position visual copy that preserves the
pointer's original grab offset. The source remains as a muted placeholder.
An insertion line distinguishes before/after placement; scroll edges work even
when the mouse stops moving. Only release commits ordering. Escape, pointer
cancellation, lost capture, window blur and project changes clean up the drag.
The active tab has an underline and a grip cue. Close/menu buttons remain separate
actions. Touch keeps native scrolling and the existing menu-based ordering.

`test_drag_ui.cjs` exercises the actual UI in Edge without sending AI messages or
changing stored conversations. Verified pointer/preview coordinate agreement,
horizontal and vertical reorder, saved order after reload, insertion lines,
Escape, invalid drop, close buttons, horizontal edge scrolling, pointercancel,
and light/dark screenshots. No browser errors. All 11 existing Node regression
tests also pass. Run with `PLAYWRIGHT_PATH` pointing to the installed Playwright
package, then `node test_drag_ui.cjs`; the app must be running on port 7878.

Evidence: `.runtime/drag-tabs-verified.png`, `drag-sidebar-verified.png`, and
`drag-dark-verified.png`. Actual touch hardware was not tested.

## Reproduced and fixed

Four regression tests failed against the previous organization implementation:
loading another project erased its saved tabs; 30 unchanged renders performed
30 synchronous storage writes; a delayed team edit could continue into the next
project; archive undo did not retain the original project destination.
The same coverage now passes. Delayed responses are scoped to their originating
project, and layouts are reconciled only after that project's data has loaded.

Message submission now tracks in-flight sends per conversation. A newer draft
typed while a send is pending survives its completion, another project's input
is untouched, duplicate submissions produce one request, and an empty workspace
cannot submit. New reference attachments are retained when old ones are sent.

Team membership now uses a locked server-side delta instead of fetching a team
and resubmitting its entire membership list. Concurrent additions retain both
people and the team's metadata. Repeated add/remove operations are idempotent;
archived members are rejected. The board retains its search and horizontal
position after editing, and a late edit does not dismiss a different dialog.

## Easier and lighter

- Sidebar search matches conversation name, role or provider. Ctrl/Cmd+K focuses
  it; Escape and the clear button remove the filter.
- Each project restores its saved tabs and selected conversation. Drafts remain
  separate when switching between projects.
- Unchanged layout renders now do zero additional localStorage writes (measured
  over 30 repeated renders, compared with 30 before).
- Membership changes use one API call rather than a bootstrap read plus write.
- Main polling interval: visible active work 1.4s; visible idle 5s; hidden 30s.
  These intervals imply 72% fewer idle polling cycles and 83% fewer hidden cycles
  than the prior configuration. This is a configured polling comparison, not a
  measurement of total application traffic or CPU usage. Returning to the app
  triggers immediate refresh. Account-usage fetches retain their own cache.

## Verification

Commands, all passing:

```
node --test test_connection.cjs test_organization.cjs test_send.cjs
.venv/Scripts/python.exe -m unittest test_hub test_workspace test_runtime test_watchdog test_team_membership
```

11 JavaScript tests and 30 Python tests, including real isolated process
termination/recovery and streaming preservation with a mock provider. The new
frontend concurrency tests execute production functions with controlled delayed
responses and DOM/storage fixtures; they are not a complete browser emulation.

Actual browser checks on isolated port 7880:

- A → B → A project navigation preserved three open tabs, selected Development,
  and A's unsent draft while B retained its own separate draft.
- Searching `codex` showed only Development. Ctrl+K focused the search input.
- Adding Planning to Release Verification changed its visible membership to two.
- Removing Planning from Product changed its count to one while the board search
  stayed `기획`, and the filtered library still showed Planning.
- No browser errors were reported during these checks.

Production runtime updated only after confirming all projects had zero runs;
the runtime shutdown guard also checks for active work. All six original agent
IDs and native session IDs matched before and after deployment. This turn did
not send test prompts into user conversations or consume provider generation
quota. Existing real-provider session-resume evidence is in AUTO_RECOVERY.md.

## Sources and limits

Applied stale-response handling described by
[React's effect lifecycle guidance](https://react.dev/learn/synchronizing-with-effects)
without adding React, and reduced synchronous storage writes based on
[MDN's Web Storage documentation](https://developer.mozilla.org/en-US/docs/Web/API/Web_Storage_API).
No external source code or package was copied.

These checks establish the listed behavior, not an absence of all possible bugs.
Long unattended operation, external CLI version changes and simultaneous use of
the same native session in another application remain outside this test run.
