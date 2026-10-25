p = 'F1/Assets/Editor/BuildBayLoop.cs'
s = open(p, encoding='utf-8').read()

old_garage = s[s.index('        // 정비소 PitCrew: MART 동쪽 부두'):s.index('        // MART 앞 포장 자리')]
new_garage = '''        // 정비소 PitCrew: MART 동쪽 부두, 예전 위판장(+8) 자리. 셔터가 길(+z) 쪽을 본다.
        // 과제 0032: 속이 빈 창고(벽·지붕·바닥 콜라이더) — 시네마틱 장면 2 에서 차가 안에 서 있고 셔터가 말려 올라간다.
        {
            var c = new Vector3(mart.x + 8f, 0, mart.y - 12f); float y = H(c.x, c.z + 3f);
            var g = new GameObject("TUT_Garage_PitCrew").transform; g.SetParent(parent); g.position = new Vector3(c.x, y, c.z);
            float fz = c.z + 3.25f;
            Box(g, "HallFloor", new Vector3(10f, 0.3f, 6.4f), new Vector3(c.x, y - 0.15f, c.z), Quaternion.identity, M("CurbDark"), true);
            Box(g, "HallBack", new Vector3(10f, 5.2f, 0.3f), new Vector3(c.x, y + 2.6f, c.z - 3.05f), Quaternion.identity, M("TinRoof"), true);
            Box(g, "HallL", new Vector3(0.3f, 5.2f, 6.4f), new Vector3(c.x - 4.85f, y + 2.6f, c.z), Quaternion.identity, M("TinRoof"), true);
            Box(g, "HallR", new Vector3(0.3f, 5.2f, 6.4f), new Vector3(c.x + 4.85f, y + 2.6f, c.z), Quaternion.identity, M("TinRoof"), true);
            Box(g, "HallRoof", new Vector3(10f, 0.3f, 6.4f), new Vector3(c.x, y + 5.05f, c.z), Quaternion.identity, M("TinRoof"), true);
            Box(g, "FrontL", new Vector3(2.3f, 5.2f, 0.3f), new Vector3(c.x - 3.85f, y + 2.6f, fz - 0.1f), Quaternion.identity, M("TinRoof"), true);
            Box(g, "FrontR", new Vector3(2.3f, 5.2f, 0.3f), new Vector3(c.x + 3.85f, y + 2.6f, fz - 0.1f), Quaternion.identity, M("TinRoof"), true);
            Box(g, "Lintel", new Vector3(5.4f, 1.7f, 0.3f), new Vector3(c.x, y + 4.35f, fz - 0.1f), Quaternion.identity, M("TinRoof"), false);
            Box(g, "RoofCap", new Vector3(10.6f, 0.35f, 7f), new Vector3(c.x, y + 5.3f, c.z), Quaternion.identity, M("Dark"), false);
            Box(g, "Workbench", new Vector3(2.4f, 0.9f, 0.7f), new Vector3(c.x - 3.3f, y + 0.45f, c.z - 2.4f), Quaternion.identity, M("Wood"), false);
            Box(g, "TireStack", new Vector3(0.7f, 0.9f, 0.7f), new Vector3(c.x + 3.9f, y + 0.45f, c.z - 2.2f), Quaternion.identity, M("Dark"), false);
            // 셔터: 위(문틀 꼭대기)에 축을 둔 묶음 — OpeningTutorial 이 localScale.y 를 1→0.04 로 줄여 말아 올린다.
            var roll = new GameObject("ShutterRoll").transform; roll.SetParent(g); roll.position = new Vector3(c.x, y + 3.5f, fz);
            Box(roll, "Shutter", new Vector3(5.4f, 3.5f, 0.12f), new Vector3(c.x, y + 1.75f, fz), Quaternion.identity, M("CurbDark"), false);
            for (int i = 0; i < 7; i++) Box(roll, "ShutterSlat" + i, new Vector3(5.4f, 0.06f, 0.16f), new Vector3(c.x, y + 0.3f + i * 0.5f, fz + 0.02f), Quaternion.identity, M("Stone"), false);
            ArtSlot(roll, "ArtSlot_GarageShutter", new Vector3(c.x, y + 1.75f, fz + 0.1f), Quaternion.LookRotation(Vector3.back));
            Box(g, "SignBoard", new Vector3(6.4f, 1.1f, 0.2f), new Vector3(c.x, y + 4.4f, fz + 0.12f), Quaternion.identity, M("Red"), false);
            Label(g, "PitCrew", new Vector3(c.x, y + 4.4f, fz + 0.25f), Quaternion.LookRotation(Vector3.back), 0.75f, Color.white);
            ArtSlot(g, "ArtSlot_GarageSign", new Vector3(c.x, y + 4.4f, fz + 0.3f), Quaternion.LookRotation(Vector3.back));
            Mark(g, "TUT_GarageCarSpot", new Vector3(c.x, y + 0.5f, c.z - 0.2f), Quaternion.identity);
            // 정비소 사람(셔터 왼쪽) — "또 그걸로 배달 가?"
            var mech = Person(g, "TUT_Mechanic", new Vector3(c.x - 3.4f, y, fz + 1.2f), Quaternion.LookRotation(new Vector3(0.6f, 0, 1f)), M("Fish"));
            Audio(mech, "Audio_Tools", new Vector3(c.x - 3.3f, y + 1f, c.z - 2.4f));
            Mark(g, "Shot_2a", new Vector3(c.x + 4.5f, y + 1.7f, fz + 11f), Quaternion.LookRotation(new Vector3(c.x, y + 1.6f, fz) - new Vector3(c.x + 4.5f, y + 1.7f, fz + 11f)));
            Mark(g, "Shot_2b", new Vector3(c.x + 1.6f, y + 1.35f, fz + 4.6f), Quaternion.LookRotation(new Vector3(c.x - 0.6f, y + 0.8f, c.z) - new Vector3(c.x + 1.6f, y + 1.35f, fz + 4.6f)));
            log.Add($"정비소({c.x:F0},{y:F1},{c.z:F0}) 속빈창고·셔터축");
        }
'''
s = s.replace(old_garage, new_garage, 1)

