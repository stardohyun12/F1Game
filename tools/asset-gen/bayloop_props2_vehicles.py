"""D141 parked vehicles: metres, Z up, front -Y; geometry only.

Uses the host's metric b.face UVs; no bpy dependency, materials or exports.
"""
import math


def _prism(b, profile, width, tile='body'):
    """Extrude a convex Y/Z profile along X."""
    left=[(-width/2,y,z) for y,z in profile]
    right=[(width/2,y,z) for y,z in profile]
    b.face(list(reversed(left)),tile); b.face(right,tile)
    for i in range(len(profile)):
        j=(i+1)%len(profile)
        b.face([left[i],left[j],right[j],right[i]],tile)


def _wheel(b,x,y,r=.27,width=.17):
    b.PART='tires'
    # 16 sides retain a floor tangent while avoiding the stone-disc silhouette.
    rings=[]
    for xx,rr in [(x-width/2,r*.88),(x-width*.35,r),(x+width*.35,r),(x+width/2,r*.88)]:
        rings.append([(xx,y+rr*math.sin(i*math.tau/16),r-rr*math.cos(i*math.tau/16)) for i in range(16)])
    for a,c in zip(rings,rings[1:]):
        for i in range(16):j=(i+1)%16;b.face([a[i],a[j],c[j],c[i]],'rubber')
    for ring in [rings[0],rings[-1]]:b.face(ring,'rubber')
    b.PART='wheel_hubs'
    for side in [-1,1]:
        xx=x+side*(width/2+.003)
        b.face([(xx,y+r*.53*math.sin(i*math.tau/12),r-r*.53*math.cos(i*math.tau/12)) for i in range(12)],'trim')


def _rounded_box(b,x,y,z,w,d,h,bevel=.06,tile='body'):
    c=min(bevel,w/4,d/4,h/3)
    def ring(zz,inset):
        a=w/2-inset;q=d/2-inset;k=max(.005,c-inset*.5)
        return [(x+xx,y+yy,zz) for xx,yy in [(-a+k,-q),(a-k,-q),(a,-q+k),(a,q-k),(a-k,q),(-a+k,q),(-a,q-k),(-a,-q+k)]]
    rr=[ring(z,c*.5),ring(z+c,0),ring(z+h-c,0),ring(z+h,c*.5)]
    for low,high in zip(rr,rr[1:]):
        for i in range(8):j=(i+1)%8;b.face([low[i],low[j],high[j],high[i]],tile)
    b.face(list(reversed(rr[0])),tile);b.face(rr[-1],tile)


