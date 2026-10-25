"""D-142 A/C body geometry, metre dimensions; Blender -Y forward, Z up.

Method: vehicle-artist proportion lock -> symmetric shell -> separate systems
-> functional seams -> restrained asymmetrical repair. No MCP or asset imports.
Caller supplies the old car generator's face/box/hull/beam/disc_x API and UVs.
"""
import math


def build(api, variant='A'):
    assert variant in ('A', 'C')
    api.groups.clear()
    box, hull, beam, face = api.box, api.hull, api.beam, api.face
    rally = variant == 'C'
    body_tile = 0

    def system(name):
        api.part = name

    def polygon(name, points, tile):
        api.detail = name
        face(points, tile)

    def lamp(name, x, y, z, radius, depth=.065):
        # Road-light sealed drum, glass recessed behind an actual metal rim.
        api.detail = name
        n = 12
        rear = [(x+math.sin(k*math.tau/n)*radius, y+depth/2,
                 z+math.cos(k*math.tau/n)*radius) for k in range(n)]
        front = [(px, y-depth/2, pz) for px, _, pz in rear]
        inset = [(x+(px-x)*.78, y-depth/2-.001, z+(pz-z)*.78)
                 for px, _, pz in front]
        for k in range(n):
            j = (k+1) % n
            face([rear[k], rear[j], front[j], front[k]], 3)
            face([front[k], front[j], inset[j], inset[k]], 3)
        face(inset, 12)
        face(rear[::-1], 14)

    # Contract first: 2.30m wheelbase, 1.58m track, old tyre radii/widths.
    system('body')
    hull('floor_pan', [(-1.48,.21,.155,.215),(-.52,.58,.155,.215),
                       (.88,.57,.155,.215),(1.40,.29,.155,.215)], 14)
    # Rounded eight-edge sections with progressively broader shoulders.
    hull('nose_shell', [(-2.03,.115,.275,.36),(-1.76,.15,.275,.425),
                        (-1.05,.21,.26,.535),(-.64,.28,.25,.61),
                        (-.36,.31,.25,.635)], body_tile)
    # Open cockpit: a floor, two coamings and a dashboard; never a filled box.
    for s in (-1, 1):
        hull('cockpit_coaming', [(-.36,.043,.50,.64),(-.25,.057,.48,.68),
                                 (.27,.057,.48,.68),(.43,.043,.48,.64)],
             body_tile, shift=s*.285)
        hull('sidepod_shell', [(-.57,.09,.235,.40),(-.36,.195,.225,.535),
                               (.21,.22,.225,.57),(.76,.20,.235,.54),
                               (1.20,.095,.255,.41)],
             1 if s < 0 else body_tile, shift=s*.425)
        box('radiator_mouth', (s*.425,-.383,.407), (.25,.018,.125), 14)
        # An assembled panel seam follows the trailing radiator edge.
        box('panel_joint', (s*.647,.32,.397), (.006,.012,.23), 14)
        # Small readable number area, not a huge sign covering the sidepod.
        polygon('race_number', [(s*.650,-.20,.29),(s*.650,.12,.29),
                                 (s*.650,.12,.49),(s*.650,-.20,.49)], 10)
    # Patch is one replacement body panel, not random painted fragments.
    hull('replacement_nose_access', [(-1.24,.19,.468,.503),
                                     (-.92,.23,.529,.564)], 1)
    polygon('mechanic_tape', [(-.595,.40,.557),(-.34,.40,.557),
                              (-.34,.445,.553),(-.595,.445,.553)], 11)
    polygon('pod_tape_return', [(-.626,.40,.40),(-.626,.445,.40),
                                 (-.626,.445,.535),(-.626,.40,.535)], 11)

    system('interior')
    box('cockpit_floor', (0,.04,.43), (.46,.66,.07), 14)
    hull('seat_cushion', [(-.13,.17,.46,.50),(.27,.17,.46,.50)], 9)
    hull('seat_back', [(.24,.18,.48,.77),(.33,.17,.48,.77)], 9)
    box('dashboard', (0,-.30,.575), (.45,.09,.15), 14)
    # Steering rim tilted 22 degrees, visible from chase camera.
    for k in range(12):
        def steering(j):
            a=j*math.tau/12
            return (.135*math.cos(a),-.205-.05*math.sin(a),.635+.105*math.sin(a))
        beam('steering_rim',steering(k),steering(k+1),.021,9)
    beam('steering_spoke',(-.12,-.205,.635),(.12,-.205,.635),.018,3)

    system('engine')
    hull('engine_cover',[(.40,.24,.28,.63),(.73,.23,.28,.59),
                         (1.12,.20,.28,.49),(1.36,.12,.28,.40)],body_tile)
    for s in (-1,1):
        beam('exhaust',(s*.18,1.20,.30),(s*.18,1.59,.30),.052,3)

    system('aero')
    # Curved planform assembled wing rather than an oversized flat rectangle.
    for s in (-1,1):
        hull('front_wing_half',[(-1.99,.34,.168,.202),(-1.79,.43,.168,.22),
                                (-1.64,.35,.175,.235)],body_tile,shift=s*.43)
        box('front_endplate',(s*.865,-1.80,.23),(.025,.37,.16),1)
        beam('rear_wing_pylon',(s*.32,1.24,.34),(s*.32,1.51,.68),.035,3)
        box('rear_endplate',(s*.70,1.57,.69),(.025,.36,.17),1)
    box('rear_wing',(0,1.57,.73),(1.42,.37,.045),body_tile)
    box('rear_wing_lip',(0,1.746,.755),(1.37,.018,.02),11)

    system('suspension')
    for s in (-1,1):
        for y,z in [(-1.2,.35),(1.1,.375)]:
            for hz in (.265,.42):
                for dy in (-.23,.23):
                    beam('wishbone',(s*.25,y+dy,hz),(s*.79,y,z),.024,3)
        beam('shock_absorber',(s*.30,-1.0,.46),(s*.62,-1.18,.31),.043,14)

    system('road_conversion')
    for s in (-1,1):
        beam('mirror_stalk',(s*.31,-.30,.625),(s*.47,-.39,.72),.023,3)
        hull('mirror_case',[(-.44,.065,.69,.755),(-.36,.065,.69,.755)],body_tile,shift=s*.49)
        box('mirror_face',(s*.49,-.355,.722),(.10,.006,.045),13)
        if not rally:lamp('road_headlamp',s*.35,-1.39,.47,.072)
        box('tail_lamp',(s*.30,1.535,.43),(.10,.024,.055),15)
    box('plate_bracket',(0,1.55,.36),(.26,.035,.115),3)
    box('registration_plate',(0,1.572,.36),(.235,.008,.09),3)

    system('cargo_frame')
    # Empty load space; no box occupying gameplay cargo attachment volume.
    if not rally:
        for s in (-1,1):
            for y in (.68,1.23):
                beam('welded_rack_leg',(s*.28,y,.43),(s*.34,y,.70),.05,3)
            beam('tray_under_channel',(s*.34,.63,.678),(s*.34,1.27,.678),.04,3)
            box('cargo_tray_side',(s*.3575,.95,.735),(.025,.68,.055),3)
        box('cargo_tray',(0,.95,.70),(.74,.68,.025),3)
        box('cargo_tray_rear_stop',(0,1.2775,.735),(.69,.025,.055),3)

    system('rollbar')
    # Rounded corner hoop, below the measured old helmet top.
    hoop_top=.9085 if rally else 1.0555
    hoop_shoulder=.83 if rally else .92
    hoop=[(-.24,.39,.60),(-.24,.39,hoop_shoulder),(-.17,.39,hoop_top),
          (.17,.39,hoop_top),(.24,.39,hoop_shoulder),(.24,.39,.60)]
    for a,b in zip(hoop,hoop[1:]):beam('roll_hoop',a,b,.033,3)
    for s in (-1,1):beam('hoop_rear_brace',(s*.20,.39,.89 if rally else .96),(s*.32,.88,.60),.026,3)

    if rally:
        system('rally_conversion')
        # Individually mounted lamps leave the narrow F1 nose unobstructed.
        for x in (-.20,.20):
            beam('spot_mount_arm',(x*.7,-1.10,.52),(x,-1.13,.58),.025,3)
            lamp('rally_spot',x,-1.15,.62,.07,.065)
        for s in (-1,1):
            for y,z,r in [(-1.2,.35,.35),(1.1,.375,.375)]:
                box('rubber_mudflap',(s*.79,y+r+.015,.245),(.28,.015,.17),9)
                beam('mudflap_support',(s*.43,y+.19,.32),(s*.79,y+r+.015,.32),.021,3)
        # A single shallow tray on the roll hoop replaces stacked wire cages.
        for s in (-1,1):
            box('upper_tray_side',(s*.2775,.60,.944),(.025,.34,.040),3)
            beam('upper_rack_brace',(s*.27,.75,.925),(s*.32,.89,.64),.040,3)
            beam('hoop_tray_bracket',(s*.17,.39,.9085),(s*.27,.46,.925),.030,3)
        box('upper_cargo_tray',(0,.60,.925),(.58,.34,.025),3)
        box('upper_tray_rear_stop',(0,.7575,.944),(.53,.025,.040),3)
        # A tied, folded weather cover makes the rack's purpose immediately visible.
        hull('folded_canvas_cover',[(.46,.15,.95,1.045),(.50,.21,.95,1.064),
                                    (.70,.21,.95,1.064),(.74,.15,.95,1.045)],6)
        for x in (-.13,.13):
            box('canvas_tie_top',(x,.60,1.068),(.026,.22,.008),9)
            for y in (.482,.718):box('canvas_tie_end',(x,y,1.005),(.026,.010,.095),9)
        # Single restrained centre stripe instead of twin racing ribbons.
        polygon('rally_nose_stripe',[(-.027,-1.74,.431),(.027,-1.74,.431),
                                     (.027,-1.28,.501),(-.027,-1.28,.501)],6)
        for s in (-1,1):
            box('rally_side_band',(s*.649,.47,.43),(.006,.24,.065),8)

    centers={}
    for axle,y in [('front',-1.2),('rear',1.1)]:
        for side,s in [('left',1),('right',-1)]:
            name=f'wheel_{axle}_{side}'
            system(name)
            cx=s*.79
            r=.35 if axle=='front' else .375
            w=.30 if axle=='front' else .378909035
            z=r
            centers[name]=(cx,y,z)
            api.detail='rubber'
            rings=[]
            n=20
            for dx,rad in [(-w/2,r*.88),(-w/2+.032,r),(w/2-.032,r),(w/2,r*.88)]:
                rings.append([(cx+dx,y+math.sin(k*math.tau/n)*rad,
                               z+math.cos(k*math.tau/n)*rad) for k in range(n)])
            for j,(a,b) in enumerate(zip(rings,rings[1:])):
                for k in range(n):face([a[k],a[(k+1)%n],b[(k+1)%n],b[k]],5 if j==1 else 4)
            # Open sidewalls: a full circular tyre cap would occlude the rim.
            for end in (0,-1):
                outer=rings[end]
                xx=outer[0][0]
                hole=[(xx,y+math.sin(k*math.tau/n)*r*.55,
                       z+math.cos(k*math.tau/n)*r*.55) for k in range(n)]
                for k in range(n):
                    j=(k+1)%n
                    pts=[outer[k],outer[j],hole[j],hole[k]]
                    face(pts[::-1] if end==0 else pts,4)
            for ss in (-1,1):
                # Metal stays inside tyre outer width: exact existing bounds.
                xx=cx+ss*(w/2-.0003)
                api.detail='rim'
                outer=[];inner=[]
                for k in range(12):
                    a=k*math.tau/12
                    outer.append((xx,y+math.sin(a)*r*.54,z+math.cos(a)*r*.54))
                    inner.append((xx,y+math.sin(a)*r*.39,z+math.cos(a)*r*.39))
                for k in range(12):face([outer[k],outer[(k+1)%12],inner[(k+1)%12],inner[k]],2)
                api.disc_x('rim_inset',xx+ss*.0001,y,z,r*.39,12,14)
                api.disc_x('hub',xx+ss*.0002,y,z,.057,8,2)
    api.wheel_centers=centers
    return centers
