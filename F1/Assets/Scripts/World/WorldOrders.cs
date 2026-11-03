using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>D-289 World 층: 화물 종류와 주문. 원천 = world/world.yaml. 첫 주문 보수만 Stages 가 Core.Rules 에 넣는다.
/// D-290: 포장(상자 3·완충재 2·화물 성질) — ApplyPacking 이 Core.PackRules 에 넣는다.</summary>
public class WorldOrders : ScriptableObject
{
    [Serializable]
    public class Cargo
    {
        public string name;
        public string weakTo;
        public string status;
    }

    [Serializable]
    public class Order
    {
        public string id;
        public string from;
        public string to;
        public string cargo;
        public int reward;
        /// <summary>배달지 — 씬 DP_* 이름(WorldPlaces.deliveryPoints 에 있다).</summary>
        public string dest;
    }

    [Serializable] public class Box { public string name; public Vector3 inner; }
    [Serializable] public class Filler { public string name; public string kind; public float thick; public int roll; }
    [Serializable] public class Trait { public string cargo; public string trait; public Vector3 size; }
    [Serializable] public class Cushion { public string trait; public float bubble, paper; }

    [Serializable]
    public class Packing
    {
        public List<Box> boxes = new List<Box>();
        public List<Filler> fillers = new List<Filler>();
        public List<Trait> traits = new List<Trait>();
        public List<Cushion> cushions = new List<Cushion>();
        public float snugGapMax = 0.03f, baseLoss = 0.25f, rattleLoss = 0.4f, rattleFull = 2f, looseLoss = 0.15f, minStart = 0.4f;
    }

    public List<Cargo> cargo = new List<Cargo>();
    public List<Order> orders = new List<Order>();
    public Packing packing = new Packing();

    public Order Find(string id) { foreach (var o in orders) if (o.id == id) return o; return null; }
    public Trait TraitOf(string cargoName) { foreach (var t in packing.traits) if (t.cargo == cargoName) return t; return null; }

    /// <summary>Core.PackRules 에 값을 넣는다(에셋이 없으면 Core 기본값 = yaml 값).</summary>
    public void ApplyPacking()
    {
        var p = packing;
        if (p == null || p.boxes.Count == 0) return;
        PackRules.SnugGapMax = p.snugGapMax; PackRules.BaseLoss = p.baseLoss; PackRules.RattleLoss = p.rattleLoss;
        PackRules.RattleFull = p.rattleFull; PackRules.LooseLoss = p.looseLoss; PackRules.MinStart = p.minStart;
        foreach (var c in p.cushions)
        {
            int t = PackRules.TraitIndex(c.trait);
            if (t < 0) continue;
            PackRules.Cushions[t, (int)PackFiller.Bubble] = c.bubble;
            PackRules.Cushions[t, (int)PackFiller.Paper] = c.paper;
        }
    }
}
