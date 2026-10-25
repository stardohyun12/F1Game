// 과제 0404: BayLoop 언덕 풀잎. Proto WindGrass(과제 1827)의 바람 물결을 그대로 쓰되, 색은 땅(TerrainSlope)과 같은 규칙으로 바꿨다.
//  - 풀잎 색 = 뿌리 자리 땅 아틀라스 풀 칸의 텍셀 하나(uv = 뿌리 xz). 땅과 같은 32px/m 격자 → 풀잎이 땅에서 자란 것처럼 같은 색 점.
//  - 끝 밝기·물결 마루·구름 그림자 전부 계단(2~3단) — 부드러운 그라데이션 없이 픽셀 규칙 유지.
//  - 흙 패치 위 뿌리는 버린다(TerrainSlope 와 같은 패치 노이즈).
// Proto 셰이더·머티리얼은 건드리지 않는다(Proto_CoastCourse·PostcardSlice 보호).
Shader "PitCrew/BayLoop/WindGrass"
{
 Properties {
  _MainTex("Terrain atlas (same as TerrainSlope)",2D)="white" {}
  _TileGrass("Grass tile",Vector)=(0,0.5,0.5,0.5)
  _WorldScale("Tile repeats per metre",Float)=0.5376344
  _SoilAmount("Soil patches (0=off)",Range(0,1))=1
  _PatchScale("Patch cells per metre",Float)=0.07
  _PatchThreshold("Patch threshold",Range(0,1))=0.70
  _Tint("Tint",Color)=(1,1,1,1)
  _TipLift("Tip brightness step",Range(1,1.5))=1.14
  _FaceShade("Broad face shading",Range(0,1))=0.28
  _Ambient("Ambient tone",Color)=(0.93,0.93,0.88,1)
  _FogColor("Haze",Color)=(0.54,0.65,0.69,1)
  _FogStart("Fog start",Float)=30
  _FogEnd("Fog end",Float)=105
  _WindDir("Wind dir (xz)",Vector)=(0.8,0,-0.6,0)
  _WaveLen("Wave length m",Float)=14
  _WaveSpeed("Wave speed m/s",Float)=4
  _Sway("Sway m",Float)=0.30
  _Band("Crest brightness step",Range(0,0.6))=0.10
  _CloudShadow("Cloud shadow strength",Range(0,1))=0.24
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
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST, _TileGrass, _Tint, _Ambient, _FogColor, _WindDir;
    float _WorldScale, _SoilAmount, _PatchScale, _PatchThreshold, _TipLift, _FaceShade, _FogStart, _FogEnd, _WaveLen, _WaveSpeed, _Sway, _Band, _CloudShadow, _CloudScale;
   CBUFFER_END
   float4 _CloudOffset;
   float hash21(float2 p){p=frac(p*float2(123.34,345.45));p+=dot(p,p+34.345);return frac(p.x*p.y);}
   float vnoise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);float a=hash21(i),b=hash21(i+float2(1,0)),c=hash21(i+float2(0,1)),d=hash21(i+1);return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);}
   // TerrainSlope 와 같은 구름 그림자: 1/32m 격자로 잘라 0 / 0.5 / 1 세 단
   float CloudStep(float2 xz){float2 q=(floor(xz*32)/32+_CloudOffset.xy)/_CloudScale;float c=vnoise(q)*.75+vnoise(q*2.7)*.25;return floor(saturate((c-.52)/.16)*2+.5)*.5;}
   struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION; float3 pos:TEXCOORD0; float2 root:TEXCOORD1; float4 color:TEXCOORD2; float wave:TEXCOORD3;};
   float Wave(float2 xz) {
    float2 d=normalize(_WindDir.xz+1e-4);
    float a=dot(xz,d)*6.2831/_WaveLen-_Time.y*_WaveSpeed*6.2831/_WaveLen;
    float b=dot(xz,float2(-d.y,d.x))*6.2831/(_WaveLen*2.7)+_Time.y*0.35;
    return sin(a)*0.7+sin(a*0.43+b)*0.3;
   }
   Varyings vert(Attributes v) {
    Varyings o; float3 p=TransformObjectToWorld(v.positionOS.xyz);
    float w=Wave(v.uv); float k=v.color.a;
    float2 d=normalize(_WindDir.xz+1e-4);
    p.xz+=d*(w*0.5+0.5)*_Sway*k; p.y-=(w*0.5+0.5)*_Sway*0.35*k;
    o.pos=p; o.positionCS=TransformWorldToHClip(p); o.root=v.uv; o.color=v.color; o.wave=w; return o;
   }
   half4 frag(Varyings i):SV_Target {
    float2 q=floor(i.root*32)/32*_PatchScale;
    float nz=vnoise(q)*.62+vnoise(q*2.6+float2(11.3,7.1))*.38;
    clip(_SoilAmount>0 && nz>_PatchThreshold ? -1 : 1);                 // 흙 패치 위엔 풀잎 없음
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.pos));
    float diffuse=saturate(sun.direction.y);                              // 땅 평지와 같은 빛(위를 향한 면)
    float3 lit=_Ambient.rgb*0.36+sun.color.rgb*(diffuse*sun.shadowAttenuation);
    float3 shade=lerp(float3(1,1,1),lit,_FaceShade);
    float2 uv=_TileGrass.xy+(.035+frac(i.root*_WorldScale)*.93)*_TileGrass.zw;
    float3 col=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).rgb*i.color.rgb;
    col*=i.color.a>.55?_TipLift:1;                                        // 끝 한 단 밝게
    col*=1+_Band*step(.35,i.wave);                                        // 물결 마루 한 단 밝게
    col*=_Tint.rgb*shade*(1-_CloudShadow*CloudStep(i.root));
    float fog=saturate((distance(i.pos,_WorldSpaceCameraPos)-_FogStart)/max(1,_FogEnd-_FogStart));
    return half4(lerp(col,_FogColor.rgb,fog),1);
   }
   ENDHLSL
  }
 }
}
