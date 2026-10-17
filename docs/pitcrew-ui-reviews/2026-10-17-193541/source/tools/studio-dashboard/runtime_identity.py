"""Diagnostic fingerprints of startup code objects and source files.

Code fingerprints cover module-defined functions/methods, including nested code.
They do not attest to mutable globals, imported dependencies, or external providers.
Source hashes are observations at capture time, not proof of what Python compiled.
"""
import hashlib
import inspect
import json
from pathlib import Path
import sys
import time
import types


def _code_value(value):
    if isinstance(value,types.CodeType):
        fields=('co_argcount','co_posonlyargcount','co_kwonlyargcount','co_nlocals','co_stacksize',
                'co_flags','co_code','co_consts','co_names','co_varnames','co_freevars','co_cellvars',
                'co_name','co_qualname','co_exceptiontable')
        return ['code',[[field,_code_value(getattr(value,field,None))] for field in fields]]
    if isinstance(value,(tuple,frozenset)):
        items=[_code_value(v) for v in value]
        if isinstance(value,frozenset):items.sort(key=lambda item:json.dumps(item,ensure_ascii=True))
        return [type(value).__name__,items]
    if isinstance(value,bytes):return ['bytes',value.hex()]
    return [type(value).__name__,repr(value)]


def code_fingerprint(module):
    rows = []
    def visit(prefix, value):
        if isinstance(value, (staticmethod, classmethod)): value = value.__func__
        if inspect.isfunction(value) and value.__module__ == module.__name__:
            encoded=json.dumps(_code_value(value.__code__),ensure_ascii=True,separators=(',',':')).encode()
            rows.append((prefix, hashlib.sha256(encoded).hexdigest()))
        elif inspect.isclass(value) and value.__module__ == module.__name__:
            for key, member in sorted(vars(value).items()):
                if inspect.isfunction(member) or isinstance(member, (staticmethod, classmethod)):
                    visit(prefix+'.'+key, member)
    for name, value in sorted(vars(module).items()): visit(name, value)
    serialized='\n'.join(name+':'+digest for name,digest in rows).encode('utf-8')
    return hashlib.sha256(serialized).hexdigest(), len(rows)


def source_hash(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def capture(modules):
    result = dict(schema=1, capturedAt=time.time(), python=sys.version.split()[0],
                  codeFormat='python-code-structure-v1', modules={})
    for label,module in sorted(modules.items()):
        digest,count=code_fingerprint(module)
        row=dict(loadedCodeSha256=digest, functionCount=count)
        try: row['sourceSha256AtCapture']=source_hash(module.__file__)
        except OSError: row['sourceSha256AtCapture']=None
        result['modules'][label]=row
    return result


def snapshot(identity, directory):
    if not identity: return dict(available=False)
    result={**identity, 'available':True, 'modules':{}}
    for name,saved in identity['modules'].items():
        row=dict(saved)
        try: row['sourceSha256Now']=source_hash(Path(directory)/name)
        except OSError: row['sourceSha256Now']=None
        before,now=row['sourceSha256AtCapture'],row['sourceSha256Now']
        row['sourceChangedSinceCapture']=(before!=now) if before is not None and now is not None else None
        result['modules'][name]=row
    return result
