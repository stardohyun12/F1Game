p='F1/Assets/Editor/RoadNetV2.cs'
s=open(p,encoding='utf-8').read()
def R(a,b):
    global s
    assert s.count(a)==1,(a[:60],s.count(a)); s=s.replace(a,b)
R('''if (f == e || f.w <= e.w || !V2BBX(eb, V2BB(f.p, f.w))) continue;''','''if (f == e || f.w < e.w || (f.w == e.w && V2Len(f.p) <= V2Len(e.p)) || !V2BBX(eb, V2BB(f.p, f.w))) continue;
                    // 2단계: 같은 폭끼리도 나란히 겹침(tomo_118267009_9_s1 ⊂ tomo_461561406_27 11 m, 겹침 쌍 100+) → 짧은 쪽을 자른다. 직각 교차(띠 안 ≈ 폭)는 안 자르게 1.6×폭 이상만
                    float need = f.w > e.w ? 4f : Mathf.Max(6f, f.w * 1.6f);''')
R('''if (V2Len(e.p.GetRange(i, j - i + 1)) >= 4f)''','''if (V2Len(e.p.GetRange(i, j - i + 1)) >= need)''')
R('''int moved = 0, padN = 0; float maxD = 0;''','''int moved = 0, padN = 0; float maxD = 0; Vector3 maxAt = Vector3.zero; int big = 0;''')
R('''maxD = Mathf.Max(maxD, Mathf.Abs(y - orig)); }''','''if (Mathf.Abs(y - orig) > maxD) { maxD = Mathf.Abs(y - orig); maxAt = p; } if (Mathf.Abs(y - orig) > 3f) big++; }''')
R('''lg.Add($"지형 정점 {moved}개(최대 {maxD:F2} m)''','''lg.Add($"지형 정점 {moved}개(최대 {maxD:F2} m @({maxAt.x:F0},{maxAt.z:F0}) 원래 {maxAt.y:F1}, 3 m 넘게 바뀐 정점 {big})''')
open(p,'w',encoding='utf-8').write(s); print('ok')
