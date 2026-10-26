// 과제 2024 (D-147 규칙 8·7): Look 5 전용 바닥 원판. PCLookScene 이 가로등·벽등·전조등 밑(가산 빛 웅덩이)과 차·사람 밑(블롭 그림자)에 깐다.
//  _Add=1: 가산(빛 웅덩이) — 세기 = _Color × 전역 _PCPool(낮 0 · 노을/초저녁 >0). 가장자리 부드럽게, 디더 없음.
//  _Add=0: 곱(블롭 그림자) — 가운데 _Color.a 만큼 어둡게.
// uv (0.5,0.5) 가 중심. 전조등 원뿔은 uv.y 방향으로 길쭉한 쿼드에 _Cone=1.
Shader "PitCrew/Look/FloorDisc"
{
 Properties {
  _Color("Color",Color)=(1,0.8,0.5,1)
  [Toggle] _Add("Additive pool (else blob shadow)",Float)=1
  _Cone("Headlight cone",Float)=0
  [HideInInspector] _SrcB("",Float)=1
  [HideInInspector] _DstB("",Float)=1
 }
 SubShader {
  Tags {"RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline"}
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   ZWrite Off Cull Off Offset -2,-2
   Blend [_SrcB] [_DstB]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Assets/Art/Look/PCLook.hlsl"
   float _PCPool;
   CBUFFER_START(UnityPerMaterial)
    float4 _Color; float _Add, _Cone, _SrcB, _DstB;
   CBUFFER_END
   struct A {float4 positionOS:POSITION; float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 pos:TEXCOORD1;};
   V vert(A v){V o;o.pos=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=PCSnap(TransformWorldToHClip(o.pos));o.uv=v.uv;return o;}
   half4 frag(V i):SV_Target{
    if (PCMode()!=5) discard;
    float2 p=i.uv*2-1;
    float r=_Cone>.5 ? length(float2(p.x/max(.15,(p.y+1)*.5),p.y)) : length(p);   // 원뿔: 차 쪽(-1)이 좁고 앞(+1)이 넓다
    float k=1-smoothstep(.25,1,r);
    float fog=saturate((distance(i.pos,_WorldSpaceCameraPos)-_PCFogParams.x)/max(1,_PCFogParams.y-_PCFogParams.x))*_PCFogParams.z;
    if (_Add>.5) return half4(_Color.rgb*k*_PCPool*(1-fog),1);
    return half4(lerp(float3(1,1,1),float3(1,1,1)*(1-_Color.a),k*(1-fog)),1);
   }
   ENDHLSL
  }
 }
}
