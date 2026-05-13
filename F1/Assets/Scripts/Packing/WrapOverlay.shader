Shader "PitCrew/WrapOverlay"
{
    Properties
    {
        _BaseColor   ("Base Color", Color) = (0.55, 0.45, 0.32, 1)
        _WrapTex     ("Wrap State RT", 2D) = "black" {}
        _ColorR      ("Channel R Color (Bubble)", Color) = (0.42, 0.72, 0.91, 0.85)
        _ColorG      ("Channel G Color (Foam)",   Color) = (0.91, 0.84, 0.42, 0.95)
        _ColorB      ("Channel B Color (Hardcase)", Color) = (0.33, 0.33, 0.36, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            sampler2D _WrapTex;
            fixed4 _ColorR, _ColorG, _ColorB;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 nrm : TEXCOORD1; };

            v2f vert (appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv;
                o.nrm = UnityObjectToWorldNormal(v.normal); return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 w = tex2D(_WrapTex, i.uv);
                // 최상위 재료 = 채널 우선순위 B > G > R
                fixed3 col = _BaseColor.rgb;
                if (w.r > 0.5) col = lerp(col, _ColorR.rgb, _ColorR.a);
                if (w.g > 0.5) col = lerp(col, _ColorG.rgb, _ColorG.a);
                if (w.b > 0.5) col = lerp(col, _ColorB.rgb, _ColorB.a);
                // 간단한 lambert (URP 의존 줄임)
                float ndl = saturate(dot(normalize(i.nrm), normalize(float3(0.3, 1, 0.2))));
                col *= 0.6 + 0.4 * ndl;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
