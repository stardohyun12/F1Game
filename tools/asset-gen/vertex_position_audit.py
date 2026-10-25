"""Order-independent positional-set comparison at a fixed absolute tolerance."""
from itertools import product
import numpy as np

def compare_positions(left, right, tolerance=1e-4):
    # Exporters split vertices for UV seams differently. Compare positions as
    # sets (as the previous np.unique audit did), never round before matching.
    left=np.unique(np.asarray(left,dtype=float),axis=0)
    right=np.unique(np.asarray(right,dtype=float),axis=0)
    if not len(left) or not len(right) or not np.isfinite(left).all() or not np.isfinite(right).all():
        return {'pass':False,'reason':'Empty or non-finite position set'}
    offsets=list(product((-1,0,1),repeat=3))
    def directed(source,target):
        bins={}
        for p in target:bins.setdefault(tuple(np.floor(p/tolerance).astype(int)),[]).append(p)
        maximum=0.;missing=[];worst=None
        for p in source:
            cell=np.floor(p/tolerance).astype(int)
            candidates=[q for offset in offsets for q in bins.get(tuple(cell+offset),[])]
            if not candidates:
                missing.append(p.tolist());continue
            qs=np.asarray(candidates);distances=np.max(np.abs(qs-p),axis=1);i=int(distances.argmin());distance=float(distances[i])
            if distance>maximum:maximum=distance;worst={'source':p.tolist(),'target':qs[i].tolist(),'max_axis_error_m':distance}
            if distance>tolerance:missing.append(p.tolist())
        return {'pass':not missing,'unmatched_count':len(missing),'unmatched_examples':missing[:8],
                'maximum_matched_axis_error_m':maximum,'worst_pair':worst}
    forward=directed(left,right);reverse=directed(right,left)
    old_left=np.unique(np.round(left,4),axis=0);old_right=np.unique(np.round(right,4),axis=0)
    return {'pass':forward['pass'] and reverse['pass'],'tolerance_m':tolerance,
            'method':'Unrounded positional sets; bidirectional spatial-neighbour match with L-infinity absolute distance <= tolerance. No relative tolerance; no count or ordering dependence from decimal rounding.',
            'left_exact_unique_count':len(left),'right_exact_unique_count':len(right),
            'legacy_rounded_unique_counts':[len(old_left),len(old_right)],
            'legacy_sorted_max_axis_error_m':float(np.max(np.abs(old_left-old_right))) if old_left.shape==old_right.shape else None,
            'legacy_sorted_rounded_pass':old_left.shape==old_right.shape and bool(np.allclose(old_left,old_right,atol=tolerance,rtol=0)),
            'left_to_right':forward,'right_to_left':reverse}
