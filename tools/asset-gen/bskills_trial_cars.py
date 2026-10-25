"""Blender-skills trial cars. Metres, Z up, front -Y; host metric UV API.
Original props2 remains untouched; only its wheel and mirror primitives reused.
"""
import math
from bayloop_props2_vehicles import _wheel, _rounded_box

PROPORTIONS = [
    # body width, length, height, wheelbase, wheel radius, cabin front/rear,
    # roof front/rear. Locked before panel details per vehicle-artist.
    (1.48,3.20,1.43,1.98,.265,-.91,1.12,-.40,.61),
    (1.54,3.64,1.44,2.28,.280,-1.05,1.43,-.57,.79),
    (1.395,3.295,1.68,2.335,.280,-1.01,1.43,-.75,1.28),
]

def _lerp(a,c,t):return tuple(x+(y-x)*t for x,y in zip(a,c))

def _pane(b, quad, split=None):
    """Actual hole + border, no glass pasted over an opaque cabin prism.
    All points lie on the supplied planar quad; glass and surround meet edges.
    Quad order bottom-left, bottom-right, top-right, top-left.
    """
    bl,br,tr,tl=quad
    def at(u,v):return _lerp(_lerp(bl,br,u),_lerp(tl,tr,u),v)
    # 3-5 cm perimeter on these passenger-car surfaces.
    u0,u1,v0,v1=.035,.965,.075,.945
    inner=[at(u0,v0),at(u1,v0),at(u1,v1),at(u0,v1)]
    b.PART='body'
    for i in range(4):b.face([quad[i],quad[(i+1)%4],inner[(i+1)%4],inner[i]],'body')
    # Side B/C pillars use fixed physical Y, matching the lower door seams.
    # The previous darkroof tile visually merged with the glass; body-colour
    # 75mm pillars replace that strip and share exact boundaries with glass.
    lowleft,lowright=at(u0,v0),at(u1,v0)
    highleft,highright=at(u0,v1),at(u1,v1)
    def y_at(y,v):
        p,q=at(0,v),at(1,v)
        return _lerp(p,q,(y-p[1])/(q[1]-p[1]))
    panes=[]
    for y in split or []:
        lo0,lo1=y_at(y-.0375,v0),y_at(y+.0375,v0)
        hi0,hi1=y_at(y-.0375,v1),y_at(y+.0375,v1)
        panes.append([lowleft,lo0,hi0,highleft])
        b.PART='body'
        b.face([lo0,lo1,hi1,hi0],'body')
        lowleft,highleft=lo1,hi1
    panes.append([lowleft,lowright,highright,highleft])
    b.PART='glass'
    for pane in panes:b.face(pane,'glass')



def build(p,b):
    assert p.get('type',p.get('kind'))=='parked_car'
    v=int(p.get('variant',0))%3
    w,l,h,axle,r,cf,cr,rf,rr=PROPORTIONS[v]
    a=l/2;top=.79 if v!=2 else .86;shoulder=top-.12
    b.PART='chassis';b.box(0,0,.20,w-.44,l-.36,.20,'darkroof')
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
    # Cabin narrows upward: the sides and front/back remain genuine planes.
    rr=PROPORTIONS[v][-1]
    cw=w-.10;rw=cw-.16;roofz=h-.035
    left=[(-cw/2,cf,top),(-cw/2,cr,top),(-rw/2,rr,roofz),(-rw/2,rf,roofz)]
    right=[(-x,y,z) for x,y,z in left]
    # Roof has only a 35 mm crown: no tall upper band or separate roof plate.
    b.PART='body'
    b.face([left[3],right[3],(rw/2-.035,rf+.035,h),(-rw/2+.035,rf+.035,h)],'body')
    b.face([right[2],left[2],(-rw/2+.035,rr-.035,h),(rw/2-.035,rr-.035,h)],'body')
    b.face([left[2],left[3],(-rw/2+.035,rf+.035,h),(-rw/2+.035,rr-.035,h)],'body')
    b.face([right[3],right[2],(rw/2-.035,rr-.035,h),(rw/2-.035,rf+.035,h)],'body')
    b.face([(-rw/2+.035,rf+.035,h),(rw/2-.035,rf+.035,h),(rw/2-.035,rr-.035,h),(-rw/2+.035,rr-.035,h)],'body')
    pillar_y=[.03,.79] if v==2 else [.28]
    _pane(b,left,pillar_y)
    _pane(b,right,pillar_y)
    _pane(b,[left[0],right[0],right[3],left[3]])
    _pane(b,[right[1],left[1],left[2],right[2]])
    # Panel construction seams belong on body sides and hood boundary.
    b.PART='trim'
    for side in [-1,1]:
        x=side*(w/2+.004)
        trailing=.03 if v==2 else .28
        leading=-axle/2+r+.09
        for yy in [leading,trailing]:b.box(x,yy,.345,.008,.010,top-.345,'darkroof')
        b.box(x,(leading+trailing)/2,.34,.008,trailing-leading,.010,'darkroof')
        for yy in [trailing-.10,.72] if v==2 else [trailing-.10]:
            b.box(x,yy,top-.15,.020,.12,.028,'metal')
        if v==2:
            end=.79
            b.box(x,end,.345,.008,.010,top-.345,'darkroof')
            b.box(x,(trailing+end)/2,.34,.008,end-trailing,.010,'darkroof')
    # Horizontal hood seam, fascia, bumper and grille have separate values.
    b.box(0,cf-.025,top+.002,w-.16,.012,.003,'metal')
    for yy in [-a-.018,a+.018]:b.box(0,yy,.34,w-.10,.08,.10,'metal')
    b.PART='lights'
    for xx in [-w*.32,w*.32]:
        b.box(xx,-a-.012,.57,.27,.028,.12,'trim')
        b.box(xx,a+.012,.56,.18,.028,.13,'rust')
    b.PART='trim'
    b.box(0,-a-.016,.47,w*.45,.020,.09,'darkroof')
    b.box(0,-a-.029,.508,w*.43,.012,.012,'metal')
    for side in [-1,1]:
        b.PART='trim'
        b.beam((side*cw/2,cf+.2,.98),(side*(w/2+.055),cf+.16,1.0),.025,'metal')
        _rounded_box(b,side*(w/2+.055),cf+.16,.99,.09,.14,.075,.02,'body')
        for yy in [-axle/2,axle/2]:
            start=len(b.PARTS)
            _wheel(b,side*(w/2-.075),yy,r,.18)
            label='wheel_'+('left' if side<0 else 'right')+('_front' if yy<0 else '_rear')
            for i in range(start,len(b.PARTS)):b.PARTS[i]=label
    return dict(form=['rounded_european_hatch','angular_european_hatch','tall_japanese_kei'][v],
        design_body_dimensions_m=[w,l,h],wheelbase_m=axle,track_m=w-.15,
        wheel_radius_m=r,wheel_count=4,front_overhang_m=(l-axle)/2,
        rear_overhang_m=(l-axle)/2,cabin_front_rear_m=[cf,cr],roof_front_rear_m=[rf,rr],
        ground_footprint_m=[w,l],maxtris=1500,front_axis='-Y',rigged=False,
        skill_applied='vehicle-artist: proportion blockout and separate systems',
        wear_locations=['tire tread','bumper ends','door handles'])
