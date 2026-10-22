"""Read accepted values from the design, never from implementation settings."""
import hashlib
import re
from pathlib import Path

def load_criteria(base):
    path = Path(base) / 'docs/superpowers/specs/2026-10-20-spline-town-design.md'
    text = path.read_text(encoding='utf-8')
    section = text.split('### 2.9 ', 1)[1].split('## 3.', 1)[0]
    def row(label):
        line = next(x for x in section.splitlines() if x.startswith('| '+label+' |'))
        return float(re.search(r'\*\*([\d.]+)', line.split('|')[3]).group(1))
    return dict(spec=str(path.relative_to(base)), sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                height_scale=row('높이 배율'), cutfill_m=row('깎기·메우기 상한'),
                generated_grade=row('종단 경사 — 잇기 길·섞기 경계 길')/100,
                pad_grade=row('헤어핀 패드 경사')/100,
                main_grade=float(re.search(r'실제 OSM 큰길은 (\d+)%', section).group(1))/100,
                warning_fraction=float(re.search(r'6m 초과 점 ≤ (\d+)%', section).group(1))/100,
                changed_terrain_m=float(re.search(r'메우기가 ([\d.]+)m 이상', section).group(1)),
                absolute_cutfill_m=float(re.search(r'절대 상한 (\d+)m', section).group(1)),
                alley_grade=.12)
