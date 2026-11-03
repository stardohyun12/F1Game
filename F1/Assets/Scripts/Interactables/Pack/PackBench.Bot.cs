using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.PackBench
{
    // D-287: PackBench 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
    public partial class PackBench
    {
        /// <summary>시나리오: "bare" = 완충재 없이 달걀판만 · "bottom" = 뽁뽁이 바닥만(위 빈틈 그대로) · "full" = 뽁뽁이 바닥 + 달걀판 + 신문지 3×3 위 · "flip" = full 인데 판을 뒤집어 넣음. 끝나면 LastShakeJson.</summary>
        public static string BotRun(string scenario, float shakeSec = 3f)
        {
            if (I == null) return "no PackBench";
            I.StartCoroutine(I.BotRoutine(scenario, shakeSec));
            return "started " + scenario;
        }
        public static string BotStatus = "idle";
        /// <summary>QA: 비어 있지 않으면 봇이 단계마다 화면을 찍는다({dir}/{scenario}_{step}.png) + 흔드는 동안 QaFrameEvery 초마다.</summary>
        public static string QaShotDir = "";
        public static float QaFrameEvery = 0.5f;
        string botScenario;

        IEnumerator Shot(string step)
        {
            if (string.IsNullOrEmpty(QaShotDir)) yield break;
            ScreenCapture.CaptureScreenshot($"{QaShotDir}/{botScenario}_{step}.png");
            yield return null; yield return null;
        }

        static void SetLidSeeThrough(PackBox b, bool on)
        {
            var lid = b.transform.Find("Shell/LidPivot");
            if (lid != null) foreach (var r in lid.GetComponentsInChildren<Renderer>()) r.enabled = !on;
            var tape = b.transform.Find("Shell/Tape");
            if (tape != null) foreach (var r in tape.GetComponentsInChildren<Renderer>()) r.enabled = !on;
            foreach (var r in b.GetComponentsInChildren<Renderer>()) if (r.name == "Label" || (r.transform.parent != null && r.transform.parent.name == "Label")) r.enabled = !on;
        }

        IEnumerator BotRoutine(string scenario, float shakeSec)
        {
            BotStatus = "run " + scenario; botScenario = scenario;
            SimActive = true;
            float t0 = Time.time; firstTouch = Time.time; timed = false;
            var box = FindBox("M");
            if (benchBox != box)
            {
                if (benchBox != null) { benchBox.Open(); benchBox.TryClose(); benchBox.SnapLid(); if (benchBox.shelfSlot != null) benchBox.transform.SetPositionAndRotation(benchBox.shelfSlot.position, benchBox.shelfSlot.rotation); }
                benchBox = box; box.Open(); MoveBox(box, new Pose(benchSpot.position, benchSpot.rotation));
            }
            else if (box.IsClosed) box.Open();
            while (moving != null) yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.5f);
            yield return Shot("1_box");
            // 상자 안 정리: 이전 신문지·뽁뽁이 치우고 달걀판을 상자 위로
            foreach (var it in FindObjectsByType<PackItem>(FindObjectsSortMode.None)) if (it.kind != PackItemKind.Eggs) Destroy(it.gameObject);
            yield return null;
            var tr = box.transform;
            float floorY = box.InnerBottomY;
            bool full = scenario == "full" || scenario == "flip";
            if (full || scenario == "bottom")
            {
                var bub = SpawnBubble(tr.TransformPoint(new Vector3(0f, floorY + 0.02f, 0f)));
                bub.transform.rotation = tr.rotation;
                yield return new WaitForSeconds(0.6f);
                floorY += 0.02f;
                yield return Shot("2_bubble");
            }
            var tray = Tray();
            tray.Body.isKinematic = false;
            var trayAt = tr.TransformPoint(new Vector3(0f, floorY + 0.0325f + 0.01f, 0f));
            // "flip" = 꽉 채웠지만 판을 뒤집어 넣음(방향 틀림)
            var trayRot = scenario == "flip" ? tr.rotation * Quaternion.Euler(180f, 0f, 0f) : tr.rotation;
            tray.transform.SetPositionAndRotation(trayAt, trayRot);
            tray.Body.position = trayAt; tray.Body.rotation = trayRot;
            tray.Body.linearVelocity = Vector3.zero; tray.Body.angularVelocity = Vector3.zero;
            yield return new WaitForSeconds(0.8f);
            yield return Shot("3_tray");
            if (full)
            {
                // 뚜껑까지 채움 = 공 꼭대기가 테두리(RimY)에 닿거나 살짝 넘침(뚜껑이 눌러 줌, TryClose 허용 1 cm 안) — 6.2 cm 공은 1.2 cm 틈이 남아 3개가 굴렀다(1153)
                const float pr = 0.034f;
                float y = floorY + 0.065f + pr;
                for (int x = -1; x <= 1; x++)
                    for (int z = -1; z <= 1; z++)
                        SpawnPaper(tr.TransformPoint(new Vector3(x * 0.1f, y + 0.005f, z * 0.1f)), pr);
                yield return new WaitForSeconds(1.0f);
                yield return Shot("4_paper");
            }
            bool closed = box.TryClose();
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i <= 10; i++) box.AddTape(i / 10f);
            box.StickLabel(tr.TransformPoint(new Vector3(0.06f, box.RimY + box.wall * 2f, -0.06f)), tr.up);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("5_taped");
            RattlePlayed = 0; RattleVolumeSum = 0f;
            Lift(box);
            yield return new WaitForSeconds(0.6f);
            yield return Shot("6_lifted");
            simShakeT0 = Time.time; simShakeUntil = Time.time + shakeSec;
            if (!string.IsNullOrEmpty(QaShotDir) && QaFrameEvery > 0f)
            {
                // 흔드는 동안 연속 프레임 — 뚜껑을 투명하게(보기만, 충돌은 그대로) 해서 속을 찍는다
                SetLidSeeThrough(box, true);
                int n = 0;
                while (Time.time < simShakeUntil)
                {
                    ScreenCapture.CaptureScreenshot($"{QaShotDir}/{scenario}_shake_{n++:D2}.png");
                    yield return new WaitForSeconds(QaFrameEvery);
                }
                SetLidSeeThrough(box, false);
                yield return new WaitForSeconds(0.2f);
            }
            else yield return new WaitForSeconds(shakeSec + 0.2f);
            PutDown();
            while (lifted != null) yield return new WaitForFixedUpdate();
            SimActive = false;
            yield return Shot("7_after");
            BotStatus = $"pairs={LastPairs} done {scenario} closed={closed} played={RattlePlayed} volSum={RattleVolumeSum:F2} botSec={Time.time - t0:F1} " + LastShakeJson;
            Debug.Log("[PackBench] 봇 " + BotStatus);
        }
    }
}
