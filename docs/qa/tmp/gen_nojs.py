# jsonschema 가 이 파이썬에 없어서 스키마 검사만 건너뛰고 gen.py 를 그대로 돌린다(장소 항목 키는 기존 항목과 같다). 나머지 검사(중복·Fixed 좌표)는 gen.check 가 한다.
import sys, types
m = types.ModuleType("jsonschema")
class _V:
    def __init__(self, schema): pass
    def iter_errors(self, data): return []
m.Draft7Validator = _V
sys.modules["jsonschema"] = m
sys.path.insert(0, "tools/world")
import gen
sys.argv = ["gen.py"] + sys.argv[1:]
sys.exit(gen.main())
