using UnityEngine;

namespace PitCrew.PackBench
{
    public enum PackItemKind { Eggs, Paper, Bubble }
    public enum PackSupplyKind { Newspaper, Bubble, Tape, Label }

    /// <summary>
    /// 포장 작업대(D-250 ②)에서 손으로 집어 상자에 넣는 물건 하나 — 달걀 한 판·구긴 신문지·뽁뽁이 한 장.
    /// 간단한 리지드바디. 상자를 들고 흔드는 동안 부딪힌 세기를 상자(PackBox.ReportHit)에 알린다 → 덜그럭 소리 크기.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PackItem : MonoBehaviour
    {
        public PackItemKind kind;
        Rigidbody body;
        public Rigidbody Body => body != null ? body : (body = GetComponent<Rigidbody>());

        /// <summary>소리 무게 — 달걀판(딱딱·무거움) 1, 뽁뽁이 0.25, 신문지 0.15 (가설).</summary>
        public float SoundWeight => kind == PackItemKind.Eggs ? 1f : kind == PackItemKind.Bubble ? 0.25f : 0.15f;

        void OnCollisionEnter(Collision c)
        {
            var bench = PackBench.I;
            var box = bench != null ? bench.Shaking : null;
            if (box == null || !box.HasContent(this)) return;
            // 물건끼리 부딪힘은 양쪽이 다 받는다 → id 작은 쪽만 센다
            var other = c.collider != null ? c.collider.GetComponentInParent<PackItem>() : null;
            if (other != null && other.GetInstanceID() < GetInstanceID()) return;
            box.ReportHit(this, other, c.relativeVelocity.magnitude);
        }
    }

    /// <summary>작업대 위 재료 더미(신문지 묶음·뽁뽁이 롤·테이프·라벨 묶음). 수량 없음(무한) — 선반 실물 수량은 다음 판.</summary>
    public class PackSupply : MonoBehaviour
    {
        public PackSupplyKind kind;
    }
}