# 포장 자리 블록 끝에 MART 주인·포장 트리거·장면 3 카메라
old_pack_end = '''            log.Add($"포장자리({x:F0},{y:F1},{z:F0})");
        }'''
new_pack_end = '''            log.Add($"포장자리({x:F0},{y:F1},{z:F0})");
            // 과제 0032: MART 주인(탁자 옆, 길 쪽을 봄) + 머리 위 마름모(D-138) + 가게 종 소리 자리
            var op = new Vector3(x + 1.5f, y, z - 0.5f);
            var owner = Person(parent, "TUT_MartOwner", op, Quaternion.LookRotation(Vector3.back), M("Red"));
            Box(owner, "NPC_Marker", new Vector3(0.4f, 0.4f, 0.4f), op + Vector3.up * 2.3f, Quaternion.Euler(45f, 0f, 45f), M("Orange"), false);
            Audio(owner, "Audio_ShopBell", op + Vector3.up * 2.2f);
            var s3a = op + new Vector3(-2.2f, 1.7f, -6.5f); Mark(parent, "Shot_3a", s3a, Quaternion.LookRotation(op + Vector3.up * 1.4f - s3a));
            var s3b = op + new Vector3(-0.9f, 1.5f, -3.2f); Mark(parent, "Shot_3b", s3b, Quaternion.LookRotation(op + Vector3.up * 1.3f - s3b));
            // 포장 시작 지점: 본선 #0(MART 바로 앞 길). 차가 4.5m 안에 들어오면 포장 화면.
            Mark(parent, "TUT_PackTrigger", main.pts[0] + Vector3.up * 0.3f, Quaternion.LookRotation(Flat(main.pts[2] - main.pts[0])));
        }'''
assert old_pack_end in s
s = s.replace(old_pack_end, new_pack_end, 1)

# 찻집 블록: 배달 지점(DeliveryPoint) 추가
old_tea_end = '''            log.Add($"찻집NPC({x:F0},{y:F1},{z:F0})");'''
new_tea_end = '''            // 과제 0032: 배달 지점 — 라벨 TeaHouse, 반경 5m. OpeningTutorial 이 DeliveryManager 에 넣는다(다른 판엔 안 쓴다).
            var dp = new GameObject("TUT_TeaDelivery"); dp.transform.SetParent(g); dp.transform.position = new Vector3(F(p[0]), H(F(p[0]), z) + 0.6f, z);
            dp.AddComponent<SphereCollider>(); var dpc = dp.AddComponent<DeliveryPoint>();
            var dso = new SerializedObject(dpc); dso.FindProperty("label").stringValue = "TeaHouse"; dso.FindProperty("radius").floatValue = 5f; dso.ApplyModifiedPropertiesWithoutUndo();
            log.Add($"찻집NPC({x:F0},{y:F1},{z:F0})");'''
assert old_tea_end in s
s = s.replace(old_tea_end, new_tea_end, 1)

# 해안 직선: 시작 관문
old_sign = '''            ArtSlot(g, "ArtSlot_StraightEndSign", sp + Vector3.up * 2.4f - f * 0.08f, rot);'''
new_sign = '''            ArtSlot(g, "ArtSlot_StraightEndSign", sp + Vector3.up * 2.4f - f * 0.08f, rot);
            Mark(parent, "TUT_CoastGateStart", P[best0], Quaternion.LookRotation(Flat(P[best0 + 2] - P[best0])));
            Mark(parent, "TUT_CoastGateEnd", P[e], rot);'''
