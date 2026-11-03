p='F1/Assets/Scripts/Debug/DebugPlaces.cs'
s=open(p,encoding='utf-8',newline='').read()
i=s.index('    public static readonly Place[] All =')
j=s.index('    };\n}\n#endif')+len('    };\n')
new='''    static Place[] all;

    /// <summary>D-289: 장소 표는 world/world.yaml 이 원천 → WorldPlaces.asset(gen.py). 여기는 읽어서 Place 로 바꿀 뿐이다. 에셋이 없으면 빈 표.</summary>
    public static Place[] All => all ??= Build();

    /// <summary>에셋을 다시 만든 뒤(gen.py) 부른다 — 캐시를 버리고 다시 읽는다.</summary>
    public static void Reload() { all = null; }

    static Place[] Build()
    {
        var src = WorldData.Places;
        if (src == null) { Debug.LogWarning("[DebugPlaces] WorldPlaces 에셋 없음 — python tools/world/gen.py"); return new Place[0]; }
        var list = new Place[src.spots.Count];
        for (int i = 0; i < list.Length; i++)
        {
            var s = src.spots[i];
            var rule = (CarRule)System.Enum.Parse(typeof(CarRule), s.car);
            list[i] = rule == CarRule.Fixed ? new Place(s.name, s.feet, s.yaw, s.carAt, s.carYaw, s.role) : new Place(s.name, s.feet, s.yaw, rule, s.role);
        }
        return list;
    }
'''
s=s[:i]+new+s[j:]
s=s.replace('''/// 출처: 집 방''','''/// (D-289: 아래 출처 줄은 표를 yaml 로 옮기기 전 기록이다. 표 자체는 world/world.yaml 의 places.)
/// 출처: 집 방''',1)
open(p,'w',encoding='utf-8',newline='').write(s)
