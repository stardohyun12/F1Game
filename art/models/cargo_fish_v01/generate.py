"""Reuse shared geometry pipeline while keeping this file's fish output directory."""
from pathlib import Path
shared=Path(__file__).resolve().parent.parent/'cargo_wine_v01'/'generate.py'
exec(compile(shared.read_text(encoding='utf-8'),str(shared),'exec'))
