// D-236 노을 7판 역광 테: LowResRenderTest 가 Nearest 로 화면에 올릴 때(smooth 꺼짐) 노을 동안만 끼우는 소재.
// 수평선 위에서 어두운 실루엣 텍셀의 바로 위(또는 옆)가 밝은 하늘이면 그 텍셀 하나를 노을빛으로 = 저해상도 1px 테.
// 나머지는 RT 그대로 Nearest(색 후처리 없음).
Shader "PitCrew/Look/DuskRim"
{
    Properties
    {
        [PerRendererData] _MainTex ("RT", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Cull Off ZWrite Off ZTest Always Blend Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SamplerState sampler_point_clamp_dr;
            float4 _MainTex_TexelSize;
            float4 _PCDusk, _PCDuskRim, _PCDuskRimCol, _PCDuskRimHz;   // SunsetLook 전역(_PCDuskRimHz.x = 수평선 화면 v)
            float4 _PCDuskSunScr, _PCDuskScrGlow, _PCDuskScrGlowCol;     // 8판 해 둘레 빛무리: 해 화면 (x, y, 보임), (r1, r2 화면 높이 비, a1, a2), 색
            // 4×4 Bayer 0..1(렌더 픽셀 고정)
            float Bayer4(float2 p)
            {
                uint2 q = (uint2)p & 3;
                const float m[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
                return (m[q.y * 4 + q.x] + .5) / 16;
            }
            struct a2v { float4 v : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(a2v i) { v2f o; o.pos = TransformObjectToHClip(i.v.xyz); o.uv = i.uv; return o; }
            #define T(o) SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_point_clamp_dr, tc + (o) * px, 0).rgb
            #define L(o) dot(T(o), half3(.2126, .7152, .0722))
            half4 frag(v2f i) : SV_Target
            {
                half3 c = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_point_clamp_dr, i.uv, 0).rgb;
                if (_PCDusk.x > 0 && _PCDuskRim.x > .5 && i.uv.y > _PCDuskRimHz.x)
                {
                    // 이웃 = 실제 렌더 픽셀 하나(_PCDuskRimHz.zw = 렌더 가로·세로. RT 는 renderScale 때문에 2배일 수 있어 RT 텍셀로 보면 반 픽셀)
                    float2 grid = _PCDuskRimHz.z > 0 ? _PCDuskRimHz.zw : _MainTex_TexelSize.zw, px = 1 / grid;
                    float2 tc = (floor(i.uv * grid) + .5) * px;
                    c = T(float2(0, 0));
                    // 해 쪽 이웃만 본다(_PCDuskRimHz.y = 해 화면 x): 해 왼쪽 실루엣은 오른쪽 가장자리, 오른쪽은 왼쪽 가장자리. 위쪽(꼭대기)은 약하게
                    float side = i.uv.x < _PCDuskRimHz.y ? 1 : -1;
                    half l0 = dot(c, half3(.2126, .7152, .0722)), lu = L(float2(0, 1)), ls = L(float2(side, 0));
                    half dark = step(l0, _PCDuskRim.y);
                    half sky = max(step(_PCDuskRim.z, ls), step(_PCDuskRim.z, lu) * .5);
                    c = lerp(c, _PCDuskRimCol.rgb, dark * sky * _PCDuskRim.w);
                }
                // 8판: 해 둘레만 아주 약한 따뜻한 빛무리 — 블룸 대신 2단 계단(단 끝만 Bayer 디더), 나무·물 위에도 얹힌다(screen 섞기)
                if (_PCDusk.x > 0 && _PCDuskSunScr.z > 0)
                {
                    float2 grid = _PCDuskRimHz.z > 0 ? _PCDuskRimHz.zw : _MainTex_TexelSize.zw;
                    float2 pp = floor(i.uv * grid);
                    float2 dv = (pp + .5 - _PCDuskSunScr.xy * grid) / grid.y;
                    float d = length(dv), th = (Bayer4(pp) - .5) * .035;
                    half a = d < _PCDuskScrGlow.x + th ? _PCDuskScrGlow.z : (d < _PCDuskScrGlow.y + th ? _PCDuskScrGlow.w : 0);
                    c = c + (1 - c) * _PCDuskScrGlowCol.rgb * a;
                }
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
