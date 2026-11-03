using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.PackBench
{
    // D-287: PackBench 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
    public partial class PackBench
    {
        // ── 놓일 자리 그림자(점선 윤곽) ──
        Transform[] dots; Material dotMat;
        const int DotCount = 36;

        void LateUpdate()
        {
            bool show = (held != null || heldSheet != null) && !SimHideDots;
            // Play 진입 도메인 리로드가 private 배열을 null 이 아니라 빈 배열로 되살린다 → 길이로 판단
            if (dots == null || dots.Length != DotCount) { if (!show) return; MakeDots(); }
            if (!show) { foreach (var d in dots) d.gameObject.SetActive(false); return; }
            // 윤곽(바닥에 비친 네 귀퉁이) — 상자형은 자기 축, 시트는 경계 상자
            Vector3 c0, ax, az;
            if (held != null)
            {
                var bc = held.GetComponent<BoxCollider>();
                var t = held.transform;
                if (bc != null)
                {
                    Vector3 hx = t.TransformVector(new Vector3(bc.size.x * 0.5f, 0f, 0f)), hy = t.TransformVector(new Vector3(0f, bc.size.y * 0.5f, 0f)), hz = t.TransformVector(new Vector3(0f, 0f, bc.size.z * 0.5f));
                    // 바닥에 비친 넓은 두 축(제일 위를 향한 축을 뺀다)
                    float ux = Mathf.Abs(hx.normalized.y), uy = Mathf.Abs(hy.normalized.y), uz = Mathf.Abs(hz.normalized.y);
                    if (ux >= uy && ux >= uz) { ax = hy; az = hz; } else if (uy >= uz) { ax = hx; az = hz; } else { ax = hx; az = hy; }
                    c0 = t.TransformPoint(bc.center);
                }
                else { var b = Bounds(held); c0 = b.center; ax = new Vector3(b.extents.x, 0f, 0f); az = new Vector3(0f, 0f, b.extents.z); }
            }
            else { var b = heldSheet.WorldBounds; c0 = b.center; ax = new Vector3(b.extents.x, 0f, 0f); az = new Vector3(0f, 0f, b.extents.z); }
            ax.y = 0f; az.y = 0f;
            for (int i = 0; i < DotCount; i++)
            {
                float u = i / (float)DotCount * 4f; int side = (int)u; float f = u - side;
                Vector3 off = side == 0 ? Vector3.Lerp(-ax - az, ax - az, f) : side == 1 ? Vector3.Lerp(ax - az, ax + az, f) : side == 2 ? Vector3.Lerp(ax + az, -ax + az, f) : Vector3.Lerp(-ax + az, -ax - az, f);
                Vector3 o = c0 + off;
                bool hit = Physics.Raycast(o, Vector3.down, out var h, 1.2f, ~(1 << 2), QueryTriggerInteraction.Ignore);
                var d = dots[i];
                d.gameObject.SetActive(hit);
                if (hit) d.SetPositionAndRotation(h.point + h.normal * 0.0015f, Quaternion.LookRotation(-h.normal));
            }
        }

        public static bool SimHideDots;

        void MakeDots()
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            dotMat = new Material(sh != null ? sh : Shader.Find("Unlit/Color")) { name = "PB_Dot" };
            var col = new Color(0.08f, 0.08f, 0.09f);
            dotMat.SetColor("_BaseColor", col); dotMat.color = col;
            var root = new GameObject("PlaceDots").transform;
            dots = new Transform[DotCount];
            for (int i = 0; i < DotCount; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.transform.SetParent(root, false);
                q.transform.localScale = Vector3.one * 0.009f;
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = dotMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                dots[i] = q.transform;
                q.SetActive(false);
            }
        }

        // ── 신문지 ──
        void StartCrumple()
        {
            var it = SpawnPaper(lookHit.point + Vector3.up * 0.08f, paperRadiusLoose);
            Grab(it);
            crumpling = true; crumpleT0 = Time.time;
        }

        void FinishCrumple()
        {
            crumpling = false;
            if (held == null) return;
            float k = Mathf.Clamp01((Time.time - crumpleT0) / crumpleFullSec);
            float r = Mathf.Lerp(paperRadiusLoose, paperRadiusTight, k);
            held.transform.localScale = Vector3.one * 2f * r;
            heldHalfH = r;
        }

        public PackItem SpawnPaper(Vector3 at, float radius)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "Paper";
            g.transform.position = at;
            g.transform.localScale = Vector3.one * 2f * radius;
            g.transform.rotation = Random.rotation;
            if (paperMat != null) g.GetComponent<Renderer>().sharedMaterial = paperMat;
            g.GetComponent<Collider>().sharedMaterial = SoftMat;
            var rb = g.AddComponent<Rigidbody>();
            // 실제 신문지 한 장 ≈ 20 g 이지만 2 kg 판과 67배 차이면 PhysX 가 판을 완충재 속으로 밀어 넣는다(1153 진단) → 질량은 풀이 안정용(가설), 소리 무게는 PackItem.SoundWeight 따로
            rb.mass = 0.15f; rb.linearDamping = 0.6f; rb.angularDamping = 2f;
            Setup(rb);
            var it = g.AddComponent<PackItem>(); it.kind = PackItemKind.Paper;
            return it;
        }

        public PackItem SpawnBubble(Vector3 at)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "Bubble";
            g.transform.position = at;
            g.transform.rotation = Quaternion.Euler(0f, benchSpot != null ? benchSpot.eulerAngles.y : 0f, 0f);
            g.transform.localScale = new Vector3(0.30f, 0.02f, 0.30f);
            if (bubbleMat != null) g.GetComponent<Renderer>().sharedMaterial = bubbleMat;
            g.GetComponent<Collider>().sharedMaterial = SoftMat;
            // 뽁뽁이 알갱이(보기만)
            for (int x = 0; x < 5; x++)
                for (int z = 0; z < 5; z++)
                {
                    var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(d.GetComponent<Collider>());
                    d.transform.SetParent(g.transform, false);
                    d.transform.localPosition = new Vector3((x - 2) * 0.19f, 0.5f, (z - 2) * 0.19f);
                    d.transform.localScale = new Vector3(0.12f, 0.9f, 0.12f);
                    if (bubbleMat != null) d.GetComponent<Renderer>().sharedMaterial = bubbleMat;
                }
            var rb = g.AddComponent<Rigidbody>();
            rb.mass = 0.4f; rb.linearDamping = 0.8f; rb.angularDamping = 2f; // 질량 = 풀이 안정용(위 신문지 주석)
            Setup(rb);
            var it = g.AddComponent<PackItem>(); it.kind = PackItemKind.Bubble;
            return it;
        }

        public static PhysicsMaterial SoftMaterial => SoftMat;

        public static void Setup(Rigidbody rb)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // 키네마틱(든 동안)에서도 경고 없음
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.solverIterations = 12; rb.solverVelocityIterations = 4;
        }

        static PhysicsMaterial softMat;
        static PhysicsMaterial SoftMat => softMat != null ? softMat : (softMat = new PhysicsMaterial("PackSoft") { dynamicFriction = 0.8f, staticFriction = 0.9f, bounciness = 0f, frictionCombine = PhysicsMaterialCombine.Maximum, bounceCombine = PhysicsMaterialCombine.Minimum });

        // ── 도구(테이프·라벨) ──
        void TakeTool(Tool t, PackSupply s)
        {
            tool = t; toolSupply = s;
            var r = s.GetComponentInChildren<Renderer>();
            toolVis = Instantiate(r.gameObject);
            foreach (var c in toolVis.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var c in toolVis.GetComponentsInChildren<PackSupply>()) Destroy(c);
            toolVis.transform.SetParent(cam.transform, false);
            toolVis.transform.localPosition = new Vector3(0.16f, -0.12f, 0.38f);
            toolVis.transform.localRotation = Quaternion.Euler(20f, -25f, 0f);
            toolVis.transform.localScale = r.transform.lossyScale;
            foreach (var rr in s.GetComponentsInChildren<Renderer>()) rr.enabled = false;
            Sfx(sfxPick, 0.5f, 0.9f);
        }

        void PutToolBack()
        {
            if (toolSupply != null) foreach (var rr in toolSupply.GetComponentsInChildren<Renderer>()) rr.enabled = true;
            if (toolVis != null) Destroy(toolVis);
            tool = Tool.None; toolSupply = null; taping = false;
        }
    }
}
