p='F1/Assets/Editor/BuildBayLoop.Lived.cs'
s=open(p,encoding='utf-8').read()
def rep(a,b):
    global s
    assert s.count(a)==1,(s.count(a),a[:60])
    s=s.replace(a,b)
rep('int slotTry = 0, wear = 0,','int scoot = 0, garage = 0, display = 0, cafe = 0, mailb = 0, slotTry = 0, wear = 0,')
rep('if (u < 0.40) { id = "Patch";','if (u < 0.40) { id = rnd.Next(2) == 0 ? "road_patch_01" : "road_patch_02";')
rep('float sc = id == "Patch" ? 0.8f + (float)rnd.NextDouble() * 1.6f : 1f;','bool patch = id.StartsWith("road_patch");\n                    float sc = patch ? 0.7f + (float)rnd.NextDouble() * 0.6f : 1f;')
rep('(id == "Patch" || id == "OilStain" ?','(patch || id == "OilStain" ?')
rep('if (id == "Patch" && dz >= 1','if (patch && dz >= 1')
old_sign=s[s.index('                    var g = new GameObject("SIGN_Stop")'):s.index('                    signs++;')]
rep(old_sign,'                    if (Prop(gL, "street_sign_0" + (1 + rnd.Next(3)), sp, Quaternion.LookRotation(inward)) != null)\n')
old_m=s[s.index('                        var g = new GameObject("MIRROR")'):s.index('                        mirrors++;')]
rep(old_m,'                        if (Prop(gL, "traffic_mirror_01", mp, Quaternion.LookRotation(inward)) != null)\n')
rep('''                    var cp = gp.edge - gp.dir * (0.4f + 2.0f); cp.y = H(cp.x, cp.z);
                    if (ParkedCar(gL, cp, Quaternion.LookRotation(gp.dir * (rnd.Next(2) == 0 ? 1 : -1)), rnd)) parked++;''',
'''                    // 차는 아트 보류(D-141 v5b) — 자리는 비우고 스쿠터만
                    var cp = gp.edge - gp.dir * (0.6f + 1.2f) + lat * ((float)rnd.NextDouble() - .5f) * (gp.w - 1.2f); cp.y = H(cp.x, cp.z);
                    if (Scooter(gL, cp, Quaternion.LookRotation(gp.dir * (rnd.Next(2) == 0 ? 1 : -1)), rnd)) scoot++;''')
rep('''            var cp = b.edge - b.dir * (b.space * .5f); cp.y = H(cp.x, cp.z); slotTry++;
            if (ParkedCar(gL, cp, Quaternion.LookRotation(new Vector3(b.dir.z, 0, -b.dir.x) * (rnd.Next(2) == 0 ? 1 : -1)), rnd)) parked++;''',
'''            slotTry++;
            var blat = new Vector3(b.dir.z, 0, -b.dir.x); float gs = rnd.Next(2) == 0 ? -1f : 1f;
            // 차고 입구: 집 앞벽에 붙여(보통 구역 집), 앞 공간은 비워 둔다(차 자리 — 아트 보류)
            if (b.w >= 5.5f && rnd.NextDouble() < 0.6)
            {
                var gpz = b.edge - b.dir * b.space + blat * gs * (b.w * .5f - 1.7f); gpz.y = H(gpz.x, gpz.z);
                var gd = Prop(gL, "garage_door_inset_01", gpz, Quaternion.LookRotation(b.dir));
                if (gd != null) { foreach (var c in gd.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c); garage++; }
            }
            // 스쿠터: 차고 반대쪽 앞마당
            var sp2 = b.edge - b.dir * (b.space * .5f) - blat * gs * Mathf.Min(1.4f, b.w * .25f); sp2.y = H(sp2.x, sp2.z);
            if (rnd.NextDouble() < 0.7 && Scooter(gL, sp2, Quaternion.LookRotation(b.dir * (rnd.Next(2) == 0 ? 1 : -1)), rnd)) scoot++;
            // 우편함: 인도 바깥선 대문 자리
            if (rnd.NextDouble() < 0.4)
            {
                var mp2 = b.edge - b.dir * 0.35f - blat * gs * 0.9f; mp2.y = H(mp2.x, mp2.z);
                if (!NearCar(mp2, 0.2f) && Prop(gL, "mailbox_cluster_01", mp2, Quaternion.LookRotation(b.dir)) != null) mailb++;
            }''')
