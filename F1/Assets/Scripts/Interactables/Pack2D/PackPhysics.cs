using UnityEngine;

/// <summary>
/// D-295 포장 2D ③: 상자 안 물리. 유니티 물리 엔진을 쓰지 않는 작은 풀이기 — 판(수평면) 위에서 내려다본 그릇 안의 물건 하나가 미끄러지고 벽에 부딪힌다.
/// 에디트 모드에서도 같은 값이 나와야 해서(불변 검사가 돌린다) 고정 간격(1/120 s)·순수 C# 으로 짰다. 씬 콜라이더·레이어와 안 엮인다.
/// 모델: 상자 틀에서 본다 — 상자가 a 로 가속하면 물건은 −a 를 받는다. 마찰(쿨롱, 정지 마찰 포함) · 벽 반발 · 완충재가 틈을 메움(두께 합만큼 자유 틈이 준다).
/// 덜그럭 값 = 최근 1.5 s 동안 물건이 상자 기준 움직인 거리의 합(m) 의 최댓값(PeakWindow). 틈이 0 이면 0.
/// </summary>
public sealed class PackPhysics
{
    public struct Mat
    {
        public float mass, bounce, friction;
        public Mat(float m, float b, float f) { mass = m; bounce = b; friction = f; }
    }

    /// <summary>높이(z) 방향 낙하 하나 — 놓는 순간부터 떨어져 자리 잡는다.</summary>
    public struct Fall
    {
        public float z, vz, bounce;
        public bool Active => z > 0f || vz != 0f;
        public void Drop(float height, float e) { z = height; vz = 0f; bounce = e; }
        /// <summary>한 걸음. 땅에 닿은 이번 걸음의 속도(없으면 0).</summary>
        public float Step(float dt)
        {
            if (!Active) return 0f;
            vz -= G * dt; z += vz * dt;
            if (z > 0f) return 0f;
            float hit = -vz; z = 0f;
            vz = hit * bounce > 0.25f ? hit * bounce : 0f;
            if (vz > 0f) z = vz * dt;
            return hit;
        }
    }

    public const float G = 9.8f, H = 1f / 120f, Window = 1.5f;
    /// <summary>표준 흔들기(길 시험): 진폭 4 cm · 3 Hz → 가속 14 m/s². 사람이 흔드는 힘과 같은 자릿수의 가설.</summary>
    public const float StdAmp = 0.04f, StdHz = 3f, StdSecs = 2.2f;
    /// <summary>이보다 센 상자 가속(m/s²)으로 움직인 시간만 "흔든 시간"으로 센다.</summary>
    public const float ShakeAccMin = 6f;
    /// <summary>이보다 약한 부딪힘(m/s)은 소리·세기에서 뺀다(벽에 기대 서 있는 것).</summary>
    public const float HitMin = 0.15f;

    // ── 설정 ──
    public Vector2 inner, part;        // 상자 안(u,v) · 물건 발자국(u,v), m
    public float pad;                  // 완충재가 메우는 두께 합(m)
    public float mu, e, mass;          // 마찰 계수 · 벽 반발 · 물건 질량(kg)
    public float wrapK = 1f;           // D-296 겹 계수: 감싼 물건은 부딪힘 세기(소리·손상)가 이만큼으로. 반발 e 에는 이미 곱해 들어 있다

    // ── 상태 ──
    public Vector2 pos, vel;           // 상자 가운데 기준 물건 자리·속도

    // ── 측정 ──
    public float Moved { get; private set; }           // 지금까지 움직인 거리 합
    public float PeakWindow { get; private set; }      // 최근 1.5 s 합의 최댓값 = 덜그럭 값
    public float ShakeTime { get; private set; }       // 상자가 ShakeAccMin 이상으로 가속한 시간
    public float SimTime { get; private set; }
    public int Hits { get; private set; }
    /// <summary>마지막으로 읽은 뒤 가장 센 부딪힘 = 속도 × 질량(kg·m/s). 읽으면 0 으로.</summary>
    public float TakeImpact() { float i = impact; impact = 0f; return i; }

    float impact, acc, windowSum;
    readonly float[] ring = new float[(int)(Window / H + 0.5f)];
    int ringAt;

