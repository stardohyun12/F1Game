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
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
