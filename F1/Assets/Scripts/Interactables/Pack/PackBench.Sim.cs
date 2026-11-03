using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PitCrew.PackBench
{
    // D-287: PackBench 의 한 조각 — 자리만 옮김(동작 그대로). 지도: docs/world/CODEMAP.md
    public partial class PackBench
    {
        // ══ 보관소 작업대(D-251) — 걷기 감독이 켜고 끈다 ══
        /// <summary>흔들어 보고 작업대에 다시 내려놓은 순간(상자, 결과).</summary>
        public static event System.Action<PackBox, PackBox.ShakeResult> Shaken;
        /// <summary>작업대를 떠나도 되나 — 든 상자가 아직 공중이거나 선반 상자가 옮겨지는 중이면 아니다.</summary>
        public bool CanLeave => lifted == null && moving == null;
        /// <summary>손에 든 물건·종이·도구를 놓는다. 든 상자는 내려놓기 시작(CanLeave 가 될 때까지 기다릴 것).</summary>
        public void ReleaseAll()
        {
            ReleaseLeft();                                  // 왼손을 뗀 것과 같은 길(구기던 종이 마무리·뽁뽁이 뜯기·든 상자 내려놓기)
            if (tool != Tool.None) PutToolBack();
            simHold = false; prevSimHold = false;
        }

        // ══ 봇·QA (마우스 대신 — 같은 동작을 스크립트로) ══
        public PackBox FindBox(string size) => boxes.Find(b => b.sizeName == size);

        /// <summary>QA: 손으로 집기·놓기·뽁뽁이 풀기를 마우스 없이(같은 코드 경로). 목표점은 SimTarget.</summary>
        public void SimGrab(PackItem it) { if (held != null) Drop(); Grab(it); }
        public void SimDrop() { if (held != null) Drop(); if (heldSheet != null) { heldSheet.Release(); heldSheet = null; } }
        public void SimGrabSheet(BubbleSheet sh) { GrabSheet(sh, sh.Center); }
        public bool SimPullStart()
        {
            foreach (var s in FindObjectsByType<PackSupply>(FindObjectsSortMode.None)) if (s.kind == PackSupplyKind.Bubble) { StartPull(s); return true; }
            return false;
        }
        public void SimPullStep() { if (pullSheet != null) HoldLeft(); }
        public BubbleSheet SimPullEnd() { var sh = pullSheet; if (sh != null) FinishPull(); return sh; }
        public Vector3 RollExit => rollExit;

        public PackItem Tray()
        {
            foreach (var it in FindObjectsByType<PackItem>(FindObjectsSortMode.None)) if (it.kind == PackItemKind.Eggs) return it;
            return null;
        }
    }
}
