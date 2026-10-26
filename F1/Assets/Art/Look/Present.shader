// Look 5c(D-149 지글거림 금지, 은은하게): 저해상도 RT → 화면 확대 소재. LowResRenderTest 의 RawImage / 캡처 Blit 이 쓴다.
// 1) 바이리니어 확대 + 0.6텍셀 텐트 4탭 = 계단·크롤링 없이 저해상도 흐림만 남김
// 2) 약한 블룸: 밝은 곳(휘도 > _Bloom.x)만 2.5텍셀 반경 8탭 평균을 더함(_Bloom.y 세기)
// 3) 아주 옅은 헤이즈: 안개색 쪽으로 _Haze 만큼(어두운 곳 위주 — 밝고 맑음 유지, 뿌옇게 금지)
Shader "PitCrew/Look/Present"
{
    Properties
    {
        [PerRendererData] _MainTex ("RT", 2D) = "white" {}
        _Bloom ("Bloom (threshold, strength, radius texels)", Vector) = (.72, .28, 2.5, 0)
        _Haze ("Haze", Range(0, .2)) = .035
        _Soft ("Soft (tent texels)", Range(0, 1.5)) = .6
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
            TEXTURE2D(_MainTex); SamplerState sampler_linear_clamp_present;
            float4 _MainTex_TexelSize, _Bloom, _PCFogColor;
            float _Haze, _Soft;
            struct a2v { float4 v : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(a2v i) { v2f o; o.pos = TransformObjectToHClip(i.v.xyz); o.uv = i.uv; return o; }
            #define S(o) SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_linear_clamp_present, i.uv + (o) * _MainTex_TexelSize.xy, 0).rgb
            half4 frag(v2f i) : SV_Target
            {
                float s = _Soft;
                half3 c = (S(float2(-s, -s)) + S(float2(s, -s)) + S(float2(-s, s)) + S(float2(s, s))) * .25;
                half3 b = 0; float r = _Bloom.z;
                [unroll] for (int k = 0; k < 8; k++)
                {
                    float a = k * 0.785398;
                    half3 t = S(float2(cos(a), sin(a)) * r);
                    b += max(0, t - _Bloom.x);
                }
                c += b * (_Bloom.y / 8);
                half L = dot(c, half3(.2126, .7152, .0722));
                c = lerp(c, _PCFogColor.rgb, _Haze * (1.4 - L));
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
