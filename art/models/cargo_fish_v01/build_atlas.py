"""Same reviewed material pipeline as wine, selected by this directory name."""
from pathlib import Path
shared=Path(__file__).resolve().parent.parent/'cargo_wine_v01'/'build_atlas.py'
exec(compile(shared.read_text(encoding='utf-8'),str(shared),'exec'))
