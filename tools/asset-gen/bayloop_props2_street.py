"""D141 street life geometry; metres, Z up, fronts -Y, metric photo UVs.

Only appends to the supplied bayloop_buildings geometry collector. No bpy,
material, export, atlas or global scene ownership. Main generator recentres.
"""
import math


TYPES = ('laundry_line', 'traffic_mirror', 'street_sign', 'crosswalk_paint',
         'road_patch', 'shop_display', 'cafe_set', 'parasol',
         'mailbox_cluster', 'garage_door_inset')


def _cyl(b, x, y, z, r, h, tile='metal', n=12, r2=None):
    r2 = r if r2 is None else r2
    lo = [(x+r*math.cos(i*math.tau/n), y+r*math.sin(i*math.tau/n), z) for i in range(n)]
    hi = [(x+r2*math.cos(i*math.tau/n), y+r2*math.sin(i*math.tau/n), z+h) for i in range(n)]
    for i in range(n):
        j = (i+1) % n
        b.face([lo[i], lo[j], hi[j], hi[i]], tile)
    b.face(lo[::-1], tile)
    b.face(hi, tile)


def _plate(b, outline, y, depth, tile):
    """Convex XZ outline, counterclockwise as seen from front."""
    front = [(x, y, z) for x, z in outline]
    back = [(x, y+depth, z) for x, z in outline]
    b.face(front, tile)
    b.face(back[::-1], tile)
    for i in range(len(front)):
        j = (i+1) % len(front)
        b.face([front[j], front[i], back[i], back[j]], tile)


def _disc(b, x, y, z, r, tile, n=16, depth=.025):
    _plate(b, [(x+r*math.cos(i*math.tau/n), z+r*math.sin(i*math.tau/n)) for i in range(n)], y, depth, tile)


def _ring(b, x, y, z, outer, inner, tile, n=16):
    for i in range(n):
        a, c = i*math.tau/n, (i+1)*math.tau/n
        b.face([(x+inner*math.cos(a), y, z+inner*math.sin(a)),
                (x+outer*math.cos(a), y, z+outer*math.sin(a)),
                (x+outer*math.cos(c), y, z+outer*math.sin(c)),
                (x+inner*math.cos(c), y, z+inner*math.sin(c))], tile)


def _chair(b, x, y, angle):
    def pt(a, c, z):
        return (x+a*math.cos(angle)-c*math.sin(angle), y+a*math.sin(angle)+c*math.cos(angle), z)
    def rail(a, c, w=.025, tile='metal'):
        b.beam(pt(*a), pt(*c), w, tile)
    for xx in [-.19, .19]:
        for yy in [-.18, .18]:
            rail((xx*1.22, yy*1.25, .015), (xx, yy, .46))
        rail((xx, .18, .43), (xx, .25, .86))
        rail((xx, -.18, .25), (xx, .18, .25), .018)
    for yy in [-.16, -.055, .05, .155]:
        rail((-.205, yy, .46), (.205, yy, .46), .075, 'wood_dark')
    for zz in [.65, .77, .85]:
        rail((-.21, .18+(zz-.43)*.163, zz), (.21, .18+(zz-.43)*.163, zz), .065, 'teal')


