"""M1: exposed shoulder perimeter minus its contact with road/sidewalk.

No quad order, vertex order, winding, width, or subdivision count is assumed.
Inputs are world-space scene triangles, not regenerated centerlines.
"""
import collections
import itertools
import math

TOL = 0.0001  # 0.1 mm, well below M1's 50 mm threshold


def point_segment_distance(p, a, b):
    ab = [b[k] - a[k] for k in range(3)]
    length2 = sum(x*x for x in ab)
    t = max(0., min(1., sum((p[k]-a[k])*ab[k] for k in range(3))/length2)) if length2 else 0.
    return math.dist(p, [a[k]+t*ab[k] for k in range(3)])


def welded_mesh(mesh):
    # Sort by coordinates first: canonical representatives are independent of input order.
    vertices = []
    buckets = collections.defaultdict(list)
    remap = {}
    for old in sorted(range(len(mesh['vertices'])), key=lambda i: tuple(mesh['vertices'][i])):
        p = tuple(mesh['vertices'][old])
        cell = tuple(math.floor(x/TOL) for x in p)
        matches = []
        for delta in itertools.product((-1, 0, 1), repeat=3):
            near = tuple(cell[k]+delta[k] for k in range(3))
            matches.extend(i for i in buckets.get(near, ()) if math.dist(p, vertices[i]) <= TOL)
        if matches:
            remap[old] = min(matches)
        else:
            remap[old] = len(vertices)
            buckets[cell].append(len(vertices))
            vertices.append(p)
    triangles = set()
    idx = mesh['triangles']
    for i in range(0, len(idx), 3):
        t = tuple(sorted(remap[x] for x in idx[i:i+3]))
        if len(set(t)) == 3:
            triangles.add(t)  # ignore duplicate/back-face copies
    counts = collections.Counter()
    for t in triangles:
        for a,b in itertools.combinations(t, 2):
            counts[a,b] += 1
    return vertices, counts


class ContactEdges:
    """Spatial index of real road triangle edges, including curb/sidewalk seams."""
    def __init__(self, meshes):
        self.edges = []
        self.bins = collections.defaultdict(list)
        for mesh in meshes:
            verts, counts = welded_mesh(mesh)
            for a,b in counts:
                p,q = verts[a],verts[b]
                index = len(self.edges)
                self.edges.append((p,q))
                for x in range(math.floor((min(p[0],q[0])-TOL)/4), math.floor((max(p[0],q[0])+TOL)/4)+1):
                    for z in range(math.floor((min(p[2],q[2])-TOL)/4), math.floor((max(p[2],q[2])+TOL)/4)+1):
                        self.bins[x,z].append(index)

    def touches(self, p):
        return any(point_segment_distance(p, *self.edges[i]) <= 2*TOL
                   for i in self.bins.get((math.floor(p[0]/4),math.floor(p[2]/4)), ()))


def outer_edges(shoulder, road_meshes):
    verts, counts = welded_mesh(shoulder)
    contacts = ContactEdges(road_meshes)
    attached = [contacts.touches(p) for p in verts]
    result = []
    stats = collections.Counter()
    ambiguous = []
    for (a,b), count in sorted(counts.items()):
        if count != 1:
            stats['internal_edges' if count == 2 else 'nonmanifold_edges'] += 1
            continue
        p,q = verts[a],verts[b]
        stats['perimeter_edges'] += 1
        if attached[a] and attached[b]:
            # A cap can touch two different roads. Require its interior to contact too.
            interior = [tuple(p[k]+t*(q[k]-p[k]) for k in range(3)) for t in (.25,.5,.75)]
            if all(contacts.touches(v) for v in interior):
                stats['inner_contact_edges'] += 1
            else:
                ambiguous.append((p,q))
        elif attached[a] != attached[b]:
            stats['end_cap_edges'] += 1
        else:
            result.append((p,q))
    stats['outer_edges'] = len(result)
    stats['ambiguous_edges'] = len(ambiguous)
    if not result:
        raise ValueError('No outer shoulder boundary found; cannot claim M1 PASS')
    return result, dict(stats), ambiguous
