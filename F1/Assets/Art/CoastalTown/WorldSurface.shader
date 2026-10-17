Shader "PitCrew/CoastalTown/WorldSurface"
{
 Properties {
  _MainTex("Painted pixel atlas",2D)="white" {}
  _Tile("Atlas tile",Vector)=(0,0,0.125,0.125)
  _WorldScale("Tile repeats per metre",Float)=0.5
  _Tint("Tint",Color)=(1,1,1,1)
  _FaceShade("Broad face shading",Range(0,1))=0.28
  _Ambient("Ambient tone",Color)=(0.93,0.93,0.88,1)
  _FogColor("Maritime haze",Color)=(0.54,0.65,0.69,1)
  _FogStart("Fog start",Float)=30
  _FogEnd("Fog end",Float)=105
  // 2026-10-13 Claude 3차: 넓은 바닥(잔디·흙·모래)에 두 번째 칸을 큰 덩어리 무늬로 섞는다. _Patch=0 이면 이전과 동일.
  _Tile2("Patch atlas tile",Vector)=(0,0,0.125,0.125)
  _Patch("Patch amount (0=off)",Range(0,1))=0
  _PatchScale("Patch cells per metre",Float)=0.09
  _PatchThreshold("Patch threshold",Range(0,1))=0.55
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
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST, _Tint, _Ambient, _FogColor;
    float _FaceShade, _FogStart, _FogEnd, _WorldScale; float4 _Tile;
    float4 _Tile2; float _Patch, _PatchScale, _PatchThreshold;
   CBUFFER_END
   float hash21(float2 p){p=frac(p*float2(123.34,345.45));p+=dot(p,p+34.345);return frac(p.x*p.y);}
   float vnoise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);float a=hash21(i),b=hash21(i+float2(1,0)),c=hash21(i+float2(0,1)),d=hash21(i+1);return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);}
   struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 pos:TEXCOORD2;};
   Varyings vert(Attributes v) {
    Varyings o;o.pos=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.pos);
    o.uv=TRANSFORM_TEX(v.uv,_MainTex);
    o.normal=TransformObjectToWorldNormal(v.normalOS);return o;
   }
   half4 frag(Varyings i):SV_Target {
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.pos));
    float diffuse=saturate(dot(normalize(i.normal),sun.direction));
    // D-101 v3: 해 색·세기가 면에 실린다 (전엔 0.55~1.0 고정 명암이라 해를 올려도 "햇빛"이 안 생겼다). 그늘 바닥 = _Ambient*0.42 (하늘빛), 양지 = + sun*0.75
    float3 lit=_Ambient.rgb*0.36+sun.color.rgb*(diffuse*sun.shadowAttenuation)*1.0;   // v4: 양지 1.0·그늘 0.36 → 차이 2배 ("쨍쨍")
    float3 shade=lerp(float3(1,1,1),lit,_FaceShade);
    float3 an=abs(normalize(i.normal)); float2 worldUV=an.y>.5?i.pos.xz:(an.x>an.z?i.pos.zy:i.pos.xy); float2 tileUV=_Tile.xy+(.035+frac(worldUV*_WorldScale)*.93)*_Tile.zw;
    half3 col=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,tileUV).rgb;
    if(_Patch>0){
     float2 q=i.pos.xz*_PatchScale; float n=vnoise(q)*.62+vnoise(q*2.6+float2(11.3,7.1))*.38;   // 큰 덩어리 + 작은 덩어리
     float mask=step(_PatchThreshold,n)*_Patch;                                                  // 계단식 경계 = 픽셀아트 결
     float2 tileUV2=_Tile2.xy+(.035+frac(worldUV*_WorldScale)*.93)*_Tile2.zw;
     col=lerp(col,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,tileUV2).rgb,mask);
    }
    col*=_Tint.rgb*shade;
    float fog=saturate((distance(i.pos,_WorldSpaceCameraPos)-_FogStart)/max(1,_FogEnd-_FogStart));
    return half4(lerp(col,_FogColor.rgb,fog),1);
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

