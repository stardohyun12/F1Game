using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// D-289 World 층: 장소 데이터. 원천 = world/world.yaml, 이 에셋은 tools/world/gen.py 가 덮어쓴다(손으로 고치지 않는다).
/// spots = 디버그 창 순간이동 자리(순서가 GoPlace 번호) · deliveryPoints = 씬 DP_* 배달 지점.
/// </summary>
public class WorldPlaces : ScriptableObject
{
    [Serializable]
    public class Spot
    {
        public string name;
        public string role;
        public Vector3 feet;
        public float yaw;
        /// <summary>CartSpot | Near | BesideCar | Fixed (DebugPlaces.CarRule 이름).</summary>
        public string car;
        public Vector3 carAt;
        public float carYaw;
    }

    [Serializable]
    public class DeliveryPointData
    {
        public string name;
        public string label;
        public string role;
        public Vector3 pos;
        /// <summary>받는 사람 이름. 아직 없으면 빈 글.</summary>
        public string recipient;
    }

    /// <summary>이 에셋을 만든 world.yaml 의 SHA1(줄바꿈 LF 로 맞춘 뒤). 불변 검사가 yaml 이 더 새로우면 알린다.</summary>
    public string sourceHash;
    public List<Spot> spots = new List<Spot>();
    public List<DeliveryPointData> deliveryPoints = new List<DeliveryPointData>();
}
