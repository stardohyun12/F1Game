// 장면 A7(과제 20261028-0236) 붓질 필터 — 720 저해상도 RT 위에서 Kuwahara.
// 화면 픽셀마다 "자기가 속한 RT 텍셀" 기준으로 주변 네 사각형(각 (r+1)²텍셀)의 평균·분산을 보고, 가장 고른(분산 작은) 사각형의 평균색을 쓴다
// = 가장자리는 살고 안쪽은 붓 덩어리처럼 뭉친다. 결과도 RT 텍셀 단위로 계단(Nearest)이라 픽셀 격자가 유지된다.
// LowResRenderTest 의 presentMaterial 자리(RawImage·Present 둘 다)에 꽂아 쓴다.
Shader "Hidden/PitCrew/A7Kuwahara"
{
    Properties
    {
        _MainTex ("RT", 2D) = "white" {}
        _Radius ("radius (RT texels)", Range(1, 4)) = 2
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            Texture2D _MainTex; SamplerState sampler_point_clamp; float4 _MainTex_TexelSize; float _Radius;
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; o.pos = UnityObjectToClipPos(i.pos); o.uv = i.uv; return o; }

            float3 S(float2 tc) { return _MainTex.SampleLevel(sampler_point_clamp, tc * _MainTex_TexelSize.xy, 0).rgb; }

            fixed4 frag(V i) : SV_Target
            {
                float2 tc = floor(i.uv * _MainTex_TexelSize.zw) + 0.5;     // 이 화면 픽셀이 속한 RT 텍셀 중심
                int r = (int)round(_Radius);
                float3 m[4] = { 0.0.xxx, 0.0.xxx, 0.0.xxx, 0.0.xxx }; float3 s[4] = { 0.0.xxx, 0.0.xxx, 0.0.xxx, 0.0.xxx };
                const int2 q[4] = { int2(-1, -1), int2(1, -1), int2(-1, 1), int2(1, 1) };
                [unroll] for (int k = 0; k < 4; k++)
                {
                    [loop] for (int y = 0; y <= r; y++)
                    [loop] for (int x = 0; x <= r; x++)
                    {
                        float3 c = S(tc + float2(x * q[k].x, y * q[k].y));
                        m[k] += c; s[k] += c * c;
                    }
                }
                float n = (r + 1) * (r + 1); float best = 1e9; float3 col = 0;
                [unroll] for (int j = 0; j < 4; j++)
                {
                    float3 mu = m[j] / n; float3 v = abs(s[j] / n - mu * mu);
                    float vs = v.r + v.g + v.b;
                    if (vs < best) { best = vs; col = mu; }
                }
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