def build(p, b):
    """p: {'type': TYPES member, 'variant': int}; returns placement/design data."""
    kind = p.get('type', p.get('kind'))
    if kind not in TYPES:
        raise ValueError('Unknown street prop: '+str(kind))
    v = int(p.get('variant', 0))
    start = len(b.V)
    b.PART = kind
    mount = 0.0
    offset = 0.0
    notes = []
    if kind == 'cafe_set':
        _cyl(b, 0, 0, .715, .36, .045, 'wood_dark', 12)
        _cyl(b, 0, 0, .055, .028, .66, 'metal', 8)
        for a in [0, math.tau/3, math.tau*2/3]:
            b.beam((0, 0, .17), (.30*math.cos(a), .30*math.sin(a), .025), .030, 'metal')
        _chair(b, -.64, .08, math.pi/2)
        _chair(b, .63, -.13, -math.pi/2+.12)
        # Open ceramic cup: thick rim, dark liquid below the mouth, inner wall.
        cx,cy=.10,.05
        for i in range(10):
            a=i*math.tau/10;c=(i+1)*math.tau/10
            def cp(r,ang,z):return(cx+r*math.cos(ang),cy+r*math.sin(ang),z)
            b.face([cp(.035,a,.76),cp(.035,c,.76),cp(.043,c,.838),cp(.043,a,.838)],'trim')
            b.face([cp(.043,a,.838),cp(.043,c,.838),cp(.033,c,.838),cp(.033,a,.838)],'trim')
            b.face([cp(.033,a,.838),cp(.033,c,.838),cp(.029,c,.814),cp(.029,a,.814)],'trim')
            b.face([(cx,cy,.814),cp(.029,a,.814),cp(.029,c,.814)],'wood_dark')
        for a,c in [((.139,.05,.82),(.17,.05,.82)),((.17,.05,.82),(.17,.05,.782)),((.17,.05,.782),(.136,.05,.782))]:
            b.beam(a,c,.012,'trim')
        b.box(-.12, -.05, .761, .16, .12, .012, 'cloth')
        notes = ['760mm cafe table, two pulled-out slatted chairs, genuinely open rimmed cup with handle and recessed drink; table steel proportions reduced to match chairs.', 'Wear: chair feet, tabletop edge, hand-contact back rails.']
    elif kind == 'shop_display':
        # Open slatted fruit stand, not a closed crate-shaped solid.
        for x in [-.55, .55]:
            for y in [-.29, .29]:
                b.beam((x, y, 0), (x, y, .96), .055, 'wood_dark')
            b.beam((x, -.29, .25), (x, .29, .25), .035, 'wood_dark')
        for z in [.30, .73]:
            for y in [-.26, -.13, 0, .13, .26]:
                b.box(0, y, z, 1.13, .105, .035, 'wood_dark')
        for x in [-.29, .29]:
            for y in [-.30, .30]:
                for z in [.78, .87]:
                    b.box(x, y, z, .53, .035, .065, 'wood_dark')
            for xx in [x-.265, x+.265]:
                for z in [.78, .87]:
                    b.box(xx, 0, z, .035, .60, .065, 'wood_dark')
            for j in range(6):
                xx=x-.16+(j%3)*.16; yy=-.14+(j//3)*.24
                tile='roof_clay' if x<0 else 'stonewall'
                rings=[[(xx+r*math.cos(k*math.tau/8),yy+r*math.sin(k*math.tau/8),zz) for k in range(8)] for r,zz in [(.045,.785),(.078,.835),(.049,.888)]]
                for k in range(8):
                    l=(k+1)%8
                    b.face([(xx,yy,.773),rings[0][l],rings[0][k]],tile)
                    for low,high in zip(rings,rings[1:]):b.face([low[k],low[l],high[l],high[k]],tile)
                    b.face([rings[-1][k],rings[-1][l],(xx,yy,.902)],tile)
                b.beam((xx,yy,.898),(xx+.006,yy,.921),.009,'wood_dark')
        b.box(.30, -.334, .80, .14, .015, .075, 'wood_dark')
        b.box(.30, -.344, .81, .12, .004, .052, 'cloth')
        # Gathered cloth sack replaces the featureless white storage cube.
        _cyl(b,-.33,.02,.335,.15,.15,'cloth',10,.125)
        _cyl(b,-.33,.02,.485,.125,.07,'cloth',10,.042)
        _cyl(b,-.33,.02,.555,.042,.035,'wood_dark',8,.04)
        _cyl(b,-.33,.02,.59,.04,.045,'cloth',8,.065)
        notes = ['1.15m open two-tier greengrocer rack, twelve round faceted fruit with stems, open crate rails and gathered sack.', 'Wear: crate lifting edges and stand feet; smaller recessed price holder, no fake writing.']
    elif kind == 'road_patch':
        if v%2==0:
            outline=[(-1.1,-.40),(-.81,-.64),(-.42,-.61),(-.28,-.64),(.20,-.59),(.64,-.49),(.70,-.44),(.97,-.38),(1.12,.18),(.98,.29),(.94,.34),(.67,.61),(.23,.56),(.10,.59),(-.45,.56),(-.74,.44),(-.79,.39),(-1.02,.30)]
        else:
            # Long utility trench, clipped corners and two small broken sawcuts.
            outline=[(-1.65,-.34),(-1.50,-.42),(-.54,-.42),(-.47,-.39),(.16,-.42),(1.56,-.42),(1.67,-.31),(1.65,.33),(1.50,.40),(.69,.40),(.61,.37),(-.24,.40),(-1.54,.40),(-1.67,.28)]
        rough=[]
        for j,(a,c) in enumerate(zip(outline,outline[1:]+outline[:1])):
            dx,dy=c[0]-a[0],c[1]-a[1];length=math.hypot(dx,dy)
            count=max(1,math.ceil(length/.115))
            for k in range(count):
                t=k/count;noise=.009*math.sin(j*5.1+k*2.3+v)*math.sin(math.pi*t)
                rough.append((a[0]+dx*t-dy/length*noise,a[1]+dy*t+dx/length*noise))
        outline=rough
        fstart=len(b.F)
        for a,c in zip(outline,outline[1:]+outline[:1]):
            b.face([(0,0,0),(a[0],a[1],0),(c[0],c[1],0)],'darkroof')
        # Continuous photographed asphalt across all triangles; no bright rim.
        idx=b.TILES['darkroof']
        for fi in range(fstart,len(b.F)):
            b.UV[fi]=[(((idx%4)*256+2+44+(b.V[vi][0]+1.7)*32)/b.N,
                       1-((idx//4)*256+2+40+v*40+(b.V[vi][1]+.7)*32)/b.N)
                      for vi in b.F[fi]]
        # Thin irregular tar seal follows the actual joint, not a bright frame.
        inner=[]
        for j,a in enumerate(outline):
            prev=outline[j-1];nxt=outline[(j+1)%len(outline)]
            ex,ey=a[0]-prev[0],a[1]-prev[1];fx,fy=nxt[0]-a[0],nxt[1]-a[1]
            el=math.hypot(ex,ey);fl=math.hypot(fx,fy)
            nx,ny=-ey/el-fy/fl,ex/el+fx/fl;nl=math.hypot(nx,ny)
            width=.043+.014*math.sin(j*1.7+v*.8)
            inner.append((a[0]+nx/nl*width,a[1]+ny/nl*width))
        for j,a in enumerate(outline):
            k=(j+1)%len(outline);c=outline[k]
            b.face([(a[0],a[1],.0004),(c[0],c[1],.0004),(inner[k][0],inner[k][1],.0004),(inner[j][0],inner[j][1],.0004)],'seam')
        offset=.002
        notes=['Opaque flush asphalt repair: irregular resurfaced area (01) or long utility trench (02), perimeter sampled at 115mm with sub-centimetre broken cuts.', 'Variable 29-57mm dark tar joint (seam tile13), no bright or raised rim; 0.4mm seal surface over asphalt. Place 2mm above road, no collider.']
    elif kind == 'traffic_mirror':
        _cyl(b,0,.09,0,.075,.12,'stone',8)
        _cyl(b,0,.09,.12,.032,2.20,'metal',8)
        b.beam((0,.09,2.1), (0,-.13,2.1), .045, 'metal')
        _disc(b,0,-.18,2.12,.32,'rust',16,.075)
        # Convex photo-silver dish with an explicit horizon. Each original fan
        # triangle is clipped at the horizontal reflection band, not recoloured.
        def clipz(poly, height, above):
            out=[]
            for a,c in zip(poly,poly[1:]+poly[:1]):
                ai=a[2]>=height if above else a[2]<=height
                ci=c[2]>=height if above else c[2]<=height
                if ai:out.append(a)
                if ai!=ci:
                    t=(height-a[2])/(c[2]-a[2])
                    out.append(tuple(a[k]+t*(c[k]-a[k]) for k in range(3)))
            return out
        for i in range(16):
            a=i*math.tau/16;c=(i+1)*math.tau/16
            poly=[(0,-.25,2.12),(.285*math.cos(a),-.195,2.12+.285*math.sin(a)),(.285*math.cos(c),-.195,2.12+.285*math.sin(c))]
            for low,high,tile in [(1.80,2.08,'metal'),(2.08,2.13,'trim'),(2.13,2.45,'trim')]:
                cut=clipz(clipz(poly,low,True),high,False)
                if len(cut)>=3:b.face(cut,tile,(64,48) if low>2.1 else (0,0))
        for i in range(8):
            a=i*math.pi/8;c=(i+1)*math.pi/8
            b.face([(.33*math.cos(a),-.22,2.12+.33*math.sin(a)),(.33*math.cos(c),-.22,2.12+.33*math.sin(c)),(.33*math.cos(c),-.12,2.12+.33*math.sin(c)),(.33*math.cos(a),-.12,2.12+.33*math.sin(a))],'rust')
        b.box(0,-.055,1.77,.13,.16,.12,'metal')
        _disc(b,0,-.145,1.83,.035,'trim',8,.025)
        notes=['640mm convex corner mirror with close-fitting rain hood, visible adjustment bolt and 2.1m centre.', 'Upper sky reflection uses white photo tile, lower reflection metal photo tile; static faceted reflection, not live reflection.']
    elif kind == 'street_sign':
        _cyl(b,0,.035,0,.035,2.36 if v%3==1 else 2.45,'metal',8)
        z=2.20
        if v%3==0:
            pts=[(.35*math.cos(math.pi/8+i*math.pi/4),z+.35*math.sin(math.pi/8+i*math.pi/4)) for i in range(8)]
            _plate(b,pts,-.065,.035,'trim')
            _plate(b,[(x*.9,z+(zz-z)*.9) for x,zz in pts],-.07,.004,'rust')
            # Open palm: genuine pictogram geometry, no letters or fake glyphs.
            _plate(b,[(-.105,z-.11),(.075,z-.11),(.105,z+.015),(.085,z+.09),(-.105,z+.09)],-.079,.003,'trim')
            for x,top in [(-.083,.185),(-.03,.225),(.023,.235),(.076,.20)]:
                b.box(x,-.080,z+.06,.038,.004,top-.06,'trim')
            _plate(b,[(-.08,z+.015),(-.15,z+.075),(-.185,z+.065),(-.19,z+.015),(-.105,z-.065)],-.081,.003,'trim')
            b.box(-.01,-.080,z-.17,.115,.004,.065,'trim')
            notes=['Octagonal stop sign with open-palm stop pictogram from opaque white photo geometry; no text.']
        elif v%3==1:
            b.box(0,-.035,z-.20,.72,.055,.40,'teal')
            # Arrow split into two convex polygons for safe metric clipping.
            b.face([(-.27,-.067,z-.055),(.10,-.067,z-.055),(.10,-.067,z+.055),(-.27,-.067,z+.055)],'trim')
            b.face([(.08,-.068,z-.14),(.28,-.068,z),(.08,-.068,z+.14)],'trim')
            for zz in [z-.155,z+.155]:
                _disc(b,0,-.071,zz,.014,'metal',6,.008)
            notes=['One-way directional arrow made from opaque geometry; two visible 28mm face fixing bolts and pole ending behind plate at 2.36m.']
        else:
            _disc(b,0,-.065,z,.32,'teal',16)
            _ring(b,0,-.071,z,.32,.27,'warning')
            # Front-view car pictogram, not a letter: roof, glazing, bonnet,
            # headlights and two wheels identify the prohibited subject.
            _plate(b,[(-.17,z-.065),(.17,z-.065),(.17,z+.025),(-.17,z+.025)],-.078,.003,'paint')
            _plate(b,[(-.15,z+.025),(.15,z+.025),(.10,z+.13),(-.10,z+.13)],-.078,.003,'paint')
            _plate(b,[(-.105,z+.045),(.105,z+.045),(.075,z+.105),(-.075,z+.105)],-.083,.002,'darkroof')
            for x in [-.12,.12]:
                b.box(x,-.080,z-.115,.045,.005,.055,'paint')
                b.box(x,-.083,z-.015,.037,.003,.022,'darkroof')
            b.beam((-.20,-.105,z-.20),(.20,-.105,z+.20),.055,'warning')
            notes=['Car prohibition pictogram: photographed white car silhouette under red photo-tile ring and slash; no P or other letters.', 'Fictional neighborhood sign, not a regulatory sign certification.']
        for zz in [1.98,2.35]:
            b.box(0,.07,zz,.16,.025,.035,'rust')
    elif kind == 'crosswalk_paint':
        for i in range(6):
            x=-2.125+i*.85
            # Erosion enters from an outside edge only; never splits a bar.
            chips=[(-.82+i*.21,-1,.07,.055),(.79-i*.13,1,.045,.038)]
            if i%2:chips.append((-.12+i*.11,1,.055,.07))
            bounds=sorted(set([-1.25,1.25]+[-1.25+j*2.5/24 for j in range(1,24)]+[q for at,side,w,depth in chips for q in [at-w,at,at+w]]))
            fstart=len(b.F)
            def edge(y,side):
                erosion=sum(depth*max(0,1-abs(y-at)/w) for at,s,w,depth in chips if s==side)
                return x+side*(.25-erosion)+.009*math.sin(y*13+i*2+side)+.006*math.sin(y*29+i)
            for ya,yc in zip(bounds,bounds[1:]):
                b.face([(edge(ya,-1),ya,0),(edge(ya,1),ya,0),(edge(yc,1),yc,0),(edge(yc,-1),yc,0)],'paint')
            # Different photo crop per stripe, continuous through worn gaps.
            idx=b.TILES['paint']
            for fi in range(fstart,len(b.F)):
                b.UV[fi]=[(((idx%4)*256+2+24+i*24+(b.V[vi][0]-x+.27)*32)/b.N,
                           1-((idx//4)*256+2+24+(i%3)*35+(b.V[vi][1]+1.25)*32)/b.N)
                          for vi in b.F[fi]]
        offset=.002
        notes=['Six continuous 0.5m zebra bars, 0.35m gaps, 2.5m depth; each bar has 2-3 small irregular edge chips, no full-width seams or breaks.', 'Zero-thickness opaque paint at Z=0, place 2mm above road; no backing or collider. Continuous UV crop differs per stripe without adding noise.']
    elif kind == 'parasol':
        _cyl(b,0,0,0,.27,.10,'stone',8)
        _cyl(b,0,0,.10,.025,2.16,'wood_dark',8)
        _cyl(b,0,0,1.62,.060,.10,'metal',10)
        _cyl(b,0,0,2.22,.072,.055,'metal',10)
        b.box(.036,0,1.10,.055,.075,.11,'metal')
        b.beam((.055,0,1.14),(.12,0,1.07),.018,'metal')
        b.beam((.12,0,1.07),(.12,-.065,1.07),.022,'wood_dark')
        for i in range(8):
            a=i*math.tau/8;c=(i+1)*math.tau/8
            tile='cloth' if i%2 else 'trim'
            rings=[(0,2.30),(.42,2.245),(.78,2.105),(1.12,1.91)]
            def canopy(r,z,t):
                return(r*((1-t)*math.cos(a)+t*math.cos(c)),r*((1-t)*math.sin(a)+t*math.sin(c)),z-.020*(r/1.12)*math.sin(t*math.pi))
            for j in range(3):
                t0=j/3;t1=(j+1)/3
                for k in range(3):
                    r0,z0=rings[k];r1,z1=rings[k+1]
                    p0=canopy(r0,z0,t0);p1=canopy(r1,z1,t0);p2=canopy(r1,z1,t1);p3=canopy(r0,z0,t1)
                    triangles=[[p0,p1,p2]] if k==0 else [[p0,p1,p2],[p0,p2,p3]]
                    for tri in triangles:
                        b.face(tri,tile)
                        b.face([(x,y,z-.002) for x,y,z in tri[::-1]],tile)
                # A short turned cloth hem replaces the 10cm continuous skirt.
                e0=canopy(1.12,1.91,t0);e1=canopy(1.12,1.91,t1)
                hem=[e0,(e0[0],e0[1],e0[2]-.018),(e1[0],e1[1],e1[2]-.018),e1]
                b.face(hem,tile)
                b.face([(x*.999,y*.999,z) for x,y,z in hem[::-1]],tile)
            rib=[(0,0,2.20),(.42*math.cos(a),.42*math.sin(a),2.215),(.78*math.cos(a),.78*math.sin(a),2.078),(1.075*math.cos(a),1.075*math.sin(a),1.915)]
            for p0,p1 in zip(rib,rib[1:]):b.beam(p0,p1,.016,'metal')
            b.beam((0,0,1.70),(.70*math.cos(a),.70*math.sin(a),2.10),.014,'metal')
        notes=['2.24m octagonal cafe parasol with sampled curved canopy and 20mm between-rib sag; 2mm inner cloth skin and short 18mm turned hem instead of rigid skirt.', 'Visible upper hub, sliding runner, eight segmented ribs and stretcher links plus hand crank; weighted base.']
    elif kind == 'mailbox_cluster':
        for i in range(3):
            x=(i-1)*.35; z=.025*(i%2)
            b.box(x,-.12,z,.32,.24,.40,['teal','rust','metal'][i])
            b.box(x,-.248,z+.29,.24,.012,.024,'darkroof')
            b.box(x,-.255,z+.275,.26,.035,.025,'metal')
            b.box(x,-.25,z+.10,.135,.012,.052,'metal')
            b.box(x,-.258,z+.109,.105-.01*i,.004,.032,'cloth')
            b.box(x+.10,-.26,z+.18,.025,.014,.025,'metal')
            roof=[(x-.17,-.27,z+.413),(x+.17,-.27,z+.413),(x+.17,.01,z+.445),(x-.17,.01,z+.445)]
            under=[(xx,yy,zz-.012) for xx,yy,zz in roof]
            b.face(roof,'metal');b.face(under[::-1],'metal')
            for k in range(4):
                l=(k+1)%4;b.face([roof[k],under[k],under[l],roof[l]],'metal')
            b.box(x,-.272,z+.39,.34,.012,.023,'metal')
        mount=1.10
        notes=['Three separate wall letterboxes; slots, hinged rain flaps, small recessed paper name holders and locks; folded 12mm closed roof with downturned drip lip.', 'Asset bottom pivot; installation bottom 1.10m above paving; wear at flap and keyhole; no invented lettering.']
    elif kind == 'garage_door_inset':
        w=2.5;h=2.2
        for x in [-w/2,w/2]:
            b.box(x,.08,0,.12,.22,h,'stone')
        b.box(0,.08,h,w+.12,.22,.13,'stone')
        b.box(0,.15,.035,w-.12,.055,h-.035,'darkroof')
        for i in range(17):
            b.box(0,.10,.055+i*.13,w-.20,.05,.124,'teal' if v%2 else 'metal')
        for x in [-.22,.22]:
            b.box(x,.045,.36,.04,.06,.06,'metal')
        b.beam((-.22,-.015,.39),(.22,-.015,.39),.035,'trim')
        b.box(0,-.08,0,w+.25,.55,.045,'stone')
        notes=['2.5m roll-up garage insert with recessed curtain, 17 slats with narrow 6mm joints, butt-jointed jamb/header (no overlapping front faces) and projecting bright handled grip.', 'Use in an actual host-wall opening; raised 45mm worn threshold. Low-contrast metal atlas is supplied upstream; no geometry noise added.']
    elif kind == 'laundry_line':
        # Wall anchors + gently sagging cable; module bottom is garment hem.
        def linez(x): return 1.15+.15*(abs(x)/1.6)**2
        for i in range(12):
            a=-1.6+i*3.2/12;c=-1.6+(i+1)*3.2/12
            b.beam((a,0,linez(a)),(c,0,linez(c)),.012,'metal')
        for x in [-1.6,1.6]:
            b.box(x,.045,1.20,.09,.12,.16,'rust')
        def fabric(poly,tile,fold_offset=0):
            # One continuous garment-space UV mapping: small triangles must not
            # restart the photograph and create repeated rectangular markings.
            pts=[(x,.024*math.sin(x*15)+.014*math.sin(z*5+x*3)+fold_offset,z) for x,z in poly]
            for i in range(1,len(pts)-1):
                tri=[pts[0],pts[i],pts[i+1]]
                for side in [tri,tri[::-1]]:
                    fstart=len(b.F)
                    b.face(side,tile)
                    idx=b.TILES[tile]
                    for fi in range(fstart,len(b.F)):
                        target=[(((idx%4)*256+2+48+(b.V[vi][0]+1.6)*32)/b.N,
                                 1-((idx//4)*256+2+64+(1.4-b.V[vi][2])*32)/b.N)
                                for vi in b.F[fi]]
                        vs=[b.V[vi] for vi in b.F[fi]]
                        e=[vs[1][k]-vs[0][k] for k in range(3)];f=[vs[2][k]-vs[0][k] for k in range(3)]
                        normal=[e[1]*f[2]-e[2]*f[1],e[2]*f[0]-e[0]*f[2],e[0]*f[1]-e[1]*f[0]]
                        length=math.sqrt(sum(n*n for n in normal))
                        if length and abs(normal[1])/length<.92:
                            # Steep triangles cannot use XZ projection without
                            # shrinking a texel axis. Retain true 3D metric UV,
                            # translated into the same photographed crop.
                            du=target[0][0]-b.UV[fi][0][0];dv=target[0][1]-b.UV[fi][0][1]
                            b.UV[fi]=[(u+du,w+dv) for u,w in b.UV[fi]]
                        else:b.UV[fi]=target
        def drape(x,w,bottom,top,tile,cols=6,rows=4):
            # Sampled broad folds, curved hem and inward-pulled hanging sides.
            def point(i,j):
                u=i/cols;t=j/rows
                xx=x+(u-.5)*w*(1-.055*math.sin(t*math.pi))+.009*math.sin(t*math.pi*1.3)
                hem=bottom+.018*(1-math.cos(u*math.tau*1.5))
                zz=hem*(1-t)+(top-.035*math.sin(u*math.pi))*t
                return xx,zz
            for j in range(rows):
                for i in range(cols):
                    fabric([point(i,j),point(i+1,j),point(i+1,j+1),point(i,j+1)],tile)
        x=-.97;t=linez(x)
        # Shirt torso and distinct short sleeves, with a visible V collar gap.
        drape(x,.40,.48,t-.17,'trim',6,3)
        fabric([(x-.20,t-.17),(x-.24,t-.05),(x-.085,t),(x,t-.10)],'trim')
        fabric([(x,t-.10),(x+.085,t),(x+.24,t-.05),(x+.20,t-.17)],'trim')
        fabric([(x-.20,t-.17),(x,t-.10),(x+.20,t-.17)],'trim')
        fabric([(x-.24,t-.05),(x-.39,t-.14),(x-.36,t-.24),(x-.30,t-.30),(x-.20,t-.25)],'trim')
        fabric([(x+.20,t-.25),(x+.30,t-.30),(x+.36,t-.24),(x+.39,t-.14),(x+.24,t-.05)],'trim')
        # Small turned sleeve cuffs share the same photo field.
        fabric([(x-.36,t-.24),(x-.30,t-.30),(x-.295,t-.275),(x-.34,t-.22)],'trim',-.006)
        fabric([(x+.30,t-.30),(x+.36,t-.24),(x+.34,t-.22),(x+.295,t-.275)],'trim',-.006)
        # One long towel preserves a bottom-origin anchor at zero.
        x=-.25;t=linez(x)
        drape(x,.46,0,t,'cloth',8,6)
        # Trousers with a real open leg gap, not a painted line.
        x=.43;t=linez(x)
        fabric([(x-.20,t-.25),(x+.20,t-.25),(x+.20,t),(x-.20,t)],'teal')
        for a,c in [(x-.20,x-.025),(x+.025,x+.20)]:
            drape((a+c)/2,c-a,.20,t-.25,'teal',3,4)
        # Soft ankle/heel curves and rounded toes replace rigid right-angle feet.
        for x in [1.00,1.27]:
            t=linez(x)
            drape(x,.105,.72,t,'trim',3,3)
            outline=[(x-.052,.74),(x-.05,.69),(x-.025,.665),(x+.025,.67),
                     (x+.105,.705),(x+.13,.73),(x+.128,.75),(x+.108,.765),
                     (x+.075,.76),(x+.052,.745)]
            # Fan around a point inside the convex, softly bent foot.
            centre=(x+.035,.718)
            for a,c in zip(outline,outline[1:]+outline[:1]):
                fabric([centre,a,c],'trim')
        for x,w in [(-.97,.36),(-.25,.42),(.43,.36),(1.0,.06),(1.27,.06)]:
            for xx in [x-w/2,x+w/2]:
                b.box(xx,-.005,linez(x)-.025,.022,.045,.06,'wood_dark')
        mount=2.7
        notes=['3.2m wall-to-wall line, shirt with turned cuffs, sagging towel, split-leg trousers and rounded-heel socks; sampled curved hems and broad folds.', 'Continuous garment-space photo UV prevents repeated tile fragments; mount towel hem at 2.7m, anchors belong on host walls; no wind animation.']
    vertices=b.V[start:]
    dimensions=[round(max(q[k] for q in vertices)-min(q[k] for q in vertices),4) for k in range(3)]
    return {'dimensions':dimensions, 'design_notes':notes, 'mount_height_m':mount,
            'placement_offset_m':offset, 'pivot':'bottom_center',
            'coordinates':'Blender Z-up; front -Y', 'triangle_budget':1500}
