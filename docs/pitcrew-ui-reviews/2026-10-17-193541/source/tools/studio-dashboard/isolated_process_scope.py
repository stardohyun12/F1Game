"""Windows-only test ownership for detached descendants, including before descriptor creation.

Create suspended -> assign exact process handle to job -> resume. No descriptor PID adoption.
See https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects
"""
import ctypes
from ctypes import wintypes as w
import os
import subprocess
import tempfile
import time


class BasicLimits(ctypes.Structure):
    _fields_ = [('times', ctypes.c_longlong * 2), ('flags', w.DWORD),
                ('minWorking', ctypes.c_size_t), ('maxWorking', ctypes.c_size_t),
                ('activeLimit', w.DWORD), ('affinity', ctypes.c_size_t),
                ('priority', w.DWORD), ('scheduling', w.DWORD)]


class ExtendedLimits(ctypes.Structure):
    _fields_ = [('basic', BasicLimits), ('io', ctypes.c_ulonglong * 6),
                ('memory', ctypes.c_size_t * 4)]


class Accounting(ctypes.Structure):
    _fields_ = [('times', ctypes.c_longlong * 4), ('faults', w.DWORD),
                ('total', w.DWORD), ('active', w.DWORD), ('terminated', w.DWORD)]


class WindowsProcessScope:
    """Own only processes launched through run(), until this context exits."""
    def __init__(self):
        if os.name != 'nt':
            raise RuntimeError('Windows test process scope requires Windows')
        self.api = ctypes.WinDLL('kernel32', use_last_error=True)
        signatures = {
            'CreateJobObjectW': ([ctypes.c_void_p, w.LPCWSTR], w.HANDLE),
            'SetInformationJobObject': ([w.HANDLE, ctypes.c_int, ctypes.c_void_p, w.DWORD], w.BOOL),
            'AssignProcessToJobObject': ([w.HANDLE, w.HANDLE], w.BOOL),
            'QueryInformationJobObject': ([w.HANDLE, ctypes.c_int, ctypes.c_void_p, w.DWORD, ctypes.c_void_p], w.BOOL),
            'TerminateJobObject': ([w.HANDLE, w.UINT], w.BOOL),
            'ResumeThread': ([w.HANDLE], w.DWORD),
            'OpenProcess': ([w.DWORD, w.BOOL, w.DWORD], w.HANDLE),
            'IsProcessInJob': ([w.HANDLE, w.HANDLE, ctypes.POINTER(w.BOOL)], w.BOOL),
            'WaitForSingleObject': ([w.HANDLE, w.DWORD], w.DWORD),
            'CloseHandle': ([w.HANDLE], w.BOOL),
        }
        for name, (args, result) in signatures.items():
            function = getattr(self.api, name)
            function.argtypes, function.restype = args, result
        self.handle = self.api.CreateJobObjectW(None, None)
        if not self.handle:
            raise ctypes.WinError(ctypes.get_last_error())
        limits = ExtendedLimits()
        limits.basic.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE; no breakaway.
        if not self.api.SetInformationJobObject(self.handle, 9, ctypes.byref(limits), ctypes.sizeof(limits)):
            error = ctypes.WinError(ctypes.get_last_error())
            self.api.CloseHandle(self.handle)
            self.handle = None
            raise error
        self.remaining = None

    def __enter__(self):
        return self

    def active_count(self):
        if self.handle is None:
            return self.remaining
        info = Accounting()
        if not self.api.QueryInformationJobObject(self.handle, 1, ctypes.byref(info), ctypes.sizeof(info), None):
            raise ctypes.WinError(ctypes.get_last_error())
        return info.active

    def run(self, args, *, env, cwd, timeout):
        import _winapi
        import msvcrt
        if self.handle is None:
            raise RuntimeError('Process scope is closed')
        with open(os.devnull, 'rb') as stdin, tempfile.TemporaryFile() as stdout, tempfile.TemporaryFile() as stderr:
            handles = [msvcrt.get_osfhandle(f.fileno()) for f in (stdin, stdout, stderr)]
            startup = subprocess.STARTUPINFO()
            startup.dwFlags = subprocess.STARTF_USESTDHANDLES
            startup.hStdInput, startup.hStdOutput, startup.hStdError = handles
            startup.lpAttributeList = {'handle_list': handles}
            try:
                for handle in handles:
                    os.set_handle_inheritable(handle, True)
                process, thread, _, _ = _winapi.CreateProcess(None, subprocess.list2cmdline(args),
                    None, None, True, subprocess.CREATE_NO_WINDOW | 4, env, str(cwd), startup)
            finally:
                for handle in handles:
                    os.set_handle_inheritable(handle, False)
            assigned = False
            try:
                if not self.api.AssignProcessToJobObject(self.handle, process):
                    raise ctypes.WinError(ctypes.get_last_error())
                assigned = True
                if self.api.ResumeThread(thread) == 0xffffffff:
                    raise ctypes.WinError(ctypes.get_last_error())
                if _winapi.WaitForSingleObject(process, int(timeout * 1000)) == 258:
                    raise subprocess.TimeoutExpired(args, timeout)
                code = _winapi.GetExitCodeProcess(process)
                stdout.seek(0)
                stderr.seek(0)
                return subprocess.CompletedProcess(args, code, stdout.read().decode('utf-8'), stderr.read().decode('utf-8'))
            finally:
                if not assigned:
                    _winapi.TerminateProcess(process, 1)
                    _winapi.WaitForSingleObject(process, 5000)
                _winapi.CloseHandle(thread)
                _winapi.CloseHandle(process)

    def __exit__(self, exc_type, exc_value, traceback):
        try:
            self._close()
        except BaseException as cleanup_error:
            if exc_value is not None:
                raise BaseExceptionGroup('Test failure and process cleanup failure',
                                         [exc_value, cleanup_error]) from None
            raise

    def _close(self):
        handles = []
        try:
            # Job accounting can reach zero before kernel handles/file locks finish closing.
            # Obtain wait handles only from this job and verify membership after opening.
            capacity = 64
            while True:
                class ProcessIds(ctypes.Structure):
                    _fields_ = [('assigned', w.DWORD), ('count', w.DWORD),
                                ('ids', ctypes.c_size_t * capacity)]
                ids = ProcessIds()
                if self.api.QueryInformationJobObject(self.handle, 3, ctypes.byref(ids), ctypes.sizeof(ids), None):
                    break
                if ctypes.get_last_error() != 234:
                    raise ctypes.WinError(ctypes.get_last_error())
                capacity *= 2
            for pid in ids.ids[:ids.count]:
                handle = self.api.OpenProcess(0x101000, False, pid)  # synchronize + query limited
                if not handle:
                    if ctypes.get_last_error() == 87:  # already exited
                        continue
                    raise ctypes.WinError(ctypes.get_last_error())
                member = w.BOOL()
                if not self.api.IsProcessInJob(handle, self.handle, ctypes.byref(member)) or not member.value:
                    self.api.CloseHandle(handle)
                    continue
                handles.append(handle)
            if not self.api.TerminateJobObject(self.handle, 1):
                raise ctypes.WinError(ctypes.get_last_error())
            deadline = time.monotonic() + 5
            for handle in handles:
                if self.api.WaitForSingleObject(handle, max(0, int((deadline - time.monotonic()) * 1000))) != 0:
                    raise RuntimeError('Test process exit did not complete')
            while self.active_count():
                if time.monotonic() >= deadline:
                    raise RuntimeError('Test descendants did not exit')
                time.sleep(.02)
            self.remaining = 0
        finally:
            self.api.CloseHandle(self.handle)
            self.handle = None
            for handle in handles:
                self.api.CloseHandle(handle)