def _car(b,v):
    # compact rounded three-door, longer angular hatch, tall narrow kei wagon.
    w,l,h,axle,r=[(1.48,3.20,1.43,1.98,.265),(1.54,3.64,1.44,2.28,.28),(1.395,3.295,1.68,2.335,.255)][v]
    a=l/2; top=.79 if v!=2 else .86;shoulder=top-.12
    b.PART='underbody';b.box(0,0,.20,w-.44,l-.36,.20,'darkroof')
    b.PART='body'
    # One continuous shoulder cap. The former bevelled box overlapped the
    # side panels, leaving coplanar stripes along the entire belt line.
    cross=[(-w/2,shoulder),(w/2,shoulder),(w/2,top-.04),(w/2-.04,top),(-w/2+.04,top),(-w/2,top-.04)]
    front_ring=[(x,-a,z) for x,z in cross];back_ring=[(x,a,z) for x,z in cross]
    b.face(list(reversed(front_ring)),'body');b.face(back_ring,'body')
    for i in range(len(cross)):
        j=(i+1)%len(cross);b.face([front_ring[i],front_ring[j],back_ring[j],back_ring[i]],'body')
    # Closed front/rear fascia behind lamps and bumper: never an open cavity.
    for yy in [-a+.055,a-.055]:b.box(0,yy,.26,w,.11,shoulder-.26,'body')
    # Open wheel wells: each outer side is genuinely absent over the tire.
    for side in [-1,1]:
        x=side*w/2;wy=[-axle/2,axle/2];rr=r+.045
        # Butt joints: the fascia owns the end 11cm. Extending these side
        # boxes to +/-a duplicates its visible corner faces and z-fights.
        spans=[(-a+.11,wy[0]-rr),(wy[0]+rr,wy[1]-rr),(wy[1]+rr,a-.11)]
        for lo,hi in spans:
            b.box(x-side*.055,(lo+hi)/2,.26,.11,hi-lo,shoulder-.26,'body')
        for yy in wy:
            for i in range(6):
                p=math.pi*i/6;q=math.pi*(i+1)/6
                y0=yy+rr*math.cos(p);y1=yy+rr*math.cos(q)
                z0=r+rr*math.sin(p);z1=r+rr*math.sin(q)
                b.face([(x,y0,z0),(x,y1,z1),(x,y1,shoulder),(x,y0,shoulder)],'body')
                b.face([(x,y0,z0),(x-side*.12,y0,z0),(x-side*.12,y1,z1),(x,y1,z1)],'darkroof')
            for yy0 in [yy-rr,yy+rr]:
                b.box(x-side*.055,yy0,.26,.11,.015,r-.26 if r>.26 else .01,'body')
    b.PART='cabin'
    if v==0: profile=[(-.91,top),(-.53,1.30),(-.30,h),(.47,h),(.78,1.29),(1.12,top)];cw=w-.15
    elif v==1:profile=[(-1.05,top),(-.57,h-.05),(.75,h), (1.43,top)];cw=w-.14
    else:profile=[(-1.01,top),(-.76,h-.06),(-.64,h), (1.31,h),(1.43,top)];cw=w-.10
    _prism(b,profile,cw)
    # Windows subdivided by structural A/B/C pillars; real inset border geometry.
    b.PART='windows'
    front=profile[0]; roof_front=profile[1];rear=profile[-1];roof_rear=profile[-2]
    for side in [-1,1]:
        x=side*(cw/2+.008)
        f0=front[0]+.10;f1=roof_front[0]+.055;rz=roof_front[1]-.08
        re0=rear[0]-.12;re1=roof_rear[0]-.10;rh=roof_rear[1]-.09
        b.face([(x,f0,top+.08),(x,-.02,top+.08),(x,-.02,rz),(x,f1,rz)],'glass')
        b.face([(x,.065,top+.08),(x,re0,top+.08),(x,re1,rh),(x,.065,rh)],'glass')
        # Door hardware belongs on the wide lower body, not inside its shoulder.
        exterior=side*(w/2+.009)
        for yy in [-.08,.70] if v==2 else [.30]:
            b.box(exterior,yy,top-.16,.025,.15,.035,'metal')
        # Thin inset-like seam strips (not thick tubular decorative outlines).
        # The front door trailing seam aligns with the window B pillar.
        trailing=.025 if v==2 else .48
        leading=-axle/2+r+.09
        for yy in [leading,trailing]:
            b.box(exterior,yy,.345,.009,.013,top-.345,'darkroof')
        b.box(exterior,(leading+trailing)/2,.34,.009,trailing-leading,.013,'darkroof')
        if v==2:
            rear_end=axle/2-r-.09
            b.box(exterior,rear_end,.345,.009,.013,top-.345,'darkroof')
            b.box(exterior,(trailing+rear_end)/2,.34,.009,rear_end-trailing,.013,'darkroof')
    def sloped_window(p,q):
        y0,z0=p;y1,z1=q
        b.face([(-cw*.42,y0+(y1-y0)*.12-.008,z0+(z1-z0)*.12), (cw*.42,y0+(y1-y0)*.12-.008,z0+(z1-z0)*.12),(cw*.42,y0+(y1-y0)*.85-.008,z0+(z1-z0)*.85),(-cw*.42,y0+(y1-y0)*.85-.008,z0+(z1-z0)*.85)],'glass')
    sloped_window(front,roof_front)
    # Back screen is offset rearward, clear of the hatch panel.
    y0,z0=rear;y1,z1=roof_rear
    b.face([(-cw*.40,y0+(y1-y0)*.13+.008,z0+(z1-z0)*.13),(cw*.40,y0+(y1-y0)*.13+.008,z0+(z1-z0)*.13),(cw*.40,y0+(y1-y0)*.85+.008,z0+(z1-z0)*.85),(-cw*.40,y0+(y1-y0)*.85+.008,z0+(z1-z0)*.85)],'glass')
    b.PART='fittings'
    for yy in [-a-.018,a+.018]:b.box(0,yy,.34,w-.10,.08,.10,'darkroof')
    for xx in [-w*.32,w*.32]:
        b.box(xx,-a-.012,.57,.27,.028,.12,'trim');b.box(xx,a+.012,.56,.18,.028,.13,'rust')
    b.PART='radiator_grille'
    b.box(0,-a-.016,.465,w*.48,.020,.105,'darkroof')
    for zz in [.485,.525]:b.box(0,-a-.030,zz,w*.46,.014,.014,'metal')
    for xx in [-w*.24,w*.24]:b.box(xx,-a-.029,.465,.018,.014,.105,'metal')
    for side in [-1,1]:
        b.beam((side*cw/2,front[0]+.2,.98),(side*(w/2+.07),front[0]+.16,1.0),.035,'metal')
        _rounded_box(b,side*(w/2+.08),front[0]+.16,.99,.10,.16,.09,.025,'body')
        for yy in [-axle/2,axle/2]:_wheel(b,side*(w/2-.075),yy,r,.18)
    if v==0:
        b.PART='fabric_roof';b.box(0,.12,h+.003,cw*.64,.61,.012,'cloth')
    return {'form':['rounded_european_hatch','angular_european_hatch','tall_japanese_kei'][v],'design_body_dimensions_m':[w,l,h],'wheelbase_m':axle,'wheel_count':4,'ground_footprint_m':[w,l]}


