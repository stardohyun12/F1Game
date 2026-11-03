using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.PackBench
{
    // D-287: PackBench 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
    public partial class PackBench
    {
        // ── [E] 칩 ──
        void UpdatePrompt()
        {
            Prompt = ""; PromptAnchor = null;
            CurrentStep = ComputeStep();
            if (lifted == null && moving == null && tool == Tool.None && held == null)
            {
                var look = LookAt<PackItem>();
                BubbleSheet sh = null; PackItem target = null;
                if (look != null)
                {
                    if (heldSheet != null && look.sheet == null && !look.Wrapped && look.GetComponent<BoxCollider>() != null) { sh = heldSheet; target = look; }
                    else if (heldSheet == null) FindWrapPair(look, out sh, out target);
                }
                if (sh != null && target != null) { Prompt = "E 감싸기"; PromptAnchor = Bounds(target).center + Vector3.up * (Bounds(target).extents.y + 0.03f); return; }
            }
            if (held != null || lifted != null || moving != null || tool != Tool.None || heldSheet != null || pullSheet != null) return;
            var b = LookBox();
            // 과제 20261103-1827 메모 3: 칩 하나가 순서대로 바뀐다. E 로 실제 하는 일이 단계와 같을 때만 키를 단다(상자 고르기 · 닫고 테이프(열려 있을 때)).
            switch (CurrentStep)
            {
                case Step.Pick:
                    Prompt = b != null && b != benchBox ? "E 상자 고르기" : "상자 고르기";
                    PromptAnchor = b != null ? BoxChipAnchor(b) : (Vector3?)null;
                    return;
                case Step.Done:
                    Prompt = "Q 물러나기";
                    PromptAnchor = BoxChipAnchor(benchBox);
                    return;
            }
            string label = StepLabel(CurrentStep);
            bool eCloses = CurrentStep == Step.Close && benchBox != null && !benchBox.IsClosed && b == benchBox;
            Prompt = eCloses ? "E " + label : label;
            PromptAnchor = BoxChipAnchor(benchBox);
        }

        static Vector3 BoxChipAnchor(PackBox b) => b.transform.position + b.transform.up * (b.RimY + 0.02f);

        // ── 포장 단계(칩 순서) ──
        public enum Step { Pick, Floor, Put, Fill, Close, Shake, Done }
        public Step CurrentStep { get; private set; }
        /// <summary>달걀판 들어간 뒤 이 값(빈 부피 비율) 이하면 「빈틈 채우기」가 끝난 것. 실측: 완충재 다 채움 0.22 · 달걀판만 0.40.</summary>
        public const float FillGapOk = 0.30f;
        PackBox shakenBox; int shakenOpen;
        float gapAt = -9f, gapVal = 1f;

        public static string StepLabel(Step s)
        {
            switch (s)
            {
                case Step.Pick: return "상자 고르기";
                case Step.Floor: return "바닥 깔기";
                case Step.Put: return "물건 넣기";
                case Step.Fill: return "빈틈 채우기";
                case Step.Close: return "닫고 테이프";
                case Step.Shake: return "들어서 흔들기";
                default: return "물러나기";
            }
        }

        /// <summary>안내만 — 손 순서를 막지 않는다. 지금 상자 상태에서 다음에 할 일.</summary>
        Step ComputeStep()
        {
            var b = benchBox;
            if (b == null) return Step.Pick;
            if (b.IsClosed)
            {
                if (!b.Taped) return Step.Close;
                return shakenBox == b && shakenOpen == b.OpenCount ? Step.Done : Step.Shake;
            }
            bool eggs = false; int soft = 0;
            foreach (var it in b.Contents()) { if (it.kind == PackItemKind.Eggs) eggs = true; else soft++; }
            if (!eggs) return soft == 0 ? Step.Floor : Step.Put;
            if (Time.time - gapAt > 0.4f) { b.MeasureFill(out gapVal, out _); gapAt = Time.time; }   // 320 점 검사라 0.4 초에 한 번
            return gapVal > FillGapOk ? Step.Fill : Step.Close;
        }
    }
}
