# Automatic recovery (2026-10-14)

`start.ps1` now activates a hidden, WMI-launched `watchdog.py`. It adopts an
existing server without stopping its runs. A per-port OS file lock prevents
duplicate supervisors. Every roughly two seconds it checks the runtime and web
listener. Dead services are started again; repeated failures back off to at most
30 seconds. AI sends and tool actions are never replayed by the supervisor.

The design reuses the supervision/backoff principles described in
[Supervisor's configuration](https://supervisord.org/configuration.html) and
[process implementation](https://github.com/Supervisor/supervisor/blob/main/supervisor/process.py).
No Supervisor code or dependency was copied; Windows WMI launches the supervisor
outside the calling terminal's process tree. Existing project databases and
provider-native session identifiers remain authoritative.

## Verified

- 27 Python tests and 4 browser-connection unit tests pass.
- Isolated web process forcibly terminated while mock AI streaming: watchdog
  restarts web automatically; runtime PID/token unchanged; complete answer;
  exactly one provider request and one user message.
- Isolated runtime forcibly terminated: watchdog restarts it automatically;
  original agent and native session ID retained; unfinished work is interrupted,
  not resent; duplicate watchdog exits without taking ownership.
- `live_watchdog_check.py`: actual disposable Codex and Claude conversations,
  followed by runtime termination and automatic restart, then another turn.
  Both returned the remembered random word with the same session ID and both
  user messages preserved. No existing user conversation was sent a test prompt.
- Production port 7878 is monitored; its existing runtime was adopted unchanged.

## Scope and operation

Web failure does not interrupt the independent AI runtime. Runtime failure does
interrupt in-flight work; the next user message resumes the stored native
session, rather than inventing a new conversation. This does not guarantee that
an uncommitted final output or tool result survived a crash. A slow but listening
process is not forcibly killed. An unresponsive machine and watchdog death during
the same login remain outside this supervisor's recovery scope.

## Windows login startup (2026-10-15)

`install-autostart.ps1` installs `PitCrew.lnk` in the current user's Windows
Startup folder. It runs the virtual environment's `pythonw.exe` with `watchdog.py`
without opening a console. This starts after Windows **sign-in**, not before it.
Run the script again after moving this repository. Use `-Remove` to unregister;
existing services and conversations remain running.

This follows [Microsoft's Startup folder guidance](https://support.microsoft.com/en-US/Windows/Experience/Startup-Boot/configure-startup-applications-in-windows).
No external code, packages, administrator privileges or execution-policy changes
are required. Registration was missing on this machine, explaining why an already
stopped server did not return after reboot.

Verified with port 7878 stopped: launching the actual installed shortcut started
the watchdog, runtime and web server, and health returned HTTP 200. Launching it
again retained watchdog PID 8012 and runtime PID 30264. An actual OS restart or
sign-out was **not** performed. Windows must permit the Startup entry, the D: drive
must be available, and the repository/virtual environment must remain in place.

Logs/status: `.runtime/watchdog.log`, `watchdog-7878.json`, `lifecycle.log`,
`server.stdout.log`, `server.stderr.log`. To stop automatic recovery without
stopping ongoing AI work, create `.runtime/watchdog-7878.stop`; the watchdog exits
and leaves services alone. `start.ps1` removes that marker and reactivates it.
Stop supervision before deliberately stopping the runtime, otherwise it restarts.

Run isolated recovery tests with `.venv/Scripts/python.exe -m unittest
test_runtime test_watchdog`; the optional live check consumes four small provider
turns. These tests kill only their own temporary fixture processes.
