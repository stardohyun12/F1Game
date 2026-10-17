# PitCrew UI/UX release review — 2026-10-15

## Delivered

- Start guide with actual CLI readiness, project, import/new-role and team actions.
- CommonMark rendering, tables, nested lists, safe links, individual code copying;
  mobile tables scroll instead of squeezing code into unreadable columns.
- Pointer-following tabs and conversation cards, insertion markers, edge scrolling,
  cancel behavior, persisted order, active-tab underline and visible grip.
- Context-link direction, per-recipient draft retention, team-wide summary and
  optional connect-all. One atomic write validates every changed recipient before
  saving; outside-team links remain intact, stale membership and >8 peers rejected.
- Persistent failure/interruption/approval notices with actionable focus targets.
  Failed sends retain drafts and are never automatically replayed.
- Named dialogs with keyboard focus containment; mobile comparison fills the
  reading pane and closing it returns focus. Text and control contrast corrected.

## Run and update

Open `http://localhost:7878/` and refresh the browser to load the updated UI.
`PitCrew.cmd` starts the app. `install-autostart.ps1` registers startup after Windows
sign-in; it is installed on this PC. Re-run registration after relocating the repo.
For a deliberate web-only restart, use `start.ps1 -Restart`. `runtime.py --restart`
refuses shutdown when work or an approval is pending. Conversation stores are local
under `.runtime`; do not delete that directory to reset the UI.

## Final verification

- Python **35**: `test_hub test_workspace test_runtime test_watchdog
  test_team_membership test_team_links`.
- Node **15**: `test_reader.cjs test_connection.cjs test_organization.cjs test_send.cjs`.
- Edge UI suites: `test_drag_ui.cjs`, `test_reader_ui.cjs`, `test_setup_ui.cjs`,
  `test_release_ui.cjs`. The release fixture intercepts APIs and never calls paid
  providers or writes production conversations. Reader UI uses the existing
  disposable real-provider test project; no new provider prompt is needed.
- `test_accessibility_ui.cjs`: workspace, setup, team board, import, fully loaded
  settings, dark theme and mobile, WCAG 2 A/AA + 2.1 AA tags, zero violations found.
  Release fixture also audits shared-context and approval screens.
- Real deployed `/api/hub/team-links`: saved identical links in the disposable
  verification project, returned both unchanged peer lists. No user conversations
  were sent test messages. Original Claude/Codex session tests remain documented
  in `TEAM_FLOW_VERIFICATION.md`.
- Model output streaming fixture: scroll position remains when reading above the
  bottom; new output is followed at the bottom. Mobile 20-team board and blank
  project tested. Keyboard Tab/Shift+Tab/Escape and focus targets exercised.

To run browser tests, install Playwright and axe-core as **test tools** or set
`PLAYWRIGHT_PATH` and `AXE_PATH` to their existing local paths. The application does
not depend on either. Edge and the app must be running/available. Results and
screenshots are under `.runtime`, including `accessibility-audit.json`,
`team-links-desktop-verified.png`, `approval-mobile-verified.png`,
`compare-mobile-verified.png`, `reader-*-verified.png`, and `setup-*-verified.png`.

## References and adaptations

[UIBowl](https://uibowl.io/website) public layout informed navigation hierarchy;
private gallery assets were not copied. [Atlassian drag guidance](https://atlassian.design/components/pragmatic-drag-and-drop/design-guidelines/)
and [dnd-kit overlay](https://dndkit.com/legacy/api-documentation/draggable/drag-overlay/)
informed pointer previews, source placeholders and insertion cues. These behaviors
were adapted to vanilla JavaScript, without those libraries as dependencies.
[Primer empty states](https://primer.github.io/design/ui-patterns/empty-states/)
informed actionable start/empty views. [W3C dialog guidance](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/)
informed focus behavior and labels. Failures reproduced by tests were fixed and
the same scenarios rerun rather than treating a design reference as validation.

Markdown-it 15.0.2 is the unmodified official browser distribution, loaded locally.
Its basic headings/tables were reproduced before adding rendering rules. Raw HTML
is disabled; code is escaped and external images render as links. npm tarball
integrity and source hashes are in `web/vendor-manifest.json`; MIT and dependency
notices are in `web/THIRD_PARTY_NOTICES.txt`. Known dependency vulnerabilities in the
isolated npm audit: zero on the verification date; this is not a security warranty.

## Verified scope and remaining limitations

This review covers the existing Windows local app and its main user flows; it is
not a claim of universal usability, formal accessibility certification or a hosted
multi-user release. Physical touch devices and screen-reader hardware were not
tested. Responsive layouts were exercised in Edge. Account/model availability is
determined by the connected CLI. Imported conversation history does not transfer
another app's proprietary tools or live subagents. A reboot itself was not
performed; the actual installed startup shortcut was tested from a stopped server.
