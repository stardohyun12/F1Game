// Look 5b 하늘(D-148 밝고 맑게): 지평선 = 안개색(_PCFogColor), 위로 갈수록 맑은 하늘색(_PCSkyColor). 둘 다 PCLookScene 이 Linear 로 넣는 전역.
// 계단 없이 매끈한 두 색 — EDC 규칙 5 의 "하늘 = 안개 한 색"을 지평선 띠로만 남기고 위는 파랑.
Shader "PitCrew/Look/SkyGrad"
{
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _PCFogColor, _PCSkyColor, _PCSunDir, _PCSunGlow;   // 2203: 해 쪽 지평선 금빛
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.pos = TransformObjectToHClip(v.xyz); o.dir = v.xyz; return o; }
            half4 frag(v2f i) : SV_Target
            {
                float y = normalize(i.dir).y;
                float t = smoothstep(.0, .32, y);   // 지평선 위 0~18° 띠에서 안개 → 파랑
                float3 col = lerp(_PCFogColor.rgb, _PCSkyColor.rgb, t);
                float sd = saturate(dot(normalize(i.dir), _PCSunDir.xyz));
                col = lerp(col, _PCSunGlow.rgb, pow(sd, 8) * (1 - t * .7) * _PCSunGlow.a);   // 2203: 노을 = 해 쪽 금빛 → 위·반대쪽 라벤더
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
