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
            float4 _PCFogColor, _PCSkyColor;
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.pos = TransformObjectToHClip(v.xyz); o.dir = v.xyz; return o; }
            half4 frag(v2f i) : SV_Target
            {
                float y = normalize(i.dir).y;
                float t = smoothstep(.0, .32, y);   // 지평선 위 0~18° 띠에서 안개 → 파랑
                return half4(lerp(_PCFogColor.rgb, _PCSkyColor.rgb, t), 1);
            }
            ENDHLSL
        }
    }
}
