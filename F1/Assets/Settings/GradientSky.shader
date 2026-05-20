Shader "PitCrew/GradientSky"
{
    Properties
    {
        _TopColor      ("Top (Zenith)", Color)  = (0.27, 0.50, 0.85, 1)
        _HorizonColor  ("Horizon", Color)       = (0.80, 0.88, 0.96, 1)
        _BottomColor   ("Bottom (Ground)", Color)= (0.52, 0.54, 0.58, 1)
        _Exponent      ("Sky Falloff", Float)    = 1.8
        _BottomExponent("Ground Falloff", Float) = 2.0
        _HorizonBlur   ("Horizon Softness", Range(0.001,0.3)) = 0.08
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            half4 _TopColor, _HorizonColor, _BottomColor;
            float _Exponent, _BottomExponent, _HorizonBlur;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.dir = IN.positionOS.xyz; // skybox mesh centered → dir = view ray
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float y = normalize(IN.dir).y;
                // 지평선 부근 부드러운 전환
                float up   = pow(saturate(smoothstep(_HorizonBlur, 1.0, y)),  1.0 / max(_Exponent, 0.01));
                float down = pow(saturate(smoothstep(_HorizonBlur, 1.0, -y)), 1.0 / max(_BottomExponent, 0.01));
                half3 col = _HorizonColor.rgb;
                col = lerp(col, _TopColor.rgb, up);
                col = lerp(col, _BottomColor.rgb, down);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
