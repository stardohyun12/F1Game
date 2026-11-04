p = 'F1/Assets/Art/SceneA7/SceneA7Lit.shader'
s = open(p, encoding='utf-8').read()
def rep(a, b):
    global s
    assert s.count(a) == 1, a[:60]
    s = s.replace(a, b)
rep('''        //  _A7Dio = (켬, -, 카메라 쪽 벽 버리는 문턱(상자 좌표), 천장 버리는 문턱) · _A7DioAmb = 실내 바탕빛(어두운 색)
        //  켜지면 방 상자 밖 조각은 버리고(배경 검정), 카메라 쪽 벽·천장도 버린다. 상자 안은 해 대신 추가 광원(창·책상 전등)만 받는다''',
'''        //  _A7Dio = (켬, 해 세기 배수(1 − 안개), 카메라 쪽 벽 버리는 문턱(상자 좌표), 천장 버리는 문턱) · _A7DioAmb = (실내 바탕빛 rgb, 자르는 집 범위(상자 좌표))
        //  켜지면 집을 단면으로 자른다: 집 범위 안에서 천장 위(지붕)와 카메라 쪽 벽 바깥만 버리고, 집 밖 세계·방 바닥 아래는 그린다(과제 20261104-1635: 창 밖 = 바깥 세계).
        //  상자 안은 해(그림자 포함) × 배수 + 약한 바탕빛 + 추가 광원(책상 전등)''')
rep('''            float3 b = mul(_A7InBox, float4(wp, 1)).xyz;
            if (max(abs(b.x), max(abs(b.y), abs(b.z))) > 1 || b.y > _A7Dio.w) return true;
            float3 c''', '''            float3 b = mul(_A7InBox, float4(wp, 1)).xyz;
            if (max(abs(b.x), abs(b.z)) > _A7DioAmb.w || b.y < -1) return false;   // 집 밖·1층은 그대로
            if (b.y > _A7Dio.w) return true;
            float3 c''')
rep('''                // 디오라마: 해·그늘색 대신 어두운 바탕빛 + 추가 광원(창 한 곳·책상 전등 한 곳). 4단 계단(garage A7LitPoint 와 같은 식)
                if (dio)
                {
                    c = t.rgb * _A7DioAmb.rgb;''', '''                // 디오라마: 해(창으로 들어온 빛 — 단단한 그림자, 2단) × (1 − 안개) + 약한 바탕빛 + 추가 광원(책상 전등). 전등은 4단 계단(garage A7LitPoint 와 같은 식)
                if (dio)
                {
                    Light sl = GetMainLight(TransformWorldToShadowCoord(i.wp));
                    float sk = dot(n, sl.direction);
                    float sv = (sk > 0.3 ? 1.0 : (sk > 0.02 ? 0.6 : 0.0)) * step(0.5, sl.shadowAttenuation) * _A7Dio.y;
                    c = t.rgb * (_A7DioAmb.rgb + sl.color * sv);''')
open(p, 'w', encoding='utf-8').write(s)
print('ok')
