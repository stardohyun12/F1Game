// 과제 20261104-1449(D-297 B안): 법선·깊이 RT 로 1px 어두운 윤곽선. 깊이 끊김·꺾임 모두 앞(가까운) 쪽 픽셀만 칠함 → 선 굵기 1px.
// 흐리지 않는다 — 선 픽셀은 한 색(바탕 × _Dark 와 먹색을 섞음), 나머지는 원본 그대로.
Shader "Hidden/PitCrew/RoomOutlineEdge"
{
    Properties { _MainTex ("", 2D) = "white" {} }
    SubShader
    {
        ZTest Always Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; sampler2D _RoomND; float4 _RoomNDTexel;
            float4 _RoomEdge;   // x 깊이 끊김(비율) · y 꺾임 dot 문턱 · z 선 = 바탕 × · w 먹색 섞기
            float4 _RoomInk;
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(appdata_img i) { V o; o.pos = UnityObjectToClipPos(i.vertex); o.uv = i.texcoord; return o; }
            float4 nd(float2 uv) { return tex2Dlod(_RoomND, float4(uv, 0, 0)); }
            float4 frag(V i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float4 p = nd(i.uv);
                if (p.w <= 0) return c;   // 집이 아닌 곳(창밖)
                float2 t = _RoomNDTexel.xy;
                float2 o4[4] = { float2(t.x, 0), float2(-t.x, 0), float2(0, t.y), float2(0, -t.y) };
                float3 np = p.xyz * 2 - 1;
                bool edge = false;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float4 q = nd(i.uv + o4[k]);
                    float qz = q.w <= 0 ? 1e4 : q.w;
                    if (qz - p.w > _RoomEdge.x * p.w) edge = true;                         // 내가 앞 — 실루엣
                    // 꺾임: 비스듬한 벽은 옆 픽셀 거리가 몇 % 씩 벌어지므로 문턱을 따로(25%) 둔다. 가까운 쪽 픽셀만 → 끊기지 않는 1px
                    if (q.w > 0 && abs(qz - p.w) <= 0.25 * p.w && p.w <= qz && dot(np, q.xyz * 2 - 1) < _RoomEdge.y) edge = true;
                }
                if (!edge) return c;
                float3 e = lerp(c.rgb * _RoomEdge.z, _RoomInk.rgb, _RoomEdge.w);
                return float4(min(e, c.rgb * 0.7), c.a);   // 선은 언제나 바탕보다 어둡게(어두운 문 위에서 밝은 선 방지)
            }
            ENDHLSL
        }
    }
}
