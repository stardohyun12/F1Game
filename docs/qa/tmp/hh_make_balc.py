# 0031: studio_cap.cs 사본 — 발코니 빈 객체(STUDIO_CamBalc→Look)에서 Cam_ST_in 으로 바깥 1컷 → sNN_<설명>_balcony.png
s = open('docs/qa/tmp/studio_cap.cs', encoding='utf-8').read()
for a, b in (('new[] { "road", "front", "34", "close", "in" }', 'new[] { "in" }'), ('"STUDIO_CamIn") ci', '"STUDIO_CamBalc") ci'),
             ('"STUDIO_CamInLook") cl', '"STUDIO_CamBalcLook") cl'), ('+ "_" + v + ".png"', '+ "_balcony.png"')):
    assert a in s, a; s = s.replace(a, b)
open('docs/qa/tmp/studio_cap_balc.cs', 'w', encoding='utf-8').write(s); print('ok')
