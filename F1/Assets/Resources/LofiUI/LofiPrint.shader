// D-258 UI A안(과제 20261102-2038): 세상 속 종이·칠판 면 — LofiSurface 와 같은 빛(해·그늘·구름)·안개 계산.
// 다른 점 두 가지만: ① PCTex 9탭 흐림 대신 2~3배로 찍은 RT 를 선형으로 한 번 읽는다(1배·최근접은 반 픽셀 어긋나 Galmuri 획이 빠졌다 — 21:00 실측)
// ② 알파 0.5 미만은 버린다(찢긴 종이 가장자리), 양면.
Shader "PitCrew/LofiPilot/Print"
{
 Properties {
  _MainTex("Printed RT",2D)="white" {}
  _Tint("Tint",Color)=(1,1,1,1)
  _FaceShade("Broad face shading",Range(0,1))=0.28
  _Ambient("Ambient tone",Color)=(0.93,0.93,0.88,1)
  _FogColor("Maritime haze",Color)=(0.54,0.65,0.69,1)
  _FogStart("Fog start",Float)=30
  _FogEnd("Fog end",Float)=105
 }
 SubShader {
  Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline"}
  Cull Off
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Assets/Art/Look/PCLook.hlsl"
   TEXTURE2D(_MainTex);   // sampler_linear_clamp 는 URP Core 가 이미 선언
   CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_TexelSize;
    float4 _MainTex_ST, _Tint, _Ambient, _FogColor;
    float _FaceShade, _FogStart, _FogEnd;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 pos:TEXCOORD2;};
   Varyings vert(Attributes v) {
    Varyings o;o.pos=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=PCSnap(TransformWorldToHClip(o.pos));
    o.uv=TRANSFORM_TEX(v.uv,_MainTex);
    o.normal=TransformObjectToWorldNormal(v.normalOS);return o;
   }
   half4 frag(Varyings i):SV_Target {
    half4 t=SAMPLE_TEXTURE2D(_MainTex,sampler_linear_clamp,i.uv);
    clip(t.a-0.5);
    float3 n=normalize(i.normal);
    if(dot(n,GetWorldSpaceViewDir(i.pos))<0) n=-n;   // 양면: 보는 쪽 면으로
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.pos));
    float diffuse=saturate(dot(n,sun.direction));
    float3 shade=PCShade(_Ambient.rgb,sun.color.rgb,diffuse,sun.shadowAttenuation,_FaceShade);
    shade*=SkyCloudShade(i.pos,sun.direction,diffuse);
    half3 col=t.rgb*_Tint.rgb*shade; col=PCPost(col,i.pos,n);
    return half4(PCFog(col,i.pos,_FogColor.rgb,_FogStart,_FogEnd),1);
   }
   ENDHLSL
  }
 }
}
