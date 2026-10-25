"""D140 civic structures; geometry only, using the common metric photo atlas.

Coordinates are metres, front wall at Y=0, front faces -Y. No texture,
material, export or scene state is created here. build(p,b,i) is the API.
"""
import math


DEFAULTS = {
    'chapel_bell': (7., 9., 1), 'school_small': (10., 9., 2),
    'bus_stop': (4.8, 2.2, 1), 'fuel_stand': (7., 5., 1),
    'cafe_kiosk': (4., 3.5, 1),
}


def build(p, b, i):
    typ = p['type']
    dw, dd, df = DEFAULTS[typ]
    w = float(p.get('width', dw)); d = float(p.get('depth', dd))
    floors = int(p.get('floors', df))
    box, face, beam = b.box, b.face, b.beam

    def shifted(fn, x=0, y=0, z=0):
        start = len(b.V); fn()
        for n in range(start, len(b.V)):
            a, c, e = b.V[n]; b.V[n] = (a+x, c+y, e+z)

    def gable(ww, depth, z, rise, x=0, y=0, tile='roof_clay'):
        a=x-ww/2-.18; c=x+ww/2+.18; f=y-.22; r=y+depth+.22
        face([(a,f,z),(x,f,z+rise),(x,r,z+rise),(a,r,z)],tile)
        face([(x,f,z+rise),(c,f,z),(c,r,z),(x,r,z+rise)],tile)
        face([(a,f,z),(c,f,z),(x,f,z+rise)],'plaster')
        face([(c,r,z),(a,r,z),(x,r,z+rise)],'plaster')
        beam((x,f,z+rise),(x,r,z+rise),.12,tile)
        for xx in (a,c): beam((xx,f,z),(xx,r,z),.12,'wood_dark')

    def bench(x,y,width):
        box(x,y,.43,width,.40,.065,'wood_dark')
        box(x,y+.18,.52,width,.06,.34,'wood_dark')
        for xx in (x-width*.35,x+width*.35):box(xx,y,0,.07,.34,.43,'metal')

    def plain_window(x,y,z,ww=1.05,hh=1.5):
        # Institutional windows without residential louver complexity.
        box(x,y+.09,z,ww,.06,hh,'glass')
        for xx in (x-ww/2,x+ww/2):box(xx,y-.015,z,.07,.14,hh,'trim')
        for zz in (z,z+hh):box(x,y-.02,zz,ww+.14,.15,.07,'trim')
        box(x,y-.035,z, .045,.09,hh,'wood_dark')
        box(x,y-.04,z+hh*.52,ww,.09,.045,'wood_dark')
        box(x,y-.07,max(0,z-.08),ww+.18,.22,.08,'stone')

    def side_window(xx,yy,z,ww=1.,hh=1.5):
        start=len(b.V);plain_window(0,-.08,z,ww,hh)
        for n in range(start,len(b.V)):
            a,c,e=b.V[n];b.V[n]=(xx-c,yy+a,e)

    def arch_ring(x,y,z,width,height,depth=.20):
        # No backing plane: sunlight passes through the bell opening.
        r=width/2; spring=z+height-r; thickness=.16
        for xx in (x-r-thickness/2,x+r+thickness/2):
            box(xx,y,z,thickness,depth,height-r,'trim')
        for k in range(10):
            a=k*math.pi/10;c=(k+1)*math.pi/10
            for yy in (y-depth/2,y+depth/2):
                face([(x+r*math.cos(a),yy,spring+r*math.sin(a)),
                      (x+(r+thickness)*math.cos(a),yy,spring+(r+thickness)*math.sin(a)),
                      (x+(r+thickness)*math.cos(c),yy,spring+(r+thickness)*math.sin(c)),
                      (x+r*math.cos(c),yy,spring+r*math.sin(c))],'trim')
            face([(x+r*math.cos(a),y-depth/2,spring+r*math.sin(a)),
                  (x+r*math.cos(c),y-depth/2,spring+r*math.sin(c)),
                  (x+r*math.cos(c),y+depth/2,spring+r*math.sin(c)),
                  (x+r*math.cos(a),y+depth/2,spring+r*math.sin(a))],'stone')

    def bell(x,y,z):
        rings=[(.0,.35),(.12,.30),(.42,.19),(.55,.12)]
        for (zz,r),(zz2,r2) in zip(rings,rings[1:]):
            for k in range(10):
                a=k*math.tau/10;c=(k+1)*math.tau/10
                face([(x+r*math.cos(a),y+r*math.sin(a),z+zz),
                      (x+r*math.cos(c),y+r*math.sin(c),z+zz),
                      (x+r2*math.cos(c),y+r2*math.sin(c),z+zz2),
                      (x+r2*math.cos(a),y+r2*math.sin(a),z+zz2)],'rust')
        beam((x,y,z-.09),(x,y,z+.7),.065,'metal')

    if typ == 'chapel_bell':
        b.PART='chapel_nave';nw=w*.69;nx=-w*.155;h=4.45
        shifted(lambda:b.shell(nw,d,h,'plaster',[(-.7,.7,0,2.75)]),nx)
        b.arched_window(nx,.1,.08,1.4,2.67)
        box(nx,.025,.08,1.24,.045,1.95,'wood_dark')
        box(nx+.36,-.025,1.05,.04,.08,.2,'metal')
        for step in range(2):box(nx,-.19-step*.19,0,1.85,.42+step*.38,.12*(2-step),'stone')
        gable(nw,d,h,1.25,nx)
        # Modest facade oculus made as an octagonal stone ring.
        for k in range(12):
            a=k*math.tau/12;c=(k+1)*math.tau/12
            face([(nx+.30*math.cos(a),-.025,3.6+.30*math.sin(a)),
                  (nx+.38*math.cos(a),-.025,3.6+.38*math.sin(a)),
                  (nx+.38*math.cos(c),-.025,3.6+.38*math.sin(c)),
                  (nx+.30*math.cos(c),-.025,3.6+.30*math.sin(c))],'trim')
        box(nx,-.01,3.32,.53,.02,.55,'glass')
        b.PART='bell_tower';tw=w*.29;tx=w/2-tw/2;td=2.55
        box(tx,td/2,0,tw,td,5.5,'plaster')
        box(tx,td/2,5.45,tw+.18,td+.18,.18,'stone')
        for xx in (tx-tw/2+.16,tx+tw/2-.16):
            for yy in (.16,td-.16):box(xx,yy,5.6,.32,.32,2.0,'plaster')
        arch_ring(tx,.17,5.65,tw-.7,1.75)
        arch_ring(tx,td-.17,5.65,tw-.7,1.75)
        beam((tx-tw/2+.15,td/2,7.15),(tx+tw/2-.15,td/2,7.15),.13,'wood_dark')
        bell(tx,td/2,6.45)
        box(tx,td/2,7.58,tw+.18,td+.18,.18,'stone')
        gable(tw,td,7.76,.7,tx)
        beam((tx,td/2,8.35),(tx,td/2,9.0),.07,'metal')
        beam((tx-.18,td/2,8.78),(tx+.18,td/2,8.78),.07,'metal')
        b.PART='chapel_daily_life';bench(nx-1.4,-.45,1.1);b.pot(nx+1.55,-.35)
        box(tx,-.08,1.45,.5,.10,.7,'wood_dark');box(tx,-.14,1.52,.36,.025,.53,'cloth')
        form='small plaster nave, terracotta gable, open bell stage and real hanging bell'
    elif typ == 'school_small':
        b.PART='school_shell';floors=max(1,min(3,floors));fh=3.15;h=floors*fh
        xs=[-w*.36,-w*.18,w*.18,w*.36];ops=[(-.75,.75,.15,2.6)]
        for level in range(floors):ops += [(x-.55,x+.55,level*fh+.85,level*fh+2.5) for x in xs]
        b.shell(w,d,h,'plaster',ops)
        for level in range(floors):
            for x in xs:plain_window(x,0,level*fh+.85,1.1,1.65)
        b.arched_window(0,.1,.15,1.5,2.45);box(0,.025,.15,1.3,.04,.65,'wood_dark')
        box(0,-.35,0,2.15,.75,.15,'stone')
        for zz in (3.08,h-.10):box(0,-.035,zz,w,.10,.12,'trim')
        gable(w,d,h,1.25)
        for yy in (2.4,d-2.0):
            for level in range(floors):side_window(w/2,yy,level*fh+.85,1.25,1.65)
        b.PART='school_entry_life';i.awning(0,0,2.9,2.15)
        box(-1.2,-.09,1.55,.65,.13,.8,'wood_dark');box(-1.2,-.17,1.65,.48,.025,.58,'cloth')
        for x in (-w*.36,w*.36):bench(x,-.65,1.35)
        b.pot(w/2-.4,-.5)
        beam((w/2-.1,-.09,.1),(w/2-.1,-.09,h-.1),.06,'metal')
        form='two-storey village school, repeated tall classroom windows, central sheltered door and benches'
    elif typ == 'bus_stop':
        b.PART='bus_shelter';h=2.45
        box(0,d/2,0,w,d,.08,'stone')
        for xx in (-w/2+.14,w/2-.14):
            for yy in (.15,d-.15):box(xx,yy,.08,.09,.09,h,'metal')
        box(0,d-.12,.08,w-.2,.12,1.2,'stonewall')
        box(0,d-.08,1.3,w-.2,.08,1.1,'glass')
        face([(-w/2-.1,-.2,h),(w/2+.1,-.2,h),(w/2+.1,d+.15,h+.22),(-w/2-.1,d+.15,h+.22)],'metal')
        for yy in (-.2,d+.15):box(0,yy,h-.1,w+.2,.09,.13,'metal')
        bench(0,d-.53,w*.64)
        b.PART='bus_stop_marker';xx=-w/2+.18
        box(xx,-.18,0,.07,.07,2.65,'metal')
        box(xx,-.18,2.2,.5,.12,.52,'teal')
        box(xx,-.25,2.27,.35,.025,.34,'cloth')
        box(w/2-.27,d-.19,1.35,.48,.06,.82,'wood_dark')
        box(w/2-.27,d-.23,1.42,.35,.025,.68,'cloth')
        form='open roadside shelter, worn metal sloping roof, bench, stop pole and timetable case'
    elif typ == 'fuel_stand':
        b.PART='fuel_canopy';h=3.5
        for xx in (-w*.38,w*.38):box(xx,d*.52,0,.18,.18,h,'metal')
        box(0,d/2,h,w,d,.22,'trim')
        box(0,-.025,h+.035,w,.07,.12,'teal')
        box(0,d/2,h+.22,w-.12,d-.12,.035,'metal')
        b.PART='pump_island';box(0,d*.48,0,w*.64,.86,.12,'stone')
        for xx in (-w*.20,w*.20):
            yy=d*.48
            box(xx,yy,.12,.62,.48,.72,'teal');box(xx,yy,.84,.68,.52,.7,'trim')
            box(xx,yy-.27,1.12,.44,.025,.22,'glass')
            box(xx,yy-.28,.92,.35,.035,.1,'metal')
            # Low polygon hose follows the casing edge, with a visible nozzle.
            path=[(xx+.37,yy,1.36),(xx+.56,yy,1.20),(xx+.62,yy,.43),(xx+.43,yy,.32),(xx+.35,yy,.7)]
            for a,c in zip(path,path[1:]):beam(a,c,.045,'wood_dark')
            beam((xx+.35,yy,.7),(xx+.37,yy,.93),.075,'metal')
        b.PART='fuel_service_hut';hw=w*.32;hd=1.65;hx=w*.31;hy=d+.2
        shifted(lambda:b.shell(hw,hd,2.6,'plaster',[(-.45,.45,.05,2.1)]),hx,hy)
        plain_window(hx,hy,.05,.9,2.05);box(hx,hy+hd/2,2.6,hw+.1,hd+.1,.14,'metal')
        box(-w*.43,-.12,0,.12,.12,2.7,'metal')
        box(-w*.43,-.12,1.95,.6,.12,.7,'teal')
        form='small two-pump forecourt, thin canopy, hose loops and detached attendant hut'
    elif typ == 'cafe_kiosk':
        b.PART='kiosk_shell';h=2.65
        b.shell(w,d,h,'plaster',[(-w*.34,w*.34,.95,2.35)])
        # Counter opening has visible interior depth, shelves and service objects.
        box(0,.38,.92,w*.76,.8,.10,'stone')
        box(0,d-.27,1.0,w*.68,.12,1.3,'wood_dark')
        for zz in (1.35,1.82):
            box(0,d-.38,zz,w*.7,.32,.055,'wood_dark')
            for k in range(5):box(-w*.28+k*w*.14,d-.45,zz+.06,.17,.13,.23 if k%2 else .3,'cloth' if k%2 else 'teal')
        gable(w,d,h,.55)
        i.awning(0,0,2.6,w+.1)
        b.PART='cafe_terrace';bench(-w*.28,-.7,1.05)
        box(w*.27,-.75,.77,.65,.6,.07,'wood_dark')
        box(w*.27,-.75,0,.07,.07,.77,'metal')
        b.pot(w/2-.2,-.45)
        box(-w/2+.28,-.09,1.25,.42,.12,.7,'wood_dark')
        box(-w/2+.28,-.16,1.32,.29,.025,.53,'cloth')
        # Side access into the kiosk is distinct from the public counter.
        side_window(w/2,d-.65,.08,.7,2.05)
        form='walk-up kiosk with deep open serving counter, shelves, cloth awning and modest outside seating'
    else:
        raise ValueError(typ)
    return dict(footprint_m=[w,d],storeys=floors,front_wall_z=0,
                form=form,life_layer='human-scale notice boards, seating and service attachments',
                wear_locations=['entrance threshold','lower masonry','roof edge','handled wood and metal'])