def _three(b):
    b.PART='chassis';b.box(0,.20,.26,1.06,2.15,.13,'darkroof')
    b.PART='cab';_prism(b,[(-1.25,.52),(-1.25,.80),(-.96,1.54),(-.79,1.59),(-.15,1.59),(-.13,.52)],1.10)
    b.PART='windows'
    b.face([(-.46,-1.178,.99),(.46,-1.178,.99),(.46,-.981,1.49),(-.46,-.981,1.49)],'glass')
    b.PART='windshield_fittings'
    # Slim upper surround and a single parked wiper provide window structure.
    b.beam((-.46,-.985,1.49),(.46,-.985,1.49),.022,'metal')
    b.beam((-.29,-1.181,1.01),(.06,-1.142,1.11),.018,'darkroof')
    for s in [-1,1]:
        x=s*.558;b.face([(x,-1.12,.91),(x,-.25,.91),(x,-.25,1.49),(x,-.91,1.49)],'glass')
        b.PART='quarterlight_frame'
        # Small fixed quarterlight plus larger winding pane, not one black slab.
        b.box(s*.572,-.79,.925,.022,.025,.565,'metal')
        b.box(s*.572,-.68,.92,.022,.85,.021,'metal')
        b.box(s*.572,-.58,1.49,.022,.66,.021,'metal')
        b.box(x,-.3,.78,.025,.14,.035,'trim')
        b.beam((x,-1.01,1.11),(s*.66,-1.05,1.17),.035,'metal');b.box(s*.66,-1.05,1.14,.06,.11,.17,'metal')
        b.PART='cab_door_seams'
        for yy in [-1.02,-.22]:b.box(x,yy,.58,.012,.015,.31,'metal')
        b.box(x,-.62,.575,.012,.80,.015,'metal')
        b.PART='cab_step';b.box(s*.51,-.57,.42,.17,.56,.055,'metal')
    b.PART='cargo_bed';b.box(0,.61,.54,1.26,1.31,.10,'wood_dark')
    for x in [-.60,.60]:
        b.box(x,.61,.64,.06,1.31,.38,'body')
        for yy in [.08,.64,1.17]:b.box(x+( .035 if x>0 else -.035),yy,.64,.022,.035,.38,'metal')
    for yy in [-.02,1.235]:b.box(0,yy,.64,1.20,.06,.38,'body')
    b.PART='cargo';b.box(-.27,.76,.64,.44,.44,.31,'wood_dark')
    for z in [.69,.87]:b.box(-.27,.532,z,.43,.025,.045,'trim')
    b.PART='fittings';b.box(0,-1.27,.48,.87,.07,.09,'darkroof')
    for x in [-.36,.36]:b.box(x,-1.26,.65,.17,.025,.12,'trim')
    # Visible steering fork and wheel clearance below the cab, rather than
    # a tire disappearing directly into the solid driver's floor.
    b.PART='front_fork'
    for x in [-.095,.095]:b.beam((x,-1.12,.225),(x,-1.02,.65),.035,'metal')
    b.beam((-.115,-1.12,.225),(.115,-1.12,.225),.035,'metal')
    b.PART='front_mudguard'
    for i in range(8):
        t=i*math.pi/8;q=(i+1)*math.pi/8
        b.face([(-.115,-1.12+.265*math.cos(t),.225+.265*math.sin(t)),(.115,-1.12+.265*math.cos(t),.225+.265*math.sin(t)),(.115,-1.12+.265*math.cos(q),.225+.265*math.sin(q)),(-.115,-1.12+.265*math.cos(q),.225+.265*math.sin(q))],'body')
    # Actual lowest Z already equals rear-wheel Z. Widen the contact patch
    # across the beveled sidewall without sinking or lowering the vehicle.
    # Project UVs AFTER shaping; triangles keep the edited bevel faces planar.
    original_face=b.face
    def contact_face(points,tile='body',offset=(0,0)):
        points=[(x,y,0.0 if z<.030 else z) for x,y,z in points]
        for i in range(1,len(points)-1):original_face([points[0],points[i],points[i+1]],tile,offset)
    b.face=contact_face
    try:_wheel(b,0,-1.12,.225,.13)
    finally:b.face=original_face
    b.PART='rear_axle';b.beam((-.53,.79,.225),(.53,.79,.225),.065,'metal')
    for x in [-.40,.40]:b.beam((x,.47,.34),(x,1.04,.34),.055,'metal')
    for x in [-.53,.53]:_wheel(b,x,.79,.225,.17)
    return {'form':'three_wheel_drop_side_cargo','design_body_dimensions_m':[1.26,2.53,1.59],'wheel_count':3,'ground_footprint_m':[1.26,2.53]}


