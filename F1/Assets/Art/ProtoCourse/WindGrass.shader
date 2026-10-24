// 과제 1827: 언덕 위 들판. 바람 물결 = 큰 파장(기본 14m) 사인 두 겹이 바람 방향으로 흘러간다.
// 정점 색 a = 흔들림 가중(풀잎 끝 1, 뿌리·땅 0). 물결 마루는 밝게 → 960 해상도에서도 "덩어리로 넘실"이 보인다.
// 구름 그림자: 전역 _CloudOffset(CloudDrift 가 매 프레임 넣음)만큼 흘러가는 값 노이즈로 어둡게.
// 월드 셰이더와 같은 _Ambient/_FogColor/_FogStart/_FogEnd/_Tint/_FaceShade 를 가져 무드(MoodController)가 그대로 구워진다.
Shader "PitCrew/Proto/WindGrass"
{
 Properties {
  _Tint("Root color",Color)=(0.30,0.46,0.22,1)
  _TipColor("Tip color",Color)=(0.62,0.74,0.38,1)
  _FaceShade("Broad face shading",Range(0,1))=0.5
  _Ambient("Ambient tone",Color)=(0.93,0.93,0.88,1)
  _FogColor("Haze",Color)=(0.54,0.65,0.69,1)
  _FogStart("Fog start",Float)=30
  _FogEnd("Fog end",Float)=105
  _WindDir("Wind dir (xz)",Vector)=(0.8,0,-0.6,0)
  _WaveLen("Wave length m",Float)=14
  _WaveSpeed("Wave speed m/s",Float)=4
  _Sway("Sway m",Float)=0.35
  _Band("Crest brightness",Range(0,0.6))=0.22
  _CloudShadow("Cloud shadow strength",Range(0,1))=0.32
  _CloudScale("Cloud shadow size m",Float)=70
 }
 SubShader {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
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
   CBUFFER_START(UnityPerMaterial)
    float4 _Tint, _TipColor, _Ambient, _FogColor, _WindDir;
    float _FaceShade, _FogStart, _FogEnd, _WaveLen, _WaveSpeed, _Sway, _Band, _CloudShadow, _CloudScale;
   CBUFFER_END
   float4 _CloudOffset;
   struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION; float3 pos:TEXCOORD0; float3 normal:TEXCOORD1; float4 color:TEXCOORD2; float wave:TEXCOORD3;};
   float Wave(float2 xz) {
    float2 d=normalize(_WindDir.xz+1e-4);
    float a=dot(xz,d)*6.2831/_WaveLen-_Time.y*_WaveSpeed*6.2831/_WaveLen;
    float b=dot(xz,float2(-d.y,d.x))*6.2831/(_WaveLen*2.7)+_Time.y*0.35;
    return sin(a)*0.7+sin(a*0.43+b)*0.3;   // -1..1, 큰 덩어리
   }
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
   Varyings vert(Attributes v) {
    Varyings o; float3 p=TransformObjectToWorld(v.positionOS.xyz);
    float w=Wave(p.xz); float k=v.color.a;
    float2 d=normalize(_WindDir.xz+1e-4);
    p.xz+=d*(w*0.5+0.5)*_Sway*k; p.y-=(w*0.5+0.5)*_Sway*0.35*k;
    o.pos=p; o.positionCS=TransformWorldToHClip(p);
    o.normal=TransformObjectToWorldNormal(v.normalOS); o.color=v.color; o.wave=w; return o;
   }
   half4 frag(Varyings i):SV_Target {
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.pos));
    float diffuse=saturate(dot(normalize(i.normal),sun.direction));
    float3 lit=_Ambient.rgb*0.36+sun.color.rgb*(diffuse*sun.shadowAttenuation);
    float3 shade=lerp(float3(1,1,1),lit,_FaceShade);
    float3 col=lerp(_Tint.rgb,_TipColor.rgb,i.color.a)*i.color.rgb;
    col*=1+_Band*saturate(i.wave);                                  // 물결 마루 밝게
    float c=Noise((i.pos.xz+_CloudOffset.xy)/_CloudScale);
    c=smoothstep(0.55,0.72,c+Noise((i.pos.xz+_CloudOffset.xy)/(_CloudScale*0.37))*0.25-0.12);
    col*=shade*(1-_CloudShadow*c);
    float fog=saturate((distance(i.pos,_WorldSpaceCameraPos)-_FogStart)/max(1,_FogEnd-_FogStart));
    return half4(lerp(col,_FogColor.rgb,fog),1);
   }
   ENDHLSL
  }
 }
}
