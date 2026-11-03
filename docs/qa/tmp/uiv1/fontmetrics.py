import struct, sys
for n in ['Galmuri11', 'Galmuri9']:
    b = open('F1/Assets/UI/Fonts/Galmuri/' + n + '.ttf', 'rb').read()
    num = struct.unpack('>H', b[4:6])[0]
    tabs = {}
    for i in range(num):
        tag, cs, off, ln = struct.unpack('>4sIII', b[12 + 16 * i:28 + 16 * i])
        tabs[tag.decode()] = off
    upm = struct.unpack('>H', b[tabs['head'] + 18:tabs['head'] + 20])[0]
    a, d, g = struct.unpack('>hhh', b[tabs['hhea'] + 4:tabs['hhea'] + 10])
    o = tabs['OS/2']
    ta, td, tg, wa, wd = struct.unpack('>hhhHH', b[o + 68:o + 78])
    print(n, 'upm', upm, 'hhea', a, d, g, 'typo', ta, td, tg, 'win', wa, wd)