def _scooter(b,v):
    b.PART='frame';b.box(0,.03,.255,.15,.62,.045,'metal')
    _wheel(b,0,-.66,.24,.12);_wheel(b,0,.68,.24,.14)
    b.PART='rear_cowl';_rounded_box(b,0,.53,.32,.49,.70,.33,.09)
    b.PART='footboard';_rounded_box(b,0,-.04,.29,.43,.69,.065,.02,'darkroof')
    b.PART='leg_shield';_prism(b,[(-.60,.32),(-.72,.70),(-.58,1.01),(-.43,.97),(-.45,.45),(-.35,.32)],.48 if v==0 else .42)
    b.PART='seat';_rounded_box(b,0,.28 if v else .40,.70,.39,.52 if v else .70,.085,.03,'darkroof')
    b.PART='fork'
    for x in [-.085,.085]:b.beam((x,-.66,.24),(x,-.56,.77),.035,'metal')
    b.beam((-.105,-.66,.24),(.105,-.66,.24),.028,'metal')
    b.beam((0,-.56,.72),(0,-.56,.98),.045,'metal')
    b.PART='swingarm';b.beam((.115,.28,.37),(.115,.68,.24),.055,'metal')
    b.beam((-.10,.68,.24),(.14,.68,.24),.03,'metal')
    b.PART='front_fender';_rounded_box(b,0,-.66,.43,.19,.51,.12,.04)
    b.PART='handlebars';b.beam((-.34,-.57,1.04),(.34,-.57,1.04),.045,'metal')
    for s in [-1,1]:
        b.beam((s*.25,-.57,1.04),(s*.365,-.57,1.04),.055,'darkroof')
        b.beam((s*.25,-.57,1.05),(s*.30,-.55,1.25),.022,'metal')
        _rounded_box(b,s*.30,-.55,1.23,.10,.035,.075,.012,'metal')
    _rounded_box(b,0,-.60,1.0,.18,.16,.12,.03,'body');b.box(0,-.688,1.025,.12,.012,.065,'trim')
    b.PART='stand'
    for s in [-1,1]:
        b.beam((s*.10,.20,.30),(s*.24,.26,.02),.025,'metal')
        b.box(s*.24,.26,0,.075,.10,.02,'metal')
    b.PART='tail';b.box(0,.888,.53,.13,.025,.08,'rust')
    if v:
        b.PART='rear_rack';b.box(0,.86,.73,.46,.45,.035,'metal')
        for s in [-1,1]:
            b.beam((s*.17,.49,.50),(s*.17,.99,.73),.035,'metal')
            b.beam((s*.17,.40,.61),(s*.17,.70,.73),.035,'metal')
        b.PART='delivery_box';_rounded_box(b,0,.86,.78,.43,.40,.33,.025,'body')
        b.box(0,.86,1.11,.45,.42,.035,'darkroof')
        b.PART='box_mounts'
        for x in [-.145,.145]:b.box(x,.86,.76,.035,.34,.045,'metal')
        for x in [-.22,.22]:b.box(x,.86,.82,.018,.05,.30,'metal')
    return {'form':'step_through_delivery_scooter' if v else 'step_through_scooter','design_body_dimensions_m':[.75,1.87,1.31],'wheelbase_m':1.34,'wheel_count':2,'ground_footprint_m':[.75,1.87]}


def build(p,b):
    kind=p.get('type',p.get('kind'));v=int(p.get('variant',0))
    if kind=='parked_car':result=_car(b,v%3)
    elif kind=='three_wheeler':result=_three(b)
    elif kind=='scooter':result=_scooter(b,v%2)
    else:raise ValueError('Unknown vehicle type: '+str(kind))
    result.update(maxtris=1500,front_axis='-Y',wear_locations=['tire tread','bumper ends','door handles','cargo bed floor'] if kind!='scooter' else ['footboard','seat edge','stand foot'],rigged=False)
    return result
