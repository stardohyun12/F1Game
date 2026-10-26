Shader "PitCrew/BayLoop/TerrainSlope"
{
 // 과제 0226: BayLoop 땅 전용. WorldSurface(CoastalTown) 와 같은 빛·안개 식에, 아틀라스 네 칸을 경사로 고른다.
 //  평지 = 풀 + 흙 패치(큰 덩어리 무늬) / _DryDeg 넘으면 마른 풀 / _RockDeg 넘으면 바위.
 //  경계는 계단식(step, 1/32m 텍셀) + 저주파 흔들림(1448) — 부드러운 섞임 없이 픽셀 규칙 유지. 기존 WorldSurface 는 건드리지 않는다(Proto·ComplexTrack 보호).
 Properties {
  _MainTex("Terrain atlas (2x2: grass soil / dry rock)",2D)="white" {}
  _TileGrass("Grass tile",Vector)=(0,0.5,0.5,0.5)
  _TileSoil("Soil tile",Vector)=(0.5,0.5,0.5,0.5)
  _TileDry("Dry grass tile",Vector)=(0,0,0.5,0.5)
  _TileRock("Rock tile",Vector)=(0.5,0,0.5,0.5)
  _WorldScale("Tile repeats per metre",Float)=0.5376344
  _DryDeg("Dry grass from (deg)",Range(0,90))=22
  _RockDeg("Rock from (deg)",Range(0,90))=38
  _EdgeJitter("Boundary jitter (deg)",Range(0,15))=5
  _SoilAmount("Soil patches (0=off)",Range(0,1))=1
  _PatchScale("Patch cells per metre",Float)=0.07
  _PatchThreshold("Patch threshold",Range(0,1))=0.62
  _Tint("Tint",Color)=(1,1,1,1)
  _FaceShade("Broad face shading",Range(0,1))=0.28
  _Ambient("Ambient tone",Color)=(0.93,0.93,0.88,1)
  _FogColor("Maritime haze",Color)=(0.54,0.65,0.69,1)
  _FogStart("Fog start",Float)=30
  _FogEnd("Fog end",Float)=105
  _CloudShadow("Cloud shadow strength (0404)",Range(0,1))=0.24
  _CloudScale("Cloud shadow size m",Float)=70
  _SlopeTex("Smoothed slope deg/90 (1448, builder bakes)",2D)="black" {}
  _SlopeRect("Slope rect x0 z0 1/w 1/d (1448)",Vector)=(0,0,0,0)
  _JitterScale("Jitter wave size m (1448)",Float)=9
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
    float _FaceShade, _FogStart, _FogEnd, _WorldScale;
    float4 _TileGrass, _TileSoil, _TileDry, _TileRock;
    float _DryDeg, _RockDeg, _EdgeJitter, _SoilAmount, _PatchScale, _PatchThreshold;
    float _CloudShadow, _CloudScale;
    float4 _SlopeRect; float _JitterScale;
   CBUFFER_END
   TEXTURE2D(_SlopeTex); SAMPLER(sampler_SlopeTex);
   float4 _CloudOffset;   // 과제 0404: CloudDrift 가 매 프레임 넣는 전역 — 구름 그림자가 바람 따라 흐른다
   float hash21(float2 p){p=frac(p*float2(123.34,345.45));p+=dot(p,p+34.345);return frac(p.x*p.y);}
   float vnoise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);float a=hash21(i),b=hash21(i+float2(1,0)),c=hash21(i+float2(0,1)),d=hash21(i+1);return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);}
   // 과제 0404: 구름 그림자. 1/32m 격자로 자르고 0 / 0.5 / 1 세 단 — BL_WindGrass 와 같은 식(풀잎과 땅이 같은 그림자를 받는다)
   float CloudStep(float2 xz){float2 q=(floor(xz*32)/32+_CloudOffset.xy)/_CloudScale;float c=vnoise(q)*.75+vnoise(q*2.7)*.25;return floor(saturate((c-.52)/.16)*2+.5)*.5;}
   struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 normal:TEXCOORD1;float3 pos:TEXCOORD2;};
   Varyings vert(Attributes v) {
    Varyings o;o.pos=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.pos);
    o.normal=TransformObjectToWorldNormal(v.normalOS);return o;
   }
   half3 Tile(float4 t, float2 worldUV){ return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,t.xy+(.035+frac(worldUV*_WorldScale)*.93)*t.zw).rgb; }
   half4 frag(Varyings i):SV_Target {
    float3 n=normalize(i.normal);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.pos));
    float diffuse=saturate(dot(n,sun.direction));
    float3 lit=_Ambient.rgb*0.36+sun.color.rgb*(diffuse*sun.shadowAttenuation)*1.0;   // WorldSurface v4 와 같은 식
    float3 shade=lerp(float3(1,1,1),lit,_FaceShade);
    // 과제 1448: 경사는 빌더가 높이맵에서 구워 둔 텍스처(2m 격자, 한 번 다듬음)를 텍셀(1/32m) 자리에서 읽는다.
    //  예전엔 면 법선 보간이라 2m 삼각형마다 경계가 꺾여 큰 톱니가 됐다. 이제 경계는 등고선을 따라 휘고 화면에선 1/32m 계단.
    float2 tx=floor(i.pos.xz*32)/32+1.0/64;
    float2 suv=(tx-_SlopeRect.xy)*_SlopeRect.zw;
    bool inRect=_SlopeRect.z>0&&all(suv>0)&&all(suv<1);
    float deg=inRect?SAMPLE_TEXTURE2D(_SlopeTex,sampler_SlopeTex,suv).r*90:degrees(acos(saturate(n.y)));
    // 흔들림 = 2m 칸 해시 → 9m 물결(저주파, 같은 텍셀 격자)
    float j=(vnoise(tx/_JitterScale+float2(3.7,1.3))-.5)*2*_EdgeJitter;
    float d=deg+j;
    // 투영 면도 다듬은 경사로 고른다(삼각형마다 xz/측면 투영이 바뀌던 이음매 제거)
    float3 an=abs(n); float2 worldUV=(inRect?deg<60:an.y>.5)?i.pos.xz:(an.x>an.z?i.pos.zy:i.pos.xy);
    half3 col;
    if(d>=_RockDeg) col=Tile(_TileRock,worldUV);
    else if(d>=_DryDeg) col=Tile(_TileDry,worldUV);
    else {
     col=Tile(_TileGrass,worldUV);
     float2 q=floor(i.pos.xz*32)/32*_PatchScale;   // 패치 경계를 텍셀(1/32m) 격자에 맞춘다 — 매끈한 곡선 대신 픽셀 계단
     float nz=vnoise(q)*.62+vnoise(q*2.6+float2(11.3,7.1))*.38;
     if(_SoilAmount>0 && nz>_PatchThreshold) col=Tile(_TileSoil,worldUV);
    }
    col*=_Tint.rgb*shade*(1-_CloudShadow*CloudStep(i.pos.xz));
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
