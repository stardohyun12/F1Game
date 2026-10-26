Shader "PitCrew/LofiPilot/Surface"
{
 Properties {
  _MainTex("Painted pixel atlas",2D)="white" {}
  _Tint("Tint",Color)=(1,1,1,1)
  _FaceShade("Broad face shading",Range(0,1))=0.28
  _Ambient("Ambient tone",Color)=(0.93,0.93,0.88,1)
  _FogColor("Maritime haze",Color)=(0.54,0.65,0.69,1)
  _FogStart("Fog start",Float)=30
  _FogEnd("Fog end",Float)=105
 }
 SubShader {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Assets/Art/Look/PCLook.hlsl"   // 과제 1548: 그림체 3안(_PCLook 0 = 이전과 같음)
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
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
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.pos));
    float diffuse=saturate(dot(normalize(i.normal),sun.direction));
    // D-101 v3: 해 색·세기가 면에 실린다 (전엔 0.55~1.0 고정 명암이라 해를 올려도 "햇빛"이 안 생겼다). 그늘 바닥 = _Ambient*0.42 (하늘빛), 양지 = + sun*0.75
    float3 lit=_Ambient.rgb*0.36+sun.color.rgb*(diffuse*sun.shadowAttenuation)*1.0;   // v4: 양지 1.0·그늘 0.36 → 차이 2배 ("쨍쨍")
    float3 shade=PCShade(_Ambient.rgb,sun.color.rgb,diffuse,sun.shadowAttenuation,_FaceShade);
    half3 col=PCTex(TEXTURE2D_ARGS(_MainTex,sampler_MainTex),i.uv,_MainTex_TexelSize)*_Tint.rgb*shade; col=PCPost(col,i.pos,normalize(i.normal));
    return half4(PCFog(col,i.pos,_FogColor.rgb,_FogStart,_FogEnd),1);
   }
   ENDHLSL
  }
  Pass {
   Name "ShadowCaster"
   Tags {"LightMode"="ShadowCaster"}
   ZWrite On ZTest LEqual ColorMask 0 Cull Back
   HLSLPROGRAM
   #pragma vertex shadowVert
   #pragma fragment shadowFrag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
   float3 _LightDirection;
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   float4 shadowVert(A v):SV_POSITION {
    float3 p=TransformObjectToWorld(v.positionOS.xyz);
    float4 c=TransformWorldToHClip(ApplyShadowBias(p,TransformObjectToWorldNormal(v.normalOS),_LightDirection));
    #if UNITY_REVERSED_Z
     c.z=min(c.z,UNITY_NEAR_CLIP_VALUE);
    #else
     c.z=max(c.z,UNITY_NEAR_CLIP_VALUE);
    #endif
    return c;
   }
   half4 shadowFrag():SV_Target{return 0;}
   ENDHLSL
  }
 }
}
