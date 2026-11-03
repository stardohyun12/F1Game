using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.PackBench
{
    // D-287: PackBox 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
    public partial class PackBox
    {
        // ── 흔들기 ──
        public struct ShakeResult
        {
            public string box; public float seconds, rattle, peakHit, trayMoved, eggShock, gap, bottomCushion, bareFloor;
            public int hits, escaped; public bool upright, closed, taped, labeled;
            public string locks, faces;
            // 소리 나눠 보기: 달걀판이 낀 부딪힘 / 완충재끼리·완충재-벽 · 눌려 잠긴 완충재 수
            public float rattleTray, rattleSoft; public int hitsTray, hitsSoft, softPinned, softTotal;
            public string ToJson() =>
                $"{{\"box\":\"{box}\",\"sec\":{seconds:F2},\"rattle\":{rattle:F3},\"peakHit\":{peakHit:F3},\"hits\":{hits},\"trayMovedM\":{trayMoved:F4},\"eggShock\":{eggShock:F2}," +
                $"\"rattleTray\":{rattleTray:F3},\"hitsTray\":{hitsTray},\"rattleSoft\":{rattleSoft:F3},\"hitsSoft\":{hitsSoft},\"softPinned\":\"{softPinned}/{softTotal}\"," +
                $"\"gap\":{gap:F3},\"bottomCushion\":{bottomCushion:F2},\"bareFloor\":{bareFloor:F2},\"upright\":{(upright ? "true" : "false")},\"escaped\":{escaped}," +
                $"\"closed\":{(closed ? "true" : "false")},\"taped\":{(taped ? "true" : "false")},\"labeled\":{(labeled ? "true" : "false")},\"locks\":\"{locks}\",\"faces\":\"{faces}\"}}";
        }

        ShakeResult cur;
        PackItem tray;
        ConfigurableJoint trayJoint;
        Vector3 trayPrevLocal, prevVel, prevPos;
        float shakeT0;
        bool shaking;
        public bool IsShaking => shaking;

        /// <summary>상자를 들어 올린 순간 — 내용물 정리·판정·달걀판 축 잠금.</summary>
        public void BeginShake()
        {
            shakeSet.Clear(); shakeSet.AddRange(Contents());
            var dump = new System.Text.StringBuilder();
            foreach (var it in shakeSet) { var b = WorldBounds(it); dump.Append(it.kind).Append(" y=").Append(transform.InverseTransformPoint(b.min).y.ToString("F3")).Append("~").Append(transform.InverseTransformPoint(b.max).y.ToString("F3")).Append("; "); }
            LastBeginDump = dump.ToString();
            cur = new ShakeResult { box = sizeName, closed = IsClosed, taped = Taped, labeled = Labeled, locks = "", faces = "" };
            MeasureFill(out cur.gap, out cur.bottomCushion);
            tray = shakeSet.Find(i => i.kind == PackItemKind.Eggs);
            if (tray != null)
            {
                cur.upright = Vector3.Dot(tray.transform.up, transform.up) > 0.9f;
                var fr = MeasureSupport(tray, out cur.bareFloor);
                var sb = new System.Text.StringBuilder();
                for (int f = 0; f < 6; f++) sb.Append(FaceNames[f]).Append('=').Append(fr[f].ToString("F2")).Append(f < 5 ? " " : "");
                cur.faces = sb.ToString();
                // 뒤집혔으면(방향 틀림) 판 안의 달걀이 논다 → 잠그지 않는다
                bool lx = cur.upright && fr[0] >= faceSupportedFrac && fr[1] >= faceSupportedFrac;
                bool ly = cur.upright && fr[2] >= faceSupportedFrac && fr[3] >= faceSupportedFrac;
                bool lz = cur.upright && fr[4] >= faceSupportedFrac && fr[5] >= faceSupportedFrac;
                cur.locks = (lx ? "X" : "") + (ly ? "Y" : "") + (lz ? "Z" : "");
                if (lx || ly || lz)
                {
                    trayJoint = tray.gameObject.AddComponent<ConfigurableJoint>();
                    trayJoint.connectedBody = Body;
                    trayJoint.autoConfigureConnectedAnchor = true;
                    trayJoint.anchor = Vector3.zero;
                    trayJoint.axis = Vector3.right; trayJoint.secondaryAxis = Vector3.up;
                    trayJoint.xMotion = lx ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
                    trayJoint.yMotion = ly ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
                    trayJoint.zMotion = lz ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
                    // 두 축 이상 벽에 끼어 있으면 판이 상자 안에서 돌 수 없다(옆 틈 7.5 mm) → 회전도 잠근다. 위아래로는 뛸 수 있다.
                    int nl = (lx ? 1 : 0) + (ly ? 1 : 0) + (lz ? 1 : 0);
                    var am = nl >= 2 ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
                    trayJoint.angularXMotion = trayJoint.angularYMotion = trayJoint.angularZMotion = am;
                    trayLocked = nl == 3;
                    // 다 잠겼으면 상자의 일부 — 벽과 계속 비비면 관절과 싸워 가짜 덜그럭이 난다(앞 세션 full hits 1625 의 일부)
                    trayJoint.enableCollision = !trayLocked;
                }
                trayPrevLocal = transform.InverseTransformPoint(tray.Body.position);
            }
            // 완충재: 위·아래가 둘 다 막혀 있으면(뚜껑·판·바닥 사이에 눌림) 상자에 잠근다 — 구긴 종이가 눌려 제자리에 박힌 것의 근사.
            // 안 눌린 것(위가 비어 굴러다니는 공)은 그대로 놀아서 사각사각 작은 소리를 낸다.
            foreach (var it in shakeSet)
            {
                if (it.kind == PackItemKind.Eggs) continue;
                cur.softTotal++;
                // 뽁뽁이 시트(2판): 상자 안에서 내려앉으면 이미 상자 몸의 일부(굽힌 칸 콜라이더)
                if (it.sheet != null) { if (it.transform.IsChildOf(transform)) cur.softPinned++; continue; }
                if (!IsPinned(it)) continue;
                var j = it.gameObject.AddComponent<FixedJoint>();
                j.connectedBody = Body;
                softJoints.Add(j); pinned.Add(it);
                cur.softPinned++;
            }
            foreach (var it in shakeSet) { if (it.Body != null) it.Body.WakeUp(); }
            pairs.Clear();
            prevPos = Body.position; prevVel = Vector3.zero;
            shakeT0 = Time.time; shaking = true;
        }

        void FixedUpdate()
        {
            if (!shaking) return;
            float dt = Time.fixedDeltaTime;
            Vector3 vel = (Body.position - prevPos) / dt;
            Vector3 acc = (vel - prevVel) / dt;
            prevPos = Body.position; prevVel = vel;
            if (tray != null)
            {
                var lp = transform.InverseTransformPoint(tray.Body.position);
                cur.trayMoved += (lp - trayPrevLocal).magnitude;
                trayPrevLocal = lp;
                // 달걀 충격: 판이 상자 바닥에 맨살로 닿아 있으면 상자의 흔들림이 그대로 들어간다. 기준 = CargoTypePresets 달걀 longWarnG.
                float thr = CargoTypePresets.Get(CargoType.Eggs).longWarnG * 9.81f;
                float over = Mathf.Max(0f, acc.magnitude - thr);
                float trans = Mathf.Lerp(0.2f, 1f, cur.bareFloor) * (cur.upright ? 1f : 3f);
                cur.eggShock += over * dt * trans;
            }
        }

        /// <summary>내려놓은 순간 — 결과를 닫는다.</summary>
        public ShakeResult EndShake()
        {
            if (!shaking) return cur;
            shaking = false;
            cur.seconds = Time.time - shakeT0;
            foreach (var it in shakeSet) if (it != null && !InsideInner(it.transform.position, 0.01f)) cur.escaped++;
            if (trayJoint != null) { Destroy(trayJoint); trayJoint = null; }
            trayLocked = false;
            foreach (var j in softJoints) if (j != null) Destroy(j);
            softJoints.Clear(); pinned.Clear();
            shakeSet.Clear();
            return cur;
        }

        bool trayLocked;
        /// <summary>QA: 들어 올린 순간 내용물 높이(상자 좌표, 바닥~꼭대기).</summary>
        public static string LastBeginDump = "";
        readonly List<FixedJoint> softJoints = new List<FixedJoint>();
        readonly List<PackItem> pinned = new List<PackItem>();

        bool IsLocked(PackItem it) => it != null && (it.kind == PackItemKind.Eggs ? trayLocked : pinned.Contains(it) || (it.sheet != null && it.transform.IsChildOf(transform)));

        /// <summary>완충재 하나가 위·아래(상자 축) 둘 다 supportTol 안에서 막혀 있나.</summary>
        bool IsPinned(PackItem it)
        {
            var col = it.GetComponent<Collider>();
            if (col == null) return false;
            Vector3 c = col.bounds.center, up = transform.up;
            float h = Mathf.Abs(Vector3.Dot(col.bounds.extents, new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z))));
            return Blocked(it, c, up, h) && Blocked(it, c, -up, h);
        }

        bool Blocked(PackItem self, Vector3 c, Vector3 d, float h)
        {
            // 자기 콜라이더 안에서 쏘면 자기는 안 맞는다 — 밖으로 h + supportTol
            foreach (var hit in Physics.RaycastAll(c, d, h + supportTol, ~0, QueryTriggerInteraction.Ignore))
            {
                var o = hit.collider.GetComponentInParent<PackItem>();
                if (o == self) continue;
                if (o != null || hit.collider.GetComponentInParent<PackBox>() == this) return true;
            }
            return false;
        }

        /// <summary>QA: 흔드는 동안 부딪힌 짝별 (횟수, 소리 합) — "Eggs-Lid" 꼴.</summary>
        readonly Dictionary<string, Vector2> pairs = new Dictionary<string, Vector2>();
        public static Collider lastHitCollider;
        public string PairsJson()
        {
            var sb = new System.Text.StringBuilder("{");
            foreach (var kv in pairs) sb.Append('"').Append(kv.Key).Append("\":[").Append(kv.Value.x.ToString("F0")).Append(',').Append(kv.Value.y.ToString("F2")).Append("],");
            if (sb.Length > 1) sb.Length--;
            return sb.Append('}').ToString();
        }

        public void ReportHit(PackItem it, PackItem other, float relSpeed)
        {
            if (!shaking || relSpeed < hitMinSpeed) return;
            // 잠긴 것끼리·잠긴 것과 상자 = 한 덩어리 → 소리 없음
            if (IsLocked(it) && (other == null || IsLocked(other))) return;
            // 무른 것(신문지·뽁뽁이)에 닿으면 소리는 무른 쪽 크기 — 판이 뽁뽁이 위로 떨어지면 '퍽'이지 '딱'이 아니다(1153: 뽁뽁이만 깐 상자가 맨 상자보다 시끄럽던 원인)
            float w = it.SoundWeight;
            if (other != null) w = Mathf.Min(w, other.SoundWeight);
            float vol = w * Mathf.Clamp01((relSpeed - hitMinSpeed) / (hitFullSpeed - hitMinSpeed));
            if (vol <= 0.005f) return;
            cur.rattle += vol; cur.hits++; cur.peakHit = Mathf.Max(cur.peakHit, vol);
            string key = it.kind + "-" + (other != null ? other.kind.ToString() : (lastHitCollider != null ? lastHitCollider.name : "?"));
            pairs.TryGetValue(key, out var pv); pairs[key] = new Vector2(pv.x + 1f, pv.y + vol);
            bool trayHit = it.kind == PackItemKind.Eggs || (other != null && other.kind == PackItemKind.Eggs);
            if (trayHit) { cur.rattleTray += vol; cur.hitsTray++; } else { cur.rattleSoft += vol; cur.hitsSoft++; }
            PackBench.PlayRattle(other != null && other.SoundWeight < it.SoundWeight ? other : it, vol); // 소리 종류도 무른 쪽
        }
    }
}
