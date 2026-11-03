using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.PackBench
{
    // D-287: PackBench 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
    public partial class PackBench
    {
        void Touch() { if (firstTouch < 0f) firstTouch = Time.time; }

        void PressE()
        {
            if (TryWrapLook()) return;
            if (held != null || lifted != null || moving != null || heldSheet != null) return;
            var b = LookBox();
            if (b == null) return;
            Touch();
            if (b == benchBox)
            {
                if (!b.IsClosed) { if (b.TryClose()) Sfx(sfxClose, 0.7f, 0.9f); else Sfx(sfxDeny, 0.6f, 0.7f); }
                else { b.Open(); Sfx(sfxScratch[0], 0.6f, 0.8f); }
                return;
            }
            // 선반 상자 고르기 — 작업대 상자가 비어 있으면 선반으로 돌려놓고 바꾼다
            if (benchBox != null)
            {
                if (benchBox.Contents().Count > 0) { Sfx(sfxDeny, 0.6f, 0.7f); return; }
                var old = benchBox;
                old.Open(); old.TryClose(); old.SnapLid();
                if (old.shelfSlot != null) old.transform.SetPositionAndRotation(old.shelfSlot.position, old.shelfSlot.rotation);
            }
            benchBox = b;
            b.Open();
            MoveBox(b, new Pose(benchSpot.position, benchSpot.rotation));
            Sfx(sfxPick, 0.6f, 1f);
        }

        void PressLeft()
        {
            if (lifted != null || moving != null) return;
            if (tool == Tool.Tape)
            {
                var s = LookAt<PackSupply>();
                if (s != null && s.kind == PackSupplyKind.Tape) { PutToolBack(); return; }
                var b = LookBox();
                if (b != null && b == benchBox && b.SeamParam(lookHit.point, out float t)) { taping = true; b.AddTape(t); Sfx(sfxScratch[1], 0.5f, 1.3f); }
                return;
            }
            if (tool == Tool.Label)
            {
                var s = LookAt<PackSupply>();
                if (s != null && s.kind == PackSupplyKind.Label) { PutToolBack(); return; }
                var b = LookAt<PackBox>();
                if (b != null && b == benchBox) { b.StickLabel(lookHit.point, lookHit.normal); Sfx(sfxPick, 0.6f, 1.2f); PutToolBack(); }
                return;
            }
            var item = LookAt<PackItem>();
            if (item != null && item.sheet != null) { Touch(); GrabSheet(item.sheet, lookHit.point); return; }
            if (item != null) { Touch(); Grab(item); return; }
            var sup = LookAt<PackSupply>();
            if (sup != null)
            {
                Touch();
                switch (sup.kind)
                {
                    case PackSupplyKind.Newspaper: StartCrumple(); break;
                    case PackSupplyKind.Bubble: StartPull(sup); break;
                    case PackSupplyKind.Tape: TakeTool(Tool.Tape, sup); break;
                    case PackSupplyKind.Label: TakeTool(Tool.Label, sup); break;
                }
                return;
            }
            var box = LookAt<PackBox>();
            if (box != null && box == benchBox) { Touch(); Lift(box); }
        }

        void HoldLeft()
        {
            if (pullSheet != null)
            {
                Vector3 hand = HoldTargetFree();
                float before = pullSheet.Length;
                pullSheet.Pull(rollExit, hand, rollCross);
                // 풀릴 때 롤이 도는 소리(길이 4 cm 마다 작게)
                pullTick += Mathf.Max(0f, pullSheet.Length - before);
                if (pullTick > 0.04f) { pullTick = 0f; Sfx(sfxScratch[Random.Range(0, sfxScratch.Length)], 0.18f, Random.Range(0.7f, 0.85f)); }
            }
            if (crumpling && held != null)
            {
                float k = Mathf.Clamp01((Time.time - crumpleT0) / crumpleFullSec);
                float r = Mathf.Lerp(paperRadiusLoose, paperRadiusTight, k);
                // 펴진 한 장(납작) → 공으로
                float flat = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(k * 3f));
                held.transform.localScale = new Vector3(2 * r / flat, 2 * r * flat, 2 * r / flat);
                heldHalfH = r;
                if (Random.value < Time.deltaTime * 12f) Sfx(sfxScratch[Random.Range(0, sfxScratch.Length)], 0.35f, Random.Range(1.1f, 1.4f));
            }
            if (taping && benchBox != null && hasHit && LookBox() == benchBox && benchBox.SeamParam(lookHit.point, out float t))
            {
                bool was = benchBox.Taped;
                benchBox.AddTape(t);
                if (!was && benchBox.Taped) Sfx(sfxDone, 0.6f, 1f);
            }
        }

        void ReleaseLeft()
        {
            if (pullSheet != null) FinishPull();
            if (heldSheet != null) { heldSheet.Release(); heldSheet = null; }
            if (crumpling) FinishCrumple();
            if (taping) { taping = false; if (benchBox != null && benchBox.Taped) PutToolBack(); }
            if (held != null) Drop();
            if (lifted != null && !liftedReturning) PutDown();
        }

        // ── 집기·놓기 ──
        void Grab(PackItem it)
        {
            if (it == null) return;
            held = it;
            heldRot = it.Body.rotation;
            var b = Bounds(it);
            heldHalfH = b.extents.y;
            holdDist = Mathf.Clamp(Vector3.Distance(cam.transform.position, it.Body.position), 0.25f, reach);
            breakT = 0f;
            WakeSheetsNear(b);
            if (LegacyHold) it.Body.isKinematic = true;
            else
            {
                // 2판: 동적 그대로 — 손은 속도로 끌어당긴다(FixedUpdate). 들고 있는 동안만 중력 끔(손이 받쳐 줌)
                it.Body.isKinematic = false;
                it.Body.useGravity = false;
                it.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                it.Body.maxAngularVelocity = 20f;
                it.Body.WakeUp();
            }
            SetLayer(it.gameObject, 2);
            Sfx(sfxPick, 0.4f, Random.Range(0.95f, 1.1f));
        }

        void Drop()
        {
            var it = held; held = null; crumpling = false;
            it.Body.isKinematic = false;
            it.Body.useGravity = true;
            it.Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            // 손을 떼면 손 속도가 조금 남는다(던지기는 없음 — 최대 0.6 m/s)
            it.Body.linearVelocity = Vector3.ClampMagnitude(it.Body.linearVelocity, 0.6f);
            it.Body.angularVelocity = Vector3.ClampMagnitude(it.Body.angularVelocity, 3f);
            SetLayer(it.gameObject, 0);
            it.Body.WakeUp();
        }

        static void SetLayer(GameObject g, int layer) { foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }

        static Bounds Bounds(PackItem it)
        {
            var cs = it.GetComponentsInChildren<Collider>();
            var b = cs.Length > 0 ? cs[0].bounds : new Bounds(it.transform.position, Vector3.one * 0.05f);
            for (int i = 1; i < cs.Length; i++) b.Encapsulate(cs[i].bounds);
            return b;
        }

        /// <summary>손 목표점 — 시선 위 잡은 거리(휠). 그 앞에 면이 있으면 면 위에 얹는다(물건 두께만큼 띄움).</summary>
        Vector3 HoldTarget()
        {
            if (SimTarget.HasValue) return SimTarget.Value;
            Vector3 o = cam.transform.position, f = cam.transform.forward;
            Vector3 p = o + f * holdDist;
            if (!hasHit) return p;
            Vector3 n = lookHit.normal;
            float ext = heldHalfH;
            if (held != null)
            {
                var e = Bounds(held).extents;
                ext = Mathf.Abs(n.x) * e.x + Mathf.Abs(n.y) * e.y + Mathf.Abs(n.z) * e.z;
            }
            else if (heldSheet != null) ext = 0.02f;
            // 휠로 보낸 점이 면에서 물건 두께 + 띄움보다 가까우면 면 위에 얹는다.
            // (전엔 면에 1 cm 까지 붙어야 얹어서, 그 사이엔 목표가 면 속 → 물건이 면에 걸려 휠이 안 먹는 구간이 있었다)
            float cos = Mathf.Max(0.2f, -Vector3.Dot(f, n));
            if ((lookHit.distance - holdDist) * cos < ext + holdLift) p = lookHit.point + n * (ext + holdLift);
            return p;
        }

        /// <summary>면에 얹지 않은 목표점(뽁뽁이 풀기 — 끈 거리 그대로).</summary>
        Vector3 HoldTargetFree() => SimTarget ?? (hasHit && holdDist >= lookHit.distance ? lookHit.point + Vector3.up * 0.02f : cam.transform.position + cam.transform.forward * holdDist);

        void WakeSheetsNear(Bounds b)
        {
            b.Expand(0.04f);
            foreach (var sh in FindObjectsByType<BubbleSheet>(FindObjectsSortMode.None))
                if (sh != heldSheet && sh.Settled && sh.WorldBounds.Intersects(b)) sh.Wake();
        }

        // ── 뽁뽁이(2판) ──
        void StartPull(PackSupply roll)
        {
            // 롤 = 눕힌 원기둥(축 = 롤의 up). 시트는 롤 윗면에서 손 쪽으로 풀려 나온다.
            var t = roll.transform;
            rollCross = t.up;
            float rad = t.lossyScale.x * 0.5f;
            Vector3 toCam = cam.transform.position - t.position; toCam = Vector3.ProjectOnPlane(toCam, rollCross).normalized;
            rollExit = t.position + Vector3.up * rad * 0.7f + toCam * rad * 0.7f;
            pullSheet = BubbleSheet.Create(bubbleMat);
            holdDist = Mathf.Clamp(Vector3.Distance(cam.transform.position, rollExit), 0.25f, reach);
            pullSheet.Pull(rollExit, rollExit + toCam * 0.02f, rollCross);
            pullTick = 0f;
            Sfx(sfxBubble, 0.45f, 0.9f);
        }

        void FinishPull()
        {
            var sh = pullSheet; pullSheet = null;
            if (sh.Length < BubbleSheet.MinLength) { Destroy(sh.gameObject); return; } // 거의 안 끌었으면 롤에 도로 감김
            sh.Tear();
            // 찢는 소리 — 짧은 긁힘 셋을 높게 겹친다
            Sfx(sfxScratch[0], 0.55f, 1.55f); Sfx(sfxScratch[1], 0.45f, 1.8f); Sfx(sfxScratch[2], 0.35f, 2.1f);
            Debug.Log($"[PackBench] 뽁뽁이 {sh.Length:F2} m 뜯음");
        }

        void GrabSheet(BubbleSheet sh, Vector3 at)
        {
            heldSheet = sh;
            sh.Grab(at);
            holdDist = Mathf.Clamp(Vector3.Distance(cam.transform.position, at), 0.25f, reach);
            Vector3 r = cam.transform.right; r.y = 0f;
            sheetCross = r.sqrMagnitude > 1e-4f ? r.normalized : Vector3.right;
            Sfx(sfxBubble, 0.35f, 1.1f);
        }

        /// <summary>E = 감싸기: 시트를 든 채 물건을 보거나, 물건 위에 얹힌 시트(또는 시트가 얹힌 물건)를 보고.</summary>
        bool TryWrapLook()
        {
            var look = LookAt<PackItem>();
            BubbleSheet sh = null; PackItem target = null;
            if (heldSheet != null && look != null && look.sheet == null) { sh = heldSheet; target = look; }
            else if (held == null && heldSheet == null && look != null) FindWrapPair(look, out sh, out target);
            if (sh == null || target == null) return false;
            return Wrap(sh, target);
        }

        /// <summary>물건 + 그 위에 얹힌 시트 짝 찾기(시트를 보든 물건을 보든).</summary>
        public static void FindWrapPair(PackItem look, out BubbleSheet sh, out PackItem target)
        {
            sh = null; target = null;
            if (look.sheet != null)
            {
                var sb = look.sheet.WorldBounds; sb.Expand(0.03f);
                foreach (var it in FindObjectsByType<PackItem>(FindObjectsSortMode.None))
                    if (it.sheet == null && !it.Wrapped && it.GetComponent<BoxCollider>() != null && sb.Intersects(Bounds(it))) { sh = look.sheet; target = it; return; }
                return;
            }
            if (look.Wrapped || look.GetComponent<BoxCollider>() == null) return;
            var b = Bounds(look); b.Expand(0.03f);
            // 닿은 시트가 여럿이면(밑에 깐 것 + 위에 덮은 것) 가장 긴 것 — 짧은 깐 시트를 골라 "모자람"으로 거절되던 것 고침
            foreach (var s in FindObjectsByType<BubbleSheet>(FindObjectsSortMode.None))
                if (s.Settled && s.WorldBounds.Intersects(b) && s.WorldBounds.max.y > b.center.y && (sh == null || s.Length > sh.Length)) { sh = s; target = look; }
        }

        public bool Wrap(BubbleSheet sh, PackItem target)
        {
            float need = BubbleSheet.WrapNeed(target);
            if (sh.Length < need * 0.9f) { Sfx(sfxDeny, 0.6f, 0.7f); Debug.Log($"[PackBench] 감싸기 실패 — 뽁뽁이 {sh.Length:F2} m < 한 바퀴 {need:F2} m"); return true; }
            if (heldSheet == sh) heldSheet = null;
            target.Wrap(bubbleMat, 0.005f);
            Destroy(sh.gameObject);
            Sfx(sfxBubble, 0.6f, 0.8f); Sfx(sfxScratch[1], 0.4f, 1.3f);
            Debug.Log($"[PackBench] 감쌈 {target.name} (뽁뽁이 {sh.Length:F2} m / 한 바퀴 {need:F2} m)");
            return true;
        }
    }
}
