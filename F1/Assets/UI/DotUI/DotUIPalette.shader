// UI 2판(D-241): 도트 화면 UI 전용. 세상과 같은 방식 — 정해진 팔레트(시간대 색, DotScreenUI 가 매 프레임 넣음)에서
// 가장 가까운 색으로 다시 찍고, 그 전에 화면 픽셀 고정 Bayer 4×4(Dusk.hlsl dkBayer4 와 같은 식)로 살짝 흔든다.
// → 종이 결(±8~12%)이 팔레트 단 사이에서만 디더로 갈리고, 평평한 면은 한 색으로 남는다.
// 좌표: World Space 캔버스 루트 1 단위 = 도트 1칸(DotScreenUI.Fit) → 정점 위치(캔버스 공간)를 그대로 도트 좌표로 쓴다.
// 공유 .mat 없음 — 머티리얼은 DotScreenUI 가 실행 중에 만든다.
Shader "PitCrew/UI/DotPalette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Spread ("Bayer 흔들기(감마 공간 폭)", Float) = 0.06
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _TextureSampleAdd;   // 글꼴(Alpha8)은 Text 가 (1,1,1,0) 을 넣는다
            float _Spread;
            #define PAL_N 10
            float4 _Pal[PAL_N];          // 선형 색. w 는 안 씀
            float _PalCount;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float2 dotp : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.dotp = v.vertex.xy;   // 캔버스 공간 = 도트 칸(가운데 원점, 320·180 은 4 의 배수라 세상 Bayer 와 같은 위상)
                return o;
            }

            float Bayer2(float2 a) { a = floor(a); return frac(a.x * .5 + a.y * a.y * .75); }
            float Bayer4(float2 a) { return Bayer2(.5 * a) * .25 + Bayer2(a); }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 c = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                clip(c.a - 0.5);                                   // 반투명 없음 — 칸은 있거나 없거나
                float th = Bayer4(floor(i.dotp + 1e-3)) - 0.5;
                float3 g = sqrt(saturate(c.rgb)) + th * _Spread;   // 감마 근사 공간에서 흔들고 비교
                float best = 1e9; float3 col = c.rgb;
                [unroll] for (int k = 0; k < PAL_N; k++)
                {
                    float3 d = g - sqrt(_Pal[k].rgb);
                    float e = dot(d * d, float3(0.30, 0.55, 0.15)) + (k < _PalCount ? 0 : 1e8);
                    if (e < best) { best = e; col = _Pal[k].rgb; }
                }
                return float4(col, 1);
            }
            ENDCG
        }
    }
}
