using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.PackBench
{
    public enum PackItemKind { Eggs, Paper, Bubble }
    // PackSupply·PackSupplyKind 는 본체(Packing/PackSupply.cs) — 씬이 「PitCrew 어셈블리의 PackSupply」로 찾는다(D-287 6단계).

    /// <summary>
    /// 포장 작업대(D-250 ②)에서 손으로 집어 상자에 넣는 물건 하나 — 달걀 한 판·구긴 신문지·뽁뽁이 한 장.
    /// 간단한 리지드바디. 상자를 들고 흔드는 동안 부딪힌 세기를 상자(PackBox.ReportHit)에 알린다 → 덜그럭 소리 크기.
    /// </summary>
    /// 뽁뽁이 시트(BubbleSheet)는 리지드바디가 없다 — 굽힌 칸 콜라이더만(상자 안이면 상자 몸의 일부). 그래서 RequireComponent 없음.
    public class PackItem : MonoBehaviour
    {
        public PackItemKind kind;
        /// <summary>뽁뽁이 시트면 그 천(2판). 아니면 null.</summary>
        [System.NonSerialized] public BubbleSheet sheet;
        /// <summary>뽁뽁이로 감쌌나(2판 — 시트 위에 놓고 E).</summary>
        public bool Wrapped { get; private set; }
        Rigidbody body;
        public Rigidbody Body => body != null ? body : (body = GetComponent<Rigidbody>());

        /// <summary>소리 무게 — 달걀판(딱딱·무거움) 1, 감싼 달걀판 0.35, 뽁뽁이 0.25, 신문지 0.15 (가설).</summary>
        public float SoundWeight => kind == PackItemKind.Eggs ? (Wrapped ? 0.35f : 1f) : kind == PackItemKind.Bubble ? 0.25f : 0.15f;

        float lastTouch;

        /// <summary>
        /// 연속 충돌(ContinuousSpeculative)은 아직 안 닿았는데 "곧 닿을 것"을 미리 접촉으로 알린다(떨어진 거리 = 양수 separation).
        /// 1판 '바닥만 깐 상자가 맨 상자보다 시끄러움(95 vs 39)'의 원인: 판이 뽁뽁이 2 cm 위에서 튀는데 그 아래 상자 바닥(Floor)과의
        /// 미리 접촉이 50번 덜그럭으로 세어졌다. → 떨어진 접촉은 보류했다가 실제로 닿는 순간(Stay 에서 거리 ≤ SpecSep) 센다.
        /// </summary>
        public static bool FilterSpeculative = true;
        public static int SpecDeferred, SpecLanded;
        const float SpecSep = 0.002f;
        readonly HashSet<Collider> pendingSpec = new HashSet<Collider>();

        static float MinSeparation(Collision c)
        {
            float m = float.MaxValue;
            for (int i = 0; i < c.contactCount; i++) m = Mathf.Min(m, c.GetContact(i).separation);
            return c.contactCount > 0 ? m : 0f;
        }

        void OnCollisionEnter(Collision c)
        {
            if (FilterSpeculative && MinSeparation(c) > SpecSep) { pendingSpec.Add(c.collider); SpecDeferred++; return; }
            Hit(c);
        }

        void OnCollisionStay(Collision c)
        {
            if (pendingSpec.Count == 0 || !pendingSpec.Contains(c.collider)) return;
            if (MinSeparation(c) > SpecSep) return;
            pendingSpec.Remove(c.collider); SpecLanded++;
            Hit(c);
        }

        void OnCollisionExit(Collision c) { if (pendingSpec.Count > 0) pendingSpec.Remove(c.collider); }

        void Hit(Collision c)
        {
            var bench = PackBench.I;
            var box = bench != null ? bench.Shaking : null;
            var other = c.collider != null ? c.collider.GetComponentInParent<PackItem>() : null;
            if (box == null || !box.HasContent(this))
            {
                // 흔드는 중이 아니면: 내려놓을 때·벽에 닿을 때 살짝 툭(2판)
                float v = c.relativeVelocity.magnitude;
                if (v > 0.12f && Time.time - lastTouch > 0.12f) { lastTouch = Time.time; PackBench.PlayTouch(this, other, v); }
                return;
            }
            // 물건끼리 부딪힘은 양쪽이 다 받는다 → id 작은 쪽만 센다
            if (other != null && other.GetInstanceID() < GetInstanceID()) return;
            PackBox.lastHitCollider = c.collider;
            box.ReportHit(this, other, c.relativeVelocity.magnitude);
        }

        /// <summary>뽁뽁이로 감싼다 — 충돌 상자를 사방 thick 만큼 키우고 겉에 뽁뽁이 껍질(보기)을 씌운다.</summary>
        public void Wrap(Material mat, float thick)
        {
            if (Wrapped) return;
            var bc = GetComponent<BoxCollider>();
            if (bc == null) return;
            Wrapped = true;
            Vector3 ls = transform.lossyScale;
            bc.size += new Vector3(2f * thick / ls.x, 2f * thick / ls.y, 2f * thick / ls.z);
            var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "Wrap";
            Destroy(shell.GetComponent<Collider>());
            shell.transform.SetParent(transform, false);
            shell.transform.localPosition = bc.center;
            shell.transform.localScale = bc.size;
            if (mat != null) shell.GetComponent<Renderer>().sharedMaterial = mat;
            if (Body != null) Body.mass += 0.05f;
        }
    }
}
