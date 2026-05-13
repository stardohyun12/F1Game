Shader "PitCrew/WrapStripBlit"
{
    Properties
    {
        _MainTex ("Source", 2D) = "black" {}
        _UVPrev ("UV Prev", Vector) = (0,0,0,0)
        _UVCurr ("UV Curr", Vector) = (0,0,0,0)
        _Width  ("Strip Width (UV)", Float) = 0.06
        _ChannelMask ("Channel Mask (R/G/B/A 1=draw)", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _UVPrev;
            float4 _UVCurr;
            float  _Width;
            float4 _ChannelMask;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            // distance from point p to segment a-b in 2D
            float distSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a, ba = b - a;
                float h = saturate(dot(pa, ba) / max(dot(ba, ba), 1e-6));
                return length(pa - ba * h);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 src = tex2D(_MainTex, i.uv);
                float d = distSegment(i.uv, _UVPrev.xy, _UVCurr.xy);
                float inside = step(d, _Width * 0.5);
                // active channel: set to 1 inside the strip; other channels preserved
                return lerp(src, src + (_ChannelMask * inside), inside);
            }
            ENDCG
        }
    }
}