rep('''            foreach (float sd in new[] { -0.6f, 0.6f })
            {
                var pp = sh.face + sh.dir * 0.55f + lat * sd; pp.y = H(pp.x, pp.z) + 0.02f;
                if (NearCar(pp, 0.4f)) continue;
                Prop(gL, rnd.NextDouble() < 0.5 ? "CrateStack" : "Crate", pp, Quaternion.LookRotation(sh.dir));
            }''',
'''            // 가게 앞 진열대(한쪽) + 상자(다른 쪽)
            float ds = rnd.Next(2) == 0 ? -1f : 1f;
            var dp = sh.face + sh.dir * 0.5f + lat * ds * 0.9f; dp.y = H(dp.x, dp.z) + 0.02f;
            if (!NearCar(dp, 0.5f) && Prop(gL, "shop_display_01", dp, Quaternion.LookRotation(sh.dir)) != null) display++;
            var pp = sh.face + sh.dir * 0.55f - lat * ds * 1.0f; pp.y = H(pp.x, pp.z) + 0.02f;
            if (!NearCar(pp, 0.4f)) Prop(gL, rnd.NextDouble() < 0.5 ? "CrateStack" : "Crate", pp, Quaternion.LookRotation(sh.dir));
            // 광장·상점가(밀집): 카페 세트 + 파라솔 — 차양보다 바깥, 귀가 차길에 안 닿을 때만
            if (DensAt(sh.face.x, sh.face.z, 10f) == 2 && rnd.NextDouble() < 0.35)
            {
                var cc = sh.face + sh.dir * 1.9f; cc.y = H(cc.x, cc.z) + 0.02f;
                bool ok = true; foreach (var q in new[] { cc + lat * 1.1f + sh.dir * 0.6f, cc - lat * 1.1f + sh.dir * 0.6f, cc + sh.dir * 1.2f }) if (NearCar(q, 0.2f)) ok = false;
                if (ok && Prop(gL, "cafe_set_01", cc, Quaternion.LookRotation(lat)) != null)
                {
                    cafe++;
                    var pa = Prop(gL, "parasol_01", cc, Quaternion.Euler(0, rnd.Next(360), 0));
                    if (pa != null) foreach (var c in pa.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                }
            }
            // 가게 앞 스쿠터(배달·손님) 25%
            if (rnd.NextDouble() < 0.25)
            {
                var scp = sh.face + sh.dir * 1.3f + lat * ds * (Mathf.Min(sh.w * .5f, 3f)); scp.y = H(scp.x, scp.z);
                if (Scooter(gL, scp, Quaternion.LookRotation(lat * (rnd.Next(2) == 0 ? 1 : -1)), rnd)) scoot++;
            }''')
rep('주차 {parked}(앞마당 시도 {slotTry}/{frontSlots.Count}·틈 {gaps.Count})','스쿠터 {scoot}·차고 {garage}·우편함 {mailb}(앞마당 시도 {slotTry}/{frontSlots.Count}·틈 {gaps.Count}, 차 보류), 진열 {display}, 카페 {cafe}')
rep('struct Slot { public Vector3 edge, dir; public float space; }','struct Slot { public Vector3 edge, dir; public float space, w; }')
rep('''        float lift = r.kind == "main" ? 0f : -0.03f; var rot = Quaternion.LookRotation(Flat(f));
        for (float x''','''        float lift = r.kind == "main" ? 0f : -0.03f; var rot = Quaternion.LookRotation(Flat(f));
        var cpos = c; cpos.y = c.y + lift + 0.010f;
        var cw = Decal(parent, "crosswalk_paint_01", cpos, rot.eulerAngles.y, 1f);
        if (cw != null) { cw.transform.localScale = new Vector3(lh * 2f / 4.78f, 1f, 1f); return; }
        for (float x''')
a=s.index('    static readonly string[] CarModels'); b=s.index('    /// <summary>땅을 따라 가는 낮은 담')
s=s[:a]+'''    /// <summary>스쿠터(아트 2차). 길이 2m·폭 0.8m 덩치가 차길·건물에 닿으면 생략. 차(kenney 폴백)는 쓰지 않는다 — 아트 보류.</summary>
    static bool Scooter(Transform parent, Vector3 pos, Quaternion rot, System.Random rnd)
    {
        var fwd = rot * Vector3.forward; var lat = rot * Vector3.right;
        foreach (var q in new[] { pos + fwd * 1.0f, pos - fwd * 1.0f, pos + lat * 0.4f, pos - lat * 0.4f })
            if (NearCar(q, 0.25f) || NearBuilding(q, 0.05f)) return false;
        return Prop(parent, rnd.Next(2) == 0 ? "scooter_01" : "scooter_02", pos + Vector3.up * 0.01f, rot) != null;
    }

'''+s[b:]
open(p,'w',encoding='utf-8').write(s)
q='F1/Assets/Editor/BuildBayLoop.cs'
t=open(q,encoding='utf-8').read()
a='frontSlots.Add(new Slot { edge = f3 + dir * fsp, dir = dir, space = fsp });'
assert t.count(a)==1
t=t.replace(a,'frontSlots.Add(new Slot { edge = f3 + dir * fsp, dir = dir, space = fsp, w = w });')
open(q,'w',encoding='utf-8').write(t)
print('ok')