assert old_sign in s
s = s.replace(old_sign, new_sign, 1)

# 끝: 방·차 시작·복귀·장면 4·연출 감독
old_ret = '''        return "튜토리얼 " + string.Join(" ", log);
    }'''
new_ret = '''        // 과제 0032 장면 1: 항구 위 비탈집 방(블록아웃 상자). 창이 만(남서쪽)을 본다. 포스터·신청서·저금통·라디오는 ArtSlot.
        {
            var r0 = new Vector3(-40f, 0, -95f); r0.y = H(r0.x, r0.z) + 7f;
            var rot = Quaternion.LookRotation(new Vector3(-60f, 0, -55f));
            var g = new GameObject("TUT_Room").transform; g.SetParent(parent); g.SetPositionAndRotation(r0, rot);
            System.Action<string, Vector3, Vector3, string> B = (n, size, lp, mat) => Box(g, n, size, g.TransformPoint(lp), rot, M(mat), false);
            B("Floor", new Vector3(4.3f, 0.15f, 4.7f), new Vector3(0, -0.075f, 0), "Wood");
            B("Ceiling", new Vector3(4.3f, 0.15f, 4.7f), new Vector3(0, 2.875f, 0), "MartWall");
            B("WallBack", new Vector3(4.3f, 2.8f, 0.15f), new Vector3(0, 1.4f, -2.275f), "Sand");
            B("WallL", new Vector3(0.15f, 2.8f, 4.7f), new Vector3(-2.075f, 1.4f, 0), "Sand");
            B("WallR", new Vector3(0.15f, 2.8f, 4.7f), new Vector3(2.075f, 1.4f, 0), "Sand");
            B("WinWallL", new Vector3(1.25f, 2.8f, 0.15f), new Vector3(-1.525f, 1.4f, 2.275f), "Sand");
            B("WinWallR", new Vector3(1.25f, 2.8f, 0.15f), new Vector3(1.525f, 1.4f, 2.275f), "Sand");
            B("WinWallBottom", new Vector3(1.8f, 0.9f, 0.15f), new Vector3(0, 0.45f, 2.275f), "Sand");
            B("WinWallTop", new Vector3(1.8f, 0.7f, 0.15f), new Vector3(0, 2.45f, 2.275f), "Sand");
            B("WinFrameV", new Vector3(0.06f, 1.2f, 0.08f), new Vector3(0, 1.5f, 2.29f), "Wood");
            B("WinFrameH", new Vector3(1.8f, 0.06f, 0.08f), new Vector3(0, 1.5f, 2.29f), "Wood");
            B("Bed", new Vector3(1.2f, 0.45f, 2.1f), new Vector3(-1.35f, 0.225f, -1.1f), "PaintWhite");
            B("Desk", new Vector3(1.3f, 0.08f, 0.65f), new Vector3(1.3f, 0.78f, 1.6f), "Wood");
            B("DeskLegs", new Vector3(1.2f, 0.74f, 0.55f), new Vector3(1.3f, 0.37f, 1.6f), "Dark");
            B("Radio", new Vector3(0.36f, 0.22f, 0.16f), new Vector3(1.65f, 0.93f, 1.7f), "Dark");
            B("PiggyBank", new Vector3(0.26f, 0.2f, 0.34f), new Vector3(1.05f, 0.92f, 1.55f), "Pink");
            B("RacePoster", new Vector3(0.03f, 0.9f, 0.65f), new Vector3(-2.0f, 1.65f, 0.5f), "Red");
            B("EntryForm", new Vector3(0.03f, 0.42f, 0.3f), new Vector3(-2.0f, 1.45f, 1.25f), "PaintWhite");
            ArtSlot(g, "ArtSlot_RacePoster", g.TransformPoint(new Vector3(-1.98f, 1.65f, 0.5f)), rot * Quaternion.LookRotation(Vector3.right));
            ArtSlot(g, "ArtSlot_EntryForm", g.TransformPoint(new Vector3(-1.98f, 1.45f, 1.25f)), rot * Quaternion.LookRotation(Vector3.right));
            ArtSlot(g, "ArtSlot_PiggyBank", g.TransformPoint(new Vector3(1.05f, 1.05f, 1.55f)), rot);
            Audio(g, "Audio_Radio", g.TransformPoint(new Vector3(1.65f, 0.95f, 1.7f)));
            Audio(g, "Audio_Gulls", g.TransformPoint(new Vector3(0, 1.5f, 4f)));
            var lamp = new GameObject("RoomLamp").AddComponent<Light>(); lamp.transform.SetParent(g); lamp.transform.position = g.TransformPoint(new Vector3(0, 2.5f, -0.4f));
            lamp.type = LightType.Point; lamp.range = 6f; lamp.intensity = 1.6f; lamp.color = new Color(1f, .82f, .6f); lamp.shadows = LightShadows.None;
            System.Action<string, Vector3, Vector3> S = (n, lp, look) => Mark(g, n, g.TransformPoint(lp), Quaternion.LookRotation(g.TransformPoint(look) - g.TransformPoint(lp)));
            S("Shot_1a", new Vector3(1.2f, 1.55f, -1.5f), new Vector3(-2f, 1.55f, 0.8f));
            S("Shot_1b", new Vector3(0.4f, 1.35f, 0.2f), new Vector3(1.1f, 0.95f, 1.55f));
            S("Shot_1c", new Vector3(0f, 1.5f, 1.75f), new Vector3(0f, 0.6f, 12f));
            log.Add($"방({r0.x:F0},{r0.y:F1},{r0.z:F0})");
        }
        // 장면 4·조작 시작: 차 시작 = 본선 끝-12m(MART 앞 #0 까지 12m 뒤), 동쪽(+x)을 본다. 엔진 첫 소리 자리.
        {
            var P = main.pts; int k = P.Count - 12; var f = Flat(P[(k + 2) % P.Count] - P[k - 2]);
            var cs = Mark(parent, "TUT_CarStart", P[k] + Vector3.up * 0.6f, Quaternion.LookRotation(f));
            Audio(cs, "Audio_EngineStart", P[k] + Vector3.up * 0.8f);
            var s4 = P[k] + new Vector3(-26f, 22f, -30f); Mark(parent, "Shot_4a", s4, Quaternion.LookRotation(P[k] + f * 20f - s4));
            Mark(parent, "TUT_ReturnGarage", P[8] + Vector3.up * 0.3f, Quaternion.LookRotation(Flat(P[10] - P[6])));
            var dir = new GameObject("OpeningTutorial (D-142 오프닝·첫 배달)"); dir.transform.SetParent(parent);
            var ot = dir.AddComponent<OpeningTutorial>();
            var oso = new SerializedObject(ot);
            oso.FindProperty("morning").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MoodProfile>(MoodSwitcher.PathMorning);
            oso.ApplyModifiedPropertiesWithoutUndo();
            log.Add($"차시작 본선#{k}({P[k].x:F0},{P[k].z:F0}) 연출감독");
        }
        return "튜토리얼 " + string.Join(" ", log);
    }

    static Transform Mark(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        var t = new GameObject(name).transform; t.SetParent(parent); t.SetPositionAndRotation(pos, rot); return t;
    }

    /// <summary>사람 자리(몸·머리·팔) — 팔은 어깨 축 'ArmPivot' 아래. 아트는 ArtSlot_* 에.</summary>
    static Transform Person(Transform parent, string name, Vector3 feet, Quaternion rot, Material body)
    {
        var g = new GameObject(name).transform; g.SetParent(parent); g.SetPositionAndRotation(feet, rot);
        Box(g, "NPC_Body", new Vector3(0.55f, 1.1f, 0.4f), g.TransformPoint(new Vector3(0, 0.55f, 0)), rot, body, false);
        Box(g, "NPC_Head", new Vector3(0.36f, 0.36f, 0.36f), g.TransformPoint(new Vector3(0, 1.32f, 0)), rot, M("Sand"), false);
        var arm = new GameObject("ArmPivot").transform; arm.SetParent(g); arm.SetPositionAndRotation(g.TransformPoint(new Vector3(0.34f, 1.02f, 0)), rot);
        Box(arm, "Arm", new Vector3(0.14f, 0.6f, 0.14f), g.TransformPoint(new Vector3(0.34f, 0.72f, 0)), rot, body, false);
        ArtSlot(g, "ArtSlot_" + name.Replace("TUT_", ""), g.TransformPoint(new Vector3(0, 0.8f, 0)), rot);
        return g;
    }

    /// <summary>소리 자리 — 클립 없이 AudioSource 만. 클립이 꽂히면 OpeningTutorial 이 장면에 맞춰 튼다.</summary>
    static Transform Audio(Transform parent, string name, Vector3 pos)
    {
        var t = Mark(parent, name, pos, Quaternion.identity);
        var a = t.gameObject.AddComponent<AudioSource>(); a.playOnAwake = false; a.spatialBlend = 0.6f;
        return t;
    }'''
assert old_ret in s
s = s.replace(old_ret, new_ret, 1)

old_c = '''"Gutter" => new Color(.42f, .42f, .42f), _ => Color.magenta'''
new_c = '''"Gutter" => new Color(.42f, .42f, .42f), "Pink" => new Color(.92f, .62f, .66f), _ => Color.magenta'''
assert old_c in s
s = s.replace(old_c, new_c, 1)
open(p, 'w', encoding='utf-8').write(s)
print('ok')
