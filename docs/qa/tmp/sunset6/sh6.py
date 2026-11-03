p='F1/Assets/Art/Look/DuskSea.shader'
s=open(p,encoding='utf-8').read()
def rep(a,b,cnt=1):
    global s
    assert s.count(a)==cnt,(a,s.count(a)); s=s.replace(a,b)
rep('''        _Gerst("Gerstner''','''        _Spec("6판 ② 노멀 전용 파 8개 (on 0/1, 가장 긴 파장 m, 파장 비, 방위 퍼짐 deg)", Vector) = (0, 12, 1.4, 40)
        _Micro("6판 ③ 비주기 세부 노멀 2겹 (on 0/1, 크기 a m, 크기 b m, 기울기)", Vector) = (0, 0.9, 0.35, 0.05)
        _Dash("6판 ⑤ 빛길 가로 대시 (옆 퍼짐 배수 1 = 끔, 0, 0, 0)", Vector) = (1, 0, 0, 0)
        _Gerst("Gerstner''')
rep('float4 _W0, _W1, _W2, _W3, _Detail, _Detail2,','float4 _W0, _W1, _W2, _W3, _Detail, _Detail2, _Spec, _Micro, _Dash,')
rep('''float4 _PCSeaReflOn;   //''','''float4 _PCSeaReflOn, _PCSeaClock;   // _PCSeaClock.x = 1 → 시간을 y 로 고정(편집 모드 전/후 캡처용). ''')
rep('''            float WaveTime() { float t = _Time.y,''','''            float Now() { return _PCSeaClock.x > .5 ? _PCSeaClock.y : _Time.y; }
            float WaveTime() { float t = Now(),''')
rep('floor(_Time.y * 4)','floor(Now() * 4)')
rep('frac(_Time.y * _SparkleRate','frac(Now() * _SparkleRate')
rep('''                N = normalize(N);
                crest =''','''                // 6판 ②: 노멀 전용 파 8개 — 파장 _Spec.y 부터 _Spec.z 배씩 짧게, 방위는 ±_Spec.w 안에 흩뿌림(한 방향 원뿔 → 교차하는 잔파)
                if (_Spec.x > .5)
                {
                    [unroll] for (int q = 0; q < 8; q++)
                    {
                        float L = _Spec.y * pow(abs(_Spec.z), -q), S = .04 * att;
                        float hq = frac(sin(q * 12.9898 + 4.1) * 43758.5453) * 2 - 1;
                        float2 D = WaveDir(hq * _Spec.w);
                        float kk = 6.2832 / L, ph = kk * dot(D, xz) - sqrt(9.8 * kk) * t + 1.7 * q + .4;
                        float f = 1 - smoothstep(_Filt, _Filt * 2, fp / L);
                        N.xz -= D * S * cos(ph) * f;
                        var += S * S * .5 * (1 - f);
                    }
                }
                // 6판 ③: 비주기 세부 노멀 2겹 — 값 노이즈 기울기(중앙 차분), 서로 다른 회전·크기·흐름. 사인 합의 반복 무늬를 깸
                if (_Micro.x > .5)
                {
                    [unroll] for (int m = 0; m < 2; m++)
                    {
                        float sc = m == 0 ? _Micro.y : _Micro.z, an = radians(m == 0 ? 35 : -20);
                        float2x2 Rm = float2x2(cos(an), -sin(an), sin(an), cos(an));
                        float2 qn = mul(Rm, xz) / sc + float2(0, t * sqrt(9.8 * sc / 6.2832) / sc) + m * 17.3;
                        const float h = .25;
                        float2 g = float2(dkNoise(qn + float2(h, 0)) - dkNoise(qn - float2(h, 0)), dkNoise(qn + float2(0, h)) - dkNoise(qn - float2(0, h))) / (2 * h);
                        float S = _Micro.w * att / 1.5;
                        float f = 1 - smoothstep(_Filt, _Filt * 2, fp / sc);
                        N.xz -= mul(transpose(Rm), g) * S * f;
                        var += S * S * 1.1 * (1 - f);
                    }
                }
                N = normalize(N);
                crest =''')
rep('''                float P = pow(saturate(dot(R, sun)), sh) * min(1, sh / _Shine * 2.5) * _Glint;''','''                float P = pow(saturate(dot(R, sun)), sh);
                // 6판 ⑤: 옆(수평)으로만 _Dash.x 배 넓게 퍼지는 해 반사 → 파 면 하나가 가로로 긴 대시로 비침
                if (_Dash.x > 1.01)
                {
                    float3 sd = normalize(float3(sun.z, 0, -sun.x)), su = cross(sun, sd);
                    float dx = dot(R, sd), dy = dot(R, su);
                    P = step(0, dot(R, sun)) * exp(-.5 * sh * (dx * dx / _Dash.x + dy * dy));
                }
                P *= min(1, sh / _Shine * 2.5) * _Glint;''')
open(p,'w',encoding='utf-8',newline='\n').write(s)
print('ok')
