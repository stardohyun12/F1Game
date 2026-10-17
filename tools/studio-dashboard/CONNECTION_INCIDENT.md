# Connection incident — 2026-10-14

Both the web listener (7878) and runtime process were absent. Requests failed
with connection refused, rather than an AI response timeout. Existing stdout,
stderr and runtime logs were empty. Windows had not rebooted since October 11.
The exact cause of the previous process exits cannot be established from this evidence.

The launcher previously used Start-Process, and runtime.py used Popen with
CREATE_NO_WINDOW / CREATE_NEW_PROCESS_GROUP. These do not themselves prove
independence from a caller's Windows job or process-tree cleanup.
Reference: https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects
and https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/create-method-in-class-win32-process

The Windows launcher now requests a hidden process through WMI. The web host
sets its port explicitly, appends flushed logs and records startup/exit and
exceptions. Runtime records startup, readiness, minute heartbeats and exits in
.runtime/lifecycle.log. No messages are automatically replayed.

Verified on this machine: WmiPrvSE.exe is the new web process's ancestor;
the launch command exited and subsequent separate commands received HTTP 200;
the original six agent records were present; 25 Python tests passed, including
mock streaming across web restart. Tests that forcibly terminate the invoking
process were rejected by automatic approval review without a detailed reason,
so survival of forced caller termination has not been verified. Long unattended
operation and machine reboot recovery are also not established by these checks.

This corrects the earlier completion report's overstatement: web restart tests
covered an AI stream surviving a web restart, not the launching tool's lifecycle.
