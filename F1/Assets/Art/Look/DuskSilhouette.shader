// D-236 노을 수평선 연출물(먼 곶·섬·등대·배) — 빛 안 받는 단색 실루엣. 멀수록 수평선 하늘색으로 한 단 밝게(_Haze, 렌더러별 프로퍼티 블록).
//  색은 아트 에셋이 아니라 이 장면 연출용 대역 — 실루엣이라 형태 외곽만 읽힌다.
Shader "PitCrew/Look/DuskSilhouette"
{
    Properties
    {
        _Color("Silhouette", Color) = (0.13, 0.10, 0.22, 1)
        _Haze("Haze to horizon sky 0..1", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Art/Look/Dusk.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Color; float _Haze;
            CBUFFER_END
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };
            v2f vert(float4 v : POSITION) { v2f o; o.wpos = TransformObjectToWorld(v.xyz); o.pos = TransformWorldToHClip(o.wpos); return o; }
            half4 frag(v2f i) : SV_Target
            {
                float3 V = i.wpos - _WorldSpaceCameraPos;
                float3 hz = DuskSkyColor(float3(V.x, .002, V.z), 0);
                return half4(lerp(_Color.rgb, hz, _Haze), 1);
            }
            ENDHLSL
        }
    }
}
