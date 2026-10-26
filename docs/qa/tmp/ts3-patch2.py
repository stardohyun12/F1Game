p='F1/Assets/Editor/BuildToneSlice.cs'
s=open(p,encoding='utf-8').read()
s=s.replace('g.localScale = Vector3.one * 1.15f;   // 2144: 1.7→1.95m·굵게 — 30~60m 에서 1× 실루엣이 읽히게',
            'g.localScale = Vector3.one * 1.25f;   // 2144→2203: 1.7→2.1m·굵게 — 30~40m 에서 1× 실루엣이 읽히게\n        Box(g, "BlobShadow", new Vector3(0, .02f, 0), new Vector3(.95f, .02f, .7f), blobMat);   // 2203: 발밑 블롭 그림자(실시간 그림자 끔, 규칙 7)')
s=s.replace('sh.localScale = Vector3.one * 1.15f;','sh.localScale = Vector3.one * 1.25f;')
s=s.replace('    static Transform Person(','    static Material blobMat;\n    static Transform Person(')
a='            int i = 1075; var f = Fwd(i); var s = Sea(i);'
assert a in s
s=s.replace(a,'            int i = 1095; var f = Fwd(i); var s = Sea(i);   // 2203: 1075 → 1095(1060 시점 35m 앞, 사람이 서는 쉼 자리)')
s=s.replace('// ── 2) 비어 있는 쉼 자리: 1075','// ── 2) 쉼 자리: 1095')
a=s.index('        // ── 4) 멀리 사람 3'); b=s.index('        // ── 5) 창 불빛')
new='''        // ── 4) 사람 3(2203): 30~40m 길가 쉼 자리 2명(동작 + 화면에서 가장 또렷한 작은 색 하나씩) + 진입 때 길가를 오가는 1명
        blobMat = Mat("BlobShadow", new Color(.20f, .19f, .24f));
        var red = Mat("CoatBrightRed", new Color(.88f, .14f, .10f)); var yellow = Mat("CoatBrightYellow", new Color(.98f, .80f, .14f)); var denim = Mat("CoatDenim", new Color(.30f, .42f, .62f));
        var swayArms = new GameObject("WavingArms").transform; swayArms.SetParent(root, false);
        var laundryArms = new GameObject("LaundryArms").transform; laundryArms.SetParent(root, false);
        var laundrySway = new GameObject("LaundryCloths").transform; laundrySway.SetParent(root, false);
        {
            // 4a) 쉼 자리 벤치 옆에 선 사람(빨강) — 길 쪽을 보고 손 흔듦
            var rest = root.Find("RestSpot_Empty");
            var wp = rest.TransformPoint(new Vector3(-2.4f, 0, .2f)); wp.y = Gnd(wp, root);
            Person(root, "Person_Waving", wp, c[1088] - wp, red, trDark, skin, swayArms);
            var ws = swayArms.gameObject.AddComponent<ToneSway>(); ws.amplitude = 28f; ws.frequency = .9f; ws.waveLength = 1f; ws.axis = Vector3.forward;
            log.Append($"waver@{wp:F0} d={(wp - c[1060]).magnitude:F0}m ");

            // 4b) 빨래 줄 + 너는 사람(노랑): 1100 육지 쪽 길가(길 끝에서 5m)
            Vector3 lp = c[1100] - Sea(1100) * (hw + 5f); lp.y = Gnd(lp, root);
            var line = new GameObject("LaundryLine").transform; line.SetParent(root, false); line.position = lp; line.rotation = Quaternion.LookRotation(Fwd(1100));
            Box(line, "PoleA", new Vector3(0, 1.0f, -2.4f), new Vector3(.1f, 2.0f, .1f), wood);
            Box(line, "PoleB", new Vector3(0, 1.0f, 2.4f), new Vector3(.1f, 2.0f, .1f), wood);
            Box(line, "Rope", new Vector3(0, 1.92f, 0), new Vector3(.06f, .06f, 4.8f), stone);   // 6cm: 더 가늘면 저해상도에서 끊겨 깜빡임
            var cloths = new[] { clothW, clothB, clothW, clothW, clothB };   // 빨강·노랑은 사람 옷에만(가장 또렷한 작은 색)
            for (int k = 0; k < 5; k++)
            {
                var piv = new GameObject("Cloth" + k).transform; piv.SetParent(laundrySway, false);
                piv.position = line.TransformPoint(new Vector3(0, 1.9f, -1.8f + k * .9f)); piv.rotation = line.rotation;
                float h = k == 2 ? .9f : .65f;
                Box(piv, "Sheet", new Vector3(0, -h * .5f, 0), new Vector3(.04f, h, .7f), cloths[k]);
            }
            var ls = laundrySway.gameObject.AddComponent<ToneSway>(); ls.amplitude = 10f; ls.frequency = .5f; ls.waveLength = 6f; ls.axis = Vector3.forward;
            var hp = line.TransformPoint(new Vector3(-.7f, 0, 2.9f)); hp.y = Gnd(hp, root);
            Person(root, "Person_HangingLaundry", hp, line.position - hp + line.right * -.5f, yellow, trDark, skin, laundryArms);
            var la = laundryArms.gameObject.AddComponent<ToneSway>(); la.amplitude = 18f; la.frequency = .55f; la.waveLength = 1f; la.axis = Vector3.forward;
            log.Append($"laundry@{lp:F0} d={(lp - c[1060]).magnitude:F0}m ");

            // 4c) 진입 때 길가(육지 쪽 길 끝 3.5m)를 오가는 사람 — 1060 출발 시점에서 15~20m 앞
            var walkP = c[1076] - Sea(1076) * (hw + 3.5f); walkP.y = Gnd(walkP, root);
            var walker = Person(root, "Person_Walker", walkP, Fwd(1076), denim, trDark, skin);
            var tw = walker.gameObject.AddComponent<ToneWalker>(); tw.dir = Fwd(1076); tw.span = 6f; tw.speed = .9f;
            log.Append($"walker@{walkP:F0} d={(walkP - c[1060]).magnitude:F0}m ");
        }

        // ── 7) 길 가장자리 세월(TONE 6, 2203): 낮은 돌 연석 줄(두 가지 회색, 이 빠짐) + 바깥 가장자리로 넘어오는 마른 풀
        {
            var kerbA = Mat("KerbStoneA", new Color(.64f, .62f, .58f)); var kerbB = Mat("KerbStoneB", new Color(.55f, .53f, .50f));
            var kerb = new GameObject("KerbStones").transform; kerb.SetParent(root, false);
            var edge = new GameObject("EdgeGrass").transform; edge.SetParent(root, false);
            var er = new System.Random(2203); int nK = 0, nE = 0;
            for (int i = I0; i <= I1; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    if (er.NextDouble() < .06) continue;   // 이 빠진 연석
                    var kp = c[i] + Right(i) * side * (hw + .14f); kp.y = Gnd(kp, root) + .05f;
                    if (Mathf.Abs(kp.y - c[i].y) > 1.5f) continue;
                    var kb = Box(kerb, "Kerb", Vector3.zero, new Vector3(.24f, .14f, .9f), er.NextDouble() < .5 ? kerbA : kerbB);
                    kb.transform.position = kp; kb.transform.rotation = Quaternion.LookRotation(Fwd(i)) * Quaternion.Euler(0, (float)(er.NextDouble() * 4 - 2), 0); nK++;
                    if (er.NextDouble() < .45)
                    {
                        var gp = c[i] + Right(i) * side * (hw + 1.4f + (float)er.NextDouble() * 1.4f) + Fwd(i) * (float)(er.NextDouble() - .5); gp.y = Gnd(gp, root) - .03f;
                        if (Mathf.Abs(gp.y - c[i].y) > 1.5f) continue;
                        var g = new GameObject("EdgeClump"); g.transform.SetParent(edge, false);
                        g.transform.position = gp; g.transform.rotation = Quaternion.Euler(0, (float)er.NextDouble() * 360f, 0);
                        float sc = .45f + (float)er.NextDouble() * .3f; g.transform.localScale = new Vector3(sc, sc * .8f, sc);
                        g.AddComponent<MeshFilter>().sharedMesh = clumps[er.Next(3)];
                        g.AddComponent<MeshRenderer>().sharedMaterial = er.NextDouble() < .65 ? straw : olive; nE++;
                    }
                }
            var eg = edge.gameObject.AddComponent<ToneSway>(); eg.amplitude = 5f; eg.frequency = .45f; eg.waveLength = 18f; eg.axis = Vector3.forward;
            log.Append($"kerb={nK} edgeGrass={nE} ");
        }

        // ── 8) 가로등 불(2203): 구간 근처 가로등 머리 아래에 작은 등 상자 — ToneWindowGlow(노을·초저녁에 켜짐)
        {
            var lampOn = Mat("LampGlass", new Color(.40f, .40f, .38f)); int nL = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name != "StreetLamp" || t.parent == null || t.parent.name != "Poles") continue;
                bool near = false; for (int i = I0 - 20; i <= Mathf.Min(I1 + 20, c.Length - 1); i += 10) if ((t.position - c[i]).sqrMagnitude < 35f * 35f) { near = true; break; }
                if (!near) continue;
                var mf = t.GetComponentInChildren<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                var vs = mf.sharedMesh.vertices; var tr = mf.transform; float top = -1e9f;
                foreach (var v in vs) top = Mathf.Max(top, tr.TransformPoint(v).y);
                Vector3 head = t.position; float far = -1f;
                foreach (var v in vs) { var w = tr.TransformPoint(v); if (w.y < top - .5f) continue; var dxz = new Vector2(w.x - t.position.x, w.z - t.position.z).magnitude; if (dxz > far) { far = dxz; head = w; } }
                var g = Box(root, "LampGlow_" + nL, Vector3.zero, new Vector3(.34f, .12f, .34f), lampOn);
                g.transform.position = new Vector3(head.x, top - .22f, head.z);
                var tg = g.AddComponent<ToneWindowGlow>(); tg.off = new Color(.42f, .42f, .40f); tg.on = new Color(1.9f, 1.4f, .75f); nL++;
            }
            log.Append($"lamps={nL} ");
        }

'''
s=s[:a]+new+s[b:]
open(p,'w',encoding='utf-8').write(s)
print('ok')
