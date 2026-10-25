"""Bounded vehicle-artist trial: geometry only, metres, Z-up/front -Y.

Reuses the previous local prism/wheel primitives, not the failed load path.
Host owns metric32 UVs, photograph atlas, functional object split and export.
"""
import math
from bayloop_props2_vehicles import _prism, _wheel


def build(p, b):
    start = len(b.F)
    # Lock Ape 50 reference proportions before detailing: 1.59 m wheelbase,
    # 1.06 m track, 0.225 m wheel radius; modest stylized cabin height 1.59 m.
    front_y, rear_y = -.99, .60
    b.PART = 'chassis'
    for x in [-.40, .40]:
        # Short stamped rails with rising ends; no rectangular bar protruding
        # beyond the rear bed support. The top remains level for load contact.
        profile=[(-.88,.38),(-.68,.31),(.66,.31),(.91,.39),(.91,.45),(-.88,.45)]
        left=[(x-.05,y,z) for y,z in profile]
        right=[(x+.05,y,z) for y,z in profile]
        b.face(list(reversed(left)),'darkroof'); b.face(right,'darkroof')
        for i in range(len(profile)):
            j=(i+1)%len(profile)
            b.face([left[i],left[j],right[j],right[i]],'darkroof')
    # Continuous transverse members overlap the rails and the load floor.
    b.PART = 'chassis_crossmembers'
    for y in [.12, .84]:
        b.box(0, y, .43, 1.24, .12, .13, 'body')
    b.PART = 'body'
    _prism(b, [(-1.25,.52),(-1.25,.80),(-.96,1.54),(-.79,1.59),(-.15,1.59),(-.13,.52)], 1.10)
    # A continuous floor/sill, spanning the two rails, physically carries cab.
    b.PART = 'cab_floor'
    b.box(0, -.64, .43, 1.10, 1.04, .10, 'body')
    b.PART = 'glass'
    b.face([(-.46,-1.178,.99),(.46,-1.178,.99),(.46,-.981,1.49),(-.46,-.981,1.49)], 'glass')
    b.PART = 'window_frame'
    b.beam((-.46,-.985,1.49),(.46,-.985,1.49),.018,'trim')
    b.beam((-.29,-1.181,1.01),(.06,-1.142,1.11),.016,'darkroof')
    for s in [-1, 1]:
        x = s*.558
        b.PART = 'glass'
        # Quarterlight and door pane follow the cabin's sloping A-pillar.
        b.face([(x,-1.12,.93),(x,-.81,.93),(x,-.81,1.49),(x,-.91,1.49)], 'glass')
        b.face([(x,-.775,.93),(x,-.25,.93),(x,-.25,1.49),(x,-.775,1.49)], 'glass')
        b.PART = 'window_frame'
        b.box(s*.565,-.793,.925,.018,.025,.575,'trim')
        b.PART = 'trim'
        b.box(x,-.30,.78,.025,.14,.035,'metal')
        b.beam((x,-1.01,1.11),(s*.66,-1.05,1.17),.035,'metal')
        b.box(s*.66,-1.05,1.14,.06,.11,.17,'metal')
        b.PART = 'door_seams'
        for y in [-1.02,-.22]:
            b.box(x,y,.58,.010,.012,.31,'darkroof')
        b.box(x,-.62,.575,.010,.80,.012,'darkroof')
        b.PART = 'step_brackets'
        for y in [-.81,-.33]:
            b.box(s*.48,y,.35,.24,.065,.10,'darkroof')
        b.PART = 'steps'
        b.box(s*.54,-.57,.42,.22,.59,.045,'darkroof')
    b.PART = 'loadbed_floor'
    b.box(0,.61,.54,1.26,1.31,.10,'body')
    b.PART = 'loadbed'
    for x in [-.60,.60]:
        b.box(x,.61,.64,.06,1.31,.38,'body')
        for y in [.08,.64,1.17]:
            b.box(x+(.035 if x>0 else -.035),y,.64,.022,.028,.38,'body')
    for y in [-.02,1.235]:
        b.box(0,y,.64,1.20,.06,.38,'body')
    b.PART = 'trim'
    b.box(0,-1.27,.48,.87,.07,.09,'darkroof')
    for x in [-.36,.36]:
        b.box(x,-1.26,.65,.17,.025,.12,'trim')
    b.PART = 'front_fork'
    for x in [-.095,.095]:
        b.beam((x,front_y,.225),(x,-.94,.57),.035,'metal')
    b.beam((-.115,front_y,.225),(.115,front_y,.225),.035,'metal')
    b.beam((-.11,-.94,.57),(.11,-.94,.57),.045,'darkroof')
    b.beam((0,-.94,.57),(0,-.81,.44),.065,'darkroof')
    b.PART = 'front_mudguard'
    for i in range(8):
        t=i*math.pi/8; q=(i+1)*math.pi/8
        b.face([(-.115,front_y+.265*math.cos(t),.225+.265*math.sin(t)),(.115,front_y+.265*math.cos(t),.225+.265*math.sin(t)),(.115,front_y+.265*math.cos(q),.225+.265*math.sin(q)),(-.115,front_y+.265*math.cos(q),.225+.265*math.sin(q))],'body')
    b.PART = 'rear_axle'
    b.beam((-.53,rear_y,.225),(.53,rear_y,.225),.065,'metal')
    b.PART = 'rear_suspension'
    for x in [-.40,.40]:
        b.box(x,rear_y,.225,.08,.32,.105,'darkroof')
    # Preserve old floor-contact flattening, with UV projection after shaping.
    original_face = b.face
    def wheel_face(points,tile='body',offset=(0,0)):
        points=[(x,y,0.0 if z<.030 else z) for x,y,z in points]
        for i in range(1,len(points)-1):
            original_face([points[0],points[i],points[i+1]],tile,offset)
    b.face=wheel_face
    try:
        _wheel(b,0,front_y,.225,.13)
        for x in [-.53,.53]: _wheel(b,x,rear_y,.225,.17)
    finally:
        b.face=original_face
    tris=sum(len(f)-2 for f in b.F[start:])
    assert tris <= 1500, tris
    return dict(form='three_wheel_drop_side_cargo',maxtris=1500,
                wheelbase_m=1.59,track_m=1.06,wheel_radius_m=.225,wheel_count=3,
                design_body_dimensions_m=[1.26,2.57,1.59],
                ground_footprint_m=[1.38,2.57],front_axis='-Y',rigged=False,
                skill='vehicle-artist',geometry_triangles=tris,
                loadpath=dict(crossmember_count=2,rail_top_z=.45,
                    crossmember_z=[.43,.56],floor_z=[.54,.64],
                    rail_crossmember_overlap_m=.02,crossmember_floor_overlap_m=.02,
                    cab_floor_z=[.43,.53],cab_floor_body_overlap_m=.01,
                    step_bracket_z=[.35,.45],step_z=[.42,.465],
                    step_bracket_overlap_m=.03),
                wear_locations=['step top','door handles','loadbed seams'])
