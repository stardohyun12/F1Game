using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PitCrew.PackBench
{
    /// <summary>
    /// 2판(D-253) 검사 — 마우스 대신 같은 코드 경로(Grab·DriveHeld·StartPull…)를 스크립트로 몬다. 씬 Proto_PackBench Play 중에만.
    ///   wall  = 달걀판을 상자 벽 쪽으로 세게 끌기 20회(1판 키네마틱 vs 2판 물리) → 벽 통과 횟수
    ///   wheel = 휠로 잡은 거리 바꾸기 전후 위치
    ///   sheet = 뽁뽁이 0.2/0.5/0.9 m 풀기 → 바닥에 깔기 → 판 얹기 → 덮기 → 감싸기
    /// 결과는 Qa2Status(JSON 꼴 한 줄씩).
    /// </summary>
    public partial class PackBench
    {
        public static string Qa2Status = "idle";
        public static readonly List<string> Qa2Log = new List<string>();
        public static string Qa2Dir = "";

        public static string Qa2Run(string what)
        {
            if (I == null) return "no PackBench";
            Qa2Status = "run " + what;
            I.StartCoroutine(I.Qa2Routine(what));
            return "started " + what;
        }

        static void QLog(string s) { Qa2Log.Add(s); Debug.Log("[PackBench QA2] " + s); }

        IEnumerator Qa2Routine(string what)
        {
            if (what == "shake" || what == "all")
            {
                // 1판 흔들기 회귀(맨 상자 시끄러움 / 바닥만 / 꽉 채움 조용) — 같은 봇, 10초. 미리 접촉 거르기 끔(1판) → 켬(2판)
                foreach (var sc in new[] { "bare:off", "bottom:off", "bare:on", "bottom:on", "full:on" })
                {
                    var parts = sc.Split(':');
                    PackItem.FilterSpeculative = parts[1] == "on";
                    int d0 = PackItem.SpecDeferred, l0 = PackItem.SpecLanded;
                    lidProbe = true; lidMin = 9f; lidNear = 0; lidSteps = 0; StartCoroutine(LidProbe());
                    yield return BotRoutine(parts[0], 10f);
                    lidProbe = false;
                    QLog($"{{\"test\":\"shake\",\"scenario\":\"{parts[0]}\",\"specFilter\":\"{parts[1]}\",\"lidMinGapMm\":{lidMin * 1000f:F1},\"lidTouchSteps\":{lidNear}/{lidSteps},\"specDeferred\":{PackItem.SpecDeferred - d0},\"specLanded\":{PackItem.SpecLanded - l0},\"played\":{RattlePlayed},\"pairs\":{LastPairs},\"result\":{LastShakeJson}" + "}");
                    PackItem.FilterSpeculative = true;
                    var trayW = Tray(); if (trayW != null && trayW.Wrapped) QLog("warn: tray wrapped during shake");
                }
                if (what == "shake") { Qa2Status = "done shake"; yield break; }
            }
            SimActive = true;
            yield return QaBenchBox();
            if (what == "wall" || what == "all") { yield return QaWall(true); yield return QaWall(false); }
            if (what.Contains("wheel") || what == "all") yield return QaWheel();
            if (what.Contains("sheet") || what == "all") yield return QaSheet();
            SimTarget = null; SimDrop(); LegacyHold = false;
            SimActive = false;
            Qa2Status = "done " + what;
        }

        // 흔드는 동안 판(충돌 상자 8꼭짓점)과 뚜껑 사이 가장 가까운 거리 — 뚜껑 덜그럭이 진짜 닿음인지 미리 접촉인지 가른다
        bool lidProbe; float lidMin; int lidNear, lidSteps;
        IEnumerator LidProbe()
        {
            var tray = Tray(); var bc = tray != null ? tray.GetComponent<BoxCollider>() : null;
            while (lidProbe)
            {
                yield return new WaitForFixedUpdate();
                var box = Shaking; if (box == null || bc == null) continue;
                Collider lid = null;
                foreach (var c in box.GetComponentsInChildren<Collider>()) if (c.name == "Lid" && c.enabled) lid = c;
                if (lid == null) continue;
                float m = 9f;
                for (int k = 0; k < 8; k++)
                {
                    var s = new Vector3((k & 1) == 0 ? -0.5f : 0.5f, (k & 2) == 0 ? -0.5f : 0.5f, (k & 4) == 0 ? -0.5f : 0.5f);
                    Vector3 c = bc.transform.TransformPoint(bc.center + Vector3.Scale(bc.size, s));
                    m = Mathf.Min(m, Vector3.Distance(lid.ClosestPoint(c), c));
                }
                lidSteps++; lidMin = Mathf.Min(lidMin, m); if (m <= 0.002f) lidNear++;
            }
        }

        IEnumerator QaBenchBox()
        {
            var box = FindBox("M");
            if (benchBox != box)
            {
                if (benchBox != null) { benchBox.Open(); benchBox.TryClose(); benchBox.SnapLid(); if (benchBox.shelfSlot != null) benchBox.transform.SetPositionAndRotation(benchBox.shelfSlot.position, benchBox.shelfSlot.rotation); }
                benchBox = box; box.Open(); MoveBox(box, new Pose(benchSpot.position, benchSpot.rotation));
            }
            else if (box.IsClosed) box.Open();
            while (moving != null) yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.3f);
        }

        void QaPlaceTray(Vector3 local)
        {
            var box = benchBox; var tr = box.transform; var tray = Tray();
            SimTarget = null; SimDrop();
            tray.Body.isKinematic = false;
            Vector3 p = tr.TransformPoint(local);
            tray.transform.SetPositionAndRotation(p, tr.rotation);
            tray.Body.position = p; tray.Body.rotation = tr.rotation;
            tray.Body.linearVelocity = Vector3.zero; tray.Body.angularVelocity = Vector3.zero;
        }

        int qaShot;
        void QaShot(string tag)
        {
            if (string.IsNullOrEmpty(Qa2Dir)) return;
            ScreenCapture.CaptureScreenshot($"{Qa2Dir}/{tag}_{qaShot++:D3}.png");
        }

        // ── 벽 통과 ──
        IEnumerator QaWall(bool legacy)
        {
            LegacyHold = legacy;
            var box = benchBox; var tr = box.transform; var tray = Tray();
            float floorY = box.InnerBottomY;
            Vector3 home = new Vector3(0f, floorY + 0.0325f + 0.004f, 0f);
            float outer = box.inner.x * 0.5f + box.wall; // 벽 바깥면(상자 좌표)
            int passed = 0, slipped = 0; int slip0 = SlipCount;
            var per = new StringBuilder();
            string tag = legacy ? "wall_v1" : "wall_v2";
            for (int i = 0; i < 20; i++)
            {
                QaPlaceTray(home);
                yield return new WaitForSeconds(0.35f);
                Vector3 dirL = (i % 4) switch { 0 => Vector3.right, 1 => Vector3.left, 2 => Vector3.forward, _ => Vector3.back };
                float dist = (i / 4) switch { 0 => 0.12f, 1 => 0.20f, 2 => 0.30f, 3 => 0.40f, _ => 0.50f }; // 12 cm(벽에 눌러 붙이기)~50 cm(세게 = 손에서 빠짐)
                SimGrab(tray);
                SimTarget = tr.TransformPoint(home + dirL * dist);
                float maxOut = 0f; bool wasHeld = true;
                for (float t = 0f; t < 1.0f; t += Time.fixedDeltaTime)
                {
                    yield return new WaitForFixedUpdate();
                    Vector3 lp = tr.InverseTransformPoint(tray.Body.position);
                    maxOut = Mathf.Max(maxOut, Mathf.Max(Mathf.Abs(lp.x), Mathf.Abs(lp.z)));
                    if (held == null) wasHeld = false;
                    if (i == 0 && Mathf.Repeat(t, 0.1f) < Time.fixedDeltaTime) QaShot(tag);
                }
                // 판 중심이 벽 바깥면을 넘었으면 통과(판 반폭 15 cm 라 중심이 벽에 닿기 전에 몸이 벽에 박힌다)
                bool pass = maxOut > outer - 0.15f + 0.02f;
                if (pass) passed++;
                if (!wasHeld) slipped++;
                // 칸 = 통과 P / 막힘 . , s = 손에서 빠짐, 숫자 = 판 중심 최대 이동(mm)
                per.Append(pass ? 'P' : '.').Append(wasHeld ? "" : "s").Append(Mathf.RoundToInt(maxOut * 1000f)).Append(' ');
                SimTarget = null; SimDrop();
            }
            QLog($"{{\"test\":\"{tag}\",\"trials\":20,\"passed\":{passed},\"slipped\":{SlipCount - slip0},\"perTrial\":\"{per}\",\"passIfCenterMovedMm\":{Mathf.RoundToInt((outer - 0.15f + 0.02f) * 1000f)}" + "}");
            LegacyHold = false;
            QaPlaceTray(home);
            yield return new WaitForSeconds(0.3f);
        }

        // ── 휠 = 잡은 거리 ──
        IEnumerator QaWheel()
        {
            var box = benchBox; var tr = box.transform; var tray = Tray();
            QaPlaceTray(new Vector3(0f, box.InnerBottomY + 0.04f, 0f));
            yield return new WaitForSeconds(0.4f);
            SimGrab(tray);
            // 먼저 똑바로 들어 올린다(판이 상자에 꼭 맞아 비스듬히 끌면 벽에 걸려 손에서 빠진다 — 1차 실행에서 그랬음)
            SimTarget = tr.TransformPoint(new Vector3(0f, box.RimY + 0.12f, 0f));
            yield return new WaitForSeconds(1.0f);
            Vector3 cp = cam.transform.position;
            holdDist = Mathf.Min(Vector3.Distance(cp, tray.Body.position), hasHit ? lookHit.distance - 0.15f : 9f);
            SimTarget = null; // 여기부터는 사람 손과 같은 시선 + 잡은 거리
            yield return new WaitForSeconds(1.0f);
            int slip0 = SlipCount;
            string P(string name) { var p = tray.Body.position; return $"\"{name}\":{{\"held\":{(held == tray).ToString().ToLower()},\"holdDist\":{holdDist:F3},\"camDist\":{Vector3.Distance(cp, p):F3},\"pos\":[{p.x:F3},{p.y:F3},{p.z:F3}],\"lookHit\":{(hasHit ? lookHit.distance : -1f):F3}" + "}"; }
            var sb = new StringBuilder("{\"test\":\"wheel\",");
            sb.Append(P("start")).Append(',');
            QaShot("wheel");
            SimWheel(+4f); // 4칸 앞으로 = +12 cm
            yield return new WaitForSeconds(1.0f);
            sb.Append(P("after+4")).Append(',');
            QaShot("wheel");
            SimWheel(-8f); // 8칸 당겨 오기 = −24 cm
            yield return new WaitForSeconds(1.0f);
            sb.Append(P("after-8")).Append(',');
            QaShot("wheel");
            // 천천히 내려놓기: 휠로 바닥까지 보낸다(면에 닿으면 면 위에 얹힘)
            int touch0 = TouchPlayed;
            for (int k = 0; k < 20; k++) { SimWheel(+1f); yield return new WaitForSeconds(0.08f); }
            yield return new WaitForSeconds(0.6f);
            sb.Append(P("lowered")).Append(',');
            QaShot("wheel");
            SimDrop();
            yield return new WaitForSeconds(0.8f);
            Vector3 lp = tr.InverseTransformPoint(tray.Body.position);
            sb.Append($"\"dropLocalY\":{lp.y:F3},\"floorY\":{box.InnerBottomY:F3},\"touchSounds\":{TouchPlayed - touch0},\"slips\":{SlipCount - slip0}}}");
            QLog(sb.ToString());
        }

        // ── 뽁뽁이 ──
        IEnumerator QaSheet()
        {
            var box = benchBox; var tr = box.transform; var tray = Tray();
            foreach (var it in FindObjectsByType<PackItem>(FindObjectsSortMode.None)) if (it.kind != PackItemKind.Eggs) Destroy(it.gameObject);
            // 판은 상자 밖 작업대 위 처음 자리로(빌더 MakeTray 위치 — 롤과 안 겹침)
            {
                SimTarget = null; SimDrop();
                var tp = new Vector3(-0.45f, 0.9f + 0.0325f + 0.004f, 0.45f);
                tray.Body.isKinematic = false;
                tray.transform.SetPositionAndRotation(tp, Quaternion.identity);
                tray.Body.position = tp; tray.Body.rotation = Quaternion.identity;
                tray.Body.linearVelocity = Vector3.zero; tray.Body.angularVelocity = Vector3.zero;
            }
            yield return new WaitForSeconds(0.5f);
            var sheets = new List<BubbleSheet>();
            float[] lens = { 0.2f, 0.5f, 0.9f };
            foreach (float L in lens)
            {
                if (!SimPullStart()) { QLog("{\"test\":\"pull\",\"error\":\"no roll\"}"); yield break; }
                Vector3 ex = RollExit;
                Vector3 dir = cam.transform.position - ex; dir.y = 0f; dir = dir.normalized;
                // 손을 천천히(0.5 m/s) 끌어 당긴다
                float cur = 0.02f;
                while (cur < L)
                {
                    cur = Mathf.Min(L, cur + 0.5f * Time.deltaTime);
                    SimTarget = ex + dir * cur + Vector3.up * 0.05f;
                    SimPullStep();
                    yield return null;
                }
                SimPullStep();
                yield return null;
                QaShot($"pull{Mathf.RoundToInt(L * 100)}");
                yield return null; yield return null;
                var sh = SimPullEnd();
                SimTarget = null;
                QLog($"{{\"test\":\"pull\",\"handDist\":{L:F2},\"sheetLength\":{(sh != null ? sh.Length : -1f):F3}" + "}");
                if (sh != null) sheets.Add(sh);
                yield return new WaitForSeconds(0.4f);
            }
            // 시트가 내려앉는 동안 프레임 시간
            float t0 = Time.realtimeSinceStartup; int f0 = Time.frameCount; float simMax = 0f;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { simMax = Mathf.Max(simMax, BubbleSheet.LastSimMs); yield return null; }
            float msSim = (Time.realtimeSinceStartup - t0) * 1000f / Mathf.Max(1, Time.frameCount - f0);
            QLog($"{{\"test\":\"frame\",\"phase\":\"3 sheets falling\",\"frameMs\":{msSim:F2},\"sheetSimMsMax\":{simMax:F2}" + "}");
            if (sheets.Count < 3) yield break;
            if (sheets[0] != null) Destroy(sheets[0].gameObject);

            // ① 바닥에 펴서 깔기 — 0.5 m 시트 끝을 잡고 상자 한쪽 위로 → 낮추며 반대쪽으로 끌어 편다
            var s5 = sheets[1];
            yield return new WaitForSeconds(1.0f);
            SimGrabSheet(s5);
            Vector3 fl = tr.TransformPoint(new Vector3(0f, box.InnerBottomY, 0f));
            // 먼저 높이 들어 늘어뜨린다(바닥에서 곧장 끌면 남은 쪽이 앞벽 바깥에 걸림 — 3차 실행에서 그랬음)
            Vector3 s50 = s5.Center;
            SimTarget = new Vector3(s50.x, fl.y + 0.55f, s50.z);
            yield return new WaitForSeconds(1.0f);
            SimTarget = fl + tr.up * 0.55f + tr.forward * 0.12f;
            yield return new WaitForSeconds(1.2f);
            SimTarget = fl + tr.up * 0.20f + tr.forward * 0.12f;
            yield return new WaitForSeconds(0.8f);
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                float k = t / 1.2f;
                SimTarget = fl + tr.up * Mathf.Lerp(0.20f, 0.03f, k) + tr.forward * Mathf.Lerp(0.12f, -0.12f, k);
                if (Mathf.Repeat(t, 0.1f) < Time.deltaTime) QaShot("lay");
                yield return null;
            }
            SimDrop(); SimTarget = null;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { if (Mathf.Repeat(t, 0.1f) < Time.deltaTime) QaShot("lay"); yield return null; }
            for (float t = 0f; t < 4f && !s5.Settled; t += Time.deltaTime) yield return null;
            var b5 = s5.WorldBounds;
            QLog($"{{\"test\":\"lay\",\"settled\":{s5.Settled.ToString().ToLower()},\"inBox\":{s5.transform.IsChildOf(tr).ToString().ToLower()},\"minYAboveFloor\":{(tr.InverseTransformPoint(b5.min).y - box.InnerBottomY):F3},\"maxYAboveFloor\":{(tr.InverseTransformPoint(b5.max).y - box.InnerBottomY):F3},\"len\":{s5.Length:F2}" + "}");

            // 판을 깐 시트 위에 내려놓는다(손으로)
            // 사람처럼: 들어 올림 → 상자 위로 → 내려놓기 (곧장 끌면 상자 벽에 막혀 손에서 빠진다 — 1차 실행)
            SimGrab(tray);
            float carryY = tr.TransformPoint(new Vector3(0f, box.RimY + 0.10f, 0f)).y;
            Vector3 tp0 = tray.Body.position;
            SimTarget = new Vector3(tp0.x, carryY, tp0.z);
            yield return new WaitForSeconds(0.8f);
            Vector3 over = tr.TransformPoint(new Vector3(0f, box.RimY + 0.10f, 0f));
            for (float t = 0f; t < 1.0f; t += Time.deltaTime) { SimTarget = Vector3.Lerp(new Vector3(tp0.x, carryY, tp0.z), over, t); if (Mathf.Repeat(t, 0.1f) < Time.deltaTime) QaShot("carry"); yield return null; }
            yield return new WaitForSeconds(0.5f);
            Vector3 down = tr.TransformPoint(new Vector3(0f, box.InnerBottomY + 0.05f, 0f));
            for (float t = 0f; t < 1.0f; t += Time.deltaTime) { SimTarget = Vector3.Lerp(over, down, t); if (Mathf.Repeat(t, 0.1f) < Time.deltaTime) QaShot("carry"); yield return null; }
            yield return new WaitForSeconds(0.5f);
            QLog($"{{\"test\":\"carry\",\"heldAtEnd\":{(held == tray).ToString().ToLower()}" + "}");
            SimDrop(); SimTarget = null;
            yield return new WaitForSeconds(1.0f);
            float trayBottom = tr.InverseTransformPoint(tray.Body.position).y - 0.0325f - box.InnerBottomY;
            QLog($"{{\"test\":\"trayOnSheet\",\"trayBottomAboveFloor\":{trayBottom:F3}" + "}");

            // ② 물건 위에 덮기 — 0.9 m 시트를 판 위로 가져가 놓는다
            var s9 = sheets[2];
            SimGrabSheet(s9);
            // 끝을 쥐고 상자 한가운데 높이 들었다가(시트가 늘어뜨려짐) 판 바로 위까지 천천히 내린다 → 접히며 판 위에 쌓임
            // 0.9 m 라 45 cm 로는 끝이 앞벽에 걸려 밖으로 흘렀다(4차) → 시트 길이만큼 높이 든 뒤 상자 위로
            Vector3 sp0 = s9.Center; float hi = 0.85f;
            SimTarget = new Vector3(sp0.x, tr.TransformPoint(new Vector3(0f, box.RimY + hi, 0f)).y, sp0.z);
            yield return new WaitForSeconds(1.2f);
            SimTarget = tr.TransformPoint(new Vector3(0f, box.RimY + hi, 0f));
            yield return new WaitForSeconds(1.5f);
            for (float t = 0f; t < 2.0f; t += Time.deltaTime)
            {
                SimTarget = tr.TransformPoint(new Vector3(0f, Mathf.Lerp(box.RimY + hi, box.InnerBottomY + 0.12f, t / 2.0f), 0f));
                if (Mathf.Repeat(t, 0.1f) < Time.deltaTime) QaShot("cover");
                yield return null;
            }
            SimDrop(); SimTarget = null;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime) { if (Mathf.Repeat(t, 0.1f) < Time.deltaTime) QaShot("cover"); yield return null; }
            for (float t = 0f; t < 4f && !s9.Settled; t += Time.deltaTime) yield return null;
            var b9 = s9.WorldBounds;
            float trayTop = tr.InverseTransformPoint(tray.Body.position).y + 0.0325f;
            QLog($"{{\"test\":\"cover\",\"settled\":{s9.Settled.ToString().ToLower()},\"inBox\":{s9.transform.IsChildOf(tr).ToString().ToLower()},\"sheetMaxY_minus_trayTop\":{(tr.InverseTransformPoint(b9.max).y - trayTop):F3}" + "}");
            QaShot("cover_done");

            // ③ 감싸기 — 판을 보고 E(덮인 시트 + 판 짝)
            FindWrapPair(tray, out var wsh, out var wt);
            bool pairFound = wsh != null && wt != null; // Wrap 이 시트를 지우면 wsh 가 null 로 보이므로 미리
            bool ok = pairFound && Wrap(wsh, wt);
            yield return new WaitForSeconds(0.5f);
            QaShot("wrap_done");
            QLog($"{{\"test\":\"wrap\",\"pairFound\":{pairFound.ToString().ToLower()},\"wrapped\":{tray.Wrapped.ToString().ToLower()},\"need\":{BubbleSheet.WrapNeed(tray):F2}" + "}");
        }
    }
}