    /// <summary>한 축이 움직일 수 있는 절반 폭(m). 0 이면 물건이 꽉 끼었다.</summary>
    public Vector2 Limit => new Vector2(Mathf.Max(0f, inner.x - part.x - pad) * 0.5f, Mathf.Max(0f, inner.y - part.y - pad) * 0.5f);

    public void Reset(Vector2 at, Vector2 v)
    {
        var l = Limit;
        pos = new Vector2(Mathf.Clamp(at.x, -l.x, l.x), Mathf.Clamp(at.y, -l.y, l.y)); vel = v;
        Moved = PeakWindow = ShakeTime = SimTime = windowSum = impact = 0f; Hits = 0; ringAt = 0; acc = 0f;
        System.Array.Clear(ring, 0, ring.Length);
    }

    /// <summary>자리·속도는 그대로 두고 측정만 처음부터(상자를 닫은 순간부터 잰다).</summary>
    public void ResetMeasure()
    {
        Moved = PeakWindow = ShakeTime = windowSum = impact = 0f; Hits = 0; ringAt = 0;
        System.Array.Clear(ring, 0, ring.Length);
    }

    public PackPhysics Copy() => new PackPhysics { inner = inner, part = part, pad = pad, mu = mu, e = e, mass = mass, wrapK = wrapK };

    /// <summary>boxAcc(상자의 가속, m/s²)가 dt 동안 일정하다고 보고 나아간다. 프레임이 얼마든 고정 간격으로 쪼개서 같은 값이 나온다.</summary>
    public void Advance(float dt, Vector2 boxAcc)
    {
        acc += dt;
        while (acc >= H - 1e-6f) { acc -= H; Sub(boxAcc); }
    }

    void Sub(Vector2 boxAcc)
    {
        SimTime += H;
        if (boxAcc.magnitude > ShakeAccMin) ShakeTime += H;
        vel += -boxAcc * H;                                          // 상자 틀에서 본 관성력
        float dv = mu * G * H, sp = vel.magnitude;                   // 마찰: 속도를 깎되 0 을 넘어 뒤집지 않는다(= 정지 마찰)
        vel = sp <= dv ? Vector2.zero : vel * ((sp - dv) / sp);
        var p0 = pos; var l = Limit;
        pos += vel * H;
        pos.x = Wall(pos.x, l.x, ref vel.x); pos.y = Wall(pos.y, l.y, ref vel.y);
        float step = (pos - p0).magnitude;
        windowSum += step - ring[ringAt]; ring[ringAt] = step; ringAt = (ringAt + 1) % ring.Length;
        Moved += step;
        if (windowSum > PeakWindow) PeakWindow = windowSum;
    }

    float Wall(float x, float lim, ref float v)
    {
        if (lim <= 0f) { v = 0f; return 0f; }                        // 꽉 끼었다: 갈 데가 없으니 부딪히지도 소리 나지도 않는다
        if (x > lim || x < -lim)
        {
            float s = x > 0f ? 1f : -1f, hit = v * s;
            if (hit > 0f)
            {
                if (hit > HitMin) { Hits++; float im = hit * mass * wrapK; if (im > impact) impact = im; }
                v = hit * e > 0.05f ? -hit * e * s : 0f;
            }
            return lim * s;
        }
        return x;
    }

    /// <summary>표준 흔들기: 이 설정 그대로 복사해 StdSecs 동안 흔들고 덜그럭 값(m)을 돌려준다. 이 상자의 상태(pos·측정)는 건드리지 않는다.</summary>
    public float MeasureStandard() => MeasureStandard(out _);

    /// <summary>hits = 그동안 벽에 부딪힌 횟수(소리가 나는 것).</summary>
    public float MeasureStandard(out int hits)
    {
        var c = Copy(); c.Reset(Vector2.zero, Vector2.zero);
        float w = 2f * Mathf.PI * StdHz;
        for (int i = 0; i * H < StdSecs; i++) c.Advance(H, new Vector2(-StdAmp * w * w * Mathf.Sin(w * i * H), 0f));
        hits = c.Hits;
        return c.PeakWindow;
    }
}
