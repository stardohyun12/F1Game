Shader "PitCrew/WrapRibbon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.42, 0.72, 0.91, 0.7)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            struct A { float4 pos:POSITION; float3 nrm:NORMAL; float2 uv:TEXCOORD0; };
            struct V { float4 pos:SV_POSITION; float3 nrm:TEXCOORD0; float2 uv:TEXCOORD1; };

            V vert(A i)
            {
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.nrm = TransformObjectToWorldNormal(i.nrm);
                o.uv = i.uv;
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                float ndl = saturate(dot(normalize(i.nrm), normalize(float3(0.3, 1, 0.2))));
                float3 col = _BaseColor.rgb * (0.55 + 0.45 * ndl);
                return half4(col, _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
