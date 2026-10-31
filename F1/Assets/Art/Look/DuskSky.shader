// D-227 노을 하늘: 지평선 주황(해 쪽) / 라벤더(반대) → 분홍 → 위 보라·청, 해 원반 + 낮은 구름 띠. 값은 SunsetLook 전역(Dusk.hlsl).
Shader "PitCrew/Look/DuskSky"
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
            #include "Assets/Art/Look/Dusk.hlsl"
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.pos = TransformObjectToHClip(v.xyz); o.dir = v.xyz; return o; }
            half4 frag(v2f i) : SV_Target { return half4(DuskSkyColor(i.dir, 1), 1); }
            ENDHLSL
        }
    }
}
