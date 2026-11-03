using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>D-289 World 층: 화물 종류와 주문. 원천 = world/world.yaml. 첫 주문 보수만 Stages 가 Core.Rules 에 넣는다.</summary>
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

    public List<Cargo> cargo = new List<Cargo>();
    public List<Order> orders = new List<Order>();

    public Order Find(string id) { foreach (var o in orders) if (o.id == id) return o; return null; }
}
