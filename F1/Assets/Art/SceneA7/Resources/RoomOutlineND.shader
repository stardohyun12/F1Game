// 과제 20261104-1449(D-297 B안): 방 윤곽선용 — 화면 공간 법선(xyz, 0..1) + 눈 거리(w, m). RoomOutline 이 집 렌더러만 따로 그린다.
Shader "Hidden/PitCrew/RoomOutlineND"
{
    SubShader
    {
        Pass
        {
            Cull Off ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 pos : POSITION; float3 n : NORMAL; };
            struct V { float4 pos : SV_POSITION; float3 vn : TEXCOORD0; float vz : TEXCOORD1; };
            V vert(A i)
            {
                V o; float3 wp = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(wp);
                o.vn = TransformWorldToViewDir(TransformObjectToWorldNormal(i.n), true);
                o.vz = -TransformWorldToView(wp).z; return o;
            }
            float4 frag(V i, bool front : SV_IsFrontFace) : SV_Target
            {
                float3 n = normalize(i.vn) * (front ? 1 : -1);   // 안쪽에서 보는 벽(Cull Off)도 카메라 쪽 법선
                return float4(n * 0.5 + 0.5, i.vz);
            }
            ENDHLSL
        }
    }
}
